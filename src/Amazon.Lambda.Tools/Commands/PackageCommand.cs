using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    public class PackageCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "package";
        public const string COMMAND_DESCRIPTION = "Command to package a Lambda project into a zip file ready for deployment";
        public const string COMMAND_ARGUMENTS = "<ZIP-FILE> The name of the zip file to package the project into";

        public static readonly IList<CommandOption> PackageCommandOptions = new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS,
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE,
            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
            LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK
        };

        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string OutputPackageFileName { get; set; }
        
        public string MSBuildParameters { get; set; }

        public bool? DisableVersionCheck { get; set; }


        /// <summary>
        /// If the value for Payload points to an existing file then the contents of the file is sent, otherwise
        /// value of Payload is sent as the input to the function.
        /// </summary>
        public string Payload { get; set; }

        public PackageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, PackageCommandOptions, args)
        {
        }


        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.OutputPackageFileName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.Switch)) != null)
                this.OutputPackageFileName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.Switch)) != null)
                this.DisableVersionCheck = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.Switch)) != null)
                this.MSBuildParameters = tuple.Item2.StringValue;

            if (!string.IsNullOrEmpty(values.MSBuildParameters))
            {
                if (this.MSBuildParameters == null)
                    this.MSBuildParameters = values.MSBuildParameters;
                else
                    this.MSBuildParameters += " " + values.MSBuildParameters;
            }
        }

        protected override Task<bool> PerformActionAsync()
        {
            EnsureInProjectDirectory();

            // Disable interactive since this command is intended to be run as part of a pipeline.
            this.DisableInteractive = true;

            return Task.Run(() =>
            {
                // Release will be the default configuration if nothing set.
                var configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false);

                var targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                var projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
                var msbuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false);
                var disableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault();

                var zipArchivePath = GetStringValueOrDefault(this.OutputPackageFileName, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false);

                string publishLocation;
                var success = LambdaPackager.CreateApplicationBundle(this.DefaultConfig, this.Logger, this.WorkingDirectory, projectLocation, configuration, targetFramework, msbuildParameters, disableVersionCheck, out publishLocation, ref zipArchivePath);
                if (!success)
                {
                    this.Logger.WriteLine("Failed to create application package");
                    return false;
                }


                this.Logger.WriteLine("Lambda project successfully packaged: " + zipArchivePath);

                return true;
            });
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.ConfigFileKey, this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE.ConfigFileKey, this.GetStringValueOrDefault(this.OutputPackageFileName, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.ConfigFileKey, this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false));    
        }
    }
}