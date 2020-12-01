using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using static Amazon.Lambda.Tools.Integ.Tests.TestConstants;

namespace Amazon.Lambda.Tools.Integ.Tests
{
    public class DeployProjectTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public DeployProjectTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }


        [Fact]
        public async Task TestSimpleImageProjectTest()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/TestSimpleImageProject");

            var functionName = "test-simple-image-project-" + DateTime.Now.Ticks;

            var command = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
            command.FunctionName = functionName;
            command.Region = TEST_REGION;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.DockerImageTag = $"{TEST_ECR_REPOSITORY}:simpleimageproject1";
            command.DisableInteractive = true;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                toolLogger.ClearBuffer();


                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = command.FunctionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                // Make sure waiting works.
                Assert.Contains("... Waiting", toolLogger.Buffer);
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);


                // Update function without changing settings.
                toolLogger.ClearBuffer();
                var updateCommand = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
                updateCommand.FunctionName = functionName;
                updateCommand.DisableInteractive = true;
                updateCommand.Region = TEST_REGION;
                updateCommand.DockerImageTag = $"{TEST_ECR_REPOSITORY}:simpleimageproject1";

                var updated = await updateCommand.ExecuteAsync();
                Assert.True(updated);

                Assert.DoesNotContain("... Waiting", toolLogger.Buffer);

                toolLogger.ClearBuffer();
                invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = command.FunctionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;
                await invokeCommand.ExecuteAsync();
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);



                // Update function with changed settings.
                toolLogger.ClearBuffer();
                updateCommand = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
                updateCommand.FunctionName = functionName;
                updateCommand.MemorySize = 1024;
                updateCommand.DockerImageTag = $"{TEST_ECR_REPOSITORY}:simpleimageproject1";
                updateCommand.Region = TEST_REGION;
                updateCommand.DisableInteractive = true;

                updated = await updateCommand.ExecuteAsync();
                Assert.True(updated);
                Assert.Contains("... Waiting", toolLogger.Buffer);


                toolLogger.ClearBuffer();
                invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = command.FunctionName;
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
                        await command.LambdaClient.DeleteFunctionAsync(command.FunctionName);
                    }
                    catch { }
                }
            }
        }

        [Fact]
        public async Task TestMultiStageBuildWithSupportLibrary()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/MultiStageBuildWithClassLibraries/TheFunction");

            var functionName = "test-multistage-with-support-library-" + DateTime.Now.Ticks;

            var command = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
            command.FunctionName = functionName;
            command.Region = TEST_REGION;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.DockerImageTag = $"{TEST_ECR_REPOSITORY}:multistagetest";
            command.DisableInteractive = true;

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                toolLogger.ClearBuffer();


                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = command.FunctionName;
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();

                Assert.Contains("Hello from support library", toolLogger.Buffer);
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
        public async Task PackageFunctionAsLocalImageThenDeploy()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/TestSimpleImageProject");

            var packageCommand = new PackageCommand(toolLogger, fullPath, new string[0]);
            packageCommand.Region = TEST_REGION;
            packageCommand.DockerImageTag = $"{TEST_ECR_REPOSITORY}:packageanddeploy1";
            packageCommand.DisableInteractive = true;
            packageCommand.PackageType = "image";

            var packageSuccess = await packageCommand.ExecuteAsync();
            Assert.True(packageSuccess);

            Assert.Contains($"Packaged project as image: \"{packageCommand.DockerImageTag}\"", toolLogger.Buffer);
            Assert.DoesNotContain("Pushing image to ECR repository", toolLogger.Buffer);

            var functionName = "test-package-then-deploy-" + DateTime.Now.Ticks;

            toolLogger.ClearBuffer();
            var deployCommand = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
            deployCommand.FunctionName = functionName;
            deployCommand.Region = TEST_REGION;
            deployCommand.Role = await TestHelper.GetTestRoleArnAsync();
            deployCommand.LocalDockerImage = packageCommand.DockerImageTag;
            deployCommand.DisableInteractive = true;

            var deploySuccess = await deployCommand.ExecuteAsync();
            try
            {
                Assert.True(deploySuccess);
                Assert.DoesNotContain("docker build", toolLogger.Buffer);
                Assert.Contains($"{TEST_ECR_REPOSITORY}:packageanddeploy1 Push Complete.", toolLogger.Buffer);

                toolLogger.ClearBuffer();
                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = deployCommand.FunctionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                // Make sure waiting works.
                Assert.Contains("... Waiting", toolLogger.Buffer);
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);

            }
            finally
            {
                if (deploySuccess)
                {
                    await deployCommand.LambdaClient.DeleteFunctionAsync(deployCommand.FunctionName);
                }
            }
        }


        [Fact]
        public async Task PackageFunctionAsLocalImageThenDeployWithDifferentECRTag()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/ImageBasedProjects/TestSimpleImageProject");

            var packageCommand = new PackageCommand(toolLogger, fullPath, new string[0]);
            packageCommand.Region = TEST_REGION;
            packageCommand.DockerImageTag = $"{TEST_ECR_REPOSITORY}:packageanddeploywithdifferenttags1";
            packageCommand.DisableInteractive = true;
            packageCommand.PackageType = "image";

            var packageSuccess = await packageCommand.ExecuteAsync();
            Assert.True(packageSuccess);

            Assert.Contains($"Packaged project as image: \"{packageCommand.DockerImageTag}\"", toolLogger.Buffer);
            Assert.DoesNotContain("Pushing image to ECR repository", toolLogger.Buffer);

            var functionName = "test-package-then-deploy-differenttags-" + DateTime.Now.Ticks;

            toolLogger.ClearBuffer();
            var deployCommand = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
            deployCommand.FunctionName = functionName;
            deployCommand.Region = TEST_REGION;
            deployCommand.Role = await TestHelper.GetTestRoleArnAsync();
            deployCommand.LocalDockerImage = packageCommand.DockerImageTag;
            deployCommand.DockerImageTag = $"{TEST_ECR_REPOSITORY}:packageanddeploywithdifferenttags2";
            deployCommand.DisableInteractive = true;

            var deploySuccess = await deployCommand.ExecuteAsync();
            try
            {
                Assert.True(deploySuccess);
                Assert.DoesNotContain("docker build", toolLogger.Buffer);
                Assert.Contains($"{TEST_ECR_REPOSITORY}:packageanddeploywithdifferenttags2 Push Complete.", toolLogger.Buffer);

                toolLogger.ClearBuffer();
                var invokeCommand = new InvokeFunctionCommand(toolLogger, fullPath, new string[0]);
                invokeCommand.FunctionName = deployCommand.FunctionName;
                invokeCommand.Payload = "hello world";
                invokeCommand.Region = TEST_REGION;

                await invokeCommand.ExecuteAsync();
                // Make sure waiting works.
                Assert.Contains("... Waiting", toolLogger.Buffer);
                Assert.Contains("HELLO WORLD", toolLogger.Buffer);

            }
            finally
            {
                if (deploySuccess)
                {
                    await deployCommand.LambdaClient.DeleteFunctionAsync(deployCommand.FunctionName);
                }
            }
        }
    }
}
