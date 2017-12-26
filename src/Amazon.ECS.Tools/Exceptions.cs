using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Runtime;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// The deploy tool exception. This is used to throw back an error to the user but is considerd a known error
    /// so the stack trace will not be displayed.
    /// </summary>
    public class DockerToolsException : ToolsException
    {
        public enum ECSErrorCode {

            DockerBuildFailed,
            FailedToFindSolutionDirectory,

            FailedToSetupECRRepository,
            GetECRAuthTokens,
            DockerCLILoginFail,
            DockerTagFail,
            DockerPushFail,
            EnsureClusterExistsFail,

            FailedToUpdateTaskDefinition,
            FailedToExpandImageTag,
            FailedToUpdateService,
            ClusterNotFound,

            PutRuleFail,
            PutTargetFail,

            RunTaskFail,

            LogGroupDescribeFailed,
            LogGroupCreateFailed
        }

        public DockerToolsException(string message, ECSErrorCode code) : base(message, code.ToString(), null)
        {
        }

        public DockerToolsException(string message, CommonErrorCode code) : base(message, code.ToString(), null)
        {
        }

        public DockerToolsException(string message, ECSErrorCode code, Exception e) : base(message, code.ToString(), e)
        {
        }

    }
}
