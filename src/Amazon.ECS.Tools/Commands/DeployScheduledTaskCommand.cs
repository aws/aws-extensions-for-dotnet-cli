using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.ECS;
using Amazon.ECS.Model;
using ThirdParty.Json.LitJson;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ECS.Tools.Commands
{
    public class DeployScheduledTaskCommand : ECSBaseDeployCommand
    {
        public const string COMMAND_NAME = "deploy-scheduled-task";
        public const string COMMAND_DESCRIPTION = "Push the application to ECR and then sets up CloudWatch Event Schedule rule to run the application.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION,
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY,
            ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS,

            ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH,

            ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME,
            ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET,
            ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION,
            ECSDefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE,

            ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT
        },
        TaskDefinitionProperties.CommandOptions);


        DeployScheduledTaskProperties _deployScheduledTaskProperties;
        public DeployScheduledTaskProperties DeployScheduledTaskProperties
        {
            get
            {
                if (this._deployScheduledTaskProperties == null)
                {
                    this._deployScheduledTaskProperties = new DeployScheduledTaskProperties();
                }

                return this._deployScheduledTaskProperties;
            }
            set { this._deployScheduledTaskProperties = value; }
        }

        public DeployScheduledTaskCommand(IToolLogger logger, string workingDirectory, string[] args)
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
            this.DeployScheduledTaskProperties.ParseCommandArguments(values);
        }

        protected override async Task<bool> PerformActionAsync()
        {
            var skipPush = this.GetBoolValueOrDefault(this.DeployScheduledTaskProperties.SkipImagePush, ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false).GetValueOrDefault();
            var ecsContainer = this.GetStringValueOrDefault(this.TaskDefinitionProperties.ContainerName, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, true);
            var ecsTaskDefinition = this.GetStringValueOrDefault(this.TaskDefinitionProperties.TaskDefinitionName, ECSDefinedCommandOptions.ARGUMENT_TD_NAME, true);


            this.PushDockerImageProperties.DockerImageTag = this.GetStringValueOrDefault(this.PushDockerImageProperties.DockerImageTag, ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG, true).ToLower();

            if (!this.PushDockerImageProperties.DockerImageTag.Contains(":"))
                this.PushDockerImageProperties.DockerImageTag += ":latest";

            if (skipPush)
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

            if (!ecsCluster.Contains(":"))
            {
                var arnPrefix = taskDefinitionArn.Substring(0, taskDefinitionArn.LastIndexOf(":task"));
                ecsCluster = arnPrefix + ":cluster/" + ecsCluster;
            }

            var ruleName = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleTaskRule, ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME, true);
            var targetName = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleTaskRuleTarget, ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET, false);
            if (string.IsNullOrEmpty(targetName))
                targetName = ruleName;

            var scheduleExpression = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.ScheduleExpression, ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION, true);
            var cweRole = this.GetStringValueOrDefault(this.DeployScheduledTaskProperties.CloudWatchEventIAMRole, ECSDefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE, true);
            var desiredCount = this.GetIntValueOrDefault(this.DeployScheduledTaskProperties.DesiredCount, ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false);
            if (!desiredCount.HasValue)
                desiredCount = 1;

            string ruleArn = null;
            try
            {
                ruleArn = (await this.CWEClient.PutRuleAsync(new PutRuleRequest
                {
                    Name = ruleName,
                    ScheduleExpression = scheduleExpression,
                    State = RuleState.ENABLED
                })).RuleArn;

                this.Logger?.WriteLine($"Put CloudWatch Event rule {ruleName} with expression {scheduleExpression}");
            }
            catch(Exception e)
            {
                throw new DockerToolsException("Error creating CloudWatch Event rule: " + e.Message, DockerToolsException.ECSErrorCode.PutRuleFail);
            }

            try
            {
                await this.CWEClient.PutTargetsAsync(new PutTargetsRequest
                {
                    Rule = ruleName,
                    Targets = new List<Target>
                    {
                        new Target
                        {
                            Arn = ecsCluster,
                            RoleArn = cweRole,
                            Id = targetName,
                            EcsParameters = new EcsParameters
                            {
                                TaskCount = desiredCount.Value,
                                TaskDefinitionArn = taskDefinitionArn
                            }
                        }
                    }
                });
                this.Logger?.WriteLine($"Put CloudWatch Event target {targetName}");
            }
            catch (Exception e)
            {
                throw new DockerToolsException("Error creating CloudWatch Event target: " + e.Message, DockerToolsException.ECSErrorCode.PutTargetFail);
            }

            return true;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            this.PushDockerImageProperties.PersistSettings(this, data);
            this.TaskDefinitionProperties.PersistSettings(this, data);
            this.ClusterProperties.PersistSettings(this, data);
            this.DeployScheduledTaskProperties.PersistSettings(this, data);
        }
    }
}
