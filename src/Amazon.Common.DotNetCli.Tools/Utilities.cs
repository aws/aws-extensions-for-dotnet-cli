﻿using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.Util;
using System.Text.RegularExpressions;
using System.Collections;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using static Microsoft.Build.Evaluation.ProjectLoadSettings;
using static Microsoft.Build.Evaluation.Context.EvaluationContext.SharingPolicy;

namespace Amazon.Common.DotNetCli.Tools
{
    public static class Utilities
    {
        /// <summary>
        /// Compiled Regex for $(Variable) token searches
        /// </summary>
        private readonly static Regex EnvironmentVariableTokens = new Regex(@"[$][(].*?[)]", RegexOptions.Compiled);

        /// <summary>
        /// Replaces $(Variable) tokens with environment variables
        /// </summary>
        /// <param name="original">original string</param>
        /// <returns>string with environment variable replacements</returns>
        public static string ReplaceEnvironmentVariables(string original)
        {
            MatchCollection matches = EnvironmentVariableTokens.Matches(original);

            var modified = original;

            foreach (Match m in matches)
            {
                var withoutBrackets = m.Value.Substring(2, m.Value.Length - 3);

                var entry = FindEnvironmentVariable(withoutBrackets);

                if (entry == null)
                {
                    continue;
                }

                var env = (string)entry.Value.Value;

                modified = modified.Replace(m.Value, env);
            }

            return modified;
        }

        /// <summary>
        /// Helper method to find an environment variable if it exists
        /// </summary>
        /// <param name="name">environennt variable name</param>
        /// <returns>DictionaryEntry containing environment variable key value</returns>
        private static DictionaryEntry? FindEnvironmentVariable(string name)
        {
            var allEnvironmentVariables = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry de in allEnvironmentVariables)
            {
                if ((string)de.Key == name)
                {
                    return de;
                }
            }

            return null;
        }

        public static string[] SplitByComma(this string str)
        {
            return str?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Creates a relative path 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        public static string RelativePathTo(string start, string relativeTo)
        {
            start = Path.GetFullPath(start).Replace("\\", "/");
            relativeTo = Path.GetFullPath(relativeTo).Replace("\\", "/");

            string[] startDirs = start.Split('/');
            string[] relativeToDirs = relativeTo.Split('/');

            int len = startDirs.Length < relativeToDirs.Length ? startDirs.Length : relativeToDirs.Length;

            int lastCommonRoot = -1;
            int index;

            for (index = 0; index < len && string.Equals(startDirs[index], relativeToDirs[index], StringComparison.OrdinalIgnoreCase); index++)
            {
                lastCommonRoot = index;
            }

            // The 2 paths don't share a common ancestor. So the closest we can give is the absolute path to the target.
            if (lastCommonRoot == -1)
            {
                return relativeTo;
            }

            StringBuilder relativePath = new StringBuilder();
            for (index = lastCommonRoot + 1; index < startDirs.Length; index++)
            {
                if (startDirs[index].Length > 0) relativePath.Append("../");
            }

            for (index = lastCommonRoot + 1; index < relativeToDirs.Length; index++)
            {
                relativePath.Append(relativeToDirs[index]);
                if(index + 1 < relativeToDirs.Length)
                {
                    relativePath.Append("/");
                }
            }

            return relativePath.ToString();
        }

        /// <summary>
        /// Determine where the dotnet publish should put its artifacts at.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <param name="configuration"></param>
        /// <param name="targetFramework"></param>
        /// <returns></returns>
        public static string DeterminePublishLocation(string workingDirectory, string projectLocation, string configuration, string targetFramework)
        {
            var path = Path.Combine(DetermineProjectLocation(workingDirectory, projectLocation),
                    "bin",
                    configuration,
                    targetFramework,
                    "publish");
            return path;
        }

        public static string LookupTargetFrameworkFromProjectFile(string projectLocation)
        {
            var projectFile = FindProjectFileInDirectory(projectLocation);
            if (projectFile == null)
            {
                return null;
            }

            var projectOpts = new ProjectOptions
            {
                ProjectCollection = new ProjectCollection(),
                EvaluationContext = EvaluationContext.Create(Shared),
                LoadSettings = IgnoreMissingImports
            };

            var project = Project.FromFile(projectFile, projectOpts);
            var singularPropertyValue = project.GetPropertyValue("TargetFramework");
            if (!string.IsNullOrEmpty(singularPropertyValue))
            {
                return singularPropertyValue;
            }

            var pluralPropertyValue = project.GetPropertyValue("TargetFrameworks");
            if (string.IsNullOrEmpty(pluralPropertyValue))
            {
                return null;
            }

            var targetFrameworks = pluralPropertyValue.Split(';', StringSplitOptions.RemoveEmptyEntries);

            /* We need one unambiguous target framework. Otherwise, leave it indeterminate
             * so that we can fall back to the setting. */
            return targetFrameworks.Length == 1 ? targetFrameworks[0] : null;
        }

        public static string FindProjectFileInDirectory(string directory)
        {
            if (File.Exists(directory))
                return directory;
            
            foreach (var ext in new [] { "*.csproj", "*.fsproj", "*.vbproj" })
            {
                var files = Directory.GetFiles(directory, ext, SearchOption.TopDirectoryOnly);
                if (files.Length == 1)
                {
                    return files[0];
                }
            }

            return null;
        }

        /// <summary>
        /// Determines the location of the project depending on how the workingDirectory and projectLocation
        /// fields are set. This location is root of the project.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <returns></returns>
        public static string DetermineProjectLocation(string workingDirectory, string projectLocation)
        {
            string location;
            if (string.IsNullOrEmpty(projectLocation))
            {
                location = workingDirectory;
            }
            else if (string.IsNullOrEmpty(workingDirectory))
            {
                location = projectLocation;
            }
            else
            {
                location = Path.IsPathRooted(projectLocation) ? projectLocation : Path.Combine(workingDirectory, projectLocation);
            }

            if (location.EndsWith(@"\") || location.EndsWith(@"/"))
                location = location.Substring(0, location.Length - 1);

            return location;
        }
        
        /// <summary>
        /// Determine where the dotnet build directory is.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <param name="configuration"></param>
        /// <param name="targetFramework"></param>
        /// <returns></returns>
        public static string DetermineBuildLocation(string workingDirectory, string projectLocation, string configuration, string targetFramework)
        {
            var path = Path.Combine(
                DetermineProjectLocation(workingDirectory, projectLocation),
                "bin",
                configuration,
                targetFramework);
            return path;
        }

        /// <summary>
        /// A utility method for parsing KeyValue pair CommandOptions.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseKeyValueOption(string option)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(option))
                return parameters;

            try
            {
                var currentPos = 0;
                while (currentPos != -1 && currentPos < option.Length)
                {
                    string name;
                    GetNextToken(option, '=', ref currentPos, out name);

                    string value;
                    GetNextToken(option, ';', ref currentPos, out value);

                    if (string.IsNullOrEmpty(name))
                        throw new ToolsException($"Error parsing option ({option}), format should be <key1>=<value1>;<key2>=<value2>", ToolsException.CommonErrorCode.CommandLineParseError);

                    parameters[name] = value ?? string.Empty;
                }
            }
            catch (ToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error parsing option ({option}), format should be <key1>=<value1>;<key2>=<value2>: {e.Message}", ToolsException.CommonErrorCode.CommandLineParseError);
            }


            return parameters;
        }
        private static void GetNextToken(string option, char endToken, ref int currentPos, out string token)
        {
            if (option.Length <= currentPos)
            {
                token = string.Empty;
                return;
            }

            int tokenStart = currentPos;
            int tokenEnd = -1;
            bool inQuote = false;
            if (option[currentPos] == '"')
            {
                inQuote = true;
                tokenStart++;
                currentPos++;

                while (currentPos < option.Length && option[currentPos] != '"')
                {
                    currentPos++;
                }

                if (option[currentPos] == '"')
                    tokenEnd = currentPos;
            }

            while (currentPos < option.Length && option[currentPos] != endToken)
            {
                currentPos++;
            }


            if (!inQuote)
            {
                if (currentPos < option.Length && option[currentPos] == endToken)
                    tokenEnd = currentPos;
            }

            if (tokenEnd == -1)
                token = option.Substring(tokenStart);
            else
                token = option.Substring(tokenStart, tokenEnd - tokenStart);

            currentPos++;
        }

        public static string DetermineToolVersion()
        {
            AssemblyInformationalVersionAttribute attribute = null;
            try
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                    return null;
                attribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            }
            catch (Exception)
            {
                // ignored
            }

            return attribute?.InformationalVersion;
        }

        public static void ZipDirectory(IToolLogger logger, string directory, string zipArchivePath)
        {
            zipArchivePath = Path.GetFullPath(zipArchivePath);
            if (File.Exists(zipArchivePath))
                File.Delete(zipArchivePath);

            var zipArchiveParentDirectory = Path.GetDirectoryName(zipArchivePath);
            if (!Directory.Exists(zipArchiveParentDirectory))
            {
                logger?.WriteLine($"Creating directory {zipArchiveParentDirectory}");
                new DirectoryInfo(zipArchiveParentDirectory).Create();
            }

#if NETCOREAPP3_1_OR_GREATER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                BundleWithDotNetCompression(zipArchivePath, directory, logger);
            }
            else
            {
                // Use the native zip utility if it exist which will maintain linux/osx file permissions
                var zipCLI = AbstractCLIWrapper.FindExecutableInPath("zip");
                if (!string.IsNullOrEmpty(zipCLI))
                {
                    BundleWithZipCLI(zipCLI, zipArchivePath, directory, logger);
                }
                else
                {
                    throw new ToolsException("Failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.", ToolsException.CommonErrorCode.FailedToFindZipProgram);
                }
            }
#else
            BundleWithDotNetCompression(zipArchivePath, directory, logger);
#endif
        }


        /// <summary>
        /// Get the list of files from the publish folder that should be added to the zip archive.
        /// This will skip all files in the runtimes folder because they have already been flatten to the root.
        /// </summary>
        /// <param name="publishLocation"></param>
        /// <returns></returns>
        private static IDictionary<string, string> GetFilesToIncludeInArchive(string publishLocation)
        {
            const char uniformDirectorySeparator = '/';
            var includedFiles = new Dictionary<string, string>();
            var allFiles = Directory.GetFiles(publishLocation, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var relativePath = file.Substring(publishLocation.Length)
                    .Replace(Path.DirectorySeparatorChar.ToString(), uniformDirectorySeparator.ToString());
                
                if (relativePath[0] == uniformDirectorySeparator)
                    relativePath = relativePath.Substring(1);

                includedFiles[relativePath] = file;
            }

            return includedFiles;
        }

        /// <summary>
        /// Zip up the publish folder using the .NET compression libraries. This is what is used when run on Windows.
        /// </summary>
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="publishLocation">The location to be bundled.</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithDotNetCompression(string zipArchivePath, string publishLocation, IToolLogger logger)
        {
            using (var zipArchive = ZipFile.Open(zipArchivePath, ZipArchiveMode.Create))
            {
                var includedFiles = GetFilesToIncludeInArchive(publishLocation);
                foreach (var kvp in includedFiles)
                {
                    zipArchive.CreateEntryFromFile(kvp.Value, kvp.Key);

                    logger?.WriteLine($"... zipping: {kvp.Key}");
                }
            }
        }

        /// <summary>
        /// Creates the deployment bundle using the native zip tool installed
        /// on the system (default /usr/bin/zip). This is what is typically used on Linux and OSX
        /// </summary>
        /// <param name="zipCLI">The path to the located zip binary.</param>
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="publishLocation">The location to be bundled.</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithZipCLI(string zipCLI, string zipArchivePath, string publishLocation, IToolLogger logger)
        {
            var args = new StringBuilder("\"" + zipArchivePath + "\"");

            var allFiles = GetFilesToIncludeInArchive(publishLocation);
            foreach (var kvp in allFiles)
            {
                args.AppendFormat(" \"{0}\"", kvp.Key);
            }

            var psiZip = new ProcessStartInfo
            {
                FileName = zipCLI,
                Arguments = args.ToString(),
                WorkingDirectory = publishLocation,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var handler = (DataReceivedEventHandler)((o, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                logger?.WriteLine("... zipping: " + e.Data);
            });

            using (var proc = new Process())
            {
                proc.StartInfo = psiZip;
                proc.Start();

                proc.ErrorDataReceived += handler;
                proc.OutputDataReceived += handler;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                proc.EnableRaisingEvents = true;
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    logger?.WriteLine(string.Format("Created publish archive ({0}).", zipArchivePath));
                }
            }
        }
        
        public static async Task ValidateBucketRegionAsync(IAmazonS3 s3Client, string s3Bucket)
        {
            string bucketRegion;
            try
            {
                bucketRegion = await Utilities.GetBucketRegionAsync(s3Client, s3Bucket);
            }
            catch(Exception e)
            {
                Console.Error.WriteLine($"Warning: Unable to determine region for bucket {s3Bucket}, assuming bucket is in correct region: {e.Message}", ToolsException.CommonErrorCode.S3GetBucketLocation, e);
                return;
            }

            var configuredRegion = s3Client.Config.RegionEndpoint?.SystemName;
            if(configuredRegion == null && !string.IsNullOrEmpty(s3Client.Config.ServiceURL))
            {
                configuredRegion = AWSSDKUtils.DetermineRegion(s3Client.Config.ServiceURL);
            }

            // If we still don't know the region and assume we are running in a non standard way and assume the caller
            // knows what they are doing.
            if (configuredRegion == null)
                return;
            
            if (!string.Equals(bucketRegion, configuredRegion))
            {
                throw new ToolsException($"Error: S3 bucket must be in the same region as the configured region {configuredRegion}. {s3Bucket} is in the region {bucketRegion}.", ToolsException.CommonErrorCode.BucketInDifferentRegionThenClient);
            }

        }

        private static async Task<string> GetBucketRegionAsync(IAmazonS3 s3Client, string bucket)
        {
            var request = new GetBucketLocationRequest { BucketName = bucket };
            var response = await s3Client.GetBucketLocationAsync(request);

            // Handle the legacy naming conventions
            if (response.Location == S3Region.US)
                return "us-east-1";
            if (response.Location == S3Region.EU)
                return "eu-west-1";

            return response.Location.Value;
        }

        public static async Task<bool> EnsureBucketExistsAsync(IToolLogger logger, IAmazonS3 s3Client, string bucketName)
        {
            bool ret = false;
            logger?.WriteLine("Making sure bucket '" + bucketName + "' exists");
            try
            {
                await s3Client.PutBucketAsync(new PutBucketRequest() { BucketName = bucketName, UseClientRegion = true });
                ret = true;
            }
            catch (AmazonS3Exception exc)
            {
                if (System.Net.HttpStatusCode.Conflict != exc.StatusCode)
                {
                    logger?.WriteLine("Attempt to create deployment upload bucket caught AmazonS3Exception, StatusCode '{0}', Message '{1}'", exc.StatusCode, exc.Message);
                }
                else
                {
                    // conflict may occur if bucket belongs to another user or if bucket owned by this user but in a different region
                    if (exc.ErrorCode != "BucketAlreadyOwnedByYou")
                        logger?.WriteLine("Unable to use bucket name '{0}'; bucket exists but is not owned by you\r\n...S3 error was '{1}'.", bucketName, exc.Message);
                    else
                    {
                        logger?.WriteLine("..a bucket with name '{0}' already exists and will be used for upload", bucketName);
                        ret = true;
                    }
                }
            }
            catch (Exception exc)
            {
                logger?.WriteLine("Attempt to create deployment upload bucket caught Exception, Message '{0}'", exc.Message);
            }

            return ret;
        }

        public static Task<string> UploadToS3Async(IToolLogger logger, IAmazonS3 s3Client, string bucket, string prefix, string rootName, Stream stream)
        {
            var extension = ".zip";
            if (!string.IsNullOrEmpty(Path.GetExtension(rootName)))
            {
                extension = Path.GetExtension(rootName);
                rootName = Path.GetFileNameWithoutExtension(rootName);
            }

            var key = (prefix ?? "") + $"{rootName}-{DateTime.Now.Ticks}{extension}";

            return UploadToS3Async(logger, s3Client, bucket, key, stream);
        }

        public static async Task<string> UploadToS3Async(IToolLogger logger, IAmazonS3 s3Client, string bucket, string key, Stream stream)
        {
            logger?.WriteLine($"Uploading to S3. (Bucket: {bucket} Key: {key})");

            var request = new TransferUtilityUploadRequest()
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream
            };

            request.UploadProgressEvent += Utilities.CreateTransferUtilityProgressHandler(logger);

            try
            {
                await new TransferUtility(s3Client).UploadAsync(request);
            }
            catch (Exception e)
            {
                throw new ToolsException($"Error uploading to {key} in bucket {bucket}: {e.Message}", ToolsException.CommonErrorCode.S3UploadError, e);
            }

            return key;
        }

        const int UPLOAD_PROGRESS_INCREMENT = 10;
        private static EventHandler<StreamTransferProgressArgs> CreateProgressHandler(IToolLogger logger)
        {
            var percentToUpdateOn = UPLOAD_PROGRESS_INCREMENT;
            EventHandler<StreamTransferProgressArgs> handler = ((s, e) =>
            {
                if (e.PercentDone != percentToUpdateOn && e.PercentDone <= percentToUpdateOn) return;
                
                var increment = e.PercentDone % UPLOAD_PROGRESS_INCREMENT;
                if (increment == 0)
                    increment = UPLOAD_PROGRESS_INCREMENT;
                percentToUpdateOn = e.PercentDone + increment;
                logger?.WriteLine($"... Progress: {e.PercentDone}%");
            });

            return handler;
        }
        
        private static EventHandler<UploadProgressArgs> CreateTransferUtilityProgressHandler(IToolLogger logger)
        {
            var percentToUpdateOn = UPLOAD_PROGRESS_INCREMENT;
            EventHandler<UploadProgressArgs> handler = ((s, e) =>
            {
                if (e.PercentDone != percentToUpdateOn && e.PercentDone <= percentToUpdateOn) return;
                
                var increment = e.PercentDone % UPLOAD_PROGRESS_INCREMENT;
                if (increment == 0)
                    increment = UPLOAD_PROGRESS_INCREMENT;
                percentToUpdateOn = e.PercentDone + increment;
                logger?.WriteLine($"... Progress: {e.PercentDone}%");
            });

            return handler;
        }
        
        internal static int WaitForPromptResponseByIndex(int min, int max)
        {
            int chosenIndex = -1;
            while (chosenIndex == -1)
            {
                var indexInput = Console.ReadLine()?.Trim();
                int parsedIndex;
                if (int.TryParse(indexInput, out parsedIndex) && parsedIndex >= min && parsedIndex <= max)
                {
                    chosenIndex = parsedIndex;
                }
                else
                {
                    Console.Out.WriteLine($"Invalid selection, must be a number between {min} and {max}");
                }
            }

            return chosenIndex;
        }
        
        
        static readonly string GENERIC_ASSUME_ROLE_POLICY =
            @"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""{ASSUME_ROLE_PRINCIPAL}""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static string GetAssumeRolePolicy(string assumeRolePrincipal)
        {
            return GENERIC_ASSUME_ROLE_POLICY.Replace("{ASSUME_ROLE_PRINCIPAL}", assumeRolePrincipal);
        }

        public class ExecuteShellCommandResult
        {
            public int ExitCode { get; }
            public string Stdout { get; }

            public ExecuteShellCommandResult(int exitCode, string stdout)
            {
                this.ExitCode = exitCode;
                this.Stdout = stdout;
            }
        }
        public static ExecuteShellCommandResult ExecuteShellCommand(string workingDirectory, string process, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = process,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            StringBuilder capturedOutput = new StringBuilder();
            var handler = (DataReceivedEventHandler)((o, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                capturedOutput.AppendLine(e.Data);
            });            
            
            using (var proc = new Process())
            {
                proc.StartInfo = startInfo;
                proc.Start();

                if (startInfo.RedirectStandardOutput)
                {
                    proc.ErrorDataReceived += handler;
                    proc.OutputDataReceived += handler;
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    proc.EnableRaisingEvents = true;
                }

                proc.WaitForExit();
                return new ExecuteShellCommandResult(proc.ExitCode, capturedOutput.ToString());
            }            
        }

        public static string ReadSecretFromConsole()
        {
            var code = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (code.Length > 0)
                    {
                        code.Remove(code.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                // i.Key > 31: Skip the initial ascii control characters like ESC and tab. The space character is 32.
                // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
                else if ((int)i.Key > 31 && i.KeyChar != '\u0000') 
                {
                    code.Append(i.KeyChar);
                    Console.Write("*");
                }
            }
            return code.ToString().Trim();
        }
        
        
        public static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool copySubDirectories)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirectory);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirectory);
            }
            
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destinationDirectory, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirectories)
            {
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdirectory in dirs)
                {
                    string temppath = Path.Combine(destinationDirectory, subdirectory.Name);
                    CopyDirectory(subdirectory.FullName, temppath, copySubDirectories);
                }
            }
        }

        public static bool TryGenerateECRRepositoryName(string projectName, out string repositoryName)
        {
            repositoryName = null;
            if (Directory.Exists(projectName))
            {
                projectName = new DirectoryInfo(projectName).Name;
            }
            else if(File.Exists(projectName))
            {
                projectName = Path.GetFileNameWithoutExtension(projectName);
            }

            projectName = projectName.ToLower();
            var sb = new StringBuilder();

            foreach(var c in projectName)
            {
                if(char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if(sb.Length > 0 && (c == '.' || c == '_' || c == '-'))
                {
                    sb.Append(c);
                }
            }

            // Repository name must be at least 2 characters
            if(sb.Length > 1)
            {
                repositoryName = sb.ToString();

                // Max length of repository name is 256 characters.
                if (Constants.MAX_ECR_REPOSITORY_NAME_LENGTH < repositoryName.Length)
                {
                    repositoryName = repositoryName.Substring(0, Constants.MAX_ECR_REPOSITORY_NAME_LENGTH);
                }
            }

            return !string.IsNullOrEmpty(repositoryName);
        }
    }
}
