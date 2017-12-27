using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using Amazon.CloudWatchEvents;
using Amazon.CloudWatchLogs;
using Amazon.EC2;
using Amazon.ECR;
using Amazon.ECS;

using System.Reflection;
using ThirdParty.Json.LitJson;
using System.Text;
using System.IO;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.IdentityManagement.Model;
using System.Threading;

namespace Amazon.ECS.Tools.Commands
{

    public abstract class ECSBaseCommand : BaseCommand<ECSToolsDefaults>
    {




        public ECSBaseCommand(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        public ECSBaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
        {
        }

        protected override string ToolName => "AWSECSToolsDotnet";

        IAmazonCloudWatchEvents _cweClient;
        public IAmazonCloudWatchEvents CWEClient
        {
            get
            {
                if (this._cweClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonCloudWatchEventsConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._cweClient = new AmazonCloudWatchEventsClient(DetermineAWSCredentials(), config);
                }
                return this._cweClient;
            }
            set { this._cweClient = value; }
        }

        IAmazonCloudWatchLogs _cwlClient;
        public IAmazonCloudWatchLogs CWLClient
        {
            get
            {
                if (this._cwlClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonCloudWatchLogsConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._cwlClient = new AmazonCloudWatchLogsClient(DetermineAWSCredentials(), config);
                }
                return this._cwlClient;
            }
            set { this._cwlClient = value; }
        }

        IAmazonECR _ecrClient;
        public IAmazonECR ECRClient
        {
            get
            {
                if (this._ecrClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECRConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecrClient = new AmazonECRClient(DetermineAWSCredentials(), config);
                }
                return this._ecrClient;
            }
            set { this._ecrClient = value; }
        }

        IAmazonECS _ecsClient;
        public IAmazonECS ECSClient
        {
            get
            {
                if (this._ecsClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonECSConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ecsClient = new AmazonECSClient(DetermineAWSCredentials(), config);
                }
                return this._ecsClient;
            }
            set { this._ecsClient = value; }
        }

        IAmazonEC2 _ec2Client;
        public IAmazonEC2 EC2Client
        {
            get
            {
                if (this._ec2Client == null)
                {
                    SetUserAgentString();

                    var config = new AmazonEC2Config();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ec2Client = new AmazonEC2Client(DetermineAWSCredentials(), config);
                }
                return this._ec2Client;
            }
            set { this._ec2Client = value; }
        }

        public bool IsFargateLaunch(string property)
        {
            var launchType = this.GetStringValueOrDefault(property, ECSDefinedCommandOptions.ARGUMENT_LAUNCH_TYPE, true);
            bool isFargate = string.Equals(launchType, LaunchType.FARGATE, StringComparison.OrdinalIgnoreCase);
            return isFargate;
        }

        public async Task AttemptToCreateServiceLinkRoleAsync()
        {
            try
            {
                await this.IAMClient.CreateServiceLinkedRoleAsync(new CreateServiceLinkedRoleRequest
                {
                    AWSServiceName = "ecs.amazonaws.com"
                });
                this.Logger.WriteLine("Created IAM Role service role for ecs.amazonaws.com");

                this.Logger.WriteLine("Waiting for new IAM Role to propagate to AWS regions");
                long start = DateTime.Now.Ticks;
                while (TimeSpan.FromTicks(DateTime.Now.Ticks - start).TotalSeconds < RoleHelper.SLEEP_TIME_FOR_ROLE_PROPOGATION.TotalSeconds)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Console.Write(".");
                    Console.Out.Flush();
                }
                Console.WriteLine("\t Done");
            }
            catch(Exception)
            {

            }
        }
    }
}
