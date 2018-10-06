using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace Amazon.Lambda.Tools.Test
{
    public class ConvertToMapOfFilesTests
    {



        [Theory]
        [InlineData("app.js")]
        [InlineData("./app.js")]
        [InlineData("dir/app.js")]
        [InlineData("./dir/app.js")]
        public void FileCombinations(string file)
        {
            var files = LambdaPackager.ConvertToMapOfFiles(GetTestRoot(), new string[] { file });
            Assert.Single(files);

            Assert.True(files.ContainsKey(file));
            Assert.Equal($"{GetTestRoot()}/{file}", files[file]);
        }

        [Theory]
        [InlineData("app.js")]
        [InlineData("./app.js")]
        [InlineData("dir/app.js")]
        [InlineData("./dir/app.js")]
        public void RootPathWithTrailingSlash(string file)
        {
            var files = LambdaPackager.ConvertToMapOfFiles(GetTestRoot() + "/", new string[] { file });
            Assert.Single(files);

            Assert.True(files.ContainsKey(file));
            Assert.Equal($"{GetTestRoot()}/{file}", files[file]);
        }


        public static string GetTestRoot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return @"c:/temp";

            return "/home/norm/temp";
        }
    }
}
