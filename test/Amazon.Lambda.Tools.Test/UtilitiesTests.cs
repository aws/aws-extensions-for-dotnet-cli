using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Amazon.Lambda.Tools.Test
{
    public  class UtilitiesTests
    {
        [Theory]
        [InlineData("dotnetcore2.1", "netcoreapp2.1")]
        [InlineData("dotnetcore2.0", "netcoreapp2.0")]
        [InlineData("dotnetcore1.0", "netcoreapp1.0")]
        public void MapLambdaRuntimeWithFramework(string runtime, string framework)
        {
            Assert.Equal(runtime, LambdaUtilities.DetermineLambdaRuntimeFromTargetFramework(framework));
        }

        [Fact]
        public void MapInvalidLambdaRuntimeWithFramework()
        {
            Assert.Null(LambdaUtilities.DetermineLambdaRuntimeFromTargetFramework("not-real"));
        }

        [Theory]
        [InlineData("exactlength", "exactlength", 11)]
        [InlineData("short", "short", 10)]
        [InlineData("longlonglong", "longlong", 8)]
        public void TestGettingLayerDescriptionForNonDotnetLayer(string fullLayer, string displayLayer, int maxLength)
        {
            var value = LambdaUtilities.DetermineListDisplayLayerDescription(fullLayer, maxLength);
            Assert.Equal(displayLayer, value);
        }

        [Theory]
        [InlineData("netcoreapp1.0", "rhel.7.2-x64")]
        [InlineData("netcoreapp1.1", "linux-x64")]
        [InlineData("netcoreapp2.0", "rhel.7.2-x64")]
        [InlineData("netcoreapp2.1", "rhel.7.2-x64")]
        [InlineData("netcoreapp2.2", "linux-x64")]
        [InlineData("netcoreapp3.0", "linux-x64")]
        [InlineData("netcoreapp3.1", "linux-x64")]
        [InlineData("netcoreapp6.0", "linux-x64")]
        public void TestDetermineRuntimeParameter(string targetFramework, string expectedValue)
        {
            var runtime = LambdaUtilities.DetermineRuntimeParameter(targetFramework);
            Assert.Equal(expectedValue, runtime);
        }
    }
}
