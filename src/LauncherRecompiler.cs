using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BlizzLauncherArchive
{
    public static class LauncherRecompiler
    {
        public static bool RecompileLauncher(string launcherFolder, LauncherModConfig config, LauncherItem item = null, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(launcherFolder))
                return false;

            try
            {
                Directory.CreateDirectory(launcherFolder);
                config ??= LauncherModConfig.LoadOrCreate(launcherFolder, item);
                config.Save(launcherFolder);
                log?.Invoke($"[Recompiler] Saved configuration to {LauncherModConfig.GetConfigPath(launcherFolder)}");

                string decompiledDir = LauncherAssetDecompiler.GetDecompiledFolder(launcherFolder);
                if (!Directory.Exists(decompiledDir) || !File.Exists(Path.Combine(decompiledDir, "ASSET_MANIFEST.json")))
                {
                    LauncherAssetDecompiler.DecompileLauncherAssets(launcherFolder, item, log);
                }
                else
                {
                    LauncherAssetDecompiler.GenerateEditableWebAssets(launcherFolder, decompiledDir, config, log);
                }

                // Locate clean base binary (.orig)
                string sourceExe = LauncherAssetDecompiler.FindBestSourceBinary(launcherFolder);
                if (string.IsNullOrEmpty(sourceExe) || !File.Exists(sourceExe))
                {
                    log?.Invoke("[Recompiler] Could not locate base launcher executable to recompile.");
                    return false;
                }

                // Copy Qt4 runtime dependencies from ProgramData\Battle.net\Client\Blizzard Launcher.888 if building standalone custom binary
                string client888Dir = @"C:\ProgramData\Battle.net\Client\Blizzard Launcher.888";
                string customExePath = Path.Combine(launcherFolder, "Blizzard Launcher Custom.exe");
                File.Copy(sourceExe, customExePath, true);

                if (Directory.Exists(client888Dir))
                {
                    foreach (string dllOrManifest in Directory.GetFiles(client888Dir))
                    {
                        string ext = Path.GetExtension(dllOrManifest).ToLowerInvariant();
                        if (ext == ".dll" || ext == ".manifest" || ext == ".ttf")
                        {
                            string destFile = Path.Combine(launcherFolder, Path.GetFileName(dllOrManifest));
                            if (!File.Exists(destFile))
                            {
                                try { File.Copy(dllOrManifest, destFile, false); } catch { }
                            }
                        }
                    }
                }

                // 1. Apply Playable Main Screen & URL patches to Blizzard Launcher Custom.exe
                int port = config.LocalWebFeedPort > 0 ? config.LocalWebFeedPort : 29505;
                LauncherPatcher.PatchNativeLauncher(customExePath, log, port);

                // 2. Repack any modified assets from Decompiled_Assets into Blizzard Launcher Custom.exe
                int repackedAssets = RepackModifiedAssets(customExePath, decompiledDir, config, log);

                // 3. Also update ProgramData\Battle.net\Client\Blizzard Launcher.888\Blizzard Launcher.exe if present so running via standard path reflects changes immediately
                string progDataExe = Path.Combine(client888Dir, "Blizzard Launcher.exe");
                if (File.Exists(progDataExe))
                {
                    try
                    {
                        LauncherPatcher.PatchNativeLauncher(progDataExe, null, port);
                        RepackModifiedAssets(progDataExe, decompiledDir, config, null);
                    }
                    catch
                    {
                    }
                }

                // 4. Generate Launch-Custom-Server.bat
                string batPath = Path.Combine(launcherFolder, "Launch-Custom-Server.bat");
                File.WriteAllText(batPath, $@"@echo off
title {config.ServerName} — Custom Launcher
cd /d ""%~dp0""
echo Starting {config.ServerName} ({config.ServerHost}:{config.ServerPort})...
start """" ""%~dp0Blizzard Launcher Custom.exe"" --locale=enUS
");

                log?.Invoke($"[Recompiler] Recompiled 'Blizzard Launcher Custom.exe' ({repackedAssets} modified asset slot(s) updated).");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Recompiler] Error during recompile: {ex.Message}");
                return false;
            }
        }

        private static int RepackModifiedAssets(string targetExePath, string decompiledDir, LauncherModConfig config, Action<string> log)
        {
            byte[] pe = File.ReadAllBytes(targetExePath);
            int updatedCount = 0;

            string manifestPath = Path.Combine(decompiledDir, "ASSET_MANIFEST.json");
            if (File.Exists(manifestPath))
            {
                try
                {
                    var entries = JsonSerializer.Deserialize<List<DecompiledAssetEntry>>(File.ReadAllText(manifestPath));
                    if (entries != null)
                    {
                        foreach (var entry in entries)
                        {
                            string fileOnDisk = Path.Combine(decompiledDir, entry.RelativeFilePath.Replace('/', Path.DirectorySeparatorChar));
                            if (!File.Exists(fileOnDisk)) continue;
                            if (entry.BinaryDataOffset < 4 || entry.BinaryDataOffset + entry.BinaryDataLength > pe.Length) continue;

                            byte[] diskBytes = File.ReadAllBytes(fileOnDisk);
                            if (!string.IsNullOrEmpty(entry.OriginalSha256))
                            {
                                string currentDiskSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(diskBytes));
                                if (string.Equals(currentDiskSha, entry.OriginalSha256, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // Unmodified by user
                                }
                            }

                            byte[] payloadToEmbed = entry.IsZlibCompressed ? QtCompress(diskBytes) : diskBytes;
                            if (payloadToEmbed.Length <= entry.BinaryDataLength)
                            {
                                if (entry.IsQtResource)
                                {
                                    WriteUInt32BE(pe, entry.BinaryDataOffset - 4, (uint)payloadToEmbed.Length);
                                }
                                for (int i = 0; i < entry.BinaryDataLength; i++)
                                {
                                    pe[entry.BinaryDataOffset + i] = i < payloadToEmbed.Length ? payloadToEmbed[i] : (byte)0;
                                }
                                updatedCount++;
                                log?.Invoke($"[Recompiler] Embedded modified resource: {entry.RelativeFilePath} ({payloadToEmbed.Length} bytes)");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[Recompiler] Asset manifest notice: {ex.Message}");
                }
            }

            // Patch custom status text ("Game is up to date" at 0x56680c -> max 18 chars) if customized
            if (!string.IsNullOrWhiteSpace(config?.StatusText) &&
                !string.Equals(config.StatusText, "Game is up to date", StringComparison.OrdinalIgnoreCase))
            {
                if (PatchAsciiStringInPlace(pe, "Game is up to date", config.StatusText))
                    updatedCount++;
            }

            File.WriteAllBytes(targetExePath, pe);
            return updatedCount;
        }

        private static bool PatchAsciiStringInPlace(byte[] pe, string originalStr, string newStr)
        {
            byte[] origBytes = Encoding.ASCII.GetBytes(originalStr + "\0");
            int pos = FindBytes(pe, origBytes);
            if (pos == -1) return false;

            string truncated = newStr.Length > originalStr.Length ? newStr.Substring(0, originalStr.Length) : newStr;
            byte[] newBytes = Encoding.ASCII.GetBytes(truncated);
            for (int i = 0; i <= originalStr.Length; i++)
            {
                pe[pos + i] = i < newBytes.Length ? newBytes[i] : (byte)0;
            }
            return true;
        }

        private static byte[] QtCompress(byte[] uncompressed)
        {
            using var msOut = new MemoryStream();
            // 4-byte big-endian uncompressed length
            uint len = (uint)uncompressed.Length;
            msOut.WriteByte((byte)(len >> 24));
            msOut.WriteByte((byte)(len >> 16));
            msOut.WriteByte((byte)(len >> 8));
            msOut.WriteByte((byte)len);
            // 2-byte zlib header (78 9C)
            msOut.WriteByte(0x78);
            msOut.WriteByte(0x9C);
            using (var deflate = new DeflateStream(msOut, CompressionLevel.SmallestSize, true))
            {
                deflate.Write(uncompressed, 0, uncompressed.Length);
            }
            // 4-byte Adler32 checksum
            uint adler = ComputeAdler32(uncompressed);
            msOut.WriteByte((byte)(adler >> 24));
            msOut.WriteByte((byte)(adler >> 16));
            msOut.WriteByte((byte)(adler >> 8));
            msOut.WriteByte((byte)adler);
            return msOut.ToArray();
        }

        private static uint ComputeAdler32(byte[] data)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;
            foreach (byte d in data)
            {
                a = (a + d) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }
            return (b << 16) | a;
        }

        private static uint ReadUInt32BE(byte[] buf, int offset)
        {
            return ((uint)buf[offset] << 24) |
                   ((uint)buf[offset + 1] << 16) |
                   ((uint)buf[offset + 2] << 8) |
                   buf[offset + 3];
        }

        private static void WriteUInt32BE(byte[] buf, int offset, uint val)
        {
            buf[offset] = (byte)(val >> 24);
            buf[offset + 1] = (byte)(val >> 16);
            buf[offset + 2] = (byte)(val >> 8);
            buf[offset + 3] = (byte)val;
        }

        private static int FindBytes(byte[] haystack, byte[] needle)
        {
            int limit = haystack.Length - needle.Length;
            for (int i = 0; i <= limit; i++)
            {
                if (haystack[i] != needle[0]) continue;
                bool match = true;
                for (int j = 1; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
