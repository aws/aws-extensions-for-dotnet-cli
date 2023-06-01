using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;
using Amazon.Common.DotNetCli.Tools;
using System.Threading.Tasks;
using Moq;
using Amazon.S3;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System.Threading;
using Amazon.S3.Model;
using Xunit.Abstractions;
using System.Linq;
using static Amazon.Lambda.Tools.TemplateProcessor.UpdatableResource;

namespace Amazon.Lambda.Tools.Test
{
    public class UtilitiesTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UtilitiesTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

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

            if (expectRepositoryName == null)
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

        [Theory]
        [InlineData("netcoreapp1.0", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp1.1", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp2.0", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp2.1", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp2.2", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp3.0", LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData("netcoreapp3.1", LambdaConstants.ARCHITECTURE_X86_64, "public.ecr.aws/sam/build-dotnetcore3.1:latest-x86_64")]
        [InlineData("netcoreapp3.1", "", "public.ecr.aws/sam/build-dotnetcore3.1:latest-x86_64")]
        [InlineData("netcoreapp3.1", LambdaConstants.ARCHITECTURE_ARM64, "public.ecr.aws/sam/build-dotnetcore3.1:latest-arm64")]
        [InlineData("net6.0", LambdaConstants.ARCHITECTURE_X86_64, "public.ecr.aws/sam/build-dotnet6:latest-x86_64")]
        [InlineData("net6.0", null, "public.ecr.aws/sam/build-dotnet6:latest-x86_64")]
        [InlineData("net6.0", LambdaConstants.ARCHITECTURE_ARM64, "public.ecr.aws/sam/build-dotnet6:latest-arm64")]
        [InlineData("net7.0", LambdaConstants.ARCHITECTURE_X86_64, "public.ecr.aws/sam/build-dotnet7:latest-x86_64")]
        [InlineData("net7.0", " ", "public.ecr.aws/sam/build-dotnet7:latest-x86_64")]
        [InlineData("net7.0", LambdaConstants.ARCHITECTURE_ARM64, "public.ecr.aws/sam/build-dotnet7:latest-arm64")]
        [InlineData(null, LambdaConstants.ARCHITECTURE_X86_64, "throws")]
        [InlineData(null, LambdaConstants.ARCHITECTURE_ARM64, "throws")]
        public void GetDefaultBuildImage(string targetFramework, string architecture, string expectedValue)
        {
            if (expectedValue == "throws")
            {
                Assert.Throws<LambdaToolsException>(() => LambdaUtilities.GetDefaultBuildImage(targetFramework, architecture, new TestToolLogger(_testOutputHelper)));
                return;
            }

            var containerImageFile = LambdaUtilities.GetDefaultBuildImage(targetFramework, architecture, new TestToolLogger(_testOutputHelper));
            Assert.Equal(expectedValue, containerImageFile);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("somelocalpath", false)]
        [InlineData("123456789012", false)]
        [InlineData("139480602983.dkr.ecr.us-east-2.amazonaws.com/test-deploy-serverless-image-uri", true)]
        [InlineData("139480602.dkr.ecr.us-east-2.amazonaws.com/test-deploy-serverless-image-uri", false)]
        public void TestIsECRImage(string path, bool expectedValue)
        {
            bool isECRImage = UpdatableResourceField.IsECRImage(path);
            Assert.Equal(expectedValue, isECRImage);
        }

        [Theory]
        [InlineData("../../../../../testapps/TestFunction", "net6.0", false, false)]
        [InlineData("../../../../../testapps/TestFunction", "net6.0", true, true)]
        [InlineData("../../../../../testapps/TestFunction", "net7.0", true, true)]
        [InlineData("../../../../../testapps/TestFunction", "net7.0", false, false)]
        [InlineData("../../../../../testapps/TestFunction", "net5.0", false, false)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "net7.0", true, false)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "net7.0", false, false)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "net6.0", false, false)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "net6.0", true, true)]
        [InlineData("../../../../../testapps/TestNativeAotSingleProject", "net5.0", true, true)]
        public void TestValidateTargetFramework(string projectLocation, string targetFramework, bool isNativeAot, bool shouldThrow)
        {
            if (shouldThrow)
            {
                Assert.Throws<LambdaToolsException>(() => LambdaUtilities.ValidateTargetFramework(projectLocation, targetFramework, isNativeAot));
            }
            else
            {
                // If this throws an exception, the test will fail, hench no assert is necessary
                LambdaUtilities.ValidateTargetFramework(projectLocation, targetFramework, isNativeAot);
            }
        }

        [Fact]
        public async Task UseDefaultBucketThatAlreadyExists()
        {
            var expectedBucket = $"{LambdaConstants.DEFAULT_BUCKET_NAME_PREFIX}us-west-2-123412341234";

            bool getCallerIdentityCallCount = false;
            var stsClientMock = new Mock<IAmazonSecurityTokenService>();
            stsClientMock.Setup(client => client.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                        .Returns((GetCallerIdentityRequest r, CancellationToken token) =>
                        {
                            getCallerIdentityCallCount = true;
                            var response = new GetCallerIdentityResponse { Account = "123412341234" };

                            return Task.FromResult(response);
                        });

            bool listBucketsAsyncCallCount = false;
            bool putBucketCallCount = false;
            var s3ClientMock = new Mock<IAmazonS3>();
            s3ClientMock.SetupGet(client => client.Config).Returns(new AmazonS3Config { RegionEndpoint = RegionEndpoint.USWest2 });
            s3ClientMock.Setup(client => client.ListBucketsAsync(It.IsAny<ListBucketsRequest>(), It.IsAny<CancellationToken>()))
                        .Returns(() =>
                        {
                            listBucketsAsyncCallCount = true;
                            var response = new ListBucketsResponse { Buckets = { new S3Bucket { BucketName = expectedBucket } } };

                            return Task.FromResult(response);
                        });
            s3ClientMock.Setup(client => client.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
                        .Returns(() =>
                        {
                            putBucketCallCount = true;
                            return Task.FromResult(new PutBucketResponse());
                        });




            var logger = new TestToolLogger();

            var s3Bucket = await LambdaUtilities.ResolveDefaultS3Bucket(logger, s3ClientMock.Object, stsClientMock.Object);
            Assert.Equal(expectedBucket, s3Bucket);

            Assert.True(getCallerIdentityCallCount);
            Assert.True(listBucketsAsyncCallCount);
            Assert.False(putBucketCallCount);
        }

        [Fact]
        public async Task CreateDefaultBucket()
        {
            var expectedBucket = $"{LambdaConstants.DEFAULT_BUCKET_NAME_PREFIX}us-west-2-123412341234";

            bool getCallerIdentityCallCount = false;
            var stsClientMock = new Mock<IAmazonSecurityTokenService>();
            stsClientMock.Setup(client => client.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                        .Returns((GetCallerIdentityRequest r, CancellationToken token) =>
                        {
                            getCallerIdentityCallCount = true;
                            var response = new GetCallerIdentityResponse { Account = "123412341234" };

                            return Task.FromResult(response);
                        });

            bool listBucketsAsyncCallCount = false;
            bool putBucketCallCount = false;
            var s3ClientMock = new Mock<IAmazonS3>();
            s3ClientMock.SetupGet(client => client.Config).Returns(new AmazonS3Config { RegionEndpoint = RegionEndpoint.USWest2 });
            s3ClientMock.Setup(client => client.ListBucketsAsync(It.IsAny<ListBucketsRequest>(), It.IsAny<CancellationToken>()))
                        .Returns(() =>
                        {
                            listBucketsAsyncCallCount = true;
                            return Task.FromResult(new ListBucketsResponse());
                        });
            s3ClientMock.Setup(client => client.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
                        .Callback<PutBucketRequest, CancellationToken>((request, token) =>
                        {
                            Assert.Equal(expectedBucket, request.BucketName);
                        })
                        .Returns(() =>
                        {
                            putBucketCallCount = true;
                            return Task.FromResult(new PutBucketResponse());
                        });




            var logger = new TestToolLogger();

            var s3Bucket = await LambdaUtilities.ResolveDefaultS3Bucket(logger, s3ClientMock.Object, stsClientMock.Object);
            Assert.Equal(expectedBucket, s3Bucket);

            Assert.True(getCallerIdentityCallCount);
            Assert.True(listBucketsAsyncCallCount);
            Assert.True(putBucketCallCount);
        }
    }
}
