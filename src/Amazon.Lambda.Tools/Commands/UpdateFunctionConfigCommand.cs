using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
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
            LambdaDefinedCommandOptions.ARGUMENT_APPLY_DEFAULTS_FOR_UPDATE_OBSOLETE
        });

        public string FunctionName { get; set; }
        public string Description { get; set; }
        public bool? Publish { get; set; }
        public string Handler { get; set; }
        public int? MemorySize { get; set; }
        public string Role { get; set; }
        public int? Timeout { get; set; }
        public string[] SubnetIds { get; set; }
        public string[] SecurityGroupIds { get; set; }
        public Runtime Runtime { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
        public Dictionary<string, string> AppendEnvironmentVariables { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string KMSKeyArn { get; set; }
        public string DeadLetterTargetArn { get; set; }
        public string TracingMode { get; set; }

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
        }


        protected override async Task<bool> PerformActionAsync()
        {
            var currentConfiguration = await GetFunctionConfigurationAsync();
            if(currentConfiguration == null)
            {
                this.Logger.WriteLine($"Could not find existing Lambda function {this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)}");
                return false;
            }
            await UpdateConfigAsync(currentConfiguration);

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

        protected async Task UpdateConfigAsync(GetFunctionConfigurationResponse existingConfiguration)
        {
            var request = CreateConfigurationRequestIfDifferent(existingConfiguration);
            if (request != null)
            {
                this.Logger.WriteLine($"Updating runtime configuration for function {this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)}");
                try
                {
                    await this.LambdaClient.UpdateFunctionConfigurationAsync(request);
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException($"Error updating configuration for Lambda function: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaUpdateFunctionConfiguration, e);
                }
            }
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
        /// Create an UpdateFunctionConfigurationRequest if any fields have changed. Otherwise it returns back null causing the Update
        /// to skip.
        /// </summary>
        /// <param name="existingConfiguration"></param>
        /// <returns></returns>
        private UpdateFunctionConfigurationRequest CreateConfigurationRequestIfDifferent(GetFunctionConfigurationResponse existingConfiguration)
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

            var handler = this.GetStringValueOrDefault(this.Handler, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER, false);
            if (!string.IsNullOrEmpty(handler) && !string.Equals(handler, existingConfiguration.Handler, StringComparison.Ordinal))
            {
                request.Handler = handler;
                different = true;
            }

            var memorySize = this.GetIntValueOrDefault(this.MemorySize, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE, false);
            if(memorySize.HasValue && memorySize.Value != existingConfiguration.MemorySize)
            {
                request.MemorySize = memorySize.Value;
                different = true;
            }

            var runtime = this.GetStringValueOrDefault(this.Runtime, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_RUNTIME, false);
            if (runtime != null && runtime != existingConfiguration.Runtime)
            {
                request.Runtime = runtime;
                different = true;
            }

            var timeout = this.GetIntValueOrDefault(this.Timeout, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT, false);
            if (timeout.HasValue && timeout.Value != existingConfiguration.Timeout)
            {
                request.Timeout = timeout.Value;
                different = true;
            }

            var subnetIds = this.GetStringValuesOrDefault(this.SubnetIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS, false);
            if (subnetIds != null)
            {
                if(request.VpcConfig == null)
                {
                    request.VpcConfig = new VpcConfig
                    {
                        SubnetIds = subnetIds.ToList()
                    };
                    different = true;
                }
                if(AreDifferent(subnetIds, request.VpcConfig.SubnetIds))
                {
                    request.VpcConfig.SubnetIds = subnetIds.ToList();
                    different = true;
                }
            }

            var securityGroupIds = this.GetStringValuesOrDefault(this.SecurityGroupIds, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS, false);
            if (securityGroupIds != null)
            {
                if (request.VpcConfig == null)
                {
                    request.VpcConfig = new VpcConfig
                    {
                        SecurityGroupIds = securityGroupIds.ToList()
                    };
                    different = true;
                }
                if (AreDifferent(securityGroupIds, request.VpcConfig.SecurityGroupIds))
                {
                    request.VpcConfig.SecurityGroupIds = securityGroupIds.ToList();
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
            if (environmentVariables != null && AreDifferent(environmentVariables, existingConfiguration?.Environment?.Variables))
            {
                request.Environment = new Model.Environment { Variables = environmentVariables };
                different = true;
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
        
        protected override void SaveConfigFile(JsonData data)
        {
            
        }
    }
}
