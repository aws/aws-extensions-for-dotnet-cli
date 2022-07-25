using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.Lambda.Tools.Test
{
    public  class UtilitiesTests
    {
        [Theory]
        [InlineData("dotnet6", "net6.0")]
        [InlineData("dotnetcore3.1", "netcoreapp3.1")]
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
        [InlineData("netcoreapp1.0", LambdaConstants.ARCHITECTURE_X86_64, "rhel.7.2-x64")]
        [InlineData("netcoreapp1.1", LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData("netcoreapp2.0", LambdaConstants.ARCHITECTURE_X86_64, "rhel.7.2-x64")]
        [InlineData("netcoreapp2.1", LambdaConstants.ARCHITECTURE_X86_64, "rhel.7.2-x64")]
        [InlineData("netcoreapp2.2", LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData("netcoreapp3.0", LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData("netcoreapp3.1", LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData("netcoreapp6.0", LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData("netcoreapp3.1", LambdaConstants.ARCHITECTURE_ARM64, "linux-arm64")]
        [InlineData("netcoreapp6.0", LambdaConstants.ARCHITECTURE_ARM64, "linux-arm64")]
        [InlineData(null, LambdaConstants.ARCHITECTURE_X86_64, "linux-x64")]
        [InlineData(null, LambdaConstants.ARCHITECTURE_ARM64, "linux-arm64")]
        public void TestDetermineRuntimeParameter(string targetFramework, string architecture, string expectedValue)
        {
            var runtime = LambdaUtilities.DetermineRuntimeParameter(targetFramework, architecture);
            Assert.Equal(expectedValue, runtime);
        }

        [Theory]
        [InlineData("repo:old", "repo", "old")]
        [InlineData("repo", "repo", "latest")]
        [InlineData("repo:", "repo", "latest")]
        public void TestSplitImageTag(string imageTag, string repositoryName, string tag)
        {
            var tuple = Amazon.Lambda.Tools.Commands.PushDockerImageCommand.SplitImageTag(imageTag);
            Assert.Equal(repositoryName, tuple.RepositoryName);
            Assert.Equal(tag, tuple.Tag);
        }

        [Theory]
        [InlineData("Seed", "foo:bar", "1234", "foo:seed-1234-bar")]
        [InlineData("Seed", "foo", "1234", "foo:seed-1234-latest")]
        public void TestGenerateUniqueTag(string uniqueSeed, string destinationDockerTag, string imageId, string expected)
        {
            var tag = Amazon.Lambda.Tools.Commands.PushDockerImageCommand.GenerateUniqueEcrTag(uniqueSeed, destinationDockerTag, imageId);
            Assert.Equal(expected, tag);
        }

        [Fact]
        public void TestGenerateUniqueTagWithNullImageId()
        {
            var tag = Amazon.Lambda.Tools.Commands.PushDockerImageCommand.GenerateUniqueEcrTag("Seed", "foo:bar", null);

            Assert.StartsWith("foo:seed-", tag);
            Assert.EndsWith("-bar", tag);

            var tokens = tag.Split('-');
            var ticks = long.Parse(tokens[1]);
            var dt = new DateTime(ticks);
            Assert.Equal(DateTime.UtcNow.Year, dt.Year);
        }

        [Theory]
        [InlineData("ProjectName", "projectname")]
        [InlineData("Amazon.Lambda_Tools-CLI", "amazon.lambda_tools-cli")]
        [InlineData("._-Solo", "solo")]
        [InlineData("Foo@Bar", "foobar")]
        [InlineData("#$%^&!", null)]
        [InlineData("a", null)]
        public void TestGeneratingECRRepositoryName(string projectName, string expectRepositoryName)
        {
            string repositoryName;
            var computed = Amazon.Common.DotNetCli.Tools.Utilities.TryGenerateECRRepositoryName(projectName, out repositoryName);

            if(expectRepositoryName == null)
            {
                Assert.False(computed);
            }
            else
            {
                Assert.True(computed);
                Assert.Equal(expectRepositoryName, repositoryName);
            }
        }

        [Fact]
        public void TestMaxLengthGeneratedRepositoryName()
        {
            var projectName = new string('A', 1000);
            string repositoryName;
            var computed = Amazon.Common.DotNetCli.Tools.Utilities.TryGenerateECRRepositoryName(projectName, out repositoryName);

            Assert.True(computed);
            Assert.Equal(new string('a', 256), repositoryName);
        }

        [Theory]
        [InlineData("Dockerfile.custom", "", "Dockerfile.custom")]
        [InlineData(@"c:\project\Dockerfile.custom", null, @"c:\project\Dockerfile.custom")]
        [InlineData("Dockerfile.custom", @"c:\project", "Dockerfile.custom")]
        [InlineData(@"c:\project\Dockerfile.custom", @"c:\project", "Dockerfile.custom")]
        [InlineData(@"c:\par1\Dockerfile.custom", @"c:\par1\par2", "../Dockerfile.custom")]
        public void TestSavingDockerfileInDefaults(string dockerfilePath, string projectLocation, string expected)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dockerfilePath = dockerfilePath.Replace(@"c:\", "/").Replace(@"\", "/");
                projectLocation = projectLocation?.Replace(@"c:\", "/").Replace(@"\", "/");
                expected = expected.Replace(@"c:\", "/").Replace(@"\", "/");
            }
            var rootData = new ThirdParty.Json.LitJson.JsonData();
            rootData.SetFilePathIfNotNull("Dockerfile", dockerfilePath, projectLocation);

            Assert.Equal(expected, rootData["Dockerfile"]);
        }
    }
}
