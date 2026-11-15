using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public sealed class UpdateInfo
    {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = "1.0.0";
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string PublishedAt { get; set; } = string.Empty;
    }

    public static class UpdateChecker
    {
        public const string CurrentVersion = "1.0.0";
        private const string ReleasesApiUrl = "https://api.github.com/repos/EliteLevelSoftware/BlizzLauncherArchive/releases/latest";

        public static async Task<UpdateInfo> CheckForUpdatesAsync(HttpClient http)
        {
            var info = new UpdateInfo { CurrentVersion = CurrentVersion };
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
                req.Headers.UserAgent.ParseAdd("BlizzLauncherArchive/1.0");

                var resp = await http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                    return info;

                string json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                string body = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                string published = root.TryGetProperty("published_at", out var p) ? p.GetString() ?? "" : "";
                string htmlUrl = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";

                string cleanLatest = tagName.TrimStart('v', 'V').Trim();
                info.LatestVersion = cleanLatest;
                info.ReleaseNotes = body;
                info.PublishedAt = published;
                info.DownloadUrl = htmlUrl;

                if (Version.TryParse(cleanLatest, out var latestVer) && Version.TryParse(CurrentVersion, out var currVer))
                {
                    if (latestVer > currVer)
                    {
                        info.UpdateAvailable = true;
                    }
                }
            }
            catch
            {
                // Silent fail on network/rate-limiting
            }
            return info;
        }
    }
}
