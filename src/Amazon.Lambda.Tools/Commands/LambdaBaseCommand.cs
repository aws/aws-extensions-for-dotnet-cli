using System.Collections.Generic;

using Amazon.CloudFormation;

using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools.Options;

namespace Amazon.Lambda.Tools.Commands
{

    public abstract class LambdaBaseCommand : BaseCommand<LambdaToolsDefaults>
    {
        public LambdaBaseCommand(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        public LambdaBaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
        {
        }

        protected override string ToolName => "AWSLambdaToolsDotnet";
        


        IAmazonLambda _lambdaClient;
        public IAmazonLambda LambdaClient
        {
            get
            {
                if (this._lambdaClient != null) return this._lambdaClient;

                SetUserAgentString();
                var config = new AmazonLambdaConfig {RegionEndpoint = DetermineAWSRegion()};

                this._lambdaClient = new AmazonLambdaClient(DetermineAWSCredentials(), config);
                return this._lambdaClient;
            }
            set { this._lambdaClient = value; }
        }

        IAmazonCloudFormation _cloudFormationClient;
        public IAmazonCloudFormation CloudFormationClient
        {
            get
            {
                if (this._cloudFormationClient == null)
                {
                    SetUserAgentString();

                    AmazonCloudFormationConfig config =
                        new AmazonCloudFormationConfig {RegionEndpoint = DetermineAWSRegion()};


                    this._cloudFormationClient = new AmazonCloudFormationClient(DetermineAWSCredentials(), config);
                }
                return this._cloudFormationClient;
            }
            set { this._cloudFormationClient = value; }
        }
    }
}
