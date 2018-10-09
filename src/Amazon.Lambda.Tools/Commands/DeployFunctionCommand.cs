using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


using Amazon.Lambda.Model;


using ThirdParty.Json.LitJson;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;

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
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS,
            LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN,
            LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE,
            LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN,
            LambdaDefinedCommandOptions.ARGUMENT_APPLY_DEFAULTS_FOR_UPDATE_OBSOLETE,
            LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET,
            LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK
        });

        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string Package { get; set; }
        public string MSBuildParameters { get; set; }

        public string S3Bucket { get; set; }
        public string S3Prefix { get; set; }

        public bool? DisableVersionCheck { get; set; }


        // Disable handler validation for now.
        // TODO: Fix issue with loading dependent assemblies when doing validation.
        public bool SkipHandlerValidation { get; set; } = true;


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
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.Switch)) != null)
                this.S3Bucket = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.Switch)) != null)
                this.S3Prefix = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.Switch)) != null)
                this.DisableVersionCheck = tuple.Item2.BoolValue;

            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.Switch)) != null)
                this.MSBuildParameters = tuple.Item2.StringValue;

            if (!string.IsNullOrEmpty(values.MSBuildParameters))
            {
                if (this.MSBuildParameters == null)
                    this.MSBuildParameters = values.MSBuildParameters;
                else
                    this.MSBuildParameters += " " + values.MSBuildParameters;
            }
        }



        protected override async Task<bool> PerformActionAsync()
        {
            string projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
            string zipArchivePath = null;
            string package = this.GetStringValueOrDefault(this.Package, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE, false);
            if(string.IsNullOrEmpty(package))
            {
                EnsureInProjectDirectory();

                // Release will be the default configuration if nothing set.
                string configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false);

                string targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                string msbuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false);

                ValidateTargetFrameworkAndLambdaRuntime();

                bool disableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault();
                string publishLocation;
                LambdaPackager.CreateApplicationBundle(this.DefaultConfig, this.Logger, this.WorkingDirectory, projectLocation, configuration, targetFramework, msbuildParameters, disableVersionCheck, out publishLocation, ref zipArchivePath);
                if (string.IsNullOrEmpty(zipArchivePath))
                    return false;
            }
            else
            {
                if(!File.Exists(package))
                    throw new LambdaToolsException($"Package {package} does not exist", LambdaToolsException.LambdaErrorCode.InvalidPackage);
                if(!string.Equals(Path.GetExtension(package), ".zip", StringComparison.OrdinalIgnoreCase))
                    throw new LambdaToolsException($"Package {package} must be a zip file", LambdaToolsException.LambdaErrorCode.InvalidPackage);

                this.Logger.WriteLine($"Skipping compilation and using precompiled package {package}");
                zipArchivePath = package;
            }


            using (var stream = new MemoryStream(File.ReadAllBytes(zipArchivePath)))
            {
                var s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false);
                string s3Key = null;
                if (!string.IsNullOrEmpty(s3Bucket))
                {
                    await Utilities.ValidateBucketRegionAsync(this.S3Client, s3Bucket);

                    var functionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true);
                    var s3Prefix = this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false);
                    s3Key = await Utilities.UploadToS3Async(this.Logger, this.S3Client, s3Bucket, s3Prefix, functionName, stream);
                }


                var currentConfiguration = await GetFunctionConfigurationAsync();
                if (currentConfiguration == null)
                {
                    this.Logger.WriteLine($"Creating new Lambda function {this.FunctionName}");
                    var createRequest = new CreateFunctionRequest
                    {
                        FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true),
                        Description = this.GetStringValueOrDefault(this.Description, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION, false),
                        
                        Role = this.GetRoleValueOrDefault(this.Role, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE, 
                            Constants.LAMBDA_PRINCIPAL, LambdaConstants.AWS_LAMBDA_MANAGED_POLICY_PREFIX, 
                            LambdaConstants.KNOWN_MANAGED_POLICY_DESCRIPTIONS, true),
                        
                        Handler = this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, true),
                        Publish = this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false).GetValueOrDefault(),
                        MemorySize = this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, true).GetValueOrDefault(),
                        Runtime = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, true),
                        Timeout = this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, true).GetValueOrDefault(),
                        KMSKeyArn = this.GetStringValueOrDefault(this.KMSKeyArn, LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, false),
                        VpcConfig = new VpcConfig
                        {
                            SubnetIds = this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false)?.ToList(),
                            SecurityGroupIds = this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false)?.ToList()
                        }
                    };

                    var environmentVariables = GetEnvironmentVariables(null);

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
                            ZipFile = stream
                        };
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
                }
                else
                {
                    this.Logger.WriteLine($"Updating code for existing function {this.FunctionName}");

                    var updateCodeRequest = new UpdateFunctionCodeRequest
                    {
                        FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)
                    };

                    if (s3Bucket != null)
                    {
                        updateCodeRequest.S3Bucket = s3Bucket;
                        updateCodeRequest.S3Key = s3Key;
                    }
                    else
                    {
                        updateCodeRequest.ZipFile = stream;
                    }

                    try
                    {
                        await this.LambdaClient.UpdateFunctionCodeAsync(updateCodeRequest);
                    }
                    catch (Exception e)
                    {
                        throw new LambdaToolsException($"Error updating code for Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaUpdateFunctionCode, e);
                    }

                    await base.UpdateConfigAsync(currentConfiguration);

                    await base.ApplyTags(currentConfiguration.FunctionArn);

                    var publish = this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false).GetValueOrDefault();
                    if(publish)
                    {
                        await base.PublishFunctionAsync(updateCodeRequest.FunctionName);
                    }
                }
            }

            return true;
        }

        private void ValidateTargetFrameworkAndLambdaRuntime()
        {
            string runtimeName = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, true);
            string frameworkName = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime(runtimeName, frameworkName);
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
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH.ConfigFileKey, this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER.ConfigFileKey, this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE.ConfigFileKey, this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE.ConfigFileKey, this.GetStringValueOrDefault(this.Role, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT.ConfigFileKey, this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME.ConfigFileKey, this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, false));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS.ConfigFileKey, LambdaToolsDefaults.FormatCommaDelimitedList(this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false)));

            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN.ConfigFileKey, this.GetStringValueOrDefault(this.DeadLetterTargetArn, LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE.ConfigFileKey, this.GetStringValueOrDefault(this.TracingMode, LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.EnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.AppendEnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN.ConfigFileKey, this.GetStringValueOrDefault(this.KMSKeyArn, LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.ConfigFileKey, this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.ConfigFileKey, this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false));

        }

    }
}
