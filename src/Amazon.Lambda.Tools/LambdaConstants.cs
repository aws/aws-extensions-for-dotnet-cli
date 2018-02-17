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

        // The .NET Core 1.0 version of the runtime hierarchies for .NET Core taken from the corefx repository
        // https://github.com/dotnet/corefx/blob/release/1.0.0/pkg/Microsoft.NETCore.Platforms/runtime.json
#if NETCORE
        internal const string RUNTIME_HIERARCHY = "Amazon.Lambda.Tools.Resources.netcore.runtime.hierarchy.json";
#else
        internal const string RUNTIME_HIERARCHY = "Amazon.AWSToolkit.Lambda.LambdaTools.Resources.netcore.runtime.hierarchy.json";
#endif

        // The closest match to Amazon Linux
        internal const string RUNTIME_HIERARCHY_STARTING_POINT = "rhel.7.2-x64";


    }
}
