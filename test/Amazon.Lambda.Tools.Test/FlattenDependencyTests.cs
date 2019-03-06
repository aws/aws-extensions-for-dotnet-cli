﻿using System;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.Tools.Commands;
using System.IO;
using System.Runtime.InteropServices;
using Amazon.Common.DotNetCli.Tools;
using Xunit.Abstractions;

namespace Amazon.Lambda.Tools.Test
{
    public class FlattenDependencyTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FlattenDependencyTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        private string GetTestProjectPath(string project)
        {
            var assembly = this.GetType().GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/FlattenDependencyTestProjects/" + project);
            return fullPath;
        }

        [Fact]
        public async Task NpgsqlTest()
        {
            var fullPath = GetTestProjectPath("NpgsqlExample");
            var command = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.DisableInteractive = true;
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp1.0";

            command.OutputPackageFileName = Path.GetTempFileName() + ".zip";

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                using (var archive = ZipFile.OpenRead(command.OutputPackageFileName))
                {
                    Assert.True(archive.GetEntry("Npgsql.dll") != null, "Failed to find Npgsql.dll");
                    Assert.True(archive.GetEntry("System.Net.NetworkInformation.dll") != null, "Failed to find System.Net.NetworkInformation.dll");
                    Assert.True(archive.GetEntry("runtimes/linux/lib/netstandard1.3/System.Net.NetworkInformation.dll") == null, "runtimes/linux/lib/netstandard1.3/System.Net.NetworkInformation.dll should not be zip file.");
                    ValidateNoRuntimeFolder(archive);
                }
            }
            finally
            {
                if (File.Exists(command.OutputPackageFileName))
                    File.Delete(command.OutputPackageFileName);
            }
        }

        [Fact]
        public async Task SqlClientTest()
        {
            var fullPath = GetTestProjectPath("SQLServerClientExample");
            var command = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.DisableInteractive = true;
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp1.0";

            command.OutputPackageFileName = Path.GetTempFileName() + ".zip";

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                using (var archive = ZipFile.OpenRead(command.OutputPackageFileName))
                {
                    Assert.True(archive.GetEntry("System.Data.SqlClient.dll") != null, "Failed to find System.Data.SqlClient.dll");
                    Assert.True(archive.GetEntry("System.IO.Pipes.dll") != null, "Failed to find System.IO.Pipes.dll");
                    Assert.True(archive.GetEntry("runtimes/linux/lib/netstandard1.3/System.Data.SqlClient.dll") == null, "runtimes/linux/lib/netstandard1.3/System.Data.SqlClient.dll should not be zip file.");
                    ValidateNoRuntimeFolder(archive);

                    MakeSureCorrectAssemblyWasPicked(archive, fullPath, "System.Data.SqlClient.dll", "runtimes/unix/lib/netstandard1.3");
                    MakeSureCorrectAssemblyWasPicked(archive, fullPath, "System.IO.Pipes.dll", "runtimes/unix/lib/netstandard1.3");
                }
            }
            finally
            {
                if (File.Exists(command.OutputPackageFileName))
                    File.Delete(command.OutputPackageFileName);
            }
        }

        [Fact]
        public async Task NativeDependencyExample()
        {
            var fullPath = GetTestProjectPath("NativeDependencyExample");
            var command = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.DisableInteractive = true;
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp1.0";

            command.OutputPackageFileName = Path.GetTempFileName() + ".zip";

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                using (var archive = ZipFile.OpenRead(command.OutputPackageFileName))
                {
                    Assert.True(archive.GetEntry("System.Diagnostics.TraceSource.dll") != null, "Failed to find System.Diagnostics.TraceSource.dll");
                    ValidateNoRuntimeFolder(archive);

                    MakeSureCorrectAssemblyWasPicked(archive, fullPath, "System.Diagnostics.TraceSource.dll", "runtimes/unix/lib/netstandard1.3");
                }
            }
            finally
            {
                if (File.Exists(command.OutputPackageFileName))
                    File.Delete(command.OutputPackageFileName);
            }
        }

        [Fact]
        public async Task NativeDependency2Example()
        {
            var fullPath = GetTestProjectPath("NativeDependencyExample2");
            var command = new PackageCommand(new TestToolLogger(_testOutputHelper), fullPath, new string[0]);
            command.DisableInteractive = true;
            command.Configuration = "Release";
            command.TargetFramework = "netcoreapp2.1";

            command.OutputPackageFileName = Path.GetTempFileName() + ".zip";

            var created = await command.ExecuteAsync();
            try
            {
                Assert.True(created);

                using (var archive = ZipFile.OpenRead(command.OutputPackageFileName))
                {
                    Assert.True(archive.GetEntry("libgit2-b0d9952.so") != null, "Failed to find libgit2-b0d9952.so");
                    ValidateNoRuntimeFolder(archive);

                    MakeSureCorrectAssemblyWasPicked(archive, "libgit2sharp.nativebinaries", "1.0.226", "libgit2-b0d9952.so", "runtimes/rhel-x64/native/");
                }
            }
            finally
            {
                if (File.Exists(command.OutputPackageFileName))
                    File.Delete(command.OutputPackageFileName);
            }
        }
        
        private ZipArchive GetNugetZip(string package, string version)
        {
            var packagesFolderPath =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "%UserProfile%\\.nuget\\packages"
                    : @"%HOME%/.nuget/packages";

            var packageFileName = package + "." + version + ".nupkg";
            var packagePath = Path.Combine(Environment.ExpandEnvironmentVariables(packagesFolderPath), package, version, packageFileName);

            if (!File.Exists(packagePath))
            {
                throw new InvalidOperationException($"{nameof(packagePath)} {packagePath} is not found");
            }

            return ZipFile.OpenRead(packagePath);
        }

        private void MakeSureCorrectAssemblyWasPicked(ZipArchive archive, string nuGetPackage, string packageVersion, string assembly, string path)
        {
            MemoryStream buffer = new MemoryStream();
            var entry = archive.GetEntry(assembly);
            using (var stream = entry.Open())
            {
                stream.CopyTo(buffer);
            }
            var archivedBites = buffer.ToArray();

            buffer = new MemoryStream();

            byte[] expectedBites;
            using (var nupkgArchive = GetNugetZip(nuGetPackage, packageVersion))
            {
                var nupkgEntry = nupkgArchive.GetEntry(path + assembly);

                using (var stream = nupkgEntry.Open())
                {
                    stream.CopyTo(buffer);
                }
                expectedBites = buffer.ToArray();
            }

            Assert.True(expectedBites.Length == archivedBites.Length, $"{assembly} has different size then expected");

            for (int i = 0; i < archivedBites.Length; i++)
            {
                Assert.True(archivedBites[i] == expectedBites[i], $"{assembly} has different bits then expected");
            }
        }

        private void MakeSureCorrectAssemblyWasPicked(ZipArchive archive, string projectLocation, string assembly, string path)
        {
            string publishLocation = Path.Combine(projectLocation, "bin", "Release", "netcoreapp1.0", "publish");

            MemoryStream buffer = new MemoryStream();
            var entry = archive.GetEntry(assembly);
            using (var stream = entry.Open())
            {
                stream.CopyTo(buffer);
            }
            var archivedBites = buffer.ToArray();

            buffer = new MemoryStream();
            using (var stream = File.OpenRead(Path.Combine(publishLocation, path, assembly)))
            {
                stream.CopyTo(buffer);
            }
            var expectedBites = buffer.ToArray();

            Assert.True(expectedBites.Length == archivedBites.Length, $"{assembly} has different size then expected");

            for(int i = 0; i < archivedBites.Length; i++)
            {
                Assert.True(archivedBites[i] == expectedBites[i], $"{assembly} has different bits then expected");
            }
        }

        private void ValidateNoRuntimeFolder(ZipArchive archive)
        {
            foreach(var entry in archive.Entries)
            {
                Assert.False(entry.FullName.StartsWith("runtimes/"), $"Found a file in the runtimes folder that wasn't flatten: {entry.FullName}");
            }
        }
    }
}
