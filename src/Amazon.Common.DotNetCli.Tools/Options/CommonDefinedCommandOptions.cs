
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
                Description = "Target framework to compile, for example netcoreapp3.1.",
            };
        public static readonly CommandOption ARGUMENT_SELF_CONTAINED =
            new CommandOption
            {
                Name = "Self Contained",
                Switch = "--self-contained",
                ValueType = CommandOption.CommandOptionValueType.BoolValue,
                Description = "If true a self contained deployment bundle including the targeted .NET runtime will be created.",
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


        public static readonly CommandOption ARGUMENT_DOCKER_TAG =
            new CommandOption
            {
                Name = "Docker Image Tag",
                ShortSwitch = "-it",
                Switch = "--image-tag",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Name and optionally a tag in the 'name:tag' format.",
            };
        public static readonly CommandOption ARGUMENT_DOCKER_TAG_OBSOLETE =
            new CommandOption
            {
                Name = "Docker Image Tag",
                ShortSwitch = "-t",
                Switch = "--tag",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "Obsolete. This has been replaced with the --image-tag switch.",
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
        public static readonly CommandOption ARGUMENT_DOCKERFILE =
            new CommandOption
            {
                Name = "Dockerfile",
                ShortSwitch = "-df",
                Switch = "--dockerfile",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = $"The docker file used to build the image. Default value is \"{Constants.DEFAULT_DOCKERFILE}\".",
            };
        public static readonly CommandOption ARGUMENT_LOCAL_DOCKER_IMAGE =
            new CommandOption
            {
                Name = "Local Docker Image",
                ShortSwitch = "-ldi",
                Switch = "--local-docker-image",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "If set the docker build command is skipped and the indicated local image is pushed to ECR.",
            };

        public static readonly CommandOption ARGUMENT_HOST_BUILD_OUTPUT =
            new CommandOption
            {
                Name = "Host Build Output Directory",
                Switch = "--docker-host-build-output-dir",
                ValueType = CommandOption.CommandOptionValueType.StringValue,
                Description = "If set a \"dotnet publish\" command is executed on the host machine before executing \"docker build\". The output can be copied into image being built.",
            };
    }
}
