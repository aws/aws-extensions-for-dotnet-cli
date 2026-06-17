using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;

using Moq;
using Xunit;

namespace Amazon.Lambda.Tools.Test
{
    public class InvokeFunctionCommandTests
    {
        const string FunctionName = "fakeFunction";

        /// <summary>
        /// Creates a mock Lambda client that reports the function as immediately available so the
        /// WaitTillFunctionAvailableAsync call made by the command completes without delay.
        /// </summary>
        private static Mock<IAmazonLambda> CreateAvailableLambdaMock()
        {
            var mockLambda = new Mock<IAmazonLambda>();
            mockLambda.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetFunctionConfigurationResponse
                {
                    State = State.Active,
                    LastUpdateStatus = LastUpdateStatus.Successful
                }));
            return mockLambda;
        }

        private static InvokeFunctionCommand CreateCommand(TestToolLogger logger, IAmazonLambda lambdaClient, params string[] args)
        {
            var command = new InvokeFunctionCommand(logger, Directory.GetCurrentDirectory(), args)
            {
                LambdaClient = lambdaClient,
                DisableInteractive = true
            };
            return command;
        }

        [Theory]
        [InlineData("RequestResponse", InvokeFunctionCommand.InvokeMode.RequestResponse)]
        [InlineData("requestresponse", InvokeFunctionCommand.InvokeMode.RequestResponse)]
        [InlineData("Event", InvokeFunctionCommand.InvokeMode.Event)]
        [InlineData("event", InvokeFunctionCommand.InvokeMode.Event)]
        [InlineData("Stream", InvokeFunctionCommand.InvokeMode.Stream)]
        [InlineData("STREAM", InvokeFunctionCommand.InvokeMode.Stream)]
        [InlineData("DurableExecution", InvokeFunctionCommand.InvokeMode.DurableExecution)]
        [InlineData("durableexecution", InvokeFunctionCommand.InvokeMode.DurableExecution)]
        public void ParseInvokeModeOption(string value, InvokeFunctionCommand.InvokeMode expected)
        {
            var command = new InvokeFunctionCommand(new TestToolLogger(), Directory.GetCurrentDirectory(),
                new[] { FunctionName, "--invoke-mode", value });

            Assert.Equal(expected, command.Mode);
        }

        [Fact]
        public void InvokeModeDefaultsToNull()
        {
            var command = new InvokeFunctionCommand(new TestToolLogger(), Directory.GetCurrentDirectory(),
                new[] { FunctionName });

            Assert.Null(command.Mode);
        }

        [Fact]
        public void ParseInvalidInvokeModeThrows()
        {
            var ex = Assert.Throws<LambdaToolsException>(() =>
                new InvokeFunctionCommand(new TestToolLogger(), Directory.GetCurrentDirectory(),
                    new[] { FunctionName, "--invoke-mode", "NotAMode" }));

            Assert.Contains("NotAMode", ex.Message);
            Assert.Contains("RequestResponse", ex.Message);
        }

        [Fact]
        public async Task RequestResponseIsTheDefaultInvocationType()
        {
            var mockLambda = CreateAvailableLambdaMock();
            InvokeRequest capturedRequest = null;
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Callback<InvokeRequest, CancellationToken>((r, t) => capturedRequest = r)
                .Returns(Task.FromResult(new InvokeResponse
                {
                    StatusCode = 200,
                    Payload = new MemoryStream(Encoding.UTF8.GetBytes("\"hello\"")),
                    LogResult = Convert.ToBase64String(Encoding.UTF8.GetBytes("the logs"))
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName);

            Assert.True(await command.ExecuteAsync());

            Assert.NotNull(capturedRequest);
            Assert.Equal(InvocationType.RequestResponse, capturedRequest.InvocationType);
            Assert.Equal(LogType.Tail, capturedRequest.LogType);
            Assert.Contains("\"hello\"", logger.Buffer);
            Assert.Contains("the logs", logger.Buffer);
        }

        [Fact]
        public async Task RequestResponseReportsFunctionError()
        {
            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new InvokeResponse
                {
                    StatusCode = 200,
                    FunctionError = "Unhandled",
                    Payload = new MemoryStream(Encoding.UTF8.GetBytes("{\"errorMessage\":\"boom\"}")),
                    LogResult = Convert.ToBase64String(Encoding.UTF8.GetBytes("the logs"))
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName, "--invoke-mode", "RequestResponse");

            Assert.True(await command.ExecuteAsync());

            Assert.Contains("Unhandled", logger.Buffer);
        }

        [Fact]
        public async Task EventModeUsesEventInvocationType()
        {
            var mockLambda = CreateAvailableLambdaMock();
            InvokeRequest capturedRequest = null;
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Callback<InvokeRequest, CancellationToken>((r, t) => capturedRequest = r)
                .Returns(Task.FromResult(new InvokeResponse
                {
                    StatusCode = 202
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName, "--invoke-mode", "Event");

            Assert.True(await command.ExecuteAsync());

            Assert.NotNull(capturedRequest);
            Assert.Equal(InvocationType.Event, capturedRequest.InvocationType);
        }

        [Fact]
        public async Task EventModeDisplaysFunctionErrorAndDurableExecutionArn()
        {
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:$LATEST/durable-execution/abc/def";
            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new InvokeResponse
                {
                    StatusCode = 202,
                    FunctionError = "Unhandled",
                    DurableExecutionArn = durableArn
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName, "--invoke-mode", "Event");

            Assert.True(await command.ExecuteAsync());

            Assert.Contains("Unhandled", logger.Buffer);
            Assert.Contains(durableArn, logger.Buffer);
        }

        [Fact]
        public async Task DurableExecutionResolvesLatestVersionArnWhenNameIsNotArn()
        {
            const string functionArnV2 = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:2";
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:2/durable-execution/abc/def";

            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.ListVersionsByFunctionAsync(It.IsAny<ListVersionsByFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ListVersionsByFunctionResponse
                {
                    Versions = new System.Collections.Generic.List<FunctionConfiguration>
                    {
                        new FunctionConfiguration { Version = "$LATEST", FunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:$LATEST" },
                        new FunctionConfiguration { Version = "1", FunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1" },
                        new FunctionConfiguration { Version = "2", FunctionArn = functionArnV2 }
                    }
                }));

            InvokeRequest capturedInvoke = null;
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Callback<InvokeRequest, CancellationToken>((r, t) => capturedInvoke = r)
                .Returns(Task.FromResult(new InvokeResponse { StatusCode = 202, DurableExecutionArn = durableArn }));

            mockLambda.Setup(client => client.GetDurableExecutionAsync(It.IsAny<GetDurableExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionResponse { Status = ExecutionStatus.SUCCEEDED, Result = "\"done\"" }));
            mockLambda.Setup(client => client.GetDurableExecutionHistoryAsync(It.IsAny<GetDurableExecutionHistoryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionHistoryResponse()));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName, "--invoke-mode", "DurableExecution");

            Assert.True(await command.ExecuteAsync());

            // The latest published version (2) should be resolved and used for the invocation.
            Assert.Equal(functionArnV2, capturedInvoke.FunctionName);
            Assert.Equal(InvocationType.Event, capturedInvoke.InvocationType);
            Assert.Contains("Resolved latest version 2", logger.Buffer);
            Assert.Contains(functionArnV2, logger.Buffer);
            Assert.Contains("SUCCEEDED", logger.Buffer);
            Assert.Contains("\"done\"", logger.Buffer);
        }

        [Fact]
        public async Task DurableExecutionUsesProvidedArnWithoutVersionLookup()
        {
            const string functionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:3";
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:3/durable-execution/abc/def";

            var mockLambda = CreateAvailableLambdaMock();
            InvokeRequest capturedInvoke = null;
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Callback<InvokeRequest, CancellationToken>((r, t) => capturedInvoke = r)
                .Returns(Task.FromResult(new InvokeResponse { StatusCode = 202, DurableExecutionArn = durableArn }));
            mockLambda.Setup(client => client.GetDurableExecutionAsync(It.IsAny<GetDurableExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionResponse { Status = ExecutionStatus.SUCCEEDED }));
            mockLambda.Setup(client => client.GetDurableExecutionHistoryAsync(It.IsAny<GetDurableExecutionHistoryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionHistoryResponse()));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, functionArn, "--invoke-mode", "DurableExecution");

            Assert.True(await command.ExecuteAsync());

            Assert.Equal(functionArn, capturedInvoke.FunctionName);
            mockLambda.Verify(client => client.ListVersionsByFunctionAsync(It.IsAny<ListVersionsByFunctionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DurableExecutionThrowsWhenNoVersionsFound()
        {
            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.ListVersionsByFunctionAsync(It.IsAny<ListVersionsByFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ListVersionsByFunctionResponse
                {
                    Versions = new System.Collections.Generic.List<FunctionConfiguration>
                    {
                        new FunctionConfiguration { Version = "$LATEST", FunctionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:$LATEST" }
                    }
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, FunctionName, "--invoke-mode", "DurableExecution");

            // ExecuteAsync catches ToolsException, reports it, and returns false.
            Assert.False(await command.ExecuteAsync());
            var ex = Assert.IsType<LambdaToolsException>(command.LastException);
            Assert.Equal(LambdaToolsException.LambdaErrorCode.NoFunctionVersionsFound.ToString(), ex.Code);
            Assert.Contains("No published versions", ex.Message);
        }

        [Fact]
        public async Task DurableExecutionPollsUntilTerminalStatusAndWritesHistory()
        {
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1/durable-execution/abc/def";

            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new InvokeResponse { StatusCode = 202, DurableExecutionArn = durableArn }));

            // First two polls report RUNNING, then SUCCEEDED.
            var statusSequence = new Queue<ExecutionStatus>(new[] { ExecutionStatus.RUNNING, ExecutionStatus.RUNNING, ExecutionStatus.SUCCEEDED });
            mockLambda.Setup(client => client.GetDurableExecutionAsync(It.IsAny<GetDurableExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetDurableExecutionResponse
                {
                    Status = statusSequence.Count > 1 ? statusSequence.Dequeue() : statusSequence.Peek()
                }));

            var historyCallCount = 0;
            mockLambda.Setup(client => client.GetDurableExecutionHistoryAsync(It.IsAny<GetDurableExecutionHistoryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    historyCallCount++;
                    var events = new System.Collections.Generic.List<Event>();
                    // Surface a new event on the first poll and a duplicate plus a new one on later polls.
                    if (historyCallCount == 1)
                    {
                        events.Add(new Event { EventId = 1, EventType = EventType.ExecutionStarted });
                    }
                    else
                    {
                        events.Add(new Event { EventId = 1, EventType = EventType.ExecutionStarted });
                        events.Add(new Event { EventId = 2, EventType = EventType.ExecutionSucceeded });
                    }
                    return Task.FromResult(new GetDurableExecutionHistoryResponse { Events = events });
                });

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1", "--invoke-mode", "DurableExecution");

            Assert.True(await command.ExecuteAsync());

            Assert.Contains("ExecutionStarted", logger.Buffer);
            Assert.Contains("ExecutionSucceeded", logger.Buffer);
            Assert.Contains("status: SUCCEEDED", logger.Buffer);
            // Each event should only be written once even though the history is returned repeatedly.
            var startedOccurrences = logger.Buffer.Split(new[] { "ExecutionStarted" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(1, startedOccurrences);
        }

        [Fact]
        public async Task DurableExecutionDisplaysEventDetailsAndTruncatesLargePayloads()
        {
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1/durable-execution/abc/def";
            const string functionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1";

            // A result payload larger than the 1000 character display limit so we can verify truncation.
            var largePayload = new string('x', 2500);

            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new InvokeResponse { StatusCode = 202, DurableExecutionArn = durableArn }));
            mockLambda.Setup(client => client.GetDurableExecutionAsync(It.IsAny<GetDurableExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionResponse { Status = ExecutionStatus.SUCCEEDED }));

            mockLambda.Setup(client => client.GetDurableExecutionHistoryAsync(It.IsAny<GetDurableExecutionHistoryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetDurableExecutionHistoryResponse
                {
                    Events = new List<Event>
                    {
                        new Event
                        {
                            EventId = 1,
                            EventType = EventType.StepSucceeded,
                            Name = "MyStep",
                            StepSucceededDetails = new StepSucceededDetails
                            {
                                Result = new EventResult { Payload = largePayload }
                            }
                        },
                        new Event
                        {
                            EventId = 2,
                            EventType = EventType.StepFailed,
                            Name = "MyFailingStep",
                            StepFailedDetails = new StepFailedDetails
                            {
                                Error = new EventError
                                {
                                    Payload = new ErrorObject
                                    {
                                        ErrorType = "MyError",
                                        ErrorMessage = "something went wrong"
                                    }
                                }
                            }
                        }
                    }
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, functionArn, "--invoke-mode", "DurableExecution");

            Assert.True(await command.ExecuteAsync());

            // Detail fields should be expanded and displayed.
            Assert.Contains("Result:", logger.Buffer);
            Assert.Contains("Type: MyError", logger.Buffer);
            Assert.Contains("Message: something went wrong", logger.Buffer);

            // The large payload should be truncated and a truncation note shown.
            Assert.Contains("payload truncated, showing first 1000 of 2500 characters", logger.Buffer);
            Assert.DoesNotContain(new string('x', 1001), logger.Buffer);
            Assert.Contains(new string('x', 1000), logger.Buffer);
        }

        [Fact]
        public async Task DurableExecutionDoesNotReportTruncatedWhenPayloadIsNull()
        {
            const string durableArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1/durable-execution/abc/def";
            const string functionArn = "arn:aws:lambda:us-east-1:123456789012:function:fakeFunction:1";

            var mockLambda = CreateAvailableLambdaMock();
            mockLambda.Setup(client => client.InvokeAsync(It.IsAny<InvokeRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new InvokeResponse { StatusCode = 202, DurableExecutionArn = durableArn }));
            mockLambda.Setup(client => client.GetDurableExecutionAsync(It.IsAny<GetDurableExecutionRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new GetDurableExecutionResponse { Status = ExecutionStatus.SUCCEEDED }));

            mockLambda.Setup(client => client.GetDurableExecutionHistoryAsync(It.IsAny<GetDurableExecutionHistoryRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new GetDurableExecutionHistoryResponse
                {
                    Events = new List<Event>
                    {
                        // Result event whose payload is null but the service set Truncated to true.
                        new Event
                        {
                            EventId = 1,
                            EventType = EventType.StepSucceeded,
                            Name = "EmptyResultStep",
                            StepSucceededDetails = new StepSucceededDetails
                            {
                                Result = new EventResult { Payload = null, Truncated = true }
                            }
                        },
                        // Error event whose error payload is null but the service set Truncated to true.
                        new Event
                        {
                            EventId = 2,
                            EventType = EventType.StepFailed,
                            Name = "EmptyErrorStep",
                            StepFailedDetails = new StepFailedDetails
                            {
                                Error = new EventError { Payload = null, Truncated = true }
                            }
                        }
                    }
                }));

            var logger = new TestToolLogger();
            var command = CreateCommand(logger, mockLambda.Object, functionArn, "--invoke-mode", "DurableExecution");

            Assert.True(await command.ExecuteAsync());

            // The event headers are still written.
            Assert.Contains("EmptyResultStep", logger.Buffer);
            Assert.Contains("EmptyErrorStep", logger.Buffer);

            // But nothing should be written about a result/error payload or truncation when there was no payload.
            Assert.DoesNotContain("Result:", logger.Buffer);
            Assert.DoesNotContain("Error:", logger.Buffer);
            Assert.DoesNotContain("truncated", logger.Buffer);
        }
    }
}
