using Amazon.Common.DotNetCli.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Amazon.ECS.Tools
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

        public int Build(ECSToolsDefaults defaults, string workingDirectory, string dockerFile, string imageTag, string additionalBuildOptions)
        {
            _logger?.WriteLine($"... invoking 'docker build', working folder '{workingDirectory}, docker file {dockerFile}, image name {imageTag}'");


            StringBuilder arguments = new StringBuilder($"build -f \"{dockerFile}\" -t {imageTag}");

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
    }
}
