using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class UtilitiesTests
    {
        [Theory]
        [InlineData("../../../../../testapps/TestFunction", "net6.0")]
        [InlineData("../../../../../testapps/ServerlessWithYamlFunction", "net6.0")]
        [InlineData("../../../../../testapps/TestBeanstalkWebApp", "netcoreapp3.1")]
        [InlineData("../../../../../testapps/TestFunctionBuildProps/TestFunctionBuildProps", "net6.0")]
        public void CheckFramework(string projectPath, string expectedFramework)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + projectPath);
            var determinedFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectPath, null);
            Assert.Equal(expectedFramework, determinedFramework);
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

        [Theory]
        [InlineData("../../../../../testapps/TestFunction", null, false)]
        [InlineData("../../../../../testapps/TestFunction", "", false)]
        [InlineData("../../../../../testapps/TestFunction", "publishaot=true", true)]
        [InlineData("../../../../../testapps/TestFunction", "publishAOT=False", false)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", null, true)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "publishAOT=False", false)]
        public void TestLookForPublishAotFlag(string projectLocation, string msBuildParameters, bool expected)
        {
            var result = Utilities.LookPublishAotFlag(projectLocation, msBuildParameters);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("../../../../../testapps/TestFunction", "Library")]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "Exe")]
        public void TestLookupOutputTypeFromProjectFile(string projectLocation, string expected)
        {
            var result = Utilities.LookupOutputTypeFromProjectFile(projectLocation, null);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("../../../../../testapps/TestFunction", null, false)]
        [InlineData("../../../../../testapps/TestFunction", "", false)]
        [InlineData("../../../../../testapps/TestFunction", "--self-contained=true", true)]
        [InlineData("../../../../../testapps/TestFunction", "--self-contained=false", true)]
        [InlineData("../../../../../testapps/TestFunction", "--self-contained true", true)]
        [InlineData("../../../../../testapps/TestFunction", "--self-contained false", true)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", null, true)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "--self-contained false", true)]
        public void TestHasExplicitSelfContainedFlag(string projectLocation, string msBuildParameters, bool expected)
        {
            var result = Utilities.HasExplicitSelfContainedFlag(projectLocation, msBuildParameters);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TargetFramework", "", "net6.0")]
        [InlineData("TargetFramework", "/p:NonExistence=net20.0", "net6.0")]
        [InlineData("TargetFramework", "/p:TargetFramework=net20.0", "net20.0")]
        public void TestPropertyEvaluationWithMSBuildParameters(string property, string msbuildparameters, string expectedValue)
        {
            var projectLocation = "../../../../../testapps/TestFunction";

            var value = Utilities.LookupProjectProperties(projectLocation, msbuildparameters, property)[property];
            Assert.Equal(expectedValue, value);
        }
    }
}