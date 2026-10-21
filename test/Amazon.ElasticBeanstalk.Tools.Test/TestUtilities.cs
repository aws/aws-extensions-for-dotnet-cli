// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Amazon.ElasticBeanstalk.Tools.Test
{
    public static class TestUtilities
    {
        public static string TestBeanstalkWebAppPath
        {
            get
            {
                var assembly = typeof(TestUtilities).GetTypeInfo().Assembly;

                var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/TestBeanstalkWebApp");
                return fullPath;
            }
        }
    }

    /// <summary>
    /// Gives a test class its own copy of testapps/TestBeanstalkWebApp to publish from.
    /// xUnit runs test classes in parallel, and two classes running "dotnet publish" against
    /// the same project directory race on obj/ and fail with MSB3371. Copying the project
    /// source per class keeps the classes parallel while removing the shared state.
    /// The copy lives under testapps/Temp/ (gitignored) so repo-level build config still applies.
    /// </summary>
    public class TestBeanstalkWebAppFixture : IDisposable
    {
        private static readonly HashSet<string> ExcludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj" };

        public string ProjectPath { get; }

        public TestBeanstalkWebAppFixture()
        {
            var source = TestUtilities.TestBeanstalkWebAppPath;
            var testappsDir = Path.GetDirectoryName(source);
            ProjectPath = Path.Combine(testappsDir, "Temp", "TestBeanstalkWebApp-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            CopyDirectory(source, ProjectPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(ProjectPath))
                    Directory.Delete(ProjectPath, recursive: true);
            }
            catch
            {
                // Best effort cleanup; a leftover directory under testapps/Temp is gitignored.
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                var name = Path.GetFileName(directory);
                if (ExcludedDirectories.Contains(name))
                    continue;

                CopyDirectory(directory, Path.Combine(destination, name));
            }
        }
    }
}
