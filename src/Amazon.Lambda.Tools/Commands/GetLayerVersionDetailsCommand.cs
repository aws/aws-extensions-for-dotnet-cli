using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using Amazon.Lambda.Model;

namespace Amazon.Lambda.Tools.Commands
{
    /// <summary>
    /// Get all the details for a Lambda layer including the .NET Runtime package store manifest
    /// </summary>
    public class GetLayerVersionDetailsCommand : LambdaBaseCommand
    {
        const int PAD_SIZE = 25;
        
        public const string COMMAND_NAME = "get-layer-version";
        public const string COMMAND_DESCRIPTION = "Command to get the details of a Layer version";
        public const string COMMAND_ARGUMENTS = "<LAYER-VERSION-ARN> The layer version arn to get details for";
        
        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {            
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_VERSION_ARN
        });

        public string LayerVersionArn { get; set; }
        
        public GetLayerVersionDetailsCommand(IToolLogger logger, string workingDirectory, string[] args)
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

            var result = LambdaUtilities.ParseLayerVersionArn(layerVersionArn);

            var getRequest = new GetLayerVersionRequest
            {
                LayerName = result.Name,
                VersionNumber = result.VersionNumber
            };


            try
            {
                var response = await this.LambdaClient.GetLayerVersionAsync(getRequest);

                
                this.Logger.WriteLine("Layer ARN:".PadRight(PAD_SIZE) + response.LayerArn);
                this.Logger.WriteLine("Version Number:".PadRight(PAD_SIZE) + response.Version);
                this.Logger.WriteLine("Created:".PadRight(PAD_SIZE) + DateTime.Parse(response.CreatedDate).ToString("g"));
                this.Logger.WriteLine("License Info:".PadRight(PAD_SIZE) + response.LicenseInfo);
                this.Logger.WriteLine("Compatible Runtimes:".PadRight(PAD_SIZE) + string.Join(", ", response.CompatibleRuntimes.ToArray()));

                LayerDescriptionManifest manifest;
                if (!LambdaUtilities.AttemptToParseLayerDescriptionManifest(response.Description, out manifest))
                {
                    this.Logger.WriteLine("Description:".PadRight(PAD_SIZE) + response.Description);
                }
                else 
                {
                    switch (manifest.Nlt)
                    {
                        case LayerDescriptionManifest.ManifestType.RuntimePackageStore: 
                            this.Logger.WriteLine("Layer Type:".PadRight(PAD_SIZE) + LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE_DISPLAY_NAME);
                            await GetRuntimePackageManifest(manifest);
                            break;
                        default:
                            this.Logger.WriteLine("Layer Type:".PadRight(PAD_SIZE) + manifest.Nlt);
                            break;
                    }
                }
            }
            catch(Exception e)
            {
                throw new LambdaToolsException("Error getting layer version details: " + e.Message, LambdaToolsException.LambdaErrorCode.LambdaGetLayerVersionDetails, e);
            }

            return true;
        }

        private async Task GetRuntimePackageManifest(LayerDescriptionManifest manifest)
        {
            try
            {
                this.Logger.WriteLine("");
                this.Logger.WriteLine($"{LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE_DISPLAY_NAME} Details:");
                this.Logger.WriteLine("Manifest Location:".PadRight(PAD_SIZE) + $"s3://{manifest.Buc}/{manifest.Key}");
                this.Logger.WriteLine("Packages Optimized:".PadRight(PAD_SIZE) + (manifest.Op == LayerDescriptionManifest.OptimizedState.Optimized));
                this.Logger.WriteLine("Packages Directory:".PadRight(PAD_SIZE) + "/opt/" + manifest.Dir);
                using (var response = await this.S3Client.GetObjectAsync(manifest.Buc, manifest.Key))
                using(var reader = new StreamReader(response.ResponseStream))
                {
                    this.Logger.WriteLine("");
                    this.Logger.WriteLine("Manifest Contents");
                    this.Logger.WriteLine("-----------------------");
                    this.Logger.WriteLine(reader.ReadToEnd());
                }
            }
            catch (Exception)
            {
            }
        }
        
        protected override void SaveConfigFile(Dictionary<string, object> data)
        {
        }           
    }
}