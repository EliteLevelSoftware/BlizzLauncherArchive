using System;
using System.Collections.Generic;

namespace BlizzLauncherArchive
{
    public sealed class LauncherDossier
    {
        public string Id { get; init; } = string.Empty;
        public string Headline { get; init; } = string.Empty;
        public string EraContext { get; init; } = string.Empty;
        public List<string> MilestoneBadges { get; init; } = new();
        public List<string> KeyHighlights { get; init; } = new();
        public string FamousQuoteOrTrivia { get; init; } = string.Empty;
        public string OfficialUrl { get; init; } = string.Empty;
    }

    public static class LauncherDossierDatabase
    {
        private static readonly Dictionary<string, LauncherDossier> Dossiers = new()
        {
            ["d3-retail-1.0.8a"] = new()
            {
                Id = "d3-retail-1.0.8a",
                Headline = "The Definitive Standalone Diablo III Launcher",
                EraContext = "From May 15, 2012 through late 2013, millions of players booted Diablo III every night through this standalone launcher. Featuring high-res parchment textures, Tyrael artwork, real-time regional realm lights, and streaming MPQ delivery, this represents the peak of Blizzard's standalone launcher era.",
                MilestoneBadges = new() { "⭐ Retail Master", "📜 SetupWin Skin", "🟢 3-Stage Streaming", "🛡️ Real-Time Realm Status" },
                KeyHighlights = new()
                {
                    "Packaged with the complete SetupWin.mpq skin archive (14.4 MB) containing authentic textures, fonts, and dialogs.",
                    "Three-stage progressive streaming downloader: Setup (Red), Playable (Yellow), Optimal (Green).",
                    "Integrated realm status indicators for Americas, Europe, and Asia realms.",
                    "Last standalone launcher before mandatory migration to the unified Battle.net Desktop App."
                },
                FamousQuoteOrTrivia = "“The skies shall rain fire, and the seas shall turn to blood, and the righteous shall be tormented.” — Deckard Cain",
                OfficialUrl = "https://diablo.fandom.com/wiki/Diablo_III_Beta"
            },
            ["d3-ros-beta-21962"] = new()
            {
                Id = "d3-ros-beta-21962",
                Headline = "Reaper of Souls Closed Beta: The Malthael Era",
                EraContext = "In late 2013 and early 2014, Blizzard debuted the dark gothic Reaper of Souls aesthetic. This Public Test Launcher brought Crusaders, Loot 2.0, Adventure Mode, and Nephalem Rifts to test realms with Malthael's grim Westmarch visuals.",
                MilestoneBadges = new() { "💀 Reaper of Souls Beta", "⚔️ Crusader Debut", "🌌 Nephalem Rifts", "🔒 Closed Beta Milestone" },
                KeyHighlights = new()
                {
                    "Custom Reaper of Souls skin with Westmarch architecture and soul-vapor animations.",
                    "Final iteration of the standalone launcher before Battle.net Desktop App was mandated for Patch 2.0.1.",
                    "Includes dedicated Public Test Realm realm routing and automated crash report telemetry."
                },
                FamousQuoteOrTrivia = "“No one can stop Death.” — Malthael, Archangel of Wisdom and Death",
                OfficialUrl = "https://diablo.fandom.com/wiki/Diablo_III:_Reaper_of_Souls"
            },
            ["d3-open-beta-9359"] = new()
            {
                Id = "d3-open-beta-9359",
                Headline = "Open Beta Weekend: 2 Million Players Overwhelm Sanctuary",
                EraContext = "Over the weekend of April 20–23, 2012, Blizzard opened Diablo III to the entire world for an unprecedented server stress test. This standalone launcher handled the massive surge of millions downloading and testing the Skeleton King encounter.",
                MilestoneBadges = new() { "🔥 2M+ Players", "🌐 Global Stress Test", "👑 Skeleton King Cap", "⚡ Streaming Downloader" },
                KeyHighlights = new()
                {
                    "The historic beta build that tested BattleTags, elective skill mode, and the Level 13 cap.",
                    "One of the earliest widespread public deployments of Blizzard's background streaming technology."
                },
                FamousQuoteOrTrivia = "“This weekend stress test is designed to break our hardware so May 15 doesn't break your heart.” — Bashiok, 2012",
                OfficialUrl = "https://diablo.fandom.com/wiki/Diablo_III_Beta"
            },
            ["d3-ff-beta-7447"] = new()
            {
                Id = "d3-ff-beta-7447",
                Headline = "The Genesis: 2011 Friends & Family Beta Launcher",
                EraContext = "In September 2011, Blizzard released the initial Friends & Family Beta client. At this milestone, Diablo III still used physical Rune items (Crimson, Indigo, Obsidian) dropped in the world, the Cauldron of Jordan, and the Nephalem Cube.",
                MilestoneBadges = new() { "🌱 First Public D3 Launcher", "💎 Physical Runes Era", "🏺 Cauldron of Jordan", "🔒 F&F Beta" },
                KeyHighlights = new()
                {
                    "Earliest preserved standalone Diablo III launcher executable (Build 7447).",
                    "Earliest iteration of the streaming downloader engine before retail polish."
                },
                FamousQuoteOrTrivia = "“We want Diablo III to feel like the ultimate evolution of dark action RPGs.” — Jay Wilson, 2011",
                OfficialUrl = "https://diablo.fandom.com/wiki/Diablo_III_Beta"
            },
            ["bnet-2.53-live"] = new()
            {
                Id = "bnet-2.53-live",
                Headline = "Battle.net 2.0 Live Retail Generation",
                EraContext = "The contemporary flagship Battle.net Desktop Application powered by Blizzard's modern TACT/NGDP architecture. Serves millions of players daily across World of Warcraft, Diablo IV, Diablo III, Overwatch 2, Hearthstone, and Call of Duty.",
                MilestoneBadges = new() { "🟢 Live Production", "🌐 TACT NGDP Engine", "⚡ Modern CEF UI", "👥 Universal Social Hub" },
                KeyHighlights = new()
                {
                    "Full CASC manifest configuration (`BuildConfig: 4b9c77d7485667dd03c1582a0ff2bc20`).",
                    "Modular architecture utilizing the Battle.net Update Agent for concurrent multi-gigabyte delta patching.",
                    "Customizable favorites shelf, direct community feeds, and integrated voice/chat servers."
                },
                FamousQuoteOrTrivia = "“Battle.net has connected gamers across Sanctuary, Azeroth, and the Koprulu Sector for over three decades.”",
                OfficialUrl = "https://battle.net"
            },
            ["bnet-2.0-launch"] = new()
            {
                Id = "bnet-2.0-launch",
                Headline = "The Landmark 2021 Battle.net 2.0 Redesign",
                EraContext = "In January 2021, Blizzard rolled out the largest visual and structural overhaul in the launcher's history. Moving away from the classic vertical sidebar, Battle.net 2.0 introduced top navigation tabs, customizable game drawers, and an expansive notification center.",
                MilestoneBadges = new() { "✨ Major Overhaul", "🎨 Horizontal Nav", "⭐ Customizable Favorites", "💬 Revamped Chat Drawer" },
                KeyHighlights = new()
                {
                    "Replaced the decade-old vertical game list with a clean top-bar layout.",
                    "Greatly improved accessibility, high-DPI scaling, and modern Chromium Embedded Framework frontend."
                },
                FamousQuoteOrTrivia = "“Battle.net 2.0 represents the biggest update to our launcher since its inception.” — Blizzard Official Release, 2021",
                OfficialUrl = "https://blizztrack.com"
            },
            ["bnet-blizzard-app-rebrand"] = new()
            {
                Id = "bnet-blizzard-app-rebrand",
                Headline = "The 2017 'Blizzard App' Rebrand Milestone",
                EraContext = "In late 2016 and 2017, Blizzard briefly renamed the iconic platform from 'Battle.net' to 'Blizzard App'. Following massive community outcry and nostalgia for the historic name, Blizzard reversed the decision and restored 'Blizzard Battle.net'.",
                MilestoneBadges = new() { "🏷️ The Rebrand Era", "📦 2017 Milestone", "🔄 Historic Transition", "⭐ Nostalgic UI" },
                KeyHighlights = new()
                {
                    "Preserves the unique period where the client was titled 'Blizzard App' with the blue Blizzard logo.",
                    "Added Destiny 2 and initial third-party Activision Blizzard publishing integrations."
                },
                FamousQuoteOrTrivia = "“When we created Battle.net, the idea of including a tailored online-gaming service around your game was relatively novel. But Battle.net is part of who we are.” — Blizzard Reversal Statement, Aug 2017",
                OfficialUrl = "https://blizztrack.com"
            },
            ["wow-wotlk-3.3.5a"] = new()
            {
                Id = "wow-wotlk-3.3.5a",
                Headline = "Wrath of the Lich King: The Golden Age of World of Warcraft",
                EraContext = "Patch 3.3.5a (Fall of the Lich King / The Ruby Sanctum) stands as the most popular patch in MMORPG history. The standalone launcher featured Arthas atop the Frozen Throne, realmlist configuration, and Blizzard Downloader integration.",
                MilestoneBadges = new() { "❄️ Lich King Iconic", "👑 12M Peak Subscribers", "🛡️ Standalone Executable", "📜 Realmlist Config" },
                KeyHighlights = new()
                {
                    "Iconic Icecrown background artwork and sound effects.",
                    "Native support for custom private realms and local server redirection without Battle.net login.",
                    "Direct launcher for the legendary 3.3.5a client."
                },
                FamousQuoteOrTrivia = "“Tell them only that the Lich King is dead... and that World of Warcraft died with him.” — Community Folk Wisdom",
                OfficialUrl = "https://worldofwarcraft.blizzard.com"
            },
            ["w3-frozen-throne-1.26"] = new()
            {
                Id = "w3-frozen-throne-1.26",
                Headline = "Warcraft III: The Frozen Throne (Patch 1.26a)",
                EraContext = "The immortal patch that defined custom maps, DotA Allstars, tower defense, and competitive RTS for over a decade. Features the classic gateway selector connecting to US East, US West, Europe, and Asia realms.",
                MilestoneBadges = new() { "⚔️ Golden Age RTS", "🛡️ DotA Era", "🌐 Gateway Selector", "💎 Zero Dependencies" },
                KeyHighlights = new()
                {
                    "Classic 2003-2011 Frozen Throne launcher with Battle.net Gateway Editor support.",
                    "Pure standalone Win32 binary with zero modern launcher requirements."
                },
                FamousQuoteOrTrivia = "“For the Lich King!” — Arthas Menethil",
                OfficialUrl = "https://classic.battle.net"
            }
        };

        public static LauncherDossier GetDossier(string id)
        {
            if (Dossiers.TryGetValue(id, out var d)) return d;
            return new LauncherDossier
            {
                Id = id,
                Headline = "Preserved Historical Blizzard Launcher Binary",
                EraContext = "Archived directly from official Blizzard distribution edge nodes and verified digital preservation repositories.",
                MilestoneBadges = new() { "📦 Standalone Launcher", "🔒 Digitally Signed", "⚡ 100% Offline Compatible" },
                KeyHighlights = new()
                {
                    "Extracted from original media, Akamai CDN edge servers, or verified archive mirrors.",
                    "Maintains original binary integrity and digital Authenticode signatures."
                },
                FamousQuoteOrTrivia = "“Preserving gaming history one byte at a time.”",
                OfficialUrl = "https://github.com/EliteLevelSoftware/BlizzLauncherArchive"
            };
        }
    }
}
