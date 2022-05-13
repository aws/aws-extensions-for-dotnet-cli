using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;
using System;
using System.IO;
using System.Net.Http;
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

        [Fact]
        public async Task SetAndClearEnvironmentVariables()
        {
            var toolLogger = new TestToolLogger(_testOutputHelper);
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location) + "../../../../../../testapps/TestHttpFunction");
            var functionName = "SetAndClearEnvironmentVariables-" + DateTime.Now.Ticks;

            // Initial deployment with Function Url enabled and Auth will default to NONE.
            var command = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
            command.FunctionName = functionName;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.Runtime = "dotnet6";
            command.EphemeralStorageSize = 750;
            command.EnvironmentVariables = new System.Collections.Generic.Dictionary<string, string> { { "Key1", "Value1" } };
            command.DisableInteractive = true;
            var created = await command.ExecuteAsync();

            try
            {
                Assert.True(created);
                var getConfigResponse = await command.LambdaClient.GetFunctionConfigurationAsync(functionName);
                Assert.Equal(750, getConfigResponse.EphemeralStorage.Size);
                Assert.Single(getConfigResponse.Environment.Variables);

                // Redeploy changing the ephemeral size and clearning environment variables.
                command = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
                command.FunctionName = functionName;
                command.Role = await TestHelper.GetTestRoleArnAsync();
                command.Runtime = "dotnet6";
                command.EphemeralStorageSize = 800;
                command.EnvironmentVariables = new System.Collections.Generic.Dictionary<string, string> ();
                command.DisableInteractive = true;

                created = await command.ExecuteAsync();
                Assert.True(created);

                getConfigResponse = await command.LambdaClient.GetFunctionConfigurationAsync(functionName);
                Assert.Equal(800, getConfigResponse.EphemeralStorage.Size);
                Assert.Null(getConfigResponse.Environment);
            }
            finally
            {
                try
                {
                    await command.LambdaClient.DeleteFunctionAsync(functionName);
                }
                catch { }
            }
        }

        [Fact]
        public async Task DeployWithFunctionUrl()
        {
            using var httpClient = new HttpClient();
            var toolLogger = new TestToolLogger(_testOutputHelper);

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(this.GetType().GetTypeInfo().Assembly.Location) + "../../../../../../testapps/TestHttpFunction");
            var functionName = "DeployWithFunctionUrl-" + DateTime.Now.Ticks;

            async Task TestFunctionUrl(string functionUrl, bool expectSuccess)
            {
                var httpResponse = await httpClient.GetAsync(functionUrl);
                Assert.Equal(expectSuccess, httpResponse.IsSuccessStatusCode);
                httpResponse.Dispose();
            }

            async Task TestPublicPermissionStatement(IAmazonLambda lambdaClient, bool expectExist)
            {
                if(expectExist)
                {
                    var policy = (await lambdaClient.GetPolicyAsync(new GetPolicyRequest { FunctionName = functionName })).Policy;
                    Assert.Contains(LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID, policy);
                }
                else
                {
                    try
                    {
                        var policy = (await lambdaClient.GetPolicyAsync(new GetPolicyRequest { FunctionName = functionName })).Policy;
                        Assert.DoesNotContain(LambdaConstants.FUNCTION_URL_PUBLIC_PERMISSION_STATEMENT_ID, policy);
                    }
                    catch (ResourceNotFoundException)
                    {
                        // If the last statement is deleted from the policy then a ResourceNotFoundException is thrown which is also proof the statement id has been removed.
                    }
                }
            }

            async Task<DeployFunctionCommand> TestDeployProjectAsync(bool? enableUrl, string authType = null)
            {
                var command = new DeployFunctionCommand(toolLogger, fullPath, new string[0]);
                command.FunctionName = functionName;
                command.Role = await TestHelper.GetTestRoleArnAsync();
                command.Runtime = "dotnet6";

                command.DisableInteractive = true;
                
                if(enableUrl.HasValue)
                {
                    command.FunctionUrlEnable = enableUrl;
                }

                if (authType != null)
                {
                    command.FunctionUrlAuthType = authType;
                }

                var created = await command.ExecuteAsync();
                Assert.True(created);
                await LambdaUtilities.WaitTillFunctionAvailableAsync(toolLogger, command.LambdaClient, functionName);
                return command;
            }

            // Initial deployment with Function Url enabled and Auth will default to NONE.
            var command = await TestDeployProjectAsync(enableUrl: true);
            try
            {
                // Ensure initial deployment was successful with function URL and NONE authtype
                Assert.NotNull(command.FunctionUrlLink);
                var functionUrl = command.FunctionUrlLink;
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: true);
                await TestFunctionUrl(functionUrl, expectSuccess: true);

                // Redeploy without making any changes to FunctionUrl. Make sure we don't unintended remove function url config
                // when just updating bits.
                command = await TestDeployProjectAsync(enableUrl: null);
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: true);
                await TestFunctionUrl(functionUrl, expectSuccess: true);

                // Redeploy turning off Function Url
                command = await TestDeployProjectAsync(enableUrl: false);
                Assert.Null(command.FunctionUrlLink);
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: false);
                await TestFunctionUrl(functionUrl, expectSuccess: false);

                // Redeploy turning Function Url back on using AWS_IAM. (Not public
                command = await TestDeployProjectAsync(enableUrl: true, authType: "AWS_IAM");
                Assert.NotNull(command.FunctionUrlLink);
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: false);

                // Redeploy switching auth to NONE (Public)
                command = await TestDeployProjectAsync(enableUrl: true, authType: "NONE");
                Assert.NotNull(command.FunctionUrlLink);
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: true);

                // Redeploy switching back to AWS_IAM and prove public statement removed.
                command = await TestDeployProjectAsync(enableUrl: true, authType: "AWS_IAM");
                Assert.NotNull(command.FunctionUrlLink);
                await TestPublicPermissionStatement(command.LambdaClient, expectExist: false);
            }
            finally
            {
                try
                {
                    await command.LambdaClient.DeleteFunctionAsync(functionName);
                }
                catch { }
            }
        }
    }
}
