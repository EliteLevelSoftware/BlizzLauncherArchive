using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public sealed class BackupInventoryReport
    {
        public int TotalBuildsCount { get; set; }
        public int PresentBuildsCount { get; set; }
        public int NeededBuildsCount { get; set; }
        public long PresentBytes { get; set; }
        public long NeededBytes { get; set; }
        public List<LauncherItem> NeededBuilds { get; set; } = new();
        public List<LauncherItem> PresentBuilds { get; set; } = new();
    }

    public static class BackupEngine
    {
        public static BackupInventoryReport InspectDestination(string destinationRoot, IEnumerable<LauncherItem> catalog)
        {
            var report = new BackupInventoryReport();
            if (string.IsNullOrWhiteSpace(destinationRoot))
                return report;

            foreach (var item in catalog)
            {
                report.TotalBuildsCount++;
                string folder = Path.Combine(destinationRoot, $"{item.Family}_{item.Category}_{item.Version}_{item.BuildNumber}");
                string binaryPath = Path.Combine(folder, item.BinaryFileName);

                if (File.Exists(binaryPath) && new FileInfo(binaryPath).Length > 0)
                {
                    report.PresentBuildsCount++;
                    report.PresentBytes += new FileInfo(binaryPath).Length;
                    report.PresentBuilds.Add(item);
                }
                else
                {
                    report.NeededBuildsCount++;
                    report.NeededBytes += item.ApproxSizeBytes;
                    report.NeededBuilds.Add(item);
                }
            }
            return report;
        }

        public static async Task ExecuteBulkArchiveAsync(
            HttpClient http,
            string destinationRoot,
            IEnumerable<LauncherItem> itemsToArchive,
            bool binariesOnly,
            Action<string> log,
            Action<double, string> onProgress,
            CancellationToken ct)
        {
            Directory.CreateDirectory(destinationRoot);
            var list = new List<LauncherItem>(itemsToArchive);
            int total = list.Count;
            int current = 0;

            log("========================================================================");
            log(" BLIZZARD LAUNCHER ARCHIVE — COLD STORAGE BULK ARCHIVAL ENGINE");
            log($" Destination Root : {destinationRoot}");
            log($" Total Build Items : {total}");
            log($" Mode             : {(binariesOnly ? "Fast Binaries Only" : "Full Binaries + SetupWin MPQ Skins")}");
            log("========================================================================");

            foreach (var item in list)
            {
                ct.ThrowIfCancellationRequested();
                current++;
                double overallPct = (double)(current - 1) / total * 100.0;
                onProgress?.Invoke(overallPct, $"Archiving [{current}/{total}] {item.Title}...");

                log($"[{current}/{total}] Processing: {item.DisplayTitle}");
                await LauncherDownloader.DownloadLauncherAsync(
                    http,
                    item,
                    destinationRoot,
                    binariesOnly,
                    log,
                    (p, msg) =>
                    {
                        double itemPart = p / 100.0 * (100.0 / total);
                        onProgress?.Invoke(overallPct + itemPart, $"[{current}/{total}] {msg}");
                    },
                    ct);
            }

            // Generate Master Index Script in root folder
            GenerateMasterLauncherScript(destinationRoot, list);

            onProgress?.Invoke(100.0, "Bulk archival completed successfully!");
            log("========================================================================");
            log($" ALL {total} LAUNCHER BUILDS ARCHIVED & VERIFIED SUCCESSFULLY!");
            log($" Destination: {destinationRoot}");
            log("========================================================================");
        }

        private static void GenerateMasterLauncherScript(string rootDir, List<LauncherItem> items)
        {
            try
            {
                string batPath = Path.Combine(rootDir, "Browse-Launcher-Archive.bat");
                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("chcp 65001 >nul");
                sb.AppendLine("title Blizzard Launcher Archive — Master Catalog");
                sb.AppendLine("echo ========================================================================");
                sb.AppendLine("echo  BLIZZARD LAUNCHER ARCHIVE — COLD STORAGE MASTER CATALOG");
                sb.AppendLine("echo ========================================================================");
                sb.AppendLine("echo.");
                sb.AppendLine("echo Available Historical Launchers:");
                sb.AppendLine("echo.");

                int idx = 1;
                foreach (var item in items)
                {
                    string folder = $"{item.Family}_{item.Category}_{item.Version}_{item.BuildNumber}";
                    sb.AppendLine($"echo   [{idx,2}] {item.Family,-12} | {item.Title,-45} (v{item.Version})");
                    idx++;
                }

                sb.AppendLine("echo.");
                sb.AppendLine("echo Type the folder name or browse to any launcher directory to run it.");
                sb.AppendLine("pause");
                File.WriteAllText(batPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
