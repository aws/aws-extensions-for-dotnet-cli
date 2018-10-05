using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Amazon.Common.DotNetCli.Tools;
using Amazon.S3;
using Amazon.S3.Transfer;
using Newtonsoft.Json.Schema;

namespace Amazon.Lambda.Tools.TemplateProcessor
{    
    public class TemplateProcessorManager
    {        
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

                foreach (var field in updatableResource.Fields)
                {
                    var localPath = field.GetLocalPath();
                    if (localPath == null)
                        continue;
                    
                    string s3Key;
                    if (!cacheOfLocalPathsToS3Keys.TryGetValue(localPath, out s3Key))
                    {
                        this.Logger?.WriteLine(
                            $"Initiate packaging of {field.GetLocalPath()} for resource {updatableResource.Name}");
                        s3Key = await ProcessUpdatableResourceAsync(templateDirectory, field);
                        cacheOfLocalPathsToS3Keys[localPath] = s3Key;
                    }
                    else
                    {
                        this.Logger?.WriteLine(
                            $"Using previous upload artifact s3://{this.S3Bucket}/{s3Key} for resource {updatableResource.Name}");
                    }

                    field.SetS3Location(this.S3Bucket, s3Key);
                }
            }

            var newTemplate = parser.GetUpdatedTemplate();
            return newTemplate;
        }


        private async Task<string> ProcessUpdatableResourceAsync(string templateDirectory, IUpdateResourceField field)
        {
            var localPath = field.GetLocalPath();

            if (!Path.IsPathRooted(localPath))
                localPath = Path.Combine(templateDirectory, localPath);

            bool deleteArchiveAfterUploaded = false;
            string zipArchivePath = null;
            if(File.Exists(localPath))
            {                
                zipArchivePath = localPath;
            }
            else if (IsCurrentDirectory(field.GetLocalPath()) && !string.IsNullOrEmpty(this.DefaultOptions.Package))
            {
                zipArchivePath = this.DefaultOptions.Package;
            }
            else if(IsDotnetProjectDirectory(localPath))
            {
                zipArchivePath = await PackageDotnetProjectAsync(field, localPath);
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

        private async Task<string> PackageDotnetProjectAsync(IUpdateResourceField field, string location)
        {
            var command = new Commands.PackageCommand(this.Logger, location, null);
            var outputPackage = Path.Combine(Path.GetTempPath(), $"{field.Resource.Name}-{DateTime.Now.Ticks}.zip");
            command.OutputPackageFileName = outputPackage;
            command.TargetFramework =
                LambdaUtilities.DetermineTargetFrameworkFromLambdaRuntime(field.Resource.LambdaRuntime);

            if (IsCurrentDirectory(field.GetLocalPath()))
            {
                if (!string.IsNullOrEmpty(this.DefaultOptions.TargetFramework))
                    command.TargetFramework = this.DefaultOptions.TargetFramework;
                
                command.Configuration = this.DefaultOptions.Configuration;
                command.DisableVersionCheck = this.DefaultOptions.DisableVersionCheck;
                command.MSBuildParameters = this.DefaultOptions.MSBuildParameters;

            }
            
            if(!await command.ExecuteAsync())
            {
                var message = $"Error packaging up project in {location} for CloudFormation resource {field.Resource.Name}";
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
