using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


using Amazon.Lambda.Model;


using ThirdParty.Json.LitJson;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Runtime.Internal;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Command to deploy a function to AWS Lambda. When redeploying an existing function only function configuration properties
    /// that were explicitly set will be used. Default function configuration values are ignored for redeploy. This
    /// is to avoid any accidental changes to the function.
    /// </summary>
    public class DeployFunctionCommand : UpdateFunctionConfigCommand
    {
        public const string COMMAND_DEPLOY_NAME = "deploy-function";
        public const string COMMAND_DEPLOY_DESCRIPTION = "Command to deploy the project to AWS Lambda";
        public const string COMMAND_DEPLOY_ARGUMENTS = "<FUNCTION-NAME> The name of the function to deploy";

        public static readonly IList<CommandOption> DeployCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS,
            LambdaDefinedCommandOptions.ARGUMENT_PACKAGE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION,
            LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS,

            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT,
            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND,
            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY,
            CommonDefinedCommandOptions.ARGUMENT_DOCKER_TAG,

            LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS,
            LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN,
            LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE,
            LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN,
            LambdaDefinedCommandOptions.ARGUMENT_APPLY_DEFAULTS_FOR_UPDATE_OBSOLETE,
            LambdaDefinedCommandOptions.ARGUMENT_RESOLVE_S3,
            LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET,
            LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK,

            CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE,
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
        public string Package { get; set; }
        public string MSBuildParameters { get; set; }

        public bool? ResolveS3 { get; set; }
        public string S3Bucket { get; set; }
        public string S3Prefix { get; set; }

        public bool? DisableVersionCheck { get; set; }

        public string DockerFile { get; set; }
        public string DockerBuildOptions { get; set; }
        public string DockerBuildWorkingDirectory { get; set; }
        public string DockerImageTag { get; set; }

        public string HostBuildOutput { get; set; }
        public string LocalDockerImage { get; set; }

        public bool? UseContainerForBuild { get; set; }

        public string ContainerImageForBuild { get; set; }
        public string CodeMountDirectory { get;  set; }

        public DeployFunctionCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, DeployCommandOptions, args)
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
                this.FunctionName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_PACKAGE.Switch)) != null)
                this.Package = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_RESOLVE_S3.Switch)) != null)
                this.ResolveS3 = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.Switch)) != null)
                this.S3Bucket = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.Switch)) != null)
                this.S3Prefix = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.Switch)) != null)
                this.DisableVersionCheck = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.Switch)) != null)
                this.Architecture = tuple.Item2.StringValue;

            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.Switch)) != null)
                this.MSBuildParameters = tuple.Item2.StringValue;

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
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE.Switch)) != null)
                this.LocalDockerImage = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD.Switch)) != null)
                this.UseContainerForBuild = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD.Switch)) != null)
                this.ContainerImageForBuild = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY.Switch)) != null)
                this.CodeMountDirectory = tuple.Item2.StringValue;
        }



        protected override async Task<bool> PerformActionAsync()
        {
            string projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));
            string zipArchivePath = null;
            string package = this.GetStringValueOrDefault(this.Package, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE, false);

            var layerVersionArns = this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false);
            var layerPackageInfo = await LambdaUtilities.LoadLayerPackageInfos(this.Logger, this.LambdaClient, this.S3Client, layerVersionArns);

            var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);

            Lambda.PackageType packageType = DeterminePackageType();
            string ecrImageUri = null;

            if (packageType == Lambda.PackageType.Image)
            {
                var pushResults = await PushLambdaImageAsync();

                if (!pushResults.Success)
                {
                    if (pushResults.LastToolsException != null)
                        throw pushResults.LastToolsException;

                    return false;
                }

                ecrImageUri = pushResults.ImageUri;
            }
            else
            {
                if (string.IsNullOrEmpty(package))
                {
                    EnsureInProjectDirectory();

                    // Release will be the default configuration if nothing set.
                    string configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false);

                    var targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
                    if (string.IsNullOrEmpty(targetFramework))
                    {
                        targetFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectLocation);

                        // If we still don't know what the target framework is ask the user what targetframework to use.
                        // This is common when a project is using multi targeting.
                        if(string.IsNullOrEmpty(targetFramework))
                        {
                            targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                        }
                    }
                    string msbuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false);

                    bool isNativeAot = Utilities.LookPublishAotFlag(projectLocation, this.MSBuildParameters);

                    ValidateTargetFrameworkAndLambdaRuntime(targetFramework);

                    bool disableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault();
                    string publishLocation;
                    LambdaPackager.CreateApplicationBundle(defaults: this.DefaultConfig,
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
                                                            zipArchivePath: ref zipArchivePath
                                                            );

                    if (string.IsNullOrEmpty(zipArchivePath))
                        return false;
                }
                else
                {
                    if (!File.Exists(package))
                        throw new LambdaToolsException($"Package {package} does not exist", LambdaToolsException.LambdaErrorCode.InvalidPackage);
                    if (!string.Equals(Path.GetExtension(package), ".zip", StringComparison.OrdinalIgnoreCase))
                        throw new LambdaToolsException($"Package {package} must be a zip file", LambdaToolsException.LambdaErrorCode.InvalidPackage);

                    this.Logger.WriteLine($"Skipping compilation and using precompiled package {package}");
                    zipArchivePath = package;
                }
            }


            MemoryStream lambdaZipArchiveStream = null;
            if(zipArchivePath != null)
            {
                lambdaZipArchiveStream = new MemoryStream(File.ReadAllBytes(zipArchivePath));
            }
            try
            {
                var s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false);
                bool? resolveS3 = this.GetBoolValueOrDefault(this.ResolveS3, LambdaDefinedCommandOptions.ARGUMENT_RESOLVE_S3, false);
                string s3Key = null;
                if (zipArchivePath != null && (resolveS3 == true || !string.IsNullOrEmpty(s3Bucket)))
                {
                    if(string.IsNullOrEmpty(s3Bucket))
                    {
                        s3Bucket = await LambdaUtilities.ResolveDefaultS3Bucket(this.Logger, this.S3Client, this.STSClient);
                    }
                    else
                    {
                        await Utilities.ValidateBucketRegionAsync(this.S3Client, s3Bucket);
                    }

                    var functionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true);
                    var s3Prefix = this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false);
                    s3Key = await Utilities.UploadToS3Async(this.Logger, this.S3Client, s3Bucket, s3Prefix, functionName, lambdaZipArchiveStream);
                }


                var currentConfiguration = await GetFunctionConfigurationAsync();
                if (currentConfiguration == null)
                {
                    this.Logger.WriteLine($"Creating new Lambda function {this.FunctionName}");
                    var createRequest = new CreateFunctionRequest
                    {
                        PackageType = packageType,
                        FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true),
                        Description = this.GetStringValueOrDefault(this.Description, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION, false),
                        Role = this.GetRoleValueOrDefault(this.Role, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE, 
                            Constants.LAMBDA_PRINCIPAL, LambdaConstants.AWS_LAMBDA_MANAGED_POLICY_PREFIX, 
                            LambdaConstants.KNOWN_MANAGED_POLICY_DESCRIPTIONS, true),
                        
                        Publish = this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false).GetValueOrDefault(),
                        MemorySize = this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, true).GetValueOrDefault(),
                        Timeout = this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, true).GetValueOrDefault(),
                        KMSKeyArn = this.GetStringValueOrDefault(this.KMSKeyArn, LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, false),
                        VpcConfig = new VpcConfig
                        {
                            SubnetIds = this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false)?.ToList(),
                            SecurityGroupIds = this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false)?.ToList()
                        }
                    };

                    var ephemeralSize = this.GetIntValueOrDefault(this.EphemeralStorageSize, LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE, false);
                    if(ephemeralSize.HasValue)
                    {
                        createRequest.EphemeralStorage = new EphemeralStorage
                        {
                            Size = ephemeralSize.Value
                        };
                    }

                    if (!string.IsNullOrEmpty(architecture))
                    {
                        createRequest.Architectures = new List<string> { architecture };
                    }

                    if(packageType == Lambda.PackageType.Zip)
                    {
                        createRequest.Handler = this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, true);
                        createRequest.Runtime = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, true);
                        createRequest.Layers = layerVersionArns?.ToList();

                        if (s3Bucket != null)
                        {
                            createRequest.Code = new FunctionCode
                            {
                                S3Bucket = s3Bucket,
                                S3Key = s3Key
                            };
                        }
                        else
                        {
                            createRequest.Code = new FunctionCode
                            {
                                ZipFile = lambdaZipArchiveStream
                            };
                        }
                    }
                    else if(packageType == Lambda.PackageType.Image)
                    {
                        createRequest.Code = new FunctionCode
                        {
                            ImageUri = ecrImageUri
                        };

                        createRequest.ImageConfig = new ImageConfig
                        {
                            Command = this.GetStringValuesOrDefault(this.ImageCommand, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND, false)?.ToList(),
                            EntryPoint = this.GetStringValuesOrDefault(this.ImageEntryPoint, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT, false)?.ToList(),
                            WorkingDirectory = this.GetStringValueOrDefault(this.ImageWorkingDirectory, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY, false)
                        };
                    }

                    var environmentVariables = GetEnvironmentVariables(null);

                    var dotnetShareStoreVal = layerPackageInfo.GenerateDotnetSharedStoreValue();
                    if(!string.IsNullOrEmpty(dotnetShareStoreVal))
                    {
                        if(environmentVariables == null)
                        {
                            environmentVariables = new Dictionary<string, string>();
                        }
                        environmentVariables[LambdaConstants.ENV_DOTNET_SHARED_STORE] = dotnetShareStoreVal;
                    }

                    if (environmentVariables != null && environmentVariables.Count > 0)
                    {
                        createRequest.Environment = new Model.Environment
                        {
                            Variables = environmentVariables
                        };

                    }

                    var tags = this.GetKeyValuePairOrDefault(this.Tags, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS, false);
                    if(tags != null && tags.Count > 0)
                    {
                        createRequest.Tags = tags;
                    }

                    var deadLetterQueue = this.GetStringValueOrDefault(this.DeadLetterTargetArn, LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN, false);
                    if(!string.IsNullOrEmpty(deadLetterQueue))
                    {
                        createRequest.DeadLetterConfig = new DeadLetterConfig {TargetArn = deadLetterQueue };
                    }

                    var tracingMode = this.GetStringValueOrDefault(this.TracingMode, LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE, false);
                    if(!string.IsNullOrEmpty(tracingMode))
                    {
                        createRequest.TracingConfig = new TracingConfig { Mode = tracingMode };
                    }


                    try
                    {
                        await this.LambdaClient.CreateFunctionAsync(createRequest);
                        this.Logger.WriteLine("New Lambda function created");
                    }
                    catch (Exception e)
                    {
                        throw new LambdaToolsException($"Error creating Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaCreateFunction, e);
                    }

                    if(this.GetBoolValueOrDefault(this.FunctionUrlEnable, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE, false).GetValueOrDefault())
                    {
                        var authType = this.GetStringValueOrDefault(this.FunctionUrlAuthType, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH, false);
                        await base.CreateFunctionUrlConfig(createRequest.FunctionName, authType);
                        this.Logger.WriteLine($"Function url config created: {this.FunctionUrlLink}");
                    }
                }
                else
                {
                    this.Logger.WriteLine($"Updating code for existing function {this.FunctionName}");

                    var updateCodeRequest = new UpdateFunctionCodeRequest
                    {
                        FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)
                    };

                    if (!string.IsNullOrEmpty(architecture))
                    {
                        updateCodeRequest.Architectures = new List<string> { architecture };
                    }

                    // In case the function is currently being updated from previous deployment wait till it available
                    // to be updated.
                    if (currentConfiguration.LastUpdateStatus == LastUpdateStatus.InProgress)
                    {
                        await LambdaUtilities.WaitTillFunctionAvailableAsync(Logger, this.LambdaClient, updateCodeRequest.FunctionName);
                    }

                    if (packageType == Lambda.PackageType.Zip)
                    {
                        if (s3Bucket != null)
                        {
                            updateCodeRequest.S3Bucket = s3Bucket;
                            updateCodeRequest.S3Key = s3Key;
                        }
                        else
                        {
                            updateCodeRequest.ZipFile = lambdaZipArchiveStream;
                        }
                    }
                    else if (packageType == Lambda.PackageType.Image)
                    {
                        updateCodeRequest.ImageUri = ecrImageUri;
                    }

                    var configUpdated = false;
                    try
                    {
                        // Update config should run before updating the function code to avoid a situation such as
                        // upgrading from an EOL .NET version to a supported version where the update would fail 
                        // since lambda thinks we are updating an EOL version instead of upgrading.
                        configUpdated = await base.UpdateConfigAsync(currentConfiguration, layerPackageInfo.GenerateDotnetSharedStoreValue());
                    }
                    catch (Exception e)
                    {
                        throw new LambdaToolsException($"Error updating configuration for Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaUpdateFunctionConfiguration, e);
                    }

                    try
                    {
                        await LambdaUtilities.WaitTillFunctionAvailableAsync(Logger, this.LambdaClient, updateCodeRequest.FunctionName);
                        await this.LambdaClient.UpdateFunctionCodeAsync(updateCodeRequest);
                    }
                    catch (Exception e)
                    {
                        if (configUpdated)
                        {
                            await base.AttemptRevertConfigAsync(currentConfiguration);
                        }
                        throw new LambdaToolsException($"Error updating code for Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaUpdateFunctionCode, e);
                    }

                    await base.ApplyTags(currentConfiguration.FunctionArn);

                    var publish = this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false).GetValueOrDefault();
                    if(publish)
                    {
                        await base.PublishFunctionAsync(updateCodeRequest.FunctionName);
                    }
                }
            }
            finally
            {
                lambdaZipArchiveStream?.Dispose();
            }

            return true;
        }

        private Lambda.PackageType DeterminePackageType()
        {
            var strPackageType = this.GetStringValueOrDefault(this.PackageType, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE, false);
            return LambdaUtilities.DeterminePackageType(strPackageType);
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
                

                PushDockerImageProperties = new BasePushDockerImageCommand<LambdaToolsDefaults>.PushDockerImagePropertyContainer
                {
                    Configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false),
                    TargetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false),

                    LocalDockerImage = this.GetStringValueOrDefault(this.LocalDockerImage, CommonDefinedCommandOptions.ARGUMENT_LOCAL_DOCKER_IMAGE, false),
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
            result.ImageUri = pushCommand.PushedImageUri;
            result.LastToolsException = pushCommand.LastToolsException;

            return result;
        }



        private void ValidateTargetFrameworkAndLambdaRuntime(string targetFramework)
        {
            string runtimeName = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, true);
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime(runtimeName, targetFramework);
        }

        protected override void SaveConfigFile(JsonData data)
        {

            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION.ConfigFileKey, this.GetStringValueOrDefault(this.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE.ConfigFileKey, this.GetStringValueOrDefault(this.Profile, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProfileLocation, CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE_LOCATION, false));

            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.ConfigFileKey, this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION.ConfigFileKey, this.GetStringValueOrDefault(this.Description, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.Tags, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS, false)));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE.ConfigFileKey, this.GetStringValueOrDefault(this.PackageType, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_TYPE, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH.ConfigFileKey, this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER.ConfigFileKey, this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE.ConfigFileKey, this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE.ConfigFileKey, this.GetStringValueOrDefault(this.Role, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT.ConfigFileKey, this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME.ConfigFileKey, this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.ConfigFileKey, this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false)));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE.ConfigFileKey, this.GetIntValueOrDefault(this.EphemeralStorageSize, LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE.ConfigFileKey, this.GetBoolValueOrDefault(this.FunctionUrlEnable, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionUrlAuthType, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.ImageEntryPoint, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.ImageCommand, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY.ConfigFileKey, this.GetStringValueOrDefault(this.ImageWorkingDirectory, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY, false));


            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false)));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN.ConfigFileKey, this.GetStringValueOrDefault(this.DeadLetterTargetArn, LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE.ConfigFileKey, this.GetStringValueOrDefault(this.TracingMode, LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.EnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.AppendEnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN.ConfigFileKey, this.GetStringValueOrDefault(this.KMSKeyArn, LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_RESOLVE_S3.ConfigFileKey, this.GetBoolValueOrDefault(this.ResolveS3, LambdaDefinedCommandOptions.ARGUMENT_RESOLVE_S3, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.ConfigFileKey, this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.ConfigFileKey, this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false));

            var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));
            data.SetFilePathIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE.ConfigFileKey, this.GetStringValueOrDefault(this.DockerFile, CommonDefinedCommandOptions.ARGUMENT_DOCKERFILE, false), projectLocation);

            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.ConfigFileKey, this.GetStringValueOrDefault(this.DockerBuildOptions, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.ConfigFileKey, this.GetStringValueOrDefault(this.DockerBuildWorkingDirectory, CommonDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT.ConfigFileKey, this.GetStringValueOrDefault(this.HostBuildOutput, CommonDefinedCommandOptions.ARGUMENT_HOST_BUILD_OUTPUT, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_TAG.ConfigFileKey, this.GetStringValueOrDefault(this.DockerImageTag, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_TAG, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD.ConfigFileKey, this.GetBoolValueOrDefault(this.UseContainerForBuild, LambdaDefinedCommandOptions.ARGUMENT_USE_CONTAINER_FOR_BUILD, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD.ConfigFileKey, this.GetStringValueOrDefault(this.ContainerImageForBuild, LambdaDefinedCommandOptions.ARGUMENT_CONTAINER_IMAGE_FOR_BUILD, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY.ConfigFileKey, this.GetStringValueOrDefault(this.CodeMountDirectory, LambdaDefinedCommandOptions.ARGUMENT_CODE_MOUNT_DIRECTORY, false));
        }
    }
}
