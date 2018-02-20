using System.IO;
using System.Reflection;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Xunit;
using Amazon.Lambda.Tools.Commands;
using Amazon.Tools.TestHelpers;

namespace Amazon.Lambda.Tools.Test
{
    public class LambdaToolsDefaultsReaderTest
    {
        private string GetTestProjectPath()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../TestFunction/");
            return fullPath;
        }

        [Fact]
        public void LoadDefaultsDirectly()
        {
            var defaults = new LambdaToolsDefaults();
            defaults.LoadDefaults(TestUtilities.GetTestProjectPath("HelloWorldWebApp"), LambdaToolsDefaults.DEFAULT_FILE_NAME);

            Assert.Equal(defaults.Region, "us-east-2");
            Assert.Equal(defaults["region"], "us-east-2");

            Assert.Equal(defaults["disable-version-check"], true);
            Assert.Equal(defaults["function-memory-size"], 128);

        }

        [Fact]
        public void CommandInferRegionFromDefaults()
        {
            var command = new DeployFunctionCommand(new ConsoleToolLogger(), GetTestProjectPath(), new string[0]);

            Assert.Equal("us-east-2", command.GetStringValueOrDefault(command.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, true));
        }
    }
}
