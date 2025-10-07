using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.Common.DotNetCli.Tools.Commands;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using System.Threading.Tasks;
using Amazon.Runtime;

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

        protected override string ToolName => EBConstants.TOOLNAME;

        IAmazonElasticBeanstalk _ebClient;
        public IAmazonElasticBeanstalk EBClient
        {
            get
            {
                if (this._ebClient == null)
                {
                    var config = new AmazonElasticBeanstalkConfig();
                    config.RegionEndpoint = DetermineAWSRegion();

                    this._ebClient = new AmazonElasticBeanstalkClient(DetermineAWSCredentials(), config);
                    Utilities.SetUserAgentString((AmazonServiceClient)_ebClient, UserAgentString);
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
            if (allSolutionStacks == null)
                return new List<string>();
            foreach (var stack in allSolutionStacks.OrderByDescending(x => x))
            {
                if (EBUtilities.IsSolutionStackWindows(stack) || EBUtilities.IsSolutionStackLinuxNETCore(stack))
                    solutionStacks.Add(stack);
            }

            return FilterSolutionStackToLatestVersion(solutionStacks);
        }

        private static SolutionStackNameProperties ParseSolutionStackName(string solutionStackName)
        {
            Version version = null;
            var tokens = solutionStackName.Split(' ');
            var familyName = new StringBuilder();
            foreach (var token in tokens)
            {
                if (token.StartsWith("v") && char.IsNumber(token[1]))
                {
                    Version.TryParse(token.Substring(1), out version);
                }
                else
                {
                    familyName.Append(token + " ");
                }
            }

            if (version == null)
            {
                return new SolutionStackNameProperties { FamilyName = solutionStackName, FullName = solutionStackName };
            }

            return new SolutionStackNameProperties { FamilyName = familyName.ToString().TrimEnd(), FullName = solutionStackName, Version = version };
        }

        public static IList<string> FilterSolutionStackToLatestVersion(IList<string> allSolutionStacks)
        {


            var latestVersions = new Dictionary<string, SolutionStackNameProperties>();
            foreach(var solutionStackName in allSolutionStacks)
            {
                var properties = ParseSolutionStackName(solutionStackName);
                if(properties.Version == null)
                {
                    latestVersions[properties.FamilyName] = properties;
                }
                else if(latestVersions.TryGetValue(properties.FamilyName, out var current))
                {
                    if(current.Version < properties.Version)
                    {
                        latestVersions[properties.FamilyName] = properties;
                    }
                }
                else
                {
                    latestVersions[properties.FamilyName] = properties;
                }
            }

            var filterList = latestVersions.Values.Select(x => x.FullName).OrderBy(x => x).ToList();

            return filterList;
        }

        class SolutionStackNameProperties
        { 
            public string FamilyName { get; set; }
            public string FullName { get; set; }
            public Version Version { get; set; }
        }

    }
}
