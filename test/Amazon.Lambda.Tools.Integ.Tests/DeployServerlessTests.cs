using Amazon.Lambda.Tools.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;

using static Amazon.Lambda.Tools.Integ.Tests.TestConstants;
using Newtonsoft.Json.Linq;

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

        [Fact]
        public async Task TestImageFunctionServerlessTemplateExamples()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/ServerlessTemplateExamples");

            var command = new PackageCICommand(toolLogger, fullPath, new string[0]);
            command.Region = TEST_REGION;
            command.DisableInteractive = true;
            command.S3Bucket = this._testFixture.Bucket;
            command.CloudFormationTemplate = "serverless-resource.template";
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
                Assert.Contains("usedockermetadata:usedockermetadatafunction", useDefaultDockerFunction["ImageUri"]?.ToString());
                Assert.EndsWith("latest", useDefaultDockerFunction["ImageUri"]?.ToString());
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
    }
}
