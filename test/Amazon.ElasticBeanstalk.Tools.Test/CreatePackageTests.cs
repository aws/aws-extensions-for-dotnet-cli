using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

using Amazon.ElasticBeanstalk.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amazon.ElasticBeanstalk.Tools.Test
{
    public class CreatePackageTests
    {
        [Fact]
        public async Task NoIISSettings()
        {
            var outputPackage = Path.GetTempFileName();
            var packageCommand = new PackageCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath, new string[] { "--output-package", outputPackage });
            packageCommand.DisableInteractive = true;
            await packageCommand.ExecuteAsync();
            Assert.Null(packageCommand.LastToolsException);

            var manifest = ReadManifestFromPackage(outputPackage);
            var appInManifest = manifest["deployments"]["aspNetCoreWeb"][0];
            Assert.NotNull(appInManifest);

            var appInManifestParameters = appInManifest["parameters"];
            Assert.Equal(".", appInManifestParameters["appBundle"].ToString());
            Assert.Equal("/", appInManifestParameters["iisPath"].ToString());
            Assert.Equal("Default Web Site", appInManifestParameters["iisWebSite"].ToString());
        }

        [Fact]
        public async Task ExplicitIISSettings()
        {
            var outputPackage = Path.GetTempFileName();
            var packageCommand = new PackageCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath, 
                new string[] { "--output-package", outputPackage, "--iis-website", "The WebSite", "--app-path", "/child" });
            packageCommand.DisableInteractive = true;
            await packageCommand.ExecuteAsync();

            var manifest = ReadManifestFromPackage(outputPackage);
            var appInManifest = manifest["deployments"]["aspNetCoreWeb"][0];
            Assert.NotNull(appInManifest);

            var appInManifestParameters = appInManifest["parameters"];
            Assert.Equal(".", appInManifestParameters["appBundle"].ToString());
            Assert.Equal("/child", appInManifestParameters["iisPath"].ToString());
            Assert.Equal("The WebSite", appInManifestParameters["iisWebSite"].ToString());
        }

        [Fact]
        public async Task ExpandEnvironmentVariable()
        {
            const string envName = "EB_OUTPUT_PACKAGE";
            var outputPackage = Path.GetTempFileName();
            Environment.SetEnvironmentVariable(envName, outputPackage);
            try
            {
                var packageCommand = new PackageCommand(new ConsoleToolLogger(), TestUtilities.TestBeanstalkWebAppPath, new string[] { "--config-file", "env-eb-config.json" });
                await packageCommand.ExecuteAsync();

                var manifest = ReadManifestFromPackage(outputPackage);
                var appInManifest = manifest["deployments"]["aspNetCoreWeb"][0];
                Assert.NotNull(appInManifest);

                var appInManifestParameters = appInManifest["parameters"];
                Assert.Equal(".", appInManifestParameters["appBundle"].ToString());
                Assert.Equal("/", appInManifestParameters["iisPath"].ToString());
                Assert.Equal("Default Web Site", appInManifestParameters["iisWebSite"].ToString());
            }
            finally
            {
                Environment.SetEnvironmentVariable(envName, null);
            }
        }


        public JObject ReadManifestFromPackage(string packagePath)
        {
            using (var zipStream = File.Open(packagePath, FileMode.Open))
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                var entry = zipArchive.Entries.FirstOrDefault(x => string.Equals(x.Name, "aws-windows-deployment-manifest.json"));
                if (entry == null)
                    throw new Exception("Failed to find aws-windows-deployment-manifest.json in package bundle");

                using (var entryReader = new StreamReader(entry.Open()))
                {
                    return JsonConvert.DeserializeObject(entryReader.ReadToEnd()) as JObject;
                }
            }
        }



    }
}
