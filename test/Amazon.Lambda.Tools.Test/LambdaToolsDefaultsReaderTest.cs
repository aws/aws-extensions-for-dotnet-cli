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
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction/");
            return fullPath;
        }

        [Fact]
        public void LoadDefaultsDirectly()
        {
            var defaults = new LambdaToolsDefaults();
            defaults.LoadDefaults(GetTestProjectPath(), LambdaToolsDefaults.DEFAULT_FILE_NAME);

            Assert.Equal("us-east-2", defaults.Region);
            Assert.Equal("us-east-2", defaults["region"]);

            Assert.Equal(true, defaults["disable-version-check"]);
            Assert.Equal(128, defaults["function-memory-size"]);

        }

        [Fact]
        public void CommandInferRegionFromDefaults()
        {
            var command = new DeployFunctionCommand(new ConsoleToolLogger(), GetTestProjectPath(), new string[0]);

            Assert.Equal("us-east-2", command.GetStringValueOrDefault(command.Region, CommonDefinedCommandOptions.ARGUMENT_AWS_REGION, true));
        }
    }
}
