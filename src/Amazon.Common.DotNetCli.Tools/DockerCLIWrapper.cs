using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools
{
    public class DockerCLIWrapper : AbstractCLIWrapper
    {
        string _dockerCLI;

        public DockerCLIWrapper(IToolLogger logger, string workingDirectory)
            : base(logger, workingDirectory)
        {
            this._dockerCLI = FindExecutableInPath("docker.exe");
            if (this._dockerCLI == null)
                this._dockerCLI = FindExecutableInPath("docker");
            if (string.IsNullOrEmpty(this._dockerCLI))
                throw new Exception("Failed to locate docker CLI executable. Make sure the docker CLI is installed in the environment PATH.");
        }

        public int Build(string workingDirectory, string dockerFile, string imageTag, string additionalBuildOptions, bool arm64Build = false)
        {
            _logger?.WriteLine($"... invoking 'docker build', working folder '{workingDirectory}, docker file {dockerFile}, image name {imageTag}'");

            var arguments = new StringBuilder();

#if NETCOREAPP3_1_OR_GREATER
            var runningOnLinuxArm64 = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
#else
            var runningOnLinuxArm64 = false;
#endif
            if (arm64Build && !runningOnLinuxArm64)
            {
                _logger?.WriteLine("The docker CLI \"buildx\" command is used to build ARM64 images. This requires version 20 or later of the docker CLI.");
                arguments.Append($"buildx build --platform linux/arm64 ");
            }
            else
            {
                arguments.Append($"build ");
            }

            arguments.Append($" -f \"{dockerFile}\" -t {imageTag}");

            if(!string.IsNullOrEmpty(additionalBuildOptions))
            {
                arguments.Append($" {additionalBuildOptions}");
            }

            arguments.Append(" .");

            _logger?.WriteLine($"... docker {arguments.ToString()}");

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            return base.ExecuteCommand(psi, "docker build");
        }

        public string GetImageId(string imageTag)
        {
            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                // Make sure to have the space after the "{{.ID}}" and the closing quote.
                Arguments = "images --format \"{{.ID}}\" " + imageTag,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            var imageId = process.StandardOutput.ReadToEnd()?.Trim();

            // To prevent a unlikely hang give 10 seconds as max for waiting for the docker CLI to execute and return back the image id.
            process.WaitForExit(10000);

            if (imageId.Length != 12)
                return null;

            return imageId;
        }

        public int Login(string username, string password, string proxy)
        {
            _logger?.WriteLine($"... invoking 'docker login'");

            var arguments = $"login --username {username} --password {password} {proxy}";

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            return base.ExecuteCommand(psi, "docker login");
        }

        public int Tag(string sourceTagName, string targetTagName)
        {
            _logger?.WriteLine($"... invoking 'docker tag'");

            var arguments = $"tag {sourceTagName} {targetTagName}";

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            return base.ExecuteCommand(psi, "docker tag");
        }

        public int Push(string targetTagName)
        {
            _logger?.WriteLine($"... invoking 'docker push'");

            var arguments = $"push {targetTagName}";

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory
            };


            return base.ExecuteCommand(psi, "docker push");
        }

        public int Run(string imageId, string containerName)
        {
            _logger?.WriteLine($"... invoking 'docker run --name {containerName} {imageId}'");

            var arguments = $"run --name {containerName} {imageId}";

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory
            };


            return base.ExecuteCommand(psi, "docker run");
        }

        public int Copy(string containerName, string internalContainerPathToExtract, string outputPath)
        {
            _logger?.WriteLine($"... invoking 'docker cp {containerName}:{internalContainerPathToExtract} .'");

            var arguments = $"cp {containerName}:{internalContainerPathToExtract} {outputPath}";

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments.ToString(),
                WorkingDirectory = this._workingDirectory
            };


            return base.ExecuteCommand(psi, "docker cp");
        }
    }
}
