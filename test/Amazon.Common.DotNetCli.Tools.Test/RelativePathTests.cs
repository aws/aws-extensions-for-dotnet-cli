using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test
{
    public class RelativePathTests
    {
        [Theory]
        [InlineData(@"C:\Code\Solution\Project", @"C:\Code\Solution", "../")]
        [InlineData(@"c:\code\solution\project", @"C:\Code\Solution", "../")]
        [InlineData(@"C:\Code\Solution\ProjectA", @"C:\Code\Solution\ProjectB\file.csproj", "../ProjectB/file.csproj")]
        [InlineData(@"C:\Code\Solution\Project", @"D:\Code\Solution", "D:/Code/Solution")]
        [InlineData(@"C:\Code\Solution", @"C:\Code\Solution\Project", "Project")]
        [InlineData(@"C:\Code\Solution", @"C:\Code\Solution\Project\foo.cs", "Project/foo.cs")]
        [InlineData(@"/home/user/Solution/Project", @"/home/user/Solution", "../")]
        [InlineData(@"/home/user/Solution", @"/home/user/Solution/Project", "Project")]
        public void ToParentDirectory(string start, string relativeTo, string expected)
        {
            Assert.Equal(expected, Utilities.RelativePathTo(start, relativeTo));
        }
    }
}
