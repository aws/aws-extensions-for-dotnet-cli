using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.ElasticBeanstalk.Tools
{
    public class EBDefinedCommandOptions
    {
        public static readonly CommandOption ARGUMENT_INPUT_PACKAGE =
            new CommandOption
            {
                Name = "Package",
                ShortSwitch = "-pac",
                Switch = "--package",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Application package to use for deployment, skips building the project"
            };
        public static readonly CommandOption ARGUMENT_EB_APPLICATION =
            new CommandOption
            {
                Name = "Elastic Beanstalk Application",
                ShortSwitch = "-app",
                Switch = "--application",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the Elastic Beanstalk application."
            };
        public static readonly CommandOption ARGUMENT_EB_ENVIRONMENT =
            new CommandOption
            {
                Name = "Elastic Beanstalk Environment",
                ShortSwitch = "-env",
                Switch = "--environment",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The name of the Elastic Beanstalk environment."
            };
        public static readonly CommandOption ARGUMENT_CNAME_PREFIX =
            new CommandOption
            {
                Name = "Environment CNAME",
                Switch = "--cname",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "CNAME prefix for a new environment."
            };
        public static readonly CommandOption ARGUMENT_SOLUTION_STACK =
            new CommandOption
            {
                Name = "Elastic Beanstalk Solution Stack",
                Switch = "--solution-stack",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The type of environment to create. For example \"64bit Windows Server 2016 v1.2.0 running IIS 10.0\"."
            };
        public static readonly CommandOption ARGUMENT_ENVIRONMENT_TYPE =
            new CommandOption
            {
                Name = "Type of Elastic Beanstalk Environment",
                Switch = "--environment-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"Type of the environment to launch \"{EBConstants.ENVIRONMENT_TYPE_LOADBALANCED}\" or \"{EBConstants.ENVIRONMENT_TYPE_SINGLEINSTANCE}\". The default is \"{EBConstants.ENVIRONMENT_TYPE_LOADBALANCED}\"."
            };
        public static readonly CommandOption ARGUMENT_EC2_KEYPAIR =
            new CommandOption
            {
                Name = "EC2 Key Pair",
                Switch = "--key-pair",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "EC2 Key pair assigned to the EC2 instances for the environment."
            };
        public static readonly CommandOption ARGUMENT_INSTANCE_TYPE =
            new CommandOption
            {
                Name = "EC2 instance type",
                Switch = "--instance-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"Type of the EC2 instances launched for the environment. The default is \"{EBConstants.DEFAULT_LINUX_INSTANCE_TYPE}\" for Linux and {EBConstants.DEFAULT_WINDOWS_INSTANCE_TYPE} for Windows."
            };
        public static readonly CommandOption ARGUMENT_HEALTH_CHECK_URL =
            new CommandOption
            {
                Name = "Health Check URL",
                Switch = "--health-check-url",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Health Check URL."
            };
        public static readonly CommandOption ARGUMENT_INSTANCE_PROFILE =
            new CommandOption
            {
                Name = "EC2 Instance Profile",
                Switch = "--instance-profile",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Instance profile that provides AWS Credentials to access AWS services."
            };
        public static readonly CommandOption ARGUMENT_SERVICE_ROLE =
            new CommandOption
            {
                Name = "Service Role",
                Switch = "--service-role",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "IAM role to allow Beanstalk to make calls to AWS services."
            };
        public static readonly CommandOption ARGUMENT_EB_VERSION_LABEL =
            new CommandOption
            {
                Name = "Version Label",
                Switch = "--version-label",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Version label that will be assigned to the uploaded version of code. The default is current tick count."
            };
        public static readonly CommandOption ARGUMENT_EB_TAGS =
            new CommandOption
            {
                Name = "Elastic Beanstalk Environment",
                Switch = "--tags",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Tags to assign to the Elastic Beanstalk environment. Format is <tag1>=<value1>;<tag2>=<value2>."
            };
        public static readonly CommandOption ARGUMENT_APP_PATH =
            new CommandOption
            {
                Name = "Application URL Path",
                Switch = "--app-path",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The application path. The default is '/'."
            };
        public static readonly CommandOption ARGUMENT_IIS_WEBSITE =
            new CommandOption
            {
                Name = "IIS Web Site",
                Switch = "--iis-website",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The IIS WebSite for the web application. The default is 'Default Web Site'"
            };
        public static readonly CommandOption ARGUMENT_WAIT_FOR_UPDATE =
            new CommandOption
            {
                Name = "Wait for Update",
                Switch = "--wait",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "Wait for the environment update to complete before exiting."
            };
        public static readonly CommandOption ARGUMENT_ENABLE_XRAY =
            new CommandOption
            {
                Name = "Enable AWS X-Ray",
                Switch = "--enable-xray",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If set to true then the AWS X-Ray daemon will be enabled on EC2 instances running the application."
            };
        public static readonly CommandOption ARGUMENT_ENHANCED_HEALTH_TYPE =
            new CommandOption
            {
                Name = "Enhanced Health Type",
                Switch = "--enhanced-health-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"The type of enhanced health to be enabled. Valid values: {EBConstants.ENHANCED_HEALTH_TYPE_ENHANCED}, {EBConstants.ENHANCED_HEALTH_TYPE_BASIC}"
            };
        public static readonly CommandOption ARGUMENT_LOADBALANCER_TYPE =
            new CommandOption
            {
                Name = "Loadbalancer Type",
                Switch = "--loadbalancer-type",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"LoadBalancer type for the environment. If no value set then a single instance environment type is created. Valid values: {EBConstants.LOADBALANCER_TYPE_APPLICATION}, {EBConstants.LOADBALANCER_TYPE_NETWORK}, {EBConstants.LOADBALANCER_TYPE_CLASSIC}"
            };
        public static readonly CommandOption ARGUMENT_ENABLE_STICKY_SESSIONS =
            new CommandOption
            {
                Name = "Enable Sticky Sessions",
                Switch = "--enable-sticky-sessions",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If set to true sticky sessions will be enabled for the load balancer of the environment."
            };


        public static readonly CommandOption ARGUMENT_EB_ADDITIONAL_OPTIONS =
            new CommandOption
            {
                Name = "Additional Options",
                Switch = "--additional-options",
                ValueType = CommandOption.CommandOptionValueType.KeyValuePairs,
                Description = "Additional options for the environment. Format is <option-namespace>,<option-name>=<option-value>;..."
            };
        public static readonly CommandOption ARGUMENT_OUTPUT_PACKAGE =
            new CommandOption
            {
                Name = "The output zip file name",
                ShortSwitch = "-o",
                Switch = "--output-package",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The zip file containing the application will be packaged into."
            };
        public static readonly CommandOption ARGUMENT_PROXY_SERVER =
            new CommandOption
            {
                Name = "Reverse Proxy Server",
                Switch = "--proxy-server",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"The reverse proxy server used on Linux EC2 instances. Valid values: {EBConstants.PROXY_SERVER_NGINX}, {EBConstants.PROXY_SERVER_NONE}. The default is \"{EBConstants.PROXY_SERVER_NGINX}\"."
            };
        public static readonly CommandOption ARGUMENT_APPLICATION_PORT =
            new CommandOption
            {
                Name = "Application Port",
                Switch = "--application-port",
                ValueType = CommandOption.CommandOptionValueType.IntValue,
                Description = $"The application port that will be redirect to port 80. The default is port 5000."
            };
    }
}
