// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Moq;
using Xunit;

namespace Amazon.Lambda.Tools.Test
{
    /// <summary>
    /// Unit tests for the RoleHelper managed-policy helpers added for durable execution. Fully mocked, no AWS calls.
    /// </summary>
    public class RoleHelperDurableTests
    {
        private const string PolicyArn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicDurableExecutionRolePolicy";

        [Theory]
        [InlineData("arn:aws:iam::123456789012:role/MyRole", "MyRole")]
        [InlineData("arn:aws:iam::123456789012:role/path/segment/MyRole", "MyRole")]
        [InlineData("MyRole", "MyRole")]
        public void GetRoleNameFromArnOrName_ExtractsName(string input, string expected)
        {
            Assert.Equal(expected, RoleHelper.GetRoleNameFromArnOrName(input));
        }

        private static Mock<IAmazonIdentityManagementService> BuildMock(List<AttachRolePolicyRequest> attachCalls, params string[] alreadyAttached)
        {
            var mock = new Mock<IAmazonIdentityManagementService>();
            mock.Setup(c => c.ListAttachedRolePoliciesAsync(It.IsAny<ListAttachedRolePoliciesRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
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
            mock.Setup(c => c.AttachRolePolicyAsync(It.IsAny<AttachRolePolicyRequest>(), It.IsAny<CancellationToken>()))
                .Callback<AttachRolePolicyRequest, CancellationToken>((r, token) => attachCalls.Add(r))
                .Returns(Task.FromResult(new AttachRolePolicyResponse()));
            return mock;
        }

        [Fact]
        public async Task IsManagedPolicyAttached_TrueWhenPresent()
        {
            var mock = BuildMock(new List<AttachRolePolicyRequest>(), PolicyArn);
            Assert.True(await RoleHelper.IsManagedPolicyAttachedAsync(mock.Object, "arn:aws:iam::123456789012:role/MyRole", PolicyArn));
        }

        [Fact]
        public async Task IsManagedPolicyAttached_FalseWhenAbsent()
        {
            var mock = BuildMock(new List<AttachRolePolicyRequest>());
            Assert.False(await RoleHelper.IsManagedPolicyAttachedAsync(mock.Object, "arn:aws:iam::123456789012:role/MyRole", PolicyArn));
        }

        [Fact]
        public async Task EnsureManagedPolicyAttached_AttachesWhenMissing()
        {
            var attachCalls = new List<AttachRolePolicyRequest>();
            var mock = BuildMock(attachCalls);

            var attached = await RoleHelper.EnsureManagedPolicyAttachedAsync(mock.Object, "arn:aws:iam::123456789012:role/MyRole", PolicyArn);

            Assert.True(attached);
            Assert.Single(attachCalls);
            Assert.Equal(PolicyArn, attachCalls[0].PolicyArn);
            Assert.Equal("MyRole", attachCalls[0].RoleName);
        }

        [Fact]
        public async Task EnsureManagedPolicyAttached_IdempotentWhenAlreadyPresent()
        {
            var attachCalls = new List<AttachRolePolicyRequest>();
            var mock = BuildMock(attachCalls, PolicyArn);

            var attached = await RoleHelper.EnsureManagedPolicyAttachedAsync(mock.Object, "arn:aws:iam::123456789012:role/MyRole", PolicyArn);

            Assert.False(attached);
            Assert.Empty(attachCalls);
        }
    }
}
