using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR.Model;
using Amazon.ECR;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;

namespace Amazon.ECS.Tools.Commands
{
    public class PushDockerImageCommand : BasePushDockerImageCommand<ECSToolsDefaults>
    {
        public const string COMMAND_NAME = "push-image";
        public const string COMMAND_DESCRIPTION = "Build Docker image and push the image to Amazon ECR.";

        protected override string ToolName => Constants.TOOLNAME;

        public PushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, args)
        {
        }
    }
}
