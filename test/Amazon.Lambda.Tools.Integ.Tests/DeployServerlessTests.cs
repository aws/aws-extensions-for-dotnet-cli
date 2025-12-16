using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Lambda.Tools.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using static Amazon.Lambda.Tools.Integ.Tests.TestConstants;

namespace Amazon.Lambda.Tools.Integ.Tests
{
    public class DeployServerlessTests : IClassFixture<DeployTestFixture>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly DeployTestFixture _testFixture;

        public DeployServerlessTests(DeployTestFixture testFixture, ITestOutputHelper testOutputHelper)
        {
            this._testFixture = testFixture;
            this._testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("serverless-resource.template")]
        [InlineData("serverless-resource-arm.template")]
        public async Task TestImageFunctionServerlessTemplateExamples(string template)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = template;
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
            var outputRoot = JsonConvert.DeserializeObject(File.ReadAllText(command.CloudFormationOutputTemplate)) as JObject;

            {
                var useDefaultDockerFunction = outputRoot["Resources"]["UseDefaultDockerFunction"]["Properties"] as JObject;
                Assert.Contains("dkr.ecr", useDefaultDockerFunction["ImageUri"]?.ToString());
                Assert.Contains("aws-extensions-tests:usedefaultdockerfunction", useDefaultDockerFunction["ImageUri"]?.ToString());
                Assert.EndsWith("usedefaultdocker", useDefaultDockerFunction["ImageUri"]?.ToString());
            }

            {
                var useDockerMetadata = outputRoot["Resources"]["UseDockerMetadataFunction"]["Properties"] as JObject;
                Assert.Contains("dkr.ecr", useDockerMetadata["ImageUri"]?.ToString());
                Assert.Contains("aws-extensions-tests:usedockermetadatafunction", useDockerMetadata["ImageUri"]?.ToString());
                Assert.EndsWith("usedockermetadata", useDockerMetadata["ImageUri"]?.ToString());
            }
        }

        // Test confirming fix for issue https://github.com/aws/aws-extensions-for-dotnet-cli/issues/414
        // Test confirming fix for https://github.com/aws/aws-lambda-dotnet/issues/2230 allowing comments in the config file
        [Fact]
        public async Task TestSettingConfigFile()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestServerlessWebApp");

            var command = new PackageCICommand(toolLogger, fullPath, new string[] {"--config-file", "dummy-config.json" });
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
        }

        [Fact]
        public async Task TestMissingImageTagServerlessMetadata()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless-function-missing-image-tag.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);

            var outputRoot = JsonConvert.DeserializeObject(File.ReadAllText(command.CloudFormationOutputTemplate)) as JObject;
            {
                var useDefaultDockerFunction = outputRoot["Resources"]["UseDockerMetadataFunction"]["Properties"] as JObject;
                Assert.Contains("dkr.ecr", useDefaultDockerFunction["ImageUri"]?.ToString());
                Assert.Contains("aws-extensions-tests:usedockermetadatafunction", useDefaultDockerFunction["ImageUri"]?.ToString());
                Assert.EndsWith("usedockermetadata", useDefaultDockerFunction["ImageUri"]?.ToString());
            }
        }

        [Fact]
        public async Task TestMissingDockerFileServerlessMetadata()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless-function-missing-dockerfile.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.False(created);
            Assert.Contains("Error failed to find file ", toolLogger.Buffer);
        }

        [Fact]
        public async Task TestLambdaResource()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "lambda-resource.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
            var outputRoot = JsonConvert.DeserializeObject(File.ReadAllText(command.CloudFormationOutputTemplate)) as JObject;

            {
                var useDefaultDockerFunction = outputRoot["Resources"]["UseDefaultDockerLambdaFunction"]["Properties"] as JObject;
                Assert.Contains("dkr.ecr", useDefaultDockerFunction["Code"]["ImageUri"]?.ToString());
                Assert.Contains("aws-extensions-tests:usedefaultdocker", useDefaultDockerFunction["Code"]["ImageUri"]?.ToString());
            }
        }

        [Fact]
        public async Task TestDockerBuildArgsMetadataJsonTemplate()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless-resource-dockerbuildargs-json.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
            Assert.Contains("--build-arg PROJECT_PATH=/src/path-to/project", toolLogger.Buffer);
            Assert.Contains("--build-arg PROJECT_FILE=project.csproj", toolLogger.Buffer);
        }

        [Fact]
        public async Task TestDockerBuildArgsMetadataYamlTemplate()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless-resource-dockerbuildargs-yaml.template";
            command.CloudFormationOutputTemplate = Path.GetTempFileName();

            var created = await command.ExecuteAsync();
            Assert.True(created);
            Assert.Contains("--build-arg PROJECT_PATH=/src/path-to/project", toolLogger.Buffer);
            Assert.Contains("--build-arg PROJECT_FILE=project.csproj", toolLogger.Buffer);
        }

        [Fact]
        public async Task TestDeployServerlessECRImageUriNoMetadataYamlTemplate()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var toolLogger = new TestToolLogger(_testOutputHelper);
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/TestSimpleImageProject");

            var pushImageCommand = new PushDockerImageCommand(toolLogger, fullPath, new string[0])
            {
                DisableInteractive = true,
                Region = TEST_REGION,
                WorkingDirectory = fullPath,
                SkipPushToECR = false,

                PushDockerImageProperties = new BasePushDockerImageCommand<LambdaToolsDefaults>.PushDockerImagePropertyContainer
                {
                    DockerImageTag = $"{TEST_ECR_REPOSITORY}:deployserverlessimageurinometadata",
                }
            };

            var pushImageResult = await pushImageCommand.ExecuteAsync();
            Assert.Contains("Pushing image to ECR repository", toolLogger.Buffer);
            Assert.Contains($"{TEST_ECR_REPOSITORY}:deployserverlessimageurinometadata Push Complete.", toolLogger.Buffer);

            string functionName = $"HelloWorldFunction{DateTime.Now.Ticks}";
            string yamlTemplate = @$"
AWSTemplateFormatVersion: 2010-09-09
Transform: 'AWS::Serverless-2016-10-31'
Description: An AWS Serverless Application.
Resources:
  {functionName}:
    Type: 'AWS::Serverless::Function'
    Properties:
      FunctionName: {functionName}
      MemorySize: 256
      Timeout: 30
      Policies:
        - AWSLambdaBasicExecutionRole
      PackageType: Image
      ImageUri: {pushImageCommand.PushedImageUri}
      ImageConfig:
        Command: ['TestSimpleImageProject::TestSimpleImageProject.Function::FunctionHandler']
Outputs: {{}}
            ";
            var tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, yamlTemplate);
            var deployServerlessCommand = new DeployServerlessCommand(toolLogger, fullPath, new string[] { "--template", tempFileName });
            deployServerlessCommand.DisableInteractive = true;
            deployServerlessCommand.Region = TEST_REGION;
            deployServerlessCommand.Configuration = "Release";
            deployServerlessCommand.StackName = functionName;
            deployServerlessCommand.S3Bucket = this._testFixture.Bucket;
            deployServerlessCommand.WaitForStackToComplete = true;

            var created = false;
            try
            {
                created = await deployServerlessCommand.ExecuteAsync();
                Assert.True(created);
                using (var cfClient = new AmazonCloudFormationClient(RegionEndpoint.GetBySystemName(TEST_REGION)))
                {
                    var describeResponse = await cfClient.DescribeStacksAsync(new DescribeStacksRequest
                    {
                        StackName = deployServerlessCommand.StackName
                    });

                    Assert.Equal(StackStatus.CREATE_COMPLETE, describeResponse.Stacks[0].StackStatus);
                }

                toolLogger.ClearBuffer();
                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = functionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);
            }
            finally
            {
                if (created)
                {
                    try
                    {
                        var deleteCommand = new DeleteServerlessCommand(toolLogger, fullPath, new string[0]);
                        deleteCommand.StackName = deployServerlessCommand.StackName;
                        deleteCommand.Region = TEST_REGION;
                        await deleteCommand.ExecuteAsync();
                    }
                    catch
                    {
                        // Bury exception because we don't want to lose any exceptions during the deploy stage.
                    }
                }

                File.Delete(tempFileName);
            }
        }

        [Fact]
        public async Task TestDeployServerlessReferencingSingleFile()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var toolLogger = new TestToolLogger(_testOutputHelper);
            var templatePath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/SingleFileLambdaFunctions/serverless.template");

            var stackName = "TestDeployServerlessReferencingSingleFile-" + DateTime.Now.Ticks;
            var deployServerlessCommand = new DeployServerlessCommand(toolLogger, Environment.CurrentDirectory, new string[] { "--template", templatePath });
            deployServerlessCommand.DisableInteractive = true;
            deployServerlessCommand.Region = TEST_REGION;
            deployServerlessCommand.Configuration = "Release";
            deployServerlessCommand.StackName = stackName;
            deployServerlessCommand.S3Bucket = this._testFixture.Bucket;
            deployServerlessCommand.WaitForStackToComplete = true;

            var created = false;
            try
            {
                string functionName = null;
                created = await deployServerlessCommand.ExecuteAsync();
                Assert.True(created);
                using (var cfClient = new AmazonCloudFormationClient(RegionEndpoint.GetBySystemName(TEST_REGION)))
                {
                    var describeResponse = await cfClient.DescribeStacksAsync(new DescribeStacksRequest
                    {
                        StackName = deployServerlessCommand.StackName
                    });

                    Assert.Equal(StackStatus.CREATE_COMPLETE, describeResponse.Stacks[0].StackStatus);

                    var describeResourceResponse = await cfClient.DescribeStackResourceAsync(new DescribeStackResourceRequest { StackName = stackName, LogicalResourceId = "ToUpperFunctionNoAOT" });
                    functionName = describeResourceResponse.StackResourceDetail.PhysicalResourceId;
                }

                toolLogger.ClearBuffer();
                var invokeCommand = new InvokeFunctionCommand(toolLogger, Environment.CurrentDirectory, new string[0]);
                invokeCommand.FunctionName = functionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);
            }
            finally
            {
                if (created)
                {
                    try
                    {
                        var deleteCommand = new DeleteServerlessCommand(toolLogger, Environment.CurrentDirectory, new string[0]);
                        deleteCommand.StackName = deployServerlessCommand.StackName;
                        deleteCommand.Region = TEST_REGION;
                        await deleteCommand.ExecuteAsync();
                    }
                    catch
                    {
                        // Bury exception because we don't want to lose any exceptions during the deploy stage.
                    }
                }
            }
        }

        [Fact]
        public async Task TestDeployServerlessECRImageUriNoMetadataJsonTemplate()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var toolLogger = new TestToolLogger(_testOutputHelper);
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/TestSimpleImageProject");

            var pushImageCommand = new PushDockerImageCommand(toolLogger, fullPath, new string[0])
            {
                DisableInteractive = true,
                Region = TEST_REGION,
                WorkingDirectory = fullPath,
                SkipPushToECR = false,

                PushDockerImageProperties = new BasePushDockerImageCommand<LambdaToolsDefaults>.PushDockerImagePropertyContainer
                {
                    DockerImageTag = $"{TEST_ECR_REPOSITORY}:deployserverlessimageurinometadata",
                }
            };

            var pushImageResult = await pushImageCommand.ExecuteAsync();
            Assert.Contains("Pushing image to ECR repository", toolLogger.Buffer);
            Assert.Contains($"{TEST_ECR_REPOSITORY}:deployserverlessimageurinometadata Push Complete.", toolLogger.Buffer);

            string functionName = $"HelloWorldFunction{DateTime.Now.Ticks}";
            string jsonTemplate = @$"
{{
    ""AWSTemplateFormatVersion"": ""2010-09-09"",
    ""Transform"": ""AWS::Serverless-2016-10-31"",
    ""Description"": ""An AWS Serverless Application."",
    ""Resources"": {{
        ""{functionName}"": {{
            ""Type"": ""AWS::Serverless::Function"",
            ""Properties"": {{
                ""FunctionName"": ""{functionName}"",
                ""MemorySize"": 256,
                ""Timeout"": 30,
                ""Policies"": [
                    ""AWSLambdaBasicExecutionRole""
                ],
                ""PackageType"": ""Image"",
                ""ImageUri"": ""{pushImageCommand.PushedImageUri}"",
                ""ImageConfig"": {{
                    ""Command"": [
                        ""TestSimpleImageProject::TestSimpleImageProject.Function::FunctionHandler""
                    ]
                }}
            }}
        }}
    }},
    ""Outputs"": {{}}
}}
            ";
            
            var tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, jsonTemplate);
            var deployServerlessCommand = new DeployServerlessCommand(toolLogger, fullPath, new string[] { "--template", tempFileName });
            deployServerlessCommand.DisableInteractive = true;
            deployServerlessCommand.Region = TEST_REGION;
            deployServerlessCommand.Configuration = "Release";
            deployServerlessCommand.StackName = functionName;
            deployServerlessCommand.S3Bucket = this._testFixture.Bucket;
            deployServerlessCommand.WaitForStackToComplete = true;

            var created = false;
            try
            {
                created = await deployServerlessCommand.ExecuteAsync();
                Assert.True(created);
                using (var cfClient = new AmazonCloudFormationClient(RegionEndpoint.GetBySystemName(TEST_REGION)))
                {
                    var describeResponse = await cfClient.DescribeStacksAsync(new DescribeStacksRequest
                    {
                        StackName = deployServerlessCommand.StackName
                    });

                    Assert.Equal(StackStatus.CREATE_COMPLETE, describeResponse.Stacks[0].StackStatus);
                }

                toolLogger.ClearBuffer();
                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = functionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);
            }
            finally
            {
                if (created)
                {
                    try
                    {
                        var deleteCommand = new DeleteServerlessCommand(toolLogger, fullPath, new string[0]);
                        deleteCommand.StackName = deployServerlessCommand.StackName;
                        deleteCommand.Region = TEST_REGION;
                        await deleteCommand.ExecuteAsync();
                    }
                    catch
                    {
                        // Bury exception because we don't want to lose any exceptions during the deploy stage.
                    }
                }

                File.Delete(tempFileName);
            }
        }
    }
}
