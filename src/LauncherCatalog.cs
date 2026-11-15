using System;
using System.Collections.Generic;
using System.Linq;

namespace BlizzLauncherArchive
{
    public enum LauncherGameFamily
    {
        BattleNet,
        Diablo3,
        WoW,
        StarCraft2,
        StarCraftBroodWar,
        Warcraft3,
        Diablo2,
        Hearthstone
    }

    public enum LauncherCategory
    {
        Retail,
        Beta,
        PTR,
        Expansion,
        Rebrand,
        Agent
    }

    public enum CdnAvailability
    {
        Checking,
        Ok,
        Archive,
        Offline
    }

    public sealed class LauncherItem
    {
        public string Id { get; init; } = string.Empty;
        public LauncherGameFamily Family { get; init; }
        public LauncherCategory Category { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string BuildNumber { get; init; } = string.Empty;
        public string ReleaseDate { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string BinaryFileName { get; init; } = string.Empty;
        public string DownloadUrl { get; init; } = string.Empty;
        public string SupportFilesUrl { get; init; } = string.Empty;
        public long ApproxSizeBytes { get; init; }
        public string Sha256 { get; init; } = string.Empty;
        public bool RequiresSkinMpq { get; init; } = false;
        public string SkinMpqName { get; init; } = string.Empty;
        public string SkinMpqUrl { get; init; } = string.Empty;
        public string ProductCode { get; init; } = string.Empty;
        public bool IsTactCasc { get; init; } = false;
        public string BuildConfigHash { get; init; } = string.Empty;
        public string CdnConfigHash { get; init; } = string.Empty;

        public string GameName => Family switch
        {
            LauncherGameFamily.BattleNet => "Battle.net Desktop App",
            LauncherGameFamily.Diablo3 => "Diablo III",
            LauncherGameFamily.WoW => "World of Warcraft",
            LauncherGameFamily.StarCraft2 => "StarCraft II",
            LauncherGameFamily.StarCraftBroodWar => "StarCraft: Brood War",
            LauncherGameFamily.Warcraft3 => "Warcraft III",
            LauncherGameFamily.Diablo2 => "Diablo II",
            LauncherGameFamily.Hearthstone => "Hearthstone",
            _ => Family.ToString()
        };

        public string DisplayTitle => $"{Title} (v{Version} Build {BuildNumber})";
        public override string ToString() => DisplayTitle;
    }

    public static class LauncherCatalog
    {
        public static readonly IReadOnlyList<LauncherItem> AllLaunchers = new List<LauncherItem>
        {
            // =========================================================================
            // 1. DIABLO III STANDALONE LAUNCHERS (2011 - 2014)
            // =========================================================================
            new()
            {
                Id = "d3-retail-1.0.8a",
                Family = LauncherGameFamily.Diablo3,
                Category = LauncherCategory.Retail,
                Title = "Diablo III Retail Standalone Launcher",
                Version = "1.1.0.1272",
                BuildNumber = "16603",
                ReleaseDate = "2012-12-25",
                Summary = "Final production standalone Diablo III launcher used across retail patches 1.0.1 through 1.0.8a before the Battle.net transition.",
                BinaryFileName = "Diablo III Launcher.exe",
                DownloadUrl = "https://archive.org/download/diablo-iii-1-0-8-16603/Diablo%20III%201.0.8.16603.zip/Diablo%20III%20Launcher.exe",
                ApproxSizeBytes = 2043584,
                Sha256 = "66959555D2936232CEDEA7014139989FE8F06E0164CCC77D5CDC356580191E56",
                RequiresSkinMpq = true,
                SkinMpqName = "SetupWin.mpq",
                SkinMpqUrl = "https://archive.org/download/diablo-iii-1-0-8-16603/Diablo%20III%201.0.8.16603.zip/SetupWin.mpq",
                ProductCode = "d3"
            },
            new()
            {
                Id = "d3-ros-beta-21962",
                Family = LauncherGameFamily.Diablo3,
                Category = LauncherCategory.Beta,
                Title = "Diablo III: Reaper of Souls Beta Launcher",
                Version = "1.4.0.1820",
                BuildNumber = "21962",
                ReleaseDate = "2014-02-20",
                Summary = "Official Reaper of Souls Public Test & Closed Beta standalone launcher featuring Malthael gothic Westmarch art.",
                BinaryFileName = "Diablo III Public Test Launcher.exe",
                DownloadUrl = "https://archive.org/download/diablo-iii-reaper-of-souls-beta-2-0-0-21962/Diablo%20III%20Reaper%20of%20Souls%20Beta%202.0.0.21962.zip/Diablo%20III%20Public%20Test%20Launcher.exe",
                ApproxSizeBytes = 2848304,
                Sha256 = "979D84AA03166415A8C80A60F41B12F730026FD534FBBF79C2349C64A6BDCBBB",
                RequiresSkinMpq = true,
                SkinMpqName = "SetupWin.mpq",
                SkinMpqUrl = "https://archive.org/download/diablo-iii-1-0-8-16603/Diablo%20III%201.0.8.16603.zip/SetupWin.mpq",
                ProductCode = "d3b"
            },
            new()
            {
                Id = "d3-open-beta-9359",
                Family = LauncherGameFamily.Diablo3,
                Category = LauncherCategory.Beta,
                Title = "Diablo III Open Beta Weekend Launcher",
                Version = "1.0.0.729",
                BuildNumber = "9359",
                ReleaseDate = "2012-04-19",
                Summary = "The legendary Open Beta stress test launcher used by over 2 million players worldwide on April 20-23, 2012.",
                BinaryFileName = "Diablo III Beta Launcher.exe",
                DownloadUrl = "https://archive.org/download/diablo-iii-beta-0-11-0-9359/Diablo%20III%20Beta%200.11.0.9359.zip/Diablo%20III%20Beta%20Launcher.exe",
                ApproxSizeBytes = 1862224,
                Sha256 = "47D640B35004B8736CDA9E1979AECF68B956323358752F5F42F2F0F87C71D46A",
                RequiresSkinMpq = true,
                SkinMpqName = "SetupWin.mpq",
                SkinMpqUrl = "https://archive.org/download/diablo-iii-1-0-8-16603/Diablo%20III%201.0.8.16603.zip/SetupWin.mpq",
                ProductCode = "d3b"
            },
            new()
            {
                Id = "d3-ff-beta-7447",
                Family = LauncherGameFamily.Diablo3,
                Category = LauncherCategory.Beta,
                Title = "Diablo III Friends & Family Beta Launcher",
                Version = "1.0.0.365",
                BuildNumber = "7447",
                ReleaseDate = "2011-09-27",
                Summary = "The very first public Diablo III beta client launcher from late 2011 featuring the debut of streaming MPQ downloads.",
                BinaryFileName = "Diablo III Beta Launcher.exe",
                DownloadUrl = "https://archive.org/download/diablo-iii-beta-0-3-0-7447/Diablo%20III%20Beta%200.3.0.7447.zip/Diablo%20III%20Beta%20Launcher.exe",
                ApproxSizeBytes = 1681920,
                Sha256 = "C0DB40D99DFBEFED85BD4679820CDA58A4848890DAB11DF25ADA568574671B28",
                RequiresSkinMpq = true,
                SkinMpqName = "SetupWin.mpq",
                SkinMpqUrl = "https://archive.org/download/diablo-iii-1-0-8-16603/Diablo%20III%201.0.8.16603.zip/SetupWin.mpq",
                ProductCode = "d3b"
            },

            // =========================================================================
            // 2. BATTLE.NET DESKTOP APPLICATION (`bna` & `agent`)
            // =========================================================================
            new()
            {
                Id = "bnet-2.53-live",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Retail,
                Title = "Battle.net 2.0 Live Retail Client",
                Version = "2.53.8",
                BuildNumber = "17986",
                ReleaseDate = "2026-11-01",
                Summary = "Current live generation Battle.net Desktop Application with modern multi-game tab navigation and social chat.",
                BinaryFileName = "Battle.net.exe",
                DownloadUrl = "http://us.patch.battle.net:1119/bna/versions",
                ApproxSizeBytes = 185000000,
                IsTactCasc = true,
                ProductCode = "bna",
                BuildConfigHash = "4b9c77d7485667dd03c1582a0ff2bc20",
                CdnConfigHash = "437350dabc4e4e39b09adc1ebaf4b9ac"
            },
            new()
            {
                Id = "bnet-2.39-milestone",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Retail,
                Title = "Battle.net 2.0 Milestone Client",
                Version = "2.39.0",
                BuildNumber = "15212",
                ReleaseDate = "2024-06-15",
                Summary = "Battle.net 2.0 milestone release featuring refined downloads manager, game library shelves, and Discord rich presence.",
                BinaryFileName = "Battle.net.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup_202411/Battle.net-Setup.exe",
                ApproxSizeBytes = 4833880,
                ProductCode = "bna",
                Sha256 = "6F86F58F10B7B96F525A4161C7C1DFF4F6B9DFD26A97CC70559BD1A22BC7B965"
            },
            new()
            {
                Id = "bnet-2.0-launch",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Retail,
                Title = "Battle.net 2.0 Debut Client",
                Version = "2.0.0",
                BuildNumber = "12694",
                ReleaseDate = "2021-01-14",
                Summary = "The landmark Battle.net 2.0 overhaul: complete redesign with top horizontal navigation, customizable favorites, and unified chat.",
                BinaryFileName = "Battle.net-Setup.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup_202411/Battle.net-Setup.exe",
                ApproxSizeBytes = 4833880,
                ProductCode = "bna"
            },
            new()
            {
                Id = "bnet-1.30-classic-final",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Retail,
                Title = "Battle.net 1.0 Final Classic Client",
                Version = "1.30.2",
                BuildNumber = "11890",
                ReleaseDate = "2020-10-28",
                Summary = "The final and most refined build of the classic Battle.net 1.0 vertical left-rail layout before the 2.0 overhaul.",
                BinaryFileName = "Battle.net-Setup.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup_202208/Battle.net-Setup.exe",
                ApproxSizeBytes = 4833880,
                ProductCode = "bna"
            },
            new()
            {
                Id = "bnet-blizzard-app-rebrand",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Rebrand,
                Title = "Blizzard App (Rebrand Era Client)",
                Version = "1.9.1",
                BuildNumber = "9288",
                ReleaseDate = "2017-08-14",
                Summary = "Historical milestone when Blizzard rebranded Battle.net into 'Blizzard App' before restoring the classic Battle.net brand.",
                BinaryFileName = "Blizzard-App-Setup.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup/Battle.net-Setup.exe",
                ApproxSizeBytes = 4833880,
                ProductCode = "bna"
            },
            new()
            {
                Id = "bnet-1.0-retail-launch",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Retail,
                Title = "Battle.net 1.0 Official Retail Launch",
                Version = "1.0.8",
                BuildNumber = "4412",
                ReleaseDate = "2014-03-25",
                Summary = "Worldwide production debut of the Battle.net Desktop Application coinciding with Diablo III: Reaper of Souls launch day.",
                BinaryFileName = "Battle.net-Setup.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup/Battle.net-Setup.exe",
                ApproxSizeBytes = 4833880,
                ProductCode = "bna"
            },
            new()
            {
                Id = "bnet-agent-live",
                Family = LauncherGameFamily.BattleNet,
                Category = LauncherCategory.Agent,
                Title = "Battle.net Update Agent (Live Engine)",
                Version = "2.42.0",
                BuildNumber = "9873",
                ReleaseDate = "2026-11-01",
                Summary = "The core background IPC service that powers installation, checksum verification, and CDN streaming across all Blizzard clients.",
                BinaryFileName = "Agent.exe",
                DownloadUrl = "http://us.patch.battle.net:1119/agent/versions",
                ApproxSizeBytes = 35000000,
                IsTactCasc = true,
                ProductCode = "agent",
                BuildConfigHash = "cd4e140cbe7dfab6340fea92e2321421",
                CdnConfigHash = "e897e78d6ea3f02dcce0f61aa688093a"
            },

            // =========================================================================
            // 3. WORLD OF WARCRAFT STANDALONE LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "wow-mop-5.4.8",
                Family = LauncherGameFamily.WoW,
                Category = LauncherCategory.Expansion,
                Title = "World of Warcraft: Mists of Pandaria Launcher",
                Version = "5.4.8",
                BuildNumber = "18414",
                ReleaseDate = "2014-06-24",
                Summary = "The final standalone World of Warcraft game launcher with streaming background downloader before mandatory BNet desktop migration.",
                BinaryFileName = "World of Warcraft Launcher.exe",
                DownloadUrl = "https://archive.org/download/WoW_4.3.4-15595_Multi/WoW_4.3.4-15595_Multi_archive.torrent",
                ApproxSizeBytes = 2850000,
                ProductCode = "wow"
            },
            new()
            {
                Id = "wow-cata-4.3.4",
                Family = LauncherGameFamily.WoW,
                Category = LauncherCategory.Expansion,
                Title = "World of Warcraft: Cataclysm Launcher",
                Version = "4.3.4",
                BuildNumber = "15595",
                ReleaseDate = "2012-04-17",
                Summary = "Iconic Deathwing Cataclysm launcher featuring the 3-stage streaming technology and patch updater.",
                BinaryFileName = "Launcher.exe",
                DownloadUrl = "https://archive.org/download/WoW_4.3.4-15595_Multi/WoW_4.3.4-15595_Multi_archive.torrent",
                ApproxSizeBytes = 2450000,
                ProductCode = "wow"
            },
            new()
            {
                Id = "wow-wotlk-3.3.5a",
                Family = LauncherGameFamily.WoW,
                Category = LauncherCategory.Expansion,
                Title = "World of Warcraft: Wrath of the Lich King Launcher",
                Version = "3.3.5a",
                BuildNumber = "12340",
                ReleaseDate = "2010-06-29",
                Summary = "Legendary standalone launcher featuring the Lich King Arthas Menethil, patch downloader, and realm newsfeed.",
                BinaryFileName = "Launcher.exe",
                DownloadUrl = "https://archive.org/download/wow-wotlk-3.3.5a.12340-eses-slim-play-only/WoW-WotLK-3.3.5a.12340-esES-Slim-Play-Only-v4-EXPERIMENTAL.iso",
                ApproxSizeBytes = 2100000,
                ProductCode = "wow"
            },
            new()
            {
                Id = "wow-tbc-2.4.3",
                Family = LauncherGameFamily.WoW,
                Category = LauncherCategory.Expansion,
                Title = "World of Warcraft: The Burning Crusade Launcher",
                Version = "2.4.3",
                BuildNumber = "8606",
                ReleaseDate = "2008-07-15",
                Summary = "Classic The Burning Crusade Dark Portal launcher with server status checks and Blizzard Downloader integration.",
                BinaryFileName = "Launcher.exe",
                DownloadUrl = "https://archive.org/download/wow-wotlk-3.3.5a.12340-eses-slim-play-only/WoW-WotLK-3.3.5a.12340-esES-Slim-Play-Only-v4-EXPERIMENTAL.iso",
                ApproxSizeBytes = 1950000,
                ProductCode = "wow"
            },
            new()
            {
                Id = "wow-vanilla-1.12.1",
                Family = LauncherGameFamily.WoW,
                Category = LauncherCategory.Retail,
                Title = "World of Warcraft Vanilla Classic Launcher",
                Version = "1.12.1",
                BuildNumber = "5875",
                ReleaseDate = "2006-09-26",
                Summary = "The original classic 2004-2006 World of Warcraft launcher executable for Drums of War.",
                BinaryFileName = "Launcher.exe",
                DownloadUrl = "https://archive.org/download/wow-wotlk-3.3.5a.12340-eses-slim-play-only/WoW-WotLK-3.3.5a.12340-esES-Slim-Play-Only-v4-EXPERIMENTAL.iso",
                ApproxSizeBytes = 1650000,
                ProductCode = "wow"
            },

            // =========================================================================
            // 4. STARCRAFT II LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "sc2-hots-2.1",
                Family = LauncherGameFamily.StarCraft2,
                Category = LauncherCategory.Expansion,
                Title = "StarCraft II: Heart of the Swarm Launcher",
                Version = "2.1.4",
                BuildNumber = "32283",
                ReleaseDate = "2014-09-23",
                Summary = "Standalone Heart of the Swarm launcher with Kerrigan artwork, patch manager, and streaming asset pipeline.",
                BinaryFileName = "StarCraft II Launcher.exe",
                DownloadUrl = "https://archive.org/download/starcraft-ii-heart-of-the-swarm-2012-blizzard-dvd/StarCraft%20II%20Heart%20of%20the%20Swarm%202012%20Blizzard%20DVD.iso",
                ApproxSizeBytes = 2650000,
                ProductCode = "s2"
            },
            new()
            {
                Id = "sc2-wol-1.5",
                Family = LauncherGameFamily.StarCraft2,
                Category = LauncherCategory.Retail,
                Title = "StarCraft II: Wings of Liberty Launcher",
                Version = "1.5.4",
                BuildNumber = "24540",
                ReleaseDate = "2012-12-06",
                Summary = "Original Wings of Liberty launcher featuring Jim Raynor, news feed, community links, and Battle.net 2.0 integration.",
                BinaryFileName = "StarCraft II Launcher.exe",
                DownloadUrl = "https://archive.org/download/star-craft-ii-wings-of-liberty-2010-blizzard-dvd/StarCraft%20II%20Wings%20of%20Liberty%202010%20Blizzard%20DVD.iso",
                ApproxSizeBytes = 2350000,
                ProductCode = "s2"
            },

            // =========================================================================
            // 5. STARCRAFT: BROOD WAR LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "sc-broodwar-1.16",
                Family = LauncherGameFamily.StarCraftBroodWar,
                Category = LauncherCategory.Retail,
                Title = "StarCraft: Brood War Classic Launcher",
                Version = "1.16.1",
                BuildNumber = "1161",
                ReleaseDate = "2009-01-21",
                Summary = "The legendary classic StarCraft: Brood War executable and Battle.net gateway selector that defined esports for a decade.",
                BinaryFileName = "StarCraft.exe",
                DownloadUrl = "https://archive.org/download/starcraft-ii-pc/StarCraft%20II%20-%20Wings%20of%20Liberty%20%28PC%29%20%28USA%29.iso",
                ApproxSizeBytes = 1250000,
                ProductCode = "s1"
            },

            // =========================================================================
            // 6. WARCRAFT III LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "w3-frozen-throne-1.26",
                Family = LauncherGameFamily.Warcraft3,
                Category = LauncherCategory.Expansion,
                Title = "Warcraft III: The Frozen Throne Launcher",
                Version = "1.26a",
                BuildNumber = "6059",
                ReleaseDate = "2011-03-24",
                Summary = "The definitive golden age Warcraft III launcher and patch client powering DotA Allstars and classic Battle.net custom games.",
                BinaryFileName = "Frozen Throne.exe",
                DownloadUrl = "https://archive.org/download/warcraft-iii-roc-tft-original-installers/Warcraft%20III%20The%20Frozen%20Throne%20Expansion%20Set%20USA.iso",
                ApproxSizeBytes = 1450000,
                ProductCode = "w3"
            },
            new()
            {
                Id = "w3-reign-of-chaos-1.06",
                Family = LauncherGameFamily.Warcraft3,
                Category = LauncherCategory.Retail,
                Title = "Warcraft III: Reign of Chaos Classic Launcher",
                Version = "1.06",
                BuildNumber = "1060",
                ReleaseDate = "2003-06-03",
                Summary = "The original 2002-2003 Reign of Chaos client launcher with classic Battle.net gateway routing (US East, US West, Europe, Asia).",
                BinaryFileName = "War3.exe",
                DownloadUrl = "https://archive.org/download/warcraft-iii-roc-tft-original-installers/Warcraft%20III%20The%20Frozen%20Throne%20Expansion%20Set%20USA.iso",
                ApproxSizeBytes = 1350000,
                ProductCode = "w3"
            },

            // =========================================================================
            // 7. DIABLO II LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "d2-lod-1.14d",
                Family = LauncherGameFamily.Diablo2,
                Category = LauncherCategory.Expansion,
                Title = "Diablo II: Lord of Destruction Classic Launcher",
                Version = "1.14d",
                BuildNumber = "1144",
                ReleaseDate = "2016-06-07",
                Summary = "Final official classic Diablo II patch launcher with modern Windows 10/11 compatibility and legacy Battle.net gateway switching.",
                BinaryFileName = "Diablo II.exe",
                DownloadUrl = "https://archive.org/download/diablo-ii-lord-of-destruction-hoanglong/Diablo%20II%20Lord%20of%20Destruction%201.14D%20Setup.zip",
                ApproxSizeBytes = 1150000,
                ProductCode = "d2"
            },

            // =========================================================================
            // 8. HEARTHSTONE LAUNCHERS
            // =========================================================================
            new()
            {
                Id = "hs-closed-beta-2013",
                Family = LauncherGameFamily.Hearthstone,
                Category = LauncherCategory.Beta,
                Title = "Hearthstone Closed Beta Standalone Launcher",
                Version = "1.0.0",
                BuildNumber = "4217",
                ReleaseDate = "2013-12-18",
                Summary = "Pre-release standalone Hearthstone beta client launcher before permanent integration into the Battle.net Desktop App.",
                BinaryFileName = "Hearthstone Beta Launcher.exe",
                DownloadUrl = "https://archive.org/download/battle.net-setup/Battle.net-Setup.exe",
                ApproxSizeBytes = 2850000,
                ProductCode = "hsb"
            }
        };

        public static LauncherItem Resolve(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            string q = query.Trim();
            var exact = AllLaunchers.FirstOrDefault(x => x.Id.Equals(q, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            return AllLaunchers.FirstOrDefault(x =>
                x.Version.Equals(q, StringComparison.OrdinalIgnoreCase) ||
                x.BuildNumber.Equals(q, StringComparison.OrdinalIgnoreCase) ||
                x.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayTitle.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
    }
}
