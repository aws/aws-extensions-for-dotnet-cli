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

        public static readonly IList<CommandOption> PackageCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS,
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIG_FILE,
            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
            LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK
        });

        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string OutputPackageFileName { get; set; }
        
        public string MSBuildParameters { get; set; }
        public string[] LayerVersionArns { get; set; }

        public bool? DisableVersionCheck { get; set; }
        
        /// <summary>
        /// Property set when the package command is being created from another command or tool
        /// and the service clients have been copied over. In that case there is no reason
        /// to look for a region or aws credentials.
        /// </summary>
        public bool DisableRegionCheck { get; set; }

        
        /// <summary>
        /// If runtime package store layers were specified the DOTNET_SHARED_STORE environment variable
        /// has to be set. This property will contain the value the environment variable must be set.
        /// </summary>
        public string NewDotnetSharedStoreValue { get; private set; }
        

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
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS.Switch)) != null)
                this.LayerVersionArns = tuple.Item2.StringValues;

            if (!string.IsNullOrEmpty(values.MSBuildParameters))
            {
                if (this.MSBuildParameters == null)
                    this.MSBuildParameters = values.MSBuildParameters;
                else
                    this.MSBuildParameters += " " + values.MSBuildParameters;
            }
        }

        protected override async Task<bool> PerformActionAsync()
        {
            EnsureInProjectDirectory();

            // Disable interactive since this command is intended to be run as part of a pipeline.
            this.DisableInteractive = true;

            var layerVersionArns = this.GetStringValuesOrDefault(this.LayerVersionArns, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_LAYERS, false);
            LayerPackageInfo layerPackageInfo = null;
            if (layerVersionArns != null)
            {
                if (!this.DisableRegionCheck)
                {
                    // Region and credentials are only required if using layers. This is new behavior so do a preemptive check when there are layers to
                    // see if region and credentials are set. If they are not set give a specific error message about region and credentials required
                    // when using layers.
                    try
                    {
                        base.DetermineAWSRegion();
                    }
                    catch (Exception)
                    {
                        throw new ToolsException("Region is required for the package command when layers are specified. The layers must be inspected to see how they affect packaging.", ToolsException.CommonErrorCode.RegionNotConfigured);
                    }
                    try
                    {
                        base.DetermineAWSCredentials();
                    }
                    catch (Exception)
                    {
                        throw new ToolsException("AWS credentials are required for the package command when layers are specified. The layers must be inspected to see how they affect packaging.", ToolsException.CommonErrorCode.InvalidCredentialConfiguration);
                    }                    
                }

                layerPackageInfo = await LambdaUtilities.LoadLayerPackageInfos(this.Logger, this.LambdaClient, this.S3Client, layerVersionArns);
            }
            else
            {
                layerPackageInfo = new LayerPackageInfo();
            }


            // Release will be the default configuration if nothing set.
            var configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false);

            var targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
            var projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
            var msbuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false);
            var disableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault();

            var zipArchivePath = GetStringValueOrDefault(this.OutputPackageFileName, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_PACKAGE, false);

            string publishLocation;
            var success = LambdaPackager.CreateApplicationBundle(this.DefaultConfig, this.Logger, this.WorkingDirectory, projectLocation, configuration, targetFramework, msbuildParameters, disableVersionCheck, layerPackageInfo, out publishLocation, ref zipArchivePath);
            if (!success)
            {
                this.Logger.WriteLine("Failed to create application package");
                return false;
            }


            this.Logger.WriteLine("Lambda project successfully packaged: " + zipArchivePath);
            var dotnetSharedStoreValue = layerPackageInfo.GenerateDotnetSharedStoreValue();
            if(!string.IsNullOrEmpty(dotnetSharedStoreValue))
            {
                this.NewDotnetSharedStoreValue = dotnetSharedStoreValue;
                
                this.Logger.WriteLine($"\nWarning: You must the {LambdaConstants.ENV_DOTNET_SHARED_STORE} environment variable when deploying the package. " +
                                      "If not set the layers specified will not be located by the .NET Core runtime. The trailing '/' is required.");
                this.Logger.WriteLine($"{LambdaConstants.ENV_DOTNET_SHARED_STORE}: {dotnetSharedStoreValue}");
            }

            return true;
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