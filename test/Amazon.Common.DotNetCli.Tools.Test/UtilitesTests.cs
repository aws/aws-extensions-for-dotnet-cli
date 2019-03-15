using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class UtilitesTests
    {
        [Theory]
        [InlineData("../../../../../testapps/TestFunction", "netcoreapp1.0")]
        [InlineData("../../../../../testapps/ServerlessWithYamlFunction", "netcoreapp2.0")]
        [InlineData("../../../../../testapps/TestBeanstalkWebApp", "netcoreapp2.1")]
        public void CheckFramework(string projectPath, string expectedFramework)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + projectPath);
            var determinedFramework = Utilities.LookupTargetFrameworkFromProjectFile(projectPath);
            Assert.Equal(expectedFramework, determinedFramework);
        }
    }
}
