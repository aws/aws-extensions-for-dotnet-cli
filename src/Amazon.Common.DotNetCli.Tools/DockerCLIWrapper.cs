using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Amazon.Common.DotNetCli.Tools
{
    public class DockerCLIWrapper : AbstractCLIWrapper
    {
        public static readonly string WorkingDirectoryMountLocation = "/tmp/source/";

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

        /// <summary>
        /// Executes the Docker run command on the given image locally
        /// </summary>
        /// <param name="imageId">Tells Docker which pre-built image to run. Can be local or remote.</param>
        /// <param name="containerName">A custom name to give the generated container. This is useful so the caller of this method knows what container to look for after.</param>
        /// <param name="commandToRun">Tells Docker what commands to invoke on the container once it is running. Can be left blank.</param>
        /// <returns></returns>
        public int Run(string imageId, string containerName, string commandToRun = "")
        {
            var argumentList = new List<string>()
            {
                "run",
                "--name",
                containerName,

                // Automatically remove the container once it's done running
                "--rm",

                // This allows the container access to the working directory in a virtual mapped path located at /tmp/source
                // That means that when the container is finished running, anything it leaves in /tmp/source (e.g. the binaries),
                // will just exist in the working directory
                "--volume",
                $"\"{this._workingDirectory}\":{WorkingDirectoryMountLocation}",

                "-i"
            };
            
            // For Linux or MacOS, running a .NET image as non-root requires some special
            // handling.  We have to pass the user's UID and GID, but we also have to specify
            // the location of where .NET and Nuget put their files, because by default, they
            // go to the /.dotnet and /.local, which a non-root user can't write to in a container
            if (PosixUserHelper.IsRunningInPosix)
            {
                var posixUser = PosixUserHelper.GetEffectiveUser(_logger);
                if (posixUser != null && (posixUser?.UserID > 0 || posixUser.Value.GroupID > 0))
                {
                    argumentList.AddRange(new []
                    {
                        // Set Docker user and group IDs
                        "-u",
                        $"{posixUser.Value.UserID}:{posixUser.Value.GroupID}",
                        // Set .NET CLI home directory
                        "-e",
                        "DOTNET_CLI_HOME=/tmp/dotnet",
                        // Set NuGet data home directory to non-root directory
                        "-e",
                        "XDG_DATA_HOME=/tmp/xdg"
                    });
                }
            }

            argumentList.Add(imageId);
            
            var arguments = string.Join(" ", argumentList);
            if (!string.IsNullOrEmpty(commandToRun))
            {
                arguments += $" {commandToRun}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = this._dockerCLI,
                Arguments = arguments,
                WorkingDirectory = this._workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger?.WriteLine($"... invoking 'docker {arguments}' from directory {this._workingDirectory}");

            return base.ExecuteCommand(psi, "docker run");
        }
    }
}
