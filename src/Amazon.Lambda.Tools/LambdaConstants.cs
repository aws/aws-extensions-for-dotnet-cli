using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.Lambda.Tools
{

    internal enum TemplateFormat { Json, Yaml }


    public static class LambdaConstants
    {
        public const string ENV_DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE = "DOTNET_LAMBDA_CLI_LOCAL_MANIFEST_OVERRIDE";

        public const string IAM_ARN_PREFIX = "arn:aws:iam::";
        public const string AWS_MANAGED_POLICY_ARN_PREFIX = "arn:aws:iam::aws:policy";

        public const string SERVERLESS_TAG_NAME = "AWSServerlessAppNETCore";

        public const int MAX_TEMPLATE_BODY_IN_REQUEST_SIZE = 50000;

        public const string DEFAULT_BUILD_CONFIGURATION = "Release";

        // The .NET Core 1.0 version of the runtime hierarchies for .NET Core taken from the corefx repository
        // https://github.com/dotnet/corefx/blob/release/1.0.0/pkg/Microsoft.NETCore.Platforms/runtime.json
        internal const string RUNTIME_HIERARCHY = "netcore.runtime.hierarchy.json";

        // The closest match to Amazon Linux
        internal const string RUNTIME_HIERARCHY_STARTING_POINT = "rhel.7.2-x64";


        public const string AWS_LAMBDA_MANAGED_POLICY_PREFIX = "AWSLambda";

        public static readonly Dictionary<string, string> KNOWN_MANAGED_POLICY_DESCRIPTIONS = new Dictionary<string, string>
        {
            {"arn:aws:iam::aws:policy/PowerUserAccess","Provides full access to AWS services and resources, but does not allow management of users and groups."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole","Provides write permissions to CloudWatch Logs."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaDynamoDBExecutionRole","Provides list and read access to DynamoDB streams and write permissions to CloudWatch Logs."},
            {"arn:aws:iam::aws:policy/AWSLambdaExecute","Provides Put, Get access to S3 and full access to CloudWatch Logs."},
            {"arn:aws:iam::aws:policy/AWSLambdaFullAccess","Provides full access to Lambda, S3, DynamoDB, CloudWatch Metrics and Logs."},
            {"arn:aws:iam::aws:policy/AWSLambdaInvocation-DynamoDB","Provides read access to DynamoDB Streams."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaKinesisExecutionRole","Provides list and read access to Kinesis streams and write permissions to CloudWatch Logs."},
            {"arn:aws:iam::aws:policy/AWSLambdaReadOnlyAccess","Provides read only access to Lambda, S3, DynamoDB, CloudWatch Metrics and Logs."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaRole","Default policy for AWS Lambda service role."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaSQSQueueExecutionRole","Provides receive message, delete message, and read attribute access to SQS queues, and write permissions to CloudWatch logs."},
            {"arn:aws:iam::aws:policy/service-role/AWSCodeDeployRoleForLambda","Provides CodeDeploy service access to perform a Lambda deployment on your behalf."},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaENIManagementAccess","Provides minimum permissions for a Lambda function to manage ENIs (create, describe, delete) used by a VPC-enabled Lambda Function."},
            {"arn:aws:iam::aws:policy/AWSDeepLensLambdaFunctionAccessPolicy","This policy specifies permissions required by DeepLens Administrative lambda functions that run on a DeepLens device"},
            {"arn:aws:iam::aws:policy/service-role/AWSLambdaVPCAccessExecutionRole","Provides minimum permissions for a Lambda function to execute while accessing a resource within a VPC"}
        };
    }
}
