using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    public class PushDockerImageCommand : BasePushDockerImageCommand<LambdaToolsDefaults>
    {
        public const string COMMAND_NAME = "push-image";
        public const string COMMAND_DESCRIPTION = "Build Lambda Docker image and push the image to Amazon ECR.";

        public static readonly IList<CommandOption> LambdaPushCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE,
        }, CommonOptions);

        protected override string ToolName => LambdaConstants.TOOLNAME;

        public string Architecture { get; set; }

        public PushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, LambdaPushCommandOptions, args)
        {
        }

        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.Switch)) != null)
                this.Architecture = tuple.Item2.StringValue;
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

            var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);

            var dotnetCli = new LambdaDotNetCLIWrapper(this.Logger, projectLocation);
            this.Logger?.WriteLine("Executing publish command");
            if (dotnetCli.Publish(defaults: this.DefaultConfig,
                                    projectLocation: projectLocation,
                                    outputLocation: publishLocation,
                                    targetFramework: targetFramework,
                                    configuration: configuration,
                                    msbuildParameters: publishOptions,
                                    architecture: architecture,
                                    publishManifests: null) != 0)
            {
                throw new ToolsException("Error executing \"dotnet publish\"", ToolsException.CommonErrorCode.DotnetPublishFailed);
            }
        }

        protected override int ExecuteDockerBuild(DockerCLIWrapper dockerCli, string dockerBuildWorkingDirectory, string fullDockerfilePath, string dockerImageTag, string dockerBuildOptions)
        {
            var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);
            var arm64Build = false;

            if (string.Equals(LambdaConstants.RUNTIME_LINUX_ARM64, LambdaUtilities.DetermineRuntimeParameter(null, architecture)))
            {
                arm64Build = true;
            }

            return dockerCli.Build(dockerBuildWorkingDirectory, fullDockerfilePath, dockerImageTag, dockerBuildOptions, arm64Build);
        }

        protected override void SaveConfigFile(JsonData data)
        {
            base.SaveConfigFile(data);
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.ConfigFileKey, this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false));
        }
    }
}
