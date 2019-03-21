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
    /// Command to delete a Lambda layer version
    /// </summary>
    public class DeleteLayerVersionCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "delete-layer-version";
        public const string COMMAND_DESCRIPTION = "Command to delete a version of a Layer";
        public const string COMMAND_ARGUMENTS = "<LAYER-VERSION-ARN> The arn of the Layer version to delete";



        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {            
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_VERSION_ARN
        });

        public string LayerVersionArn { get; set; }
        
        public DeleteLayerVersionCommand(IToolLogger logger, string workingDirectory, string[] args)
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

            if (values.Arguments.Count > 0)
            {
                this.LayerVersionArn = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_LAYER_VERSION_ARN.Switch)) != null)
                this.LayerVersionArn = tuple.Item2.StringValue;         
        }

        protected override async Task<bool> PerformActionAsync()
        {

            var layerVersionArn = this.GetStringValueOrDefault(this.LayerVersionArn,
                LambdaDefinedCommandOptions.ARGUMENT_LAYER_VERSION_ARN, true);

            var (layerName, versionNumber) = LambdaUtilities.ParseLayerVersionArn(layerVersionArn);

            var deleteRequest = new DeleteLayerVersionRequest
            {
                LayerName = layerName,
                VersionNumber = versionNumber
            };


            try
            {
                await this.LambdaClient.DeleteLayerVersionAsync(deleteRequest);
            }
            catch(Exception e)
            {
                throw new LambdaToolsException("Error deleting Lambda layer version: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaDeleteLayerVersion, e);
            }

            this.Logger?.WriteLine($"Deleted version {versionNumber} for layer {layerName}");

            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
        }        
    }
}