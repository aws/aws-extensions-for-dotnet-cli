using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Updates the configuration for an existing function. To avoid any accidental changes to the function
    /// only fields that were explicitly set are changed and defaults are ignored.
    /// </summary>
    public class UpdateFunctionConfigCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "update-function-config";
        public const string COMMAND_DESCRIPTION = "Command to update the runtime configuration for a Lambda function";
        public const string COMMAND_ARGUMENTS = "<FUNCTION-NAME> The name of the function to be updated";



        public static readonly IList<CommandOption> UpdateCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS,

            LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH,

            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT,
            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND,
            LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY,

            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS,
            LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN,
            LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE,
            LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES,
            LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, 
            LambdaDefinedCommandOptions.ARGUMENT_APPLY_DEFAULTS_FOR_UPDATE_OBSOLETE
        });

        public string FunctionName { get; set; }
        public string Description { get; set; }
        public bool? Publish { get; set; }
        public string Handler { get; set; }
        public int? MemorySize { get; set; }
        public string Role { get; set; }
        public int? Timeout { get; set; }
        public string[] LayerVersionArns { get; set; }
        public string[] SubnetIds { get; set; }
        public string[] SecurityGroupIds { get; set; }
        public Runtime Runtime { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
        public Dictionary<string, string> AppendEnvironmentVariables { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string KMSKeyArn { get; set; }
        public string DeadLetterTargetArn { get; set; }
        public string TracingMode { get; set; }

        public string[] ImageEntryPoint { get; set; }
        public string[] ImageCommand { get; set; }
        public string ImageWorkingDirectory { get; set; }

        public string PackageType { get; set; }

        public int? EphemeralStorageSize { get; set; }

        public bool? FunctionUrlEnable { get; set; }
        public string FunctionUrlAuthType { get; set; }

        public string FunctionUrlLink { get; private set; }

        public UpdateFunctionConfigCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, UpdateCommandOptions, args)
        {
        }

        protected UpdateFunctionConfigCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
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
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.Switch)) != null)
                this.FunctionName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION.Switch)) != null)
                this.Description = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH.Switch)) != null)
                this.Publish = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER.Switch)) != null)
                this.Handler = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE.Switch)) != null)
                this.MemorySize = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE.Switch)) != null)
                this.Role = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT.Switch)) != null)
                this.Timeout = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME.Switch)) != null)
                this.Runtime = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS.Switch)) != null)
                this.LayerVersionArns = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS.Switch)) != null)
                this.Tags = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS.Switch)) != null)
                this.SubnetIds = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS.Switch)) != null)
                this.SecurityGroupIds = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN.Switch)) != null)
                this.DeadLetterTargetArn = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE.Switch)) != null)
                this.TracingMode = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES.Switch)) != null)
                this.EnvironmentVariables = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES.Switch)) != null)
                this.AppendEnvironmentVariables = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN.Switch)) != null)
                this.KMSKeyArn = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT.Switch)) != null)
                this.ImageEntryPoint = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND.Switch)) != null)
                this.ImageCommand = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY.Switch)) != null)
                this.ImageWorkingDirectory = tuple.Item2.StringValue;

            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE.Switch)) != null)
                this.EphemeralStorageSize = tuple.Item2.IntValue;

            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE.Switch)) != null)
                this.FunctionUrlEnable = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH.Switch)) != null)
                this.FunctionUrlAuthType = tuple.Item2.StringValue;
        }


        protected override async Task<bool> PerformActionAsync()
        {
            var currentConfiguration = await GetFunctionConfigurationAsync();
            if(currentConfiguration == null)
            {
                this.Logger.WriteLine($"Could not find existing Lambda function {this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)}");
                return false;
            }

            var layerVersionArns = this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false);
            var layerPackageInfo = await LambdaUtilities.LoadLayerPackageInfos(this.Logger, this.LambdaClient, this.S3Client, layerVersionArns);

            await UpdateConfigAsync(currentConfiguration, layerPackageInfo.GenerateDotnetSharedStoreValue());

            await ApplyTags(currentConfiguration.FunctionArn);

            var publish = this.GetBoolValueOrDefault(this.Publish, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_PUBLISH, false).GetValueOrDefault();
            if (publish)
            {
                await PublishFunctionAsync(currentConfiguration.FunctionName);
            }

            return true;
        }

        protected async Task PublishFunctionAsync(string functionName)
        {
            try
            {
                await LambdaUtilities.WaitTillFunctionAvailableAsync(this.Logger, this.LambdaClient, functionName);

                var response = await this.LambdaClient.PublishVersionAsync(new PublishVersionRequest
                {
                    FunctionName = functionName
                });
                this.Logger.WriteLine("Published new Lambda function version: " + response.Version);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error publishing Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaPublishFunction, e);
            }
        }

        protected async Task ApplyTags(string functionArn)
        {
            try
            {
                var tags = this.GetKeyValuePairOrDefault(this.Tags, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TAGS, false);
                if (tags == null || tags.Count == 0)
                    return;

                var tagRequest = new TagResourceRequest
                {
                    Resource = functionArn,
                    Tags = tags
                };

                await this.LambdaClient.TagResourceAsync(tagRequest);
                this.Logger?.WriteLine($"Applying {tags.Count} tag(s) to function");
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error tagging Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaTaggingFunction, e);
            }
        }

        /// <summary>
        /// Reverts the lambda configuration to an earlier point, which is needed if updating the lambda function code failed.
        /// </summary>
        /// <param name="existingConfiguration"></param>
        protected async Task AttemptRevertConfigAsync(GetFunctionConfigurationResponse existingConfiguration)
        {
            try
            {
                var request = CreateRevertConfigurationRequest(existingConfiguration);
                await LambdaUtilities.WaitTillFunctionAvailableAsync(Logger, this.LambdaClient, request.FunctionName);
                this.Logger.WriteLine($"Reverting runtime configuration for function {this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)}");
                await this.LambdaClient.UpdateFunctionConfigurationAsync(request);
            }
            catch (Exception e)
            {
                this.Logger.WriteLine($"Error reverting configuration for Lambda function: {e.Message}");
            }
        }

        protected async Task<bool> UpdateConfigAsync(GetFunctionConfigurationResponse existingConfiguration, string dotnetSharedStoreValue)
        {
            var configUpdated = false;
            var request = CreateConfigurationRequestIfDifferent(existingConfiguration, dotnetSharedStoreValue);
            if (request != null)
            {
                await LambdaUtilities.WaitTillFunctionAvailableAsync(Logger, this.LambdaClient, request.FunctionName);
                this.Logger.WriteLine($"Updating runtime configuration for function {this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)}");
                try
                {
                    await this.LambdaClient.UpdateFunctionConfigurationAsync(request);
                    configUpdated = true;
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException($"Error updating configuration for Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaUpdateFunctionConfiguration, e);
                }
            }

            // only attempt to modify function url if the user has explicitly opted-in to use FunctionUrl
            if (GetBoolValueOrDefault(FunctionUrlEnable, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE, false).HasValue)
            {
                var urlConfig = await this.GetFunctionUrlConfig(existingConfiguration.FunctionName);

                // To determine what is the state of the function url check to see if the user explicitly set a value. If they did set a value then use that 
                // to either add or remove the url config. If the user didn't set a value check to see if there is an existing config to make sure we don't remove
                // the config url if the user didn't set a value.
                bool enableUrlConfig = this.GetBoolValueOrDefault(this.FunctionUrlEnable, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE, false).HasValue ? 
                                            this.GetBoolValueOrDefault(this.FunctionUrlEnable, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_ENABLE, false).Value : urlConfig != null;

                var authType = this.GetStringValueOrDefault(this.FunctionUrlAuthType, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_URL_AUTH, false);

                if (urlConfig != null)
                {
                    this.FunctionUrlLink = urlConfig.FunctionUrl;
                }

                if (urlConfig != null && !enableUrlConfig)
                {
                    await this.DeleteFunctionUrlConfig(existingConfiguration.FunctionName, urlConfig.AuthType);
                    this.Logger.WriteLine("Removing function url config");
                }
                else if(urlConfig == null && enableUrlConfig)
                {
                    await this.CreateFunctionUrlConfig(existingConfiguration.FunctionName, authType);
                    this.Logger.WriteLine($"Creating function url config: {this.FunctionUrlLink}");
                }
                else if (urlConfig != null && enableUrlConfig &&
                         !string.Equals(authType, urlConfig.AuthType.Value, StringComparison.Ordinal))
                {
                    await this.UpdateFunctionUrlConfig(existingConfiguration.FunctionName, urlConfig.AuthType, authType);
                    this.Logger.WriteLine($"Updating function url config: {this.FunctionUrlLink}");
                }
            }

            return configUpdated;
        }

        public async Task<GetFunctionConfigurationResponse> GetFunctionConfigurationAsync()
        {
            var request = new GetFunctionConfigurationRequest
            {
                FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)
            };
            try
            {
                var response = await this.LambdaClient.GetFunctionConfigurationAsync(request);
                return response;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
            catch(Exception e)
            {
                throw new LambdaToolsException($"Error retrieving configuration for function {request.FunctionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaGetConfiguration, e);
            }
        }

        /// <summary>
        /// Create an UpdateFunctionConfigurationRequest for the current configuration to be used to revert a failed configuration update.
        /// </summary>
        /// <param name="existingConfiguration"></param>
        /// <returns><see cref="UpdateFunctionConfigurationRequest"/></returns>
        private UpdateFunctionConfigurationRequest CreateRevertConfigurationRequest(GetFunctionConfigurationResponse existingConfiguration)
        {
            var request = new UpdateFunctionConfigurationRequest
            {
                FunctionName = existingConfiguration.FunctionName,
                Description = existingConfiguration.Description,
                Role = existingConfiguration.Role,
                MemorySize = existingConfiguration.MemorySize,
                EphemeralStorage = existingConfiguration.EphemeralStorage,
                Timeout = existingConfiguration.Timeout,
                Layers = existingConfiguration.Layers?.Select(x => x.Arn).ToList(),
                DeadLetterConfig = existingConfiguration.DeadLetterConfig,
                KMSKeyArn = existingConfiguration.KMSKeyArn
            };

            if (existingConfiguration.VpcConfig != null)
            {
                request.VpcConfig = new VpcConfig
                {
                    IsSecurityGroupIdsSet = existingConfiguration.VpcConfig.SecurityGroupIds?.Any() ?? false,
                    IsSubnetIdsSet = existingConfiguration.VpcConfig.SubnetIds?.Any() ?? false,
                    SecurityGroupIds = existingConfiguration.VpcConfig.SecurityGroupIds,
                    SubnetIds = existingConfiguration.VpcConfig.SubnetIds
                };
            }

            if (existingConfiguration.TracingConfig != null)
            {
                request.TracingConfig = new TracingConfig
                {
                    Mode = existingConfiguration.TracingConfig.Mode
                };
            }

            if (existingConfiguration.Environment != null)
            {
                request.Environment = new Model.Environment
                {
                    IsVariablesSet = existingConfiguration.Environment.Variables?.Any() ?? false,
                    Variables = existingConfiguration.Environment.Variables
                };
            }

            if (existingConfiguration.PackageType == Lambda.PackageType.Zip)
            {
                request.Handler = existingConfiguration.Handler;
                request.Runtime = existingConfiguration.Runtime;
            }
            else if (existingConfiguration.PackageType == Lambda.PackageType.Image)
            {
                if (existingConfiguration.ImageConfigResponse != null)
                {
                    request.ImageConfig = new ImageConfig
                    {
                        Command = existingConfiguration.ImageConfigResponse.ImageConfig?.Command,
                        EntryPoint = existingConfiguration.ImageConfigResponse.ImageConfig?.EntryPoint,
                        IsCommandSet = existingConfiguration.ImageConfigResponse.ImageConfig?.Command?.Any() ?? false,
                        IsEntryPointSet = existingConfiguration.ImageConfigResponse.ImageConfig?.EntryPoint?.Any() ?? false,
                        WorkingDirectory = existingConfiguration.ImageConfigResponse.ImageConfig?.WorkingDirectory
                    };
                }
            }

            return request;
        }

        /// <summary>
        /// Create an UpdateFunctionConfigurationRequest if any fields have changed. Otherwise it returns back null causing the Update
        /// to skip.
        /// </summary>
        /// <param name="existingConfiguration"></param>
        /// <returns></returns>
        private UpdateFunctionConfigurationRequest CreateConfigurationRequestIfDifferent(GetFunctionConfigurationResponse existingConfiguration, string dotnetSharedStoreValue)
        {
            bool different = false;
            var request = new UpdateFunctionConfigurationRequest
            {
                FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)
            };

            var description = this.GetStringValueOrDefault(this.Description, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_DESCRIPTION, false);
            if (!string.IsNullOrEmpty(description) && !string.Equals(description, existingConfiguration.Description, StringComparison.Ordinal))
            {
                request.Description = description;
                different = true;
            }

            var role = this.GetStringValueOrDefault(this.Role, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE, false);
            if (!string.IsNullOrEmpty(role))
            {
                string fullRole;
                if (role.StartsWith(LambdaConstants.IAM_ARN_PREFIX))
                    fullRole = role;
                else
                    fullRole = RoleHelper.ExpandRoleName(this.IAMClient, role);

                if (!string.Equals(fullRole, existingConfiguration.Role, StringComparison.Ordinal))
                {
                    request.Role = fullRole;
                    different = true;
                }
            }

            var memorySize = this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, false);
            if(memorySize.HasValue && memorySize.Value != existingConfiguration.MemorySize)
            {
                request.MemorySize = memorySize.Value;
                different = true;
            }

            var ephemeralSize = this.GetIntValueOrDefault(this.EphemeralStorageSize, LambdaDefinedCommandOptions.ARGUMENT_EPHEMERAL_STORAGE_SIZE, false);
            if (ephemeralSize.HasValue && ephemeralSize.Value != existingConfiguration.EphemeralStorage?.Size)
            {
                request.EphemeralStorage = new EphemeralStorage { Size = ephemeralSize.Value };
            }

            var timeout = this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, false);
            if (timeout.HasValue && timeout.Value != existingConfiguration.Timeout)
            {
                request.Timeout = timeout.Value;
                different = true;
            }

            var layerVersionArns = this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false);
            if(layerVersionArns != null && AreDifferent(layerVersionArns, existingConfiguration.Layers?.Select(x => x.Arn)))
            {
                request.Layers = layerVersionArns.ToList();
                request.IsLayersSet = true;
                different = true;
            }

            var subnetIds = this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false);
            if (subnetIds != null)
            {
                if (request.VpcConfig == null)
                {
                    request.VpcConfig = new VpcConfig();
                }

                request.VpcConfig.SubnetIds = subnetIds.ToList();
                request.VpcConfig.IsSubnetIdsSet = true;
                if (existingConfiguration.VpcConfig == null || AreDifferent(subnetIds, existingConfiguration.VpcConfig.SubnetIds))
                {
                    different = true;
                }
            }

            var securityGroupIds = this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false);
            if (securityGroupIds != null)
            {
                if (request.VpcConfig == null)
                {
                    request.VpcConfig = new VpcConfig();
                }

                request.VpcConfig.SecurityGroupIds = securityGroupIds.ToList();
                request.VpcConfig.IsSecurityGroupIdsSet = true;
                if (existingConfiguration.VpcConfig == null || AreDifferent(securityGroupIds, existingConfiguration.VpcConfig.SecurityGroupIds))
                {
                    different = true;
                }
            }

            var deadLetterTargetArn = this.GetStringValueOrDefault(this.DeadLetterTargetArn, LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN, false);
            if (deadLetterTargetArn != null)
            {
                if (!string.IsNullOrEmpty(deadLetterTargetArn) && !string.Equals(deadLetterTargetArn, existingConfiguration.DeadLetterConfig?.TargetArn, StringComparison.Ordinal))
                {
                    request.DeadLetterConfig = existingConfiguration.DeadLetterConfig ?? new DeadLetterConfig();
                    request.DeadLetterConfig.TargetArn = deadLetterTargetArn;
                    different = true;
                }
                else if (string.IsNullOrEmpty(deadLetterTargetArn) && !string.IsNullOrEmpty(existingConfiguration.DeadLetterConfig?.TargetArn))
                {
                    request.DeadLetterConfig = null;
                    request.DeadLetterConfig = existingConfiguration.DeadLetterConfig ?? new DeadLetterConfig();
                    request.DeadLetterConfig.TargetArn = string.Empty;
                    different = true;
                }
            }

            var tracingMode = this.GetStringValueOrDefault(this.TracingMode, LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE, false);
            if (tracingMode != null)
            {
                var eTraceMode = !string.Equals(tracingMode, string.Empty) ? Amazon.Lambda.TracingMode.FindValue(tracingMode) : null;
                if (eTraceMode != existingConfiguration.TracingConfig?.Mode)
                {
                    request.TracingConfig = new TracingConfig();
                    request.TracingConfig.Mode = eTraceMode;
                    different = true;
                }
            }

            var kmsKeyArn = this.GetStringValueOrDefault(this.KMSKeyArn, LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN, false);
            if (!string.IsNullOrEmpty(kmsKeyArn) && !string.Equals(kmsKeyArn, existingConfiguration.KMSKeyArn, StringComparison.Ordinal))
            {
                request.KMSKeyArn = kmsKeyArn;
                different = true;
            }

            var environmentVariables = GetEnvironmentVariables(existingConfiguration?.Environment?.Variables);

            // If runtime package store layers were set, then set the environment variable to tell the .NET Core runtime
            // to look for assemblies in the folder where the layer will be expanded. 
            if(!string.IsNullOrEmpty(dotnetSharedStoreValue))
            {
                if(environmentVariables == null)
                {
                    environmentVariables = new Dictionary<string, string>();
                }
                environmentVariables[LambdaConstants.ENV_DOTNET_SHARED_STORE] = dotnetSharedStoreValue;
            }

            if (environmentVariables != null && AreDifferent(environmentVariables, existingConfiguration?.Environment?.Variables))
            {
                request.Environment = new Model.Environment { Variables = environmentVariables };
                request.Environment.IsVariablesSet = true;
                different = true;
            }

            if (existingConfiguration.PackageType == Lambda.PackageType.Zip)
            {
                var handler = this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, false);
                if (!string.IsNullOrEmpty(handler) && !string.Equals(handler, existingConfiguration.Handler, StringComparison.Ordinal))
                {
                    request.Handler = handler;
                    different = true;
                }

                var runtime = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, false);
                if (runtime != null && runtime != existingConfiguration.Runtime)
                {
                    request.Runtime = runtime;
                    different = true;
                }
            }
            else if (existingConfiguration.PackageType == Lambda.PackageType.Image)
            {
                {
                    var imageEntryPoints = this.GetStringValuesOrDefault(this.ImageEntryPoint, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_ENTRYPOINT, false);
                    if (imageEntryPoints != null)
                    {
                        if (AreDifferent(imageEntryPoints, existingConfiguration.ImageConfigResponse?.ImageConfig?.EntryPoint))
                        {
                            if (request.ImageConfig == null)
                            {
                                request.ImageConfig = new ImageConfig();
                            }

                            request.ImageConfig.EntryPoint = imageEntryPoints.ToList();
                            request.ImageConfig.IsEntryPointSet = true;
                            different = true;
                        }
                    }
                }

                {
                    var imageCommands = this.GetStringValuesOrDefault(this.ImageCommand, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_COMMAND, false);
                    if (imageCommands != null)
                    {
                        if (AreDifferent(imageCommands, existingConfiguration.ImageConfigResponse?.ImageConfig?.Command))
                        {
                            if (request.ImageConfig == null)
                            {
                                request.ImageConfig = new ImageConfig();
                            }

                            request.ImageConfig.Command = imageCommands.ToList();
                            request.ImageConfig.IsCommandSet = true;
                            different = true;
                        }
                    }
                }

                var imageWorkingDirectory = this.GetStringValueOrDefault(this.ImageWorkingDirectory, LambdaDefinedCommandOptions.ARGUMENT_IMAGE_WORKING_DIRECTORY, false);
                if (imageWorkingDirectory != null)
                {
                    if (request.ImageConfig == null)
                    {
                        request.ImageConfig = new ImageConfig();
                    }

                    if(!string.Equals(imageWorkingDirectory, existingConfiguration.ImageConfigResponse?.ImageConfig?.WorkingDirectory, StringComparison.Ordinal))
                    {
                        request.ImageConfig.WorkingDirectory = imageWorkingDirectory;
                        different = true;
                    }
                }
            }

            if (!different)
                return null;

            return request;
        }

        public Dictionary<string, string> GetEnvironmentVariables(Dictionary<string, string> existingEnvironmentVariables)
        {
            var specifiedEnvironmentVariables = this.GetKeyValuePairOrDefault(this.EnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES, false);
            var appendEnvironmentVariables = this.GetKeyValuePairOrDefault(this.AppendEnvironmentVariables, LambdaDefinedCommandOptions.ARGUMENT_APPEND_ENVIRONMENT_VARIABLES, false);
            if (appendEnvironmentVariables == null)
            {
                return specifiedEnvironmentVariables;
            }

            var combineSet = specifiedEnvironmentVariables ?? existingEnvironmentVariables;
            if (combineSet == null)
            {
                combineSet = appendEnvironmentVariables;
            }
            else
            {
                foreach (var kvp in appendEnvironmentVariables)
                {
                    combineSet[kvp.Key] = kvp.Value;
                }
            }

            return combineSet;
        }

        private bool AreDifferent(IDictionary<string, string> source, IDictionary<string, string> target)
        {
            if (target == null)
                target = new Dictionary<string, string>();

            if (source.Count != target.Count)
                return true;

            foreach(var kvp in source)
            {
                string value;
                if (!target.TryGetValue(kvp.Key, out value))
                    return true;
                if (!string.Equals(kvp.Value, value, StringComparison.Ordinal))
                    return true;
            }

            foreach (var kvp in target)
            {
                string value;
                if (!source.TryGetValue(kvp.Key, out value))
                    return true;
                if (!string.Equals(kvp.Value, value, StringComparison.Ordinal))
                    return true;
            }


            return false;
        }

        private bool AreDifferent(IEnumerable<string> source, IEnumerable<string> target)
        {
            if (source == null && target == null)
                return false;

            if(source?.Count() != target?.Count())
                return true;

            foreach(var item in source)
            {
                if (!target.Contains(item))
                    return true;
            }
            foreach (var item in target)
            {
                if (!source.Contains(item))
                    return true;
            }

            return false;
        }

        protected async Task<GetFunctionUrlConfigResponse> GetFunctionUrlConfig(string functionName)
        {
            var request = new GetFunctionUrlConfigRequest
            {
                FunctionName = functionName
            };

            try
            {
                var response = await this.LambdaClient.GetFunctionUrlConfigAsync(request);
                return response;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error creating function url config for function {request.FunctionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaFunctionUrlGet, e);
            }
        }

        protected async Task CreateFunctionUrlConfig(string functionName, FunctionUrlAuthType authType)
        {
            if (authType == null)
                authType = Amazon.Lambda.FunctionUrlAuthType.NONE;

            var request = new CreateFunctionUrlConfigRequest
            {
                FunctionName = functionName,
                AuthType = authType
            };

            try
            {
                this.FunctionUrlLink = (await this.LambdaClient.CreateFunctionUrlConfigAsync(request)).FunctionUrl;

                if(authType == Amazon.Lambda.FunctionUrlAuthType.NONE)
                {
                    await AddFunctionUrlPublicPermissionStatement(functionName);
                }
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error creating function url config for function {request.FunctionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaFunctionUrlCreate, e);
            }
        }

        protected async Task UpdateFunctionUrlConfig(string functionName, FunctionUrlAuthType oldAuthType, FunctionUrlAuthType newAuthType)
        {
            if (newAuthType == null)
                newAuthType = Amazon.Lambda.FunctionUrlAuthType.NONE;

            var request = new UpdateFunctionUrlConfigRequest
            {
                FunctionName = functionName,
                AuthType = newAuthType
            };

            try
            {
                this.FunctionUrlLink = (await this.LambdaClient.UpdateFunctionUrlConfigAsync(request)).FunctionUrl;

                if(oldAuthType != newAuthType)
                {
                    if(newAuthType == Amazon.Lambda.FunctionUrlAuthType.NONE)
                    {
                        await AddFunctionUrlPublicPermissionStatement(functionName);
                    }
                    else
                    {
                        await RemoveFunctionUrlPublicPermissionStatement(functionName);
                    }
                }
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error updating function url config for function {request.FunctionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaFunctionUrlUpdate, e);
            }
        }

        protected async Task DeleteFunctionUrlConfig(string functionName, FunctionUrlAuthType oldAuthType)
        {
            var request = new DeleteFunctionUrlConfigRequest
            {
                FunctionName = functionName
            };

            try
            {
                await this.LambdaClient.DeleteFunctionUrlConfigAsync(request);
                if (oldAuthType == Lambda.FunctionUrlAuthType.NONE)
                {
                    await RemoveFunctionUrlPublicPermissionStatement(functionName);
                }

                this.FunctionUrlLink = null;
            }
            catch (Exception e)
            {
                throw new LambdaToolsException($"Error deleting function url config for function {request.FunctionName}: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaFunctionUrlDelete, e);
            }
        }

        private async Task AddFunctionUrlPublicPermissionStatement(string functionName)
        {
            var request = new AddPermissionRequest
            {
                StatementId = LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID,
                FunctionName = functionName,
                Principal = "*",
                Action = "lambda:InvokeFunctionUrl",
                FunctionUrlAuthType = Amazon.Lambda.FunctionUrlAuthType.NONE
            };

            try
            {
                this.Logger.WriteLine("Adding Lambda permission statement to public access for Function Url");
                await LambdaClient.AddPermissionAsync(request);
            }
            catch(Amazon.Lambda.Model.ResourceConflictException)
            {
                this.Logger.WriteLine($"Lambda permission with statement id {LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID} for public access already exists");
            }
        }

        private async Task RemoveFunctionUrlPublicPermissionStatement(string functionName)
        {
            var request = new RemovePermissionRequest
            {
                FunctionName = functionName,
                StatementId = LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID
            };

            try
            {
                this.Logger.WriteLine("Removing Lambda permission statement to allow public access for Function Url");
                await LambdaClient.RemovePermissionAsync(request);
            }
            catch(Amazon.Lambda.Model.ResourceNotFoundException)
            {
                this.Logger.WriteLine($"Lambda permission with statement id {LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID} for public access not found to be removed");
            }
        }

        protected override void SaveConfigFile(JsonData data)
        {
            
        }
    }
}
