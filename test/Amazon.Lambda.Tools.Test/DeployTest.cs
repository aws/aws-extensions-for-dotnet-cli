﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
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
    public class DeployTest : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;

        string _roleArn;
        string _bucket;

        IAmazonS3 _s3Client;

        public DeployTest(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this._s3Client = new AmazonS3Client(RegionEndpoint.USEast1);

            this._bucket = "dotnet-lambda-tests-" + DateTime.Now.Ticks;
            this._roleArn = TestHelper.GetTestRoleArn();

            Task.Run(async () => 
            {
                await _s3Client.PutBucketAsync(this._bucket);
            }).Wait();

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
            command.Role = this._roleArn;
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp1.0";
            command.Runtime = "dotnetcore1.0";
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
        public async Task DeployWithPackage()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            string packageZip = Path.GetTempFileName() + ".zip";
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");

            var packageCommand = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            packageCommand.OutputPackageFileName = packageZip;
            packageCommand.Configuration = "Release";
            packageCommand.TargetFramework = "netcoreapp1.0";

            await packageCommand.ExecuteAsync();

            var deployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), Path.GetTempPath(), new string[0]);
            deployCommand.FunctionName = "test-function-" + DateTime.Now.Ticks;
            deployCommand.Handler = "TestFunction::TestFunction.Function::ToUpper";
            deployCommand.Timeout = 10;
            deployCommand.MemorySize = 512;
            deployCommand.Role = this._roleArn;
            deployCommand.Package = packageZip;
            deployCommand.Runtime = "dotnetcore1.0";
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
            command.S3Bucket = this._bucket;
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
                renameParameterCommand.S3Bucket = this._bucket;
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
                initialDeployCommand.Role = this._roleArn;
                initialDeployCommand.Configuration = "Release";
                initialDeployCommand.TargetFramework = "netcoreapp1.0";
                initialDeployCommand.Runtime = "dotnetcore1.0";
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
                    redeployCommand.TargetFramework = "netcoreapp1.0";
                    redeployCommand.Runtime = "dotnetcore1.0";
                    redeployCommand.DisableInteractive = true;

                    var redeployed = await redeployCommand.ExecuteAsync();
                    Assert.True(redeployed);

                    funcConfig = await initialDeployCommand.LambdaClient.GetFunctionConfigurationAsync(initialDeployCommand.FunctionName);
                    Assert.Equal(queueArn, funcConfig.DeadLetterConfig?.TargetArn);

                    redeployCommand = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
                    redeployCommand.FunctionName = initialDeployCommand.FunctionName;
                    redeployCommand.Configuration = "Release";
                    redeployCommand.TargetFramework = "netcoreapp1.0";
                    redeployCommand.Runtime = "dotnetcore1.0";
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
                command.TargetFramework = "netcoreapp1.0";
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
            command.S3Bucket = this._bucket;
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
            command.TargetFramework = "netcoreapp2.0";
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
            command.TargetFramework = "netcoreapp2.0";
            command.CloudFormationTemplate = "large-serverless.template";
            command.StackName = "TestDeployLargeServerless-" + DateTime.Now.Ticks;
            command.S3Bucket = this._bucket;

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
