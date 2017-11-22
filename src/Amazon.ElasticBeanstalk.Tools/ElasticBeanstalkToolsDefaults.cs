using System;
using System.Collections.Generic;
using System.Text;

using ThirdParty.Json.LitJson;
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
            : this(new JsonData(), sourceFile)
        {
        }

        public ElasticBeanstalkToolsDefaults(JsonData data, string sourceFile)
            : base(data, sourceFile)
        {
        }


        public override string DefaultConfigFileName => DEFAULT_FILE_NAME;
    }
}
