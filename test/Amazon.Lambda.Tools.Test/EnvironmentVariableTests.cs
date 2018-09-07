using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;

using Amazon.Lambda.Tools.Commands;

namespace Amazon.Lambda.Tools.Test
{
    public class EnvironmentVariableTests
    {
        [Fact]
        public void SingleAppendEnv()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new UpdateFunctionConfigCommand(logger, fullPath, new string[0]);

            command.EnvironmentVariables = null;
            command.AppendEnvironmentVariables = new Dictionary<string, string> { { "foo", "bar" } };

            var combinedEnv = command.GetEnvironmentVariables(null);
            Assert.Single(combinedEnv);
            Assert.Equal("bar", combinedEnv["foo"]);
        }

        [Fact]
        public void CombinedEnvAndAppendEnv()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new UpdateFunctionConfigCommand(logger, fullPath, new string[0]);

            command.EnvironmentVariables = new Dictionary<string, string> { { "service", "s3" } };
            command.AppendEnvironmentVariables = new Dictionary<string, string> { { "foo", "bar" } };

            var combinedEnv = command.GetEnvironmentVariables(null);
            Assert.Equal(2, combinedEnv.Count);
            Assert.Equal("bar", combinedEnv["foo"]);
            Assert.Equal("s3", combinedEnv["service"]);
        }

        [Fact]
        public void CombinedEnvAndAppendEnvIgnoreExisting()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new UpdateFunctionConfigCommand(logger, fullPath, new string[0]);

            command.EnvironmentVariables = new Dictionary<string, string> { { "service", "s3" } };
            command.AppendEnvironmentVariables = new Dictionary<string, string> { { "foo", "bar" } };

            var combinedEnv = command.GetEnvironmentVariables(new Dictionary<string, string> { { "service", "lambda" } });
            Assert.Equal(2, combinedEnv.Count);
            Assert.Equal("bar", combinedEnv["foo"]);
            Assert.Equal("s3", combinedEnv["service"]);
        }

        [Fact]
        public void CombinedExistingEnvAndAppendEnv()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new UpdateFunctionConfigCommand(logger, fullPath, new string[0]);

            command.EnvironmentVariables = null;
            command.AppendEnvironmentVariables = new Dictionary<string, string> { { "foo", "bar" } };

            var combinedEnv = command.GetEnvironmentVariables(new Dictionary<string, string> { { "service", "lambda" } });
            Assert.Equal(2, combinedEnv.Count);
            Assert.Equal("bar", combinedEnv["foo"]);
            Assert.Equal("lambda", combinedEnv["service"]);
        }
    }
}
