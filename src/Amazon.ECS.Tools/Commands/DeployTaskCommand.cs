using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ECS.Tools.Commands
{
    public class DeployTaskCommand : ECSBaseCommand
    {
        public const string COMMAND_NAME = "deploy-task";
        public const string COMMAND_DESCRIPTION = "Push the application to ECR and then runs it as a task on the ECS Cluster.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,

            ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG,

            ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH,

            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS,
            ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP,


            ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT,
            ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP,
            ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS,
            ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY,


            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
        },
        TaskDefinitionProperties.CommandOptions);

        PushDockerImageProperties _pushProperties;
        public PushDockerImageProperties PushDockerImageProperties
        {
            get
            {
                if (this._pushProperties == null)
                {
                    this._pushProperties = new PushDockerImageProperties();
                }

                return this._pushProperties;
            }
            set { this._pushProperties = value; }
        }

        TaskDefinitionProperties _taskDefinitionProperties;
        public TaskDefinitionProperties TaskDefinitionProperties
        {
            get
            {
                if (this._taskDefinitionProperties == null)
                {
                    this._taskDefinitionProperties = new TaskDefinitionProperties();
                }

                return this._taskDefinitionProperties;
            }
            set { this._taskDefinitionProperties = value; }
        }

        ClusterProperties _clusterProperties;
        public ClusterProperties ClusterProperties
        {
            get
            {
                if (this._clusterProperties == null)
                {
                    this._clusterProperties = new ClusterProperties();
                }

                return this._clusterProperties;
            }
            set { this._clusterProperties = value; }
        }

        DeployTaskProperties _deployTaskProperties;
        public DeployTaskProperties DeployTaskProperties
        {
            get
            {
                if (this._deployTaskProperties == null)
                {
                    this._deployTaskProperties = new DeployTaskProperties();
                }

                return this._deployTaskProperties;
            }
            set { this._deployTaskProperties = value; }
        }

        public bool? PersistConfigFile { get; set; }

        public DeployTaskCommand(IToolLogger logger, string workingDirectory, string[] args)
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
            this.DeployTaskProperties.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;
        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var skipPush = this.GetBoolValueOrDefault(this.DeployTaskProperties.SkipImagePush, ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
                var ecsContainer = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ContainerName, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, true);
                var ecsTaskDefinition = this.GetStringValueOrDefault(this.TaskDefinitionProperties.TaskDefinitionName, ECSDefinedCommandOptions.ARGUMENT_TD_NAME, true);


                string dockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG, true);

                if (!dockerImageTag.Contains(":"))
                    dockerImageTag += ":latest";

                if (skipPush)
                {
                    dockerImageTag = await ECSUtilities.ExpandImageTagIfNecessary(this.Logger, this.ECRClient, dockerImageTag);
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

                    dockerImageTag = pushCommand.PushedImageUri;
                }

                var taskDefinitionArn = await ECSTaskDefinitionUtilities.CreateOrUpdateTaskDefinition(this.Logger, this.ECSClient,
                    this, this.TaskDefinitionProperties, dockerImageTag, IsFargateLaunch(this.ClusterProperties.LaunchType));

                var ecsCluster = this.GetStringValueOrDefault(this.ClusterProperties.ECSCluster, ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER, true);

                var taskCount = this.GetIntValueOrDefault(this.DeployTaskProperties.TaskCount, ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT, false);
                if (!taskCount.HasValue)
                    taskCount = 1;

                var taskGroup = this.GetStringValueOrDefault(this.DeployTaskProperties.TaskGroup, ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP, false);
                var launchType = this.GetStringValueOrDefault(this.ClusterProperties.LaunchType, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE, true);

                var runTaskRequest = new Amazon.ECS.Model.RunTaskRequest
                {
                    Cluster = ecsCluster,
                    TaskDefinition = taskDefinitionArn,
                    Count = taskCount.Value,
                    LaunchType = launchType
                };

                Amazon.ECS.Model.NetworkConfiguration networkConfiguration = null;
                if (IsFargateLaunch(this.ClusterProperties.LaunchType))
                {
                    var subnets = this.GetStringValuesOrDefault(this.ClusterProperties.SubnetIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, false);
                    var securityGroups = this.GetStringValuesOrDefault(this.ClusterProperties.SecurityGroupIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, false);

                    networkConfiguration = new Amazon.ECS.Model.NetworkConfiguration();
                    networkConfiguration.AwsvpcConfiguration = new Amazon.ECS.Model.AwsVpcConfiguration
                    {
                        SecurityGroups = new List<string>(securityGroups),
                        Subnets = new List<string>(subnets)
                    };

                    var assignPublicIp = this.GetBoolValueOrDefault(this.ClusterProperties.AssignPublicIpAddress, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, false);
                    if (assignPublicIp.HasValue)
                    {
                        networkConfiguration.AwsvpcConfiguration.AssignPublicIp = assignPublicIp.Value ? AssignPublicIp.ENABLED : AssignPublicIp.DISABLED;
                    }

                    runTaskRequest.NetworkConfiguration = networkConfiguration;

                    await this.AttemptToCreateServiceLinkRoleAsync();
                }
                else
                {
                    runTaskRequest.PlacementConstraints = ECSUtilities.ConvertPlacementConstraint(this.GetStringValuesOrDefault(this.DeployTaskProperties.PlacementConstraints, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS, false));
                    runTaskRequest.PlacementStrategy = ECSUtilities.ConvertPlacementStrategy(this.GetStringValuesOrDefault(this.DeployTaskProperties.PlacementStrategy, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY, false));
                }

                if (!string.IsNullOrEmpty(taskGroup))
                    runTaskRequest.Group = taskGroup;


                try
                {
                    var response = await this.ECSClient.RunTaskAsync(runTaskRequest);
                    this.Logger?.WriteLine($"Started {response.Tasks.Count} task:");
                    foreach(var task in response.Tasks)
                    {
                        this.Logger?.WriteLine($"\t{task.TaskArn}");
                    }
                }
                catch(Exception e)
                {
                    throw new DockerToolsException("Error executing deploy-task: " + e.Message, DockerToolsException.ECSErrorCode.RunTaskFail);
                }

                if (this.GetBoolValueOrDefault(this.PersistConfigFile, CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE, false).GetValueOrDefault())
                {
                    this.SaveConfigFile();
                }
            }
            catch (DockerToolsException e)
            {
                this.Logger?.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger?.WriteLine($"Unknown error executing deploy-task: {e.Message}");
                this.Logger?.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.PushDockerImageProperties.PersistSettings(this, data);
            this.TaskDefinitionProperties.PersistSettings(this, data);
            this.ClusterProperties.PersistSettings(this, data);
            this.DeployTaskProperties.PersistSettings(this, data);
        }

    }
}
