using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// List the Lambda layers
    /// </summary>
    public class ListLayersCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "list-layers";
        public const string COMMAND_DESCRIPTION = "Command to list Layers";

        const int TIMESTAMP_WIDTH = 20;
        const int LAYER_NAME_WIDTH = 30;
        const int LAYER_ARN_WIDTH = 30;
        const int LAYER_COMPATIBLE_RUNTIMES_WIDTH = 30;
        const int LAYER_DESCRIPTION_WIDTH = 40;
        
        public static readonly IList<CommandOption> ListCommandOptions = BuildLineOptions(new List<CommandOption>
        {

        });

        public ListLayersCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, ListCommandOptions, args)
        {
        }
        
        protected override async Task<bool> PerformActionAsync()
        {

            
            this.Logger.WriteLine("Name".PadRight(LAYER_NAME_WIDTH) + " " + 
                                  "Description".PadRight(LAYER_DESCRIPTION_WIDTH) + " " +
                                  "Compatible Runtimes".PadRight(LAYER_COMPATIBLE_RUNTIMES_WIDTH) + " " +
                                  "Created".PadRight(TIMESTAMP_WIDTH) + " " +
                                   "Latest Version ARN".PadRight(LAYER_ARN_WIDTH)
            );
            this.Logger.WriteLine($"{new string('-', LAYER_NAME_WIDTH)} {new string('-', LAYER_DESCRIPTION_WIDTH)} {new string('-', LAYER_COMPATIBLE_RUNTIMES_WIDTH)} {new string('-', TIMESTAMP_WIDTH)} {new string('-', LAYER_ARN_WIDTH)}");

            var request = new ListLayersRequest();
            ListLayersResponse response = null;
            do
            {
                if (response != null)
                    request.Marker = response.NextMarker;

                try
                {
                    response = await this.LambdaClient.ListLayersAsync(request);
                }
                catch (Exception e)
                {
                    throw new LambdaToolsException("Error listing Lambda layers: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaListLayers, e);
                }

                foreach (var layer in response.Layers)
                {
                    var latestVersion = layer.LatestMatchingVersion;
                    this.Logger.WriteLine(layer.LayerName.PadRight(LAYER_NAME_WIDTH) + " " + 
                                                    LambdaUtilities.DetermineListDisplayLayerDescription(latestVersion.Description, LAYER_DESCRIPTION_WIDTH).PadRight(LAYER_DESCRIPTION_WIDTH) + " " +
                                                    string.Join(", ", latestVersion.CompatibleRuntimes.ToArray()).PadRight(LAYER_COMPATIBLE_RUNTIMES_WIDTH) + " " +
                                                    DateTime.Parse(latestVersion.CreatedDate).ToString("g").PadRight(TIMESTAMP_WIDTH) + " " +
                                                    latestVersion.LayerVersionArn
                                          );
                }

            } while (!string.IsNullOrEmpty(response.NextMarker));

            return true;
        }
        
        protected override void SaveConfigFile(JsonData data)
        {
            
        }
    }
}