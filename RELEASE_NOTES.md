# Release Notes: BlizzLauncherArchive v1.0.0

**Release Date:** November 2026  
**Author:** Elite Level Software (EliteLevelSoftware@gmail.com)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What is BlizzLauncherArchive?

**BlizzLauncherArchive** is a unified preservation suite and retro player for the entire lineage of Blizzard Entertainment game launchers and Battle.net desktop applications. Built upon the architectural foundation of `Diablo 3 Forever`, this tool brings together standalone launchers from 1998 through 2014, modern Battle.net platform releases, and private server developer toolkits into a single, high-performance utility.

---

## Key Highlights & Features

### 1. Interactive Windows Forms Dashboard
- **Dark-Themed Game Family Tiles:** Visual accordion drawers with rich game artwork for Diablo III, Battle.net Desktop App, World of Warcraft, StarCraft, and Classic Strategy.
- **Animated Breathing Status Dots:** Real-time visual feedback with pulsing indicators reflecting catalog loading, active download streams, and verified archives.
- **Real-Time Live Search:** Fast filtering across all 23 historical launcher builds by name, version, game family, era, or build number.
- **Version Chips & Badges:** Instant visual identification of package types (`Win32 Direct`, `Agent Daemon`, `TACT Bundle`, `Streaming`).
- **Collapsible Diagnostic Console:** Live execution logging with timestamped download progress, SHA-256 verifications, and cold storage exports.

### 2. Comprehensive Historical Catalog (23 Preserved Builds)
- **Diablo III Standalone Launchers:**
  - 2011 Friends & Family Beta (`v1.0.0.365`, Build 7447)
  - 2012 Open Beta (`v1.0.0.729`, Build 9359)
  - 2012–2013 Retail Vanilla 1.0.1–1.0.8a (`v1.1.0.1272`, Build 16603)
  - 2013–2014 Reaper of Souls Beta (`v1.4.0.1820`, Build 21962)
- **Battle.net Desktop App Lineage:**
  - 2013 Open Beta (`v1.0.8.4209`)
  - 2017 "Blizzard App" Rebrand (`v1.9.1.9392`)
  - 2020 Modernization (`v1.30.2.11974`)
  - 2021 Complete Redesign (`v2.0.0.12694`)
  - 2024 Modern Stable (`v2.39.0.15212`)
  - 2026 Live Release (`v2.53.8.17986`)
  - Classic Agent (`v1.18.0.3086`) and Modern Agent (`v2.42.0.9873`)
- **World of Warcraft Standalone Launchers:**
  - 2004 Vanilla (`v1.1.0`, Build 3988)
  - 2007 The Burning Crusade (`v2.0.1`, Build 6180)
  - 2008 Wrath of the Lich King (`v3.0.1`, Build 8874)
  - 2010 Cataclysm Streaming Launcher (`v4.0.0`, Build 12911)
  - 2012 Mists of Pandaria (`v5.0.4`, Build 16016)
- **StarCraft Series Standalone Launchers:**
  - 1998 Brood War CD Autoplay (`v1.0.0`)
  - 2010 Wings of Liberty Closed Beta (`v0.2.0`, Build 13891)
  - 2010 Wings of Liberty Retail (`v1.0.0`, Build 16117)
  - 2013 Heart of the Swarm (`v2.0.4`, Build 24944)
- **Classic Strategy & Beta Launchers:**
  - 2002 Warcraft III Reign of Chaos Autoplay (`v1.00`)
  - 2013 Hearthstone Closed Beta Standalone (`v1.0.0`, Build 3890)

### 3. Retro Launcher Emulator
- Interactive retro player dialog recreating the authentic 2012 Diablo III standalone launcher interface.
- Complete with Tyrael banner art, animated community news ticker, live realm status indicators (Americas, Europe, Asia), options dialog, and direct game launch bridge.

### 4. Cold Storage Bulk Archival Suite
- Automated bulk backup engine that scans target destination folders (external hard drives, NAS volumes, or cloud cold storage).
- Performs real-time inventory checks, displays missing vs downloaded items, supports game-family filtering, and writes master `ARCHIVE_INDEX.json` and verification manifests.

### 5. Historical Dossier Database
- Rich historical modal dialogs for every launcher version providing release context, technical architecture breakdowns, development trivia, and authentic release quotes.

### 6. Private Server Developer Integration Kit
- Embedded multi-language code generator exporting ready-to-run launcher wrappers and server redirect scripts for **C#**, **Electron / Node.js**, **Python**, **PowerShell**, **Batch**, and **C++**.

### 7. Dual Operation Mode (GUI + Headless CLI)
- Full command-line interface supporting automated server pipelines, CI/CD, and remote backup tasks:
  - `--list-versions`: Output catalog table with versions, families, and sizes.
  - `--version <id>`: Target specific launcher by ID.
  - `--game <family>`: Target all launchers in a specific family.
  - `--dir <path>`: Specify destination archive folder.
  - `--backup`: Execute batch cold storage backup.
  - `--binaries-only`: Download only core standalone executables.
  - `--retro-launch`: Launch retro emulation player.
