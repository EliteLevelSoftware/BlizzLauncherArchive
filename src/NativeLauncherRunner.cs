using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public static class NativeLauncherRunner
    {
        public static string FindClientExecutable(string gameDirectory = null)
        {
            // 1. Check ProgramData\Battle.net\Client\Blizzard Launcher.*
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string clientBase = Path.Combine(programData, "Battle.net", "Client");
            if (Directory.Exists(clientBase))
            {
                var launcherDirs = Directory.GetDirectories(clientBase, "Blizzard Launcher.*")
                    .OrderByDescending(d => d)
                    .ToList();

                foreach (var dir in launcherDirs)
                {
                    string candidate = Path.Combine(dir, "Blizzard Launcher.exe");
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }

                string directCandidate = Path.Combine(clientBase, "Blizzard Launcher.exe");
                if (File.Exists(directCandidate))
                {
                    return directCandidate;
                }
            }

            // 2. Check local game directory
            if (!string.IsNullOrWhiteSpace(gameDirectory) && Directory.Exists(gameDirectory))
            {
                string localExe = Path.Combine(gameDirectory, "Blizzard Launcher.exe");
                if (File.Exists(localExe)) return localExe;

                string subClient = Path.Combine(gameDirectory, "Client", "Blizzard Launcher.exe");
                if (File.Exists(subClient)) return subClient;
            }

            return null;
        }

        public static async Task<bool> LaunchAsync(string gameDirectory, LauncherItem item = null, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                log?.Invoke($"[Native Launch] Target directory does not exist: {gameDirectory}");
                return false;
            }

            string clientExe = FindClientExecutable(gameDirectory);
            if (string.IsNullOrEmpty(clientExe) || !File.Exists(clientExe))
            {
                log?.Invoke("[Native Launch] Native Blizzard Launcher binary (Qt client) not found in ProgramData or game directory.");
                return false;
            }

            log?.Invoke($"[Native Launch] Found native client: {clientExe}");

            // Patch product switch in native client if needed
            LauncherPatcher.PatchNativeLauncher(clientExe, log);

            // Ensure launcher DB exists
            string dbPath = Path.Combine(gameDirectory, "Launcher.db");
            if (!File.Exists(dbPath))
            {
                try
                {
                    File.WriteAllText(dbPath, "enUS");
                }
                catch { }
            }

            // Ensure game executable stub or link exists so launcher sees valid installation
            string d3Exe = Path.Combine(gameDirectory, "Diablo III.exe");
            if (!File.Exists(d3Exe))
            {
                try
                {
                    // Create minimal placeholder if game binary is not yet linked
                    File.WriteAllBytes(d3Exe, Array.Empty<byte>());
                }
                catch { }
            }

            // Start Agent Shim Server
            AgentShimServer shim = null;
            try
            {
                shim = new AgentShimServer(11195, gameDirectory);
                shim.Start();
                log?.Invoke($"[Native Launch] Local Agent Shim active on port {shim.Port}.");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Native Launch] Warning: Could not start Agent Shim: {ex.Message}");
            }

            // Spawn Blizzard Launcher.exe
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = clientExe,
                    Arguments = "--locale=enUS",
                    WorkingDirectory = gameDirectory,
                    UseShellExecute = false
                };

                var proc = Process.Start(psi);
                if (proc == null)
                {
                    log?.Invoke("[Native Launch] Failed to spawn Blizzard Launcher process.");
                    shim?.Dispose();
                    return false;
                }

                log?.Invoke($"[Native Launch] Native Blizzard Launcher launched (PID {proc.Id}).");

                proc.EnableRaisingEvents = true;
                proc.Exited += (_, _) =>
                {
                    log?.Invoke("[Native Launch] Native Blizzard Launcher closed. Restoring environment.");
                    shim?.Dispose();
                };

                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Native Launch] Error launching binary: {ex.Message}");
                shim?.Dispose();
                return false;
            }
        }

        public static bool LaunchSync(string gameDirectory, LauncherItem item = null, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                log?.Invoke($"[Native Launch] Target directory does not exist: {gameDirectory}");
                return false;
            }

            string clientExe = FindClientExecutable(gameDirectory);
            if (string.IsNullOrEmpty(clientExe) || !File.Exists(clientExe))
            {
                log?.Invoke("[Native Launch] Native Blizzard Launcher binary (Qt client) not found in ProgramData or game directory.");
                return false;
            }

            log?.Invoke($"[Native Launch] Found native client: {clientExe}");

            // Patch product switch in native client if needed
            LauncherPatcher.PatchNativeLauncher(clientExe, log);

            // Ensure launcher DB exists
            string dbPath = Path.Combine(gameDirectory, "Launcher.db");
            if (!File.Exists(dbPath))
            {
                try
                {
                    File.WriteAllText(dbPath, "enUS");
                }
                catch { }
            }

            // Ensure game executable stub or link exists
            string d3Exe = Path.Combine(gameDirectory, "Diablo III.exe");
            if (!File.Exists(d3Exe))
            {
                try
                {
                    File.WriteAllBytes(d3Exe, Array.Empty<byte>());
                }
                catch { }
            }

            // Start Agent Shim Server
            using var shim = new AgentShimServer(11195, gameDirectory);
            shim.Start();
            log?.Invoke($"[Native Launch] Local Agent Shim active on port {shim.Port}.");

            var psi = new ProcessStartInfo
            {
                FileName = clientExe,
                Arguments = "--locale=enUS",
                WorkingDirectory = gameDirectory,
                UseShellExecute = false
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                log?.Invoke("[Native Launch] Failed to spawn Blizzard Launcher process.");
                return false;
            }

            log?.Invoke($"[Native Launch] Native Blizzard Launcher running (PID {proc.Id}). Waiting for exit...");
            proc.WaitForExit();
            log?.Invoke("[Native Launch] Native Blizzard Launcher closed. Restoring environment.");
            return true;
        }
    }
}
