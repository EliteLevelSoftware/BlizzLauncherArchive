using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public static class NativeLauncherRunner
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        public static string ResolveGameDirectory(string targetDir, LauncherItem item = null)
        {
            if (string.IsNullOrWhiteSpace(targetDir))
                targetDir = @"C:\Games\Blizzard_Launcher_Archive";

            if (!Directory.Exists(targetDir)) return targetDir;

            // 1. If targetDir already contains Diablo III files, use it directly
            if (File.Exists(Path.Combine(targetDir, "Diablo III Launcher.exe")) || File.Exists(Path.Combine(targetDir, "SetupWin.mpq")))
            {
                return targetDir;
            }

            // 2. Direct match with item.FolderPrefix
            if (item != null)
            {
                string direct = Path.Combine(targetDir, item.FolderPrefix);
                if (Directory.Exists(direct)) return direct;

                // Match by build number or family
                var match = Directory.GetDirectories(targetDir, $"*{item.BuildNumber}*").FirstOrDefault();
                if (match != null) return match;
            }

            // 3. Fallback to any Diablo3_* subfolder
            var d3Dir = Directory.GetDirectories(targetDir, "Diablo3_*").OrderByDescending(d => d).FirstOrDefault();
            if (d3Dir != null) return d3Dir;

            return targetDir;
        }

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

        private static void StartWindowWatcher(Process proc, Action<string> log)
        {
            Task.Run(async () =>
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    await Task.Delay(500);
                    if (proc.HasExited) break;

                    IntPtr mainHwnd = IntPtr.Zero;
                    IntPtr splashHwnd = IntPtr.Zero;

                    EnumWindows((hwnd, _) =>
                    {
                        GetWindowThreadProcessId(hwnd, out uint pid);
                        if (pid == proc.Id)
                        {
                            GetWindowRect(hwnd, out RECT r);
                            int w = r.Right - r.Left;
                            int h = r.Bottom - r.Top;
                            var sb = new StringBuilder(256);
                            GetWindowText(hwnd, sb, 256);
                            string title = sb.ToString();

                            if (title == "Diablo III" || (w >= 800 && h >= 600))
                            {
                                mainHwnd = hwnd;
                            }
                            else if (w >= 500 && w <= 600 && h >= 300 && h <= 400)
                            {
                                splashHwnd = hwnd;
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (mainHwnd != IntPtr.Zero)
                    {
                        if (!IsWindowVisible(mainHwnd))
                        {
                            ShowWindow(mainHwnd, SW_SHOW);
                            SetForegroundWindow(mainHwnd);
                            log?.Invoke("[Native Launch] Authentic Diablo III Gothic Launcher window brought to foreground.");
                        }

                        if (splashHwnd != IntPtr.Zero && IsWindowVisible(splashHwnd))
                        {
                            ShowWindow(splashHwnd, SW_HIDE);
                        }
                        break;
                    }
                }
            });
        }

        public static async Task<bool> LaunchAsync(string gameDirectory, LauncherItem item = null, Action<string> log = null)
        {
            gameDirectory = ResolveGameDirectory(gameDirectory, item);
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

            log?.Invoke($"[Native Launch] Target Game Directory: {gameDirectory}");
            log?.Invoke($"[Native Launch] Found native client: {clientExe}");

            // Apply all native client patches (Retail product switch, properties dialog bypass, install state)
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

                StartWindowWatcher(proc, log);

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
            gameDirectory = ResolveGameDirectory(gameDirectory, item);
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

            log?.Invoke($"[Native Launch] Target Game Directory: {gameDirectory}");
            log?.Invoke($"[Native Launch] Found native client: {clientExe}");

            // Apply all native client patches
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
            StartWindowWatcher(proc, log);
            proc.WaitForExit();
            log?.Invoke("[Native Launch] Native Blizzard Launcher closed. Restoring environment.");
            return true;
        }
    }
}
