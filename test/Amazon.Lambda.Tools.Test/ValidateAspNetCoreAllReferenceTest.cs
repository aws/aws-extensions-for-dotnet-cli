using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Xunit;

using Amazon.Lambda;

namespace Amazon.Lambda.Tools.Test
{
    public class ValidateAspNetCoreAllReferenceTest
    {
        [Fact]
        public void NETCore_2_0_NewerAspNetCoreReference()
        {
            var logger = new TestToolLogger();
            var manifest = File.ReadAllText(@"ManifestTestFiles/SampleManifest.xml");
            var projectFile = File.ReadAllText(@"ManifestTestFiles/NewerAspNetCoreReference.xml");

            try
            {
                LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectContent(logger, "netcoreapp2.0", manifest, projectFile);
                Assert.True(true, "Missing LambdaToolsException thrown");
            }
            catch(LambdaToolsException e)
            {
                Assert.Contains("which is newer", e.Message);
            }
        }

        [Fact]
        public void NETCore_2_0_CurrentAspNetCoreReference()
        {
            var logger = new TestToolLogger();
            var manifest = File.ReadAllText(@"ManifestTestFiles/SampleManifest.xml");
            var projectFile = File.ReadAllText(@"ManifestTestFiles/CurrentAspNetCoreReference.xml");

            LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectContent(logger, "netcoreapp2.0", manifest, projectFile);

            Assert.DoesNotContain("error", logger.Buffer.ToLower());
        }

        [Fact]
        public void NETCore_2_0_NotUsingAspNetCore()
        {
            var logger = new TestToolLogger();
            var manifest = File.ReadAllText(@"ManifestTestFiles/SampleManifest.xml");
            var projectFile = File.ReadAllText(@"ManifestTestFiles/CurrentAspNetCoreReference.xml");

            LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectContent(logger, "netcoreapp2.0", manifest, projectFile);

            Assert.DoesNotContain("error", logger.Buffer.ToLower());
        }

        [Fact]
        public void NETCore_2_1_ErrorWithAspNetCoreAllSpecifingVersion()
        {
            var logger = new TestToolLogger();
            var manifest = File.ReadAllText(@"ManifestTestFiles/SampleManifest-v2.1.xml");
            var projectFile = File.ReadAllText(@"ManifestTestFiles/NETCore_2_1_AllWithVersionNumber.xml");


            try
            {
                LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectContent(logger, "netcoreapp2.1", manifest, projectFile);
                Assert.True(true, "Missing LambdaToolsException thrown");
            }
            catch (LambdaToolsException e)
            {
                Assert.Contains(".NET Core 2.1", e.Message);
            }
        }

        [Fact]
        public void NETCore_2_1_WithoutAspNetCoreAllSpecifingVersion()
        {
            var logger = new TestToolLogger();
            var manifest = File.ReadAllText(@"ManifestTestFiles/SampleManifest-v2.1.xml");
            var projectFile = File.ReadAllText(@"ManifestTestFiles/NETCore_2_1_AllWithoutVersionNumber.xml");


            LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectContent(logger, "netcoreapp2.1", manifest, projectFile);
        }

        [Theory]
        [InlineData(@"ManifestTestFiles/ProjectFilesAspNetCoreAllValidation/csharp")]
        [InlineData(@"ManifestTestFiles/ProjectFilesAspNetCoreAllValidation/fsharp")]
        [InlineData(@"ManifestTestFiles/ProjectFilesAspNetCoreAllValidation/vb")]
        public void FindProjFiles(string projectDirectory)
        {
            var logger = new TestToolLogger();
            string manifest = LambdaUtilities.LoadPackageStoreManifest(logger, "netcoreapp2.0");

            LambdaUtilities.ValidateMicrosoftAspNetCoreAllReferenceFromProjectPath(logger, "netcoreapp2.0", manifest, projectDirectory);

            Assert.DoesNotContain("error", logger.Buffer.ToLower());
        }
    }
}
