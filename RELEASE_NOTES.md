# Release Notes: BlizzLauncherArchive v1.0.6

**Release Date:** November 2026  
**Build:** `v1.0.6` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.6

### 1. Permanent Fix for "Failed to retrieve Blizzard Launcher properties"
- **Properties Dialog Bypass (Patch 2):** Applied an automatic binary patch at VA `0x4116de` in `Blizzard Launcher.exe` (`LauncherPatcher.PatchNativeLauncher`), bypassing the conditional jump to `0x411712`. This permanently eliminates the modal error dialog `"Failed to retrieve Blizzard Launcher properties"` and ensures uninterrupted execution.
- **State Machine "Install Found" Transition (Patch 3):** Applied binary patch at VA `0x4115e8c` to force the `install found` signal (`0x5671e4`), seamlessly transitioning the client from the splash screen to the full authentic `820x650` gothic launcher window with the `PLAY` button enabled.
- **Smart Desktop Window Watcher:** Implemented a Win32 window supervisor in `NativeLauncherRunner` that automatically monitors the launched process, brings the `820x650` authentic Diablo III launcher window to the foreground, and hides any lingering splash frame.

### 2. Path & Runner Hardening
- **Smart Directory Resolver:** `NativeLauncherRunner.ResolveGameDirectory` intelligently resolves target folders across `Diablo3_Retail_1.1.0.1272_16603` and related lineage builds, preventing directory mismatches.
- **Trailing Slash CMD Escaping Fix:** Fixed an issue where `%~dp0` trailing backslashes escaped the closing quote in Windows batch scripts, ensuring `Launch-Launcher.bat` executes cleanly from any directory.
- **Expanded Fallback Discovery:** `Launch-Launcher.bat` now scans local folders, parent directories, and `Downloads` to automatically locate `BlizzLauncherArchive.exe`.

### 3. In-App Updater & Version Alignment
- Aligned update contract to `1.0.6`, ensuring accurate version comparison and smooth in-app updates with SHA-256 verification.

---

# Release Notes: BlizzLauncherArchive v1.0.5

**Release Date:** November 2026  
**Build:** `v1.0.5` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.5

### 1. Genuine Native Diablo III Launcher Integration
- **Direct Native Execution:** Boots the authentic 2011/2012 Qt4/WebKit `Blizzard Launcher.exe` binary directly onto the Windows desktop, rendering the original horned skull frame, gothic menus, and classic Diablo III sound and visual assets.
- **Automated Anti-Hang & Product Bypass Patch:** Automatically patches the client product validation check at runtime (`LauncherPatcher.PatchNativeLauncher`), bypassing dead 2012 update servers and eliminating the legacy `"Please run the game launcher from your game directory"` block.
- **Embedded Agent 1.x Protocol Shim:** Features a lightweight local HTTP Agent shim (`AgentShimServer`) implementing the classic 2011/2012 JSON REST protocol (`/agent`, `/game/Client`, `/game`, `/install`, `/update`, `/gamesession`), perfectly satisfying client property validation and preventing the `"Failed to retrieve Blizzard Launcher properties"` error.
- **One-Click Native Launch UI:** Added sleek **⚡ NATIVE LAUNCH** buttons to both the global toolbar and each launcher card in the studio UI, enabling immediate one-click execution.
- **Smart Launch-Launcher.bat Runner:** Updated the portable runner script to prioritize the native launcher engine with automatic Agent shim orchestration and direct client fallbacks.

### 2. Update Checker & In-Place Updater Polish
- Corrected update version resolution to use the explicit `1.0.5` release contract, eliminating erroneous "Update Available" indicators when running the latest release.
- Streamlined automated download and self-restart workflows with SHA-256 integrity verification.

---

# Release Notes: BlizzLauncherArchive v1.0.4

**Release Date:** November 2026  
**Build:** `v1.0.4` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.4

### 1. Authentic Diablo III Gothic Launcher Experience
- **Pixel-Perfect Ornate Gothic Frame:** Retro Play now renders the authentic 2011/2012 Diablo III launcher with transparency shaping around the carved metallic horns and Diablo horned skull artwork.
- **Interactive Top Menu Bar:** Added interactive `OPTIONS`, `INFO`, and `HELP` navigation, providing on-the-fly Realm selection, custom game client linking, and historical patch archives.
- **Vintage News & Community Feeds:** Restored the historical September 2011 Sanctuary announcements (Gold Auction House, Beta Patch 3, The Shared Stash, Community Site levels up) and community links.
- **Interactive Stone PLAY Button & Streaming Bar:** Features the carved stone `PLAY` button with glowing hover feedback, 3-stage streaming markers (`1 SETUP`, `2 AVAILABLE`, `3 PLAYABLE`), and the red soulstone download status.
- **Seamless Desktop Dragging:** Drag the launcher anywhere across your desktop by grabbing the frame.

### 2. Resolved Update Checker False-Positive
- Dynamically resolves the executing assembly version (`v1.0.4`) in `UpdateChecker.cs` instead of comparing against a static constant, preventing false "Update Available" notifications when already on the latest version.

---

# Release Notes: BlizzLauncherArchive v1.0.3

**Release Date:** November 2026  
**Build:** `v1.0.3` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.3

### 1. Clientless Standalone Retro Emulator (Preservation Mode)
- **Eliminated "Executable Not Found" Blockers:** When running in clientless preservation mode without a full 15 GB retail game client installed, clicking **PLAY** now launches the authentic **Clientless Preservation Sandbox** rather than throwing an error dialog.
- **On-the-Fly Client Linking:** Added a **`[ 📁 Link Client... ]`** button directly to the action bar, enabling users to effortlessly browse and link an existing game executable (`Diablo III.exe`, `Diablo III64.exe`, `SC2.exe`, etc.) on any drive or switch back to Standalone Mode anytime.
- **Automated Directory Discovery:** Proactively detects common retail game installation paths across `Program Files (x86)` and standard game library drives, seamlessly transitioning between Clientless Mode and Linked Client Mode.

### 2. Multi-Game Family Retro Artwork & Bulletins
- Dynamically adapts typography, banner artwork (`d3_banner.jpg`, `sc2_banner.jpg`, `wow_banner.jpg`, `wc3_banner.jpg`, `d2_banner.jpg`, `hearthstone_banner.jpg`), and realm news bulletins to the selected game family.
- Expanded Launcher & Realm Preferences dialog with gateway selection (Americas, Europe, Asia, PTR) and client management.

### 3. Smart Launch-Launcher.bat Offline Preservation Runner
- Updated generated batch script to detect if a full game client is present in the archive directory:
  - If a retail client exists, it launches the patched vintage standalone Qt client directly with the working directory set to the game path, preventing "Please run from game directory" prompts.
  - If run in an archive-only folder without retail game files, it automatically starts the clientless retro emulator.

---

# Release Notes: BlizzLauncherArchive v1.0.2

**Release Date:** November 2026  
**Build:** `v1.0.2` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.2

### 1. In-App Auto-Updater (Matching D3Forever Architecture)
- Direct in-app background downloading of release assets (`BlizzLauncherArchive.exe` and `SHA256SUMS.txt`).
- Real-time progress bar with percentage, downloaded MB, and total MB indicators.
- Mandatory SHA-256 cryptographic integrity verification before staging the update.
- Atomic self-replacing batch script that cleanly terminates the current instance, replaces the executable, and restarts the updated application.
- Dark-themed frameless update dialog matching the exact aesthetic of Diablo 3 Forever.

### 2. Standalone Anti-AutoUpdate & Hang Bypass Engine
- Resolved the historical bootstrapper hang on `"Updating Blizzard Launcher...."` and `"Updating Blizzard Update Agent...."`.
- Signature-based patch bypassing legacy Agent calls to dead Blizzard update servers (`enUS.patch.battle.net:1119`).
- Standalone portable `Launch-Launcher.bat` runner detecting cached Qt4 client instances (`Blizzard Launcher.888`) and launching them offline without tripping modern Battle.net Agent signature validation errors.

### 3. Dedicated Game Tiles by Game Name (8 Independent Hubs)
- Organized all 24 historical launchers into 8 distinct game family tiles:
  - **Battle.net Desktop App** (2013–2026)
  - **Diablo III** (2011–2014)
  - **World of Warcraft** (2004–2012)
  - **StarCraft II** (2010–2013)
  - **StarCraft: Brood War** (1998)
  - **Warcraft III** (2002)
  - **Diablo II** (2000–2001)
  - **Hearthstone** (2013)
- Custom high-resolution atmospheric banners and dynamic chip measuring for every tile.

---

# Release Notes: BlizzLauncherArchive v1.0.1

**Release Date:** November 2026  
**Build:** `v1.0.1` (source build)  
**Binary:** `release/BlizzLauncherArchive.exe` (Single-file self-contained x64 executable)  
**Platform:** Windows 10 / 11 (64-bit)  

---

## What's New in v1.0.1

### 1. 8 Dedicated Game Tiles by Game Name
- Expanded catalog into 8 distinct game family tiles: Battle.net Desktop App, Diablo III, World of Warcraft, StarCraft II, StarCraft: Brood War, Warcraft III, Diablo II, and Hearthstone.
- Added embedded high-resolution game banners for all 8 game families.
- Implemented dynamic badge pill measuring for retro play chips.

### 2. Standalone Anti-AutoUpdate Patch Engine
- Reverse-engineered state machine dispatcher to bypass Agent and client update loops.
- Added automatic creation of `.bak` untouched binary backups prior to patching.

### 3. Cold Storage Backup by Game Name
- Individual checkboxes for all 8 game families in the backup dialog.

---

# Release Notes: BlizzLauncherArchive v1.0.0

**Release Date:** November 2026  
**Build:** `v1.0.0` (source build)  
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

### 2. Comprehensive Historical Catalog (24 Preserved Builds)
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
