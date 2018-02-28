using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR.Model;
using Amazon.ECR;
using ThirdParty.Json.LitJson;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ECS.Tools.Commands
{
    public class PushDockerImageCommand : ECSBaseCommand
    {
        public const string COMMAND_NAME = "push-image";
        public const string COMMAND_DESCRIPTION = "Execute \"dotnet publish\", \"docker build\" and then push the image to Amazon ECR.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG,

            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
        });


        PushDockerImageProperties _pushProperties;
        public PushDockerImageProperties PushDockerImageProperties
        {
            get
            {
                if (this._pushProperties == null)
                {
                    this._pushProperties = new PushDockerImageProperties();
                }

                return this._pushProperties;
            }
            set { this._pushProperties = value; }
        }

        public bool? PersistConfigFile { get; set; }


        public string PushedImageUri { get; private set; }


        public PushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
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

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
        }


        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                string configuration = this.GetStringValueOrDefault(this.PushDockerImageProperties.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false) ?? "Release";
                string targetFramework = this.GetStringValueOrDefault(this.PushDockerImageProperties.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
                this.PushDockerImageProperties.DockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG, true).ToLower();

                if (!this.PushDockerImageProperties.DockerImageTag.Contains(":"))
                    this.PushDockerImageProperties.DockerImageTag += ":latest";

                var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation);
                var dockerDetails = InspectDockerFile(projectLocation);


                if (!dockerDetails.SkipDotnetBuild)
                {
                    var dotnetCli = new DotNetCLIWrapper(this.Logger, projectLocation);
                    this.Logger?.WriteLine("Executing publish command");
                    if (dotnetCli.Publish(projectLocation, dockerDetails.ExpectedPublishLocation, targetFramework, configuration) != 0)
                    {
                        throw new DockerToolsException("Error executing \"dotnet publish\"", DockerToolsException.CommonErrorCode.DotnetPublishFailed);
                    }
                }

                var dockerCli = new DockerCLIWrapper(this.Logger, projectLocation);
                this.Logger?.WriteLine("Executing docker build");

                string dockerBuildWorkingDirectory = dockerDetails.BuildFromSolutionDirectory ? DetermineSolutionDirectory(projectLocation) : projectLocation;

                if (dockerCli.Build(this.DefaultConfig, dockerBuildWorkingDirectory, Path.Combine(projectLocation, "Dockerfile"), this.PushDockerImageProperties.DockerImageTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker build\"", DockerToolsException.ECSErrorCode.DockerBuildFailed);
                }

                await InitiateDockerLogin(dockerCli);

                Repository repository = await SetupECRRepository(this.PushDockerImageProperties.DockerImageTag.Substring(0, this.PushDockerImageProperties.DockerImageTag.IndexOf(':')));

                var targetTag = repository.RepositoryUri + this.PushDockerImageProperties.DockerImageTag.Substring(this.PushDockerImageProperties.DockerImageTag.IndexOf(':'));
                this.Logger?.WriteLine($"Taging image {this.PushDockerImageProperties.DockerImageTag} with {targetTag}");
                if (dockerCli.Tag(this.PushDockerImageProperties.DockerImageTag, targetTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker tag\"", DockerToolsException.ECSErrorCode.DockerTagFail);
                }

                this.Logger?.WriteLine("Pushing image to ECR repository");
                if (dockerCli.Push(targetTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker push\"", DockerToolsException.ECSErrorCode.DockerPushFail);
                }

                this.PushedImageUri = targetTag;
                this.Logger?.WriteLine($"Image {this.PushedImageUri} Push Complete. ");

                if (this.GetBoolValueOrDefault(this.PersistConfigFile, CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE, false).GetValueOrDefault())
                {
                    this.SaveConfigFile();
                }

            }
            catch (DockerToolsException e)
            {
                this.Logger?.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger?.WriteLine($"Unknown error executing docker push to Amazon Elastic Container Registry: {e.Message}");
                this.Logger?.WriteLine(e.StackTrace);
                return false;
            }
            return true;
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
                if (describeResponse != null && describeResponse.Repositories.Count == 1)
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
                throw new DockerToolsException($"Error determining Amazon ECR repository: {e.Message}", DockerToolsException.ECSErrorCode.FailedToSetupECRRepository);
            }
        }

        private async Task InitiateDockerLogin(DockerCLIWrapper dockerCLI)
        {
            try
            {
                this.Logger?.WriteLine("Fetching ECR authorization token to use to login with the docker CLI");
                var response = await this.ECRClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());

                var authTokenBytes = Convert.FromBase64String(response.AuthorizationData[0].AuthorizationToken);
                var authToken = Encoding.UTF8.GetString(authTokenBytes);
                var decodedTokens = authToken.Split(':');

                this.Logger?.WriteLine("Executing docker CLI login command");
                if (dockerCLI.Login(decodedTokens[0], decodedTokens[1], response.AuthorizationData[0].ProxyEndpoint) != 0)
                {
                    throw new DockerToolsException($"Error executing the docker login command", DockerToolsException.ECSErrorCode.DockerCLILoginFail);
                }
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error logging on with the docker CLI: {e.Message}", DockerToolsException.ECSErrorCode.GetECRAuthTokens);
            }
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.PushDockerImageProperties.PersistSettings(this, data);
        }

        private DockerDetails InspectDockerFile(string projectLocation)
        {
            var details = new DockerDetails();

            var dockerFilePath = Path.Combine(projectLocation, "Dockerfile");
            if (File.Exists(dockerFilePath))
            {
                this.Logger?.WriteLine("Inspecting Dockerfile to figure how to build project and docker image");
                using (var stream = File.OpenRead(dockerFilePath))
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var noSpaceLine = line.Replace(" ", "");

                        if (line.StartsWith("COPY ") && line.Contains(":-"))
                        {
                            int start = line.IndexOf(":-") + 2;
                            int end = line.IndexOf('}', start);
                            if (end == -1)
                                continue;

                            details.ExpectedPublishLocation = line.Substring(start, end - start).Trim();
                            this.Logger?.WriteLine("... Determined dotnet publish location configured to: " + details.ExpectedPublishLocation);
                            break;
                        }
                        else if(noSpaceLine.StartsWith("RUNdotnetpublish"))
                        {
                            details.SkipDotnetBuild = true;
                            this.Logger?.WriteLine("... Skip building project since it is done as part of Dockerfile");
                        }
                        else if(noSpaceLine.StartsWith("COPY") && noSpaceLine.EndsWith(".sln./"))
                        {
                            details.BuildFromSolutionDirectory = true;
                            this.Logger?.WriteLine("... Determined that docker build needs to be run from solution folder.");
                        }
                    }
                }

                if(!details.SkipDotnetBuild && string.IsNullOrEmpty(details.ExpectedPublishLocation))
                {
                    details.ExpectedPublishLocation = "obj/Docker/publish";
                    this.Logger?.WriteLine("Warning: unable to determine dotnet publish folder location that Dockerfile expects. Assuming Visual Studio's default of " + details.ExpectedPublishLocation);
                }
            }

            return details;
        }

        public static string DetermineSolutionDirectory(string projectLocation)
        {
            if (Directory.GetFiles(projectLocation, "*.sln", SearchOption.TopDirectoryOnly).Length != 0)
                return projectLocation;

            var parent = Directory.GetParent(projectLocation)?.FullName;
            if (parent == null)
                throw new DockerToolsException("Unable to determine directory for the solution", DockerToolsException.ECSErrorCode.FailedToFindSolutionDirectory);

            return DetermineSolutionDirectory(parent);
        }

        public class DockerDetails
        {
            public bool BuildFromSolutionDirectory { get; set; }
            public bool SkipDotnetBuild { get; set; }
            public string ExpectedPublishLocation { get; set; }
        }
    }
}
