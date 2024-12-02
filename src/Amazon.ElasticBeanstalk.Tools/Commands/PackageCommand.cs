using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public class PackageCommand : EBBaseCommand
    {
        public const string COMMAND_NAME = "package";
        public const string COMMAND_DESCRIPTION = "Package the application to a zip file to be deployed later to an Elastic Beanstalk environment";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS,

            EBDefinedCommandOptions.ARGUMENT_APP_PATH,
            EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE,
            EBDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE
        });

        public string OutputPackageFileName { get; set; }

        public DeployEnvironmentProperties DeployEnvironmentOptions { get; } = new DeployEnvironmentProperties();

        public PackageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            this.DeployEnvironmentOptions.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.Switch)) != null)
                this.OutputPackageFileName = tuple.Item2.StringValue;

        }


        protected override Task<bool> PerformActionAsync()
        {
            this.EnsureInProjectDirectory();

            var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory,
                this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));

            string configuration = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false) ?? "Release";
            string targetFramework = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
            string publishOptions = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.PublishOptions, CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS, false);

            if (string.IsNullOrEmpty(targetFramework))
            {
                targetFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectLocation, null);
                if (string.IsNullOrEmpty(targetFramework))
                {
                    targetFramework = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                }
            }

            var dotnetCli = new DotNetCLIWrapper(this.Logger, projectLocation);

            var publishLocation = Utilities.DeterminePublishLocation(null,  projectLocation, configuration, targetFramework);
            this.Logger?.WriteLine("Determine publish location: " + publishLocation);


            this.Logger?.WriteLine("Executing publish command");
            if (dotnetCli.Publish(projectLocation, publishLocation, targetFramework, configuration, publishOptions) != 0)
            {
                throw new ElasticBeanstalkExceptions("Error executing \"dotnet publish\"", ElasticBeanstalkExceptions.CommonErrorCode.DotnetPublishFailed);
            }

            EBUtilities.SetupAWSDeploymentManifest(this.Logger, this, this.DeployEnvironmentOptions, publishLocation);

            string package = this.GetStringValueOrDefault(this.OutputPackageFileName, EBDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false);
            string zipArchivePath  = null;

            if (!string.IsNullOrWhiteSpace(package))
            {
                zipArchivePath = package;
            }
            else
            {
                zipArchivePath = Path.Combine(Directory.GetParent(publishLocation).FullName, new DirectoryInfo(projectLocation).Name + "-" + DateTime.Now.Ticks + ".zip");
            }

            this.Logger?.WriteLine("Zipping up publish folder");
            Utilities.ZipDirectory(this.Logger, publishLocation, zipArchivePath);
            this.Logger?.WriteLine("Zip archive created: " + zipArchivePath);

            return Task.FromResult(true);
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.DeployEnvironmentOptions.PersistSettings(this, data);
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.ConfigFileKey, this.GetStringValueOrDefault(this.OutputPackageFileName, EBDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false));
        }
    }
}
