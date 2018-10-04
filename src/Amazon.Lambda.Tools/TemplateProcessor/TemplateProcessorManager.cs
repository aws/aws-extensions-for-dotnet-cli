using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace Amazon.Lambda.Tools.TemplateProcessor
{    
    public class TemplateProcessorManager
    {
        public class DefaultLocationOption
        {
            public string Configuration { get; set; }
            public string TargetFramework { get; set; }
            public string MSBuildParameters { get; set; }
            public bool DisableVersionCheck { get; set; }
            public string Package { get; set; }
        }
        
        public const string CF_TYPE_LAMBDA_FUNCTION = "AWS::Lambda::Function";
        public const string CF_TYPE_SERVERLESS_FUNCTION = "AWS::Serverless::Function";

        IToolLogger Logger { get; }
        IAmazonS3 S3Client { get; }
        string S3Bucket { get; }
        string S3Prefix { get; }
        DefaultLocationOption DefaultOptions { get; }

        public TemplateProcessorManager(IToolLogger logger, IAmazonS3 s3Client, string s3Bucket, string s3Prefix, DefaultLocationOption defaultOptions)
        {
            this.Logger = logger;
            this.S3Client = s3Client;
            this.S3Bucket = s3Bucket;
            this.S3Prefix = s3Prefix;
            this.DefaultOptions = defaultOptions;
        }

        public async Task<string> TransformTemplateAsync(string templateDirectory, string templateBody)
        {
            if (File.Exists(templateDirectory))
                templateDirectory = Path.GetDirectoryName(templateDirectory);
            
            var cacheOfLocalPathsToS3Keys = new Dictionary<string, string>();
            var parser = CreateTemplateParser(templateBody);

            foreach(var updatableResource in parser.UpdatableResources())
            {
                this.Logger?.WriteLine($"Processing CloudFormation resource {updatableResource.Name}");

                var localPath = updatableResource.GetLocalPath();
                string s3Key;
                if(!cacheOfLocalPathsToS3Keys.TryGetValue(localPath, out s3Key))
                {
                    this.Logger?.WriteLine($"Initiate packaging of {updatableResource.GetLocalPath()} for resource {updatableResource.Name}");
                    s3Key = await ProcessUpdatableResourceAsync(templateDirectory, updatableResource);
                    cacheOfLocalPathsToS3Keys[localPath] = s3Key;
                }
                else
                {
                    this.Logger?.WriteLine($"Using previous upload artifact s3://{this.S3Bucket}/{s3Key} for resource {updatableResource.Name}");
                }

                updatableResource.SetS3Location(this.S3Bucket, s3Key);                
            }

            var newTemplate = parser.GetUpdatedTemplate();
            return newTemplate;
        }


        private async Task<string> ProcessUpdatableResourceAsync(string templateDirectory, IUpdatableResource updatableResource)
        {
            var localPath = updatableResource.GetLocalPath();

            if (!Path.IsPathRooted(localPath))
                localPath = Path.Combine(templateDirectory, localPath);

            bool deleteArchiveAfterUploaded = false;
            string zipArchivePath = null;
            if(File.Exists(localPath) && string.Equals(Path.GetExtension(localPath), ".zip", StringComparison.OrdinalIgnoreCase))
            {                
                zipArchivePath = localPath;
            }
            else if (IsCurrentDirectory(updatableResource.GetLocalPath()) && !string.IsNullOrEmpty(this.DefaultOptions.Package))
            {
                zipArchivePath = this.DefaultOptions.Package;
            }
            else if(IsDotnetProjectDirectory(localPath))
            {
                zipArchivePath = await PackageDotnetProjectAsync(updatableResource, localPath);
                deleteArchiveAfterUploaded = true;
            }

            string s3Key;
            using (var stream = File.OpenRead(zipArchivePath))
            {
                s3Key = await Utilities.UploadToS3Async(this.Logger, this.S3Client, this.S3Bucket, this.S3Prefix, Path.GetFileName(zipArchivePath), stream);
            }

            if (deleteArchiveAfterUploaded)
            {
                try
                {
                    File.Delete(zipArchivePath);
                }
                catch (Exception e)
                {
                    this.Logger?.WriteLine($"Warning: Unable to delete temporary archive, {zipArchivePath}, after uploading to S3: {e.Message}");
                }
            }

            return s3Key;
        }

        private async Task<string> PackageDotnetProjectAsync(IUpdatableResource updatableResource, string location)
        {
            var command = new Commands.PackageCommand(this.Logger, location, null);
            var outputPackage = Path.Combine(Path.GetTempPath(), $"{updatableResource.Name}-{DateTime.Now.Ticks}.zip");
            command.OutputPackageFileName = outputPackage;
            command.TargetFramework =
                LambdaUtilities.DetermineTargetFrameworkFromLambdaRuntime(updatableResource.LambdaRuntime);

            if (IsCurrentDirectory(updatableResource.GetLocalPath()))
            {
                if (!string.IsNullOrEmpty(this.DefaultOptions.TargetFramework))
                    command.TargetFramework = this.DefaultOptions.TargetFramework;
                
                command.Configuration = this.DefaultOptions.Configuration;
                command.DisableVersionCheck = this.DefaultOptions.DisableVersionCheck;
                command.MSBuildParameters = this.DefaultOptions.MSBuildParameters;

            }
            
            if(!await command.ExecuteAsync())
            {
                var message = $"Error packaging up project in {location} for CloudFormation resource {updatableResource.Name}";
                if (command.LastToolsException != null)
                    message += $": {command.LastToolsException.Message}";

                throw new LambdaToolsException(message, ToolsException.CommonErrorCode.DotnetPublishFailed);
            }

            return outputPackage;
        }

        private bool IsDotnetProjectDirectory(string localPath)
        {
            if (!Directory.Exists(localPath))
                return false;

            var projectFiles = Directory.GetFiles(localPath, "*.??proj", SearchOption.TopDirectoryOnly)
                .Where(x => LambdaUtilities.ValidProjectExtensions.Contains(Path.GetExtension(x))).ToArray();
            

            return projectFiles.Length == 1;
        }

        private bool IsCurrentDirectory(string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
                return true;
            if (string.Equals(".", localPath))
                return true;
            if (string.Equals("./", localPath))
                return true;
            
            return false;
        }

        public static ITemplateParser CreateTemplateParser(string templateBody)
        {
            switch (LambdaUtilities.DetermineTemplateFormat(templateBody))
            {
                case TemplateFormat.Json:
                    return new JsonTemplateParser(templateBody);
                case TemplateFormat.Yaml:
                    return new YamlTemplateParser(templateBody);
                default:
                    throw new LambdaToolsException("Unable to determine template file format", LambdaToolsException.LambdaErrorCode.ServerlessTemplateParseError);
            }
        }
    }
}
