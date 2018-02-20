using System;
using System.IO;
using System.Reflection;

namespace Amazon.Tools.TestHelpers
{
    public static class TestUtilities
    {
        public static string GetTestProjectPath(string projectName)
        {
            var assembly = typeof(TestUtilities).GetTypeInfo().Assembly;
            var fullPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) + "../../../../../../testapps/");
            fullPath = Path.Combine(fullPath, projectName);
            return fullPath;
        }
    }
}
