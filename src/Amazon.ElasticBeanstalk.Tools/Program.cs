using System;

using Amazon.Common.DotNetCli.Tools.CLi;
using Amazon.Common.DotNetCli.Tools.Commands;
using System.Collections.Generic;
using Amazon.ElasticBeanstalk.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ElasticBeanstalk.Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            MSBuildInitializer.Initialize();

            var application = new Application("eb", "Amazon Elastic Beanstalk Tools for .NET Core applications", "https://github.com/aws/aws-extensions-for-dotnet-cli",
                new List<ICommandInfo>()
                {
                    new GroupHeaderInfo("Commands to manage environments:"),
                    new CommandInfo<DeployEnvironmentCommand>(DeployEnvironmentCommand.COMMAND_NAME, DeployEnvironmentCommand.COMMAND_DESCRIPTION, DeployEnvironmentCommand.CommandOptions),
                    new CommandInfo<ListEnvironmentsCommand>(ListEnvironmentsCommand.COMMAND_NAME, ListEnvironmentsCommand.COMMAND_DESCRIPTION, ListEnvironmentsCommand.CommandOptions),
                    new CommandInfo<DeleteEnvironmentCommand>(DeleteEnvironmentCommand.COMMAND_NAME, DeleteEnvironmentCommand.COMMAND_DESCRIPTION, DeleteEnvironmentCommand.CommandOptions),

                    new GroupHeaderInfo("Other Commands:"),
                    new CommandInfo<PackageCommand>(PackageCommand.COMMAND_NAME, PackageCommand.COMMAND_DESCRIPTION, PackageCommand.CommandOptions),
                });

            var exitCode = application.Execute(args);

            if (exitCode != 0)
            {
                Environment.Exit(-1);
            }
        }
    }
}
