using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    public class PackageCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "package";
        public const string COMMAND_DESCRIPTION = "Command to package a Lambda project either into a zip file or docker image if --package-type is set to \"image\". The output can later be deployed to Lambda " +
                                                  "with either deploy-function command or with another tool.";
        public const string COMMAND_ARGUMENTS = "<ZIP-FILE> The name of the zip file to package the project into";

        public static readonly IList<CommandOption> PackageCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE,
            CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS,
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE,
            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
            LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK,

            LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE,
            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_TAG,
            CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY,
            CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT,

            LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD,
            LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD,
            LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY
        });

        public string Architecture { get; set; }
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string OutputPackageFileName { get; set; }
        
        public string MSBuildParameters { get; set; }
        public string[] LayerVersionArns { get; set; }

        public bool? DisableVersionCheck { get; set; }

        public string PackageType { get; set; }
        public string DockerFile { get; set; }
        public string DockerBuildOptions { get; set; }
        public string DockerBuildWorkingDirectory { get; set; }
        public string DockerImageTag { get; set; }
        public string HostBuildOutput { get; set; }
        public bool? UseContainerForBuild { get; set; }
        public string ContainerImageForBuild { get; set; }
        public string CodeMountDirectory { get; private set; }

        /// <summary>
        /// Property set when the package command is being created from another command or tool
        /// and the service clients have been copied over. In that case there is no reason
        /// to look for a region or aws credentials.
        /// </summary>
        public bool DisableRegionAndCredentialsCheck { get; set; }

        
        /// <summary>
        /// If runtime package store layers were specified the DOTNET_SHARED_STORE environment variable
        /// has to be set. This property will contain the value the environment variable must be set.
        /// </summary>
        public string NewDotnetSharedStoreValue { get; private set; }
        

        /// <summary>
        /// If the value for Payload points to an existing file then the contents of the file is sent, otherwise
        /// value of Payload is sent as the input to the function.
        /// </summary>
        public string Payload { get; set; }

        public PackageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, PackageCommandOptions, args)
        {
        }


        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.OutputPackageFileName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.Switch)) != null)
                this.OutputPackageFileName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.Switch)) != null)
                this.DisableVersionCheck = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.Switch)) != null)
                this.MSBuildParameters = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS.Switch)) != null)
                this.LayerVersionArns = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.Switch)) != null)
                this.Architecture = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD.Switch)) != null)
                this.UseContainerForBuild = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD.Switch)) != null)
                this.ContainerImageForBuild = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY.Switch)) != null)
                this.CodeMountDirectory = tuple.Item2.StringValue;

            if (!string.IsNullOrEmpty(values.MSBuildParameters))
            {
                if (this.MSBuildParameters == null)
                    this.MSBuildParameters = values.MSBuildParameters;
                else
                    this.MSBuildParameters += " " + values.MSBuildParameters;
            }

            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE.Switch)) != null)
                this.PackageType = tuple.Item2.StringValue;

            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE.Switch)) != null)
                this.DockerFile = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.Switch)) != null)
                this.DockerBuildOptions = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.Switch)) != null)
                this.DockerBuildWorkingDirectory = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_TAG.Switch)) != null)
                this.DockerImageTag = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT.Switch)) != null)
                this.HostBuildOutput = tuple.Item2.StringValue;
        }

        protected override async Task<bool> PerformActionAsync()
        {
            EnsureInProjectDirectory();

            // Disable interactive since this command is intended to be run as part of a pipeline.
            this.DisableInteractive = true;

            string projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));

            Lambda.PackageType packageType = LambdaUtilities.DeterminePackageType(this.GetStringValueOrDefault(this.PackageType, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE, false));
            if(packageType == Lambda.PackageType.Image)
            {
                var pushResults = await PushLambdaImageAsync();

                if (!pushResults.Success)
                {
                    if (pushResults.LastException != null)
                        throw pushResults.LastException;

                    throw new LambdaToolsException("Failed to push container image to ECR.", LambdaToolsException.LambdaErrorCode.FailedToPushImage);
                }
            }
            else
            {
                var layerVersionArns = this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false);
                LayerPackageInfo layerPackageInfo = null;
                if (layerVersionArns != null)
                {
                    if (!this.DisableRegionAndCredentialsCheck)
                    {
                        // Region and credentials are only required if using layers. This is new behavior so do a preemptive check when there are layers to
                        // see if region and credentials are set. If they are not set give a specific error message about region and credentials required
                        // when using layers.
                        try
                        {
                            base.DetermineAWSRegion();
                        }
                        catch (Exception)
                        {
                            throw new ToolsException("Region is required for the package command when layers are specified. The layers must be inspected to see how they affect packaging.", ToolsException.CommonErrorCode.RegionNotConfigured);
                        }
                        try
                        {
                            base.DetermineAWSCredentials();
                        }
                        catch (Exception)
                        {
                            throw new ToolsException("AWS credentials are required for the package command when layers are specified. The layers must be inspected to see how they affect packaging.", ToolsException.CommonErrorCode.InvalidCredentialConfiguration);
                        }
                    }

                    layerPackageInfo = await LambdaUtilities.LoadLayerPackageInfos(this.Logger, this.LambdaClient, this.S3Client, layerVersionArns);
                }
                else
                {
                    layerPackageInfo = new LayerPackageInfo();
                }

                // Release will be the default configuration if nothing set.
                var configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false);

                var msbuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false);
                var targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
                if (string.IsNullOrEmpty(targetFramework))
                {
                    targetFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectLocation, msbuildParameters);

                    // If we still don't know what the target framework is ask the user what targetframework to use.
                    // This is common when a project is using multi targeting.
                    if (string.IsNullOrEmpty(targetFramework))
                    {
                        targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                    }
                }

                bool isNativeAot = Utilities.LookPublishAotFlag(projectLocation, msbuildParameters);

                var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);
                var disableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault();

                var zipArchivePath = GetStringValueOrDefault(this.OutputPackageFileName, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false);

                string publishLocation;
                var success = LambdaPackager.CreateApplicationBundle(defaults: this.DefaultConfig,
                                                                     logger: this.Logger,
                                                                     workingDirectory: this.WorkingDirectory,
                                                                     projectLocation: projectLocation,
                                                                     configuration: configuration,
                                                                     targetFramework: targetFramework,
                                                                     msbuildParameters: msbuildParameters,
                                                                     architecture: architecture,
                                                                     disableVersionCheck: disableVersionCheck,
                                                                     layerPackageInfo: layerPackageInfo,
                                                                     isNativeAot: isNativeAot,
                                                                     useContainerForBuild: GetBoolValueOrDefault(this.UseContainerForBuild, LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD, false),
                                                                     containerImageForBuild: GetStringValueOrDefault(this.ContainerImageForBuild, LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD, false),
                                                                     codeMountDirectory: GetStringValueOrDefault(this.CodeMountDirectory, LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY, false),
                                                                     publishLocation: out publishLocation, 
                                                                     zipArchivePath: ref zipArchivePath);
                if (!success)
                {
                    throw new LambdaToolsException("Failed to create Lambda deployment bundle.", ToolsException.CommonErrorCode.DotnetPublishFailed);
                }


                this.Logger.WriteLine("Lambda project successfully packaged: " + zipArchivePath);
                var dotnetSharedStoreValue = layerPackageInfo.GenerateDotnetSharedStoreValue();
                if (!string.IsNullOrEmpty(dotnetSharedStoreValue))
                {
                    this.NewDotnetSharedStoreValue = dotnetSharedStoreValue;

                    this.Logger.WriteLine($"\nWarning: You must set the {LambdaConstants.ENV_DOTNET_SHARED_STORE} environment variable when deploying the package. " +
                                          "If not set the layers specified will not be located by the .NET Core runtime. The trailing '/' is required.");
                    this.Logger.WriteLine($"{LambdaConstants.ENV_DOTNET_SHARED_STORE}: {dotnetSharedStoreValue}");
                }
            }

            return true;
        }


        private async Task<PushLambdaImageResult> PushLambdaImageAsync()
        {
            var pushCommand = new PushDockerImageCommand(this.Logger, this.WorkingDirectory, this.OriginalCommandLineArguments)
            {
                ConfigFile = this.ConfigFile,
                DisableInteractive = this.DisableInteractive,
                Credentials = this.Credentials,
                ECRClient = this.ECRClient,
                Profile = this.Profile,
                ProfileLocation = this.ProfileLocation,
                ProjectLocation = this.ProjectLocation,
                Region = this.Region,
                WorkingDirectory = this.WorkingDirectory,
                SkipPushToECR = true,

                PushDockerImageProperties = new BasePushDockerImageCommand<LambdaToolsDefaults>.PushDockerImagePropertyContainer
                {
                    Configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false),
                    TargetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false),

                    DockerFile = this.GetStringValueOrDefault(this.DockerFile, CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE, false),
                    DockerBuildOptions = this.GetStringValueOrDefault(this.DockerBuildOptions, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS, false),
                    DockerBuildWorkingDirectory = this.GetStringValueOrDefault(this.DockerBuildWorkingDirectory, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, false),
                    DockerImageTag = this.GetStringValueOrDefault(this.DockerImageTag, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_TAG, false),
                    PublishOptions = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false),
                    HostBuildOutput = this.GetStringValueOrDefault(this.HostBuildOutput, CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT, false)
                }
            };

            var result = new PushLambdaImageResult();
            result.Success = await pushCommand.ExecuteAsync();
            result.LastException = pushCommand.LastException;

            if(result.Success)
            {
                this.Logger.WriteLine($"Packaged project as image: \"{pushCommand.PushedImageUri}\"");
            }

            return result;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.ConfigFileKey, this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.ConfigFileKey, this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.ConfigFileKey, this.GetStringValueOrDefault(this.OutputPackageFileName, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.ConfigFileKey, this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD.ConfigFileKey, this.GetBoolValueOrDefault(this.UseContainerForBuild, LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD.ConfigFileKey, this.GetStringValueOrDefault(this.ContainerImageForBuild, LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY.ConfigFileKey, this.GetStringValueOrDefault(this.CodeMountDirectory, LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY, false));
        }
    }
}