using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Amazon.Common.DotNetCli.Tools
{
    public struct PosixUserInfo
    {
        public int UserID;
        public int GroupID;
        public bool UserIDSet;
        public bool GroupIDSet;
    }

    /// <summary>
    /// Helper class to retrieve current Linux/Mac user and group information
    /// </summary>
    public static class PosixUserHelper
    {
        /// <summary>
        /// Return True if running under Unix or Mac
        /// </summary>
        public static readonly bool IsRunningInPosix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                                                       RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Return the effective user's UID and GID under Linux and Mac by calling the "id" command.
        /// This will fault if running on Windows (by design), check IsRunningInPosix before calling this method.
        /// </summary>
        /// <returns>PoxUserInfo struct with UID and GID</returns>
        /// <exception cref="Exception"></exception>
        public static PosixUserInfo GetEffectiveUser(IToolLogger logger)
        {
            var userID = 0;
            var groupID = 0;

            var userIDSet = false;
            var groupIDSet = false;

            using var proc = new Process();
            proc.StartInfo = new ProcessStartInfo
            {
                FileName = "id",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            proc.EnableRaisingEvents = true;
            proc.OutputDataReceived += (_, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                var results = Regex.Match(e.Data, @"uid=(\d+).*gid=(\d+)");
                if (results.Groups.Count != 3)
                {
                    throw new Exception($"\"id\" returned unexpected results (\"{e.Data}\")");
                }

                if (int.TryParse(results.Groups[1].Value, out var id1)) {
                    userID = id1;
                    userIDSet = true;
                }

                if (int.TryParse(results.Groups[2].Value, out var id2)) {
                    groupID = id2;
                    groupIDSet = true;
                }
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (! string.IsNullOrEmpty(e.Data))
                    logger.WriteLine($"[\"id\"]: {e.Data}");
            };
            if (!proc.Start())
            {
                throw new Exception("Unable to launch \"id\"");
            }

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception($"\"id\" exited with status {proc.ExitCode}");
            }

            if (! userIDSet)
            {
                logger?.WriteLine("Warning: Unable to get effective user from \"id\" - using root(0)");
            }

            if (! groupIDSet)
            {
                logger?.WriteLine("Warning: Unable to get effective group from \"id\" - using root(0)");
            }

            return new PosixUserInfo
            {
                UserID = userID,
                GroupID = groupID,
                UserIDSet = userIDSet,
                GroupIDSet = groupIDSet
            };
        }
    }        
}