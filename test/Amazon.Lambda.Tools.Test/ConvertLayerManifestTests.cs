using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;



namespace Amazon.Lambda.Tools.Test
{
    public class ConvertLayerManifestTests
    {
        [Fact]
        public void CheckIfWebProject()
        {
            var originalContent = "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>";
            var result = LambdaUtilities.ConvertManifestContentToSdkManifest("netcoreapp2.1",originalContent);
            Assert.True(result.Updated);
            Assert.False(object.ReferenceEquals(originalContent, result.UpdatedContent));

            originalContent = "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>";
            result = LambdaUtilities.ConvertManifestContentToSdkManifest("netcoreapp2.1", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            Assert.False(result.Updated);
            Assert.True(object.ReferenceEquals(originalContent, result.UpdatedContent));
        }

        [Fact]
        public void ConvertAspNetCoreProject()
        {
            var testManifest = File.ReadAllText("./TestFiles/ManifestAspNetCoreProject.xml");
            
            var result = LambdaUtilities.ConvertManifestContentToSdkManifest("netcoreapp2.1",testManifest);
            Assert.True(result.Updated);   
            Assert.False(object.ReferenceEquals(testManifest, result.UpdatedContent));

            var xmlDoc = XDocument.Parse(result.UpdatedContent);

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

        [Fact]
        public void Convert31SDKProjectToManifest()
        {
            var originalContent = "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>";
            var result = LambdaUtilities.ConvertManifestContentToSdkManifest("netcoreapp3.1", originalContent);
            Assert.True(result.Updated);
            
            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", result.UpdatedContent);
            Assert.Contains("<PackagesToPrune Include=\"Microsoft.CSharp\" />", result.UpdatedContent);
            Assert.DoesNotContain("Microsoft.AspNetCore.App", result.UpdatedContent);
        }
        
        [Fact]
        public void Convert31SDKWebProjectToManifest()
        {
            var originalContent = "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>";
            var result = LambdaUtilities.ConvertManifestContentToSdkManifest("netcoreapp3.1", originalContent);
            Assert.True(result.Updated);
            
            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", result.UpdatedContent);
            Assert.Contains("<PackagesToPrune Include=\"Microsoft.CSharp\" />", result.UpdatedContent);
            Assert.Contains("Microsoft.AspNetCore.App", result.UpdatedContent);
        }
    }
}