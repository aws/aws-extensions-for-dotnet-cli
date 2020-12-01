using System;
using System.Collections.Generic;
using System.Text;

using Amazon.Lambda;

using Xunit;
using Moq;
using Amazon.Lambda.Model;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace Amazon.Lambda.Tools.Test
{
    public class WaitTillFunctionAvailableTests
    {
        [Fact]
        public async Task FunctionImmediateAvailable()
        {
            var functionName = "fakeFunction";

            var mockLambda = new Mock<IAmazonLambda>();

            mockLambda.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<GetFunctionConfigurationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(functionName, request.FunctionName);
                })
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    return Task.FromResult(new GetFunctionConfigurationResponse
                    {
                        State = State.Active,
                        LastUpdateStatus = LastUpdateStatus.Successful
                    });
                });

            var logger = new TestToolLogger();
            await LambdaUtilities.WaitTillFunctionAvailableAsync(logger, mockLambda.Object, functionName);

            Assert.Equal(0, logger.Buffer.Length);
        }

        [Fact]
        public async Task NewFunctionBecomesAvailable()
        {
            var functionName = "fakeFunction";

            var mockLambda = new Mock<IAmazonLambda>();

            var getConfigCallCount = 0;
            mockLambda.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<GetFunctionConfigurationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(functionName, request.FunctionName);
                })
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    getConfigCallCount++;
                    if(getConfigCallCount < 3)
                    {
                        return Task.FromResult(new GetFunctionConfigurationResponse
                        {
                            State = State.Pending,
                            LastUpdateStatus = LastUpdateStatus.InProgress
                        });
                    }

                    return Task.FromResult(new GetFunctionConfigurationResponse
                    {
                        State = State.Active,
                        LastUpdateStatus = LastUpdateStatus.Successful
                    });
                });

            var logger = new TestToolLogger();
            await LambdaUtilities.WaitTillFunctionAvailableAsync(logger, mockLambda.Object, functionName);

            Assert.Contains("An update is currently", logger.Buffer);
            Assert.Contains("... Waiting", logger.Buffer);
        }

        [Fact]
        public async Task UpdateFunctionBecomesAvailable()
        {
            var functionName = "fakeFunction";

            var mockLambda = new Mock<IAmazonLambda>();

            var getConfigCallCount = 0;
            mockLambda.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<GetFunctionConfigurationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(functionName, request.FunctionName);
                })
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    getConfigCallCount++;
                    if (getConfigCallCount < 3)
                    {
                        return Task.FromResult(new GetFunctionConfigurationResponse
                        {
                            State = State.Active,
                            LastUpdateStatus = LastUpdateStatus.InProgress
                        });
                    }

                    return Task.FromResult(new GetFunctionConfigurationResponse
                    {
                        State = State.Active,
                        LastUpdateStatus = LastUpdateStatus.Successful
                    });
                });

            var logger = new TestToolLogger();
            await LambdaUtilities.WaitTillFunctionAvailableAsync(logger, mockLambda.Object, functionName);

            Assert.Contains("An update is currently", logger.Buffer);
            Assert.Contains("... Waiting", logger.Buffer);
        }

        [Fact]
        public async Task UpdateFunctionGoesToFailure()
        {
            var functionName = "fakeFunction";

            var mockLambda = new Mock<IAmazonLambda>();

            var getConfigCallCount = 0;
            mockLambda.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<GetFunctionConfigurationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal(functionName, request.FunctionName);
                })
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    getConfigCallCount++;
                    if (getConfigCallCount < 3)
                    {
                        return Task.FromResult(new GetFunctionConfigurationResponse
                        {
                            State = State.Active,
                            LastUpdateStatus = LastUpdateStatus.InProgress
                        });
                    }

                    return Task.FromResult(new GetFunctionConfigurationResponse
                    {
                        State = State.Active,
                        LastUpdateStatus = LastUpdateStatus.Failed,
                        LastUpdateStatusReason = "Its Bad"
                    });
                });

            var logger = new TestToolLogger();
            await LambdaUtilities.WaitTillFunctionAvailableAsync(logger, mockLambda.Object, functionName);

            Assert.Contains("An update is currently", logger.Buffer);
            Assert.Contains("... Waiting", logger.Buffer);
            Assert.Contains("Warning: function", logger.Buffer);
            Assert.Contains("Its Bad", logger.Buffer);
        }
    }
}
