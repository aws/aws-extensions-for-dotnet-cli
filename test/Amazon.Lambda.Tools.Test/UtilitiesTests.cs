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
            Assert.Equal(framework, LambdaUtilities.DetermineTargetFrameworkFromLambdaRuntime(runtime));
            Assert.Equal(runtime, LambdaUtilities.DetermineLambdaRuntimeFromTargetFramework(framework));
        }

        [Fact]
        public void MapInvalidLambdaRuntimeWithFramework()
        {
            Assert.Null(LambdaUtilities.DetermineTargetFrameworkFromLambdaRuntime("not-real"));
            Assert.Null(LambdaUtilities.DetermineLambdaRuntimeFromTargetFramework("not-real"));
        }
    }
}
