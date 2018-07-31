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
    public class DeployEnvironmentCommand : EBBaseCommand
    {
        public const string COMMAND_NAME = "deploy-environment";
        public const string COMMAND_DESCRIPTION = "Deploy the application to an AWS Elastic Beanstalk environment.";

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

        public DeployEnvironmentCommand(IToolLogger logger, string workingDirectory, string[] args)
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


        protected override async Task<bool> PerformActionAsync()
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

            bool doesApplicationExist = await DoesApplicationExist(application);
            bool doesEnvironmentExist = doesApplicationExist ? await DoesEnvironmentExist(application, environment) : false;

            if(!doesApplicationExist)
            {
                try
                {
                    this.Logger?.WriteLine("Creating new Elastic Beanstalk Application");
                    await this.EBClient.CreateApplicationAsync(new CreateApplicationRequest
                    {
                        ApplicationName = application
                    });
                }
                catch(Exception e)
                {
                    throw new ElasticBeanstalkExceptions("Error creating Elastic Beanstalk application: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedCreateApplication);
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

            SetupAWSDeploymentManifest(publishLocation);

            var zipArchivePath = Path.Combine(Directory.GetParent(publishLocation).FullName, new DirectoryInfo(projectLocation).Name + "-" + DateTime.Now.Ticks + ".zip");

            this.Logger?.WriteLine("Zipping up publish folder");
            Utilities.ZipDirectory(this.Logger, publishLocation, zipArchivePath);
            this.Logger?.WriteLine("Zip archive created: " + zipArchivePath);

            S3Location s3Loc;
            try
            {
                s3Loc = await this.UploadDeploymentPackageAsync(application, versionLabel, zipArchivePath).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkExceptions("Error uploading application bundle to S3: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedToUploadBundle);
            }

            try
            {
                this.Logger?.WriteLine("Creating new application version: " + versionLabel);
                await this.EBClient.CreateApplicationVersionAsync(new CreateApplicationVersionRequest
                {
                    ApplicationName = application,
                    VersionLabel = versionLabel,
                    SourceBundle = s3Loc
                }).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                throw new ElasticBeanstalkExceptions("Error creating Elastic Beanstalk application version: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedCreateApplicationVersion);
            }

            this.Logger?.WriteLine("Getting latest environment event date before update");
            var startingEventDate = await GetLatestEventDateAsync(application, environment);

            string environmentArn;
            if(doesEnvironmentExist)
            {
                environmentArn = await UpdateEnvironment(application, environment, versionLabel);
            }
            else
            {
                environmentArn = await CreateEnvironment(application, environment, versionLabel);
            }

            bool? waitForUpdate = this.GetBoolValueOrDefault(this.DeployEnvironmentOptions.WaitForUpdate, EBDefinedCommandOptions.ARGUMENT_WAIT_FOR_UPDATE, false);
            if(!waitForUpdate.HasValue || waitForUpdate.Value)
            {
                this.Logger?.WriteLine("Waiting for environment update to complete");
                var success = await this.WaitForDeploymentCompletionAsync(application, environment, startingEventDate);

                if (success)
                    this.Logger?.WriteLine("Update Complete");
                else
                    throw new ElasticBeanstalkExceptions("Environment update failed", ElasticBeanstalkExceptions.EBCode.FailedEnvironmentUpdate);

            }
            else
            {
                this.Logger?.WriteLine("Environment update initiated");
            }

            if (doesEnvironmentExist)
            {
                var tags = ConvertToTagsCollection();
                if (tags != null && tags.Count > 0)
                {
                    var updateTagsRequest = new UpdateTagsForResourceRequest
                    {
                        ResourceArn = environmentArn,
                        TagsToAdd = tags
                    };
                    this.Logger?.WriteLine("Updating Tags on environment");
                    try
                    {
                        await this.EBClient.UpdateTagsForResourceAsync(updateTagsRequest);
                    }
                    catch(Exception e)
                    {
                        throw new ElasticBeanstalkExceptions("Error updating tags for environment: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedToUpdateTags);
                    }
                }
            }

            return true;
        }

        private List<Tag> ConvertToTagsCollection()
        {
            var tags = this.GetKeyValuePairOrDefault(this.DeployEnvironmentOptions.Tags, EBDefinedCommandOptions.ARGUMENT_EB_TAGS, false);
            if (tags == null || tags.Count == 0)
                return null;

            var collection = new List<Tag>();
            foreach(var kvp in tags)
            {
                collection.Add(new Tag { Key = kvp.Key, Value = kvp.Value });
            }
            return collection;
        }

        private async Task<string> CreateEnvironment(string application, string environment, string versionLabel)
        {
            var createRequest = new CreateEnvironmentRequest
            {
                ApplicationName = application,
                EnvironmentName = environment,
                VersionLabel = versionLabel,
                SolutionStackName = this.GetSolutionStackOrDefault(this.DeployEnvironmentOptions.SolutionStack, EBDefinedCommandOptions.ARGUMENT_SOLUTION_STACK, true),
                CNAMEPrefix = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.CNamePrefix, EBDefinedCommandOptions.ARGUMENT_CNAME_PREFIX, false)
            };

            var environmentType = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.EnvironmentType, EBDefinedCommandOptions.ARGUMENT_ENVIRONMENT_TYPE, false) ?? "LoadBalanced";
            if(!string.IsNullOrEmpty(environmentType))
            {
                createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:environment",
                    OptionName = "EnvironmentType",
                    Value = environmentType
                });
            }

            var healthCheckURl = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.HealthCheckUrl, EBDefinedCommandOptions.ARGUMENT_HEALTH_CHECK_URL, false);
            if(string.IsNullOrEmpty(healthCheckURl) && string.Equals(environmentType, "LoadBalanced", StringComparison.OrdinalIgnoreCase))
            {
                healthCheckURl = "/";
            }

            if (!string.IsNullOrEmpty(healthCheckURl))
            {
                createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:application",
                    OptionName = "Application Healthcheck URL",
                    Value = healthCheckURl
                });
            }

            var ec2KeyPair = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.EC2KeyPair, EBDefinedCommandOptions.ARGUMENT_EC2_KEYPAIR, false);
            if (!string.IsNullOrEmpty(ec2KeyPair))
            {
                createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "EC2KeyName",
                    Value = ec2KeyPair
                });
            }

            var instanceType = this.GetStringValueOrDefault(this.DeployEnvironmentOptions.InstanceType, EBDefinedCommandOptions.ARGUMENT_INSTANCE_TYPE, false);
            if (string.IsNullOrEmpty(instanceType))
                instanceType = "t2.small";

            createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
            {
                Namespace = "aws:autoscaling:launchconfiguration",
                OptionName = "InstanceType",
                Value = instanceType
            });

            var instanceProfile = this.GetInstanceProfileOrDefault(this.DeployEnvironmentOptions.InstanceProfile, EBDefinedCommandOptions.ARGUMENT_INSTANCE_PROFILE, true, string.Format("eb_{0}_{1}", application, environment));
            if (!string.IsNullOrEmpty(instanceProfile))
            {
                int pos = instanceProfile.LastIndexOf('/');
                if (pos != -1)
                {
                    instanceProfile = instanceProfile.Substring(pos + 1);
                }

                createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "IamInstanceProfile",
                    Value = instanceProfile
                });
            }

            var serviceRole = this.GetServiceRoleOrCreateIt(this.DeployEnvironmentOptions.ServiceRole, EBDefinedCommandOptions.ARGUMENT_SERVICE_ROLE, 
                "aws-elasticbeanstalk-service-role", Constants.EC2_ASSUME_ROLE_POLICY, null, "AWSElasticBeanstalkWebTier", "AWSElasticBeanstalkMulticontainerDocker", "AWSElasticBeanstalkWorkerTier");
            if (!string.IsNullOrEmpty(serviceRole))
            {
                int pos = serviceRole.LastIndexOf('/');
                if(pos != -1)
                {
                    serviceRole = serviceRole.Substring(pos + 1);
                }

                createRequest.OptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:environment",
                    OptionName = "ServiceRole",
                    Value = serviceRole
                });
            }

            AddAdditionalOptions(createRequest.OptionSettings);

            var tags = ConvertToTagsCollection();
            if (tags != null && tags.Count > 0)
                createRequest.Tags = tags;

            try
            {
                var createResponse = await this.EBClient.CreateEnvironmentAsync(createRequest);
                return createResponse.EnvironmentArn;
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkExceptions("Error creating environment: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedToCreateEnvironment);
            }
        }

        private void AddAdditionalOptions(IList<ConfigurationOptionSetting> settings)
        {
            var additionalOptions = this.GetKeyValuePairOrDefault(this.DeployEnvironmentOptions.AdditionalOptions, EBDefinedCommandOptions.ARGUMENT_EB_ADDITIONAL_OPTIONS, false);
            if (additionalOptions != null && additionalOptions.Count > 0)
            {
                foreach (var kvp in additionalOptions)
                {
                    var tokens = kvp.Key.Split(',');
                    if (tokens.Length != 2)
                    {
                        throw new ToolsException("Additional option \"" + kvp.Key + "=" + kvp.Value + "\" in incorrect format. Format should be <option-namespace>,<option-name>=<option-value>.", ToolsException.CommonErrorCode.DefaultsParseFail);
                    }

                    settings.Add(new ConfigurationOptionSetting
                    {
                        Namespace = tokens[0],
                        OptionName = tokens[1],
                        Value = kvp.Value
                    });
                }
            }

            var enableXRay = this.GetBoolValueOrDefault(this.DeployEnvironmentOptions.EnableXRay, EBDefinedCommandOptions.ARGUMENT_ENABLE_XRAY, false);
            if(enableXRay.HasValue)
            {
                settings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:xray",
                    OptionName = "XRayEnabled",
                    Value = enableXRay.Value.ToString()
                });

                this.Logger?.WriteLine($"Enable AWS X-Ray: {enableXRay.Value}");
            }
        }

        private async Task<string> UpdateEnvironment(string application, string environment, string versionLabel)
        {
            this.Logger?.WriteLine("Updating environment {0} to new application version", environment);
            var updateRequest = new UpdateEnvironmentRequest
            {
                ApplicationName = application,
                EnvironmentName = environment,
                VersionLabel = versionLabel
            };

            AddAdditionalOptions(updateRequest.OptionSettings);

            try
            {
                var updateEnvironmentRespone = await this.EBClient.UpdateEnvironmentAsync(updateRequest);
                return updateEnvironmentRespone.EnvironmentArn;
            }
            catch(Exception e)
            {
                throw new ElasticBeanstalkExceptions("Error updating environment: " + e.Message, ElasticBeanstalkExceptions.EBCode.FailedToUpdateEnvironment);
            }
        }

        private async Task<bool> DoesApplicationExist(string applicationName)
        {
            var request = new DescribeApplicationsRequest();
            request.ApplicationNames.Add(applicationName);
            var response = await this.EBClient.DescribeApplicationsAsync(request);
            return response.Applications.Count == 1;
        }

        private async Task<bool> DoesEnvironmentExist(string applicationName, string environmentName)
        {
            var request = new DescribeEnvironmentsRequest { ApplicationName = applicationName };
            request.EnvironmentNames.Add(environmentName);
            var response = await this.EBClient.DescribeEnvironmentsAsync(request);
            if (response.Environments.Where(x => x.Status != EnvironmentStatus.Terminated && x.Status != EnvironmentStatus.Terminating).Count() != 1)
                return false;

            var environment = response.Environments[0];
            if (environment.Status == EnvironmentStatus.Terminated || environment.Status == EnvironmentStatus.Terminating)
                return false;

            return true;
        }

        private async Task<bool> WaitForDeploymentCompletionAsync(string applicationName, string environmentName, DateTime startingEventDate)
        {
            var requestEnvironment = new DescribeEnvironmentsRequest
            {
                ApplicationName = applicationName,
                EnvironmentNames = new List<string> { environmentName }
            };

            var requestEvents = new DescribeEventsRequest
            {
                ApplicationName = applicationName,
                EnvironmentName = environmentName,
                StartTime = startingEventDate
            };

            var success = true;
            var lastPrintedEventDate = startingEventDate;
            EnvironmentDescription environment = new EnvironmentDescription();
            do
            {
                Thread.Sleep(5000);

                var responseEnvironments = await this.EBClient.DescribeEnvironmentsAsync(requestEnvironment);
                if (responseEnvironments.Environments.Count == 0)
                    throw new ElasticBeanstalkExceptions("Failed to find environment when waiting for deployment completion", ElasticBeanstalkExceptions.EBCode.FailedToFindEnvironment );

                environment = responseEnvironments.Environments[0];

                requestEvents.StartTime = lastPrintedEventDate;
                var responseEvents = await this.EBClient.DescribeEventsAsync(requestEvents);
                if(responseEvents.Events.Count > 0)
                {
                    for(int i = responseEvents.Events.Count - 1; i >= 0; i--)
                    {
                        var evnt = responseEvents.Events[i];
                        if (evnt.EventDate <= lastPrintedEventDate)
                            continue;

                        this.Logger?.WriteLine(evnt.EventDate.ToLocalTime() + "    " + evnt.Severity + "    " + evnt.Message);
                        if(evnt.Message.StartsWith("Failed to deploy application", StringComparison.OrdinalIgnoreCase) ||
                           evnt.Message.StartsWith("Failed to launch environment", StringComparison.OrdinalIgnoreCase) ||
                           evnt.Message.StartsWith("Error occurred during build: Command hooks failed", StringComparison.OrdinalIgnoreCase))
                        {
                            success = false;
                        }
                    }

                    lastPrintedEventDate = responseEvents.Events[0].EventDate;
                }

            } while (environment.Status == EnvironmentStatus.Launching || environment.Status == EnvironmentStatus.Updating);

            if(success)
            {
                this.Logger?.WriteLine("Environment update complete: http://{0}/", environment.EndpointURL);
            }

            return success;
        }

        private async Task<DateTime> GetLatestEventDateAsync(string application, string environment)
        {
            var request = new DescribeEventsRequest
            {
                ApplicationName = application,
                EnvironmentName = environment
            };

            var response = await this.EBClient.DescribeEventsAsync(request);
            if (response.Events.Count == 0)
                return DateTime.Now;

            return response.Events[0].EventDate;
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

        private async Task<S3Location> UploadDeploymentPackageAsync(string application, string versionLabel, string deploymentPackage)
        {
            var bucketName = (await this.EBClient.CreateStorageLocationAsync()).S3Bucket;

            // can't use deploymentPackage directly as vs2008/vs2010 pass different names (vs08 already has version in it),
            // so synthesize one
            string key = string.Format("{0}/AWSDeploymentArchive_{0}_{1}{2}",
                                        application.Replace(' ', '-'),
                                        versionLabel.Replace(' ', '-'),
                                        Path.GetExtension(deploymentPackage));

            var fileInfo = new FileInfo(deploymentPackage);

            if (!(await Utilities.EnsureBucketExistsAsync(this.Logger, this.S3Client, bucketName)))
                throw new ElasticBeanstalkExceptions("Detected error in deployment bucket preparation; abandoning deployment", ElasticBeanstalkExceptions.EBCode.EnsureBucketExistsError );

            this.Logger?.WriteLine("....uploading application deployment package to Amazon S3");
            this.Logger?.WriteLine("......uploading from file path {0}, size {1} bytes", deploymentPackage, fileInfo.Length);

            using (var stream = File.OpenRead(deploymentPackage))
            {
                await Utilities.UploadToS3Async(this.Logger, this.S3Client, bucketName, key, stream);
            }

            return new S3Location() { S3Bucket = bucketName, S3Key = key };
        }


        protected override void SaveConfigFile(JsonData data)
        {
            this.DeployEnvironmentOptions.PersistSettings(this, data);
        }
    }
}
