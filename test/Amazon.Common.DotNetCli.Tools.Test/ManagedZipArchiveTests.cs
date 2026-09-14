// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace Amazon.Common.DotNetCli.Tools.Test;

public class ManagedZipArchiveTests
{
    // Host system value stored in the upper byte of "version made by". 3 == Unix.
    private const byte UnixHostSystem = 3;

    // Offsets within a central directory file header (relative to the signature).
    private const int VersionMadeByHostOffset = 5;
    private const int ExternalAttributesOffset = 38;
    private const int FileNameLengthOffset = 28;
    private const int ExtraFieldLengthOffset = 30;
    private const int CommentLengthOffset = 32;
    private const int CentralDirectoryHeaderSize = 46;

    [Fact]
    public void BundleFiles_ProducesReadableArchiveWithExpectedContent()
    {
        var sourceDirectory = CreateTempDirectory();
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            File.WriteAllText(Path.Combine(sourceDirectory, "bootstrap"), "#!/bin/sh\necho hi\n");
            Directory.CreateDirectory(Path.Combine(sourceDirectory, "nested"));
            File.WriteAllText(Path.Combine(sourceDirectory, "nested", "lib.dll"), "binary-content");

            var includedFiles = new Dictionary<string, string>
            {
                ["bootstrap"] = Path.Combine(sourceDirectory, "bootstrap"),
                ["nested/lib.dll"] = Path.Combine(sourceDirectory, "nested", "lib.dll"),
            };

            ManagedZipArchive.BundleFiles(zipPath, sourceDirectory, includedFiles, null);

            Assert.True(File.Exists(zipPath));

            using var archive = ZipFile.OpenRead(zipPath);
            Assert.NotNull(archive.GetEntry("bootstrap"));
            Assert.NotNull(archive.GetEntry("nested/lib.dll"));

            using var reader = new StreamReader(archive.GetEntry("bootstrap").Open());
            Assert.Equal("#!/bin/sh\necho hi\n", reader.ReadToEnd());
        }
        finally
        {
            Cleanup(sourceDirectory, zipPath);
        }
    }

    [Fact]
    public void BundleFiles_MarksEntriesWithUnixExecutablePermissions()
    {
        var sourceDirectory = CreateTempDirectory();
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            File.WriteAllText(Path.Combine(sourceDirectory, "bootstrap"), "#!/bin/sh\n");
            File.WriteAllText(Path.Combine(sourceDirectory, "function.dll"), "content");

            var includedFiles = new Dictionary<string, string>
            {
                ["bootstrap"] = Path.Combine(sourceDirectory, "bootstrap"),
                ["function.dll"] = Path.Combine(sourceDirectory, "function.dll"),
            };

            ManagedZipArchive.BundleFiles(zipPath, sourceDirectory, includedFiles, null);

            // Regardless of the OS the tool runs on, every central directory record must report the
            // Unix host system and carry an executable (0777) file mode so AWS Lambda extracts the
            // files with the correct permissions.
            var records = ReadCentralDirectoryRecords(zipPath);
            Assert.Equal(2, records.Count);
            foreach (var record in records)
            {
                Assert.Equal(UnixHostSystem, record.HostSystem);
                Assert.Equal(0x1FFu, record.UnixMode & 0x1FFu); // low 9 bits == rwxrwxrwx
            }
        }
        finally
        {
            Cleanup(sourceDirectory, zipPath);
        }
    }

    [Fact]
    public void BundleFiles_HandlesFileContentContainingCentralDirectorySignature()
    {
        // A file whose bytes contain the "PK\x01\x02" central directory signature must not confuse
        // the permission-patching logic, which locates the central directory via the end of central
        // directory record rather than by scanning for the signature.
        var sourceDirectory = CreateTempDirectory();
        var zipPath = Path.GetTempFileName() + ".zip";
        try
        {
            var trickyBytes = new byte[] { 0x50, 0x4b, 0x01, 0x02, 0x00, 0x00, 0x00, 0x00 };
            File.WriteAllBytes(Path.Combine(sourceDirectory, "data.bin"), trickyBytes);

            var includedFiles = new Dictionary<string, string>
            {
                ["data.bin"] = Path.Combine(sourceDirectory, "data.bin"),
            };

            ManagedZipArchive.BundleFiles(zipPath, sourceDirectory, includedFiles, null);

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry("data.bin");
            Assert.NotNull(entry);
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            Assert.Equal(trickyBytes, ms.ToArray());

            var records = ReadCentralDirectoryRecords(zipPath);
            Assert.Single(records);
            Assert.Equal(UnixHostSystem, records[0].HostSystem);
        }
        finally
        {
            Cleanup(sourceDirectory, zipPath);
        }
    }

    private static List<(byte HostSystem, uint UnixMode)> ReadCentralDirectoryRecords(string zipPath)
    {
        var bytes = File.ReadAllBytes(zipPath);
        var results = new List<(byte, uint)>();

        // Find the end of central directory record and read the central directory offset from it.
        for (var i = bytes.Length - 22; i >= 0; i--)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4b && bytes[i + 2] == 0x05 && bytes[i + 3] == 0x06)
            {
                var position = (int)BitConverter.ToUInt32(bytes, i + 16);
                while (position + CentralDirectoryHeaderSize <= bytes.Length
                    && bytes[position] == 0x50 && bytes[position + 1] == 0x4b
                    && bytes[position + 2] == 0x01 && bytes[position + 3] == 0x02)
                {
                    var host = bytes[position + VersionMadeByHostOffset];
                    var unixMode = BitConverter.ToUInt32(bytes, position + ExternalAttributesOffset) >> 16;
                    results.Add((host, unixMode));

                    var fileNameLength = BitConverter.ToUInt16(bytes, position + FileNameLengthOffset);
                    var extraFieldLength = BitConverter.ToUInt16(bytes, position + ExtraFieldLengthOffset);
                    var commentLength = BitConverter.ToUInt16(bytes, position + CommentLengthOffset);
                    position += CentralDirectoryHeaderSize + fileNameLength + extraFieldLength + commentLength;
                }

                break;
            }
        }

        return results;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ManagedZipArchiveTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(string sourceDirectory, string zipPath)
    {
        try
        {
            if (Directory.Exists(sourceDirectory))
                Directory.Delete(sourceDirectory, true);
        }
        catch { /* best effort */ }

        try
        {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
        catch { /* best effort */ }
    }
}
