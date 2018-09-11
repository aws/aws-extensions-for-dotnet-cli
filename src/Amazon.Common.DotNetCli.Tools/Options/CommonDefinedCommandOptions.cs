
using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools.Options
{
    public static class CommonDefinedCommandOptions
    {
        public static readonly CommandOption ARGUMENT_CONFIGURATION =
            new CommandOption
            {
                Name = "Build Configuration",
                ShortSwitch = "-c",
                Switch = "--configuration",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Configuration to build with, for example Release or Debug.",
            };
        public static readonly CommandOption ARGUMENT_FRAMEWORK =
            new CommandOption
            {
                Name = "Framework",
                ShortSwitch = "-f",
                Switch = "--framework",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Target framework to compile, for example netcoreapp2.1.",
            };
        public static readonly CommandOption ARGUMENT_PUBLISH_OPTIONS =
            new CommandOption
            {
                Name = "Publish Options",
                ShortSwitch = "-po",
                Switch = "--publish-options",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Additional options passed to the \"dotnet publish\" command.",
            };
        public static readonly CommandOption ARGUMENT_DISABLE_INTERACTIVE =
            new CommandOption
            {
                Name = "Disable Interactive",
                Switch = "--disable-interactive",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "When set to true missing required parameters will not be prompted for."
            };

        public static readonly CommandOption ARGUMENT_AWS_PROFILE =
            new CommandOption
            {
                Name = "AWS Profile",
                Switch = "--profile",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Profile to use to look up AWS credentials, if not set environment credentials will be used."
            };

        public static readonly CommandOption ARGUMENT_AWS_PROFILE_LOCATION =
            new CommandOption
            {
                Name = "AWS Profile Location",
                Switch = "--profile-location",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Optional override to the search location for Profiles, points at a shared credentials file."
            };

        public static readonly CommandOption ARGUMENT_AWS_ACCESS_KEY_ID =
            new CommandOption
            {
                Name = "AWS Access Key ID",
                Switch = "--aws-access-key-id",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The AWS access key id. Used when setting credentials explicitly instead of using --profile."
            };

        public static readonly CommandOption ARGUMENT_AWS_SECRET_KEY =
            new CommandOption
            {
                Name = "AWS Secret Key",
                Switch = "--aws-secret-key",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The AWS secret key. Used when setting credentials explicitly instead of using --profile."
            };

        public static readonly CommandOption ARGUMENT_AWS_SESSION_TOKEN =
            new CommandOption
            {
                Name = "AWS Access Key ID",
                Switch = "--aws-session-token",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The AWS session token. Used when setting credentials explicitly instead of using --profile."
            };

        public static readonly CommandOption ARGUMENT_AWS_REGION =
            new CommandOption
            {
                Name = "AWS Region",
                Switch = "--region",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The region to connect to AWS services, if not set region will be detected from the environment."
            };


        public static readonly CommandOption ARGUMENT_PROJECT_LOCATION =
            new CommandOption
            {
                Name = "Project Location",
                ShortSwitch = "-pl",
                Switch = "--project-location",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "The location of the project, if not set the current directory will be assumed."
            };
        
        public static readonly CommandOption ARGUMENT_MSBUILD_PARAMETERS =
            new CommandOption
            {
                Name = "MSBuild Parameters",
                Switch = "--msbuild-parameters",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Additional msbuild parameters passed to the 'dotnet publish' command. Add quotes around the value if the value contains spaces.",
            };
        

        public static readonly CommandOption ARGUMENT_CONFIG_FILE =
            new CommandOption
            {
                Name = "Config File",
                ShortSwitch = "-cfg",
                Switch = "--config-file",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"Configuration file storing default values for command line arguments."
            };
        public static readonly CommandOption ARGUMENT_PERSIST_CONFIG_FILE =
            new CommandOption
            {
                Name = "Persist Config File",
                ShortSwitch = "-pcfg",
                Switch = "--persist-config-file",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = $"If true the arguments used for a successful deployment are persisted to a config file."
            };

    }
}
