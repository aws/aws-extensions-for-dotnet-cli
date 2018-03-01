using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.CloudFormation.Model;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;

using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    public class DeleteServerlessCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "delete-serverless";
        public const string COMMAND_DESCRIPTION = "Command to delete an AWS Serverless application";
        public const string COMMAND_ARGUMENTS = "<STACK-NAME> The CloudFormation stack for the AWS Serverless application";



        public static readonly IList<CommandOption> DeleteCommandOptions = BuildLineOptions(new List<CommandOption>
        {            
            LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME
        });

        public string StackName { get; set; }
        

        public DeleteServerlessCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, DeleteCommandOptions, args)
        {
        }


        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.StackName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME.Switch)) != null)
                this.StackName = tuple.Item2.StringValue;         
        }

        protected override async Task<bool> PerformActionAsync()
        {
            var deleteRequest = new DeleteStackRequest
            {
                StackName = this.GetStringValueOrDefault(this.StackName, LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME, true)
            };


            try
            {
                await this.CloudFormationClient.DeleteStackAsync(deleteRequest);
            }
            catch (Exception e)
            {
                throw new LambdaToolsException("Error deleting CloudFormation stack: " + e.Message, LambdaToolsException.LambdaErrorCode.CloudFormationDeleteStack, e);
            }

            this.Logger.WriteLine($"CloudFormation stack {deleteRequest.StackName} deleted");


            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.StackName, LambdaDefinedCommandOptions.ARGUMENT_STACK_NAME, false));    
        }
    }
}
