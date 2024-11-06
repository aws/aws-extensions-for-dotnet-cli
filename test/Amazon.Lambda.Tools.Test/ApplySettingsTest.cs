using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.Model;
using Amazon.Lambda.Tools.Commands;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    public class ApplySettingsTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ApplySettingsTest(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task SetLoggingPropertiesForCreateRequest()
        {
            var mockClient = new Mock<IAmazonLambda>();

            mockClient.Setup(client => client.CreateFunctionAsync(It.IsAny<CreateFunctionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateFunctionRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal("JSON", request.LoggingConfig.LogFormat);
                    Assert.Equal("TheGroup", request.LoggingConfig.LogGroup);
                    Assert.Equal("DEBUG", request.LoggingConfig.ApplicationLogLevel);
                    Assert.Equal("WARN", request.LoggingConfig.SystemLogLevel);
                })
                .Returns((CreateFunctionRequest r, CancellationToken token) =>
                {
                    return Task.FromResult(new CreateFunctionResponse());
                });

            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var command = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.Configuration = "Release";
            command.Runtime = "dotnet8";
            command.LogFormat = "JSON";
            command.LogGroup = "TheGroup";
            command.LogApplicationLevel = "DEBUG";
            command.LogSystemLevel = "WARN";
            command.DisableInteractive = true;
            command.LambdaClient = mockClient.Object;

            var created = await command.ExecuteAsync();
            Assert.True(created);
        }

        [Fact]
        public async Task SetLoggingPropertiesForUpdateRequest()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;

            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var command = new DeployFunctionCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.FunctionName = "test-function-" + DateTime.Now.Ticks;
            command.Handler = "TestFunction::TestFunction.Function::ToUpper";
            command.Timeout = 10;
            command.MemorySize = 512;
            command.Role = await TestHelper.GetTestRoleArnAsync();
            command.Configuration = "Release";
            command.Runtime = "dotnet8";
            command.LogFormat = "JSON";
            command.LogGroup = "TheGroup";
            command.LogApplicationLevel = "DEBUG";
            command.LogSystemLevel = "WARN";
            command.DisableInteractive = true;

            var mockClient = new Mock<IAmazonLambda>();

            mockClient.Setup(client => client.GetFunctionConfigurationAsync(It.IsAny<GetFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Returns((GetFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    var response = new GetFunctionConfigurationResponse
                    {
                        FunctionName = command.FunctionName,
                        Handler = command.Handler,
                        Timeout = command.Timeout.Value,
                        MemorySize = command.MemorySize.Value,
                        Role = command.Role,
                        Runtime = command.Runtime
                    };

                    return Task.FromResult(response);
                });

            mockClient.Setup(client => client.UpdateFunctionConfigurationAsync(It.IsAny<UpdateFunctionConfigurationRequest>(), It.IsAny<CancellationToken>()))
                .Callback<UpdateFunctionConfigurationRequest, CancellationToken>((request, token) =>
                {
                    Assert.Equal("JSON", request.LoggingConfig.LogFormat);
                    Assert.Equal("TheGroup", request.LoggingConfig.LogGroup);
                    Assert.Equal("DEBUG", request.LoggingConfig.ApplicationLogLevel);
                    Assert.Equal("WARN", request.LoggingConfig.SystemLogLevel);
                })
                .Returns((UpdateFunctionConfigurationRequest r, CancellationToken token) =>
                {
                    return Task.FromResult(new UpdateFunctionConfigurationResponse());
                });


            command.LambdaClient = mockClient.Object;

            var created = await command.ExecuteAsync();
            Assert.True(created);
        }
    }
}
