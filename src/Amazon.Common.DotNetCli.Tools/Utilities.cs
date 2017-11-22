using Amazon.Runtime;
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
using System.Xml.Linq;
using System.Xml.XPath;

namespace Amazon.Common.DotNetCli.Tools
{
    public static class Utilities
    {

        public static string[] SplitByComma(this string str)
        {
            return str?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Determine where the dotnet publish should put its artifacts at.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <param name="configuration"></param>
        /// <param name="targetFramework"></param>
        /// <returns></returns>
        public static string DeterminePublishLocation(string projectLocation, string configuration, string targetFramework)
        {
            var path = Path.Combine(projectLocation,
                    "bin",
                    configuration,
                    targetFramework,
                    "publish");
            return path;
        }

        public static string LookupTargetFrameworkFromProjectFile(string projectLocation)
        {
            var files = Directory.GetFiles(projectLocation, "*.csproj", SearchOption.TopDirectoryOnly);
            if (files.Length != 1)
                return null;

            var xdoc = XDocument.Load(files[0]);

            var element = xdoc.XPathSelectElement("//PropertyGroup/TargetFramework");
            if (element != null)
                return element.Value;


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
            else
            {
                if (Path.IsPathRooted(projectLocation))
                    location = projectLocation;
                else
                    location = Path.Combine(workingDirectory, projectLocation);
            }

            if (location.EndsWith(@"\") || location.EndsWith(@"/"))
                location = location.Substring(0, location.Length - 1);

            return location;
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
                int currentPos = 0;
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
            }

            return attribute != null ? attribute.InformationalVersion : null;
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

#if NETCORE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                BundleWithDotNetCompression(zipArchivePath, directory, logger);
            }
            else
            {
                // Use the native zip utility if it exist which will maintain linux/osx file permissions
                var zipCLI = DotNetCLIWrapper.FindExecutableInPath("zip");
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
        /// <param name="flattenRuntime">If true the runtimes folder will be flatten</param>
        /// <returns></returns>
        private static IDictionary<string, string> GetFilesToIncludeInArchive(string publishLocation)
        {
            var includedFiles = new Dictionary<string, string>();
            var allFiles = Directory.GetFiles(publishLocation, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var relativePath = file.Substring(publishLocation.Length);
                if (relativePath[0] == Path.DirectorySeparatorChar)
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
        /// <param name="flattenRuntime">If true the runtimes folder will be flatten</param>
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
        /// <param name="flattenRuntime">If true the runtimes folder will be flatten</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithZipCLI(string zipCLI, string zipArchivePath, string publishLocation, IToolLogger logger)
        {
            var args = new StringBuilder("\"" + zipArchivePath + "\"");

            // so that we can archive content in subfolders, take the length of the
            // path to the root publish location and we'll just substring the
            // found files so the subpaths are retained
            var publishRootLength = publishLocation.Length;
            if (publishLocation[publishRootLength - 1] != Path.DirectorySeparatorChar)
                publishRootLength++;

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

        public static async Task<bool> EnsureBucketExistsAsync(IToolLogger logger, IAmazonS3 s3Client, string bucketName)
        {
            bool ret = false;
            logger?.WriteLine("Making sure bucket '" + bucketName + "' exists");
            try
            {
                var response =  await s3Client.PutBucketAsync(new PutBucketRequest() { BucketName = bucketName, UseClientRegion = true });
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

            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = stream
            };
            request.StreamTransferProgress = Utilities.CreateProgressHandler(logger);

            try
            {
                await s3Client.PutObjectAsync(request);
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
            int percentToUpdateOn = UPLOAD_PROGRESS_INCREMENT;
            EventHandler<StreamTransferProgressArgs> handler = ((s, e) =>
            {
                if (e.PercentDone == percentToUpdateOn || e.PercentDone > percentToUpdateOn)
                {
                    int increment = e.PercentDone % UPLOAD_PROGRESS_INCREMENT;
                    if (increment == 0)
                        increment = UPLOAD_PROGRESS_INCREMENT;
                    percentToUpdateOn = e.PercentDone + increment;
                    logger?.WriteLine($"... Progress: {e.PercentDone}%");
                }
            });

            return handler;
        }
    }
}
