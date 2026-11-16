using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BlizzLauncherArchive
{
    public class DecompiledAssetEntry
    {
        public string VirtualPath { get; set; } = "";
        public string RelativeFilePath { get; set; } = "";
        public int BinaryDataOffset { get; set; }
        public int BinaryDataLength { get; set; }
        public bool IsZlibCompressed { get; set; }
        public bool IsQtResource { get; set; } = true;
        public string OriginalSha256 { get; set; } = "";
    }

    public static class LauncherAssetDecompiler
    {
        private const uint ImageBase = 0x400000;

        public static string GetDecompiledFolder(string launcherFolder)
        {
            return Path.Combine(launcherFolder, "Decompiled_Assets");
        }

        public static int DecompileLauncherAssets(string launcherFolder, LauncherItem item = null, Action<string> log = null)
        {
            if (string.IsNullOrWhiteSpace(launcherFolder))
                return 0;

            Directory.CreateDirectory(launcherFolder);
            string outDir = GetDecompiledFolder(launcherFolder);
            Directory.CreateDirectory(outDir);

            var config = LauncherModConfig.LoadOrCreate(launcherFolder, item);
            var manifest = new List<DecompiledAssetEntry>();
            int extractedCount = 0;

            // Locate native Blizzard Launcher binary (either in ProgramData or local folder)
            string nativeExe = FindBestSourceBinary(launcherFolder);
            if (!string.IsNullOrEmpty(nativeExe) && File.Exists(nativeExe))
            {
                try
                {
                    byte[] peBytes = File.ReadAllBytes(nativeExe);
                    extractedCount += ExtractQtResourceTrees(peBytes, outDir, manifest, log);
                    if (extractedCount == 0)
                    {
                        extractedCount += ExtractRawPngStreams(peBytes, outDir, manifest, log);
                    }
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[Decompiler] Warning scanning {Path.GetFileName(nativeExe)}: {ex.Message}");
                }
            }

            // Also scan standalone bootstrapper binary in launcherFolder if different
            foreach (string exeFile in Directory.GetFiles(launcherFolder, "*.exe", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(exeFile, nativeExe, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (exeFile.EndsWith("BlizzLauncherArchive.exe", StringComparison.OrdinalIgnoreCase) ||
                    exeFile.EndsWith("Blizzard Launcher Custom.exe", StringComparison.OrdinalIgnoreCase) ||
                    exeFile.EndsWith("Blizzard Launcher.exe", StringComparison.OrdinalIgnoreCase) ||
                    exeFile.EndsWith("Diablo III.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    byte[] bootBytes = File.ReadAllBytes(exeFile);
                    int bootPngs = ExtractRawPngStreams(bootBytes, Path.Combine(outDir, "bootstrapper"), manifest, log);
                    extractedCount += bootPngs;
                }
                catch
                {
                }
            }

            // Generate editable WebKit pages and thumbnails in Decompiled_Assets/web/
            GenerateEditableWebAssets(launcherFolder, outDir, config, log);

            // Save ASSET_MANIFEST.json for the Recompiler
            try
            {
                string manifestPath = Path.Combine(outDir, "ASSET_MANIFEST.json");
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, opts));
            }
            catch
            {
            }

            // Write helpful README_MODDING.txt alongside decompiled assets
            WriteModdingReadme(outDir);

            log?.Invoke($"[Decompiler] Extracted {extractedCount} embedded assets + editable web templates into {outDir}");
            return extractedCount;
        }

        public static string FindBestSourceBinary(string launcherFolder)
        {
            string progDataOrig = @"C:\ProgramData\Battle.net\Client\Blizzard Launcher.888\Blizzard Launcher.exe.orig";
            if (File.Exists(progDataOrig)) return progDataOrig;

            string localOrig = Path.Combine(launcherFolder, "Blizzard Launcher.exe.orig");
            if (File.Exists(localOrig)) return localOrig;

            string localNative = Path.Combine(launcherFolder, "Blizzard Launcher.exe");
            if (File.Exists(localNative)) return localNative;

            string progDataExe = @"C:\ProgramData\Battle.net\Client\Blizzard Launcher.888\Blizzard Launcher.exe";
            if (File.Exists(progDataExe)) return progDataExe;

            if (Directory.Exists(launcherFolder))
            {
                foreach (var f in Directory.GetFiles(launcherFolder, "*.exe"))
                {
                    if (!f.EndsWith("BlizzLauncherArchive.exe", StringComparison.OrdinalIgnoreCase))
                        return f;
                }
            }
            return null;
        }

        private static int VaToFileOffset(uint va)
        {
            if (va >= 0x401000 && va < 0x560000)
                return (int)(va - 0x400C00); // .text
            if (va >= 0x560000 && va < 0x900000)
                return (int)(va - 0x401000); // .rdata
            if (va >= 0x900000 && va < 0xA60000)
                return (int)(va - 0x900000 + 0x4FE600); // .data
            return -1;
        }

        private static uint ReadUInt32BE(byte[] buf, int offset)
        {
            return ((uint)buf[offset] << 24) |
                   ((uint)buf[offset + 1] << 16) |
                   ((uint)buf[offset + 2] << 8) |
                   buf[offset + 3];
        }

        private static ushort ReadUInt16BE(byte[] buf, int offset)
        {
            return (ushort)((buf[offset] << 8) | buf[offset + 1]);
        }

        private static int ExtractQtResourceTrees(byte[] pe, string outDir, List<DecompiledAssetEntry> manifest, Action<string> log)
        {
            int count = 0;
            var seenTrees = new HashSet<uint>();

            // Known Qt4 qRegisterResourceData tables in Blizzard Launcher.exe (Build 888 / D3 Retail & Beta lineage)
            (uint treeVa, uint nameVa, uint dataVa)[] knownTables =
            {
                (0x59F800u, 0x59F700u, 0x56E490u),
                (0x59F930u, 0x59F904u, 0x59F8F0u),
                (0x60EB60u, 0x60EB10u, 0x59F968u),
                (0x8ECC40u, 0x8EB7A8u, 0x60EB98u),
                (0x8F2838u, 0x8F2618u, 0x8ED238u)
            };

            foreach (var (treeVa, nameVa, dataVa) in knownTables)
            {
                int treeOff = VaToFileOffset(treeVa);
                int nameOff = VaToFileOffset(nameVa);
                int dataOff = VaToFileOffset(dataVa);
                if (treeOff > 0 && nameOff > 0 && dataOff > 0 &&
                    treeOff + 14 <= pe.Length && nameOff + 6 <= pe.Length && dataOff + 4 <= pe.Length &&
                    ReadUInt32BE(pe, treeOff) == 0 && ReadUInt16BE(pe, treeOff + 4) == 2 &&
                    seenTrees.Add(treeVa))
                {
                    count += WalkQtTreeNode(pe, treeOff, nameOff, dataOff, 0, "", outDir, manifest);
                }
            }

            // Also scan .text for direct push-immediate qRegisterResourceData calls in other Qt builds
            int limit = Math.Min(pe.Length - 24, 0x160000);
            for (int i = 0x400; i < limit; i++)
            {
                if (pe[i] == 0x68 && pe[i + 5] == 0x68 && pe[i + 10] == 0x68 &&
                    pe[i + 15] == 0x6A && pe[i + 16] == 0x01)
                {
                    uint dataVa = BitConverter.ToUInt32(pe, i + 1);
                    uint nameVa = BitConverter.ToUInt32(pe, i + 6);
                    uint treeVa = BitConverter.ToUInt32(pe, i + 11);

                    int treeOff = VaToFileOffset(treeVa);
                    int nameOff = VaToFileOffset(nameVa);
                    int dataOff = VaToFileOffset(dataVa);

                    if (treeOff > 0 && nameOff > 0 && dataOff > 0 &&
                        treeOff < pe.Length && nameOff < pe.Length && dataOff < pe.Length &&
                        seenTrees.Add(treeVa))
                    {
                        count += WalkQtTreeNode(pe, treeOff, nameOff, dataOff, 0, "", outDir, manifest);
                    }
                }
            }
            return count;
        }

        private static int WalkQtTreeNode(byte[] pe, int treeOff, int nameOff, int dataOff, uint nodeIdx, string prefix, string outDir, List<DecompiledAssetEntry> manifest)
        {
            int pos = treeOff + (int)(nodeIdx * 14);
            if (pos < 0 || pos + 14 > pe.Length)
                return 0;

            uint nameOffset = ReadUInt32BE(pe, pos);
            ushort flags = ReadUInt16BE(pe, pos + 4);
            string nodeName = "";

            if (nodeIdx != 0)
            {
                int np = nameOff + (int)nameOffset;
                if (np < 0 || np + 6 > pe.Length)
                    return 0;
                ushort charLen = ReadUInt16BE(pe, np);
                if (np + 6 + charLen * 2 > pe.Length)
                    return 0;
                var sb = new StringBuilder(charLen);
                for (int c = 0; c < charLen; c++)
                {
                    char ch = (char)ReadUInt16BE(pe, np + 6 + c * 2);
                    sb.Append(ch);
                }
                nodeName = sb.ToString();
            }

            string currentPath = string.IsNullOrEmpty(prefix) ? nodeName : (string.IsNullOrEmpty(nodeName) ? prefix : $"{prefix}/{nodeName}");
            int extracted = 0;

            if ((flags & 2) != 0) // Directory node
            {
                uint childCount = ReadUInt32BE(pe, pos + 6);
                uint firstChild = ReadUInt32BE(pe, pos + 10);
                if (childCount > 2000) return 0;
                for (uint c = 0; c < childCount; c++)
                {
                    extracted += WalkQtTreeNode(pe, treeOff, nameOff, dataOff, firstChild + c, currentPath, outDir, manifest);
                }
            }
            else // File node
            {
                uint dataNodeOffset = ReadUInt32BE(pe, pos + 10);
                int dp = dataOff + (int)dataNodeOffset;
                if (dp >= 0 && dp + 4 <= pe.Length)
                {
                    uint size = ReadUInt32BE(pe, dp);
                    if (size > 0 && size < 20_000_000 && dp + 4 + (int)size <= pe.Length)
                    {
                        byte[] rawPayload = new byte[size];
                        Buffer.BlockCopy(pe, dp + 4, rawPayload, 0, (int)size);

                        bool isCompressed = (flags & 1) != 0;
                        byte[] finalPayload = rawPayload;

                        if (isCompressed && rawPayload.Length > 6)
                        {
                            try
                            {
                                // Qt qCompress prepends a 4-byte big-endian uncompressed size followed by a 2-byte zlib header (78 9C / 78 DA)
                                using var msIn = new MemoryStream(rawPayload, 6, rawPayload.Length - 6);
                                using var deflate = new DeflateStream(msIn, CompressionMode.Decompress);
                                using var msOut = new MemoryStream();
                                deflate.CopyTo(msOut);
                                finalPayload = msOut.ToArray();
                            }
                            catch
                            {
                                finalPayload = rawPayload;
                            }
                        }

                        string relPath = currentPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                        if (string.IsNullOrWhiteSpace(relPath))
                            relPath = $"resource_{dp:X}.bin";

                        string fullOutPath = Path.Combine(outDir, relPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullOutPath));
                        if (File.Exists(fullOutPath))
                        {
                            // Keep the primary (enUS/default) resource and skip duplicate localized overrides
                            return 0;
                        }
                        File.WriteAllBytes(fullOutPath, finalPayload);

                        string shaHex = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(finalPayload));
                        manifest.Add(new DecompiledAssetEntry
                        {
                            VirtualPath = "/" + currentPath.TrimStart('/'),
                            RelativeFilePath = relPath.Replace('\\', '/'),
                            BinaryDataOffset = dp + 4,
                            BinaryDataLength = (int)size,
                            IsZlibCompressed = isCompressed,
                            IsQtResource = true,
                            OriginalSha256 = shaHex
                        });
                        extracted++;
                    }
                }
            }

            return extracted;
        }

        private static int ExtractRawPngStreams(byte[] pe, string outDir, List<DecompiledAssetEntry> manifest, Action<string> log)
        {
            byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            byte[] iendChunk = { 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };
            int count = 0;
            int pos = 0;

            while (pos < pe.Length - 16)
            {
                int start = FindBytes(pe, pngHeader, pos);
                if (start == -1) break;
                int end = FindBytes(pe, iendChunk, start + 8);
                if (end == -1 || end - start > 10_000_000)
                {
                    pos = start + 8;
                    continue;
                }

                int totalLen = (end - start) + iendChunk.Length;
                string relName = $"extracted_png/sprite_{count:D3}_0x{start:X}.png";
                string fullPath = Path.Combine(outDir, relName.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                byte[] pngBytes = new byte[totalLen];
                Buffer.BlockCopy(pe, start, pngBytes, 0, totalLen);
                if (!File.Exists(fullPath))
                {
                    File.WriteAllBytes(fullPath, pngBytes);
                }

                string shaHex = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pngBytes));
                manifest.Add(new DecompiledAssetEntry
                {
                    VirtualPath = "/" + relName,
                    RelativeFilePath = relName,
                    BinaryDataOffset = start,
                    BinaryDataLength = totalLen,
                    IsZlibCompressed = false,
                    IsQtResource = false,
                    OriginalSha256 = shaHex
                });

                count++;
                pos = start + totalLen;
            }
            return count;
        }

        private static int FindBytes(byte[] haystack, byte[] needle, int startIndex)
        {
            int end = haystack.Length - needle.Length;
            for (int i = startIndex; i <= end; i++)
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

        public static void GenerateEditableWebAssets(string launcherFolder, string decompiledDir, LauncherModConfig config, Action<string> log = null)
        {
            string webDir = Path.Combine(decompiledDir, "web");
            Directory.CreateDirectory(webDir);

            // 1. Generate News Thumbnail (52x52 glowing Archangel/Tyrael blue-gold icon matching 2011 D3 launcher)
            string newsThumbPath = Path.Combine(webDir, "news_thumb.png");
            if (!File.Exists(newsThumbPath))
            {
                CreateNewsThumbnail(newsThumbPath);
            }

            // 2. Generate Community Thumbnail (52x52 fiery Diablo III icon matching 2011 D3 launcher)
            string commThumbPath = Path.Combine(webDir, "comm_thumb.png");
            if (!File.Exists(commThumbPath))
            {
                CreateCommunityThumbnail(commThumbPath, decompiledDir);
            }

            // 3. Extract/generate Left Diablo Hero Artwork (diablo_hero.png) from decompiled bg_install.png or background.png
            string heroPath = Path.Combine(webDir, "diablo_hero.png");
            CreateDiabloHeroArtwork(heroPath, decompiledDir);

            // 4. Write editable news.html, community.html, patchnotes.html, forum.html, support.html
            string newsHtmlPath = Path.Combine(webDir, "news.html");
            File.WriteAllText(newsHtmlPath, BuildLauncherNewsHtml(config), Encoding.UTF8);

            string commHtmlPath = Path.Combine(webDir, "community.html");
            File.WriteAllText(commHtmlPath, BuildMockPortalHtml(config, "COMMUNITY PORTAL & REALM STATUS"), Encoding.UTF8);

            string patchHtmlPath = Path.Combine(webDir, "patchnotes.html");
            File.WriteAllText(patchHtmlPath, BuildMockPortalHtml(config, "OFFICIAL PATCH NOTES & SERVER CHANGELOG"), Encoding.UTF8);
        }

        private static void CreateNewsThumbnail(string outPath)
        {
            using var bmp = new Bitmap(52, 52, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, 52, 52), Color.FromArgb(8, 22, 42), Color.FromArgb(4, 8, 16), 90f))
            {
                g.FillRectangle(bg, 0, 0, 52, 52);
            }

            // Radiant angelic wings / Tyrael hood glow matching the Sep 2011 screenshot
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(6, 4, 40, 38);
                using var pgb = new PathGradientBrush(glow)
                {
                    CenterColor = Color.FromArgb(230, 145, 220, 255),
                    SurroundColors = new[] { Color.FromArgb(0, 20, 80, 160) }
                };
                g.FillPath(pgb, glow);
            }

            // Wing tendrils
            using (var wingPen = new Pen(Color.FromArgb(200, 180, 235, 255), 1.6f))
            {
                for (int i = 0; i < 4; i++)
                {
                    g.DrawBezier(wingPen, new Point(20, 22 + i * 2), new Point(10, 14 + i * 3), new Point(5, 10 + i * 4), new Point(2, 8 + i * 5));
                    g.DrawBezier(wingPen, new Point(32, 22 + i * 2), new Point(42, 14 + i * 3), new Point(47, 10 + i * 4), new Point(50, 8 + i * 5));
                }
            }

            // Hooded silhouette & chest armor
            using (var armorBrush = new SolidBrush(Color.FromArgb(240, 18, 26, 38)))
            {
                Point[] hood = { new Point(26, 10), new Point(18, 22), new Point(14, 48), new Point(38, 48), new Point(34, 22) };
                g.FillPolygon(armorBrush, hood);
            }
            using (var trimPen = new Pen(Color.FromArgb(220, 195, 165, 105), 1.4f))
            {
                g.DrawLine(trimPen, 26, 11, 20, 24);
                g.DrawLine(trimPen, 26, 11, 32, 24);
                g.DrawLine(trimPen, 19, 30, 33, 30);
            }

            using (var borderPen = new Pen(Color.FromArgb(255, 92, 72, 42), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, 51, 51);
            }

            bmp.Save(outPath, ImageFormat.Png);
        }

        private static void CreateCommunityThumbnail(string outPath, string decompiledDir)
        {
            using var bmp = new Bitmap(52, 52, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, 52, 52), Color.FromArgb(65, 14, 6), Color.FromArgb(14, 4, 2), 90f))
            {
                g.FillRectangle(bg, 0, 0, 52, 52);
            }

            // Hellfire glow
            using (var glow = new GraphicsPath())
            {
                glow.AddEllipse(4, 6, 44, 40);
                using var pgb = new PathGradientBrush(glow)
                {
                    CenterColor = Color.FromArgb(230, 255, 90, 20),
                    SurroundColors = new[] { Color.FromArgb(0, 90, 10, 0) }
                };
                g.FillPath(pgb, glow);
            }

            // Three vertical fiery pillars ("III")
            using (var pillarBrush = new LinearGradientBrush(new Rectangle(0, 8, 52, 36), Color.FromArgb(255, 255, 220, 110), Color.FromArgb(240, 200, 30, 5), 90f))
            {
                g.FillRectangle(pillarBrush, 16, 11, 4, 30);
                g.FillRectangle(pillarBrush, 24, 8, 4, 36);
                g.FillRectangle(pillarBrush, 32, 11, 4, 30);
            }

            using (var f = new Font("Georgia", 6.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Color.FromArgb(245, 245, 215, 160)))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("DIABLO", f, tb, new RectangleF(0, 20, 52, 14), sf);
            }

            using (var borderPen = new Pen(Color.FromArgb(255, 92, 72, 42), 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, 51, 51);
            }

            bmp.Save(outPath, ImageFormat.Png);
        }

        private static void CreateDiabloHeroArtwork(string outPath, string decompiledDir)
        {
            // First try extracting the authentic left-side fiery Diablo artwork from embedded d3_retro_launcher.png
            try
            {
                using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("d3_retro_launcher.png");
                if (stream != null)
                {
                    using var master = new Bitmap(stream);
                    using var dest = new Bitmap(726, 295, PixelFormat.Format32bppArgb);
                    using var g = Graphics.FromImage(dest);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    // Copy the webView interior region (left Diablo artwork + warm dark parchment right background)
                    g.DrawImage(master, new Rectangle(0, 0, 726, 295), new Rectangle(46, 114, 726, 295), GraphicsUnit.Pixel);
                    // Clean the right-hand text area (x=305..726) with a smooth dark parchment gradient so live HTML News/Community text & thumbnails render crisp without double-text
                    var rightRect = new Rectangle(300, 0, 426, 295);
                    using (var rightGrad = new LinearGradientBrush(rightRect, Color.FromArgb(245, 28, 11, 7), Color.FromArgb(255, 18, 7, 5), 0f))
                    {
                        g.FillRectangle(rightGrad, new Rectangle(305, 0, 421, 295));
                    }
                    dest.Save(outPath, ImageFormat.Png);
                    return;
                }
            }
            catch
            {
            }

            string bgInstall = Path.Combine(decompiledDir, "d3", "bg_install.png");
            if (!File.Exists(bgInstall))
                bgInstall = Path.Combine(decompiledDir, "bg_install.png");
            if (File.Exists(bgInstall))
            {
                try
                {
                    using var src = new Bitmap(bgInstall);
                    using var dest = new Bitmap(726, 295, PixelFormat.Format32bppArgb);
                    using var g = Graphics.FromImage(dest);
                    g.DrawImage(src, new Rectangle(0, 0, 726, 295), new Rectangle(29, 156, Math.Min(726, src.Width - 29), Math.Min(295, src.Height - 156)), GraphicsUnit.Pixel);
                    dest.Save(outPath, ImageFormat.Png);
                }
                catch { }
            }
        }

        public static string BuildLauncherNewsHtml(LauncherModConfig config)
        {
            int port = config?.LocalWebFeedPort > 0 ? config.LocalWebFeedPort : 29505;
            string baseUrl = $"http://127.0.0.1:{port}";

            var newsSb = new StringBuilder();
            if (config?.NewsItems != null)
            {
                foreach (var n in config.NewsItems)
                {
                    string url = string.IsNullOrWhiteSpace(n.Url) ? $"{baseUrl}/d3/news" : n.Url;
                    newsSb.AppendLine($"        <li><a href=\"{EscapeHtml(url)}\">&bull; {EscapeHtml(n.Title)}</a><span class=\"date\">{EscapeHtml(n.Date)}</span></li>");
                }
            }

            var commSb = new StringBuilder();
            if (config?.CommunityItems != null)
            {
                foreach (var c in config.CommunityItems)
                {
                    string url = string.IsNullOrWhiteSpace(c.Url) ? $"{baseUrl}/d3/community" : c.Url;
                    commSb.AppendLine($"        <li><a href=\"{EscapeHtml(url)}\">&bull; {EscapeHtml(c.Title)}</a><span class=\"date\">{EscapeHtml(c.Date)}</span></li>");
                }
            }

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
html, body {{
    margin: 0;
    padding: 0;
    width: 726px;
    height: 295px;
    background: #120604 url('{baseUrl}/d3/assets/diablo_hero.png') no-repeat left top;
    color: #c79c6e;
    font-family: Georgia, 'Times New Roman', serif;
    font-size: 12px;
    overflow: hidden;
    user-select: none;
    -webkit-user-select: none;
}}
.right-column {{
    margin-left: 318px;
    padding: 34px 16px 10px 0;
    width: 390px;
}}
.section {{
    margin-bottom: 14px;
    clear: both;
}}
.section-header {{
    border-bottom: 1px solid #4a2a16;
    padding-bottom: 3px;
    margin-bottom: 7px;
    height: 20px;
}}
.section-title {{
    color: #f3e6d0;
    font-size: 17px;
    font-weight: normal;
    float: left;
    letter-spacing: 0.3px;
    text-shadow: 0 1px 2px #000;
}}
.view-all {{
    float: right;
    color: #d8621c;
    font-family: Arial, sans-serif;
    font-size: 11px;
    font-weight: bold;
    text-decoration: none;
    margin-top: 4px;
}}
.view-all:hover {{
    color: #ff944d;
    text-decoration: underline;
}}
.section-body {{
    position: relative;
    min-height: 64px;
}}
.thumb-box {{
    float: left;
    width: 52px;
    height: 52px;
    border: 1px solid #694926;
    background: #0a0503;
    box-shadow: 0 0 6px rgba(0,0,0,0.85);
    margin-right: 10px;
    margin-top: 2px;
}}
.thumb-box img {{
    width: 52px;
    height: 52px;
    display: block;
    border: 0;
}}
.items {{
    list-style: none;
    margin: 0 0 0 64px;
    padding: 0;
}}
.items li {{
    line-height: 16px;
    font-family: Arial, sans-serif;
    font-size: 11px;
    color: #9e7f56;
    white-space: nowrap;
    overflow: hidden;
}}
.items li a {{
    color: #9e7f56;
    text-decoration: none;
}}
.items li a:hover {{
    color: #f5d293;
    text-decoration: underline;
}}
.items li span.date {{
    float: right;
    color: #79603e;
    font-size: 11px;
    padding-left: 8px;
}}
</style>
</head>
<body>
  <div class=""right-column"">
    <div class=""section"">
      <div class=""section-header"">
        <span class=""section-title"">News</span>
        <a class=""view-all"" href=""{baseUrl}/d3/news"">&gt; View All</a>
      </div>
      <div class=""section-body"">
        <a class=""thumb-box"" href=""{baseUrl}/d3/news""><img src=""{baseUrl}/d3/assets/news_thumb.png"" alt=""News""></a>
        <ul class=""items"">
{newsSb}        </ul>
      </div>
    </div>
    <div class=""section"">
      <div class=""section-header"">
        <span class=""section-title"">Community</span>
        <a class=""view-all"" href=""{baseUrl}/d3/community"">&gt; View All</a>
      </div>
      <div class=""section-body"">
        <a class=""thumb-box"" href=""{baseUrl}/d3/community""><img src=""{baseUrl}/d3/assets/comm_thumb.png"" alt=""Community""></a>
        <ul class=""items"">
{commSb}        </ul>
      </div>
    </div>
  </div>
</body>
</html>";
        }

        public static string BuildMockPortalHtml(LauncherModConfig config, string heading)
        {
            int port = config?.LocalWebFeedPort > 0 ? config.LocalWebFeedPort : 29505;
            string baseUrl = $"http://127.0.0.1:{port}";
            var sb = new StringBuilder();
            if (config?.NewsItems != null)
            {
                foreach (var n in config.NewsItems)
                {
                    sb.AppendLine($"<div class='card'><h3>{EscapeHtml(n.Title)} <span>{EscapeHtml(n.Date)}</span></h3><p>{EscapeHtml(n.Summary)}</p></div>");
                }
            }
            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<style>
body {{ margin:0; padding:14px 20px; background:#140805; color:#d8b98a; font-family:Georgia,serif; font-size:12px; }}
.top {{ border-bottom:1px solid #5c361a; padding-bottom:6px; margin-bottom:10px; }}
.top h2 {{ margin:0; font-size:16px; color:#f5e1bf; display:inline; }}
.back {{ float:right; color:#ff8533; text-decoration:none; font-family:Arial,sans-serif; font-size:11px; font-weight:bold; }}
.card {{ background:#1f0d08; border:1px solid #452613; padding:8px 12px; margin-bottom:8px; }}
.card h3 {{ margin:0 0 4px 0; font-size:13px; color:#f3c677; font-family:Arial,sans-serif; }}
.card h3 span {{ float:right; color:#8d6e46; font-size:11px; font-weight:normal; }}
.card p {{ margin:0; color:#b89768; font-family:Arial,sans-serif; font-size:11px; line-height:1.4; }}
</style>
</head>
<body>
  <div class=""top"">
    <h2>{EscapeHtml(heading)} — {EscapeHtml(config?.ServerName ?? "Sanctuary Private Server")}</h2>
    <a class=""back"" href=""{baseUrl}/D3/enUS/launcher/news"">&laquo; Back to Launcher News</a>
  </div>
  {sb}
</body>
</html>";
        }

        private static void WriteModdingReadme(string outDir)
        {
            string readme = Path.Combine(outDir, "README_MODDING.txt");
            if (File.Exists(readme)) return;
            File.WriteAllText(readme, @"================================================================================
BLIZZARD LAUNCHER ARCHIVE — DECOMPILED ASSETS & PRIVATE SERVER MODDING GUIDE
================================================================================

This folder (Decompiled_Assets) contains the decompiled resources extracted directly
from the historical Blizzard Launcher binary, organized for preservation and modding:

1. UI Textures & Button States (Decompiled_Assets/*.png, d3/*.png, bnet/*.png):
   - background.png / bg_install.png / bg_winged_diablo.png : Main gothic launcher frames
   - btn_large_off.png / btn_large_over.png / btn_large_down.png : PLAY button states
   - btn_close_off.png / btn_close_over.png / btn_close_down.png : Top-right Close button
   - btn_min_off.png / btn_min_over.png / btn_min_down.png       : Top-right Minimize button
   - progress_bar_complete.png / progress_pointer_*.png          : 3-stage streaming bar

2. Decompressed Qt4 UI Layouts (Decompiled_Assets/*.ui):
   - progress_screen.ui : Main 820x650 launcher screen layout (webView, playButton, stages)
   - main_window.ui     : Outer window and OPTIONS / INFO / HELP menu bar
   - welcome_screen.ui, install_screen.ui, system_check_dialog.ui, account_dialog.ui

3. Editable WebKit News & Private Server Feed (Decompiled_Assets/web/):
   - news.html          : Live HTML/CSS rendered inside the launcher's News & Community view
   - news_thumb.png     : 52x52 News thumbnail icon
   - comm_thumb.png     : 52x52 Community thumbnail icon
   - diablo_hero.png    : Left-side Diablo hero artwork

4. Private Server Configuration (..\launcher-config.json):
   - Edit launcher-config.json (or use the built-in Mod Studio / Recompiler in
     BlizzLauncherArchive) to customize News headlines, dates, links, Target Executable,
     Private Server host/port, and progress bar stages, then click 'Recompile Launcher'!
");
        }

        private static string EscapeHtml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
