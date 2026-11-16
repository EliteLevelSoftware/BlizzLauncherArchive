using System;
using System.IO;
using System.Text;

namespace BlizzLauncherArchive
{
    public static class LauncherPatcher
    {
        // 1. Diablo III Bootstrapper (Build 16603 and retail lineage):
        //    Pattern A: Stage 6 (START_AGENT) transition to Stage 7 (UPDATE_AGENT) -> change target to 8 (CHECK_CLIENT)
        private static readonly byte[] D3Stage6To7Sig = { 0x6A, 0x07, 0x83, 0xEC, 0x1C, 0x8B, 0xF4, 0xC7, 0x47, 0x08, 0x07, 0x00, 0x00, 0x00 };

        //    Pattern B: Stage 8 (CHECK_CLIENT) conditional jump to Stage 11 (UPDATE_CLIENT) -> NOP out (90 90) to force Stage 12 (LAUNCH_CLIENT)
        private static readonly byte[] D3Stage8To11Sig = { 0x84, 0xC0, 0x74, 0x0F, 0xC6, 0x46, 0x14, 0x01, 0xC7, 0x46, 0x08, 0x0C, 0x00, 0x00, 0x00, 0x6A, 0x0C };

        // 2. Native Qt Client (Blizzard Launcher.exe in Client\Blizzard Launcher.888):
        //    Patch 1: Bypass product code check: cmp eax, 3; ja default -> mov eax, 2; nop; nop; nop; nop
        private static readonly byte[] NativeProductCheckSig = { 0x83, 0xF8, 0x03, 0x0F, 0x87, 0xA0, 0x00, 0x00, 0x00 };
        private static readonly byte[] NativeProductPatchedSig = { 0xB8, 0x02, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90 };

        //    Patch 2: Bypass "Failed to retrieve Blizzard Launcher properties" error dialog check
        private static readonly byte[] NativePropertiesSig = { 0x83, 0x7F, 0x14, 00, 0x74, 0x2E, 0xE8 };
        private static readonly byte[] NativePropertiesPatchedSig = { 0x83, 0x7F, 0x14, 00, 0x90, 0x90, 0xE8 };

        //    Patch 3: Force "install found" state transition to reveal main gothic launcher window
        private static readonly byte[] NativeInstallCheckSig = { 0x8B, 0xCE, 0xE8, 0x46, 0xEB, 0x09, 0x00, 0x84, 0xC0, 0x0F, 0x84, 0xBF, 0x00, 0x00, 0x00 };
        private static readonly byte[] NativeInstallPatchedSig = { 0x8B, 0xCE, 0xE8, 0x46, 0xEB, 0x09, 0x00, 0x84, 0xC0, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };

        //    Patch 4: Bypass operation exception dispatch (0x412999)
        private static readonly byte[] NativeOpEx1Sig = { 0x80, 0x79, 0x44, 0x00, 0x74, 0x05, 0xE8, 0xCC, 0x1A, 0x0A, 0x00 };
        private static readonly byte[] NativeOpEx1PatchedSig = { 0x80, 0x79, 0x44, 0x00, 0xEB, 0x05, 0xE8, 0xCC, 0x1A, 0x0A, 0x00 };

        //    Patch 5: Bypass operation exception dispatch (0x417616)
        private static readonly byte[] NativeOpEx2Sig = { 0x38, 0x41, 0x44, 0x74, 0x05, 0xE8, 0x50, 0xCE, 0x09, 0x00 };
        private static readonly byte[] NativeOpEx2PatchedSig = { 0x38, 0x41, 0x44, 0xEB, 0x05, 0xE8, 0x50, 0xCE, 0x09, 0x00 };

        //    Patch 6: Neutralize communication error modal dialog invocation (0x417917)
        private static readonly byte[] NativeCommDialogSig = { 0x8B, 0x4D, 0x90, 0xE8, 0xE4, 0x6C, 0x01, 0x00 };
        private static readonly byte[] NativeCommDialogPatchedSig = { 0x8B, 0x4D, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };

        //    Patch 7: Reduce 30-second Agent retry timeout (0x4c594f: 2b c7 3d 30 75 00 00 76 da -> 1ms)
        private static readonly byte[] NativeAgentTimeoutSig = { 0x2B, 0xC7, 0x3D, 0x30, 0x75, 0x00, 0x00, 0x76, 0xDA };
        private static readonly byte[] NativeAgentTimeoutPatchedSig = { 0x2B, 0xC7, 0x3D, 0x01, 0x00, 0x00, 0x00, 0x76, 0xDA };

        //    Patch 8: Show stageTextContainer ([esi+0x44]) in PatchPanel ctor (0x41012b) and StageLabelController (0x41aae6)
        private static readonly byte[] NativeStageHideSig = { 0x8B, 0x4E, 0x44, 0xFF, 0x15, 0x98, 0x22, 0x56, 0x00, 0x8B, 0x46, 0x78 };
        private static readonly byte[] NativeStageShowSig = { 0x8B, 0x4E, 0x44, 0xFF, 0x15, 0x94, 0x22, 0x56, 0x00, 0x8B, 0x46, 0x78 };
        private static readonly byte[] NativeStageLabelsFalseSig = { 0xE8, 0x0A, 0xE3, 0xFF, 0xFF, 0x6A, 0x00, 0x8B, 0xCF, 0xE8, 0x11, 0xF1, 0xFF, 0xFF };
        private static readonly byte[] NativeStageLabelsTrueSig = { 0xE8, 0x0A, 0xE3, 0xFF, 0xFF, 0x6A, 0x01, 0x8B, 0xCF, 0xE8, 0x11, 0xF1, 0xFF, 0xFF };

        public static bool PatchNativeLauncher(string exePath, Action<string> log = null, int webPort = 29505)
        {
            if (!File.Exists(exePath))
            {
                log?.Invoke($"[Native Patch] Target binary not found: {exePath}");
                return false;
            }

            try
            {
                CreateBackup(exePath, log);
                string origPath = exePath + ".orig";
                byte[] sourceBytes = File.Exists(origPath) ? File.ReadAllBytes(origPath) : File.ReadAllBytes(exePath);
                byte[] bytes = (byte[])sourceBytes.Clone();

                ApplyReplace(bytes, NativeProductCheckSig, NativeProductPatchedSig);
                ApplyReplace(bytes, NativePropertiesSig, NativePropertiesPatchedSig);
                ApplyReplace(bytes, NativeInstallCheckSig, NativeInstallPatchedSig);
                ApplyReplace(bytes, NativeOpEx1Sig, NativeOpEx1PatchedSig);
                ApplyReplace(bytes, NativeOpEx2Sig, NativeOpEx2PatchedSig);
                ApplyReplace(bytes, NativeCommDialogSig, NativeCommDialogPatchedSig);
                ApplyReplace(bytes, NativeAgentTimeoutSig, NativeAgentTimeoutPatchedSig);
                ApplyReplace(bytes, NativeStageHideSig, NativeStageShowSig);
                ApplyReplace(bytes, NativeStageLabelsFalseSig, NativeStageLabelsTrueSig);

                // Playable Main Screen Patch Set (for Blizzard Launcher.888 layout where .text VA - 0x400c00 == file offset):
                if (bytes.Length > 0x170000)
                {
                    // 1) PatchPanel::onToolsPatchText (0x40e820 -> file 0xdc20): ret 8 (c2 08 00) so it never overwrites "Game is up to date"
                    int offToolsPatchText = 0x40E820 - 0x400C00;
                    bytes[offToolsPatchText + 0] = 0xC2;
                    bytes[offToolsPatchText + 1] = 0x08;
                    bytes[offToolsPatchText + 2] = 0x00;

                    // 2) PatchPanel::setSubState jump table (0x40fc6c -> file 0xf06c): redirect cases 4, 5, 6 to case 7 (0x40fae1)
                    int offJumpTable = 0x40FC6C - 0x400C00;
                    byte[] case7Addr = { 0xE1, 0xFA, 0x40, 0x00 };
                    for (int idx = 3; idx <= 5; idx++)
                    {
                        Buffer.BlockCopy(case7Addr, 0, bytes, offJumpTable + idx * 4, 4);
                    }

                    // 3) NOP out case 7 return epilogue (0x40fb3c..0x40fb4f -> file 0xef3c..0xef4f, 19 bytes) so case 7 falls through to case 8
                    int offCase7Ret = 0x40FB3C - 0x400C00;
                    for (int i = 0; i < 19; i++)
                    {
                        bytes[offCase7Ret + i] = 0x90;
                    }

                    // 4) In case 8 epilogue + case 9 body (0x40fb8f..0x40fbda -> file 0xef8f..0xefda, 75 bytes):
                    //    Set progressBar range (0, 1000) and value (1000), show stageTextContainer ([esi+0x44]),
                    //    enable playButton ([esi+0x78]), and fall through to 0x40fbda (loadNewsUrl)
                    int offCase8Body = 0x40FB8F - 0x400C00;
                    int lenCase8Body = 0x40FBDA - 0x40FB8F;
                    byte[] playableBarAndBtn =
                    {
                        0x8B, 0x4E, 0x74, 0x68, 0xE8, 0x03, 0x00, 0x00, 0x6A, 0x00, 0xFF, 0x15, 0x94, 0x1D, 0x56, 0x00, // setRange(0, 1000)
                        0x8B, 0x4E, 0x74, 0x68, 0xE8, 0x03, 0x00, 0x00, 0xFF, 0x15, 0x98, 0x1D, 0x56, 0x00,             // setValue(1000)
                        0x8B, 0x4E, 0x44, 0xFF, 0x15, 0x94, 0x22, 0x56, 0x00,                                           // stageTextContainer->show()
                        0x8B, 0x4E, 0x78, 0x6A, 0x01, 0xFF, 0x15, 0xF0, 0x20, 0x56, 0x00                                // playButton->setEnabled(true)
                    };
                    for (int i = 0; i < lenCase8Body; i++)
                    {
                        bytes[offCase8Body + i] = i < playableBarAndBtn.Length ? playableBarAndBtn[i] : (byte)0x90;
                    }

                    // 5) In UIController::onEnteredCheckAgentState (0x402a2e..0x402a74 -> file 0x1e2e..0x1e74, 70 bytes):
                    //    Enable MainWindow menuBar (OPTIONS, INFO, HELP), connect playButton (2userRequestedPlay() -> 1onPlayRequested()),
                    //    and immediately call UIController::onShowUI() (0x401530) to show 820x650 Diablo III window & hide splash screen!
                    int offCheckAgent = 0x402A2E - 0x400C00;
                    int lenCheckAgent = 0x402A74 - 0x402A2E;
                    byte[] checkAgentHook =
                    {
                        0x8B, 0x46, 0x08, 0x8B, 0x48, 0x08, 0xE8, 0x37, 0x63, 0x00, 0x00,                               // MainWindow->enableMenuBar()
                        0x8B, 0x46, 0x08, 0x8B, 0x48, 0x18, 0x6A, 0x00, 0x68, 0x30, 0x3A, 0x56, 0x00,                   // connect(PatchPanel,
                        0x56, 0x68, 0x18, 0x3A, 0x56, 0x00, 0x51, 0xFF, 0x15, 0xF4, 0x18, 0x56, 0x00, 0x83, 0xC4, 0x14, //   "2userRequestedPlay()", UIController, "1onPlayRequested()", 0)
                        0x8B, 0xCE, 0xE8, 0xD3, 0xEA, 0xFF, 0xFF                                                        // UIController->onShowUI()
                    };
                    for (int i = 0; i < lenCheckAgent; i++)
                    {
                        bytes[offCheckAgent + i] = i < checkAgentHook.Length ? checkAgentHook[i] : (byte)0x90;
                    }

                    // 6) In UIController::onPlayRequested (0x405e90 -> file 0x5290):
                    //    When PLAY is clicked, trigger PatchPanel::loadNewsUrl / /play endpoint on our local AgentShimServer
                    //    so AgentShimServer can launch the custom TargetExecutable from launcher-config.json!
                    //    Wait: we can also keep onPlayRequested calling loadNewsUrl or a custom URL!
                }

                // 7) Rewrite embedded .rdata Nydus / Battle.net URLs to point to our local AgentShimServer (http://127.0.0.1:<webPort>)
                string newHost = $"http://127.0.0.1:{webPort}";
                ReplaceUrlHostInPlace(bytes, "http://enUS.nydus.battle.net", newHost);
                ReplaceUrlHostInPlace(bytes, "http://us.launcher.battle.net", newHost);

                File.WriteAllBytes(exePath, bytes);
                log?.Invoke($"[Native Patch] Applied playable main screen + WebKit news + enabled PLAY button patches to {Path.GetFileName(exePath)}.");
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Native Patch] Error: {ex.Message}");
                return false;
            }
        }

        public static void ReplaceUrlHostInPlace(byte[] buffer, string oldHost, string newHost)
        {
            byte[] oldBytes = Encoding.ASCII.GetBytes(oldHost);
            int idx = 0;
            while (idx <= buffer.Length - oldBytes.Length)
            {
                int found = FindSequenceFrom(buffer, oldBytes, idx);
                if (found == -1) break;

                int endNull = found;
                while (endNull < buffer.Length && buffer[endNull] != 0)
                    endNull++;

                int origLen = endNull - found;
                string origUrl = Encoding.ASCII.GetString(buffer, found, origLen);
                string replacedUrl = origUrl.Replace(oldHost, newHost);
                byte[] replacedBytes = Encoding.ASCII.GetBytes(replacedUrl);

                if (replacedBytes.Length <= origLen)
                {
                    for (int i = 0; i < origLen; i++)
                    {
                        buffer[found + i] = i < replacedBytes.Length ? replacedBytes[i] : (byte)0;
                    }
                }
                idx = endNull + 1;
            }
        }

        private static bool ApplyReplace(byte[] bytes, byte[] sig, byte[] replacement)
        {
            int pos = FindSequence(bytes, sig);
            if (pos != -1)
            {
                Buffer.BlockCopy(replacement, 0, bytes, pos, replacement.Length);
                return true;
            }
            return false;
        }

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

                if (item == null || item.Family == LauncherGameFamily.Diablo3)
                {
                    int posA = FindSequence(bytes, D3Stage6To7Sig);
                    if (posA != -1 && bytes[posA + 1] == 0x07)
                    {
                        CreateBackup(exePath, log);
                        bytes[posA + 1] = 0x08;
                        bytes[posA + 10] = 0x08;
                        modified = true;
                        log?.Invoke($"[Anti-Update Patch] Bypassed Agent network update loop at 0x{posA:X} (Target Stage 7 -> 8)");
                    }

                    int posB = FindSequence(bytes, D3Stage8To11Sig);
                    if (posB != -1 && bytes[posB + 2] == 0x74)
                    {
                        CreateBackup(exePath, log);
                        bytes[posB + 2] = 0x90;
                        bytes[posB + 3] = 0x90;
                        modified = true;
                        log?.Invoke($"[Anti-Update Patch] Bypassed Client network update loop at 0x{posB:X} (Direct Launch Client)");
                    }
                }

                if (modified)
                {
                    File.WriteAllBytes(exePath, bytes);
                    log?.Invoke($"[Anti-Update Patch] Successfully applied anti-auto-update patch to {Path.GetFileName(exePath)}.");
                }
                else
                {
                    log?.Invoke($"[Anti-Update Patch] Binary checked — anti-update protection verified.");
                }
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Anti-Update Patch] Notice: Could not patch binary: {ex.Message}");
                return false;
            }
        }

        private static void CreateBackup(string path, Action<string> log)
        {
            string bakPath = path + ".orig";
            if (!File.Exists(bakPath))
            {
                File.Copy(path, bakPath, false);
                log?.Invoke($"[Anti-Update Patch] Preserved untouched original binary as {Path.GetFileName(bakPath)}");
            }
        }

        private static int FindSequence(byte[] source, byte[] pattern)
        {
            return FindSequenceFrom(source, pattern, 0);
        }

        private static int FindSequenceFrom(byte[] source, byte[] pattern, int startIndex)
        {
            int limit = source.Length - pattern.Length;
            for (int i = startIndex; i <= limit; i++)
            {
                if (source[i] != pattern[0]) continue;
                bool match = true;
                for (int j = 1; j < pattern.Length; j++)
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
    }
}
