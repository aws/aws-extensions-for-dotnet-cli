using System;
using Xunit;
using Amazon.Common.DotNetCli.Tools.Test.Mocks;
using System.Runtime.InteropServices;

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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var mockLogger = new MockToolLogger();
            Assert.True(PosixUserHelper.IsRunningInPosix);
            var user = PosixUserHelper.GetEffectiveUser(mockLogger);
            Assert.True(user.UserIDSet);
            Assert.True(user.GroupIDSet);
        }
        else
        {
            Assert.False(PosixUserHelper.IsRunningInPosix);
        }
    }    
}
