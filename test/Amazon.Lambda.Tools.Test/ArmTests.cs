using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Xunit;
using Xunit.Abstractions;

using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Lambda.Tools;
using Amazon.Lambda.Tools.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.S3.Util;
using Amazon.S3.Transfer;

namespace Amazon.Lambda.Tools.Test
{
    public class ArmTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ArmTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task RunDeployCommand()
        {
            var mockClient = new Mock<IAmazonLambda>();

            mockClient.Setup(client => client.CreateFunctionAsync(It.IsAny<CreateFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateFunctionRequest, CancellationToken>((request, token) =>
                {
                    Assert.Single(request.Architectures);
                    Assert.Equal(Architecture.Arm64, request.Architectures[0]);

                    Assert.Equal("linux-arm64", GetRuntimeFromBundle(request.Code.ZipFile));
                })
                .Returns((CreateFunctionRequest r, CancellationToken token) =>
                {
                    return Task.FromResult(new CreateFunctionResponse());
                });

            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var command = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp3.1";
            command.Runtime = "dotnetcore3.1";
            command.Architecture = LambdaConstants.ARCHITECTURE_ARM64;
            command.DisableInteractive = true;
            command.LambdaClient = mockClient.Object;

            var created = await command.ExecuteAsync();
            Assert.True(created);
        }

        [Fact]
        public async Task CreateArmPackage()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var command = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.DisableInteractive = true;
            command.Architecture = LambdaConstants.ARCHITECTURE_ARM64;
            command.OutputPackageFileName = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
            Assert.Equal("linux-arm64", GetRuntimeFromBundle(command.OutputPackageFileName));

            File.Delete(command.OutputPackageFileName);
        }

        [Fact]
        public async Task TestServerlessPackage()
        {
            var logger = new TestToolLogger(_testOutputHelper);
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new PackageCICommand(logger, fullPath, new string[0]);
            command.Region = "us-west-2";
            command.Configuration = "Release";
            command.CloudFormationTemplate = "serverless-arm.template";
            command.CloudFormationOutputTemplate = Path.Combine(Path.GetTempPath(), "output-serverless-arm.template");
            command.S3Bucket = "serverless-package-test-" + DateTime.Now.Ticks;
            command.DisableInteractive = true;

            if (File.Exists(command.CloudFormationOutputTemplate))
                File.Delete(command.CloudFormationOutputTemplate);


            await command.S3Client.PutBucketAsync(command.S3Bucket);
            try
            {
                Assert.True(await command.ExecuteAsync());
                Assert.True(File.Exists(command.CloudFormationOutputTemplate));

                var templateJson = File.ReadAllText(command.CloudFormationOutputTemplate);
                var templateRoot = JsonConvert.DeserializeObject(templateJson) as JObject;
                var codeUri = templateRoot["Resources"]["DefaultFunction"]["Properties"]["CodeUri"].ToString();
                Assert.False(string.IsNullOrEmpty(codeUri));

                var s3Key = codeUri.Split('/').Last();

                var transfer = new TransferUtility(command.S3Client);
                var functionZipPath = Path.GetTempFileName();
                await transfer.DownloadAsync(functionZipPath, command.S3Bucket, s3Key);
                Assert.Equal("linux-arm64", GetRuntimeFromBundle(functionZipPath));

                File.Delete(functionZipPath);
            }
            finally
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(command.S3Client, command.S3Bucket);
            }

        }

        [Fact]
        public async Task TestServerlessPackageWithGlobalArchitectures()
        {
            var logger = new TestToolLogger(_testOutputHelper);
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new PackageCICommand(logger, fullPath, new string[0]);
            command.Region = "us-west-2";
            command.Configuration = "Release";
            command.CloudFormationTemplate = "serverless-global-arm.template";
            command.CloudFormationOutputTemplate = Path.Combine(Path.GetTempPath(), "output-serverless-global--arm.template");
            command.S3Bucket = "serverless-package-test-" + DateTime.Now.Ticks;
            command.DisableInteractive = true;

            if (File.Exists(command.CloudFormationOutputTemplate))
                File.Delete(command.CloudFormationOutputTemplate);


            await command.S3Client.PutBucketAsync(command.S3Bucket);
            try
            {
                if(!await command.ExecuteAsync())
                {
                    throw new Exception("Failed to publish:\n" + logger.Buffer, command.LastToolsException);
                }

                Assert.True(File.Exists(command.CloudFormationOutputTemplate));

                var templateJson = File.ReadAllText(command.CloudFormationOutputTemplate);
                var templateRoot = JsonConvert.DeserializeObject(templateJson) as JObject;
                var codeUri = templateRoot["Resources"]["DefaultFunction"]["Properties"]["CodeUri"].ToString();
                Assert.False(string.IsNullOrEmpty(codeUri));

                var s3Key = codeUri.Split('/').Last();

                var transfer = new TransferUtility(command.S3Client);
                var functionZipPath = Path.GetTempFileName();
                await transfer.DownloadAsync(functionZipPath, command.S3Bucket, s3Key);
                Assert.Equal("linux-arm64", GetRuntimeFromBundle(functionZipPath));

                File.Delete(functionZipPath);
            }
            finally
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(command.S3Client, command.S3Bucket);
            }

        }

        private string GetRuntimeFromBundle(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                return GetRuntimeFromBundle(stream);
            }
        }

        private string GetRuntimeFromBundle(Stream stream)
        {
            var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

            var depsJsonEntry = zipArchive.Entries.FirstOrDefault(x => x.Name.EndsWith(".deps.json"));
            var json = new StreamReader(depsJsonEntry.Open()).ReadToEnd();
            var jobj = JsonConvert.DeserializeObject(json) as JObject;
            var runtimeTaget = jobj["runtimeTarget"] as JObject;
            var name = runtimeTaget["name"].ToString();

            return name.Split('/')[1];
        }
    }
}
