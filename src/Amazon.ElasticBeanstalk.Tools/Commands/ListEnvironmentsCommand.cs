using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.ElasticBeanstalk.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdParty.Json.LitJson;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public class ListEnvironmentsCommand : EBBaseCommand
    {
        public const string COMMAND_NAME = "list-environments";
        public const string COMMAND_DESCRIPTION = "List the AWS Elastic Beanstalk environments.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION
        });

        public ListEnvironmentsCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);
        }

        protected override async Task<bool> PerformActionAsync()
        {
            try
            {
                var response = new DescribeEnvironmentsResponse();

                do
                {
                    response = await this.EBClient.DescribeEnvironmentsAsync(new DescribeEnvironmentsRequest
                    {
                        NextToken = response.NextToken
                    });

                    foreach(var environment in response.Environments)
                    {
                        if (environment.Status == EnvironmentStatus.Terminated)
                            continue;

                        this.Logger?.WriteLine((environment.EnvironmentName + " (" + environment.Status + "/" + environment.Health + ")").PadRight(45) + "  http://" + (environment.CNAME ?? environment.EndpointURL) + "/");
                    }

                } while (!string.IsNullOrEmpty(response.NextToken));
            }
            catch (Exception e)
            {
                throw new ElasticBeanstalkExceptions(string.Format("Error listing environments: {0}", e.Message), ElasticBeanstalkExceptions.EBCode.FailedToDeleteEnvironment);
            }

            return true;
        }

        protected override void SaveConfigFile(JsonData data)
        {
            
        }
    }
}
