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
using Newtonsoft.Json.Schema;

namespace Amazon.Lambda.Tools.TemplateProcessor
{    
    /// <summary>
    /// This class is the entry point to traversing a CloudFormation template and looking for any resources that are pointing to local
    /// paths. Those local paths are uploaded to S3 and the template is updated with the location in S3. Once this is complete this
    /// template can be deployed to CloudFormation to create or update a stack.
    /// </summary>
    public class TemplateProcessorManager
    {        
        IToolLogger Logger { get; }
        IAmazonS3 S3Client { get; }
        
        /// <summary>
        /// The S3 bucket used to store all local paths in S3.
        /// </summary>
        string S3Bucket { get; }
        
        /// <summary>
        /// Prefix for any S3 objects uploaded to S3.
        /// </summary>
        string S3Prefix { get; }
        
        /// <summary>
        /// Options to use when a local path is pointing to the current directory. This is needed to maintain backwards compatibility
        /// with the original version of the deploy-serverless and package-ci commands.
        /// </summary>
        DefaultLocationOption DefaultOptions { get; }

        public TemplateProcessorManager(IToolLogger logger, IAmazonS3 s3Client, string s3Bucket, string s3Prefix, DefaultLocationOption defaultOptions)
        {
            this.Logger = logger;
            this.S3Client = s3Client;
            this.S3Bucket = s3Bucket;
            this.S3Prefix = s3Prefix;
            this.DefaultOptions = defaultOptions;
        }

        /// <summary>
        /// Transforms the provided template by uploading to S3 any local resources the template is pointing to,
        /// like .NET projects for a Lambda project, and then updating the CloudFormation resources to point to the
        /// S3 locations.
        /// </summary>
        /// <param name="templateDirectory">The directory where the template was found.</param>
        /// <param name="templateBody">The template to search for updatable resources. The file isn't just read from
        /// templateDirectory because template substitutions might have occurred before this was executed.</param>
        /// <returns></returns>
        public async Task<string> TransformTemplateAsync(string templateDirectory, string templateBody)
        {
            // If templateDirectory is actually pointing the CloudFormation template then grab its root.
            if (File.Exists(templateDirectory))
                templateDirectory = Path.GetDirectoryName(templateDirectory);
            
            // Maintain a cache of local paths to S3 Keys so if the same local path is referred to for
            // multiple Lambda functions it is only built and uploaded once.
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


        /// <summary>
        /// Determine the action to be done for the local path, like building a .NET Core package, then uploading the
        /// package to S3. The S3 key is returned to be updated in the template.
        /// </summary>
        /// <param name="templateDirectory"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        /// <exception cref="LambdaToolsException"></exception>
        private async Task<string> ProcessUpdatableResourceAsync(string templateDirectory, IUpdateResourceField field)
        {
            var localPath = field.GetLocalPath();

            if (!Path.IsPathRooted(localPath))
                localPath = Path.Combine(templateDirectory, localPath);

            bool deleteArchiveAfterUploaded = false;
            string zipArchivePath;
            if(File.Exists(localPath))
            {
                if(field.IsCode && !string.Equals(Path.GetExtension(localPath), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    this.Logger.WriteLine($"Creating zip archive for {localPath} file");
                    zipArchivePath = GenerateOutputZipFilename(field);
                    LambdaPackager.BundleFiles(zipArchivePath, Path.GetDirectoryName(localPath), new string[] { localPath }, this.Logger);
                }
                else
                {
                    zipArchivePath = localPath;
                }
            }
            // If IsCode is false then the local path needs to point to a file and not a directory. When IsCode is true
            // it can point either to a file or a directory.
            else if (!field.IsCode && !File.Exists(localPath))
            {
                throw new LambdaToolsException($"File that the field {field.Resource.Name}/{field.Name} is pointing to doesn't exist", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingLocalPath);                
            }
            else if (!Directory.Exists(localPath))
            {
                throw new LambdaToolsException($"Directory that the field {field.Resource.Name}/{field.Name} is pointing doesn't exist", LambdaToolsException.LambdaErrorCode.ServerlessTemplateMissingLocalPath);
            }
            // To maintain compatibility if the field is point to current directory or not set at all but a prepackaged zip archive is given
            // then use it as the package source.
            else if (IsCurrentDirectory(field.GetLocalPath()) && !string.IsNullOrEmpty(this.DefaultOptions.Package))
            {
                zipArchivePath = this.DefaultOptions.Package;
            }
            else if(field.IsCode)
            {
                if (IsDotnetProjectDirectory(localPath))
                {
                    zipArchivePath = await PackageDotnetProjectAsync(field, localPath);
                }
                else
                {
                    zipArchivePath = GenerateOutputZipFilename(field);
                    LambdaPackager.BundleDirectory(zipArchivePath, localPath, false, this.Logger);                    
                }
                deleteArchiveAfterUploaded = true;                    
            }
            else
            {
                throw new LambdaToolsException($"Unable to determine package action for the field {field.Resource.Name}/{field.Name}", LambdaToolsException.LambdaErrorCode.ServerlessTemplateUnknownActionForLocalPath);
            }

            string s3Key;
            using (var stream = File.OpenRead(zipArchivePath))
            {
                s3Key = await Utilities.UploadToS3Async(this.Logger, this.S3Client, this.S3Bucket, this.S3Prefix, Path.GetFileName(zipArchivePath), stream);
            }

            // Now that the temp zip file is uploaded to S3 clean up by deleting the temp file.
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

        /// <summary>
        /// Executes the package command to create the deployment bundle for the .NET project and returns the path.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        /// <exception cref="LambdaToolsException"></exception>
        private async Task<string> PackageDotnetProjectAsync(IUpdateResourceField field, string location)
        {
            var command = new Commands.PackageCommand(this.Logger, location, null);
            var outputPackage = GenerateOutputZipFilename(field);
            command.OutputPackageFileName = outputPackage;
            command.TargetFramework =
                LambdaUtilities.DetermineTargetFrameworkFromLambdaRuntime(field.Resource.LambdaRuntime);

            // If the project is in the same directory as the CloudFormation template then use any parameters
            // there were specified on the command to build the project.
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

        private static string GenerateOutputZipFilename(IUpdateResourceField field)
        {
            var outputPackage = Path.Combine(Path.GetTempPath(), $"{field.Resource.Name}-{field.Name}-{DateTime.Now.Ticks}.zip");
            return outputPackage;
        }

        /// <summary>
        /// Check to see if the directory contains a .NET project file.
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
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
            if (string.Equals(@".\", localPath))
                return true;
            
            return false;
        }

        /// <summary>
        /// Create the appropriate parser depending whether the template is JSON or YAML.
        /// </summary>
        /// <param name="templateBody"></param>
        /// <returns></returns>
        /// <exception cref="LambdaToolsException"></exception>
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
