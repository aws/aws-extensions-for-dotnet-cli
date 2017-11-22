using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.ECS.Tools.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.Common.DotNetCli.Tools.CLi;

namespace Amazon.ECS.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            var application = new Application("ecs", "Amazon EC2 Container Service Tools for .NET Core applications", "https://github.com/aws/aws-extensions-for-dotnet-cli",
                new List<ICommandInfo>()
                {
                    new GroupHeaderInfo("Commands to deploy to Amazon EC2 Container Service:"),
                    new CommandInfo<DeployServiceCommand>(DeployServiceCommand.COMMAND_NAME, DeployServiceCommand.COMMAND_DESCRIPTION, DeployServiceCommand.CommandOptions),
                    new CommandInfo<DeployTaskCommand>(DeployTaskCommand.COMMAND_NAME, DeployTaskCommand.COMMAND_DESCRIPTION, DeployTaskCommand.CommandOptions),
                    new CommandInfo<DeployScheduledTaskCommand>(DeployScheduledTaskCommand.COMMAND_NAME, DeployScheduledTaskCommand.COMMAND_DESCRIPTION, DeployScheduledTaskCommand.CommandOptions),
                    new GroupHeaderInfo("Commands to manage docker images to Amazon EC2 Container Registry:"),
                    new CommandInfo<PushDockerImageCommand>(PushDockerImageCommand.COMMAND_NAME, PushDockerImageCommand.COMMAND_DESCRIPTION, PushDockerImageCommand.CommandOptions)
                });

            var exitCode = application.Execute(args);

            if (exitCode != 0)
            {
                Environment.Exit(-1);
            }
        }
    }
}
