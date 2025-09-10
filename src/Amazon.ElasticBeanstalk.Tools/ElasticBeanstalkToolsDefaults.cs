using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Amazon.Common.DotNetCli.Tools;

namespace Amazon.ElasticBeanstalk.Tools
{
    public class ElasticBeanstalkToolsDefaults : DefaultConfigFile
    {
        public const string DEFAULT_FILE_NAME = "aws-beanstalk-tools-defaults.json";

        public ElasticBeanstalkToolsDefaults()
        {

        }

        public ElasticBeanstalkToolsDefaults(string sourceFile)
            : this(new JsonElement(), sourceFile)
        {
        }

        public ElasticBeanstalkToolsDefaults(JsonElement data, string sourceFile)
            : base(data, sourceFile)
        {
        }


        public override string DefaultConfigFileName => DEFAULT_FILE_NAME;
    }
}
