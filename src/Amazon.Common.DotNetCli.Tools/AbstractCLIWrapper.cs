using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

#if NETCORE
using System.Runtime.InteropServices;
#endif

namespace Amazon.Common.DotNetCli.Tools
{
    public abstract class AbstractCLIWrapper
    {
        protected string _workingDirectory;
        protected IToolLogger _logger;

        public AbstractCLIWrapper(IToolLogger logger, string workingDirectory)
        {
            this._logger = logger;
            this._workingDirectory = workingDirectory;
        }

        protected int ExecuteCommand(ProcessStartInfo startInfo, string loggerLabel)
        {


            var handler = (DataReceivedEventHandler)((o, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;
                _logger?.WriteLine($"... {loggerLabel}: " + e.Data);
            });

            int exitCode;
            using (var proc = new Process())
            {
                proc.StartInfo = startInfo;
                proc.Start();

                if(startInfo.RedirectStandardOutput)
                {
                    proc.ErrorDataReceived += handler;
                    proc.OutputDataReceived += handler;
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();

                    proc.EnableRaisingEvents = true;
                }

                proc.WaitForExit();

                exitCode = proc.ExitCode;
            }

            return exitCode;
        }

        /// <summary>
        /// A collection of known paths for common utilities that are usually not found in the path
        /// </summary>
        static readonly IDictionary<string, string> KNOWN_LOCATIONS = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"dotnet.exe", @"C:\Program Files\dotnet\dotnet.exe" },
            {"chmod", @"/bin/chmod" },
            {"zip", @"/usr/bin/zip" },
            {"docker.exe", @"C:\Program Files\Docker\Docker\Resources\bin\docker.exe" }
        };

        /// <summary>
        /// Search the path environment variable for the command given.
        /// </summary>
        /// <param name="command">The command to search for in the path</param>
        /// <returns>The full path to the command if found otherwise it will return null</returns>
        public static string FindExecutableInPath(string command)
        {

            if (File.Exists(command))
                return Path.GetFullPath(command);

#if NETCORE
            if (string.Equals(command, "dotnet.exe"))
            {
                if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    command = "dotnet";
                }

                var mainModule = Process.GetCurrentProcess().MainModule;
                if (!string.IsNullOrEmpty(mainModule?.FileName)
                    && Path.GetFileName(mainModule.FileName).Equals(command, StringComparison.OrdinalIgnoreCase))
                {
                    return mainModule.FileName;
                }
            }
#endif

            Func<string, string> quoteRemover = x =>
            {
                if (x.StartsWith("\""))
                    x = x.Substring(1);
                if (x.EndsWith("\""))
                    x = x.Substring(0, x.Length - 1);
                return x;
            };

            var envPath = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in envPath.Split(Path.PathSeparator))
            {
                try
                {
                    var fullPath = Path.Combine(quoteRemover(path), command);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                catch (Exception)
                {
                    // Catch exceptions and continue if there are invalid characters in the user's path.
                }
            }

            if (KNOWN_LOCATIONS.ContainsKey(command) && File.Exists(KNOWN_LOCATIONS[command]))
                return KNOWN_LOCATIONS[command];

            return null;
        }
    }
}
