﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// Wrapper around the dotnet cli used to execute the publish command.
    /// </summary>
    public class DotNetCLIWrapper : AbstractCLIWrapper
    {
        public DotNetCLIWrapper(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
        }

        /// <summary>
        /// Generates deployment manifest for staged content
        /// </summary>
        /// <param name="projectLocation"></param>
        /// <param name="outputLocation"></param>
        /// <param name="targetFramework"></param>
        /// <param name="configuration"></param>
        public int Publish(string projectLocation, string outputLocation, string targetFramework, string configuration, string additionalPublishOptions)
        {
            if (Directory.Exists(outputLocation))
            {
                try
                {
                    Directory.Delete(outputLocation, true);
                    _logger?.WriteLine("Deleted previous publish folder");
                }
                catch (Exception e)
                {
                    _logger?.WriteLine($"Warning unable to delete previous publish folder: {e.Message}");
                }
            }

            _logger?.WriteLine($"... invoking 'dotnet publish'");

            var dotnetCLI = FindExecutableInPath("dotnet.exe");
            if (dotnetCLI == null)
                dotnetCLI = FindExecutableInPath("dotnet");
            if (string.IsNullOrEmpty(dotnetCLI))
                throw new Exception("Failed to locate dotnet CLI executable. Make sure the dotnet CLI is installed in the environment PATH.");

            StringBuilder arguments = new StringBuilder("publish");
            if (!string.IsNullOrEmpty(projectLocation))
            {
                arguments.Append($" \"{Utilities.DetermineProjectLocation(this._workingDirectory, projectLocation)}\"");
            }
            if (!string.IsNullOrEmpty(outputLocation))
            {
                arguments.Append($" --output \"{outputLocation}\"");
            }

            if (!string.IsNullOrEmpty(configuration))
            {
                arguments.Append($" --configuration \"{configuration}\"");
            }

            if (!string.IsNullOrEmpty(targetFramework))
            {
                arguments.Append($" --framework \"{targetFramework}\"");
            }

            if (!string.IsNullOrEmpty(additionalPublishOptions))
            {
                arguments.Append($" {additionalPublishOptions}");
            }


            var psi = new ProcessStartInfo
            {
                FileName = dotnetCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            int exitCode = base.ExecuteCommand(psi, "dotnet publish");
            if (exitCode != 0)
                return exitCode;

            var chmodPath = FindExecutableInPath("chmod");
            if (!string.IsNullOrEmpty(chmodPath) && File.Exists(chmodPath))
            {
                // as we are not invoking through a shell, which would handle
                // wildcard expansion for us, we need to invoke per-file
                var dllFiles = Directory.GetFiles(outputLocation, "*.dll", SearchOption.TopDirectoryOnly);
                foreach (var dllFile in dllFiles)
                {
                    var dllFilename = Path.GetFileName(dllFile);
                    var psiChmod = new ProcessStartInfo
                    {
                        FileName = chmodPath,
                        Arguments = "+r \"" + dllFilename + "\"",
                        WorkingDirectory = outputLocation,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    if(base.ExecuteCommand(psiChmod, "dotnet publish") == 0)
                    {
                        this._logger?.WriteLine($"Changed permissions on published dll (chmod +r {dllFilename}).");
                    }
                }
            }

            return 0;
        }

        public static Version GetSdkVersion()
        {
            var dotnetCLI = FindExecutableInPath("dotnet.exe");
            if (dotnetCLI == null)
                dotnetCLI = FindExecutableInPath("dotnet");
            if (string.IsNullOrEmpty(dotnetCLI))
                throw new Exception("Failed to locate dotnet CLI executable. Make sure the dotnet CLI is installed in the environment PATH.");

            var results = Utilities.ExecuteShellCommand(null, dotnetCLI, "--list-sdks");
            if(results.Exitcode != 0)
                throw new Exception("Command \"dotnet --list-sdks\" failed, captured output: \n" + results.Stdout);
            

            var maxSdkVersion = ParseListSdkOutput(results.Stdout);
            if (maxSdkVersion == null)
            {
                throw new Exception("Failed to parse latest SDK version from captured output:\n" + results.Stdout);                
            }

            return maxSdkVersion;
        }

        public static Version ParseListSdkOutput(string listSdkOutput)
        {
            var outputLines = listSdkOutput.Split('\n');
            for (int i = outputLines.Length - 1; i >= 0; i--)
            {
                var line = outputLines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                var tokens = line.Split(' ');
                // There should be at least 2 tokens, the version and the path to the SDK. There might be more than 2 tokens if the path to the SDK contained spaces.
                if (tokens.Length < 2)
                    continue;

                if(Version.TryParse(tokens[0], out var version))
                {
                    return version;
                }
            }

            return null;
        }
    }
}