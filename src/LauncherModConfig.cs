using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlizzLauncherArchive
{
    public class LauncherNewsEntry
    {
        public string Title { get; set; } = "";
        public string Date { get; set; } = "";
        public string Url { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    public class LauncherModConfig
    {
        public string ConfigVersion { get; set; } = "1.0.8";
        public string LauncherId { get; set; } = "d3_retail_16603";
        public string WindowTitle { get; set; } = "Diablo III";
        public string ServerName { get; set; } = "Sanctuary Private Server";
        public string ServerHost { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 1119;
        public int LocalWebFeedPort { get; set; } = 29505;
        public string PatchServerUrl { get; set; } = "http://127.0.0.1:29505/patch";
        public string BattleNetPortalUrl { get; set; } = "http://127.0.0.1:29505/d3/portal";

        public string TargetExecutable { get; set; } = "Diablo III.exe";
        public string LaunchArguments { get; set; } = "-launch -locale enUS";
        public string PlayButtonText { get; set; } = "PLAY";
        public string StatusText { get; set; } = "GAME IS UP TO DATE";
        public string TotalDownloadRemainingText { get; set; } = "TOTAL DOWNLOAD REMAINING: 0 MB (READY)";

        public string Stage1Title { get; set; } = "SETUP";
        public string Stage1SubText { get; set; } = "(755/933 MB)";
        public string Stage2Title { get; set; } = "AVAILABLE";
        public string Stage2SubText { get; set; } = "(1.57GB)";
        public string Stage3Title { get; set; } = "PLAYABLE";
        public string Stage3SubText { get; set; } = "(210MB)";
        public int ProgressPercentage { get; set; } = 100;

        public string NewsThumbnailFile { get; set; } = "Decompiled_Assets/web/news_thumb.png";
        public string CommunityThumbnailFile { get; set; } = "Decompiled_Assets/web/comm_thumb.png";
        public string LeftHeroArtworkFile { get; set; } = "Decompiled_Assets/web/diablo_hero.png";

        public List<LauncherNewsEntry> NewsItems { get; set; } = new List<LauncherNewsEntry>();
        public List<LauncherNewsEntry> CommunityItems { get; set; } = new List<LauncherNewsEntry>();

        public static string GetConfigPath(string launcherFolder)
        {
            return Path.Combine(launcherFolder, "launcher-config.json");
        }

        public static LauncherModConfig LoadOrCreate(string launcherFolder, LauncherItem item = null)
        {
            string path = GetConfigPath(launcherFolder);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<LauncherModConfig>(json);
                    if (loaded != null)
                        return loaded;
                }
                catch
                {
                }
            }

            var cfg = CreateDefault(item);
            cfg.Save(launcherFolder);
            return cfg;
        }

        public void Save(string launcherFolder)
        {
            try
            {
                Directory.CreateDirectory(launcherFolder);
                string path = GetConfigPath(launcherFolder);
                var opts = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, JsonSerializer.Serialize(this, opts));
            }
            catch
            {
            }
        }

        public static LauncherModConfig CreateDefault(LauncherItem item = null)
        {
            var cfg = new LauncherModConfig();
            if (item != null)
            {
                cfg.LauncherId = item.Id ?? "d3_retail_16603";
                cfg.WindowTitle = item.Title ?? "Diablo III";
                if (!string.IsNullOrWhiteSpace(item.BinaryFileName))
                {
                    cfg.TargetExecutable = item.Family == LauncherGameFamily.Diablo3 ? "Diablo III.exe" : item.BinaryFileName;
                }
            }

            cfg.NewsItems = new List<LauncherNewsEntry>
            {
                new LauncherNewsEntry
                {
                    Title = "Gold AH Added to Friends & Family",
                    Date = "Sep 16, 2011",
                    Url = "http://127.0.0.1:29505/d3/article/gold-ah",
                    Summary = "The Gold-based Auction House is now available for testing in the Friends & Family Beta realm. Test listing items, bidding, and stash delivery!"
                },
                new LauncherNewsEntry
                {
                    Title = "Diablo III Beta Patch 3",
                    Date = "Sep 14, 2011",
                    Url = "http://127.0.0.1:29505/d3/article/beta-patch-3",
                    Summary = "Beta Patch 3 (v0.3.0.7447) brings class skill tuning for Barbarian, Demon Hunter, Monk, Witch Doctor, and Wizard along with Launcher stability updates."
                },
                new LauncherNewsEntry
                {
                    Title = "The Shared Stash",
                    Date = "Sep 13, 2011",
                    Url = "http://127.0.0.1:29505/d3/article/shared-stash",
                    Summary = "Transfer weapons, armor, gems, and crafting materials effortlessly between all heroes on your Battle.net account using the new Shared Stash in New Tristram."
                },
                new LauncherNewsEntry
                {
                    Title = "Community Site Levels Up",
                    Date = "Sep 13, 2011",
                    Url = "http://127.0.0.1:29505/d3/article/community-site",
                    Summary = "Explore the interactive Skill Calculator, Follower systems, Artisan progression, and lore archives on the newly launched Diablo III community portal."
                }
            };

            cfg.CommunityItems = new List<LauncherNewsEntry>
            {
                new LauncherNewsEntry
                {
                    Title = "Beta Opt-in and FAQ",
                    Date = "Sep 8, 2011",
                    Url = "http://127.0.0.1:29505/d3/community/beta-faq",
                    Summary = "Learn how to configure your Battle.net System Check profile and connect to private or preservation test realms."
                },
                new LauncherNewsEntry
                {
                    Title = "BlizzCon Twitter Now Live",
                    Date = "Sep 8, 2011",
                    Url = "http://127.0.0.1:29505/d3/community/blizzcon-live",
                    Summary = "Follow real-time developer panels, Sanctuary lore Q&As, and tournament updates straight from Anaheim."
                },
                new LauncherNewsEntry
                {
                    Title = "BlizzCon Contest Signups Open",
                    Date = "Sep 8, 2011",
                    Url = "http://127.0.0.1:29505/d3/community/contests",
                    Summary = "Sign up for the Costume, Dance, and Fan Art competitions for a chance to win legendary prizes."
                },
                new LauncherNewsEntry
                {
                    Title = "Diablo Fan Art",
                    Date = "Sep 8, 2011",
                    Url = "http://127.0.0.1:29505/d3/community/fan-art",
                    Summary = "Behold the latest community-created illustrations of the Prime Evils, Tyrael, and the heroes of Sanctuary."
                }
            };

            return cfg;
        }
    }
}
