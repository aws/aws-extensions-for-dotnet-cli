using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
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
    public class LayerTests : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;

        string _bucket;

        IAmazonS3 _s3Client;
        IAmazonLambda _lambdaClient;

        string _singleLayerFunctionPath = Path.GetFullPath(Path.GetDirectoryName(typeof(LayerTests).GetTypeInfo().Assembly.Location) + "../../../../../../testapps/TestLayerExample");


        public LayerTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;

            this._s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            this._lambdaClient = new AmazonLambdaClient(RegionEndpoint.USEast1);

            this._bucket = "dotnet-lambda-layer-tests-" + DateTime.Now.Ticks;

            Task.Run(async () =>
            {
                await _s3Client.PutBucketAsync(this._bucket);
            }).Wait();

        }


        [Fact]
        public async Task CreateLayer()
        {
            var logger = new TestToolLogger(_testOutputHelper);

            var command = new PublishLayerCommand(logger, _singleLayerFunctionPath, new string[0]);
            command.Region = "us-east-1";
            command.TargetFramework = "netcoreapp2.1";
            command.S3Bucket = this._bucket;
            command.DisableInteractive = true;
            command.LayerName = "DotnetTest-CreateLayer";
            command.LayerType = LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE;
            command.PackageManifest = _singleLayerFunctionPath;
             
            try
            {
                Assert.True(await command.ExecuteAsync());
                Assert.NotNull(command.NewLayerVersionArn);

                var getResponse = await this._lambdaClient.GetLayerVersionAsync(new GetLayerVersionRequest {LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                Assert.NotNull(getResponse.Description);

                
                var data = JsonMapper.ToObject<LayerDescriptionManifest>(getResponse.Description);
                Assert.Equal(LayerDescriptionManifest.ManifestType.RuntimePackageStore, data.Type);
                Assert.NotNull(data.Dir);
                Assert.Equal(this._bucket, data.Buc);
                Assert.NotNull(data.Key);


                using (var getManifestResponse = await this._s3Client.GetObjectAsync(data.Buc, data.Key))
                using(var reader = new StreamReader(getManifestResponse.ResponseStream))
                {
                    var xml = await reader.ReadToEndAsync();
                    Assert.Contains("AWSSDK.S3", xml);
                    Assert.Contains("Amazon.Lambda.Core", xml);
                }                
            }
            finally
            {
                await this._lambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
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
            command.S3Bucket = this._bucket;
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

                    var getResponse = await this._lambdaClient.GetLayerVersionAsync(new GetLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                    Assert.NotNull(getResponse.Description);


                    var data = JsonMapper.ToObject<LayerDescriptionManifest>(getResponse.Description);
                    Assert.Equal(LayerDescriptionManifest.ManifestType.RuntimePackageStore, data.Type);
                    Assert.NotNull(data.Dir);
                    Assert.Equal(this._bucket, data.Buc);
                    Assert.NotNull(data.Key);


                    using (var getManifestResponse = await this._s3Client.GetObjectAsync(data.Buc, data.Key))
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
                    await this._lambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = command.NewLayerArn, VersionNumber = command.NewLayerVersionNumber });
                }
            }

        }

        [Fact]
        public async Task DeployFunctionWithLayer()
        {
            var logger = new TestToolLogger(_testOutputHelper);

            var publishLayerCommand = await PublishLayerAsync();

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


                    var getConfigResponse = await this._lambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = deployCommand.FunctionName });
                    Assert.NotNull(getConfigResponse.Layers.FirstOrDefault(x => string.Equals(x.Arn, publishLayerCommand.NewLayerVersionArn)));

                    var getCodeResponse = await this._lambdaClient.GetFunctionAsync(deployCommand.FunctionName);
                    using (var client = new HttpClient())
                    {
                        var data = await client.GetByteArrayAsync(getCodeResponse.Code.Location);
                        var zipArchive = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read);

                        Assert.NotNull(zipArchive.GetEntry("TestLayerExample.dll"));
                        Assert.Null(zipArchive.GetEntry("Amazon.Lambda.Core.dll"));
                        Assert.Null(zipArchive.GetEntry("AWSSDK.S3.dll"));
                    }

                    var redeployLayerCommand = await PublishLayerAsync();

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
                    await this._lambdaClient.DeleteFunctionAsync(new DeleteFunctionRequest { FunctionName = deployCommand.FunctionName });
                }
            }
            finally
            {
                await this._lambdaClient.DeleteLayerVersionAsync(new DeleteLayerVersionRequest { LayerName = publishLayerCommand.NewLayerArn, VersionNumber = publishLayerCommand.NewLayerVersionNumber });
            }
        }

        private async Task ValidateInvokeAsync(string functionName, string payload, string expectedResult)
        {
            var invokeResponse = await this._lambdaClient.InvokeAsync(new InvokeRequest
            {
                InvocationType = InvocationType.RequestResponse,
                FunctionName = functionName,
                Payload = payload,
                LogType = LogType.Tail
            });

            var content = new StreamReader(invokeResponse.Payload).ReadToEnd();
            Assert.Equal(expectedResult, content);
        }

        private async Task<PublishLayerCommand> PublishLayerAsync()
        {
            var logger = new TestToolLogger(_testOutputHelper);
            
            var publishLayerCommand = new PublishLayerCommand(logger, _singleLayerFunctionPath, new string[0]);
            publishLayerCommand.Region = "us-east-1";
            publishLayerCommand.TargetFramework = "netcoreapp2.1";
            publishLayerCommand.S3Bucket = this._bucket;
            publishLayerCommand.DisableInteractive = true;
            publishLayerCommand.LayerName = "Dotnet-IntegTest-";
            publishLayerCommand.LayerType = LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE;
            publishLayerCommand.PackageManifest = _singleLayerFunctionPath;

            if(!(await publishLayerCommand.ExecuteAsync()))
            {
                throw publishLayerCommand.LastToolsException;
            }

            return publishLayerCommand;
        }


        #region IDisposable Support
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AmazonS3Util.DeleteS3BucketWithObjectsAsync(this._s3Client, this._bucket).Wait();

                    this._s3Client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
