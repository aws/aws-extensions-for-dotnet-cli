using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.Common.DotNetCli.Tools.Options;
using ThirdParty.Json.LitJson;

using Amazon.Lambda.Model;
using System.Runtime.InteropServices;

namespace Amazon.Lambda.Tools.Commands
{
    public class PublishLayerCommand : LambdaBaseCommand
    {
        public const string COMMAND_NAME = "publish-layer";
        public const string COMMAND_DESCRIPTION = "Command to publish a Layer that can be associated with a Lambda function";
        public const string COMMAND_ARGUMENTS = "<LAYER-NAME> The name of the layer";

        public static readonly IList<CommandOption> PublishLayerCommandOptions = BuildLineOptions(new List <CommandOption>
        {
            CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK,
            LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE,
            LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET,
            LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX,
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME,
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_TYPE,
            LambdaDefinedCommandOptions.ARGUMENT_LAYER_LICENSE_INFO,
            LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_MANIFEST,
            LambdaDefinedCommandOptions.ARGUMENT_OPT_DIRECTORY,
            LambdaDefinedCommandOptions.ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION
        });

        public string Architecture { get; set; }
        public string TargetFramework { get; set; }
        public string S3Bucket { get; set; }
        public string S3Prefix { get; set; }
        public string LayerName { get; set; }
        public string LayerType { get; set; }
        public string LayerLicenseInfo { get; set; }
        public string PackageManifest { get; set; }
        public string OptDirectory { get; set; }
        public bool? EnablePackageOptimization { get; set; }


        public string NewLayerArn { get; set; }
        public long NewLayerVersionNumber { get; set; }
        public string NewLayerVersionArn { get; set; }


        public PublishLayerCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, PublishLayerCommandOptions, args)
        {
        }

        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            if (values.Arguments.Count > 0)
            {
                this.LayerName = values.Arguments[0];
            }

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.Switch)) != null)
                this.S3Bucket = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.Switch)) != null)
                this.S3Prefix = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME.Switch)) != null)
                this.LayerName = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_LAYER_TYPE.Switch)) != null)
                this.LayerType = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_LAYER_LICENSE_INFO.Switch)) != null)
                this.LayerLicenseInfo = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_MANIFEST.Switch)) != null)
                this.PackageManifest = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_OPT_DIRECTORY.Switch)) != null)
                this.OptDirectory = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION.Switch)) != null)
                this.EnablePackageOptimization = tuple.Item2.BoolValue;
            if ((tuple = values.FindCommandOption(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.Switch)) != null)
                this.Architecture = tuple.Item2.StringValue;
        }

        protected override async Task<bool> PerformActionAsync()
        {
            var layerName = this.GetStringValueOrDefault(this.LayerName, LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME, true);

            var s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, true);
            var s3Prefix = this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false);

            // This command will store 2 file in S3. The object structure will be
            // <provided-prefix>/
            //      <layer-name>-<unique-ticks>/
            //          packages.zip    -- Zip file containing the NuGet packages
            //          artifact.xml    -- Xml file describe the NuGet packages in the layer
            if (string.IsNullOrEmpty(s3Prefix))
            {
                s3Prefix = string.Empty;
            }
            else if(!s3Prefix.EndsWith("/"))
            {
                s3Prefix += "/";
            }
            s3Prefix += $"{layerName}-{DateTime.UtcNow.Ticks}/";

            var layerType = this.GetStringValueOrDefault(this.LayerType, LambdaDefinedCommandOptions.ARGUMENT_LAYER_TYPE, true);
            CreateLayerZipFileResult createResult;
            switch (layerType)
            {
                case LambdaConstants.LAYER_TYPE_RUNTIME_PACKAGE_STORE:
                    createResult = await CreateRuntimePackageStoreLayerZipFile(layerName, s3Prefix);
                    break;

                default:
                    throw new LambdaToolsException($"Unknown layer type {layerType}. Allowed values are: {LambdaConstants.LAYER_TYPE_ALLOWED_VALUES}", LambdaToolsException.LambdaErrorCode.UnknownLayerType);
            }

            this.Logger.WriteLine($"Uploading layer input zip file to S3");
            var s3ZipKey = await UploadFile(createResult.ZipFile, $"{s3Prefix}packages.zip");

            var request = new PublishLayerVersionRequest
            {
                LayerName = layerName,
                Description = createResult.Description,
                Content = new LayerVersionContentInput
                {
                    S3Bucket = s3Bucket,
                    S3Key = s3ZipKey
                },
                CompatibleRuntimes = createResult.CompatibleRuntimes,
                LicenseInfo = this.GetStringValueOrDefault(this.LayerLicenseInfo, LambdaDefinedCommandOptions.ARGUMENT_LAYER_LICENSE_INFO, false)
            };

            var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);
            if (!string.IsNullOrEmpty(architecture))
            {
                request.CompatibleArchitectures = new List<string> { architecture };
            }

            try
            {
                var publishResponse = await this.LambdaClient.PublishLayerVersionAsync(request);
                this.NewLayerArn = publishResponse.LayerArn;
                this.NewLayerVersionArn = publishResponse.LayerVersionArn;
                this.NewLayerVersionNumber = publishResponse.Version;
            }
            catch(Exception e)
            {
                throw new LambdaToolsException($"Error calling the Lambda service to publish the layer: {e.Message}", LambdaToolsException.LambdaErrorCode.LambdaPublishLayerVersion, e);
            }

            this.Logger?.WriteLine($"Layer publish with arn {this.NewLayerVersionArn}");

            return true;
        }

        private async Task<CreateLayerZipFileResult> CreateRuntimePackageStoreLayerZipFile(string layerName, string s3Prefix)
        {
            var targetFramework = this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, true);
            var projectLocation = this.GetStringValueOrDefault(this.ProjectLocation, CommonDefinedCommandOptions.ARGUMENT_PROJECT_LOCATION, false);
            var enableOptimization = this.GetBoolValueOrDefault(this.EnablePackageOptimization, LambdaDefinedCommandOptions.ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION, false).GetValueOrDefault();

            if(string.Equals(targetFramework, "netcoreapp3.1"))
            {
                var version = DotNetCLIWrapper.GetSdkVersion();
                
                // .NET SDK 3.1 versions less then 3.1.400 have an issue throwing NullReferenceExceptions when pruning packages out with the manifest.
                // https://github.com/dotnet/sdk/issues/10973
                if (version < Version.Parse("3.1.400"))
                {
                    var message = $"Publishing runtime package store layers targeting .NET Core 3.1 requires at least version 3.1.400 of the .NET SDK. Current version installed is {version}.";
                    throw new LambdaToolsException(message, LambdaToolsException.LambdaErrorCode.DisabledSupportForNET31Layers);
                }
            }

#if NETCORE
            if (enableOptimization)
            {
                if(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    throw new LambdaToolsException($"Package optimization is only possible on Amazon Linux. To use this feature execute the command in an Amazon Linux environment.", LambdaToolsException.LambdaErrorCode.UnsupportedOptimizationPlatform);
                }
                else
                {
                    this.Logger.WriteLine("Warning: Package optimization has been enabled. Be sure to run this on an Amazon Linux environment or the optimization might not be compatbile with the Lambda runtime.");
                }
            }
#else
            // This is the case the code is run in the AWS Toolkit for Visual Studio which will never run on Amazon Linux.
            enableOptimization = false;
#endif

            // This is the manifest that list the NuGet packages via <PackageReference> elements in the msbuild project file.
            var packageManifest = this.GetStringValueOrDefault(this.PackageManifest, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_MANIFEST, false);
            // If this is null attempt to use the current directory. This is likely if the intent is to make a
            // layer from the current Lambda project.
            if (string.IsNullOrEmpty(packageManifest))
            {
                packageManifest = Utilities.DetermineProjectLocation(this.WorkingDirectory, projectLocation);
            }

            // If this is a directory look to see if there is a single csproj of fsproj in the directory in use that.
            // This is to make it easy to make a layer in the current directory of a Lambda function.
            if (Directory.Exists(packageManifest))
            {
                var files = Directory.GetFiles(packageManifest, "*.csproj");
                if(files.Length == 1)
                {
                    packageManifest = Path.Combine(packageManifest, files[0]);
                }
                else if(files.Length == 0)
                {
                    files = Directory.GetFiles(packageManifest, "*.fsproj");
                    if (files.Length == 1)
                    {
                        packageManifest = Path.Combine(packageManifest, files[0]);
                    }
                }
            }

            if(!File.Exists(packageManifest))
            {
                throw new LambdaToolsException($"Can not find package manifest {packageManifest}. Make sure to point to a file not a directory.", LambdaToolsException.LambdaErrorCode.LayerPackageManifestNotFound);
            }

            // Create second subdirectory so that when the directory is zipped the sub directory is retained in the zip file.
            // The sub directory will be created in the /opt directory in the Lambda environment.
            var tempDirectoryName = $"{layerName}-{DateTime.UtcNow.Ticks}".ToLower();
            var optDirectory = this.GetStringValueOrDefault(this.OptDirectory, LambdaDefinedCommandOptions.ARGUMENT_OPT_DIRECTORY, false);
            if (string.IsNullOrEmpty(optDirectory))
            {
                optDirectory = LambdaConstants.DEFAULT_LAYER_OPT_DIRECTORY;
            }
            var tempRootPath = Path.Combine(Path.GetTempPath(), tempDirectoryName);
            var storeOutputDirectory = Path.Combine(tempRootPath, optDirectory);

            {
                var convertResult = LambdaUtilities.ConvertManifestToSdkManifest(targetFramework, packageManifest);
                if (convertResult.ShouldDelete)
                {
                    this.Logger?.WriteLine("Converted ASP.NET Core project file to temporary package manifest file.");
                }
                var architecture = this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false);
                var cliWrapper = new LambdaDotNetCLIWrapper(this.Logger, this.WorkingDirectory);
                if(cliWrapper.Store(defaults: this.DefaultConfig, 
                                        projectLocation: projectLocation,
                                        outputLocation: storeOutputDirectory, 
                                        targetFramework: targetFramework,
                                        packageManifest: convertResult.PackageManifest,
                                        architecture: architecture,
                                        enableOptimization: enableOptimization) != 0)
                {
                    throw new LambdaToolsException($"Error executing the 'dotnet store' command", LambdaToolsException.LambdaErrorCode.StoreCommandError);
                }

                if (convertResult.ShouldDelete)
                {
                    File.Delete(convertResult.PackageManifest);
                }
            }

            // The artifact.xml file is generated by the "dotnet store" command that lists the packages that were added to the store.
            // It is required during a "dotnet publish" so the NuGet packages in the store will be filtered out.
            var artifactXmlPath = Path.Combine(storeOutputDirectory, "x64", targetFramework, "artifact.xml");
            if(!File.Exists(artifactXmlPath))
            {
                throw new LambdaToolsException($"Failed to find artifact.xml file in created local store.", LambdaToolsException.LambdaErrorCode.FailedToFindArtifactZip);
            }
            this.Logger.WriteLine($"Uploading runtime package store manifest to S3");
            var s3Key = await UploadFile(artifactXmlPath, $"{s3Prefix}artifact.xml");

            this.Logger.WriteLine($"Create zip file of runtime package store directory");
            var zipPath = Path.Combine(Path.GetTempPath(), $"{layerName}-{DateTime.UtcNow.Ticks}.zip");
            if(File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            LambdaPackager.BundleDirectory(zipPath, tempRootPath, false, this.Logger);

            var result = new CreateLayerZipFileResult
            {
                ZipFile = zipPath,
                LayerDirectory = optDirectory
            };

            var s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, true);

            // Set the description field to the our JSON layer manifest file so when the tooling is used
            // to create a package of a Lambda function in the future the artifact.xml file can be used during "dotnet publish".
            result.Description = GeneratorRuntimePackageManifestLayerDescription(optDirectory, s3Bucket, s3Key, enableOptimization);

            var compatibleRuntime = LambdaUtilities.DetermineLambdaRuntimeFromTargetFramework(targetFramework);
            if(!string.IsNullOrEmpty(compatibleRuntime))
            {
                result.CompatibleRuntimes = new List<string>() { compatibleRuntime };
            }

            return result;
        }

        public static string GeneratorRuntimePackageManifestLayerDescription(string directory, string s3Bucket, string s3Key, bool enableOptimization)
        {
            var manifestDescription = new LayerDescriptionManifest(LayerDescriptionManifest.ManifestType.RuntimePackageStore);
            
            manifestDescription.Dir = directory;
            manifestDescription.Buc = s3Bucket;
            manifestDescription.Key = s3Key;
            manifestDescription.Op = enableOptimization ? LayerDescriptionManifest.OptimizedState.Optimized : LayerDescriptionManifest.OptimizedState.NoOptimized;

            var json = JsonMapper.ToJson(manifestDescription);
            return json;
        }

        private async Task<string> UploadFile(string filePath, string s3Key)
        {
            var s3Bucket = this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, true);

            string s3ZipKey;
            using (var stream = File.OpenRead(filePath))
            {
                s3ZipKey = await Utilities.UploadToS3Async(this.Logger, this.S3Client, s3Bucket, s3Key, stream);
                this.Logger?.WriteLine($"Upload complete to s3://{s3Bucket}/{s3ZipKey}");
            }
            return s3ZipKey;
        }
        

        protected override void SaveConfigFile(JsonData data)
        {
            data.SetIfNotNull(CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK.ConfigFileKey, this.GetStringValueOrDefault(this.TargetFramework, CommonDefinedCommandOptions.ARGUMENT_FRAMEWORK, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE.ConfigFileKey, this.GetStringValueOrDefault(this.Architecture, LambdaDefinedCommandOptions.ARGUMENT_FUNCTION_ARCHITECTURE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET.ConfigFileKey, this.GetStringValueOrDefault(this.S3Bucket, LambdaDefinedCommandOptions.ARGUMENT_S3_BUCKET, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX.ConfigFileKey, this.GetStringValueOrDefault(this.S3Prefix, LambdaDefinedCommandOptions.ARGUMENT_S3_PREFIX, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME.ConfigFileKey, this.GetStringValueOrDefault(this.LayerName, LambdaDefinedCommandOptions.ARGUMENT_LAYER_NAME, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_LAYER_TYPE.ConfigFileKey, this.GetStringValueOrDefault(this.LayerType, LambdaDefinedCommandOptions.ARGUMENT_LAYER_TYPE, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_LAYER_LICENSE_INFO.ConfigFileKey, this.GetStringValueOrDefault(this.LayerLicenseInfo, LambdaDefinedCommandOptions.ARGUMENT_LAYER_LICENSE_INFO, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_MANIFEST.ConfigFileKey, this.GetStringValueOrDefault(this.PackageManifest, LambdaDefinedCommandOptions.ARGUMENT_PACKAGE_MANIFEST, false));
            data.SetIfNotNull(LambdaDefinedCommandOptions.ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION.ConfigFileKey, this.GetBoolValueOrDefault(this.EnablePackageOptimization, LambdaDefinedCommandOptions.ARGUMENT_ENABLE_PACKAGE_OPTIMIZATION, false));
        }

        class CreateLayerZipFileResult
        {
            public string ZipFile { get; set; }
            public string Description { get; set; }
            public List<string> CompatibleRuntimes { get; set; }
            public string LayerDirectory { get; set; }
        }

    }
}
