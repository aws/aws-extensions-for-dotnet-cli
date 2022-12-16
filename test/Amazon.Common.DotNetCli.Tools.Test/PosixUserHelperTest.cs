using System;
using Xunit;
using Amazon.Common.DotNetCli.Tools.Test.Mocks;

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
        if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
        {
            var mockLogger = new MockToolLogger();
            Assert.True(PosixUserHelper.IsRunningInPosix);
            var user = PosixUserHelper.GetEffectiveUser(mockLogger);
            Assert.NotEqual(uint.MaxValue, user.UserID);
            Assert.NotEqual(uint.MaxValue, user.GroupID);
        }
        else
        {
            Assert.False(PosixUserHelper.IsRunningInPosix);
        }
    }    
}
