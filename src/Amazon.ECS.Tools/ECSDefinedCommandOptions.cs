using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// This class defines all the possible options across all the commands. The individual commands will then
    /// references the options that are appropiate.
    /// </summary>
    public static class ECSDefinedCommandOptions
    {

        public static readonly CommandOption ARGUMENT_DOCKER_TAG =
            new CommandOption
            {
                Name = "Docker Image Tag",
                ShortSwitch = "-t",
                Switch = "--tag",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name and optionally a tag in the 'name:tag' format.",
            };
        public static readonly CommandOption ARGUMENT_DOCKER_BUILD_WORKING_DIRECTORY =
            new CommandOption
            {
                Name = "Docker Build Working Directory",
                ShortSwitch = "-dbwd",
                Switch = "--docker-build-working-dir",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The directory to execute the \"docker build\" command from.",
            };
        public static readonly CommandOption ARGUMENT_DOCKER_BUILD_OPTIONS =
            new CommandOption
            {
                Name = "Docker Build Options",
                ShortSwitch = "-dbo",
                Switch = "--docker-build-options",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Additional options passed to the \"docker build\" command.",
            };
        public static readonly CommandOption ARGUMENT_SKIP_IMAGE_PUSH =
            new CommandOption
            {
                Name = "Skip Image Push",
                ShortSwitch = "-sip",
                Switch = "--skip-image-push",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Skip building and push an image to Amazon ECR.",
            };

        public static readonly CommandOption ARGUMENT_ECS_CLUSTER =
            new CommandOption
            {
                Name = "Cluster Name",
                ShortSwitch = "-ec",
                Switch = "--cluster",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Cluster to run the docker image.",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_TYPE =
            new CommandOption
            {
                Name = "Launch Type",
                ShortSwitch = "-lt",
                Switch = "--launch-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The launch type on which to run tasks. Valid values EC2 | FARGATE.",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_SUBNETS =
            new CommandOption
            {
                Name = "Launch VPC Subnets",
                ShortSwitch = "-ls",
                Switch = "--launch-subnets",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of subnet ids used when launch type is FARGATE",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_ASSIGN_PUBLIC_IP =
            new CommandOption
            {
                Name = "Assign Public IP Address",
                Switch = "--assign-public-ip",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true a public IP address is assigned to the task when launch type is FARGATE",
            };

        public static readonly CommandOption ARGUMENT_LAUNCH_SECURITYGROUPS =
            new CommandOption
            {
                Name = "Launch Type",
                ShortSwitch = "-lsg",
                Switch = "--launch-security-groups",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of security group ids used when launch type is FARGATE",
            };

        public static readonly CommandOption ARGUMENT_ECS_SERVICE =
            new CommandOption
            {
                Name = "Service Name",
                ShortSwitch = "-cs",
                Switch = "--cluster-service",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the service to run on the ECS Cluster.",
            };

        public static readonly CommandOption ARGUMENT_ECS_DESIRED_COUNT =
            new CommandOption
            {
                Name = "Desired Count",
                ShortSwitch = "-dc",
                Switch = "--desired-count",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of instantiations of the task to place and keep running in your service. Default is 1.",
            };

        public static readonly CommandOption ARGUMENT_ECS_TASK_COUNT =
            new CommandOption
            {
                Name = "Task Count",
                ShortSwitch = "-tc",
                Switch = "--task-count",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of instantiations of the task to place and keep running in your service. Default is 1.",
            };


        public static readonly CommandOption ARGUMENT_TD_EXECUTION_ROLE =
            new CommandOption
            {
                Name = "Task Definition Execution Role",
                Switch = "--task-execution-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role ECS assumes to pull images from ECR and publish logs to CloudWatch Logs. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_TD_CPU =
            new CommandOption
            {
                Name = "Task Definition Allocated CPU",
                Switch = "--task-cpu",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The amount of cpu to allocate for the task definition. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_TD_MEMORY =
            new CommandOption
            {
                Name = "Task Definition Allocated Memory",
                Switch = "--task-memory",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The amount of memory to allocated for the task definition. Fargate only."
            };
        public static readonly CommandOption ARGUMENT_ELB_TARGET_GROUP_ARN =
            new CommandOption
            {
                Name = "ELB Target ARN",
                ShortSwitch = "-etg",
                Switch = "--elb-target-group",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The full Amazon Resource Name (ARN) of the Elastic Load Balancing target group associated with a service. "
            };
        public static readonly CommandOption ARGUMENT_ELB_CONTAINER_PORT =
            new CommandOption
            {
                Name = "ELB Container Port",
                ShortSwitch = "-ecp",
                Switch = "--elb-container-port",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The port on the container to associate with the load balancer."
            };
        public static readonly CommandOption ARGUMENT_DEPLOYMENT_MAXIMUM_PERCENT =
            new CommandOption
            {
                Name = "Deployment Maximum Percent",
                Switch = "--deployment-maximum-percent",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The upper limit of the number of tasks that are allowed in the RUNNING or PENDING state in a service during a deployment."
            };
        public static readonly CommandOption ARGUMENT_DEPLOYMENT_MINIMUM_HEALTHY_PERCENT =
            new CommandOption
            {
                Name = "Deployment Minimum Healhy Percent",
                Switch = "--deployment-minimum-healthy-percent",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The lower limit of the number of running tasks that must remain in the RUNNING state in a service during a deployment."
            };
        public static readonly CommandOption ARGUMENT_ELB_SERVICE_ROLE =
            new CommandOption
            {
                Name = "ELB Service Role",
                Switch = "--elb-service-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name or (ARN) of the IAM role that allows ECS to make calls to the load balancer."
            };

        public static readonly CommandOption ARGUMENT_SCHEDULED_RULE_NAME =
            new CommandOption
            {
                Name = "Scheduled Rule",
                Switch = "--rule",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the CloudWatch Event Schedule rule."
            };
        public static readonly CommandOption ARGUMENT_SCHEDULED_RULE_TARGET =
            new CommandOption
            {
                Name = "Schedule Rule Target",
                Switch = "--rule-target",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the target that will be assigned to the rule and point to the ECS task definition."
            };
        public static readonly CommandOption ARGUMENT_SCHEDULE_EXPRESSION =
            new CommandOption
            {
                Name = "Schedule Expression",
                Switch = "--schedule-expression",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The scheduling expression. For example, \"cron(0 20 * * ? *)\" or \"rate(5 minutes)\"."
            };
        public static readonly CommandOption ARGUMENT_CLOUDWATCHEVENT_ROLE =
            new CommandOption
            {
                Name = "CloudWatch Event IAM Role",
                Switch = "--cloudwatch-event-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The role that IAM will assume to invoke the target."
            };
        public static readonly CommandOption ARGUMENT_ECS_TASK_GROUP =
            new CommandOption
            {
                Name = "Task Group",
                Switch = "--task-group",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The task group to associate with the task. The default value is the family name of the task definition."
            };


        public static readonly CommandOption ARGUMENT_PLACEMENT_CONSTRAINTS =
            new CommandOption
            {
                Name = "Placement Constraints",
                Switch = "--placement-constraints",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Placement constraint to use for tasks in service. Format is <type>=<optional expression>,...",
            };

        public static readonly CommandOption ARGUMENT_PLACEMENT_STRATEGY =
            new CommandOption
            {
                Name = "Placement Strategy",
                Switch = "--placement-strategy",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Placement strategy to use for tasks in service. Format is <type>=<optional field>,...",
            };



        // Properties for defining a task definition
        public static readonly CommandOption ARGUMENT_CONTAINER_COMMANDS =
            new CommandOption
            {
                Name = "Command",
                Switch = "--container-command",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "A comma delimited list of commands to pass to the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_CPU =
            new CommandOption
            {
                Name = "Container CPU",
                Switch = "--container-cpu",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The number of cpu units reserved for the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_DISABLE_NETWORKING =
            new CommandOption
            {
                Name = "Container Disable Networking",
                Switch = "--container-disable-networking",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "When this parameter is true, networking is disabled within the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_DNS_SEARCH_DOMAINS =
            new CommandOption
            {
                Name = "Container DNS Search-Domains",
                Switch = "--container-dns-search-domains",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "A comma delimited of DNS search domains that are presented to the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_DNS_SERVERS =
            new CommandOption
            {
                Name = "Container DNS Servers",
                Switch = "--container-dns-servers",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "A comma delimited of DNS servers that are presented to the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_DOCKER_LABELS =
            new CommandOption
            {
                Name = "Container Docker Labels",
                Switch = "--container-docker-labels",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Labels to add to the container. Format is <key1>=<value1>;<key2>=<value2>.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_DOCKER_SECURITY_OPTIONS =
            new CommandOption
            {
                Name = "Container Docker Security Options",
                Switch = "--container-docker-security-options",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "A list of strings to provide custom labels for SELinux and AppArmor multi-level security systems.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_ENTRY_POINT =
            new CommandOption
            {
                Name = "Container Entry Point",
                Switch = "--container-entry-point",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "The entry point that is passed to the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_ENVIRONMENT_VARIABLES =
            new CommandOption
            {
                Name = "Container Environment Variables",
                Switch = "--container-environment-variables",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Environment variables for a container definition. Format is <key1>=<value1>;<key2>=<value2>."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_ESSENTIAL =
            new CommandOption
            {
                Name = "Container is Essential",
                Switch = "--container-is-essential",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true, and that container fails, all other containers that are part of the task are stopped.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_EXTRA_HOSTS =
            new CommandOption
            {
                Name = "Container Extra Hosts",
                Switch = "--container-extra-hosts",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "Hostnames and IP address entries that are added to the /etc/hosts file of a container. Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_HOSTNAME =
            new CommandOption
            {
                Name = "Container Hostname",
                Switch = "--container-hostname",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The hostname to use for your container."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_LINKS =
            new CommandOption
            {
                Name = "Container Links",
                Switch = "--container-links",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "Comma delimited list of container names to communicate without the need for port mapping."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_LINUX_PARAMETERS =
            new CommandOption
            {
                Name = "Container Linux Parameters",
                Switch = "--container-linux-parameters",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "The Linux capabilities for the container that are added to or dropped. Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_LOG_CONFIGURATION =
            new CommandOption
            {
                Name = "Container Log Configuration",
                Switch = "--container-log-configuration",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "The log driver to use for the container. Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_MEMORY_HARD_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Hard Limit",
                Switch = "--container-memory-hard-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The hard limit (in MiB) of memory to present to the container.",
            };

        public static readonly CommandOption ARGUMENT_CONTAINER_MEMORY_SOFT_LIMIT =
            new CommandOption
            {
                Name = "Container Memory Soft Limit",
                Switch = "--container-memory-soft-limit",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = "The soft limit (in MiB) of memory to reserve for the container.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_MOUNT_POINTS =
            new CommandOption
            {
                Name = "Container Mount Points",
                Switch = "--container-mount-points",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "The mount points for data volumes in your container. Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_NAME =
            new CommandOption
            {
                Name = "Container Name",
                Switch = "--container-name",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the Container in a Task Definition to be created/updated.",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_PORT_MAPPING =
            new CommandOption
            {
                Name = "Container Port Mapping",
                Switch = "--container-port-mapping",
                ValueType = CommandOption.CommandOptionValueType.CommaDelimitedList,
                Description = "The mapping of ports. Format is <host-port>:<container-port>,...",
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_PRIVILEGED =
            new CommandOption
            {
                Name = "Container Privileged",
                Switch = "--container-privileged",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true, the container is given elevated privileges on the host container instance"
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_READONLY_ROOT_FILESYSTEM =
            new CommandOption
            {
                Name = "Container Readonly Root Filesystem",
                Switch = "--container-readonly-root-filesystem",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true, the container is given read-only access to its root file system."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_ULIMITS =
            new CommandOption
            {
                Name = "Container ULimits",
                Switch = "--container-ulimits",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "The ulimit settings to pass to the container. Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_USER =
            new CommandOption
            {
                Name = "Container User",
                Switch = "--container-user",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The user name to use inside the container."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_VOLUMES_FROM =
            new CommandOption
            {
                Name = "Container Volumes From",
                Switch = "--container-ulimits",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "Details on a data volume from another container in the same task definition.  Format is JSON string."
            };
        public static readonly CommandOption ARGUMENT_CONTAINER_WORKING_DIRECTORY =
            new CommandOption
            {
                Name = "Container Working Directory",
                Switch = "--container-working-directory",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The working directory in which to run commands inside the container."
            };

        public static readonly CommandOption ARGUMENT_TD_NAME =
            new CommandOption
            {
                Name = "Task Definition Name",
                Switch = "--task-definition-name",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name of the ECS Task Defintion to be created or updated.",
            };
        public static readonly CommandOption ARGUMENT_TD_NETWORK_MODE =
            new CommandOption
            {
                Name = "Task Definition Network Mode",
                Switch = "--task-definition-network-mode",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The Docker networking mode to use for the containers in the task."
            };
        public static readonly CommandOption ARGUMENT_TD_ROLE =
            new CommandOption
            {
                Name = "Task Definition Role",
                Switch = "--task-definition-task-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IAM role that will provide AWS credentials for the containers in the Task Definition."
            };
        public static readonly CommandOption ARGUMENT_TD_VOLUMES =
            new CommandOption
            {
                Name = "Task Definition Volumes",
                Switch = "--task-definition-volumes",
                ValueType = CommandOption.CommandOptionValueType.JsonValue,
                Description = "Volume definitions that containers in your task may use. Format is JSON string."
            };


    }
}