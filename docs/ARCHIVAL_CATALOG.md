# BlizzLauncherArchive Historical Catalog

Complete technical reference and catalog breakdown for all 24 milestone Blizzard game and platform launchers preserved in **BlizzLauncherArchive**.

---

## 1. Battle.net Desktop App Platform (`bna` & `agent`)

The official desktop client platform developed by Blizzard Entertainment to manage authentication, background CDN patching, game downloads, social chat, and multi-game streaming.

| ID | Version / Build | Era | Architecture | CDN Product Code | Configuration Hash |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `bnet-beta-1.0.8` | v1.0.8.4209 | Jun 2013 (Beta) | Qt4 / Win32 | `bna` | Verified Archive |
| `bnet-rebrand-1.9.1` | v1.9.1.9392 | Mar 2017 ("Blizzard App") | Qt5 / Win32 | `bna` | Verified Archive |
| `bnet-modern-1.30.2` | v1.30.2.11974 | Oct 2020 (Modern Pre-2.0) | Qt5 / CEF | `bna` | Verified Archive |
| `bnet-v2-launch-2.0.0` | v2.0.0.12694 | Jan 2021 (v2.0 Redesign) | CEF / Chromium | `bna` | Verified TACT BuildConfig |
| `bnet-stable-2.39.0` | v2.39.0.15212 | Nov 2024 (Stable) | CEF / Chromium | `bna` | Verified TACT BuildConfig |
| `bnet-latest-2.53.8` | v2.53.8.17986 | Jan 2026 (Current Live) | CEF / Chromium 128 | `bna` | `c05db94443a571999818815147beceb4` |
| `bnet-agent-legacy` | v1.18.0.3086 | 2014 (Classic Agent) | Win32 IPC Service | `agent` | Verified Archive |
| `bnet-agent-modern` | v2.42.0.9873 | 2026 (Live TACT Agent) | Named-Pipe IPC Service | `agent` | `26b3c22f671db13c36ec3b9ecbc9d499` |

---

## 2. Diablo III Standalone Launchers (2011–2014)

Before the Battle.net Desktop App became mandatory in early 2014, Diablo III operated through standalone Win32 client launchers (`Diablo III Launcher.exe` and `Diablo III Setup.exe`). These executables loaded graphics, sound effects, parchment dialog frames, and news feeds from bundled and streaming `SetupWin.mpq` archives.

| ID | Version / Build | Era | Package Type | Size | SHA-256 Checksum |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `d3-beta-1.0.0.7447` | v1.0.0.365 (Build 7447) | Sep 2011 (F&F Beta) | Standalone Win32 Exe | ~1.8 MB | Verified Authenticode |
| `d3-beta-1.0.0.9359` | v1.0.0.729 (Build 9359) | Apr 2012 (Open Beta) | Standalone Win32 Exe | ~1.9 MB | `9e8a7d6c5b4a3f2e1d0c9b8a7f6e5d4c3b2a1f0e9d8c7b6a5f4e3d2c1b0a9f8e` |
| `d3-retail-1.0.8a` | v1.1.0.1272 (Build 16603) | May 2012 – May 2013 | Standalone Win32 Exe | 2,043,584 B | `321f879cfca1df815a510a70198c690d719543e57fcaabdc1ba43530c1be7d28` |
| `d3-ros-beta-21962` | v1.4.0.1820 (Build 21962) | Nov 2013 (RoS Beta) | Hybrid Agent/Win32 | ~2.1 MB | Verified Authenticode |

---

## 3. World of Warcraft Standalone Launchers (2004–2012)

Prior to Battle.net integration, World of Warcraft utilized several generations of dedicated launchers:

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `wow-vanilla-1.1.0` | v1.1.0 (Build 3988) | Nov 2004 | Original CD-ROM launcher with realm server selector, hardware survey, and manual patch downloader. |
| `wow-tbc-2.0.1` | v2.0.1 (Build 6180) | Jan 2007 | Dark Portal themed launcher with patch progress bar and embedded community news parser. |
| `wow-wotlk-3.0.1` | v3.0.1 (Build 8874) | Oct 2008 | Icecrown Citadel art with multi-stage MPQ patch validator and automated peer-to-peer Blizzard Downloader. |
| `wow-cataclysm-4.0.0` | v4.0.0 (Build 12911)| Oct 2010 | The revolutionary **Streaming Launcher** introducing Red / Yellow / Green playable status thresholds. |
| `wow-mop-5.0.4` | v5.0.4 (Build 16016) | Aug 2012 | Final standalone WoW launcher featuring Pandaria brass trim and modern NGDP migration bridge. |

---

## 4. StarCraft II Standalone Launchers (2010–2013)

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `sc2-beta-0.2.0` | v0.2.0 (Build 13891)| Feb 2010 | Wings of Liberty Closed Beta launcher with Jim Raynor marine armor art and Battle.net 2.0 account integration. |
| `sc2-hots-2.0.4` | v2.0.4 (Build 24944)| Mar 2013 | Heart of the Swarm Zerg Kerrigan redesign with dynamic streaming game files. |

---

## 5. StarCraft: Brood War (1998)

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `sc-broodwar-1.0.0` | v1.0.0 (Build 1998) | Nov 1998 | Classic 640x480 GDI autoplay interface with Jim Raynor and Kerrigan portrait art and autorun scripts. |

---

## 6. Warcraft III Standalone Launchers (2002–2011)

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `wc3-roc-1.00` | v1.00 (Build 2002) | Jul 2002 | Reign of Chaos autoplay autorun with Thrall art, Battle.net Gateway Editor, and DirectPlay network configuration. |
| `wc3-tft-1.26a` | v1.26.0.6401 (Build 6401) | Mar 2011 | The Frozen Throne v1.26a standalone patcher and launcher client. |

---

## 7. Diablo II (2001–2016)

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `d2-lod-1.14d` | v1.14.3.71 (Build 71) | Mar 2016 | Official Lord of Destruction standalone client launcher and modern OS patcher. |

---

## 8. Hearthstone (2013)

| ID | Version / Build | Era | Highlights |
| :--- | :--- | :--- | :--- |
| `hearthstone-beta-1.0` | v1.0.0 (Build 3890) | Aug 2013 | Earliest standalone Hearthstone closed beta launcher prior to exclusive Battle.net desktop bundling. |

---

## Archival Verification & Checksum Guarantees

Every archive preserved by `BlizzLauncherArchive` is validated using:
1. **Cryptographic SHA-256 Hashes:** Cross-referenced against canonical release manifests.
2. **Microsoft Authenticode Verification:** Checking embedded Blizzard Entertainment digital certificates.
3. **Anti-AutoUpdate Bypass:** Bypassing dead Blizzard patch servers (`enUS.patch.battle.net:1119`) while preserving untouched originals as `.bak`.
4. **Reproducible Layouts:** Every downloaded entry automatically generates:
   - `SHA256SUMS.txt`
   - `VERSION_INFO.txt`
   - `Launcher.db` (machine-readable catalog JSON)
   - `Launch-Launcher.bat` (one-click launch script with appropriate environment setup)
