using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;



namespace Amazon.Lambda.Tools.Test
{
    public class ConvertAspNerCoreManifestTests
    {
        [Fact]
        public void CheckIfWebProject()
        {
            var originalContent = "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>";
            var (shouldDelete, updatedContent) = LambdaUtilities.ConvertManifestContentToSdkManifest(originalContent);
            Assert.True(shouldDelete);
            Assert.False(object.ReferenceEquals(originalContent, updatedContent));

            originalContent = "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>";
            (shouldDelete, updatedContent) = LambdaUtilities.ConvertManifestContentToSdkManifest("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            Assert.False(shouldDelete);
            Assert.True(object.ReferenceEquals(originalContent, updatedContent));
        }

        [Fact]
        public void ConvertAspNetCoreProject()
        {
            var testManifest = File.ReadAllText("./TestFiles/ManifestAspNetCoreProject.xml");
            
            var (shouldDelete, updatedContent) = LambdaUtilities.ConvertManifestContentToSdkManifest(testManifest);
            Assert.True(shouldDelete);   
            Assert.False(object.ReferenceEquals(testManifest, updatedContent));

            var xmlDoc = XDocument.Parse(updatedContent);

            Assert.Equal("Microsoft.NET.Sdk", xmlDoc.Root.Attribute("Sdk")?.Value);

            var packageReferences = xmlDoc.XPathSelectElements("//ItemGroup/PackageReference").ToList();

            Func<string, XElement> findRef = (name) =>
                {
                    return packageReferences.FirstOrDefault(x =>
                        string.Equals(name, x.Attribute("Include")?.Value, StringComparison.OrdinalIgnoreCase));
                };
            
            var netCoreAppRef = packageReferences.FirstOrDefault(x =>
                string.Equals("Microsoft.NETCore.App", x.Attribute("Update")?.Value, StringComparison.OrdinalIgnoreCase));
            
            Assert.NotNull(netCoreAppRef);
            Assert.Equal("false", netCoreAppRef.Attribute("Publish")?.Value);
            
            Assert.NotNull(findRef("Microsoft.AspNetCore.App"));
            Assert.NotNull(findRef("Amazon.Lambda.AspNetCoreServer"));
            Assert.NotNull(findRef("AWSSDK.S3"));
            Assert.NotNull(findRef("AWSSDK.Extensions.NETCore.Setup"));
        }
    }
}