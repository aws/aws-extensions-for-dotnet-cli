using System;
using System.Collections.Generic;
using Amazon.Common.DotNetCli.Tools.CLi;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Lambda.Tools.Commands;

namespace Amazon.Lambda.Tools
{
    public class Program
    {
        static void Main(string[] args)
        {
            var application = new Application("lambda", "Amazon Lambda Tools for .NET Core applications", "https://github.com/aws/aws-extensions-for-dotnet-cli, https://github.com/aws/aws-lambda-dotnet",
                new List<ICommandInfo>()
                {
                    new GroupHeaderInfo("Commands to deploy and manage AWS Lambda functions:"),
                    new CommandInfo<DeployFunctionCommand>(DeployFunctionCommand.COMMAND_DEPLOY_NAME, DeployFunctionCommand.COMMAND_DEPLOY_DESCRIPTION, DeployFunctionCommand.DeployCommandOptions, DeployFunctionCommand.COMMAND_DEPLOY_ARGUMENTS),
                    new CommandInfo<InvokeFunctionCommand>(InvokeFunctionCommand.COMMAND_NAME, InvokeFunctionCommand.COMMAND_DESCRIPTION, InvokeFunctionCommand.InvokeCommandOptions, InvokeFunctionCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<ListFunctionCommand>(ListFunctionCommand.COMMAND_NAME, ListFunctionCommand.COMMAND_DESCRIPTION, ListFunctionCommand.ListCommandOptions),
                    new CommandInfo<DeleteFunctionCommand>(DeleteFunctionCommand.COMMAND_NAME, DeleteFunctionCommand.COMMAND_DESCRIPTION, DeleteFunctionCommand.DeleteCommandOptions, DeleteFunctionCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<GetFunctionConfigCommand>(GetFunctionConfigCommand.COMMAND_NAME, GetFunctionConfigCommand.COMMAND_DESCRIPTION, GetFunctionConfigCommand.GetConfigCommandOptions, GetFunctionConfigCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<UpdateFunctionConfigCommand>(UpdateFunctionConfigCommand.COMMAND_NAME, UpdateFunctionConfigCommand.COMMAND_DESCRIPTION, UpdateFunctionConfigCommand.UpdateCommandOptions, UpdateFunctionConfigCommand.COMMAND_ARGUMENTS),
                    
                    new GroupHeaderInfo("Commands to deploy and manage AWS Serverless applications using AWS CloudFormation:"),
                    new CommandInfo<DeployServerlessCommand>(DeployServerlessCommand.COMMAND_NAME, DeployServerlessCommand.COMMAND_DESCRIPTION, DeployServerlessCommand.DeployServerlessCommandOptions, DeployServerlessCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<ListServerlessCommand>(ListServerlessCommand.COMMAND_NAME, ListServerlessCommand.COMMAND_DESCRIPTION, ListServerlessCommand.ListCommandOptions),
                    new CommandInfo<DeleteServerlessCommand>(DeleteServerlessCommand.COMMAND_NAME, DeleteServerlessCommand.COMMAND_DESCRIPTION, DeleteServerlessCommand.DeleteCommandOptions, DeleteServerlessCommand.COMMAND_ARGUMENTS),

                    new GroupHeaderInfo("Commands to publish and manage AWS Lambda Layers:"),
                    new CommandInfo<PublishLayerCommand>(PublishLayerCommand.COMMAND_NAME, PublishLayerCommand.COMMAND_DESCRIPTION, PublishLayerCommand.PublishLayerCommandOptions, PublishLayerCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<ListLayersCommand>(ListLayersCommand.COMMAND_NAME, ListLayersCommand.COMMAND_DESCRIPTION, ListLayersCommand.ListCommandOptions),
                    new CommandInfo<ListLayerVersionsCommand>(ListLayerVersionsCommand.COMMAND_NAME, ListLayerVersionsCommand.COMMAND_DESCRIPTION, ListLayerVersionsCommand.ListCommandOptions, ListLayerVersionsCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<GetLayerVersionDetailsCommand>(GetLayerVersionDetailsCommand.COMMAND_NAME, GetLayerVersionDetailsCommand.COMMAND_DESCRIPTION, GetLayerVersionDetailsCommand.CommandOptions, GetLayerVersionDetailsCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<DeleteLayerVersionCommand>(DeleteLayerVersionCommand.COMMAND_NAME, DeleteLayerVersionCommand.COMMAND_DESCRIPTION, DeleteLayerVersionCommand.CommandOptions, DeleteLayerVersionCommand.COMMAND_ARGUMENTS),

                    new GroupHeaderInfo("Other Commands:"),
                    new CommandInfo<PackageCommand>(PackageCommand.COMMAND_NAME, PackageCommand.COMMAND_DESCRIPTION, PackageCommand.PackageCommandOptions, PackageCommand.COMMAND_ARGUMENTS),
                    new CommandInfo<PackageCICommand>(PackageCICommand.COMMAND_NAME, PackageCICommand.COMMAND_SYNOPSIS, PackageCICommand.PackageCICommandOptions),
                    new CommandInfo<PushDockerImageCommand>(PushDockerImageCommand.COMMAND_NAME, PushDockerImageCommand.COMMAND_DESCRIPTION, PushDockerImageCommand.LambdaPushCommandOptions)
                });

            var exitCode = application.Execute(args);

            if (exitCode != 0)
            {
                Environment.Exit(-1);
            }
        }
    }
}
