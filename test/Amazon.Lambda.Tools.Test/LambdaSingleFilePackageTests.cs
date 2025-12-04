using Amazon.Common.DotNetCli.Tools;
using Amazon.Lambda.Tools.Commands;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    [Collection("SingleFileTests")]
    public class LambdaSingleFilePackageTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public LambdaSingleFilePackageTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task PackageToUpperNoAOTSettingWithArgument()
        {
            var tempFile = Path.GetTempFileName() + ".zip";
            try
            {
                var assembly = this.GetType().GetTypeInfo().Assembly;
                var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/SingeFileLambdaFunctions/ToUpperFunctionNoAOT.cs");
                var command = new PackageCommand(new TestToolLogger(_testOutputHelper), Environment.CurrentDirectory, new string[] {fullPath, tempFile });

                var created = await command.ExecuteAsync();
                Assert.True(created);

                Assert.True(File.Exists(tempFile));
                Assert.True(new FileInfo(tempFile).Length > 0);
            }
            finally
            {
                   if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task PackageToUpperNoAOTSettingWithProjectLocation()
        {
            var tempFile = Path.GetTempFileName() + ".zip";
            try
            {
                var assembly = this.GetType().GetTypeInfo().Assembly;
                var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/SingeFileLambdaFunctions/ToUpperFunctionNoAOT.cs");
                var command = new PackageCommand(new TestToolLogger(_testOutputHelper), Environment.CurrentDirectory, new string[] { tempFile, "--project-location", fullPath });

                var created = await command.ExecuteAsync();
                Assert.True(created);

                Assert.True(File.Exists(tempFile));
                Assert.True(new FileInfo(tempFile).Length > 0);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Theory]
        [InlineData("ToUpperFunctionNoAOT.cs", false, "")]
        [InlineData("ToUpperFunctionImplicitAOT.cs", true, "")]
        [InlineData("ToUpperFunctionNoAOT.cs", true, "/p:publishaot=true")]
        [InlineData("ToUpperFunctionImplicitAOT.cs", false, "/p:publishaot=false")]
        public void ConfirmUsingNativeAOT(string filename, bool isAot, string msBuildParameters)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + $"../../../../../../testapps/SingeFileLambdaFunctions/{filename}");
            var actualAot = Utilities.LookPublishAotFlag(fullPath, msBuildParameters);

            Assert.Equal(isAot, actualAot);
        }

        [Fact]
        public void DeterminePublishLocationForSingleFile()
        {
            string fullPath;
            string expectedPublishLocation;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullPath = @"C:\functions\helloworld.cs";
                expectedPublishLocation = @"C:/functions/artifacts/helloworld";
            }
            else
            {
                fullPath = @"/functions/helloworld.cs";
                expectedPublishLocation = @"/functions/artifacts/helloworld";
            }

            var publishLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net10.0");
            Assert.Equal(expectedPublishLocation, publishLocation.Replace("\\", "/"));
        }

        [Fact]
        public void DeterminePublishLocationForSingleFileWithMixedSlashes()
        {
            string fullPath;
            string expectedPublishLocation;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullPath = @"C:\functions/subfolder\myfunction.cs";
                expectedPublishLocation = @"C:/functions/subfolder/artifacts/myfunction";
            }
            else
            {
                fullPath = @"/functions/subfolder/myfunction.cs";
                expectedPublishLocation = @"/functions/subfolder/artifacts/myfunction";
            }

            var publishLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net10.0");
            Assert.Equal(expectedPublishLocation, publishLocation.Replace("\\", "/"));
        }

        [Fact]
        public void DeterminePublishLocationForSingleFileWithDifferentConfigurations()
        {
            string fullPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullPath = @"C:\functions\myfunction.cs";
            }
            else
            {
                fullPath = @"/functions/myfunction.cs";
            }

            // Configuration parameter doesn't affect single file publish location (unlike regular projects)
            var releaseLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net10.0");
            var debugLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Debug", "net10.0");
            
            // Both should point to the same artifacts folder since configuration is not part of the path for single files
            Assert.Equal(releaseLocation, debugLocation);
        }

        [Fact]
        public void DeterminePublishLocationForSingleFileWithDifferentTargetFrameworks()
        {
            string fullPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullPath = @"C:\functions\myfunction.cs";
            }
            else
            {
                fullPath = @"/functions/myfunction.cs";
            }

            // Target framework parameter doesn't affect single file publish location (unlike regular projects)
            var net10Location = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net10.0");
            var net9Location = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net9.0");
            
            // Both should point to the same artifacts folder since target framework is not part of the path for single files
            Assert.Equal(net10Location, net9Location);
        }

        [Fact]
        public void DeterminePublishLocationForSingleFileVsRegularProject()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            
            // Test with single file - should use artifacts folder
            var singleFilePath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/SingeFileLambdaFunctions/ToUpperFunctionNoAOT.cs");
            var singleFilePublishLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, singleFilePath, "Release", "net10.0");
            
            // Single file should have "artifacts" in the path
            Assert.Contains("artifacts", singleFilePublishLocation);
            Assert.Contains("ToUpperFunctionNoAOT", singleFilePublishLocation);
            
            // Test with regular project - should use bin/Release/targetFramework/publish
            var regularProjectPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var regularProjectPublishLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, regularProjectPath, "Release", "net6.0");
            
            // Regular project should have "bin" and "publish" in the path
            Assert.Contains("bin", regularProjectPublishLocation);
            Assert.Contains("publish", regularProjectPublishLocation);
            Assert.Contains("Release", regularProjectPublishLocation);
            Assert.Contains("net6.0", regularProjectPublishLocation);
        }

        [Fact]
        public void DeterminePublishLocationForSingleFileWithSpecialCharacters()
        {
            string fullPath;
            string expectedPublishLocation;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullPath = @"C:\functions\my-function_v2.cs";
                expectedPublishLocation = @"C:/functions/artifacts/my-function_v2";
            }
            else
            {
                fullPath = @"/functions/my-function_v2.cs";
                expectedPublishLocation = @"/functions/artifacts/my-function_v2";
            }

            var publishLocation = Utilities.DeterminePublishLocation(Environment.CurrentDirectory, fullPath, "Release", "net10.0");
            Assert.Equal(expectedPublishLocation, publishLocation.Replace("\\", "/"));
        }

        [Theory]
        [InlineData("ToUpperFunctionNoAOT.cs", true)]
        [InlineData("ToUpperFunctionNoAOT.vb", false)]
        [InlineData("ToUpperFunctionNoAOT.csproj", false)]
        [InlineData("ToUpperFunctionNoAOT", false)]
        public void ConfirmSingleFileFlagSet(string filename, bool isSingleFile)
        {
            Assert.Equal(isSingleFile, Utilities.IsSingleFileCSharpFile(filename));
        }

        [Fact]
        public void GetSolutionDirectoryForSingleFile()
        {
            string projectLocation;
            string expectedSolutionDirectory;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                projectLocation = @"C:\functions\myfunction.cs";
                expectedSolutionDirectory = @"C:\functions";
            }
            else
            {
                projectLocation = @"/functions/myfunction.cs";
                expectedSolutionDirectory = @"/functions";
            }

            var solutionDirectory = Utilities.GetSolutionDirectoryFullPath(Environment.CurrentDirectory, projectLocation, null);
            Assert.Equal(expectedSolutionDirectory, solutionDirectory);
        }

        [Fact]
        public void GetSolutionDirectoryForSingleFileWithMixedSlashes()
        {
            string projectLocation;
            string expectedSolutionDirectory;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                projectLocation = @"C:\functions/subfolder\myfunction.cs";
                expectedSolutionDirectory = @"C:\functions\subfolder";
            }
            else
            {
                projectLocation = @"/functions/subfolder/myfunction.cs";
                expectedSolutionDirectory = @"/functions/subfolder";
            }

            var solutionDirectory = Utilities.GetSolutionDirectoryFullPath(Environment.CurrentDirectory, projectLocation, null);
            Assert.Equal(expectedSolutionDirectory, solutionDirectory);
        }

        [Fact]
        public void GetSolutionDirectoryForSingleFileVsRegularProject()
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            
            // Test with single file - should return parent directory
            var singleFilePath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/SingeFileLambdaFunctions/ToUpperFunctionNoAOT.cs");
            var singleFileSolutionDir = Utilities.GetSolutionDirectoryFullPath(Environment.CurrentDirectory, singleFilePath, null);
            var expectedSingleFileDir = Path.GetDirectoryName(singleFilePath);
            Assert.Equal(expectedSingleFileDir, singleFileSolutionDir);

            // Test with regular project - should search up for solution file
            var regularProjectPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestFunction");
            var regularProjectSolutionDir = Utilities.GetSolutionDirectoryFullPath(Environment.CurrentDirectory, regularProjectPath, null);
            // For regular projects, it searches up the directory tree, so it should be different behavior
            Assert.NotNull(regularProjectSolutionDir);
        }

        [Fact]
        public void GetSolutionDirectoryWithExplicitSolutionDirectory()
        {
            string projectLocation;
            string givenSolutionDirectory;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                projectLocation = @"C:\functions\myfunction.cs";
                givenSolutionDirectory = @"C:\mysolution";
            }
            else
            {
                projectLocation = @"/functions/myfunction.cs";
                givenSolutionDirectory = @"/mysolution";
            }

            // When an explicit solution directory is provided, it should use that regardless of single file
            var solutionDirectory = Utilities.GetSolutionDirectoryFullPath(Environment.CurrentDirectory, projectLocation, givenSolutionDirectory);
            Assert.Equal(givenSolutionDirectory, solutionDirectory);
        }
    }
}
