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
    /// Command to delete a function
    /// </summary>
    public class DeleteFunctionCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "delete-function";
        public const string COMMAND_DESCRIPTION = "Command to delete an AWS Lambda function";
        public const string COMMAND_ARGUMENTS = "<FUNCTION-NAME> The name of the function to delete";



        public static readonly IList<CommandOption> DeleteCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE,
            
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME
        });

        public bool? PersistConfigFile { get; set; }

        public string FunctionName { get; set; }


        public DeleteFunctionCommand(IToolLogger logger, string workingDirectory, string[] args)
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
                this.FunctionName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.Switch)) != null)
                this.FunctionName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE.Switch)) != null)
                this.PersistConfigFile = tuple.Item2.BoolValue;            
        }

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                var deleteRequest = new DeleteFunctionRequest
                {
                    FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true)
                };


                try
                {
                    await this.LambdaClient.DeleteFunctionAsync(deleteRequest);
                }
                catch(Exception e)
                {
                    throw new LambdaToolsException("Error deleting Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaDeleteFunction, e);
                }

                this.Logger.WriteLine($"Lambda function {deleteRequest.FunctionName} deleted");
                
                if (this.GetBoolValueOrDefault(this.PersistConfigFile, CommonDefinedCommandOptions.ARGUMENT_PERSIST_CONFIG_FILE, false).GetValueOrDefault())
                {
                    this.SaveConfigFile();
                }
            }
            catch (LambdaToolsException e)
            {
                this.Logger.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger.WriteLine($"Unknown error deleting Lambda function: {e.Message}");
                this.Logger.WriteLine(e.StackTrace);
                return false;
            }

            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, false));    
        }
    }
}
