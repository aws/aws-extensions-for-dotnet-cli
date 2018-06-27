using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Task = System.Threading.Tasks.Task;

using Amazon.ECR.Model;
using Amazon.ECS.Model;
using ThirdParty.Json.LitJson;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ECS.Tools.Commands
{
    public class DeployServiceCommand : ECSBaseDeployCommand
    {
        public const string COMMAND_NAME = "deploy-service";
        public const string COMMAND_DESCRIPTION = "Push the application to ECR and runs the application as a long lived service on the ECS Cluster.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS,
            ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH,

            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP,

            ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER,
            ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE,
            ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT,
            ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT,
            ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT,
            ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS,
            ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY,

            ECSDefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE,
            ECSDefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN,
            ECSDefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT
        },
        TaskDefinitionProperties.CommandOptions);




        DeployServiceProperties _deployServiceProperties;
        public DeployServiceProperties DeployServiceProperties
        {
            get
            {
                if (this._deployServiceProperties == null)
                {
                    this._deployServiceProperties = new DeployServiceProperties();
                }

                return this._deployServiceProperties;
            }
            set { this._deployServiceProperties = value; }
        }



        public bool OverrideIgnoreTargetGroup { get; set; }

        public DeployServiceCommand(IToolLogger logger, string workingDirectory, string[] args)
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

            this.PushDockerImageProperties.ParseCommandArguments(values);
            this.TaskDefinitionProperties.ParseCommandArguments(values);
            this.ClusterProperties.ParseCommandArguments(values);
            this.DeployServiceProperties.ParseCommandArguments(values);


        }

        protected override async Task<bool> PerformActionAsync()
        {

            var skipPush = this.GetBoolValueOrDefault(this.DeployServiceProperties.SkipImagePush, ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
            var ecsContainer = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ContainerName, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, true);
            var ecsTaskDefinition = this.GetStringValueOrDefault(this.TaskDefinitionProperties.TaskDefinitionName, ECSDefinedCommandOptions.ARGUMENT_TD_NAME, true);


            this.PushDockerImageProperties.DockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG, true).ToLower();

            if (!this.PushDockerImageProperties.DockerImageTag.Contains(":"))
                this.PushDockerImageProperties.DockerImageTag += ":latest";

            if(skipPush)
            {
                this.PushDockerImageProperties.DockerImageTag = await ECSUtilities.ExpandImageTagIfNecessary(this.Logger, this.ECRClient, this.PushDockerImageProperties.DockerImageTag);
            }
            else
            {
                var pushCommand = new PushDockerImageCommand(this.Logger, this.WorkingDirectory, this.OriginalCommandLineArguments)
                {
                    ConfigFile = this.ConfigFile,
                    DisableInteractive = this.DisableInteractive,
                    Credentials = this.Credentials,
                    ECRClient = this.ECRClient,
                    Profile = this.Profile,
                    ProfileLocation = this.ProfileLocation,
                    ProjectLocation = this.ProjectLocation,
                    Region = this.Region,
                    WorkingDirectory = this.WorkingDirectory,

                    PushDockerImageProperties = this.PushDockerImageProperties,
                };
                var success = await pushCommand.ExecuteAsync();

                if (!success)
                    return false;

                this.PushDockerImageProperties.DockerImageTag = pushCommand.PushedImageUri;
            }


            var taskDefinitionArn = await ECSUtilities.CreateOrUpdateTaskDefinition(this.Logger, this.ECSClient, 
                this, this.TaskDefinitionProperties, this.PushDockerImageProperties.DockerImageTag, IsFargateLaunch(this.ClusterProperties.LaunchType));

            var ecsCluster = this.GetStringValueOrDefault(this.ClusterProperties.ECSCluster, ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER, true);
            await ECSUtilities.EnsureClusterExistsAsync(this.Logger, this.ECSClient, ecsCluster);

            var ecsService = this.GetStringValueOrDefault(this.DeployServiceProperties.ECSService, ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE, true);

            await CreateOrUpdateService(ecsCluster, ecsService, taskDefinitionArn, ecsContainer);
            this.Logger?.WriteLine($"Service {ecsService} on ECS cluster {ecsCluster} has been updated. The Cluster will now deploy the new service version.");

        
            return true;
        }



        private async Task CreateOrUpdateService(string ecsCluster, string ecsService, string taskDefinitionArn, string ecsContainer)
        {
            try
            { 
                var describeServiceResponse = await this.ECSClient.DescribeServicesAsync(new DescribeServicesRequest
                {
                    Cluster = ecsCluster,
                    Services = new List<string> { ecsService }
                });

                var desiredCount = this.GetIntValueOrDefault(this.DeployServiceProperties.DesiredCount, ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);
                var deploymentMaximumPercent = this.GetIntValueOrDefault(this.DeployServiceProperties.DeploymentMaximumPercent, ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT, false);
                var deploymentMinimumHealthyPercent = this.GetIntValueOrDefault(this.DeployServiceProperties.DeploymentMinimumHealthyPercent, ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT, false);


                var launchType = this.GetStringValueOrDefault(this.ClusterProperties.LaunchType, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE, true);
                NetworkConfiguration networkConfiguration = null;
                if (IsFargateLaunch(this.ClusterProperties.LaunchType))
                {
                    if (describeServiceResponse.Services.Count != 0)
                        networkConfiguration = describeServiceResponse.Services[0].NetworkConfiguration;
                    else
                    {
                        networkConfiguration = new NetworkConfiguration();
                    }

                    await ECSUtilities.SetupAwsVpcNetworkConfigurationAsync(this, networkConfiguration);
                }

                DeploymentConfiguration deploymentConfiguration = null;
                if (deploymentMaximumPercent.HasValue || deploymentMinimumHealthyPercent.HasValue)
                {
                    deploymentConfiguration = new DeploymentConfiguration();
                    if (deploymentMaximumPercent.HasValue)
                        deploymentConfiguration.MaximumPercent = deploymentMaximumPercent.Value;
                    if (deploymentMinimumHealthyPercent.HasValue)
                        deploymentConfiguration.MinimumHealthyPercent = deploymentMinimumHealthyPercent.Value;
                }

                if (describeServiceResponse.Services.Count == 0 || describeServiceResponse.Services[0].Status == "INACTIVE")
                {
                    this.Logger?.WriteLine($"Creating new service: {ecsService}");
                    var request = new CreateServiceRequest
                    {
                        ClientToken = Guid.NewGuid().ToString(),
                        Cluster = ecsCluster,
                        ServiceName = ecsService,
                        TaskDefinition = taskDefinitionArn,
                        DesiredCount = desiredCount.HasValue ? desiredCount.Value : 1,
                        DeploymentConfiguration = deploymentConfiguration,
                        LaunchType = launchType,
                        NetworkConfiguration = networkConfiguration
                    };

                    if(IsFargateLaunch(this.ClusterProperties.LaunchType))
                    {
                        await this.AttemptToCreateServiceLinkRoleAsync();
                    }
                    else
                    {
                        request.PlacementConstraints = ECSUtilities.ConvertPlacementConstraint(this.GetStringValuesOrDefault(this.DeployServiceProperties.PlacementConstraints, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS, false));
                        request.PlacementStrategy = ECSUtilities.ConvertPlacementStrategy(this.GetStringValuesOrDefault(this.DeployServiceProperties.PlacementStrategy, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY, false));
                    }

                    var elbTargetGroup = this.GetStringValueOrDefault(this.DeployServiceProperties.ELBTargetGroup, ECSDefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN, false);
                    if (!this.OverrideIgnoreTargetGroup && !string.IsNullOrWhiteSpace(elbTargetGroup))
                    {
                        if(!IsFargateLaunch(this.ClusterProperties.LaunchType))
                        {
                            request.Role = this.GetStringValueOrDefault(this.DeployServiceProperties.ELBServiceRole, ECSDefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE, false);
                        }

                        var port = this.GetIntValueOrDefault(this.DeployServiceProperties.ELBContainerPort, ECSDefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT, false);
                        if (!port.HasValue)
                            port = 80;
                        request.LoadBalancers.Add(new LoadBalancer
                        {
                            TargetGroupArn = elbTargetGroup,
                            ContainerName = ecsContainer,
                            ContainerPort = port.Value
                        });
                    }

                    try
                    {
                        await this.ECSClient.CreateServiceAsync(request);
                    }
                    catch(Amazon.ECS.Model.InvalidParameterException e)
                    {
                        if (e.Message.StartsWith("The target group") && !string.IsNullOrEmpty(elbTargetGroup) && string.IsNullOrEmpty(this.DeployServiceProperties.ELBTargetGroup))
                        {
                            request.LoadBalancers.Clear();
                            request.Role = null;

                            var defaultFile = string.IsNullOrEmpty(this.ConfigFile) ? ECSToolsDefaults.DEFAULT_FILE_NAME : this.ConfigFile;
                            this.Logger?.WriteLine($"Warning: ELB Target Group ARN specified in config file {defaultFile} does not exist.");
                            await this.ECSClient.CreateServiceAsync(request);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                else
                {
                    this.Logger?.WriteLine($"Updating new service: {ecsService}");
                    var updateRequest = new UpdateServiceRequest
                    {
                        Cluster = ecsCluster,
                        Service = ecsService,
                        TaskDefinition = taskDefinitionArn,
                        DeploymentConfiguration = deploymentConfiguration,
                        NetworkConfiguration = networkConfiguration
                    };

                    if(desiredCount.HasValue)
                    {
                        updateRequest.DesiredCount = desiredCount.Value;
                    }

                    await this.ECSClient.UpdateServiceAsync(updateRequest);
                }
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error updating ECS service {ecsService} on cluster {ecsCluster}: {e.Message}", DockerToolsException.ECSErrorCode.FailedToUpdateService);
            }
        }

        protected override void SaveConfigFile(JsonData data)
        {
            PushDockerImageProperties.PersistSettings(this, data);
            ClusterProperties.PersistSettings(this, data);
            TaskDefinitionProperties.PersistSettings(this, data);
            DeployServiceProperties.PersistSettings(this, data);
        }
    }
}
