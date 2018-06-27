using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Common.DotNetCli.Tools.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ECS.Tools.Commands
{
    public class PushDockerImageProperties
    {
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string PublishOptions { get; set; }
        public string DockerImageTag { get; set; }
        public string DockerBuildWorkingDirectory { get; set; }
        public string DockerBuildOptions { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.Switch)) != null)
                this.PublishOptions = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG.Switch)) != null)
                this.DockerImageTag = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.Switch)) != null)
                this.DockerBuildWorkingDirectory = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.Switch)) != null)
                this.DockerBuildOptions = tuple.Item2.StringValue;
        }


        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, command.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, command.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.ConfigFileKey, command.GetStringValueOrDefault(this.PublishOptions, CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS, false));


            var tag = command.GetStringValueOrDefault(this.DockerImageTag, ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG, false);
            if (!string.IsNullOrEmpty(tag))
            {
                // Strip the full ECR URL name.
                int pos = tag.LastIndexOf('/');
                if (pos != -1)
                {
                    tag = tag.Substring(pos + 1);
                }

                data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_DOCKER_TAG.ConfigFileKey, tag);
            }

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY.ConfigFileKey, command.GetStringValueOrDefault(this.DockerBuildWorkingDirectory, ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS.ConfigFileKey, command.GetStringValueOrDefault(this.DockerBuildOptions, ECSDefinedCommandOptions.ARGUMENT_DOCKER_BUILD_OPTIONS, false));
        }
    }


    public class ClusterProperties
    {
        public string ECSCluster { get; set; }
        public string LaunchType { get; set; }
        public string[] SubnetIds { get; set; }
        public string[] SecurityGroupIds { get; set; }
        public bool? AssignPublicIpAddress { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER.Switch)) != null)
                this.ECSCluster = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE.Switch)) != null)
                this.LaunchType = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS.Switch)) != null)
                this.SubnetIds = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS.Switch)) != null)
                this.SecurityGroupIds = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP.Switch)) != null)
                this.AssignPublicIpAddress = tuple.Item2.BoolValue;
        }

        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER.ConfigFileKey, command.GetStringValueOrDefault(this.ECSCluster, ECSDefinedCommandOptions.ARGUMENT_ECS_CLUSTER, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE.ConfigFileKey, command.GetStringValueOrDefault(this.LaunchType, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.SubnetIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SUBNETS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.SecurityGroupIds, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_SECURITYGROUPS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP.ConfigFileKey, command.GetBoolValueOrDefault(this.AssignPublicIpAddress, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP, false));
        }
    }


    public class TaskDefinitionProperties
    {
        public static IList<CommandOption> CommandOptions = new List<CommandOption>
        {
            ECSDefinedCommandOptions.ARGUMENT_TD_NAME,
            ECSDefinedCommandOptions.ARGUMENT_TD_NETWORK_MODE,
            ECSDefinedCommandOptions.ARGUMENT_TD_ROLE,
            ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE,
            ECSDefinedCommandOptions.ARGUMENT_TD_CPU,
            ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY,
            ECSDefinedCommandOptions.ARGUMENT_TD_VOLUMES,

            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_COMMANDS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_CPU,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DISABLE_NETWORKING,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SERVERS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_LABELS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENTRY_POINT,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ESSENTIAL,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_EXTRA_HOSTS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_HOSTNAME,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINKS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINUX_PARAMETERS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LOG_CONFIGURATION,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MOUNT_POINTS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PRIVILEGED,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_READONLY_ROOT_FILESYSTEM,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ULIMITS,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_USER,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_VOLUMES_FROM,
            ECSDefinedCommandOptions.ARGUMENT_CONTAINER_WORKING_DIRECTORY,
        };

        public string ContainerName { get; set; }
        public int? ContainerMemoryHardLimit { get; set; }
        public int? ContainerMemorySoftLimit { get; set; }
        public string[] ContainerCommands { get; set; }
        public bool? ContainerDisableNetworking { get; set; }
        public string[] ContainerDNSSearchDomains { get; set; }
        public string[] ContainerDNSServers { get; set; }
        public Dictionary<string, string> ContainerDockerLabels { get; set; }
        public string[] ContainerDockerSecurityOptions { get; set; }
        public string[] ContainerEntryPoint { get; set; }
        public Dictionary<string, string> ContainerEnvironmentVariables { get; set; }
        public bool? ContainerEssential { get; set; }
        public string ContainerExtraHosts { get; set; }
        public string ContainerHostname { get; set; }
        public string[] ContainerLinks { get; set; }
        public string ContainerLinuxParameters { get; set; }
        public string ContainerLogConfiguration { get; set; }
        public string ContainerMountPoints { get; set; }
        public string[] ContainerPortMappings { get; set; }
        public bool? ContainerPrivileged { get; set; }
        public bool? ContainerReadonlyRootFilesystem { get; set; }
        public string ContainerUlimits { get; set; }
        public string ContainerUser { get; set; }
        public string ContainerVolumesFrom { get; set; }
        public string ContainerWorkingDirectory { get; set; }


        public string TaskDefinitionName { get; set; }
        public string TaskDefinitionNetworkMode { get; set; }
        public string TaskDefinitionRole { get; set; }
        public string TaskDefinitionExecutionRole { get; set; }
        public string TaskCPU { get; set; }
        public string TaskMemory { get; set; }
        public string TaskDefinitionVolumes { get; set; }


        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_NAME.Switch)) != null)
                this.TaskDefinitionName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_NETWORK_MODE.Switch)) != null)
                this.TaskDefinitionNetworkMode = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_ROLE.Switch)) != null)
                this.TaskDefinitionRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE.Switch)) != null)
                this.TaskDefinitionExecutionRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_CPU.Switch)) != null)
                this.TaskCPU = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY.Switch)) != null)
                this.TaskMemory = tuple.Item2.StringValue;

            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_COMMANDS.Switch)) != null)
                this.ContainerCommands = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DISABLE_NETWORKING.Switch)) != null)
                this.ContainerDisableNetworking = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS.Switch)) != null)
                this.ContainerDNSSearchDomains = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SERVERS.Switch)) != null)
                this.ContainerDNSServers = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_LABELS.Switch)) != null)
                this.ContainerDockerLabels = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS.Switch)) != null)
                this.ContainerDockerSecurityOptions = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENTRY_POINT.Switch)) != null)
                this.ContainerEntryPoint = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES.Switch)) != null)
                this.ContainerEnvironmentVariables = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ESSENTIAL.Switch)) != null)
                this.ContainerEssential = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_EXTRA_HOSTS.Switch)) != null)
                this.ContainerExtraHosts = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_HOSTNAME.Switch)) != null)
                this.ContainerHostname = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINKS.Switch)) != null)
                this.ContainerLinks = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINUX_PARAMETERS.Switch)) != null)
                this.ContainerLinuxParameters = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LOG_CONFIGURATION.Switch)) != null)
                this.ContainerLogConfiguration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MOUNT_POINTS.Switch)) != null)
                this.ContainerMountPoints = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING.Switch)) != null)
                this.ContainerPortMappings = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PRIVILEGED.Switch)) != null)
                this.ContainerPrivileged = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ULIMITS.Switch)) != null)
                this.ContainerUlimits = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_USER.Switch)) != null)
                this.ContainerUser = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_VOLUMES_FROM.Switch)) != null)
                this.ContainerVolumesFrom = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_WORKING_DIRECTORY.Switch)) != null)
                this.ContainerWorkingDirectory = tuple.Item2.StringValue;


            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME.Switch)) != null)
                this.ContainerName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT.Switch)) != null)
                this.ContainerMemoryHardLimit = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT.Switch)) != null)
                this.ContainerMemorySoftLimit = tuple.Item2.IntValue;
        }

        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_NAME.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionName, ECSDefinedCommandOptions.ARGUMENT_TD_NAME, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_NETWORK_MODE.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionNetworkMode, ECSDefinedCommandOptions.ARGUMENT_TD_NETWORK_MODE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_CPU.ConfigFileKey, command.GetStringValueOrDefault(this.TaskCPU, ECSDefinedCommandOptions.ARGUMENT_TD_CPU, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY.ConfigFileKey, command.GetStringValueOrDefault(this.TaskMemory, ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionRole, ECSDefinedCommandOptions.ARGUMENT_TD_ROLE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionExecutionRole, ECSDefinedCommandOptions.ARGUMENT_TD_EXECUTION_ROLE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_TD_VOLUMES.ConfigFileKey, command.GetStringValueOrDefault(this.TaskDefinitionVolumes, ECSDefinedCommandOptions.ARGUMENT_TD_VOLUMES, false));

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_COMMANDS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerCommands, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_COMMANDS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DISABLE_NETWORKING.ConfigFileKey, command.GetBoolValueOrDefault(this.ContainerDisableNetworking, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DISABLE_NETWORKING, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerDNSSearchDomains, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SERVERS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerDNSServers, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DNS_SERVERS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_LABELS.ConfigFileKey, ECSToolsDefaults.FormatKeyValue(command.GetKeyValuePairOrDefault(this.ContainerDockerLabels, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_LABELS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerDockerSecurityOptions, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENTRY_POINT.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerEntryPoint, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENTRY_POINT, false)));

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ESSENTIAL.ConfigFileKey, command.GetBoolValueOrDefault(this.ContainerEssential, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ESSENTIAL, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_EXTRA_HOSTS.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerExtraHosts, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_EXTRA_HOSTS, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_HOSTNAME.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerHostname, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_HOSTNAME, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINKS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerLinks, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINKS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINUX_PARAMETERS.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerLinuxParameters, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LINUX_PARAMETERS, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LOG_CONFIGURATION.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerLogConfiguration, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_LOG_CONFIGURATION, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MOUNT_POINTS.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerMountPoints, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MOUNT_POINTS, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PRIVILEGED.ConfigFileKey, command.GetBoolValueOrDefault(this.ContainerPrivileged, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PRIVILEGED, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_READONLY_ROOT_FILESYSTEM.ConfigFileKey, command.GetBoolValueOrDefault(this.ContainerReadonlyRootFilesystem, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_READONLY_ROOT_FILESYSTEM, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ULIMITS.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerUlimits, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ULIMITS, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_USER.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerUlimits, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_USER, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_VOLUMES_FROM.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerVolumesFrom, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_VOLUMES_FROM, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_WORKING_DIRECTORY.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerWorkingDirectory, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_WORKING_DIRECTORY, false));




            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME.ConfigFileKey, command.GetStringValueOrDefault(this.ContainerName, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_NAME, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT.ConfigFileKey, command.GetIntValueOrDefault(this.ContainerMemoryHardLimit, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT.ConfigFileKey, command.GetIntValueOrDefault(this.ContainerMemorySoftLimit, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.ContainerPortMappings, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_PORT_MAPPING, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES.ConfigFileKey, ECSToolsDefaults.FormatKeyValue(command.GetKeyValuePairOrDefault(this.ContainerEnvironmentVariables, ECSDefinedCommandOptions.ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES, false)));
        }
    }



    public class DeployServiceProperties
    {
        public bool SkipImagePush { get; set; }
        public string ECSService { get; set; }
        public int? DesiredCount { get; set; }
        public string[] PlacementConstraints { get; set; }
        public string[] PlacementStrategy { get; set; }

        public int? DeploymentMinimumHealthyPercent { get; set; }
        public int? DeploymentMaximumPercent { get; set; }


        public string ELBServiceRole { get; set; }
        public string ELBTargetGroup { get; set; }
        public int? ELBContainerPort { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH.Switch)) != null)
                this.SkipImagePush = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE.Switch)) != null)
                this.ECSService = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.Switch)) != null)
                this.DesiredCount = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT.Switch)) != null)
                this.DeploymentMinimumHealthyPercent = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT.Switch)) != null)
                this.DeploymentMaximumPercent = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE.Switch)) != null)
                this.ELBServiceRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN.Switch)) != null)
                this.ELBTargetGroup = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT.Switch)) != null)
                this.ELBContainerPort = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS.Switch)) != null)
                this.PlacementConstraints = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY.Switch)) != null)
                this.PlacementStrategy = tuple.Item2.StringValues;
        }

        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH.ConfigFileKey, command.GetBoolValueOrDefault(this.SkipImagePush, ECSDefinedCommandOptions.ARGUMENT_SKIP_IMAGE_PUSH, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE.ConfigFileKey, command.GetStringValueOrDefault(this.ECSService, ECSDefinedCommandOptions.ARGUMENT_ECS_SERVICE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.ConfigFileKey, command.GetIntValueOrDefault(this.DesiredCount, ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT.ConfigFileKey, command.GetIntValueOrDefault(this.DeploymentMinimumHealthyPercent, ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT.ConfigFileKey, command.GetIntValueOrDefault(this.DeploymentMaximumPercent, ECSDefinedCommandOptions.ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT, false));

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.ELBServiceRole, ECSDefinedCommandOptions.ARGUMENT_ELB_SERVICE_ROLE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN.ConfigFileKey, command.GetStringValueOrDefault(this.ELBTargetGroup, ECSDefinedCommandOptions.ARGUMENT_ELB_TARGET_GROUP_ARN, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT.ConfigFileKey, command.GetIntValueOrDefault(this.ELBContainerPort, ECSDefinedCommandOptions.ARGUMENT_ELB_CONTAINER_PORT, false));

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.PlacementConstraints, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.PlacementStrategy, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY, false)));
        }
    }

    public class DeployTaskProperties
    {
        public bool SkipImagePush { get; set; }

        public string TaskGroup { get; set; }
        public int? TaskCount { get; set; }
        public string[] PlacementConstraints { get; set; }
        public string[] PlacementStrategy { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT.Switch)) != null)
                this.TaskCount = tuple.Item2.IntValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS.Switch)) != null)
                this.PlacementConstraints = tuple.Item2.StringValues;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY.Switch)) != null)
                this.PlacementStrategy = tuple.Item2.StringValues;
        }

        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP.ConfigFileKey, command.GetStringValueOrDefault(this.TaskGroup, ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_GROUP, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT.ConfigFileKey, command.GetIntValueOrDefault(this.TaskCount, ECSDefinedCommandOptions.ARGUMENT_ECS_TASK_COUNT, false));

            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.PlacementConstraints, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_CONSTRAINTS, false)));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY.ConfigFileKey, ECSToolsDefaults.FormatCommaDelimitedList(command.GetStringValuesOrDefault(this.PlacementStrategy, ECSDefinedCommandOptions.ARGUMENT_PLACEMENT_STRATEGY, false)));

        }
    }

    public class DeployScheduledTaskProperties
    {
        public bool SkipImagePush { get; set; }
        public string ScheduleTaskRule { get; set; }
        public string ScheduleTaskRuleTarget { get; set; }
        public string ScheduleExpression { get; set; }
        public string CloudWatchEventIAMRole { get; set; }
        public int? DesiredCount { get; set; }

        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME.Switch)) != null)
                this.ScheduleTaskRule = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET.Switch)) != null)
                this.ScheduleTaskRuleTarget = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION.Switch)) != null)
                this.ScheduleExpression = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE.Switch)) != null)
                this.CloudWatchEventIAMRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.Switch)) != null)
                this.DesiredCount = tuple.Item2.IntValue;
        }

        internal void PersistSettings(ECSBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleTaskRule, ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_NAME, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleTaskRuleTarget, ECSDefinedCommandOptions.ARGUMENT_SCHEDULED_RULE_TARGET, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION.ConfigFileKey, command.GetStringValueOrDefault(this.ScheduleExpression, ECSDefinedCommandOptions.ARGUMENT_SCHEDULE_EXPRESSION, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.CloudWatchEventIAMRole, ECSDefinedCommandOptions.ARGUMENT_CLOUDWATCHEVENT_ROLE, false));
            data.SetIfNotNull(ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT.ConfigFileKey, command.GetIntValueOrDefault(this.DesiredCount, ECSDefinedCommandOptions.ARGUMENT_ECS_DESIRED_COUNT, false));
        }
    }
}
