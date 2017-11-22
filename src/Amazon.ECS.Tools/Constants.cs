using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools
{
    public static class Constants
    {

        public static readonly string ECS_DEFAULT_SERVICE_POLICY =
@"
{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
            ""Effect"": ""Allow"",
            ""Action"": [
                ""ec2:AuthorizeSecurityGroupIngress"",
                ""ec2:Describe*"",
                ""elasticloadbalancing:DeregisterInstancesFromLoadBalancer"",
                ""elasticloadbalancing:DeregisterTargets"",
                ""elasticloadbalancing:Describe*"",
                ""elasticloadbalancing:RegisterInstancesWithLoadBalancer"",
                ""elasticloadbalancing:RegisterTargets""
            ],
            ""Resource"": ""*""
        }
    ]
}
".Trim();

    }
}
