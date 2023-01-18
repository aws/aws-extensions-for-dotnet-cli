using System;
using System.Diagnostics;
using Xunit;
using Amazon.Common.DotNetCli.Tools.Test.Mocks;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.InteropServices;
#endif
namespace Amazon.Common.DotNetCli.Tools.Test;

public class PosixUserHelperTest
{
    
    /// <summary>
    /// This is more of a quasi-functional test than a unit test.
    /// If run under MacOS or Linux, it will confirm that UID and GID can be retrieved 
    /// </summary>
    [Fact]
    public void TestGetEffectiveUser()
    {
        #if NETCOREAPP3_1_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            var mockLogger = new MockToolLogger();
            Assert.True(PosixUserHelper.IsRunningInPosix);
            var user = PosixUserHelper.GetEffectiveUser(mockLogger);
            Assert.True(user.HasValue);
            Assert.True(user.Value.UserID >= 0);
            Assert.True(user.Value.GroupID >= 0);
        }
        else
        {
            Assert.False(PosixUserHelper.IsRunningInPosix);
        }
        #else
        Assert.False(PosixUserHelper.IsRunningInPosix);
        #endif
    }    
    
    /// <summary>
    /// This mocks execution of the "id" commands  
    /// </summary>
    [Fact]
    public void TestGetEffectiveUserSucceeds()
    {
        var mockProcesses = new MockIdProcessFactory(new[]
        {
            new ProcessInstance.ProcessResults
            {
                Executed = true,
                ExitCode = 0,
                Output = "998",
                Error = ""
            },
            new ProcessInstance.ProcessResults
            {
                Executed = true,
                ExitCode = 0,
                Output = "999",
                Error = ""
            }
        });
        
        var mockLogger = new MockToolLogger();
        var user = PosixUserHelper.GetEffectiveUser(mockLogger, mockProcesses);
        Assert.True(user.HasValue);
        Assert.True(user.Value.UserID >= 998);
        Assert.True(user.Value.GroupID >= 999);
    }    

    [Fact]
    public void TestGetEffectiveUserFails()
    {
        var mockProcesses = new MockIdProcessFactory(new[]
        {
            new ProcessInstance.ProcessResults
            {
                Executed = true,
                ExitCode = 0,
                Output = "998",
                Error = ""
            },
            new ProcessInstance.ProcessResults
            {
                Executed = true,
                ExitCode = -1,
                Output = "",
                Error = "Sad trombones"
            }
        });
        
        var mockLogger = new MockToolLogger();
        var user = PosixUserHelper.GetEffectiveUser(mockLogger, mockProcesses);
        Assert.False(user.HasValue);
        Assert.Equal( "Error executing \"id -g\" - exit code -1 Sad trombones\nWarning: Unable to get effective group from \"id -g\"\n", mockLogger.Log);
    }    

    /// <summary>
    /// Mock for process factory so that we can fake "id" results
    /// </summary>
    private class MockIdProcessFactory : IProcessFactory
    {
        private readonly ProcessInstance.ProcessResults[] _mockedResults;
        private int _counter;
        public MockIdProcessFactory(ProcessInstance.ProcessResults[] mockedResults)
        {
            _mockedResults = mockedResults;
        }
        
        public ProcessInstance.ProcessResults RunProcess(ProcessStartInfo info, int timeout = 60000)
        {
            if (_counter < _mockedResults.Length)
            {
                return _mockedResults[_counter++];
            }

            throw new Exception("Out of mocked process responses");
        }
    }
    
}
