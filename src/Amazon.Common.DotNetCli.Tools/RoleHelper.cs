using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Auth.AccessControlPolicy;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// Utility class for interacting with console user to select or create an IAM role
    /// </summary>
    public static class RoleHelper
    {
        public const string EC2_ASSUME_ROLE_PRINCIPAL = "ec2.amazonaws.com";
        public const string ECS_TASK_ASSUME_ROLE_PRINCIPAL = "ecs-tasks.amazonaws.com";

        public const int DEFAULT_ITEM_MAX = 20;
        private const int MAX_LINE_LENGTH_FOR_MANAGED_ROLE = 95;
        public static readonly TimeSpan SLEEP_TIME_FOR_ROLE_PROPOGATION = TimeSpan.FromSeconds(15);


        public static string GenerateUniqueIAMRoleName(IAmazonIdentityManagementService iamClient, string baseName)
        {
            var existingRoleNames = new HashSet<string>();
            var response = new ListRolesResponse();
            do
            {
                var roles = iamClient.ListRolesAsync(new ListRolesRequest { Marker = response.Marker }).Result.Roles;
                roles.ForEach(x => existingRoleNames.Add(x.RoleName));

            } while (response.IsTruncated);

            if (!existingRoleNames.Contains(baseName))
                return baseName;

            for (int i = 1; true; i++)
            {
                var name = baseName + "-" + i;
                if (!existingRoleNames.Contains(name))
                    return name;
            }
        }

        public static string ExpandInstanceProfile(IAmazonIdentityManagementService iamClient, string instanceProfile)
        {
            if (instanceProfile.StartsWith("arn:aws"))
                return instanceProfile;

            // Wrapping this in a task to avoid dealing with aggregate exception.
            var task = Task.Run<string>(async () =>
            {
                try
                {
                    var request = new GetInstanceProfileRequest { InstanceProfileName = instanceProfile };
                    var response = await iamClient.GetInstanceProfileAsync(request).ConfigureAwait(false);
                    return response.InstanceProfile.Arn;
                }
                catch (NoSuchEntityException)
                {
                    return null;
                }

            });

            if (task.Result == null)
            {
                throw new ToolsException($"Instance Profile \"{instanceProfile}\" can not be found.", ToolsException.CommonErrorCode.RoleNotFound);
            }

            return task.Result;
        }


        public static string ExpandRoleName(IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (roleName.StartsWith("arn:aws"))
                return roleName;

            // Wrapping this in a task to avoid dealing with aggregate exception.
            var task = Task.Run<string>(async () =>
            {
                try
                {
                    var request = new GetRoleRequest { RoleName = roleName };
                    var response = await iamClient.GetRoleAsync(request).ConfigureAwait(false);
                    return response.Role.Arn;
                }
                catch (NoSuchEntityException)
                {
                    return null;
                }

            });

            if(task.Result == null)
            {
                throw new ToolsException($"Role \"{roleName}\" can not be found.", ToolsException.CommonErrorCode.RoleNotFound);
            }

            return task.Result;
        }


        public static string ExpandManagedPolicyName(IAmazonIdentityManagementService iamClient, string managedPolicy)
        {
            if (managedPolicy.StartsWith("arn:aws"))
                return managedPolicy;

            // Wrapping this in a task to avoid dealing with aggregate exception.
            var task = Task.Run<string>(async () =>
            {
                var listResponse = new ListPoliciesResponse();
                do
                {
                    var listRequest = new ListPoliciesRequest { Marker = listResponse.Marker, Scope = PolicyScopeType.All };
                    listResponse = await iamClient.ListPoliciesAsync(listRequest).ConfigureAwait(false);
                    var policy = listResponse.Policies.FirstOrDefault(x => string.Equals(managedPolicy, x.PolicyName));
                    if (policy != null)
                        return policy.Arn;

                } while (listResponse.IsTruncated);

                return null;
            });

            if (task.Result == null)
            {
                throw new ToolsException($"Policy \"{managedPolicy}\" can not be found.", ToolsException.CommonErrorCode.PolicyNotFound);
            }

            return task.Result;
        }

        public static string CreateRole(IAmazonIdentityManagementService iamClient, string roleName, string assumeRolePolicy, params string[] managedPolicies)
        {
            if (managedPolicies != null && managedPolicies.Length > 0)
            {
                for(int i = 0; i < managedPolicies.Length; i++)
                {
                    managedPolicies[i] = ExpandManagedPolicyName(iamClient, managedPolicies[i]);
                }
            }

            string roleArn;
            try
            {
                CreateRoleRequest request = new CreateRoleRequest
                {
                    RoleName = roleName,
                    AssumeRolePolicyDocument = assumeRolePolicy
                };

                var response = iamClient.CreateRoleAsync(request).Result;
                roleArn = response.Role.Arn;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error creating IAM Role: {e.Message}", ToolsException.CommonErrorCode.IAMCreateRole, e);
            }

            if (managedPolicies != null && managedPolicies.Length > 0)
            {
                try
                {
                    foreach(var managedPolicy in managedPolicies)
                    {
                        var request = new AttachRolePolicyRequest
                        {
                            RoleName = roleName,
                            PolicyArn = managedPolicy
                        };
                        iamClient.AttachRolePolicyAsync(request).Wait();
                    }
                }
                catch (Exception e)
                {
                    throw new ToolsException($"Error assigning managed IAM Policy: {e.Message}", ToolsException.CommonErrorCode.IAMAttachRole, e);
                }
            }

            bool found = false;
            do
            {
                // There is no way check if the role has propagated yet so to
                // avoid error during deployment creation do a generous sleep.
                Console.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                long start = DateTime.Now.Ticks;
                while (TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalSeconds < SLEEP_TIME_FOR_ROLE_PROPOGATION.TotalSeconds)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.Write(".");
                    Console.Out.Flush();
                }
                Console.WriteLine("\t Done");


                try
                {
                    var getResponse = iamClient.GetRoleAsync(new GetRoleRequest { RoleName = roleName }).Result;
                    if (getResponse.Role != null)
                        found = true;
                }
                catch (NoSuchEntityException)
                {

                }
                catch (Exception e)
                {
                    throw new ToolsException("Error confirming new role was created: " + e.Message, ToolsException.CommonErrorCode.IAMGetRole, e);
                }
            } while (!found);


            return roleArn;
        }

        public static async Task<IList<ManagedPolicy>> FindManagedPoliciesAsync(IAmazonIdentityManagementService iamClient, int maxPolicies)
        {
            ListPoliciesRequest request = new ListPoliciesRequest
            {
                Scope = PolicyScopeType.AWS,
            };
            ListPoliciesResponse response = null;

            IList<ManagedPolicy> policies = new List<ManagedPolicy>();
            do
            {
                request.Marker = response?.Marker;
                response = await iamClient.ListPoliciesAsync(request).ConfigureAwait(false);

                foreach (var policy in response.Policies)
                {
                    if (policy.IsAttachable && KNOWN_MANAGED_POLICY_DESCRIPTIONS.ContainsKey(policy.PolicyName))
                        policies.Add(policy);

                    if (policies.Count == maxPolicies)
                        return policies;
                }

            } while (response.IsTruncated);

            response = await iamClient.ListPoliciesAsync(new ListPoliciesRequest
            {
                Scope = PolicyScopeType.Local
            });

            foreach (var policy in response.Policies)
            {
                if (policy.IsAttachable)
                    policies.Add(policy);

                if (policies.Count == maxPolicies)
                    return policies;
            }


            return policies;
        }

        public static async Task<IList<Role>> FindExistingRolesAsync(IAmazonIdentityManagementService iamClient, string assumeRolePrincpal, int maxRoles)
        {
            List<Role> roles = new List<Role>();

            ListRolesRequest request = new ListRolesRequest();
            ListRolesResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.Marker;

                response = await iamClient.ListRolesAsync(request).ConfigureAwait(false);

                foreach (var role in response.Roles)
                {
                    if (AssumeRoleServicePrincipalSelector(role, assumeRolePrincpal))
                    {
                        roles.Add(role);
                        if (roles.Count == maxRoles)
                        {
                            break;
                        }
                    }
                }

            } while (response.IsTruncated && roles.Count < maxRoles);

            return roles;

        }

        private static IList<Role> FindExistingRoles(IAmazonIdentityManagementService iamClient, string assumeRolePrincpal, int maxRoles)
        {
            var task = Task.Run<IList<Role>>(async () =>
            {
                return await FindExistingRolesAsync(iamClient, assumeRolePrincpal, maxRoles);
            });

            return task.Result;
        }

        private static bool AssumeRoleServicePrincipalSelector(Role r, string servicePrincipal)
        {
            if (string.IsNullOrEmpty(r.AssumeRolePolicyDocument))
                return false;

            try
            {
                var decode = WebUtility.UrlDecode(r.AssumeRolePolicyDocument);
                var policy = Policy.FromJson(decode);
                foreach (var statement in policy.Statements)
                {
                    if (statement.Actions.Contains(new ActionIdentifier("sts:AssumeRole")) &&
                        statement.Principals.Contains(new Principal("Service", servicePrincipal)))
                    {
                        return true;
                    }
                }
                return r.AssumeRolePolicyDocument.Contains(servicePrincipal);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<IList<InstanceProfile>> FindExistingInstanceProfilesAsync(IAmazonIdentityManagementService iamClient, int maxRoles)
        {
            var profiles = new List<InstanceProfile>();

            ListInstanceProfilesRequest request = new ListInstanceProfilesRequest();
            ListInstanceProfilesResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.Marker;

                response = await iamClient.ListInstanceProfilesAsync(request).ConfigureAwait(false);

                foreach (var profile in response.InstanceProfiles)
                {
                    profiles.Add(profile);
                }

            } while (response.IsTruncated && profiles.Count < maxRoles);

            return profiles;
        }


        static readonly Dictionary<string, string> KNOWN_MANAGED_POLICY_DESCRIPTIONS = new Dictionary<string, string>
        {
            {"PowerUserAccess","Provides full access to AWS services and resources, but does not allow management of users and groups."},
            {"AmazonS3FullAccess","Provides full access to all buckets via the AWS Management Console."},
            {"AmazonDynamoDBFullAccess","Provides full access to Amazon DynamoDB via the AWS Management Console."},
            {"CloudWatchLogsFullAccess","Provides full access to CloudWatch Logs"}
        };

        /// <summary>
        /// Because description does not come back in the list policy operation cache known policy descriptions to 
        /// help users understand which role to pick.
        /// </summary>
        /// <param name="policyArn"></param>
        /// <returns></returns>
        public static string AttemptToGetPolicyDescription(string policyArn)
        {
            string content;
            if (!KNOWN_MANAGED_POLICY_DESCRIPTIONS.TryGetValue(policyArn, out content))
                return null;

            return content;
        }
    }
}
