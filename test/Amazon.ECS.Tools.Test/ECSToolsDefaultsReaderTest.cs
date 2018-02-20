using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;

using Amazon.ECS.Tools;

using Amazon.Tools.TestHelpers;
using Amazon.ECS.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.ECS.Tools.Test
{
    public class ECSToolsDefaultsReaderTest
    {
        [Fact]
        public void LoadDefaultsDirectly()
        {
            var defaults = new ECSToolsDefaults();
            defaults.LoadDefaults(TestUtilities.GetTestProjectPath("HelloWorldWebApp"), ECSToolsDefaults.DEFAULT_FILE_NAME);

            Assert.Equal(defaults["region"], "us-west-2");

            Assert.Equal(defaults["container-memory-hard-limit"], 512);
        }

        [Fact]
        public void CommandInferRegionFromDefaults()
        {
            var command = new DeployServiceCommand(new TestToolLogger(), TestUtilities.GetTestProjectPath("HelloWorldWebApp"), new string[0]);

            Assert.Equal("us-west-2", command.GetStringValueOrDefault(command.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, false));
        }

        [Fact]
        public void GetTaskCPUFromDefaultsAsInt()
        {
            var command = new DeployServiceCommand(new TestToolLogger(), TestUtilities.GetTestProjectPath("HelloWorldWebApp"), new string[0]);

            // CPU is set in aws-ecs-tools-defaults.json as a number
            Assert.Equal("256", command.GetStringValueOrDefault(command.TaskDefinitionProperties.TaskCPU, ECSDefinedCommandOptions.ARGUMENT_TD_CPU, false));

            // Memory is set in aws-ecs-tools-defaults.json as a string
            Assert.Equal("512", command.GetStringValueOrDefault(command.TaskDefinitionProperties.TaskMemory, ECSDefinedCommandOptions.ARGUMENT_TD_MEMORY, false));
        }
    }
}
