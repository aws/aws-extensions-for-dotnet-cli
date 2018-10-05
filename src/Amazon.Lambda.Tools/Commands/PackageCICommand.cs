using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Tools.TemplateProcessor;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    public class PackageCICommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "package-ci";
        public const string COMMAND_SYNOPSIS = "Command to use as part of a continuous integration system.";
        public const string COMMAND_DESCRIPTION =
            "Command for use as part of the build step in a continuous integration pipeline. To perform the deployment this command requires a CloudFormation template similar to the one used by Serverless projects. " +
            "The command performs the following actions: \n" +
            "\t 1) Build and package .NET Core project\n" +
            "\t 2) Upload build archive to Amazon S3\n" +
            "\t 3) Read in AWS CloudFormation template\n" +
            "\t 4) Update AWS::Lambda::Function and AWS::Serverless::Function resources to the location of the uploaded build archive\n" +
            "\t 5) Write out updated CloudFormation template\n\n" +
            "The output CloudFormation template should be used as the build step's output artifact. The deployment stage of the pipeline will use the outputted template to create a CloudFormation ChangeSet and then execute ChangeSet.";

        public static readonly IList<CommandOption> PackageCICommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS,
            LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE,
            LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS,
            LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE,
            LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET,
            LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX,
            LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK
        });

        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string MSBuildParameters { get; set; }

        public string S3Bucket { get; set; }
        public string S3Prefix { get; set; }

        public string CloudFormationTemplate { get; set; }
        public Dictionary<string, string> TemplateSubstitutions { get; set; }

        public bool? DisableVersionCheck { get; set; }

        public string CloudFormationOutputTemplate { get; set; }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE.Switch)) != null)
                this.CloudFormationTemplate = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS.Switch)) != null)
                this.TemplateSubstitutions = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE.Switch)) != null)
                this.CloudFormationOutputTemplate = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.Switch)) != null)
                this.S3Bucket = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.Switch)) != null)
                this.S3Prefix = tuple.Item2.StringValue;
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

        public PackageCICommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, PackageCICommandOptions, args)
        {
        }

        protected override async Task<bool> PerformActionAsync()
        {
            // Disable interactive since this command is intended to be run as part of a pipeline.
            DisableInteractive = true;
            
            string projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
            string s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, true);
            string s3Prefix = this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false);
            string templatePath = this.GetStringValueOrDefault(this.CloudFormationTemplate, LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE, true);
            string outputTemplatePath = this.GetStringValueOrDefault(this.CloudFormationOutputTemplate, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE, true);

            if (!Path.IsPathRooted(templatePath))
            {
                templatePath = Path.Combine(Utilities.DetermineProjectLocation(this.WorkingDirectory, projectLocation), templatePath);
            }

            if (!File.Exists(templatePath))
                throw new LambdaToolsException($"Template file {templatePath} cannot be found.", LambdaToolsException.LambdaErrorCode.ServerlessTemplateNotFound);

            await Utilities.ValidateBucketRegionAsync(this.S3Client, s3Bucket);

            var templateBody = File.ReadAllText(templatePath);

            // Process any template substitutions
            templateBody = LambdaUtilities.ProcessTemplateSubstitions(this.Logger, templateBody, this.GetKeyValuePairOrDefault(this.TemplateSubstitutions, LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS, false), Utilities.DetermineProjectLocation(this.WorkingDirectory, projectLocation));
                        
            var options = new DefaultLocationOption
            {
                Configuration = this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false),
                TargetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false),
                MSBuildParameters = this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false),
                DisableVersionCheck = this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false).GetValueOrDefault()
            };
            
            var templateProcessor = new TemplateProcessorManager(this.Logger, this.S3Client, s3Bucket, s3Prefix, options);
            templateBody = await templateProcessor.TransformTemplateAsync(templatePath, templateBody);            
            
            this.Logger.WriteLine($"Writing updated template: {outputTemplatePath}");
            File.WriteAllText(outputTemplatePath, templateBody);

            return true;            
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION.ConfigFileKey, this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, this.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));    
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS.ConfigFileKey, this.GetStringValueOrDefault(this.MSBuildParameters, CommonDefinedCommandOptions.ARGUMENT_MSBUILD_PARAMETERS, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK.ConfigFileKey, this.GetBoolValueOrDefault(this.DisableVersionCheck, LambdaDefinedCommandOptions.ARGUMENT_DISABLE_VERSION_CHECK, false));    
            

            var template = this.GetStringValueOrDefault(this.CloudFormationTemplate, LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE, false);
            if(Path.IsPathRooted(template))
            {
                string projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
                var projectRoot = Utilities.DetermineProjectLocation(this.WorkingDirectory, projectLocation);
                if(template.StartsWith(projectRoot))
                {
                    template = template.Substring(projectRoot.Length + 1);
                }
            }
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE.ConfigFileKey, template);
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS.ConfigFileKey, LambdaToolsDefaults.FormatKeyValue(this.GetKeyValuePairOrDefault(this.TemplateSubstitutions, LambdaDefinedCommandOptions.ARGUMENT_CLOUDFORMATION_TEMPLATE_SUBSTITUTIONS, false)));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE.ConfigFileKey, this.GetStringValueOrDefault(this.CloudFormationOutputTemplate, LambdaDefinedCommandOptions.ARGUMENT_OUTPUT_CLOUDFORMATION_TEMPLATE, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.ConfigFileKey, this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.ConfigFileKey, this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false));    
        }
    }
}
