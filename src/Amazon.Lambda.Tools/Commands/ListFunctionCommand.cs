using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// List all the functions currently deployed to Lambda
    /// </summary>
    public class ListFunctionCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "list-functions";
        public const string COMMAND_DESCRIPTION = "Command to list all your Lambda functions";


        public static readonly IList<CommandOption> ListCommandOptions = BuildLineOptions(new List<CommandOption>
        {

        });

        public ListFunctionCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, ListCommandOptions, args)
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
            ListFunctionsRequest request = new ListFunctionsRequest();
            ListFunctionsResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.NextMarker;

                try
                {
                    response = await this.LambdaClient.ListFunctionsAsync(request);
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException("Error listing Lambda functions: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaListFunctions, e);
                }

                foreach (var function in response.Functions)
                {
                    this.Logger.WriteLine((function.FunctionName.PadRight(40) + " (" + function.Runtime + ")").PadRight(10) + "\t" + function.Description);
                }

            } while (!string.IsNullOrEmpty(response.NextMarker));

            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            
        }
    }
}