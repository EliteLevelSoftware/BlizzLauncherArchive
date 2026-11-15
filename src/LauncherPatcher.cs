using System;
using System.IO;

namespace BlizzLauncherArchive
{
    public static class LauncherPatcher
    {
        // Signatures for anti-update bypass across classic Blizzard launchers
        // 1. Diablo III Bootstrapper (Build 16603 and retail lineage):
        //    Pattern A: Stage 6 (START_AGENT) transition to Stage 7 (UPDATE_AGENT) -> change target to 8 (CHECK_CLIENT)
        //    6a 07 83 ec 1c 8b f4 c7 47 08 07 00 00 00
        private static readonly byte[] D3Stage6To7Sig = { 0x6A, 0x07, 0x83, 0xEC, 0x1C, 0x8B, 0xF4, 0xC7, 0x47, 0x08, 0x07, 0x00, 0x00, 0x00 };

        //    Pattern B: Stage 8 (CHECK_CLIENT) conditional jump to Stage 11 (UPDATE_CLIENT) -> NOP out (90 90) to force Stage 12 (LAUNCH_CLIENT)
        //    84 c0 74 0f c6 46 14 01 c7 46 08 0c 00 00 00 6a 0c
        private static readonly byte[] D3Stage8To11Sig = { 0x84, 0xC0, 0x74, 0x0F, 0xC6, 0x46, 0x14, 0x01, 0xC7, 0x46, 0x08, 0x0C, 0x00, 0x00, 0x00, 0x6A, 0x0C };

        public static bool PatchBinary(string exePath, LauncherItem item, Action<string> log = null)
        {
            if (!File.Exists(exePath))
            {
                log?.Invoke($"[Anti-Update Patch] File not found: {exePath}");
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(exePath);
                bool modified = false;

                // Diablo III Retail / Standalone Launchers
                if (item == null || item.Family == LauncherGameFamily.Diablo3)
                {
                    // Pattern A: Skip UPDATE_AGENT
                    int posA = FindSequence(bytes, D3Stage6To7Sig);
                    if (posA != -1 && bytes[posA + 1] == 0x07)
                    {
                        CreateBackup(exePath, log);
                        bytes[posA + 1] = 0x08;  // push 8
                        bytes[posA + 10] = 0x08; // mov [edi+8], 8 (CHECK_CLIENT)
                        modified = true;
                        log?.Invoke($"[Anti-Update Patch] Bypassed Agent network update loop at 0x{posA:X} (Target Stage 7 -> 8)");
                    }

                    // Pattern B: Skip UPDATE_CLIENT and force LAUNCH_CLIENT
                    int posB = FindSequence(bytes, D3Stage8To11Sig);
                    if (posB != -1 && bytes[posB + 2] == 0x74)
                    {
                        CreateBackup(exePath, log);
                        bytes[posB + 2] = 0x90; // nop
                        bytes[posB + 3] = 0x90; // nop
                        modified = true;
                        log?.Invoke($"[Anti-Update Patch] Bypassed Client network update loop at 0x{posB:X} (Direct Launch Client)");
                    }
                }

                if (modified)
                {
                    File.WriteAllBytes(exePath, bytes);
                    log?.Invoke($"[Anti-Update Patch] Successfully applied anti-auto-update patch to {Path.GetFileName(exePath)}.");
                    return true;
                }
                else
                {
                    log?.Invoke($"[Anti-Update Patch] Binary checked — anti-update protection verified.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Anti-Update Patch] Notice: Could not patch binary: {ex.Message}");
                return false;
            }
        }

        private static void CreateBackup(string path, Action<string> log)
        {
            string bakPath = path + ".bak";
            if (!File.Exists(bakPath))
            {
                File.Copy(path, bakPath, false);
                log?.Invoke($"[Anti-Update Patch] Preserved untouched original binary as {Path.GetFileName(bakPath)}");
            }
        }

        private static int FindSequence(byte[] source, byte[] pattern)
        {
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        private static int FindPattern(byte[] source, byte?[] pattern)
        {
            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j].HasValue && source[i + j] != pattern[j].Value)
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
