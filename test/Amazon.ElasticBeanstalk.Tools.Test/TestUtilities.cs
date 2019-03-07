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
}
