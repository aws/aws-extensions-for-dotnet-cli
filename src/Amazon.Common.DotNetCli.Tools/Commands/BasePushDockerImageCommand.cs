using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR.Model;
using Amazon.ECR;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Common.DotNetCli.Tools.Commands
{
    public abstract class BasePushDockerImageCommand<TDefaultConfig> : BaseCommand<TDefaultConfig>, ICommand
        where TDefaultConfig : DefaultConfigFile, new()
    {
        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS,

            CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT,

            CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE,
            CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG_OBSOLETE,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS
        });


        PushDockerImagePropertyContainer _pushProperties;
        public PushDockerImagePropertyContainer PushDockerImageProperties
        {
            get
            {
                if (this._pushProperties == null)
                {
                    this._pushProperties = new PushDockerImagePropertyContainer();
                }

                return this._pushProperties;
            }
            set { this._pushProperties = value; }
        }


        public string PushedImageUri { get; private set; }

        public bool SkipPushToECR { get; set; }

        /// <summary>
        /// If this is set then the tag used to push to ECR will be made unique. The value is a string giving 
        /// a base known name by the user for the tag. This is typically the name of the CloudFormation resource.
        /// </summary>
        public string ImageTagUniqueSeed { get; set; }

        public BasePushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : this(logger, workingDirectory, CommandOptions, args)
        {
        }

        protected BasePushDockerImageCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> commandOptions, string[] args)
            : base(logger, workingDirectory, commandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            this.PushDockerImageProperties.ParseCommandArguments(values);
        }


        protected override async Task<bool> PerformActionAsync()
        {
            var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation);
            var dockerCli = new DockerCLIWrapper(this.Logger, projectLocation);

            string sourceDockerTag;
            string destinationDockerTag;

            var localDockerImage = this.GetStringValueOrDefault(this.PushDockerImageProperties.LocalDockerImage, CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE, false);

            // If there is no local tag then build the image
            if(string.IsNullOrEmpty(localDockerImage))
            {
                // In the case of building the image when we push the image the local image will have the same name as the ECR destionation tag.
                destinationDockerTag = DetermineDockerImageTag(true, projectLocation);
                sourceDockerTag = destinationDockerTag;

                BuildDockerImage(dockerCli, projectLocation, destinationDockerTag);
            }
            else 
            {
                destinationDockerTag = DetermineDockerImageTag(false, projectLocation);
                sourceDockerTag = localDockerImage;

                // If an ECR destination tag was not given then use the same tag as the local. That way the user isn't required to specify the --image-tag switch.
                if (string.IsNullOrEmpty(destinationDockerTag))
                {
                    destinationDockerTag = sourceDockerTag;
                    if (!destinationDockerTag.Contains(":"))
                        destinationDockerTag += ":latest";
                }
            }

            if (!this.SkipPushToECR)
            {
                string ecrTag;
                if(string.IsNullOrEmpty(ImageTagUniqueSeed))
                {
                    ecrTag = destinationDockerTag;
                }
                else
                {
                    var imageId = dockerCli.GetImageId(sourceDockerTag);
                    ecrTag = GenerateUniqueEcrTag(this.ImageTagUniqueSeed, destinationDockerTag, imageId);
                }

                await PushToECR(dockerCli, sourceDockerTag, ecrTag);
            }
            else
            {
                this.PushedImageUri = destinationDockerTag;
            }

            return true;
        }

        public static string GenerateUniqueEcrTag(string uniqueSeed, string destinationDockerTag, string imageId)
        {
            // If there is any problem getting the image ID from the CLI then default to Ticks to make sure the tag is unique.
            if (string.IsNullOrEmpty(imageId))
            {
                imageId = DateTime.UtcNow.Ticks.ToString();
            }

            var imageInfo = SplitImageTag(destinationDockerTag);
            var uniqueTag = $"{imageInfo.RepositoryName}:{uniqueSeed.ToLower()}-{imageId}-{imageInfo.Tag}";
            return uniqueTag;
        }

        private string DetermineDockerImageTag(bool required, string projectLocation)
        {
            // Try to get the tag from the newer --image-tag switch.
            var dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG, false)?.ToLower();
            if (string.IsNullOrEmpty(dockerImageTag))
            {
                // Since the newer --image-tag switch was not set attempt to get the value from the old obsolete --tag switch.
                dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG_OBSOLETE, false)?.ToLower();
                if (string.IsNullOrEmpty(dockerImageTag))
                {
                    // If docker tag is required and we still don't have then try and generate one.
                    // If it is not required then the push command is reusing a local image tag.
                    // If the generate fails then ask the user for the image tag.
                    if(required)
                    {
                        string generatedRepositoryName;
                        if (Utilities.TryGenerateECRRepositoryName(projectLocation, out generatedRepositoryName))
                        {
                            dockerImageTag = generatedRepositoryName;
                            Logger.WriteLine($"Creating image tag from project: {dockerImageTag}");
                        }
                        else
                        {
                            // If we still don't have a value ask one more time for the --image-tag switch but this time make it required.
                            dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG, required)?.ToLower();
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(dockerImageTag))
                return null;

            if (!dockerImageTag.Contains(":"))
                dockerImageTag += ":latest";

            return dockerImageTag;
        }

        private void BuildDockerImage(DockerCLIWrapper dockerCli, string projectLocation, string dockerImageTag)
        {
            var dockerFile = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerFile, CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE, false);
            if (string.IsNullOrEmpty(dockerFile))
            {
                dockerFile = Constants.DEFAULT_DOCKERFILE;
            }


            var hostBuildOutput = this.GetStringValueOrDefault(this.PushDockerImageProperties.HostBuildOutput, CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT, false);
            if (!string.IsNullOrEmpty(hostBuildOutput))
            {
                var configuration = this.GetStringValueOrDefault(this.PushDockerImageProperties.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false) ?? "Release";
                var targetFramework = this.GetStringValueOrDefault(this.PushDockerImageProperties.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
                string publishOptions = this.GetStringValueOrDefault(this.PushDockerImageProperties.PublishOptions, CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS, false);

                BuildProject(projectLocation, configuration, targetFramework, publishOptions, Path.Combine(projectLocation, hostBuildOutput));
            }

            var dockerDetails = InspectDockerFile(this.Logger, projectLocation, dockerFile);

            this.Logger?.WriteLine("Executing docker build");

            var dockerBuildWorkingDirectory = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerBuildWorkingDirectory, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, false);

            if (string.IsNullOrEmpty(dockerBuildWorkingDirectory))
            {
                dockerBuildWorkingDirectory = dockerDetails.BuildFromSolutionDirectory ? DetermineSolutionDirectory(projectLocation) : projectLocation;
            }
            else
            {
                if (!Path.IsPathRooted(dockerBuildWorkingDirectory))
                {
                    dockerBuildWorkingDirectory = Path.GetFullPath(Path.Combine(projectLocation, dockerBuildWorkingDirectory));
                }

                this.PushDockerImageProperties.DockerBuildWorkingDirectory = Utilities.RelativePathTo(projectLocation, dockerBuildWorkingDirectory);
            }

            var dockerBuildOptions = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerBuildOptions, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS, false);

            var fullDockerfilePath = Path.Combine(projectLocation, dockerFile);
            if (!Path.IsPathRooted(fullDockerfilePath))
            {
                fullDockerfilePath = Path.Combine(dockerBuildWorkingDirectory, fullDockerfilePath);
            }

            // The Docker CLI gives a poor error message if the Dockerfile does not exist. This does a preemptive check and gives a more 
            // meaningful error message.
            if (!File.Exists(fullDockerfilePath))
            {
                throw new ToolsException($"Error failed to find file \"{fullDockerfilePath}\" to build the Docker image", ToolsException.CommonErrorCode.DockerBuildFailed);
            }

            if (ExecuteDockerBuild(dockerCli, dockerBuildWorkingDirectory, fullDockerfilePath, dockerImageTag, dockerBuildOptions) != 0)
            {
                throw new ToolsException("Error executing \"docker build\"", ToolsException.CommonErrorCode.DockerBuildFailed);
            }
        }

        protected virtual int ExecuteDockerBuild(DockerCLIWrapper dockerCli, string dockerBuildWorkingDirectory, string fullDockerfilePath, string dockerImageTag, string dockerBuildOptions)
        {
            return dockerCli.Build(dockerBuildWorkingDirectory, fullDockerfilePath, dockerImageTag, dockerBuildOptions);
        }

        private async Task PushToECR(DockerCLIWrapper dockerCli, string sourceDockerImageTag, string destinationDockerImageTag)
        {
            var sourceRepoInfo = SplitImageTag(sourceDockerImageTag);

            await InitiateDockerLogin(dockerCli);

            var splitDestinationTag = SplitImageTag(destinationDockerImageTag);
            Repository repository = await SetupECRRepository(splitDestinationTag.RepositoryName);

            var targetTag = repository.RepositoryUri + ":" + splitDestinationTag.Tag;
            this.Logger?.WriteLine($"Taging image {sourceRepoInfo.FullTagName} with {destinationDockerImageTag}");
            if (dockerCli.Tag(sourceRepoInfo.FullTagName, targetTag) != 0)
            {
                throw new ToolsException("Error executing \"docker tag\"", ToolsException.CommonErrorCode.DockerTagFail);
            }

            this.Logger?.WriteLine("Pushing image to ECR repository");
            if (dockerCli.Push(targetTag) != 0)
            {
                throw new ToolsException("Error executing \"docker push\"", ToolsException.CommonErrorCode.DockerPushFail);
            }

            this.PushedImageUri = targetTag;
            this.Logger?.WriteLine($"Image {this.PushedImageUri} Push Complete. ");
        }

        public static RepoInfo SplitImageTag(string imageTag)
        {
            int pos = imageTag.IndexOf(':');
            if(pos == -1)
            {
                return new RepoInfo(imageTag, "latest");
            }

            string repo = imageTag.Substring(0, pos);

            string tag;
            // If the imageTag ended in a semicolon then use latest as the tag
            if (pos + 1 == imageTag.Length)
            {
                tag = "latest";
            }
            else
            {
                tag = imageTag.Substring(pos + 1);
            }

            return new RepoInfo(repo, tag);
        }

        protected virtual void BuildProject(string projectLocation, string configuration, string targetFramework, string publishOptions, string publishLocation)
        {
            this.EnsureInProjectDirectory();

            var dotnetCli = new DotNetCLIWrapper(this.Logger, projectLocation);
            this.Logger?.WriteLine("Executing publish command");
            if (dotnetCli.Publish(projectLocation, publishLocation, targetFramework, configuration, publishOptions) != 0)
            {
                throw new ToolsException("Error executing \"dotnet publish\"", ToolsException.CommonErrorCode.DotnetPublishFailed);
            }

        }

        private async Task<Repository> SetupECRRepository(string ecrRepositoryName)
        {
            try
            {
                DescribeRepositoriesResponse describeResponse = null;
                try
                {
                    describeResponse = await this.ECRClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { ecrRepositoryName }
                    });
                }
                catch (Exception e)
                {
                    if (!(e is RepositoryNotFoundException))
                    {
                        throw;
                    }
                }

                Repository repository;
                if (describeResponse != null && describeResponse.Repositories != null && describeResponse.Repositories.Count == 1)
                {
                    this.Logger?.WriteLine($"Found existing ECR Repository {ecrRepositoryName}");
                    repository = describeResponse.Repositories[0];
                }
                else
                {
                    this.Logger?.WriteLine($"Creating ECR Repository {ecrRepositoryName}");
                    repository = (await this.ECRClient.CreateRepositoryAsync(new CreateRepositoryRequest
                    {
                        RepositoryName = ecrRepositoryName
                    })).Repository;
                }

                return repository;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error determining Amazon ECR repository: {e.Message}", ToolsException.CommonErrorCode.FailedToSetupECRRepository);
            }
        }

        private async Task InitiateDockerLogin(DockerCLIWrapper dockerCLI)
        {
            try
            {
                this.Logger?.WriteLine("Fetching ECR authorization token to use to login with the docker CLI");
                var response = await this.ECRClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());

                if (response.AuthorizationData == null || response.AuthorizationData.Count == 0)
                {
                    throw new ToolsException("No authorization data returned from ECR", ToolsException.CommonErrorCode.GetECRAuthTokens);
                }

                var authTokenBytes = Convert.FromBase64String(response.AuthorizationData[0].AuthorizationToken);
                var authToken = Encoding.UTF8.GetString(authTokenBytes);
                var decodedTokens = authToken.Split(':');

                this.Logger?.WriteLine("Executing docker CLI login command");
                if (dockerCLI.Login(decodedTokens[0], decodedTokens[1], response.AuthorizationData[0].ProxyEndpoint) != 0)
                {
                    throw new ToolsException($"Error executing the docker login command", ToolsException.CommonErrorCode.DockerCLILoginFail);
                }
            }
            catch (ToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error logging on with the docker CLI: {e.Message}", ToolsException.CommonErrorCode.GetECRAuthTokens);
            }
        }

        protected override void SaveConfigFile(Dictionary<string, object> data)
        {
            this.PushDockerImageProperties.PersistSettings(this, data);
        }

        public static DockerDetails InspectDockerFile(IToolLogger logger, string projectLocation, string dockerfile)
        {
            var details = new DockerDetails();

            var projectFilename = DetermineProjectFile(projectLocation);
            var dockerFilePath = Path.Combine(projectLocation, dockerfile);
            if (File.Exists(dockerFilePath))
            {
                logger?.WriteLine("Inspecting Dockerfile to figure how to build project and docker image");
                using (var stream = File.OpenRead(dockerFilePath))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var noSpaceLine = line.Replace(" ", "");

                        if (noSpaceLine.StartsWith("COPY") && (noSpaceLine.EndsWith(".sln./") || (projectFilename != null && noSpaceLine.Contains("/" + projectFilename))))
                        {
                            details.BuildFromSolutionDirectory = true;
                            logger?.WriteLine("... Determined that docker build needs to be run from solution folder.");
                        }
                    }
                }
            }

            return details;
        }

        private static string DetermineProjectFile(string projectLocation)
        {
            var files = Directory.GetFiles(projectLocation, "*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 1)
                return Path.GetFileName(files[0]);
            files = Directory.GetFiles(projectLocation, "*.fsproj", SearchOption.TopDirectoryOnly);
            if (files.Length == 1)
                return Path.GetFileName(files[0]);

            return null;
        }

        public static string DetermineSolutionDirectory(string projectLocation)
        {
            if (Directory.GetFiles(projectLocation, "*.sln", SearchOption.TopDirectoryOnly).Length != 0)
                return projectLocation;

            var parent = Directory.GetParent(projectLocation)?.FullName;
            if (parent == null)
                throw new ToolsException("Unable to determine directory for the solution", ToolsException.CommonErrorCode.FailedToFindSolutionDirectory);

            return DetermineSolutionDirectory(parent);
        }

        public class DockerDetails
        {
            public bool BuildFromSolutionDirectory { get; set; }
        }

        public class PushDockerImagePropertyContainer
        {
            public string Configuration { get; set; }
            public string TargetFramework { get; set; }
            public string PublishOptions { get; set; }
            public string DockerImageTag { get; set; }
            public string DockerBuildWorkingDirectory { get; set; }
            public string DockerBuildOptions { get; set; }
            public string DockerFile { get; set; }
            public string HostBuildOutput { get; set; }
            public string LocalDockerImage { get; set; }

            public void ParseCommandArguments(CommandOptions values)
            {
                Tuple<CommandOption, CommandOptionValue> tuple;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                    this.Configuration = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                    this.TargetFramework = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.Switch)) != null)
                    this.PublishOptions = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.Switch)) != null)
                    this.DockerBuildWorkingDirectory = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.Switch)) != null)
                    this.DockerBuildOptions = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE.Switch)) != null)
                    this.DockerFile = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT.Switch)) != null)
                    this.HostBuildOutput = tuple.Item2.StringValue;
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE.Switch)) != null)
                    this.LocalDockerImage = tuple.Item2.StringValue;

                // Check the --image-tag or the old obsolete --tag for an ECR Image tag.
                if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG.Switch)) != null)
                    this.DockerImageTag = tuple.Item2.StringValue;
                else if (string.IsNullOrEmpty(this.DockerImageTag) && (tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG_OBSOLETE.Switch)) != null)
                    this.DockerImageTag = tuple.Item2.StringValue;
            }


            public void PersistSettings(BaseCommand<TDefaultConfig> command, Dictionary<string, object> data)
            {
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, command.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, command.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.ConfigFileKey, command.GetStringValueOrDefault(this.PublishOptions, CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS, false));

                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE.ConfigFileKey, command.GetStringValueOrDefault(this.DockerFile, CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE, false));

                var tag = command.GetStringValueOrDefault(this.DockerImageTag, CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG, false);
                if (!string.IsNullOrEmpty(tag))
                {
                    // Strip the full ECR URL name of form - protocol://aws_account_id.dkr.ecr.region.amazonaws.domain/repository:tag
                    // irrespective of domain
                    int dkrPos = tag.IndexOf(".dkr.ecr");
                    if (dkrPos != -1)
                    {
                        tag = tag.Substring(dkrPos + 1);
                        int pos = tag.IndexOf('/');
                        if (pos != -1)
                        {
                            tag = tag.Substring(pos + 1);
                        }
                    }

                    data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG.ConfigFileKey, tag);
                }

                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.ConfigFileKey, command.GetStringValueOrDefault(this.DockerBuildWorkingDirectory, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.ConfigFileKey, command.GetStringValueOrDefault(this.DockerBuildOptions, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS, false));
                data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT.ConfigFileKey, command.GetStringValueOrDefault(this.HostBuildOutput, CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT, false));
            }
        }

        public class RepoInfo
        {
            public RepoInfo(string repositoryName, string tag)
            {
                this.RepositoryName = repositoryName;
                this.Tag = tag ?? "latest";
            }

            public string RepositoryName { get; }
            public string Tag { get; set; }

            public string FullTagName
            {
                get
                {
                    return $"{this.RepositoryName}:{this.Tag}";
                }
            }
        }
    }
}
