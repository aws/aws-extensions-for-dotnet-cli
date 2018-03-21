using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.Common.DotNetCli.Tools
{
    public static class Constants
    {
        public static readonly string CWE_ASSUME_ROLE_POLICY =
@"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""events.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static readonly string ECS_ASSUME_ROLE_POLICY =
@"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""ecs.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static readonly string EC2_ASSUME_ROLE_POLICY =
@"
{
  ""Version"": ""2008-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""ec2.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static readonly string ECS_TASKS_ASSUME_ROLE_POLICY =
@"
{
  ""Version"": ""2008-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""ecs-tasks.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();


      public static readonly string LAMBDA_PRINCIPAL = "lambda.amazonaws.com";
      public static readonly string LAMBDA_ASSUME_ROLE_POLICY =
        @"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""lambda.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();
      
      


      
      public static readonly Dictionary<string, string> COMMON_KNOWN_MANAGED_POLICY_DESCRIPTIONS = new Dictionary<string, string>
      {
        {"PowerUserAccess","Provides full access to AWS services and resources, but does not allow management of users and groups."},
        {"AmazonS3FullAccess","Provides full access to all buckets via the AWS Management Console."},
        {"AmazonDynamoDBFullAccess","Provides full access to Amazon DynamoDB via the AWS Management Console."},
        {"CloudWatchLogsFullAccess","Provides full access to CloudWatch Logs"}
      };
    }
}
