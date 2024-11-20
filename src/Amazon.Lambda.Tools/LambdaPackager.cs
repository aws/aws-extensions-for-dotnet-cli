using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Amazon.Common.DotNetCli.Tools;
using ThirdParty.Json.LitJson;

namespace Amazon.Lambda.Tools
{
    /// <summary>
    /// This class will create the lambda zip package that can be upload to Lambda for deployment.
    /// </summary>
    public static class LambdaPackager
    {
        private const string Shebang = "#!";
        private const char LinuxLineEnding = '\n';
        private const string BootstrapFilename = "bootstrap";
        private const string LinuxOSReleaseFile = @"/etc/os-release";
        private const string AmazonLinuxNameInOSReleaseFile = "NAME=\"Amazon Linux\"";
        private const string AmazonLinux2InOSReleaseFile = "VERSION=\"2\"";
        private const string AmazonLinux2023InOSReleaseFile = "VERSION=\"2023\"";
#if NETCOREAPP3_1_OR_GREATER        
        private static readonly string BuildLambdaZipCliPath = Path.Combine(
            Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath),
            "Resources\\build-lambda-zip.exe");
#else
        private static readonly string BuildLambdaZipCliPath = Path.Combine(
            Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath),
            "Resources\\build-lambda-zip.exe");
#endif
        static IDictionary<string, Version> NETSTANDARD_LIBRARY_VERSIONS = new Dictionary<string, Version>
        {
            { "netcoreapp1.0", Version.Parse("1.6.0") },
            { "netcoreapp1.1", Version.Parse("1.6.1") }
        };

        private static bool IsAmazonLinux(IToolLogger logger)
        {
#if !NETCOREAPP3_1_OR_GREATER
        return false;
#else
            return IsAmazonLinux2(logger) || IsAmazonLinux2023(logger);
#endif
        }

        private static bool IsAmazonLinux2(IToolLogger logger)
        {
#if !NETCOREAPP3_1_OR_GREATER
        return false;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (File.Exists(LinuxOSReleaseFile))
                {
                    logger?.WriteLine($"Found {LinuxOSReleaseFile}");
                    string readText = File.ReadAllText(LinuxOSReleaseFile);
                    if (readText.Contains(AmazonLinuxNameInOSReleaseFile) && readText.Contains(AmazonLinux2InOSReleaseFile))
                    {
                        logger?.WriteLine(
                            $"Linux distribution is Amazon Linux 2, NativeAOT container build is optional");
                        return true;
                    }
                }
            }

            return false;
#endif
        }

        private static bool IsAmazonLinux2023(IToolLogger logger)
        {
#if !NETCOREAPP3_1_OR_GREATER
        return false;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (File.Exists(LinuxOSReleaseFile))
                {
                    logger?.WriteLine($"Found {LinuxOSReleaseFile}");
                    string readText = File.ReadAllText(LinuxOSReleaseFile);
                    if (readText.Contains(AmazonLinuxNameInOSReleaseFile) && readText.Contains(AmazonLinux2023InOSReleaseFile))
                    {
                        logger?.WriteLine(
                            $"Linux distribution is Amazon Linux 2023, NativeAOT container build is optional");
                        return true;
                    }
                }
            }

            return false;
#endif
        }

        /// <summary>
        /// Execute the dotnet publish command and zip up the resulting publish folder.
        /// </summary>
        /// <param name="defaults"></param>
        /// <param name="logger"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <param name="targetFramework"></param>
        /// <param name="configuration"></param>
        /// <param name="msbuildParameters"></param>
        /// <param name="disableVersionCheck"></param>
        /// <param name="publishLocation"></param>
        /// <param name="zipArchivePath"></param>
        public static bool CreateApplicationBundle(LambdaToolsDefaults defaults, IToolLogger logger, string workingDirectory,
            string projectLocation, string configuration, string targetFramework, string msbuildParameters, string architecture,
            bool disableVersionCheck, LayerPackageInfo layerPackageInfo, bool isNativeAot,
            bool? useContainerForBuild, string containerImageForBuild, string codeMountDirectory,
            out string publishLocation, ref string zipArchivePath)
        {
            LambdaUtilities.ValidateTargetFramework(projectLocation, targetFramework, isNativeAot);

            LambdaUtilities.ValidateNativeAotArchitecture(architecture, isNativeAot);

            // If use container is set to false explicitly, then that overrides other values.
            if (useContainerForBuild.HasValue && !useContainerForBuild.Value)
            {
                containerImageForBuild = null;
            }
            // Else, if we haven't been given a build image, we need to figure out if we should use a default
            else if (string.IsNullOrWhiteSpace(containerImageForBuild))
            {
                // Use a default container image if Use Container is set to true, or if we need to build NativeAOT on non-AL2
                if ((useContainerForBuild.HasValue && useContainerForBuild.Value) || (isNativeAot && !IsAmazonLinux(logger)))
                {
                    containerImageForBuild = LambdaUtilities.GetDefaultBuildImage(targetFramework, architecture, logger);
                }
            }
            // Otherwise, we've been given a build image to use, so always use that.
            // Below, the code only considers whether containerImageForBuild is set or not

            LogDeprecationMessagesIfNecessary(logger, targetFramework);

            if (msbuildParameters != null && msbuildParameters.Contains("--self-contained true"))
            {
                if (string.Equals(architecture, LambdaConstants.ARCHITECTURE_ARM64) && IsAmazonLinux2(logger))
                {
                    logger.WriteLine("WARNING: There is an issue with self-contained ARM-based .NET Lambda functions using custom runtimes on Amazon Linux 2 that causes functions to fail to run.");
                    logger.WriteLine("For more information and workarounds, see: https://github.com/aws/aws-lambda-dotnet/issues/920");
                }
                else if (IsAmazonLinux2023(logger))
                {
                    logger.WriteLine("WARNING: There is an issue with self-contained .NET Lambda functions using custom runtimes on Amazon Linux 2023 that causes functions to fail to run.");
                    logger.WriteLine("This applies to both AMD and ARM architectures.");
                    logger.WriteLine("For more information and workarounds, see: https://github.com/aws/aws-lambda-dotnet/issues/920");
                }
            }

            if (string.IsNullOrEmpty(configuration))
                configuration = LambdaConstants.DEFAULT_BUILD_CONFIGURATION;

            var lambdaRuntimePackageStoreManifestContent = LambdaUtilities.LoadPackageStoreManifest(logger, targetFramework);

            var publishManifestPath = new List<string>();
            if (!string.IsNullOrEmpty(lambdaRuntimePackageStoreManifestContent))
            {
                var tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, lambdaRuntimePackageStoreManifestContent);
                publishManifestPath.Add(tempFile);
            }

            if (layerPackageInfo != null)
            {
                foreach (var info in layerPackageInfo.Items)
                {
                    publishManifestPath.Add(info.ManifestPath);
                }
            }

            publishLocation = Utilities.DeterminePublishLocation(workingDirectory, projectLocation, configuration, targetFramework);

            logger?.WriteLine("Executing publish command");
            var cli = new LambdaDotNetCLIWrapper(logger, workingDirectory);

            if (!string.IsNullOrWhiteSpace(containerImageForBuild))
            {
                var containerBuildLogMessage = isNativeAot ? $"Starting container for native AOT build using build image: {containerImageForBuild}." : $"Starting container for build using build image: {containerImageForBuild}.";
                logger.WriteLine(containerBuildLogMessage);

                var directoryToMountToContainer = Utilities.GetSolutionDirectoryFullPath(workingDirectory, projectLocation, codeMountDirectory);

                var dockerCli = new DockerCLIWrapper(logger, directoryToMountToContainer);

                var containerName = $"tempLambdaBuildContainer-{Guid.NewGuid()}";

                string relativeContainerPathToProjectLocation = string.Concat(DockerCLIWrapper.WorkingDirectoryMountLocation, Utilities.RelativePathTo(directoryToMountToContainer, projectLocation));

                // This value is the path inside of the container that will map directly to the out parameter "publishLocation" on the host machine
                string relativeContainerPathToPublishLocation = Utilities.DeterminePublishLocation(null, relativeContainerPathToProjectLocation, configuration, targetFramework);

                var publishCommand = "dotnet " + cli.GetPublishArguments(projectLocation, relativeContainerPathToPublishLocation, targetFramework, configuration, msbuildParameters, architecture, publishManifestPath, isNativeAot, relativeContainerPathToProjectLocation);

                var runResult = dockerCli.Run(containerImageForBuild, containerName, publishCommand);
                if (runResult != 0)
                {
                    throw new LambdaToolsException($"ERROR: Container build returned {runResult}", LambdaToolsException.LambdaErrorCode.ContainerBuildFailed);
                }
            }
            else
            {
                if (cli.Publish(defaults: defaults,
                    projectLocation: projectLocation,
                    outputLocation: publishLocation,
                    targetFramework: targetFramework,
                    configuration: configuration,
                    msbuildParameters: msbuildParameters,
                    architecture: architecture,
                    publishManifests: publishManifestPath) != 0)
                {
                    throw new LambdaToolsException($"ERROR: The dotnet publish command return unsuccessful error code", LambdaToolsException.CommonErrorCode.ShellOutToDotnetPublishFailed);
                }
            }

            var buildLocation = Utilities.DetermineBuildLocation(workingDirectory, projectLocation, configuration, targetFramework);

            // This is here for legacy reasons. Some older versions of the dotnet CLI were not 
            // copying the deps.json file into the publish folder.
            foreach (var file in Directory.GetFiles(buildLocation, "*.deps.json", SearchOption.TopDirectoryOnly))
            {
                var destinationPath = Path.Combine(publishLocation, Path.GetFileName(file));
                if (!File.Exists(destinationPath))
                    File.Copy(file, destinationPath);
            }

            bool flattenRuntime = false;
            var depsJsonTargetNode = GetDepsJsonTargetNode(logger, publishLocation);
            // If there is no target node then this means the tool is being used on a future version of .NET Core
            // then was available when the this tool was written. Go ahead and continue the deployment with warnings so the
            // user can see if the future version will work.
            if (depsJsonTargetNode != null && string.Equals(targetFramework, "netcoreapp1.0", StringComparison.OrdinalIgnoreCase))
            {
                // Make sure the project is not pulling in dependencies requiring a later version of .NET Core then the declared target framework
                if (!ValidateDependencies(logger, targetFramework, depsJsonTargetNode, disableVersionCheck))
                    return false;

                // Flatten the runtime folder which reduces the package size by not including native dependencies
                // for other platforms.
                flattenRuntime = FlattenRuntimeFolder(logger, publishLocation, depsJsonTargetNode);
            }

            FlattenPowerShellRuntimeModules(logger, publishLocation, targetFramework);


            if (zipArchivePath == null)
                zipArchivePath = Path.Combine(Directory.GetParent(publishLocation).FullName, new DirectoryInfo(projectLocation).Name + ".zip");

            zipArchivePath = Path.GetFullPath(zipArchivePath);
            logger?.WriteLine($"Zipping publish folder {publishLocation} to {zipArchivePath}");
            if (File.Exists(zipArchivePath))
                File.Delete(zipArchivePath);

            var zipArchiveParentDirectory = Path.GetDirectoryName(zipArchivePath);
            if (!Directory.Exists(zipArchiveParentDirectory))
            {
                logger?.WriteLine($"Creating directory {zipArchiveParentDirectory}");
                new DirectoryInfo(zipArchiveParentDirectory).Create();
            }


            BundleDirectory(zipArchivePath, publishLocation, flattenRuntime, logger);

            return true;
        }

        public static void BundleDirectory(string zipArchivePath, string sourceDirectory, bool flattenRuntime, IToolLogger logger)
        {
#if NETCOREAPP3_1_OR_GREATER
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                BundleWithBuildLambdaZip(zipArchivePath, sourceDirectory, flattenRuntime, logger);
            }
            else
            {
                // Use the native zip utility if it exist which will maintain linux/osx file permissions
                var zipCLI = LambdaDotNetCLIWrapper.FindExecutableInPath("zip");
                if (!string.IsNullOrEmpty(zipCLI))
                {
                    BundleWithZipCLI(zipCLI, zipArchivePath, sourceDirectory, flattenRuntime, logger);
                }
                else
                {
                    throw new LambdaToolsException("Failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.", LambdaToolsException.LambdaErrorCode.FailedToFindZipProgram);
                }
            }
#else
            BundleWithBuildLambdaZip(zipArchivePath, sourceDirectory, flattenRuntime, logger);
#endif            
        }

        public static void BundleFiles(string zipArchivePath, string rootDirectory, string[] files, IToolLogger logger)
        {
            var includedFiles = ConvertToMapOfFiles(rootDirectory, files);

#if NETCOREAPP3_1_OR_GREATER
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                BundleWithBuildLambdaZip(zipArchivePath, rootDirectory, includedFiles, logger);
            }
            else
            {
                // Use the native zip utility if it exist which will maintain linux/osx file permissions
                var zipCLI = LambdaDotNetCLIWrapper.FindExecutableInPath("zip");
                if (!string.IsNullOrEmpty(zipCLI))
                {
                    BundleWithZipCLI(zipCLI, zipArchivePath, rootDirectory, includedFiles, logger);
                }
                else
                {
                    throw new LambdaToolsException("Failed to find the \"zip\" utility program in path. This program is required to maintain Linux file permissions in the zip archive.", LambdaToolsException.LambdaErrorCode.FailedToFindZipProgram);
                }
            }
#else
                BundleWithBuildLambdaZip(zipArchivePath, rootDirectory, includedFiles, logger);
#endif
        }


        public static IDictionary<string, string> ConvertToMapOfFiles(string rootDirectory, string[] files)
        {
            rootDirectory = rootDirectory.Replace("\\", "/");
            if (!rootDirectory.EndsWith("/"))
                rootDirectory += "/";

            var includedFiles = new Dictionary<string, string>(files.Length);
            foreach (var file in files)
            {
                var normalizedFile = file.Replace("\\", "/");
                if (Path.IsPathRooted(file))
                {
                    var relativePath = file.Substring(rootDirectory.Length);
                    includedFiles[relativePath] = normalizedFile;
                }
                else
                {
                    includedFiles[normalizedFile] = Path.Combine(rootDirectory, normalizedFile).Replace("\\", "/");
                }
            }

            return includedFiles;
        }

        /// <summary>
        /// Return the targets node which declares all the dependencies for the project along with the dependency's dependencies.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="publishLocation"></param>
        /// <returns></returns>
        private static JsonData GetDepsJsonTargetNode(IToolLogger logger, string publishLocation)
        {
            var depsJsonFilepath = Directory.GetFiles(publishLocation, "*.deps.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!File.Exists(depsJsonFilepath))
            {
                logger?.WriteLine($"Missing deps.json file. Skipping flattening runtime folder because {depsJsonFilepath} is an unrecognized format");
                return null;
            }

            var depsRootData = JsonMapper.ToObject(File.ReadAllText(depsJsonFilepath));
            var runtimeTargetNode = depsRootData["runtimeTarget"];
            if (runtimeTargetNode == null)
            {
                logger?.WriteLine($"Missing runtimeTarget node. Skipping flattening runtime folder because {depsJsonFilepath} is an unrecognized format");
                return null;
            }

            string runtimeTarget;
            if (runtimeTargetNode.IsString)
            {
                runtimeTarget = runtimeTargetNode.ToString();
            }
            else
            {
                runtimeTarget = runtimeTargetNode["name"]?.ToString();
            }

            if (runtimeTarget == null)
            {
                logger?.WriteLine($"Missing runtimeTarget name. Skipping flattening runtime folder because {depsJsonFilepath} is an unrecognized format");
                return null;
            }

            var target = depsRootData["targets"]?[runtimeTarget];
            if (target == null)
            {
                logger?.WriteLine($"Missing targets node. Skipping flattening runtime folder because {depsJsonFilepath} is an unrecognized format");
                return null;
            }

            return target;
        }

        /// <summary>
        /// Check to see if any of the dependencies listed in the deps.json file are pulling in later version of NETStandard.Library
        /// then the target framework supports.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="targetFramework"></param>
        /// <param name="depsJsonTargetNode"></param>
        /// <param name="disableVersionCheck"></param>
        /// <returns></returns>
        private static bool ValidateDependencies(IToolLogger logger, string targetFramework, JsonData depsJsonTargetNode, bool disableVersionCheck)
        {
            Version maxNETStandardLibraryVersion;
            // If we don't know the NETStandard.Library NuGet package version then skip validation. This is to handle
            // the case we are packaging up for a future target framework verion then this version of the tooling knows about.
            // Skip validation so the tooling doesn't get in the way.
            if (!NETSTANDARD_LIBRARY_VERSIONS.TryGetValue(targetFramework, out maxNETStandardLibraryVersion))
                return true;

            var dependenciesUsingNETStandard = new List<string>();
            Version referencedNETStandardLibrary = null;

            var errorLevel = disableVersionCheck ? "Warning" : "Error";

            foreach (KeyValuePair<string, JsonData> dependencyNode in depsJsonTargetNode)
            {
                var nameAndVersion = dependencyNode.Key.Split('/');
                if (nameAndVersion.Length != 2)
                    continue;

                if (string.Equals(nameAndVersion[0], "netstandard.library", StringComparison.OrdinalIgnoreCase))
                {
                    if(!Version.TryParse(nameAndVersion[1], out referencedNETStandardLibrary))
                    {
                        logger.WriteLine($"{errorLevel} parsing version number for declared NETStandard.Library: {nameAndVersion[1]}");
                        return true;
                    }
                }
                // Collect the dependencies that are pulling in the NETStandard.Library metapackage
                else
                {
                    var subDependencies = dependencyNode.Value["dependencies"];
                    if (subDependencies != null)
                    {
                        foreach (KeyValuePair<string, JsonData> subDependency in subDependencies)
                        {
                            if (string.Equals(subDependency.Key, "netstandard.library", StringComparison.OrdinalIgnoreCase))
                            {
                                dependenciesUsingNETStandard.Add(nameAndVersion[0] + " : " + nameAndVersion[1]);
                                break;
                            }
                        }
                    }
                }
            }

            // If true the project is pulling in a new version of NETStandard.Library then the target framework supports.
            if(referencedNETStandardLibrary != null && maxNETStandardLibraryVersion < referencedNETStandardLibrary)
            {
                logger?.WriteLine($"{errorLevel}: Project is referencing NETStandard.Library version {referencedNETStandardLibrary}. Max version supported by {targetFramework} is {maxNETStandardLibraryVersion}.");

                // See if we can find the target framework that does support the version the project is pulling in.
                // This can help the user know what framework their dependencies are targeting instead of understanding NuGet version numbers.
                var matchingTargetFramework = NETSTANDARD_LIBRARY_VERSIONS.FirstOrDefault(x =>
                {
                    return x.Value.Equals(referencedNETStandardLibrary);
                });

                if(!string.IsNullOrEmpty(matchingTargetFramework.Key))
                {
                    logger?.WriteLine($"{errorLevel}: NETStandard.Library {referencedNETStandardLibrary} is used for target framework {matchingTargetFramework.Key}.");
                }

                if (dependenciesUsingNETStandard.Count != 0)
                {
                    logger?.WriteLine($"{errorLevel}: Check the following dependencies for versions compatible with {targetFramework}:");
                    foreach(var dependency in dependenciesUsingNETStandard)
                    {
                        logger?.WriteLine($"{errorLevel}: \t{dependency}");
                    }
                }

                // If disable version check is true still write the warning messages 
                // but return true to continue deployment.
                return disableVersionCheck;
            }


            return true;
        }

        /// <summary>
        /// Work around issues with Microsoft.PowerShell.SDK NuGet package not working correctly when publish with a
        /// runtime switch. The nested Module folder under runtimes/unix/lib/{targetFramework}/ needs to be copied to the root of the deployment bundle.
        ///
        /// https://github.com/PowerShell/PowerShell/issues/13132
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="publishLocation"></param>
        private static void FlattenPowerShellRuntimeModules(IToolLogger logger, string publishLocation, string targetFramework)
        {
            var runtimeModuleDirectory = Path.Combine(publishLocation, $"runtimes/unix/lib/{targetFramework}/Modules");

            if (!File.Exists(Path.Combine(publishLocation, "Microsoft.PowerShell.SDK.dll")) || !Directory.Exists(runtimeModuleDirectory))
                return;

            Utilities.CopyDirectory(runtimeModuleDirectory, Path.Combine(publishLocation, "Modules"), true);
        }

        /// <summary>
        /// Process the runtime folder from the dotnet publish to flatten the platform specific dependencies to the
        /// root.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="publishLocation"></param>
        /// <param name="depsJsonTargetNode"></param>
        /// <returns>
        /// Returns true if flattening was successful. If the publishing folder changes in the future then flattening might fail. 
        /// In that case we want to publish the archive untouched so the tooling doesn't get in the way and let the user see if the  
        /// Lambda runtime has been updated to support the future changes. Warning messages will be written in case of failures.
        /// </returns>
        private static bool FlattenRuntimeFolder(IToolLogger logger, string publishLocation, JsonData depsJsonTargetNode)
        {

            bool flattenAny = false;
            // Copy file function if the file hasn't already copied.
            var copyFileIfNotExist = new Action<string>(sourceRelativePath =>
            {
                var sourceFullPath = Path.Combine(publishLocation, sourceRelativePath);
                var targetFullPath = Path.Combine(publishLocation, Path.GetFileName(sourceFullPath));

                // Skip the copy if it has already been copied.
                if (File.Exists(targetFullPath))
                    return;

                // Only write the log message about flattening if we are actually going to flatten anything.
                if(!flattenAny)
                {
                    logger?.WriteLine("Flattening platform specific dependencies");
                    flattenAny = true;
                }

                logger?.WriteLine($"... flatten: {sourceRelativePath}");
                File.Copy(sourceFullPath, targetFullPath);
            });

            var runtimeHierarchy = CalculateRuntimeHierarchy();
            // Loop through all the valid runtimes in precedence order so we copy over the first match
            foreach (var runtime in runtimeHierarchy)
            {
                foreach (KeyValuePair<string, JsonData> dependencyNode in depsJsonTargetNode)
                {
                    var depRuntimeTargets = dependencyNode.Value["runtimeTargets"];
                    if (depRuntimeTargets == null)
                        continue;

                    foreach (KeyValuePair<string, JsonData> depRuntimeTarget in depRuntimeTargets)
                    {
                        var rid = depRuntimeTarget.Value["rid"]?.ToString();

                        if(string.Equals(rid, runtime, StringComparison.Ordinal))
                        {
                            copyFileIfNotExist(depRuntimeTarget.Key);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Compute the hierarchy of runtimes to search for platform dependencies.
        /// </summary>
        /// <returns></returns>
        private static IList<string> CalculateRuntimeHierarchy()
        {
            var runtimeHierarchy = new List<string>();

            var lambdaAssembly = typeof(LambdaPackager).GetTypeInfo().Assembly;

            // The full name for the embedded resource changes between the dotnet CLI and AWS Toolkit for VS so just look for the resource by is file name.
            var manifestName = lambdaAssembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith(LambdaConstants.RUNTIME_HIERARCHY));

            using (var stream = typeof(LambdaPackager).GetTypeInfo().Assembly.GetManifestResourceStream(manifestName))
            using (var reader = new StreamReader(stream))
            {
                var rootData = JsonMapper.ToObject(reader.ReadToEnd());
                var runtimes = rootData["runtimes"];

                // Use a queue to do a breadth first search through the list of runtimes.
                var queue = new Queue<string>();
                queue.Enqueue(LambdaConstants.LEGACY_RUNTIME_HIERARCHY_STARTING_POINT);

                while(queue.Count > 0)
                {
                    var runtime = queue.Dequeue();
                    if (runtimeHierarchy.Contains(runtime))
                        continue;

                    runtimeHierarchy.Add(runtime);

                    var imports = runtimes[runtime]["#import"];
                    if (imports != null)
                    {
                        foreach (JsonData importedRuntime in imports)
                        {
                            queue.Enqueue(importedRuntime.ToString());
                        }
                    }
                }
            }

            return runtimeHierarchy;
        }


        /// <summary>
        /// Get the list of files from the publish folder that should be added to the zip archive.
        /// This will skip all files in the runtimes folder because they have already been flatten to the root.
        /// </summary>
        /// <param name="publishLocation"></param>
        /// <param name="flattenRuntime">If true the runtimes folder will be flatten</param>
        /// <returns></returns>
        private static IDictionary<string, string> GetFilesToIncludeInArchive(string publishLocation, bool flattenRuntime)
        {
            string RUNTIME_FOLDER_PREFIX = "runtimes" + Path.DirectorySeparatorChar;

            var includedFiles = new Dictionary<string, string>();
            var allFiles = Directory.GetFiles(publishLocation, "*.*", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var relativePath = file.Substring(publishLocation.Length);
                if (relativePath[0] == Path.DirectorySeparatorChar)
                    relativePath = relativePath.Substring(1);

                if (flattenRuntime && relativePath.StartsWith(RUNTIME_FOLDER_PREFIX))
                    continue;

                // Native debug symbols are very large and are being excluded to keep deployment size down.
                if (relativePath.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase)) 
                    continue;

                includedFiles[relativePath] = file;
            }

            return includedFiles;
        }

        /// <summary>
        /// Zip up the publish folder using the build-lambda-zip utility which will maintain linux/osx file permissions.
        /// This is what is used when run on Windows.
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="publishLocation">The location to be bundled.</param>
        /// <param name="flattenRuntime">If true the runtimes folder will be flatten</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithBuildLambdaZip(string zipArchivePath, string publishLocation, bool flattenRuntime, IToolLogger logger)
        {
            var includedFiles = GetFilesToIncludeInArchive(publishLocation, flattenRuntime);
            BundleWithBuildLambdaZip(zipArchivePath, publishLocation, includedFiles, logger);
        }

        /// <summary>
        /// Zip up the publish folder using the build-lambda-zip utility which will maintain linux/osx file permissions.
        /// This is what is used when run on Windows.
        /// </summary>
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="rootDirectory">The root directory where all of the relative paths in includedFiles is pointing to.</param>
        /// <param name="includedFiles">Map of relative to absolute path of files to include in bundle.</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithBuildLambdaZip(string zipArchivePath, string rootDirectory, IDictionary<string, string> includedFiles, IToolLogger logger)
        {               
            if (!File.Exists(BuildLambdaZipCliPath))
            {
                throw new LambdaToolsException("Failed to find the \"build-lambda-zip\" utility. This program is required to maintain Linux file permissions in the zip archive.", LambdaToolsException.LambdaErrorCode.FailedToFindZipProgram);
            }
            
            EnsureBootstrapLinuxLineEndings(rootDirectory, includedFiles);
                        
            //Write the files to disk to avoid the command line size limit when we have a large number of files to zip.            
            var inputFilename = zipArchivePath + ".txt";
            using(var writer = new StreamWriter(inputFilename))
            {                            
                foreach (var kvp in includedFiles)
                {
                    writer.WriteLine(kvp.Key);                    
                }
            }

            var args = new StringBuilder($"-o \"{zipArchivePath}\" -i \"{inputFilename}\"");

            var psiZip = new ProcessStartInfo
            {
                FileName = BuildLambdaZipCliPath,
                Arguments = args.ToString(),
                WorkingDirectory = rootDirectory,
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

            try
            {
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
            finally
            {
                try
                {
                    File.Delete(inputFilename);
                }
                catch (Exception e)
                {
                    logger?.WriteLine($"Warning: Unable to delete temporary input file, {inputFilename}, after zipping files: {e.Message}");
                }
            }                        
        }

        /// <summary>
        /// Detects if there is a bootstrap file, and if it's a script (as opposed to an actual executable),
        /// and corrects the line endings so it can be run in Linux.
        /// 
        /// TODO: possibly expand to allow files other than bootstrap to be corrected
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="includedFiles"></param>
        private static void EnsureBootstrapLinuxLineEndings(string rootDirectory, IDictionary<string, string> includedFiles)
        {
            if (includedFiles.ContainsKey(BootstrapFilename))
            {
                var bootstrapPath = Path.Combine(rootDirectory, BootstrapFilename);
                if (FileIsLinuxShellScript(bootstrapPath))
                {
                    var lines = File.ReadAllLines(bootstrapPath);
                    using (var sw = File.CreateText(bootstrapPath))
                    {
                        foreach (var line in lines)
                        {
                            sw.Write(line);
                            sw.Write(LinuxLineEnding);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the first characters of the file are #!, false otherwise.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static bool FileIsLinuxShellScript(string filePath)
        {
            using (var sr = File.OpenText(filePath))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine().Trim();
                    if (line.Length > 0)
                    {
                        return line.StartsWith(Shebang);
                    }
                }
            }
            return false;
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
        private static void BundleWithZipCLI(string zipCLI, string zipArchivePath, string publishLocation, bool flattenRuntime, IToolLogger logger)
        {
            var allFiles = GetFilesToIncludeInArchive(publishLocation, flattenRuntime);
            BundleWithZipCLI(zipCLI, zipArchivePath, publishLocation, allFiles, logger);
        }

        /// <summary>
        /// Creates the deployment bundle using the native zip tool installed
        /// on the system (default /usr/bin/zip). This is what is typically used on Linux and OSX
        /// </summary>
        /// <param name="zipCLI">The path to the located zip binary.</param>
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="rootDirectory">The root directory where all of the relative paths in includedFiles is pointing to.</param>
        /// <param name="includedFiles">Map of relative to absolute path of files to include in bundle.</param>
        /// <param name="logger">Logger instance.</param>
        private static void BundleWithZipCLI(string zipCLI, string zipArchivePath, string rootDirectory, IDictionary<string, string> includedFiles, IToolLogger logger)
        {
            EnsureBootstrapLinuxLineEndings(rootDirectory, includedFiles);
            
            var args = new StringBuilder("\"" + zipArchivePath + "\"");

            foreach (var kvp in includedFiles)
            {
                args.AppendFormat(" \"{0}\"", kvp.Key);
            }

            var psiZip = new ProcessStartInfo
            {
                FileName = zipCLI,
                Arguments = args.ToString(),
                WorkingDirectory = rootDirectory,
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

        internal static void LogDeprecationMessagesIfNecessary(IToolLogger logger, string targetFramework)
        {
            if (targetFramework == "netcoreapp2.0" && logger != null)
            {
                logger.WriteLine("--------------------------------------------------------------------------------");
                logger.WriteLine(".NET Core 2.0 Lambda Function Deprecation Notice");
                logger.WriteLine("--------------------------------------------------------------------------------");
                logger.WriteLine("Support for .NET Core 2.0 Lambda functions will soon be deprecated.");
                logger.WriteLine("");
                logger.WriteLine("Support for .NET Core 2.0 was discontinued by Microsoft in October 2018.  This");
                logger.WriteLine("version of the runtime is no longer receiving bug fixes or security updates from");
                logger.WriteLine("Microsoft.  AWS Lambda has discontinued updates to this runtime as well.");
                logger.WriteLine("");
                logger.WriteLine("You can find Lambda's runtime support policy here:");
                logger.WriteLine("https://docs.aws.amazon.com/lambda/latest/dg/runtime-support-policy.html");
                logger.WriteLine("");
                logger.WriteLine("You will notice an initial change 30 days prior to the deprecation.");
                logger.WriteLine("During the 30 day grace period you will be able to update existing .NET Core 2.0");
                logger.WriteLine("Lambda functions but you will not be able to create new ones.  After the");
                logger.WriteLine("deprecation has been finalized you will be unable to create or update .NET Core");
                logger.WriteLine("2.0 functions.");
                logger.WriteLine("");
                logger.WriteLine("However, both during and after the grace period you WILL be able to invoke .NET ");
                logger.WriteLine("Core 2.0 functions.  Existing .NET Core 2.0 function invocation will continue to");
                logger.WriteLine("be available, subject to Lambda's runtime support policy.");
                logger.WriteLine("");
                logger.WriteLine("--------------------------------------------------------------------------------");

            }
        }
    }
}
