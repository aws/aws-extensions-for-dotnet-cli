using System;
using System.Collections.Generic;
using System.Text;

namespace Amazon.ElasticBeanstalk.Tools
{
    public static class EBConstants
    {
        public const string DEFAULT_MANIFEST = @"
{
  ""manifestVersion"": 1,
  ""deployments"": {

    ""aspNetCoreWeb"": [
      {
        ""name"": ""app"",
        ""parameters"": {
          ""appBundle"": ""."",
          ""iisPath"": ""{iisPath}"",
          ""iisWebSite"": ""{iisWebSite}""
        }
      }
    ]
  }
}
";

        public const string ENVIRONMENT_TYPE_SINGLEINSTANCE = "SingleInstance";
        public const string ENVIRONMENT_TYPE_LOADBALANCED = "LoadBalanced";

        public const string ENHANCED_HEALTH_TYPE_ENHANCED = "enhanced";
        public const string ENHANCED_HEALTH_TYPE_BASIC = "basic";
        public static readonly string[] ValidEnhanceHealthType = new string[]{ ENHANCED_HEALTH_TYPE_ENHANCED, ENHANCED_HEALTH_TYPE_BASIC };

        public const string LOADBALANCER_TYPE_APPLICATION = "application";
        public const string LOADBALANCER_TYPE_NETWORK = "network";
        public const string LOADBALANCER_TYPE_CLASSIC = "classic";
        public static readonly string[] ValidLoadBalancerType = new string[] { LOADBALANCER_TYPE_APPLICATION, LOADBALANCER_TYPE_NETWORK, LOADBALANCER_TYPE_CLASSIC };

    }
}
