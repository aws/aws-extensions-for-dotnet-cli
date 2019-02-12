using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using Amazon.SQS;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Common.DotNetCli.Tools;

using ThirdParty.Json.LitJson;
using System.Runtime.InteropServices;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    public class LayerTests : IClassFixture<LayerTestsFixture>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        static string _singleLayerFunctionPath = Path.GetFullPath(Path.GetDirectoryName(typeof(LayerTests).GetTypeInfo().Assembly.Location) + "../../../../../../testapps/TestLayerExample");
        static string _serverlessLayerFunctionPath = Path.GetFullPath(Path.GetDirectoryName(typeof(LayerTests).GetTypeInfo().Assembly.Location) + "../../../../../../testapps/TestLayerServerless");

        LayerTestsFixture _testFixture;

        public LayerTests(LayerTestsFixture testFixture, ITestOutputHelper testOutputHelper)
        {
            this._testFixture = testFixture;
            this._testOutputHelper = testOutputHelper;
        }


        [Fact]
        public async Task CreateLayer()
        {
            var logger = new TestToolLogger(_testOutputHelper);

            var command = new PublishLayerCommand(logger, _singleLayerFunctionPath, new string[0]);
            command.Region = "us-east-1";
            command.TargetFramework = "netcoreapp2.1";
            command.S3Bucket = this._testFixture.Bucket;
            command.DisableInteractive = true;
            command.LayerName = "DotnetTest-CreateLayer";
            command.LayerType = LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE;
            command.PackageManifest = _singleLayerFunctionPath;
             
            try
            {
                Assert.True(await command.ExecuteAsync());
                Assert.NotNull(command.NewLayerVersionArn);

                var getResponse = await this._testFixture.LambdaClient.GetLayerVersionAsync(new GetLayerVersionRequest {LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                Assert.NotNull(getResponse.Description);

                
                var data = JsonMapper.ToObject<LayerDescriptionManifest>(getResponse.Description);
                Assert.Equal(LayerDescriptionManifest.ManifestType.RuntimePackageStore, data.Nlt);
                Assert.NotNull(data.Dir);
                Assert.Equal(this._testFixture.Bucket, data.Buc);
                Assert.NotNull(data.Key);


                using (var getManifestResponse = await this._testFixture.S3Client.GetObjectAsync(data.Buc, data.Key))
                using(var reader = new StreamReader(getManifestResponse.ResponseStream))
                {
                    var xml = await reader.ReadToEndAsync();
                    Assert.Contains("AWSSDK.S3", xml);
                    Assert.Contains("Amazon.Lambda.Core", xml);
                }                
            }
            finally
            {
                await this._testFixture.LambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
            }

        }

        [Fact]
        public async Task AttemptToCreateAnOptmizedLayer()
        {
            var logger = new TestToolLogger(_testOutputHelper);
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestLayerExample");
            var command = new PublishLayerCommand(logger, fullPath, new string[] { "--enable-package-optimization", "true"});
            command.Region = "us-east-1";
            command.TargetFramework = "netcoreapp2.1";
            command.S3Bucket = this._testFixture.Bucket;
            command.DisableInteractive = true;
            command.LayerName = "DotnetTest-AttemptToCreateAnOptmizedLayer";
            command.LayerType = LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE;
            command.PackageManifest = fullPath;

            bool success = false;
            try
            {
                success = await command.ExecuteAsync();
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Assert.False(success);
                    Assert.Equal(LambdaToolsException.LambdaErrorCode.UnsupportedOptimizationPlatform.ToString(), command.LastToolsException.Code);
                }
                else
                {
                    Assert.True(success);
                    Assert.NotNull(command.NewLayerVersionArn);

                    var getResponse = await this._testFixture.LambdaClient.GetLayerVersionAsync(new GetLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                    Assert.NotNull(getResponse.Description);


                    var data = JsonMapper.ToObject<LayerDescriptionManifest>(getResponse.Description);
                    Assert.Equal(LayerDescriptionManifest.ManifestType.RuntimePackageStore, data.Nlt);
                    Assert.NotNull(data.Dir);
                    Assert.Equal(this._testFixture.Bucket, data.Buc);
                    Assert.NotNull(data.Key);


                    using (var getManifestResponse = await this._testFixture.S3Client.GetObjectAsync(data.Buc, data.Key))
                    using (var reader = new StreamReader(getManifestResponse.ResponseStream))
                    {
                        var xml = await reader.ReadToEndAsync();
                        Assert.Contains("AWSSDK.S3", xml);
                        Assert.Contains("Amazon.Lambda.Core", xml);
                    }
                }
            }
            finally
            {
                if(success)
                {
                    await this._testFixture.LambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                }
            }

        }

        [Theory]
        [InlineData("")]
        [InlineData("nuget-store")]
        public async Task DeployFunctionWithLayer(string optDirectory)
        {
            var logger = new TestToolLogger(_testOutputHelper);

            var publishLayerCommand = await PublishLayerAsync(_singleLayerFunctionPath, optDirectory);

            try
            {
                Assert.NotNull(publishLayerCommand.NewLayerVersionArn);


                var deployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), _singleLayerFunctionPath, new string[0]);
                deployCommand.FunctionName = "test-layer-function-" + DateTime.Now.Ticks;
                deployCommand.Region = publishLayerCommand.Region;
                deployCommand.Handler = "TestLayerExample::TestLayerExample.Function::FunctionHandler";
                deployCommand.Timeout = 10;
                deployCommand.MemorySize = 512;
                deployCommand.Role = TestHelper.GetTestRoleArn();
                deployCommand.Configuration = "Release";
                deployCommand.TargetFramework = "netcoreapp2.1";
                deployCommand.Runtime = "dotnetcore2.1";
                deployCommand.LayerVersionArns = new string[] { publishLayerCommand.NewLayerVersionArn };
                deployCommand.DisableInteractive = true;

                var created = await deployCommand.ExecuteAsync();
                try
                {
                    // See if we got back the return which proves we were able to load an assembly from the S3 NuGet package
                    // return new Amazon.S3.Model.ListBucketsRequest().ToString();
                    await ValidateInvokeAsync(deployCommand.FunctionName, "\"TEST\"", "\"Amazon.S3.Model.ListBucketsRequest\"");


                    var getConfigResponse = await this._testFixture.LambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = deployCommand.FunctionName });
                    Assert.NotNull(getConfigResponse.Layers.FirstOrDefault(x => string.Equals(x.Arn, publishLayerCommand.NewLayerVersionArn)));
                    
                    if(!string.IsNullOrEmpty(optDirectory))
                    {
                        var dotnetSharedSource = getConfigResponse.Environment.Variables[LambdaConstants.ENV_DOTNET_SHARED_STORE];
                        Assert.Equal($"/opt/{optDirectory}/", dotnetSharedSource);
                    }

                    var getCodeResponse = await this._testFixture.LambdaClient.GetFunctionAsync(deployCommand.FunctionName);
                    using (var client = new HttpClient())
                    {
                        var data = await client.GetByteArrayAsync(getCodeResponse.Code.Location);
                        var zipArchive = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);

                        Assert.NotNull(zipArchive.GetEntry("TestLayerExample.dll"));
                        Assert.Null(zipArchive.GetEntry("Amazon.Lambda.Core.dll"));
                        Assert.Null(zipArchive.GetEntry("AWSSDK.S3.dll"));
                    }

                    var redeployLayerCommand = await PublishLayerAsync(_singleLayerFunctionPath, optDirectory);

                    var redeployFunctionCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), _singleLayerFunctionPath, new string[0]);
                    redeployFunctionCommand.FunctionName = deployCommand.FunctionName;
                    redeployFunctionCommand.Region = deployCommand.Region;
                    redeployFunctionCommand.Handler = deployCommand.Handler;
                    redeployFunctionCommand.Timeout = deployCommand.Timeout;
                    redeployFunctionCommand.MemorySize = deployCommand.MemorySize;
                    redeployFunctionCommand.Role = deployCommand.Role;
                    redeployFunctionCommand.Configuration = deployCommand.Configuration;
                    redeployFunctionCommand.TargetFramework = deployCommand.TargetFramework;
                    redeployFunctionCommand.Runtime = deployCommand.Runtime;
                    redeployFunctionCommand.LayerVersionArns = new string[] { redeployLayerCommand.NewLayerVersionArn };
                    redeployFunctionCommand.DisableInteractive = true;

                    if(!await redeployFunctionCommand.ExecuteAsync())
                    {
                        throw redeployFunctionCommand.LastToolsException;
                    }

                    await ValidateInvokeAsync(redeployFunctionCommand.FunctionName, "\"TEST\"", "\"Amazon.S3.Model.ListBucketsRequest\"");
                }
                finally
                {
                    await this._testFixture.LambdaClient.DeleteFunctionAsync(new DeleteFunctionRequest { FunctionName = deployCommand.FunctionName });
                }
            }
            finally
            {
                await this._testFixture.LambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = publishLayerCommand.NewLayerArn, VersionNumber = publishLayerCommand.NewLayerVersionNumber });
            }
        }

        [Fact]
        public async Task DeployServerlessWithlayer()
        {
            var logger = new TestToolLogger(_testOutputHelper);

            var templateTilePath = Path.Combine(_serverlessLayerFunctionPath, "serverless.template");
            if (File.Exists(templateTilePath))
            {
                File.Delete(templateTilePath);
            }

            var publishLayerCommand = await PublishLayerAsync(_singleLayerFunctionPath, "");            
            try
            {
                var templateContent = File.ReadAllText(Path.Combine(_serverlessLayerFunctionPath, "fake.template"));
                templateContent =
                    templateContent.Replace("LAYER_ARN_PLACEHOLDER", publishLayerCommand.NewLayerVersionArn);
                
                File.WriteAllText(templateTilePath, templateContent);
                
                var command = new DeployServerlessCommand(new TestToolLogger(_testOutputHelper), _serverlessLayerFunctionPath, new string[] { });
                command.DisableInteractive = true;
                command.StackName = "DeployServerlessWithlayer-" + DateTime.Now.Ticks;
                command.Region = publishLayerCommand.Region;
                command.S3Bucket = this._testFixture.Bucket;
                command.WaitForStackToComplete = true;
                command.DisableInteractive = true;
                command.ProjectLocation = _serverlessLayerFunctionPath;

                var created = await command.ExecuteAsync();
                try
                {
                    Assert.True(created);

                    var lambdaFunctionName =
                        await TestHelper.GetPhysicalCloudFormationResourceId(_testFixture.CFClient, command.StackName, "TheFunction");
                    
                    await ValidateInvokeAsync(lambdaFunctionName, "\"hello\"", "\"HELLO\"");

                    var getConfigResponse = await this._testFixture.LambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = lambdaFunctionName });
                    Assert.NotNull(getConfigResponse.Layers.FirstOrDefault(x => string.Equals(x.Arn, publishLayerCommand.NewLayerVersionArn)));
                    
                    var getCodeResponse = await this._testFixture.LambdaClient.GetFunctionAsync(lambdaFunctionName);
                    using (var client = new HttpClient())
                    {
                        var data = await client.GetByteArrayAsync(getCodeResponse.Code.Location);
                        var zipArchive = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);

                        Assert.NotNull(zipArchive.GetEntry("TestLayerServerless.dll"));
                        Assert.Null(zipArchive.GetEntry("Amazon.Lambda.Core.dll"));
                    }
                }
                finally
                {
                    if (created)
                    {
                        var deleteCommand = new DeleteServerlessCommand(new TestToolLogger(_testOutputHelper), _serverlessLayerFunctionPath, new string[0]);
                        deleteCommand.DisableInteractive = true;
                        deleteCommand.Region = publishLayerCommand.Region;                        
                        deleteCommand.StackName = command.StackName;
                        await deleteCommand.ExecuteAsync();
                    }
                }

            }
            finally
            {
                await this._testFixture.LambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = publishLayerCommand.NewLayerArn, VersionNumber = publishLayerCommand.NewLayerVersionNumber });
                if (File.Exists(templateTilePath))
                {
                    File.Delete(templateTilePath);
                }                
            }
        }

        [Fact]
        public void MakeSureDirectoryInDotnetSharedStoreValueOnce()
        {
            var info = new LayerPackageInfo();
            info.Items.Add(new LayerPackageInfo.LayerPackageInfoItem
            {
                Directory = LambdaConstants.DEFAULT_LAYER_OPT_DIRECTORY
            });
            info.Items.Add(new LayerPackageInfo.LayerPackageInfoItem
            {
                Directory = "Custom/Foo"
            });
            info.Items.Add(new LayerPackageInfo.LayerPackageInfoItem
            {
                Directory = LambdaConstants.DEFAULT_LAYER_OPT_DIRECTORY
            });

            var env = info.GenerateDotnetSharedStoreValue();
            var tokens = env.Split(':');

            Assert.Equal(2, tokens.Length);
            Assert.Equal(1, tokens.Count(x => x.Equals("/opt/" + LambdaConstants.DEFAULT_LAYER_OPT_DIRECTORY + "/")));
            Assert.Equal(1, tokens.Count(x => x.Equals("/opt/Custom/Foo/")));
        }

        private async Task ValidateInvokeAsync(string functionName, string payload, string expectedResult)
        {
            var invokeResponse = await this._testFixture.LambdaClient.InvokeAsync(new InvokeRequest
            {
                InvocationType = InvocationType.RequestResponse,
                FunctionName = functionName,
                Payload = payload,
                LogType = LogType.Tail
            });

            var content = new StreamReader(invokeResponse.Payload).ReadToEnd();
            Assert.Equal(expectedResult, content);
        }

        private async Task<PublishLayerCommand> PublishLayerAsync(string projectDirectory, string optDirectory)
        {
            var logger = new TestToolLogger(_testOutputHelper);
            
            var publishLayerCommand = new PublishLayerCommand(logger, projectDirectory, new string[0]);
            publishLayerCommand.Region = "us-east-1";
            publishLayerCommand.TargetFramework = "netcoreapp2.1";
            publishLayerCommand.S3Bucket = this._testFixture.Bucket;
            publishLayerCommand.DisableInteractive = true;
            publishLayerCommand.LayerName = "Dotnet-IntegTest-";
            publishLayerCommand.LayerType = LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE;
            publishLayerCommand.OptDirectory = optDirectory;
            publishLayerCommand.PackageManifest = _singleLayerFunctionPath;

            if(!(await publishLayerCommand.ExecuteAsync()))
            {
                throw publishLayerCommand.LastToolsException;
            }

            return publishLayerCommand;
        }
    }

    public class LayerTestsFixture : IDisposable
    {
        public string Bucket { get; set; }
        public IAmazonS3 S3Client { get; set; }
        public IAmazonLambda LambdaClient { get; set; }
        public IAmazonCloudFormation CFClient { get; set; }

        public LayerTestsFixture()
        {
            this.CFClient = new AmazonCloudFormationClient(RegionEndpoint.USEast1);
            this.S3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            this.LambdaClient = new AmazonLambdaClient(RegionEndpoint.USEast1);

            this.Bucket = "dotnet-lambda-layer-tests-" + DateTime.Now.Ticks;

            Task.Run(async () =>
            {
                await S3Client.PutBucketAsync(this.Bucket);

                // Wait for bucket to exist
                Thread.Sleep(10000);
            }).Wait();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AmazonS3Util.DeleteS3BucketWithObjectsAsync(this.S3Client, this.Bucket).Wait();

                    this.S3Client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
