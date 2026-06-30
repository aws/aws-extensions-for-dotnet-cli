// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    /// <summary>
    /// Verifies the durable-execution wiring on deploy-function/update-function-config: the DurableConfig is mapped
    /// onto the create/update requests, and the AWSLambdaBasicDurableExecutionRolePolicy managed policy is attached
    /// to a tool-created role (and only warned about for a user-supplied role).
    /// </summary>
    public class DurableConfigTest
    {
        // A real-looking ARN so RoleHelper.ExpandRoleName short-circuits without calling IAM.
        private const string TestRoleArn = "arn:aws:iam::123456789012:role/durable-test-role";
        private const string DurablePolicyArn = LambdaConstants.AWS_LAMBDA_BASIC_DURABLE_EXECUTION_MANAGED_POLICY;

        private readonly ITestOutputHelper _testOutputHelper;

        public DurableConfigTest(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        private static string GetTestFunctionPath()
        {
            var assembly = typeof(DurableConfigTest).GetTypeInfo().Assembly;
            return Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
        }

        /// <summary>
        /// Builds an IAM mock where the role has only the given policies attached. Records every AttachRolePolicy call.
        /// </summary>
        private static Mock<IAmazonIdentityManagementService> BuildIamMock(List<string> attachCalls, params string[] alreadyAttached)
        {
            var iamMock = new Mock<IAmazonIdentityManagementService>();

            iamMock.Setup(c => c.ListAttachedRolePoliciesAsync(It.IsAny<ListAttachedRolePoliciesRequest>(), It.IsAny<CancellationToken>()))
                .Returns((ListAttachedRolePoliciesRequest r, CancellationToken token) =>
                {
                    var response = new ListAttachedRolePoliciesResponse
                    {
                        IsTruncated = false,
                        AttachedPolicies = new List<AttachedPolicyType>()
                    };
                    foreach (var arn in alreadyAttached)
                        response.AttachedPolicies.Add(new AttachedPolicyType { PolicyArn = arn, PolicyName = arn });
                    return Task.FromResult(response);
                });

            iamMock.Setup(c => c.AttachRolePolicyAsync(It.IsAny<AttachRolePolicyRequest>(), It.IsAny<CancellationToken>()))
                .Callback<AttachRolePolicyRequest, CancellationToken>((r, token) => attachCalls.Add(r.PolicyArn))
                .Returns(Task.FromResult(new AttachRolePolicyResponse()));

            return iamMock;
        }

        [Fact]
        public async Task DurableConfigMappedOntoCreateRequest()
        {
            DurableConfig captured = null;
            var lambdaMock = new Mock<IAmazonLambda>();
            lambdaMock.Setup(c => c.CreateFunctionAsync(It.IsAny<CreateFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateFunctionRequest, CancellationToken>((request, token) => captured = request.DurableConfig)
                .Returns(Task.FromResult(new CreateFunctionResponse()));

            var attachCalls = new List<string>();
            // User-supplied role (Role set explicitly) that already has the durable policy => no attach, no warn.
            var iamMock = BuildIamMock(attachCalls, DurablePolicyArn);

            var command = NewDeployCommand();
            command.Role = TestRoleArn;
            command.DurableExecutionTimeout = 3600;
            command.DurableRetentionPeriodInDays = 30;
            command.LambdaClient = lambdaMock.Object;
            command.IAMClient = iamMock.Object;

            var created = await command.ExecuteAsync();

            Assert.True(created, command.LastToolsException?.Message);
            Assert.NotNull(captured);
            Assert.Equal(3600, captured.ExecutionTimeout);
            Assert.Equal(30, captured.RetentionPeriodInDays);
            // Role was user-supplied, so the tool must not mutate it.
            Assert.Empty(attachCalls);
        }

        [Fact]
        public async Task NonDurableCreateDoesNotAttachDurablePolicy()
        {
            var lambdaMock = new Mock<IAmazonLambda>();
            CreateFunctionRequest captured = null;
            lambdaMock.Setup(c => c.CreateFunctionAsync(It.IsAny<CreateFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateFunctionRequest, CancellationToken>((request, token) => captured = request)
                .Returns(Task.FromResult(new CreateFunctionResponse()));

            var attachCalls = new List<string>();
            var iamMock = BuildIamMock(attachCalls);

            var command = NewDeployCommand();
            command.Role = TestRoleArn;
            // No durable options set.
            command.LambdaClient = lambdaMock.Object;
            command.IAMClient = iamMock.Object;

            var created = await command.ExecuteAsync();

            Assert.True(created, command.LastToolsException?.Message);
            Assert.NotNull(captured);
            Assert.Null(captured.DurableConfig);
            Assert.Empty(attachCalls);
        }

        [Fact]
        public async Task UserSuppliedRoleMissingDurablePolicyNotifiesButDoesNotAttach()
        {
            var lambdaMock = new Mock<IAmazonLambda>();
            lambdaMock.Setup(c => c.CreateFunctionAsync(It.IsAny<CreateFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new CreateFunctionResponse()));

            var attachCalls = new List<string>();
            // User-supplied role with only the basic-execution policy => durable policy missing => notify only.
            var iamMock = BuildIamMock(attachCalls, "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole");

            var logger = new TestToolLogger(_testOutputHelper);
            var command = NewDeployCommand(logger);
            command.Role = TestRoleArn;
            command.DurableExecutionTimeout = 1200;
            command.LambdaClient = lambdaMock.Object;
            command.IAMClient = iamMock.Object;

            var created = await command.ExecuteAsync();

            Assert.True(created, command.LastToolsException?.Message);
            Assert.Empty(attachCalls);
            Assert.Contains("required permissions for Durable Functions", logger.Buffer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(DurablePolicyArn, logger.Buffer);
        }

        [Fact]
        public async Task DurableConfigMappedOntoUpdateRequestAndNotifiesWhenRoleMissingPolicy()
        {
            DurableConfig captured = null;
            var lambdaMock = new Mock<IAmazonLambda>();

            lambdaMock.Setup(c => c.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) => Task.FromResult(new GetFunctionConfigurationResponse
                {
                    FunctionName = r.FunctionName,
                    Handler = "TestFunction::TestFunction.Function::ToUpper",
                    Timeout = 10,
                    MemorySize = 512,
                    Role = TestRoleArn,
                    Runtime = "dotnet8",
                    PackageType = PackageType.Zip
                    // No existing DurableConfig => the update flips the function to durable.
                }));

            lambdaMock.Setup(c => c.UpdateFunctionConfigurationAsync(It.IsAny<UpdateFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateFunctionConfigurationRequest, CancellationToken>((request, token) => captured = request.DurableConfig)
                .Returns(Task.FromResult(new UpdateFunctionConfigurationResponse()));

            // Code update is also attempted on a deploy; stub it so the deploy path completes.
            lambdaMock.Setup(c => c.UpdateFunctionCodeAsync(It.IsAny<UpdateFunctionCodeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new UpdateFunctionCodeResponse()));

            var attachCalls = new List<string>();
            var iamMock = BuildIamMock(attachCalls); // role has no durable policy

            var logger = new TestToolLogger(_testOutputHelper);
            var command = NewDeployCommand(logger);
            command.Role = TestRoleArn;
            command.DurableExecutionTimeout = 900;
            command.LambdaClient = lambdaMock.Object;
            command.IAMClient = iamMock.Object;

            var updated = await command.ExecuteAsync();

            Assert.True(updated, command.LastToolsException?.Message);
            Assert.NotNull(captured);
            Assert.Equal(900, captured.ExecutionTimeout);
            // Update never creates a role, so it must notify rather than attach.
            Assert.Empty(attachCalls);
            Assert.Contains(DurablePolicyArn, logger.Buffer);
        }

        private DeployFunctionCommand NewDeployCommand(TestToolLogger logger = null)
        {
            var command = new DeployFunctionCommand(logger ?? new TestToolLogger(_testOutputHelper), GetTestFunctionPath(), new string[0]);
            command.FunctionName = "durable-test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Configuration = "Release";
            command.Runtime = "dotnet8";
            command.DisableInteractive = true;
            return command;
        }
    }
}
