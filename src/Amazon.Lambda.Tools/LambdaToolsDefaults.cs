using System;
using System.Collections.Generic;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools
{


    /// <summary>
    /// This class gives access to the default values for the CommandOptions defined in the project's default json file.
    /// </summary>
    public class LambdaToolsDefaults : DefaultConfigFile
    {
        public const string DEFAULT_FILE_NAME = "aws-lambda-tools-defaults.json";

        public LambdaToolsDefaults()
        {

        }

        public LambdaToolsDefaults(string sourceFile)
            : this(new JsonData(), sourceFile)
        {
        }

        public LambdaToolsDefaults(JsonData data, string sourceFile)
            : base(data, sourceFile)
        {
        }


        public override string DefaultConfigFileName => DEFAULT_FILE_NAME;




        public string Profile => GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_AWS_PROFILE);

        public string Region => GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_AWS_REGION);

        public string FunctionHandler => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_HANDLER);

        public string FunctionName => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME);

        public string FunctionRole => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ROLE);

        public int? FunctionMemory
        {
            get
            {
                var data = GetValue(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_MEMORY_SIZE);
                if (data != null && data.IsInt)
                {
                    return (int)data;
                }
                return null;
            }
        }

        public int? FunctionTimeout
        {
            get
            {
                var data = GetValue(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_TIMEOUT);
                if (data != null && data.IsInt)
                {
                    return (int)data;
                }
                return null;
            }
        }

        public string CloudFormationTemplate => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE);

        public IDictionary<string, string> CloudFormationTemplateParameters
        {
            get
            {
                var str = GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_PARAMETER);
                if (string.IsNullOrEmpty(str))
                    return null;

                try
                {
                    return Utilities.ParseKeyValueOption(str);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public IDictionary<string, string> EnvironmentVariables
        {
            get
            {
                var str = GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_ENVIRONMENT_VARIABLES);
                if (string.IsNullOrEmpty(str))
                    return null;

                try
                {
                    return Utilities.ParseKeyValueOption(str);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public string[] FunctionSubnets
        {
            get
            {
                var str = GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SUBNETS);
                if (string.IsNullOrEmpty(str))
                    return null;

                try
                {
                    return str.SplitByComma();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public string[] FunctionSecurityGroups
        {
            get
            {
                var str = GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_SECURITY_GROUPS);
                if (string.IsNullOrEmpty(str))
                    return null;

                try
                {
                    return str.SplitByComma();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public string KMSKeyArn => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_KMS_KEY_ARN);

        public string StackName => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME);

        public string S3Bucket => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET);

        public string S3Prefix => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX);

        public string Configuration => GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION);

        public string Framework => GetValueAsString(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK);

        public string DeadLetterTargetArn => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_DEADLETTER_TARGET_ARN);

        public string TracingMode => GetValueAsString(LambdaDefinedCommandOptions.ARGUMENT_TRACING_MODE);

    }
}
