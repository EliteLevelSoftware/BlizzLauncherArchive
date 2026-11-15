using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        [STAThread]
        public static int Main(string[] args)
        {
            if (args == null || args.Length == 0 || args.Any(a => a.Equals("--gui", StringComparison.OrdinalIgnoreCase)))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LauncherSelectorForm());
                return 0;
            }

            // Headless CLI execution
            AttachConsole(-1);
            Console.WriteLine("========================================================================");
            Console.WriteLine(" Blizzard Launcher Archive — Standalone & Battle.net Time Machine");
            Console.WriteLine(" by Elite Level Software (v1.0.5)");
            Console.WriteLine("========================================================================");

            string targetDir = @"C:\Games\Blizzard_Launcher_Archive";
            string versionArg = null;
            string gameArg = "all";
            bool binariesOnly = false;
            bool doBackup = false;
            bool doRetroLaunch = false;
            bool doNativeLaunch = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (a.Equals("--shim", StringComparison.OrdinalIgnoreCase))
                {
                    int shimPort = 0;
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int p))
                    {
                        shimPort = p;
                        i++;
                    }
                    Console.WriteLine($"Starting Local Agent Shim on port {(shimPort > 0 ? shimPort.ToString() : "dynamic")}...");
                    using var shim = new AgentShimServer(shimPort);
                    shim.Start();
                    Console.WriteLine($"[SHIM ACTIVE] Listening on port {shim.Port}. Press Ctrl+C to stop.");
                    Thread.Sleep(Timeout.Infinite);
                    return 0;
                }
                if (a.Equals("--patch", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    string fileToPatch = Path.GetFullPath(args[++i]);
                    Console.WriteLine($"Applying Anti-AutoUpdate patch to: {fileToPatch}");
                    bool ok = LauncherPatcher.PatchBinary(fileToPatch, null, Console.WriteLine);
                    return ok ? 0 : 1;
                }
                if (a.Equals("--snapshot-expanded", StringComparison.OrdinalIgnoreCase) && i + 2 < args.Length)
                {
                    int expandIdx = int.Parse(args[++i]);
                    string snapPath = args[++i];
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var form = new LauncherSelectorForm();
                    form.Show();
                    form.CaptureSnapshot(snapPath, expandIdx);
                    form.Close();
                    Console.WriteLine($"Expanded snapshot saved to {snapPath}");
                    return 0;
                }
                if (a.Equals("--snapshot", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    string snapPath = args[++i];
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var form = new LauncherSelectorForm();
                    form.Show();
                    form.CaptureSnapshot(snapPath);
                    form.Close();
                    Console.WriteLine($"Snapshot saved to {snapPath}");
                    return 0;
                }
                if (a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    return 0;
                }
                if (a.Equals("--list-versions", StringComparison.OrdinalIgnoreCase) || a.Equals("--versions", StringComparison.OrdinalIgnoreCase))
                {
                    ListVersions();
                    return 0;
                }
                if ((a.Equals("--version", StringComparison.OrdinalIgnoreCase) || a.Equals("-v", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    versionArg = args[++i];
                }
                else if ((a.Equals("--game", StringComparison.OrdinalIgnoreCase) || a.Equals("-g", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    gameArg = args[++i].ToLowerInvariant();
                }
                else if ((a.Equals("--dir", StringComparison.OrdinalIgnoreCase) || a.Equals("-d", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    targetDir = Path.GetFullPath(args[++i]);
                }
                else if (a.Equals("--backup", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    doBackup = true;
                    targetDir = Path.GetFullPath(args[++i]);
                }
                else if (a.Equals("--binaries-only", StringComparison.OrdinalIgnoreCase))
                {
                    binariesOnly = true;
                }
                else if (a.Equals("--retro-launch", StringComparison.OrdinalIgnoreCase))
                {
                    doRetroLaunch = true;
                }
                else if (a.Equals("--native-launch", StringComparison.OrdinalIgnoreCase))
                {
                    doNativeLaunch = true;
                }
            }

            if (doNativeLaunch)
            {
                var item = !string.IsNullOrWhiteSpace(versionArg) ? LauncherCatalog.Resolve(versionArg) : LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo3);
                string launchDir = targetDir;
                if (!string.IsNullOrWhiteSpace(versionArg) && item != null)
                {
                    string candidateDir = Path.Combine(targetDir, item.FolderPrefix);
                    if (Directory.Exists(candidateDir)) launchDir = candidateDir;
                }
                Console.WriteLine($"Initiating Native Launcher execution in: {launchDir}");
                bool ok = NativeLauncherRunner.LaunchSync(launchDir, item, Console.WriteLine);
                return ok ? 0 : 1;
            }

            if (doRetroLaunch)
            {
                var item = !string.IsNullOrWhiteSpace(versionArg) ? LauncherCatalog.Resolve(versionArg) : LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo3);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RetroLauncherEmulator(targetDir, item));
                return 0;
            }

            using var http = new HttpClient();

            if (doBackup)
            {
                Console.WriteLine($"Initiating Cold Storage Bulk Backup to: {targetDir}");
                var items = LauncherCatalog.AllLaunchers;
                if (!gameArg.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    items = items.Where(x => x.Family.ToString().Equals(gameArg, StringComparison.OrdinalIgnoreCase) || x.ProductCode.Equals(gameArg, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                BackupEngine.ExecuteBulkArchiveAsync(
                    http,
                    targetDir,
                    items,
                    binariesOnly,
                    Console.WriteLine,
                    (pct, status) => Console.Write($"\rProgress: {pct:F1}% | {status,-60}"),
                    CancellationToken.None).GetAwaiter().GetResult();

                Console.WriteLine("\n[SUCCESS] Cold storage backup completed!");
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(versionArg))
            {
                var item = LauncherCatalog.Resolve(versionArg);
                if (item == null)
                {
                    Console.WriteLine($"[ERROR] Could not resolve launcher version or build '{versionArg}'.");
                    Console.WriteLine("Use --list-versions to display all catalog items.");
                    return 1;
                }

                Console.WriteLine($"Found Launcher: {item.DisplayTitle}");
                bool ok = LauncherDownloader.DownloadLauncherAsync(
                    http,
                    item,
                    targetDir,
                    binariesOnly,
                    Console.WriteLine,
                    (pct, status) => Console.Write($"\rProgress: {pct:F1}% | {status,-60}"),
                    CancellationToken.None).GetAwaiter().GetResult();

                Console.WriteLine();
                return ok ? 0 : 1;
            }

            // Default to running GUI if no specific command given
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherSelectorForm());
            return 0;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("USAGE:");
            Console.WriteLine("  BlizzLauncherArchive.exe [options]");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  --gui                          Launch the interactive dark-mode Windows Forms studio.");
            Console.WriteLine("  --list-versions, --versions    Print all available launcher builds across all game families.");
            Console.WriteLine("  --version, -v <id|ver|build>   Archive a specific launcher build by ID, version string, or build number.");
            Console.WriteLine("  --dir, -d <path>               Specify target directory (default: C:\\Games\\Blizzard_Launcher_Archive).");
            Console.WriteLine("  --backup <path>                Perform full cold-storage bulk archival of all catalog items to path.");
            Console.WriteLine("  --game, -g <family>            Filter by game family (BattleNet, Diablo3, WoW, StarCraft2, StarCraftBroodWar, Warcraft3, Diablo2, Hearthstone, all).");
            Console.WriteLine("  --binaries-only                Skip multi-megabyte SetupWin skin archives for instant fast deployment.");
            Console.WriteLine("  --shim [port]                  Start lightweight local Agent shim server for offline authentic launchers.");
            Console.WriteLine("  --patch <file>                 Apply Anti-AutoUpdate patch to bypass dead Blizzard update servers.");
            Console.WriteLine("  --retro-launch [build]         Open the interactive retro launcher player.");
            Console.WriteLine("  --native-launch [build]        Boot the authentic native Blizzard Launcher binary directly.");
            Console.WriteLine("  --help, -h                     Display this command-line reference.");
        }

        private static void ListVersions()
        {
            Console.WriteLine("Available Historical Blizzard Launchers:");
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine($"{"ID",-24} {"FAMILY",-12} {"VERSION",-12} {"BUILD",-8} {"RELEASED",-12} {"TITLE"}");
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            foreach (var b in LauncherCatalog.AllLaunchers)
            {
                Console.WriteLine($"{b.Id,-24} {b.Family,-12} {b.Version,-12} {b.BuildNumber,-8} {b.ReleaseDate,-12} {b.Title}");
            }
            Console.WriteLine("---------------------------------------------------------------------------------------------------");
            Console.WriteLine($"Total catalog builds: {LauncherCatalog.AllLaunchers.Count}");
        }
    }
}
