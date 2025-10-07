using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// List the all of the versions of a Lambda layer.
    /// </summary>
    public class ListLayerVersionsCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "list-layer-versions";
        public const string COMMAND_DESCRIPTION = "Command to list versions for a Layer";
        public const string COMMAND_ARGUMENTS = "<LAYER-NAME> The name of the layer";
     
        
        const int TIMESTAMP_WIDTH = 20;
        const int LAYER_ARN_WIDTH = 30;
        const int LAYER_COMPATIBLE_RUNTIMES_WIDTH = 30;
        const int LAYER_DESCRIPTION_WIDTH = 40;
        
        public static readonly IList<CommandOption> ListCommandOptions = BuildLineOptions(new List<CommandOption>
        {
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME
        });

        public ListLayerVersionsCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, ListCommandOptions, args)
        {
        }
        
        public string LayerName { get; set; }

        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.LayerName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME.Switch)) != null)
                this.LayerName = tuple.Item2.StringValue;
        }
        
        
        protected override async Task<bool> PerformActionAsync()
        {
            var layerName = this.GetStringValueOrDefault(this.LayerName, LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME, true);
            
            this.Logger.WriteLine("Description".PadRight(LAYER_DESCRIPTION_WIDTH) + " " +
                                  "Compatible Runtimes".PadRight(LAYER_COMPATIBLE_RUNTIMES_WIDTH) + " " +
                                  "Created".PadRight(TIMESTAMP_WIDTH) + " " +
                                   "Latest Version ARN".PadRight(LAYER_ARN_WIDTH)
            );
            this.Logger.WriteLine($"{new string('-', LAYER_DESCRIPTION_WIDTH)} {new string('-', LAYER_COMPATIBLE_RUNTIMES_WIDTH)} {new string('-', TIMESTAMP_WIDTH)} {new string('-', LAYER_ARN_WIDTH)}");

            var request = new ListLayerVersionsRequest { LayerName = layerName};
            ListLayerVersionsResponse response = null;
            do
            {
                request.Marker = response?.NextMarker;

                try
                {
                    response = await this.LambdaClient.ListLayerVersionsAsync(request);
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException("Error listing versions for Lambda layer: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaListLayerVersions, e);
                }

                foreach (var layerVersion in response.LayerVersions)
                {
                    this.Logger.WriteLine( LambdaUtilities.DetermineListDisplayLayerDescription(layerVersion.Description, LAYER_DESCRIPTION_WIDTH).PadRight(LAYER_DESCRIPTION_WIDTH) + " " +
                                                    string.Join(", ", layerVersion.CompatibleRuntimes.ToArray()).PadRight(LAYER_COMPATIBLE_RUNTIMES_WIDTH) + " " +
                                                    DateTime.Parse(layerVersion.CreatedDate).ToString("g").PadRight(TIMESTAMP_WIDTH) + " " +
                                                    layerVersion.LayerVersionArn
                                          );
                }

            } while (!string.IsNullOrEmpty(response.NextMarker));

            return true;
        }
        
        protected override void SaveConfigFile(Dictionary<string, object> data)
        {
            
        }        
    }
}