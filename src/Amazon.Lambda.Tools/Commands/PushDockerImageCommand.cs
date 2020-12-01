using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Amazon.Lambda.Tools.Commands
{
    public class PushDockerImageCommand : BasePushDockerImageCommand<LambdaToolsDefaults>
    {
        public const string COMMAND_NAME = "push-image";
        public const string COMMAND_DESCRIPTION = "Build Lambda Docker image and push the image to Amazon ECR.";

        protected override string ToolName => LambdaConstants.TOOLNAME;

        public PushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, args)
        {
        }

        /// <summary>
        /// Exposed to allow other command within Amazon.Lambda.Tools to reuse the push command but not have the exception trapping logic
        /// in the ExecuteAsync method.
        /// </summary>
        /// <returns></returns>
        internal System.Threading.Tasks.Task<bool> PushImageAsync()
        {
            return base.PerformActionAsync();
        }

        protected override void BuildProject(string projectLocation, string configuration, string targetFramework, string publishOptions, string publishLocation)
        {
            this.EnsureInProjectDirectory();

            var dotnetCli = new LambdaDotNetCLIWrapper(this.Logger, projectLocation);
            this.Logger?.WriteLine("Executing publish command");
            if (dotnetCli.Publish(this.DefaultConfig, projectLocation, publishLocation, targetFramework, configuration, publishOptions, null) != 0)
            {
                throw new ToolsException("Error executing \"dotnet publish\"", ToolsException.CommonErrorCode.DotnetPublishFailed);
            }
        }
    }
}
