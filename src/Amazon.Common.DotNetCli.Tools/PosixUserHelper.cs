using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Amazon.Common.DotNetCli.Tools
{
    public struct PosixUserInfo
    {
        public uint UserID;
        public uint GroupID;
    }

    /// <summary>
    /// Helper class to retrieve current Linux/Mac user and group information
    /// </summary>
    public static class PosixUserHelper
    {
        /// <summary>
        /// Return True if running under Unix or Mac
        /// </summary>
        public static readonly bool IsRunningInPosix = Environment.OSVersion.Platform == PlatformID.Unix
                                                       || Environment.OSVersion.Platform == PlatformID.MacOSX;
        
        /// <summary>
        /// Return the effective user's UID and GID under Linux and Mac by calling the "id" command.
        /// This will fault if running on Windows (by design), check IsRunningInPosix before calling this method.
        /// </summary>
        /// <returns>PoxUserInfo struct with UID and GID</returns>
        /// <exception cref="Exception"></exception>
        public static PosixUserInfo GetEffectiveUser(IToolLogger logger)
        {
            var userID = uint.MaxValue;
            var groupID = uint.MaxValue;

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

                userID = uint.Parse(results.Groups[1].Value);
                groupID = uint.Parse(results.Groups[2].Value);
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

            if (userID == uint.MaxValue)
            {
                throw new Exception("Unable to get effective user from \"id\"");
            }

            if (groupID == uint.MaxValue)
            {
                throw new Exception("Unable to get effective group from \"id\"");
            }

            return new PosixUserInfo
            {
                UserID = userID,
                GroupID = groupID
            };
        }
    }        
}