using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class RelativePathTests
    {
        [Theory]
        [InlineData(@"C:\Code\Solution\Project", @"C:\Code\Solution", "../", false)]
        [InlineData(@"c:\code\solution\project", @"C:\Code\Solution", "../", false)]
        [InlineData(@"C:\Code\Solution\ProjectA", @"C:\Code\Solution\ProjectB\file.csproj", "../ProjectB/file.csproj", false)]
        [InlineData(@"C:\Code\Solution\Project", @"D:\Code\Solution", "D:/Code/Solution", true)]
        [InlineData(@"C:\Code\Solution", @"C:\Code\Solution\Project", "Project", false)]
        [InlineData(@"C:\Code\Solution", @"C:\Code\Solution\Project\foo.cs", "Project/foo.cs", false)]
        [InlineData(@"/home/user/Solution/Project", @"/home/user/Solution", "../", false)]
        [InlineData(@"/home/user/Solution", @"/home/user/Solution/Project", "Project", false)]
        public void ToParentDirectory(string start, string relativeTo, string expected, bool windowsOnly)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && windowsOnly)
                return;
            
            Assert.Equal(expected, Utilities.RelativePathTo(start, relativeTo));
        }
    }
}
