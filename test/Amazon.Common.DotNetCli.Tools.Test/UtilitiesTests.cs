using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class UtilitiesTests
    {
        [Fact]
        public void TestExecuteShellCommandSuccess()
        {
            var result = Utilities.ExecuteShellCommand(null, FindDotnetProcess(), "--info");
            Assert.Equal(0, result.Exitcode);
            Assert.Contains(".NET Core SDKs installed", result.Stdout);
        }
        
        [Fact]
        public void TestExecuteShellCommandFail()
        {
            var result = Utilities.ExecuteShellCommand(null, FindDotnetProcess(), "DoesnotExist.dll");
            Assert.Equal(1, result.Exitcode);
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