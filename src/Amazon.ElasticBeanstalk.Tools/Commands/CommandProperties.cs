using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public class DeployEnvironmentProperties
    {
        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string PublishOptions { get; set; }
        public string Application { get; set; }
        public string Environment { get; set; }
        public string UrlPath { get; set; }
        public string IISWebSite { get; set; }
        public bool? WaitForUpdate { get; set; }
        public bool? EnableXRay { get; set; }
        public Dictionary<string,string> Tags { get; set; }
        public Dictionary<string, string> AdditionalOptions { get; set; }


        public string SolutionStack { get; set; }
        public string EnvironmentType { get; set; }
        public string CNamePrefix { get; set; }
        public string InstanceType { get; set; }
        public string EC2KeyPair { get; set; }
        public string HealthCheckUrl { get; set; }
        public string InstanceProfile { get; set; }
        public string ServiceRole { get; set; }
        public string VersionLabel { get; set; }


        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.Switch)) != null)
                this.PublishOptions = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION.Switch)) != null)
                this.Application = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT.Switch)) != null)
                this.Environment = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_APP_PATH.Switch)) != null)
                this.UrlPath = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE.Switch)) != null)
                this.IISWebSite = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_WAIT_FOR_UPDATE.Switch)) != null)
                this.WaitForUpdate = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_VERSION_LABEL.Switch)) != null)
                this.VersionLabel = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_TAGS.Switch)) != null)
                this.Tags = tuple.Item2.KeyValuePairs;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_ADDITIONAL_OPTIONS.Switch)) != null)
                this.AdditionalOptions = tuple.Item2.KeyValuePairs;

            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_SOLUTION_STACK.Switch)) != null)
                this.SolutionStack = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_ENVIRONMENT_TYPE.Switch)) != null)
                this.EnvironmentType = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_CNAME_PREFIX.Switch)) != null)
                this.CNamePrefix = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_INSTANCE_TYPE.Switch)) != null)
                this.InstanceType = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EC2_KEYPAIR.Switch)) != null)
                this.EC2KeyPair = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_HEALTH_CHECK_URL.Switch)) != null)
                this.HealthCheckUrl = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_INSTANCE_PROFILE.Switch)) != null)
                this.InstanceProfile = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_SERVICE_ROLE.Switch)) != null)
                this.ServiceRole = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_ENABLE_XRAY.Switch)) != null)
                this.EnableXRay = tuple.Item2.BoolValue;
        }


        internal void PersistSettings(EBBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION.ConfigFileKey, command.GetStringValueOrDefault(this.Configuration, CommonDefinedCommandOptions.ARGUMENT_CONFIGURATION, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, command.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS.ConfigFileKey, command.GetStringValueOrDefault(this.PublishOptions, CommonDefinedCommandOptions.ARGUMENT_PUBLISH_OPTIONS, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION.ConfigFileKey, command.GetStringValueOrDefault(this.Application, EBDefinedCommandOptions.ARGUMENT_EB_APPLICATION, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT.ConfigFileKey, command.GetStringValueOrDefault(this.Environment, EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_APP_PATH.ConfigFileKey, command.GetStringValueOrDefault(this.UrlPath, EBDefinedCommandOptions.ARGUMENT_APP_PATH, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE.ConfigFileKey, command.GetStringValueOrDefault(this.IISWebSite, EBDefinedCommandOptions.ARGUMENT_IIS_WEBSITE, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EB_TAGS.ConfigFileKey, ElasticBeanstalkToolsDefaults.FormatKeyValue(command.GetKeyValuePairOrDefault(this.Tags, EBDefinedCommandOptions.ARGUMENT_EB_TAGS, false)));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_ENABLE_XRAY.ConfigFileKey, command.GetBoolValueOrDefault(this.EnableXRay, EBDefinedCommandOptions.ARGUMENT_ENABLE_XRAY, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EB_ADDITIONAL_OPTIONS.ConfigFileKey, ElasticBeanstalkToolsDefaults.FormatKeyValue(command.GetKeyValuePairOrDefault(this.AdditionalOptions, EBDefinedCommandOptions.ARGUMENT_EB_ADDITIONAL_OPTIONS, false)));

            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_SOLUTION_STACK.ConfigFileKey, command.GetStringValueOrDefault(this.SolutionStack, EBDefinedCommandOptions.ARGUMENT_SOLUTION_STACK, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_ENVIRONMENT_TYPE.ConfigFileKey, command.GetStringValueOrDefault(this.EnvironmentType, EBDefinedCommandOptions.ARGUMENT_ENVIRONMENT_TYPE, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_CNAME_PREFIX.ConfigFileKey, command.GetStringValueOrDefault(this.CNamePrefix, EBDefinedCommandOptions.ARGUMENT_CNAME_PREFIX, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_INSTANCE_TYPE.ConfigFileKey, command.GetStringValueOrDefault(this.InstanceType, EBDefinedCommandOptions.ARGUMENT_INSTANCE_TYPE, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EC2_KEYPAIR.ConfigFileKey, command.GetStringValueOrDefault(this.EC2KeyPair, EBDefinedCommandOptions.ARGUMENT_EC2_KEYPAIR, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_HEALTH_CHECK_URL.ConfigFileKey, command.GetStringValueOrDefault(this.HealthCheckUrl, EBDefinedCommandOptions.ARGUMENT_HEALTH_CHECK_URL, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_INSTANCE_PROFILE.ConfigFileKey, command.GetStringValueOrDefault(this.InstanceProfile, EBDefinedCommandOptions.ARGUMENT_INSTANCE_PROFILE, false));
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_SERVICE_ROLE.ConfigFileKey, command.GetStringValueOrDefault(this.ServiceRole, EBDefinedCommandOptions.ARGUMENT_SERVICE_ROLE, false));
        }
    }


    public class DeleteEnvironmentProperties
    {
        public string Environment { get; set; }
        internal void ParseCommandArguments(CommandOptions values)
        {
            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT.Switch)) != null)
                this.Environment = tuple.Item2.StringValue;
        }


        internal void PersistSettings(EBBaseCommand command, JsonData data)
        {
            data.SetIfNotNull(EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT.ConfigFileKey, command.GetStringValueOrDefault(this.Environment, EBDefinedCommandOptions.ARGUMENT_EB_ENVIRONMENT, false));
        }
    }
}
