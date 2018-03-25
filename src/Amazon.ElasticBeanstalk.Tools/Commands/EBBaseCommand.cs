using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using System.Threading.Tasks;

namespace Amazon.ElasticBeanstalk.Tools.Commands
{
    public abstract class EBBaseCommand : BaseCommand<ElasticBeanstalkToolsDefaults>
    {
        public EBBaseCommand(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        public EBBaseCommand(IToolLogger logger, string workingDirectory, IList<CommandOption> possibleOptions, string[] args)
            : base(logger, workingDirectory, possibleOptions, args)
        {
        }

        protected override string ToolName => "AWSElasticBeanstalkToolsDotnet";

        IAmazonElasticBeanstalk _ebClient;
        public IAmazonElasticBeanstalk EBClient
        {
            get
            {
                if (this._ebClient == null)
                {
                    SetUserAgentString();

                    var config = new AmazonElasticBeanstalkConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ebClient = new AmazonElasticBeanstalkClient(DetermineAWSCredentials(), config);
                }
                return this._ebClient;
            }
            set { this._ebClient = value; }
        }

        public string GetSolutionStackOrDefault(string propertyValue, CommandOption option, bool required)
        {
            var value = GetStringValueOrDefault(propertyValue, option, false);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
            else if (required && !this.DisableInteractive)
            {
                var solutionStacks = FindSolutionStacksAsync().Result;
                int chosenOption = PromptForValue(option, solutionStacks);

                var solutionStack = solutionStacks[chosenOption];
                if(!string.IsNullOrEmpty(solutionStack))
                {
                    _cachedRequestedValues[option] = solutionStack;    
                }
                return solutionStack;
            }

            if (required)
            {
                throw new ToolsException($"Missing required parameter: {option.Switch}", ToolsException.CommonErrorCode.MissingRequiredParameter);
            }

            return null;
        }

        public async Task<IList<string>> FindSolutionStacksAsync()
        {
            var solutionStacks = new List<string>();

            var allSolutionStacks = (await this.EBClient.ListAvailableSolutionStacksAsync()).SolutionStacks;
            foreach (var stack in allSolutionStacks.OrderByDescending(x => x))
            {
                if (stack.Contains("Windows"))
                    solutionStacks.Add(stack);
            }

            return solutionStacks;
        }
    }
}
