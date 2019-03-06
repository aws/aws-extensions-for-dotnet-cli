using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.Lambda.Tools
{
    /// <summary>
    /// This class defines all the possible options across all the commands. The individual commands will then
    /// references the options that are appropiate.
    /// </summary>
    public static class LambdaDefinedCommandOptions
    {

        public static readonly CommandOption ARGUMENT_DISABLE_VERSION_CHECK =
            new CommandOption
            {
                Name = "Disable Version Check",
                ShortSwitch = "-dvc",
                Switch = "--disable-version-check",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Disable the .NET Core version check. Only for advanced usage.",
            };
        public static readonly CommandOption ARGUMENT_PACKAGE =
            new CommandOption
            {
                Name = "Package",
                ShortSwitch = "-pac",
                Switch = "--package",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Application package to use for deployment, skips building the project",
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_NAME =
            new CommandOption
            {
                Name = "Function Name",
                ShortSwitch = "-fn",
                Switch = "--function-name",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "AWS Lambda function name"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_DESCRIPTION =
            new CommandOption
            {
                Name = "Function Description",
                ShortSwitch = "-fd",
                Switch = "--function-description",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "AWS Lambda function description"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_PUBLISH =
            new CommandOption
            {
                Name = "Publish",
                ShortSwitch = "-fp",
                Switch = "--function-publish",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Publish a new version as an atomic operation"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_HANDLER =
            new CommandOption
            {
                Name = "Handler",
                ShortSwitch = "-fh",
                Switch = "--function-handler",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Handler for the function <assembly>::<type>::<method>"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_MEMORY_SIZE =
            new CommandOption
            {
                Name = "Memory Size",
                ShortSwitch = "-fms",
                Switch = "--function-memory-size",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The amount of memory, in MB, your Lambda function is given",
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_ROLE =
            new CommandOption
            {
                Name = "Role",
                ShortSwitch = "-frole",
                Switch = "--function-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role that Lambda assumes when it executes your function"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_TIMEOUT =
            new CommandOption
            {
                Name = "Timeout",
                ShortSwitch = "-ft",
                Switch = "--function-timeout",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The function execution timeout in seconds"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_RUNTIME =
            new CommandOption
            {
                Name = "Runtime",
                ShortSwitch = "-frun",
                Switch = "--function-runtime",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The runtime environment for the Lambda function"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_SUBNETS =
            new CommandOption
            {
                Name = "Subnets",
                ShortSwitch = "-fsub",
                Switch = "--function-subnets",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of subnet ids if your function references resources in a VPC"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_SECURITY_GROUPS =
            new CommandOption
            {
                Name = "Subnets",
                ShortSwitch = "-fsec",
                Switch = "--function-security-groups",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of security group ids if your function references resources in a VPC"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_LAYERS =
            new CommandOption
            {
                Name = "Function Layers",
                ShortSwitch = "-fl",
                Switch = "--function-layers",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of Lambda layer version arns"
            };
        public static readonly CommandOption ARGUMENT_OPT_DIRECTORY =
            new CommandOption
            {
                Name = "Opt Directory",
                ShortSwitch = "-od",
                Switch = "--opt-directory",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The directory under the /opt directory the contents of the layer will be placed. If not set a directory name will be generated."
            };
        public static readonly CommandOption ARGUMENT_DEADLETTER_TARGET_ARN =
            new CommandOption
            {
                Name = "Dead Letter Target ARN",
                ShortSwitch = "-dlta",
                Switch = "--dead-letter-target-arn",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Target ARN of an SNS topic or SQS Queue for the Dead Letter Queue"
            };
        public static readonly CommandOption ARGUMENT_TRACING_MODE =
            new CommandOption
            {
                Name = "Tracing Mode",
                ShortSwitch = "-tm",
                Switch = "--tracing-mode",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Configures when AWS X-Ray should trace the function. Valid values: PassThrough or Active"
            };
        public static readonly CommandOption ARGUMENT_ENVIRONMENT_VARIABLES =
            new CommandOption
            {
                Name = "Environment Variables",
                ShortSwitch = "-ev",
                Switch = "--environment-variables",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Environment variables set for the function. For existing functions this replaces the current environment variables. Format is <key1>=<value1>;<key2>=<value2>"
            };
        public static readonly CommandOption ARGUMENT_APPEND_ENVIRONMENT_VARIABLES =
            new CommandOption
            {
                Name = "Append Environment Variables",
                ShortSwitch = "-aev",
                Switch = "--append-environment-variables",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Append environment variables to the existing set of environment variables for the function. Format is <key1>=<value1>;<key2>=<value2>"
            };
        public static readonly CommandOption ARGUMENT_FUNCTION_TAGS =
            new CommandOption
            {
                Name = "Tags",
                Switch = "--tags",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "AWS tags to apply. Format is <name1>=<value1>;<name2>=<value2>"
            };
        public static readonly CommandOption ARGUMENT_KMS_KEY_ARN =
            new CommandOption
            {
                Name = "KMS Key ARN",
                ShortSwitch = "-kk",
                Switch = "--kms-key",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "KMS Key ARN of a customer key used to encrypt the function's environment variables"
            };
        public static readonly CommandOption ARGUMENT_S3_BUCKET =
            new CommandOption
            {
                Name = "S3 Bucket",
                ShortSwitch = "-sb",
                Switch = "--s3-bucket",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "S3 bucket to upload the build output"
            };
        public static readonly CommandOption ARGUMENT_S3_PREFIX =
            new CommandOption
            {
                Name = "S3 Key Prefix",
                ShortSwitch = "-sp",
                Switch = "--s3-prefix",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "S3 prefix for for the build output"
            };
        public static readonly CommandOption ARGUMENT_STACK_NAME =
            new CommandOption
            {
                Name = "CloudFormation Stack Name",
                ShortSwitch = "-sn",
                Switch = "--stack-name",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "CloudFormation stack name for an AWS Serverless application"
            };
        public static readonly CommandOption ARGUMENT_CLOUDFORMATION_TEMPLATE =
            new CommandOption
            {
                Name = "CloudFormation Template",
                ShortSwitch = "-t",
                Switch = "--template",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Path to the CloudFormation template"
            };
        public static readonly CommandOption ARGUMENT_CLOUDFORMATION_TEMPLATE_PARAMETER =
            new CommandOption
            {
                Name = "CloudFormation Template Parameters",
                ShortSwitch = "-tp",
                Switch = "--template-parameters",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "CloudFormation template parameters. Format is <key1>=<value1>;<key2>=<value2>"
            };
        public static readonly CommandOption ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS =
            new CommandOption
            {
                Name = "CloudFormation Template Substitutions",
                ShortSwitch = "-ts",
                Switch = "--template-substitutions",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "JSON based CloudFormation template substitutions. Format is <JSONPath>=<Substitution>;<JSONPath>=..."
            };
        public static readonly CommandOption ARGUMENT_CLOUDFORMATION_DISABLE_CAPABILITIES =
            new CommandOption
            {
                Name = "Disable Capabilities",
                ShortSwitch = "-dc",
                Switch = "--disable-capabilities",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of capabilities to disable when creating a CloudFormation Stack."
            };
        public static readonly CommandOption ARGUMENT_STACK_WAIT =
            new CommandOption
            {
                Name = "Stack Wait",
                ShortSwitch = "-sw",
                Switch = "--stack-wait",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true wait for the Stack to finish updating before exiting. Default is true."
            };
        public static readonly CommandOption ARGUMENT_CLOUDFORMATION_ROLE =
            new CommandOption
            {
                Name = "CloudFormation Role ARN",
                ShortSwitch = "-cfrole",
                Switch = "--cloudformation-role",
                ValueType = CommandOption.CommandOptionValueType. StringValue,
                Description = "Optional role that CloudFormation assumes when creating or updated CloudFormation stack."
            };


        public static readonly CommandOption ARGUMENT_PAYLOAD =
            new CommandOption
            {
                Name = "Payload for function",
                ShortSwitch = "-p",
                Switch = "--payload",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The input payload to send to the Lambda function"
            };

        public static readonly CommandOption ARGUMENT_OUTPUT_PACKAGE =
            new CommandOption
            {
                Name = "The zip file that will be created with compiled and packaged Lambda function.",
                ShortSwitch = "-o",
                Switch = "--output-package",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The output zip file name"
            };


        public static readonly CommandOption ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE =
            new CommandOption
            {
                Name = "CloudFormation Ouptut Template",
                ShortSwitch = "-ot",
                Switch = "--output-template",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Path to write updated serverless template with CodeURI fields updated to the location of the packaged build artifacts in S3."
            };
        public static readonly CommandOption ARGUMENT_APPLY_DEFAULTS_FOR_UPDATE_OBSOLETE =
            new CommandOption
            {
                Name = "Apply Defaults for Update",
                Switch = "--apply-defaults",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Obsolete: as of version 3.0.0.0 defaults are always applied."
            };

        public static readonly CommandOption ARGUMENT_LAYER_NAME =
            new CommandOption
            {
                Name = "Layer Name",
                Switch = "--layer-name",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "AWS Lambda layer name"
            };
        public static readonly CommandOption ARGUMENT_LAYER_TYPE =
            new CommandOption
            {
                Name = "Layer Type",
                Switch = "--layer-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"The type of layer to publish. Valid values are: {LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE}"
            };

        public static readonly CommandOption ARGUMENT_LAYER_LICENSE_INFO =
            new CommandOption
            {
                Name = "Layer License Info",
                Switch = "--layer-license-info",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"License info to set on the Lambda layer"
            };

        public static readonly CommandOption ARGUMENT_PACKAGE_MANIFEST =
            new CommandOption
            {
                Name = "Package Manifest",
                Switch = "--package-manifest",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"Package manifest for a \"{LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE}\" layer that indicates the NuGet packages to add to the layer."
            };

        public static readonly CommandOption ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION =
            new CommandOption
            {
                Name = "Enable Package Optimization",
                Switch = "--enable-package-optimization",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true the packages will be pre-jitted to improve cold start performance. This must done on an Amazon Linux environment."
            };
    }
}