using System;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class EnvironmentReplacementTests
    {
        [Fact]
        public void NoVariables()
        {
            var result = Utilities.ReplaceEnvironmentVariables("some string");
            Assert.Equal("some string", result);
        }

        [Fact]
        public void EnvironmentVariableNotSet()
        {
            var result = Utilities.ReplaceEnvironmentVariables("some string=$(variable)");
            Assert.Equal("some string=$(variable)", result);
        }

        [Fact]
        public void SingleVariableMultipleReplacement()
        {
            Environment.SetEnvironmentVariable("Key1", "replacement1");

            var result = Utilities.ReplaceEnvironmentVariables($"some string=$(Key1) other $(Key1)");
            Assert.Equal("some string=replacement1 other replacement1", result);
        }

        [Fact]
        public void MultipleReplacement()
        {
            Environment.SetEnvironmentVariable("otherkey1", "replacement1");
            Environment.SetEnvironmentVariable("Key2", "variable2");
            Environment.SetEnvironmentVariable("KEY3", "3333");

            var result = Utilities.ReplaceEnvironmentVariables($"some string=$(otherkey1) other $(Key2) last $(KEY3) set");
            Assert.Equal("some string=replacement1 other variable2 last 3333 set", result);
        }
    }
}
