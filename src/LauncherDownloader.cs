using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public static class LauncherDownloader
    {
        public static async Task<bool> DownloadLauncherAsync(
            HttpClient http,
            LauncherItem item,
            string targetDir,
            bool binariesOnly,
            Action<string> log,
            Action<double, string> onProgress = null,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(targetDir);
            string destinationFolder = Path.Combine(targetDir, $"{item.Family}_{item.Category}_{item.Version}_{item.BuildNumber}");
            Directory.CreateDirectory(destinationFolder);

            log($"========================================================================");
            log($" ARCHIVING: {item.DisplayTitle}");
            log($" Target Folder   : {destinationFolder}");
            log($" File Size       : {item.ApproxSizeBytes / 1024.0 / 1024.0:F2} MB");
            log($" Source Category : {item.Family} / {item.Category}");
            log($"========================================================================");

            string exePath = Path.Combine(destinationFolder, item.BinaryFileName);

            // Step 1: Download the main Launcher binary
            if (File.Exists(exePath) && new FileInfo(exePath).Length > 0)
            {
                log($"[1/3] Launcher executable already exists locally: {item.BinaryFileName}");
                onProgress?.Invoke(35, "Binary verified locally...");
            }
            else
            {
                log($"[1/3] Downloading {item.BinaryFileName} from {item.DownloadUrl}...");
                onProgress?.Invoke(10, $"Downloading {item.BinaryFileName}...");
                await DownloadFileWithResumeAsync(http, item.DownloadUrl, exePath, item.ApproxSizeBytes, ct, onProgress);
                log($"      Successfully downloaded {item.BinaryFileName} ({new FileInfo(exePath).Length:N0} bytes).");
            }

            // Step 2: Download Skin MPQ / Support files if needed and requested
            if (!binariesOnly && item.RequiresSkinMpq && !string.IsNullOrWhiteSpace(item.SkinMpqUrl))
            {
                string skinPath = Path.Combine(destinationFolder, item.SkinMpqName);
                if (File.Exists(skinPath) && new FileInfo(skinPath).Length > 0)
                {
                    log($"[2/3] Skin archive already exists: {item.SkinMpqName}");
                    onProgress?.Invoke(70, "Skin archive verified locally...");
                }
                else
                {
                    log($"[2/3] Downloading UI skin archive {item.SkinMpqName} from {item.SkinMpqUrl}...");
                    onProgress?.Invoke(50, $"Downloading {item.SkinMpqName}...");
                    await DownloadFileWithResumeAsync(http, item.SkinMpqUrl, skinPath, -1, ct, onProgress);
                    log($"      Successfully archived UI skin {item.SkinMpqName} ({new FileInfo(skinPath).Length:N0} bytes).");
                }
            }
            else
            {
                log($"[2/3] Skipping auxiliary assets (Binaries-Only mode or standalone binary package).");
                onProgress?.Invoke(75, "Binaries ready...");
            }

            // Step 3: Compute SHA-256 and generate metadata / batch files
            log($"[3/3] Generating integrity manifests and portable launch scripts...");
            onProgress?.Invoke(90, "Writing metadata and manifests...");

            using var sha = SHA256.Create();
            byte[] fileBytes = await File.ReadAllBytesAsync(exePath, ct);
            string actualSha = Convert.ToHexString(sha.ComputeHash(fileBytes));

            string shaManifest = Path.Combine(destinationFolder, "SHA256SUMS.txt");
            await File.WriteAllTextAsync(shaManifest, $"{actualSha} *{item.BinaryFileName}\r\n", Encoding.UTF8, ct);

            string infoTxt = Path.Combine(destinationFolder, "VERSION_INFO.txt");
            var sbInfo = new StringBuilder();
            sbInfo.AppendLine("========================================================================");
            sbInfo.AppendLine($" Blizzard Launcher Archive — {item.Title}");
            sbInfo.AppendLine("========================================================================");
            sbInfo.AppendLine($"Family          : {item.Family}");
            sbInfo.AppendLine($"Category        : {item.Category}");
            sbInfo.AppendLine($"Version         : {item.Version}");
            sbInfo.AppendLine($"Build Number    : {item.BuildNumber}");
            sbInfo.AppendLine($"Release Date    : {item.ReleaseDate}");
            sbInfo.AppendLine($"Binary Name     : {item.BinaryFileName}");
            sbInfo.AppendLine($"SHA-256 Hash    : {actualSha}");
            sbInfo.AppendLine($"Archived Date   : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sbInfo.AppendLine($"Archive Source  : {item.DownloadUrl}");
            sbInfo.AppendLine($"Product Code    : {item.ProductCode}");
            sbInfo.AppendLine("------------------------------------------------------------------------");
            sbInfo.AppendLine("HISTORICAL CONTEXT & SUMMARY:");
            sbInfo.AppendLine(item.Summary);
            sbInfo.AppendLine("========================================================================");
            await File.WriteAllTextAsync(infoTxt, sbInfo.ToString(), Encoding.UTF8, ct);

            // Launcher DB for D3 / classic clients
            if (item.Family == LauncherGameFamily.Diablo3)
            {
                await File.WriteAllTextAsync(Path.Combine(destinationFolder, "Launcher.db"), "enUS", Encoding.ASCII, ct);
            }

            // Apply Anti-AutoUpdate Patch to prevent classic launchers hanging on dead update servers
            LauncherPatcher.PatchBinary(exePath, item, log);

            // Generate portable batch script
            string batPath = Path.Combine(destinationFolder, "Launch-Launcher.bat");
            var sbBat = new StringBuilder();
            sbBat.AppendLine("@echo off");
            sbBat.AppendLine("chcp 65001 >nul");
            sbBat.AppendLine($"title {item.Title} (Build {item.BuildNumber})");
            sbBat.AppendLine($"echo ========================================================================");
            sbBat.AppendLine($"echo  {item.Title} (Build {item.BuildNumber})");
            sbBat.AppendLine($"echo  Blizzard Launcher Archive — Offline Preservation Runner");
            sbBat.AppendLine($"echo ========================================================================");
            sbBat.AppendLine("echo Starting native launcher...");
            sbBat.AppendLine();
            sbBat.AppendLine(":: 1. Launch via BlizzLauncherArchive native engine (starts Agent shim & launches Qt client)");
            sbBat.AppendLine("if exist \"%~dp0BlizzLauncherArchive.exe\" (");
            sbBat.AppendLine($"    start \"\" \"%~dp0BlizzLauncherArchive.exe\" --native-launch --version {item.Id} --dir \"%~dp0\"");
            sbBat.AppendLine("    exit /b 0");
            sbBat.AppendLine(")");
            sbBat.AppendLine("if exist \"%~dp0..\\BlizzLauncherArchive.exe\" (");
            sbBat.AppendLine($"    start \"\" \"%~dp0..\\BlizzLauncherArchive.exe\" --native-launch --version {item.Id} --dir \"%~dp0\"");
            sbBat.AppendLine("    exit /b 0");
            sbBat.AppendLine(")");
            sbBat.AppendLine("if exist \"%~dp0..\\..\\BlizzLauncherArchive.exe\" (");
            sbBat.AppendLine($"    start \"\" \"%~dp0..\\..\\BlizzLauncherArchive.exe\" --native-launch --version {item.Id} --dir \"%~dp0\"");
            sbBat.AppendLine("    exit /b 0");
            sbBat.AppendLine(")");
            sbBat.AppendLine("if exist \"%~dp0..\\..\\release\\BlizzLauncherArchive.exe\" (");
            sbBat.AppendLine($"    start \"\" \"%~dp0..\\..\\release\\BlizzLauncherArchive.exe\" --native-launch --version {item.Id} --dir \"%~dp0\"");
            sbBat.AppendLine("    exit /b 0");
            sbBat.AppendLine(")");
            sbBat.AppendLine();
            sbBat.AppendLine(":: 2. Direct native Qt client fallback");
            sbBat.AppendLine("for /d %%D in (\"%ProgramData%\\Battle.net\\Client\\Blizzard Launcher.*\") do (");
            sbBat.AppendLine("    if exist \"%%D\\Blizzard Launcher.exe\" (");
            sbBat.AppendLine("        start \"\" /D \"%~dp0\" \"%%D\\Blizzard Launcher.exe\" --locale=enUS");
            sbBat.AppendLine("        exit /b 0");
            sbBat.AppendLine("    )");
            sbBat.AppendLine(")");
            sbBat.AppendLine();
            sbBat.AppendLine(":: 3. Bootstrapper fallback");
            sbBat.AppendLine($"start \"\" \"%~dp0{item.BinaryFileName}\"");
            await File.WriteAllTextAsync(batPath, sbBat.ToString(), Encoding.UTF8, ct);

            onProgress?.Invoke(100, "Complete!");
            log($"========================================================================");
            log($" SUCCESS: Archived {item.DisplayTitle}");
            log($" Location: {destinationFolder}");
            log($" SHA-256 : {actualSha}");
            log($"========================================================================");

            return true;
        }

        public static async Task DownloadFileWithResumeAsync(
            HttpClient http,
            string url,
            string targetPath,
            long expectedSize,
            CancellationToken ct,
            Action<double, string> onProgress = null)
        {
            string partPath = targetPath + ".part";
            long existingLen = 0;
            if (File.Exists(partPath))
            {
                existingLen = new FileInfo(partPath).Length;
            }

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingLen > 0)
            {
                req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLen, null);
            }

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                // Already complete or server doesn't like range
                File.Delete(partPath);
                existingLen = 0;
            }
            else
            {
                resp.EnsureSuccessStatusCode();
            }

            long totalBytes = resp.Content.Headers.ContentLength ?? expectedSize;
            if (resp.Content.Headers.ContentRange?.Length != null)
            {
                totalBytes = resp.Content.Headers.ContentRange.Length.Value;
            }

            using (var stream = await resp.Content.ReadAsStreamAsync(ct))
            using (var fs = new FileStream(partPath, existingLen > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[64 * 1024];
                long downloaded = existingLen;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read, ct);
                    downloaded += read;
                    if (totalBytes > 0 && onProgress != null)
                    {
                        double pct = Math.Min(100.0, (double)downloaded / totalBytes * 100.0);
                        onProgress(pct, $"Downloading {Path.GetFileName(targetPath)}: {pct:F1}%");
                    }
                }
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            File.Move(partPath, targetPath);
        }
    }
}
