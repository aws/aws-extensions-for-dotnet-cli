using System;
using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class UtilitiesTests
        : IClassFixture<UtilitiesTests.MSBuildTestFixture>
    {
        // Used to ensure that MSBuild is only initialized once per test run.
        // This is slightly safer than a static constructor, and it gives nicer error messages.
        public sealed class MSBuildTestFixture
        {
            public MSBuildTestFixture()
            {
                MSBuildInitializer.Initialize();
            }
        }

        public UtilitiesTests(MSBuildTestFixture _)
        {
        }

        [Theory]
        [InlineData("../../../../../testapps/TestFunction", "netcoreapp2.1")]
        [InlineData("../../../../../testapps/TestFunctionTargetFrameworks", "netcoreapp2.1")]
        [InlineData("../../../../../testapps/TestFunctionImportTargetFramework", "netcoreapp2.1")]
        [InlineData("../../../../../testapps/TestFunctionAmbiguous", null)]
        [InlineData("../../../../../testapps/ServerlessWithYamlFunction", "netcoreapp2.1")]
        [InlineData("../../../../../testapps/TestBeanstalkWebApp", "netcoreapp2.1")]
        public void CheckFramework(string projectPath, string expectedFramework)
        {
            Assert.Equal(expectedFramework, Utilities.LookupTargetFrameworkFromProjectFile(projectPath));
        }

        [Fact]
        public void TestExecuteShellCommandSuccess()
        {
            var result = Utilities.ExecuteShellCommand(null, FindDotnetProcess(), "--info");
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("SDKs installed", result.Stdout);
        }
        
        [Fact]
        public void TestExecuteShellCommandFail()
        {
            var result = Utilities.ExecuteShellCommand(null, FindDotnetProcess(), "DoesnotExist.dll");
            Assert.Equal(1, result.ExitCode);
        }

        [Fact]
        public void TestParseListSdkOutput()
        {
            const string EXAMPLE_OUTPUT =
@"1.1.11 [/usr/local/share/dotnet/sdk]
2.1.302 [/usr/local/share/dotnet/sdk]
2.1.403 [/usr/local/share/dotnet/sdk]
2.1.503 [/usr/local/share/dotnet/sdk]
2.2.100 [/usr/local/share/dotnet/sdk]
";

            var sdkVersion = Amazon.Common.DotNetCli.Tools.DotNetCLIWrapper.ParseListSdkOutput(EXAMPLE_OUTPUT);
            Assert.Equal(new Version("2.2.100"), sdkVersion);
        }

        string FindDotnetProcess()
        {
            var dotnetCLI = AbstractCLIWrapper.FindExecutableInPath("dotnet.exe");
            if (dotnetCLI == null)
                dotnetCLI = AbstractCLIWrapper.FindExecutableInPath("dotnet");

            return dotnetCLI;
        }
    }
}