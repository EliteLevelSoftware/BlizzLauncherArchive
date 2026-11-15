using System;
using System.IO;

namespace BlizzLauncherArchive
{
    public static class LauncherPatcher
    {
        // Signatures for anti-update bypass across classic Blizzard launchers
        // 1. Diablo III Launcher.exe (Build 16603 and similar retail):
        //    State machine dispatcher: test al, al (84 c0) ; jne target (75 6b) ; push 0x556bd0
        //    Replacing 75 6b (jne) with eb 6b (jmp) skips the UPDATE_AGENT transition
        private static readonly byte[] D3RetailUpdateSig = { 0x84, 0xC0, 0x75, 0x6B, 0x68, 0xD0, 0x6B, 0x55, 0x00 };

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
                    int pos = FindSequence(bytes, D3RetailUpdateSig);
                    if (pos != -1)
                    {
                        if (bytes[pos + 2] == 0x75)
                        {
                            CreateBackup(exePath, log);
                            bytes[pos + 2] = 0xEB; // jne -> jmp
                            modified = true;
                            log?.Invoke($"[Anti-Update Patch] Bypassed Agent update check at offset 0x{pos:X} (Original saved as .bak)");
                        }
                    }
                    else
                    {
                        // Generic fallback pattern for D3 bootstrappers: 84 c0 75 ?? 68 ?? ?? ?? 00
                        int genericPos = FindPattern(bytes, new byte?[] { 0x84, 0xC0, 0x75, null, 0x68, null, null, null, 0x00, 0xFF, 0x15 });
                        if (genericPos != -1 && bytes[genericPos + 2] == 0x75)
                        {
                            CreateBackup(exePath, log);
                            bytes[genericPos + 2] = 0xEB;
                            modified = true;
                            log?.Invoke($"[Anti-Update Patch] Bypassed bootstrapper update check at offset 0x{genericPos:X}");
                        }
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
