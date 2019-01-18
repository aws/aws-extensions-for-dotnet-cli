using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.ElasticBeanstalk.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public class CreatePackageCommand : EBBaseCommand
    {
        public const string COMMAND_NAME = "create-package";
        public const string COMMAND_DESCRIPTION = "Package the application to zip file to be deployed later";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS,

            EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION,
            EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT,
            EBDefinedCommandOptions.ARGUMENT_EB_VERSION_LABEL,
            EBDefinedCommandOptions.ARGUMENT_EB_TAGS,
            EBDefinedCommandOptions.ARGUMENT_APP_PATH,
            EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE,
            EBDefinedCommandOptions.ARGUMENT_EB_ADDITIONAL_OPTIONS,

            EBDefinedCommandOptions.ARGUMENT_CNAME_PREFIX,
            EBDefinedCommandOptions.ARGUMENT_SOLUTION_STACK,
            EBDefinedCommandOptions.ARGUMENT_ENVIRONMENT_TYPE,
            EBDefinedCommandOptions.ARGUMENT_EC2_KEYPAIR,
            EBDefinedCommandOptions.ARGUMENT_INSTANCE_TYPE,
            EBDefinedCommandOptions.ARGUMENT_HEALTH_CHECK_URL,
            EBDefinedCommandOptions.ARGUMENT_ENABLE_XRAY,
            EBDefinedCommandOptions.ARGUMENT_INSTANCE_PROFILE,
            EBDefinedCommandOptions.ARGUMENT_SERVICE_ROLE,

            EBDefinedCommandOptions.ARGUMENT_WAIT_FOR_UPDATE
        });

        DeployEnvironmentProperties _deployAppProperties;
        public DeployEnvironmentProperties DeployEnvironmentOptions
        {
            get
            {
                if (this._deployAppProperties == null)
                {
                    this._deployAppProperties = new DeployEnvironmentProperties();
                }

                return this._deployAppProperties;
            }
            set { this._deployAppProperties = value; }
        }

        public CreatePackageCommand(IToolLogger logger, string workingDirectory, string[] args)
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
                targetFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectLocation);
                if (string.IsNullOrEmpty(targetFramework))
                {
                    targetFramework = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
                }
            }

                string application = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.Application, EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION, true);
                string environment = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.Environment, EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT, true);
                string versionLabel = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.VersionLabel, EBDefinedCommandOptions.ARGUMENT_EB_VERSION_LABEL, false) ?? DateTime.Now.Ticks.ToString();

            var dotnetCli = new DotNetCLIWrapper(this.Logger, projectLocation);

            var publishLocation = Utilities.DeterminePublishLocation(null,  projectLocation, configuration, targetFramework);
            this.Logger?.WriteLine("Determine publish location: " + publishLocation);


            this.Logger?.WriteLine("Executing publish command");
            if (dotnetCli.Publish(projectLocation, publishLocation, targetFramework, configuration, publishOptions) != 0)
            {
                throw new ElasticBeanstalkExceptions("Error executing \"dotnet publish\"", ElasticBeanstalkExceptions.CommonErrorCode.DotnetPublishFailed);
            }

            SetupAWSDeploymentManifest(publishLocation);

            var zipArchivePath = Path.Combine(Directory.GetParent(publishLocation).FullName, new DirectoryInfo(projectLocation).Name + "-" + DateTime.Now.Ticks + ".zip");

            this.Logger?.WriteLine("Zipping up publish folder");
            Utilities.ZipDirectory(this.Logger, publishLocation, zipArchivePath);
            this.Logger?.WriteLine("Zip archive created: " + zipArchivePath);

            return Task.FromResult(true);
        }

        private void SetupAWSDeploymentManifest(string publishLocation)
        {
            var iisAppPath = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.UrlPath, EBDefinedCommandOptions.ARGUMENT_APP_PATH, false) ?? "/";
            var iisWebSite = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.IISWebSite, EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE, false) ?? "Default Web Site";


            var pathToManifest = Path.Combine(publishLocation, "aws-windows-deployment-manifest.json");
            string manifest;
            if (File.Exists(pathToManifest))
            {
                this.Logger?.WriteLine("Updating existing deployment manifest");

                Func<string, JsonData, JsonData> getOrCreateNode = (name, node) =>
                {
                    JsonData child = node[name] as JsonData;
                    if (child == null)
                    {
                        child = new JsonData();
                        node[name] = child;
                    }
                    return child;
                };

                JsonData root = JsonMapper.ToObject(File.ReadAllText(pathToManifest));
                if (root["manifestVersion"] == null || !root["manifestVersion"].IsInt)
                {
                    root["manifestVersion"] = 1;
                }

                JsonData deploymentNode = getOrCreateNode("deployments", root);

                JsonData aspNetCoreWebNode = getOrCreateNode("aspNetCoreWeb", deploymentNode);

                JsonData appNode;
                if (aspNetCoreWebNode.GetJsonType() == JsonType.None || aspNetCoreWebNode.Count == 0)
                {
                    appNode = new JsonData();
                    aspNetCoreWebNode.Add(appNode);
                }
                else
                    appNode = aspNetCoreWebNode[0];


                if (appNode["name"] == null || !appNode["name"].IsString || string.IsNullOrEmpty((string)appNode["name"]))
                {
                    appNode["name"] = "app";
                }

                JsonData parametersNode = getOrCreateNode("parameters", appNode);
                parametersNode["appBundle"] = ".";
                parametersNode["iisPath"] = iisAppPath;
                parametersNode["iisWebSite"] = iisWebSite;

                manifest = root.ToJson();
            }
            else
            {
                this.Logger?.WriteLine("Creating deployment manifest");

                manifest = EBConstants.DEFAULT_MANIFEST.Replace("{iisPath}", iisAppPath).Replace("{iisWebSite}", iisWebSite);

                if (File.Exists(pathToManifest))
                    File.Delete(pathToManifest);
            }

            this.Logger?.WriteLine("\tIIS App Path: " + iisAppPath);
            this.Logger?.WriteLine("\tIIS Web Site: " + iisWebSite);

            File.WriteAllText(pathToManifest, manifest);
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.DeployEnvironmentOptions.PersistSettings(this, data);
        }
    }
}
