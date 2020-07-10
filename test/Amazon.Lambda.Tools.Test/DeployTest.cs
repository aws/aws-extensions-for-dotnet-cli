using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xunit;
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
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    public class DeployTest : IClassFixture<DeployTestFixture>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        DeployTestFixture _testFixture;

        public DeployTest(DeployTestFixture testFixture, ITestOutputHelper testOutputHelper)
        {
            this._testFixture = testFixture;
            this._testOutputHelper = testOutputHelper;
        }



        [Fact]
        public async Task RunDeployCommand()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var command = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Role = TestHelper.GetTestRoleArn();
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp2.1";
            command.Runtime = "dotnetcore2.1";
            command.DisableInteractive = true;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = command.FunctionName,
                    LogType = LogType.Tail,
                    Payload = "\"hello world\""
                };
                var response = await command.LambdaClient.InvokeAsync(invokeRequest);

                var payload = new StreamReader(response.Payload).ReadToEnd();
                Assert.Equal("\"HELLO WORLD\"", payload);
            }
            finally
            {
                if (created)
                {
                    await command.LambdaClient.DeleteFunctionAsync(command.FunctionName);
                }
            }
        }
        
        [Fact]
        public async Task TestPowerShellLambdaParallelTestCommand()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestPowerShellParallelTest");
            var command = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Role = TestHelper.GetTestRoleArn();
            command.Configuration = "Release";
            command.S3Bucket = this._testFixture.Bucket;
            command.S3Prefix = "TestPowerShellParallelTest/";
            command.Region = "us-east-1";
            command.DisableInteractive = true;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = command.FunctionName,
                    LogType = LogType.Tail,
                    Payload = "{}"
                };
                var response = await command.LambdaClient.InvokeAsync(invokeRequest);

                var logTail = Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
                
                Assert.Equal(200, response.StatusCode);
                Assert.Contains("Running against: 1 for SharedVariable: Hello Shared Variable", logTail);
                Assert.Contains("Running against: 10 for SharedVariable: Hello Shared Variable", logTail);
            }
            finally
            {
                if (created)
                {
                    await command.LambdaClient.DeleteFunctionAsync(command.FunctionName);
                }
            }
        }        

        [Fact]
        public async Task RunDeployCommandWithCustomConfigAndProjectLocation()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location));
            var command = new DeployFunctionCommand(new ConsoleToolLogger(), fullPath, 
                new string[] {"--config-file", "custom-config.json", "--project-location", "../../../../../testapps/TestFunction" });
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.Role = TestHelper.GetTestRoleArn();
            command.Configuration = "Release";
            command.Runtime = "dotnetcore2.1";
            command.DisableInteractive = true;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = command.FunctionName,
                    LogType = LogType.Tail,
                    Payload = "\"hello world\""
                };
                var response = await command.LambdaClient.InvokeAsync(invokeRequest);

                var payload = new StreamReader(response.Payload).ReadToEnd();
                Assert.Equal("\"HELLO WORLD\"", payload);

                var log = UTF8Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
                Assert.Contains("Memory Size: 320 MB", log);
            }
            finally
            {
                if (created)
                {
                    await command.LambdaClient.DeleteFunctionAsync(command.FunctionName);
                }
            }
        }

        [Fact]
        public async Task DeployWithPackage()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            string packageZip = Path.GetTempFileName() + ".zip";
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");

            var packageCommand = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            packageCommand.OutputPackageFileName = packageZip;
            packageCommand.Configuration = "Release";

            await packageCommand.ExecuteAsync();

            var deployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), Path.GetTempPath(), new string[0]);
            deployCommand.FunctionName = "test-function-" + DateTime.Now.Ticks;
            deployCommand.Handler = "TestFunction::TestFunction.Function::ToUpper";
            deployCommand.Timeout = 10;
            deployCommand.MemorySize = 512;
            deployCommand.Role = TestHelper.GetTestRoleArn();
            deployCommand.Package = packageZip;
            deployCommand.Runtime = "dotnetcore2.1";
            deployCommand.Region = "us-east-1";
            deployCommand.DisableInteractive = true;

            var created = await deployCommand.ExecuteAsync();
            try
            {
                Assert.True(created);

                var invokeRequest = new InvokeRequest
                {
                    FunctionName = deployCommand.FunctionName,
                    LogType = LogType.Tail,
                    Payload = "\"hello world\""
                };
                var response = await deployCommand.LambdaClient.InvokeAsync(invokeRequest);

                var payload = new StreamReader(response.Payload).ReadToEnd();
                Assert.Equal("\"HELLO WORLD\"", payload);
            }
            finally
            {
                if(File.Exists(packageZip))
                {
                    File.Delete(packageZip);
                }

                if (created)
                {
                    await deployCommand.LambdaClient.DeleteFunctionAsync(deployCommand.FunctionName);
                }
            }

        }


        [Fact]
        public async Task RunYamlServerlessDeployCommand()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ServerlessWithYamlFunction");
            var command = new DeployServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[] { "--template-parameters", "Environment=whatever" });
            command.StackName = "ServerlessYamlStackTest-" + DateTime.Now.Ticks;
            command.S3Bucket = this._testFixture.Bucket;
            command.WaitForStackToComplete = true;
            command.DisableInteractive = true;
            command.ProjectLocation = fullPath;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                // Test if a redeployment happens with different template parameters it works.
                var renameParameterCommand = new DeployServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[] { "--template-parameters", "EnvironmentRename=whatever" });
                renameParameterCommand.StackName = command.StackName;
                renameParameterCommand.S3Bucket = this._testFixture.Bucket;
                renameParameterCommand.WaitForStackToComplete = true;
                renameParameterCommand.DisableInteractive = true;
                renameParameterCommand.ProjectLocation = fullPath;
                renameParameterCommand.CloudFormationTemplate = "rename-params-template.yaml";

                var updated = await renameParameterCommand.ExecuteAsync();
                Assert.True(updated);
            }
            finally
            {
                if (created)
                {
                    var deleteCommand = new DeleteServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    deleteCommand.StackName = command.StackName;
                    await deleteCommand.ExecuteAsync();
                }
            }
        }

        [Fact]
        public async Task FixIssueOfDLQBeingCleared()
        {
            var sqsClient = new AmazonSQSClient(RegionEndpoint.USEast2);

            var queueUrl = (await sqsClient.CreateQueueAsync("lambda-test-" + DateTime.Now.Ticks)).QueueUrl;
            var queueArn = (await sqsClient.GetQueueAttributesAsync(queueUrl, new List<string> { "QueueArn" })).QueueARN;
            try
            {

                var assembly = this.GetType().GetTypeInfo().Assembly;

                var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
                var initialDeployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                initialDeployCommand.FunctionName = "test-function-" + DateTime.Now.Ticks;
                initialDeployCommand.Handler = "TestFunction::TestFunction.Function::ToUpper";
                initialDeployCommand.Timeout = 10;
                initialDeployCommand.MemorySize = 512;
                initialDeployCommand.Role = TestHelper.GetTestRoleArn();
                initialDeployCommand.Configuration = "Release";
                initialDeployCommand.TargetFramework = "netcoreapp2.1";
                initialDeployCommand.Runtime = "dotnetcore2.1";
                initialDeployCommand.DeadLetterTargetArn = queueArn;
                initialDeployCommand.DisableInteractive = true;


                var created = await initialDeployCommand.ExecuteAsync();
                try
                {
                    Assert.True(created);

                    var funcConfig = await initialDeployCommand.LambdaClient.GetFunctionConfigurationAsync(initialDeployCommand.FunctionName);
                    Assert.Equal(queueArn, funcConfig.DeadLetterConfig?.TargetArn);

                    var redeployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    redeployCommand.FunctionName = initialDeployCommand.FunctionName;
                    redeployCommand.Configuration = "Release";
                    redeployCommand.TargetFramework = "netcoreapp2.1";
                    redeployCommand.Runtime = "dotnetcore2.1";
                    redeployCommand.DisableInteractive = true;

                    var redeployed = await redeployCommand.ExecuteAsync();
                    Assert.True(redeployed);

                    funcConfig = await initialDeployCommand.LambdaClient.GetFunctionConfigurationAsync(initialDeployCommand.FunctionName);
                    Assert.Equal(queueArn, funcConfig.DeadLetterConfig?.TargetArn);

                    redeployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    redeployCommand.FunctionName = initialDeployCommand.FunctionName;
                    redeployCommand.Configuration = "Release";
                    redeployCommand.TargetFramework = "netcoreapp2.1";
                    redeployCommand.Runtime = "dotnetcore2.1";
                    redeployCommand.DeadLetterTargetArn = "";
                    redeployCommand.DisableInteractive = true;

                    redeployed = await redeployCommand.ExecuteAsync();
                    Assert.True(redeployed);

                    funcConfig = await initialDeployCommand.LambdaClient.GetFunctionConfigurationAsync(initialDeployCommand.FunctionName);
                    Assert.Null(funcConfig.DeadLetterConfig?.TargetArn);
                }
                finally
                {
                    if (created)
                    {
                        await initialDeployCommand.LambdaClient.DeleteFunctionAsync(initialDeployCommand.FunctionName);
                    }
                }
            }
            finally
            {
                await sqsClient.DeleteQueueAsync(queueUrl);
            }
        }

        [Fact]
        public async Task DeployStepFunctionWithTemplateSubstitution()
        {
            var cfClient = new AmazonCloudFormationClient(RegionEndpoint.USEast2);
            var s3Client = new AmazonS3Client(RegionEndpoint.USEast2);

            var bucketName = "deploy-step-functions-" + DateTime.Now.Ticks;
            await s3Client.PutBucketAsync(bucketName);
            try
            {

                var logger = new TestToolLogger();
                var assembly = this.GetType().GetTypeInfo().Assembly;

                var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TemplateSubstitutionTestProjects/StateMachineDefinitionStringTest");
                var command = new DeployServerlessCommand(logger, fullPath, new string[0]);
                command.DisableInteractive = true;
                command.Configuration = "Release";
                command.TargetFramework = "netcoreapp2.1";
                command.StackName = "DeployStepFunctionWithTemplateSubstitution-" + DateTime.Now.Ticks;
                command.S3Bucket = bucketName;
                command.WaitForStackToComplete = true;

                command.TemplateParameters = new Dictionary<string, string> { { "NonExisting", "Parameter" }, { "StubParameter", "SecretFoo" } };

                var created = await command.ExecuteAsync();
                try
                {
                    Assert.True(created);

                    var describeResponse = await cfClient.DescribeStacksAsync(new DescribeStacksRequest
                    {
                        StackName = command.StackName
                    });

                    Assert.Equal(StackStatus.CREATE_COMPLETE, describeResponse.Stacks[0].StackStatus);

                    Assert.DoesNotContain("SecretFoo", logger.Buffer.ToString());
                    Assert.Contains("****", logger.Buffer.ToString());
                }
                finally
                {
                    if (created)
                    {
                        try
                        {
                            var deleteCommand = new DeleteServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                            deleteCommand.StackName = command.StackName;
                            await deleteCommand.ExecuteAsync();
                        }
                        catch
                        {
                            // Bury exception because we don't want to lose any exceptions during the deploy stage.
                        }
                    }
                }
            }
            finally
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(s3Client, bucketName);
            }
        }

        [Fact]
        public void ValidateCompatibleLambdaRuntimesAndTargetFrameworks()
        {
            // Validate that newer versions of the framework then what the current and possible future lambda runtimes throw an error.
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.0", "netcoreapp1.0");
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.1", "netcoreapp1.0");
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.1", "netcoreapp1.1");
            LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore2.0", "netcoreapp1.0");
            Assert.Throws(typeof(LambdaToolsException), (() => LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.0", "netcoreapp1.1")));
            Assert.Throws(typeof(LambdaToolsException), (() => LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.0", "netcoreapp2.0")));
            Assert.Throws(typeof(LambdaToolsException), (() => LambdaUtilities.ValidateTargetFrameworkAndLambdaRuntime("dotnetcore1.1", "netcoreapp2.0")));
        }


        [Fact]
        public async Task DeployMultiProject()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/MPDeployServerless/CurrentDirectoryTest");
            var command = new DeployServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[] { });
            command.StackName = "DeployMultiProject-" + DateTime.Now.Ticks;
            command.S3Bucket = this._testFixture.Bucket;
            command.WaitForStackToComplete = true;
            command.DisableInteractive = true;
            command.ProjectLocation = fullPath;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                using (var cfClient = new AmazonCloudFormationClient(RegionEndpoint.USEast1))
                {
                    var stack = (await cfClient.DescribeStacksAsync(new DescribeStacksRequest { StackName = command.StackName })).Stacks[0];
                    var apiUrl = stack.Outputs.FirstOrDefault(x => string.Equals(x.OutputKey, "ApiURL"))?.OutputValue;
                    Assert.NotNull(apiUrl);

                    Assert.Equal("CurrentProjectTest", await GetRestContent(apiUrl, "current"));
                    Assert.Equal("SecondCurrentProjectTest", await GetRestContent(apiUrl, "current2"));
                    Assert.Equal("SiblingProjectTest", await GetRestContent(apiUrl, "sibling"));
                    Assert.Equal("SingleFileNodeFunction", await GetRestContent(apiUrl, "singlenode"));
                    Assert.Equal("DirectoryNodeFunction", await GetRestContent(apiUrl, "directorynode"));
                }

            }
            finally
            {
                if (created)
                {
                    var deleteCommand = new DeleteServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    deleteCommand.StackName = command.StackName;
                    await deleteCommand.ExecuteAsync();
                }
            }
        }


        [Fact]
        public async Task TestServerlessPackage()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new PackageCICommand(logger, fullPath, new string[0]);
            command.Region = "us-west-2";
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp2.1";
            command.CloudFormationTemplate = "serverless.template";
            command.CloudFormationOutputTemplate = Path.Combine(Path.GetTempPath(),  "output-serverless.template");
            command.S3Bucket = "serverless-package-test-" + DateTime.Now.Ticks;
            command.DisableInteractive = true;

            if (File.Exists(command.CloudFormationOutputTemplate))
                File.Delete(command.CloudFormationOutputTemplate);


            await command.S3Client.PutBucketAsync(command.S3Bucket);
            try
            {
                Assert.True(await command.ExecuteAsync());
                Assert.True(File.Exists(command.CloudFormationOutputTemplate));
            }
            finally
            {
                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(command.S3Client, command.S3Bucket);
            }

        }

        [Fact]
        public async Task TestDeployLargeServerless()
        {
            var logger = new TestToolLogger();
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");
            var command = new DeployServerlessCommand(logger, fullPath, new string[0]);
            command.Region = "us-east-1";
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp2.1";
            command.CloudFormationTemplate = "large-serverless.template";
            command.StackName = "TestDeployLargeServerless-" + DateTime.Now.Ticks;
            command.S3Bucket = this._testFixture.Bucket;

            command.WaitForStackToComplete = true;
            command.ProjectLocation = fullPath;
            command.DisableInteractive = true;



            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);
            }
            finally
            {
                if (created)
                {
                    var deleteCommand = new DeleteServerlessCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    deleteCommand.StackName = command.StackName;
                    deleteCommand.Region = command.Region;
                    deleteCommand.DisableInteractive = true;
                    await deleteCommand.ExecuteAsync();
                }
            }

        }


        public static async Task<string> GetRestContent(string basePath, string resourcePath)
        {
            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(basePath), resourcePath);
                var content = await client.GetStringAsync(uri);
                return content;
            }
        }

    }

    public class DeployTestFixture : IDisposable
    {
        public string Bucket { get; set; }
        public IAmazonS3 S3Client { get; set; }

        public DeployTestFixture()
        {
            this.S3Client = new AmazonS3Client(RegionEndpoint.USEast1);

            this.Bucket = "dotnet-lambda-tests-" + DateTime.Now.Ticks;

            Task.Run(async () =>
            {
                await S3Client.PutBucketAsync(this.Bucket);
            }).Wait();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AmazonS3Util.DeleteS3BucketWithObjectsAsync(this.S3Client, this.Bucket).GetAwaiter().GetResult();

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
