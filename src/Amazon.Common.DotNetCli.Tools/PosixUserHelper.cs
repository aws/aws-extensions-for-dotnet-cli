using System.Diagnostics;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.InteropServices;
#endif

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
        /// Return True if running under Unix or Mac, requires running on .NET Core 3.1 or greater
        /// </summary>
        public static readonly bool IsRunningInPosix = 
        #if NETCOREAPP3_1_OR_GREATER
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
            ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        #else
            false;
        #endif
        
        /// <summary>
        /// Return the effective user's UID and GID under Linux and Mac by calling the "id" command.
        /// This will fault if running on Windows (by design), check IsRunningInPosix before calling this method.
        /// </summary>
        /// <returns>PosixUserInfo struct with UID and GID, or NULL if not detected</returns>
        public static PosixUserInfo? GetEffectiveUser(IToolLogger logger, IProcessFactory processFactory = null)
        {
            processFactory ??= ProcessFactory.Default;
            
            var values = new int?[] {null, null};

            // Call `id` twice, once to get the user,
            // once to get the group
            for(var loop = 0; loop < values.Length; loop++)
            {
                var arg = loop == 0 ? "-u" : "-g";
                var results = processFactory.RunProcess(new ProcessStartInfo
                {
                    FileName = "id",
                    Arguments = arg
                });

                if (!results.Executed)
                {
                    logger?.WriteLine($"Error executing \"id {arg}\" {results.Error}");
                }
                else if (results.ExitCode != 0)
                {
                    logger?.WriteLine($"Error executing \"id {arg}\" - exit code {results.ExitCode} {results.Error}");
                } 
                else if (! int.TryParse(results.Output, out var value))
                {
                    logger?.WriteLine($"Error parsing output \"id {arg}\" (\"{results.Output}\")");
                }
                else
                {
                    values[loop] = value;
                }
            }

            if (! values[0].HasValue)
            {
                logger?.WriteLine("Warning: Unable to get effective user from \"id -u\"");
                return null;
            }

            if (! values[1].HasValue)
            {
                logger?.WriteLine("Warning: Unable to get effective group from \"id -g\"");
                return null;
            }

            return new PosixUserInfo
            {
                UserID = values[0].Value,
                GroupID = values[1].Value
            };
        }
    }        
}