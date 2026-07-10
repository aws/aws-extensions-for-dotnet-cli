// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Amazon.Common.DotNetCli.Tools
{
    /// <summary>
    /// Creates deployment zip archives using the built-in .NET compression libraries while still
    /// producing entries that Linux (and therefore AWS Lambda) will extract with executable file
    /// permissions.
    ///
    /// The .NET zip libraries write the archive's central directory records with a "version made by"
    /// host system of MS-DOS/FAT when running on Windows. When such an archive is extracted on Linux
    /// the Unix permission bits stored in the external attributes are ignored, which strips the execute
    /// bit off files like the custom runtime "bootstrap". Historically this repository worked around the
    /// limitation by shelling out to the native "zip" program on Linux/macOS and to a vendored Go
    /// utility (build-lambda-zip.exe) on Windows.
    ///
    /// This class removes both of those external dependencies. It writes the archive with
    /// System.IO.Compression and then rewrites each central directory record so that the host system is
    /// reported as Unix and the external attributes carry a 0777 (rwxrwxrwx) file mode. This mirrors the
    /// behavior of the old build-lambda-zip utility, which unconditionally marked every entry as an
    /// executable Unix file, so the resulting archives behave identically on Lambda.
    /// </summary>
    public static class ManagedZipArchive
    {
        // Central directory file header signature: "PK\x01\x02".
        private static readonly byte[] CentralDirectorySignature = { 0x50, 0x4b, 0x01, 0x02 };

        // End of central directory record signature: "PK\x05\x06".
        private static readonly byte[] EndOfCentralDirectorySignature = { 0x50, 0x4b, 0x05, 0x06 };

        // The end of central directory record is 22 bytes plus a variable length comment.
        private const int EndOfCentralDirectoryMinSize = 22;

        // Offset of the central directory start (relative to the EOCD signature).
        private const int CentralDirectoryOffsetOffset = 16;

        // Host system value stored in the upper byte of "version made by". 3 == Unix.
        private const byte UnixHostSystem = 3;

        // Regular file (S_IFREG, 0100000 octal) with rwxrwxrwx (0777 octal) permissions. This matches
        // the permissions the previous build-lambda-zip utility applied to every entry.
        private const uint UnixRegularFileMode = 0x81FF;

        // Offsets within a central directory file header (relative to the signature).
        private const int VersionMadeByHostOffset = 5;
        private const int ExternalAttributesOffset = 38;
        private const int FileNameLengthOffset = 28;
        private const int ExtraFieldLengthOffset = 30;
        private const int CommentLengthOffset = 32;
        private const int CentralDirectoryHeaderSize = 46;

        /// <summary>
        /// Bundle the provided files into a zip archive that preserves Linux execute permissions.
        /// </summary>
        /// <param name="zipArchivePath">The path and name of the zip archive to create.</param>
        /// <param name="rootDirectory">The root directory the relative paths in <paramref name="includedFiles"/> are relative to. Included for API symmetry; the values in <paramref name="includedFiles"/> are the absolute source paths that are read.</param>
        /// <param name="includedFiles">Map of relative path (as stored in the archive) to absolute source path.</param>
        /// <param name="logger">Logger instance.</param>
        public static void BundleFiles(string zipArchivePath, string rootDirectory, IDictionary<string, string> includedFiles, IToolLogger logger)
        {
            using (var fileStream = new FileStream(zipArchivePath, FileMode.Create, FileAccess.ReadWrite))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                foreach (var kvp in includedFiles)
                {
                    // Normalize to forward slashes so the archive uses Linux-style paths.
                    var entryName = kvp.Key.Replace('\\', '/');
                    var entry = zipArchive.CreateEntry(entryName, CompressionLevel.Optimal);

                    using (var entryStream = entry.Open())
                    using (var sourceStream = File.OpenRead(kvp.Value))
                    {
                        sourceStream.CopyTo(entryStream);
                    }

                    logger?.WriteLine($"... zipping: {entryName}");
                }
            }

            ApplyUnixPermissions(zipArchivePath);

            logger?.WriteLine(string.Format("Created publish archive ({0}).", zipArchivePath));
        }

        /// <summary>
        /// Rewrite every central directory record in the archive so the host system is reported as Unix
        /// and the external attributes carry an executable Unix file mode. Extracting on Linux then
        /// honors the permission bits, which the .NET compression libraries would otherwise cause to be
        /// ignored on archives authored on Windows.
        /// </summary>
        /// <param name="zipArchivePath">The path to the archive to patch.</param>
        private static void ApplyUnixPermissions(string zipArchivePath)
        {
            var bytes = File.ReadAllBytes(zipArchivePath);

            var position = FindFirstCentralDirectoryHeader(bytes);
            if (position < 0)
            {
                // An empty archive has no central directory file headers; nothing to patch.
                return;
            }

            while (position + CentralDirectoryHeaderSize <= bytes.Length && IsCentralDirectoryHeader(bytes, position))
            {
                // Set the host system portion of "version made by" to Unix.
                bytes[position + VersionMadeByHostOffset] = UnixHostSystem;

                // Set the external attributes to an executable Unix file mode (little-endian).
                var attributes = UnixRegularFileMode << 16;
                bytes[position + ExternalAttributesOffset] = (byte)attributes;
                bytes[position + ExternalAttributesOffset + 1] = (byte)(attributes >> 8);
                bytes[position + ExternalAttributesOffset + 2] = (byte)(attributes >> 16);
                bytes[position + ExternalAttributesOffset + 3] = (byte)(attributes >> 24);

                var fileNameLength = BitConverter.ToUInt16(bytes, position + FileNameLengthOffset);
                var extraFieldLength = BitConverter.ToUInt16(bytes, position + ExtraFieldLengthOffset);
                var commentLength = BitConverter.ToUInt16(bytes, position + CommentLengthOffset);

                position += CentralDirectoryHeaderSize + fileNameLength + extraFieldLength + commentLength;
            }

            File.WriteAllBytes(zipArchivePath, bytes);
        }

        /// <summary>
        /// Locate the start of the central directory by reading the offset recorded in the end of
        /// central directory (EOCD) record. Using the recorded offset (rather than scanning for the
        /// first "PK\x01\x02" byte sequence) avoids false matches on file content that happens to
        /// contain the signature bytes.
        /// </summary>
        private static int FindFirstCentralDirectoryHeader(byte[] bytes)
        {
            // Scan backwards for the EOCD signature. It lives near the end of the file, after any
            // (typically empty) archive comment.
            for (var i = bytes.Length - EndOfCentralDirectoryMinSize; i >= 0; i--)
            {
                if (bytes[i] == EndOfCentralDirectorySignature[0]
                    && bytes[i + 1] == EndOfCentralDirectorySignature[1]
                    && bytes[i + 2] == EndOfCentralDirectorySignature[2]
                    && bytes[i + 3] == EndOfCentralDirectorySignature[3])
                {
                    var centralDirectoryOffset = BitConverter.ToUInt32(bytes, i + CentralDirectoryOffsetOffset);
                    if (centralDirectoryOffset <= bytes.Length - CentralDirectorySignature.Length
                        && IsCentralDirectoryHeader(bytes, (int)centralDirectoryOffset))
                    {
                        return (int)centralDirectoryOffset;
                    }

                    return -1;
                }
            }

            return -1;
        }

        private static bool IsCentralDirectoryHeader(byte[] bytes, int offset)
        {
            return bytes[offset] == CentralDirectorySignature[0]
                && bytes[offset + 1] == CentralDirectorySignature[1]
                && bytes[offset + 2] == CentralDirectorySignature[2]
                && bytes[offset + 3] == CentralDirectorySignature[3];
        }
    }
}
