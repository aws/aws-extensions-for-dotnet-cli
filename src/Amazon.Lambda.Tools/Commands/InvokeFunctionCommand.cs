using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;

using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Invoke a function running in Lambda
    /// </summary>
    public class InvokeFunctionCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "invoke-function";
        public const string COMMAND_DESCRIPTION = "Command to invoke a function in Lambda with an optional input";
        public const string COMMAND_ARGUMENTS = "<FUNCTION-NAME> The name of the function to invoke";


        public static readonly IList<CommandOption> InvokeCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME,
            LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD
        });

        public string FunctionName { get; set; }

        /// <summary>
        /// If the value for Payload points to an existing file then the contents of the file is sent, otherwise
        /// the value of Payload is sent as the input to the function.
        /// </summary>
        public string Payload { get; set; }

        public InvokeFunctionCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, InvokeCommandOptions, args)
        {
        }


        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if(values.Arguments.Count > 0)
            {
                this.FunctionName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.Switch)) != null)
                this.FunctionName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD.Switch)) != null)
                this.Payload = tuple.Item2.StringValue;
        }

        protected override async Task<bool> PerformActionAsync()
        {

            var invokeRequest = new InvokeRequest
            {
                FunctionName = this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, true),
                LogType = LogType.Tail
            };

            if (!string.IsNullOrWhiteSpace(this.Payload))
            {
                if (File.Exists(this.Payload))
                {
                    Logger.WriteLine($"Reading {Path.GetFullPath(this.Payload)} as input to Lambda function");
                    invokeRequest.Payload = File.ReadAllText(this.Payload);
                }
                else
                {
                    invokeRequest.Payload = this.Payload.Trim();
                }

                if(!invokeRequest.Payload.StartsWith("{"))
                {
                    invokeRequest.Payload = "\"" + invokeRequest.Payload + "\"";
                }
            }

            InvokeResponse response;
            try
            {
                response = await this.LambdaClient.InvokeAsync(invokeRequest);
            }
            catch(Exception e)
            {
                throw new LambdaToolsException("Error invoking Lambda function: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaInvokeFunction, e);
            }

            this.Logger.WriteLine("Payload:");

            PrintPayload(response);

            this.Logger.WriteLine("");
            this.Logger.WriteLine("Log Tail:");
            var log = System.Text.UTF8Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
            this.Logger.WriteLine(log);

            return true;
        }


        private void PrintPayload(InvokeResponse response)
        {
            try
            {
                var payload = new StreamReader(response.Payload).ReadToEnd();
                this.Logger.WriteLine(payload);
            }
            catch (Exception)
            {
                this.Logger.WriteLine("<unparseable data>");
            }
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.FunctionName, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_NAME, false));    
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD.ConfigFileKey, this.GetStringValueOrDefault(this.Payload, LambdaDefinedCommandOptions.ARGUMENT_PAYLOAD, false));              
        }
    }
}
