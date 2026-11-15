# BlizzLauncherArchive Server Integration Guide

This guide details how to integrate archived Blizzard launchers into custom game servers, private server emulators, and community preservation projects.

---

## Overview

Blizzard launchers from 1998 through 2014 were standalone binaries designed to communicate with Blizzard web endpoints, update daemons, and patch servers before launching the game client. By preserving these launchers, server authors can:
1. Recreate authentic launch screens for classic client versions (e.g. Diablo III 1.0.8a, WoW 3.3.5a, SC2 Beta).
2. Bypass live Battle.net client enforcement and run clients with custom server credentials or redirected ports.
3. Serve dynamic server news, changelogs, and real-time realm status inside the launcher interface.

---

## 1. Diablo III Standalone Launcher Integration

### Launching Diablo III Directly
The standalone `Diablo III Launcher.exe` can invoke `Diablo III.exe` with bypass arguments:

```cmd
"Diablo III Launcher.exe" -launch -locale enUS
```

### Passing Custom Server Flags to Diablo III
When connecting to emulators like **Mooege** or **D3Classic**:

```cmd
"Diablo III.exe" -launch -client -aurora:server=127.0.0.1:1119 -locale enUS
```

### Mocking the Diablo III News Feed
In 2012–2013, the launcher fetched community news and status alerts from:
`http://launcher.worldofwarcraft.com/alert` and `http://us.launcher.battle.net/d3/news`

To display your private server's news:
1. Add an entry to your `C:\Windows\System32\drivers\etc\hosts`:
   ```
   127.0.0.1  us.launcher.battle.net
   127.0.0.1  launcher.worldofwarcraft.com
   ```
2. Serve an HTTP GET route on port 80 at `/d3/news` returning valid XML or HTML fragments:
   ```html
   <div class="news-item">
       <h3>Welcome to D3 Sanctuary Private Realm</h3>
       <p>Version 1.0.8a Vanilla is online! Experience authentic 2012 Inferno progression.</p>
   </div>
   ```

---

## 2. World of Warcraft Standalone Launcher Integration

### Setting Realmlist
In classic WoW launchers (Vanilla, TBC, WotLK):
1. Configure `realmlist.wtf` in `Data/enUS/realmlist.wtf`:
   ```
   set realmlist logon.yourserver.org
   set patchlist 127.0.0.1
   ```
2. The launcher will read `realmlist.wtf` and display server connection status.

### Disabling Automatic Patching
To prevent the classic launcher from attempting to patch to an incompatible version:
- Run the launcher with `-noupdate`:
  ```cmd
  "Launcher.exe" -noupdate
  ```
- Or mock `patchlist` on your local web server returning HTTP 200 with an empty patch list.

---

## 3. C# Automation Code

For custom client wrappers or launchers written in C# / .NET:

```csharp
using System;
using System.Diagnostics;
using System.IO;

namespace CustomServerLauncher
{
    public class ServerLauncherBridge
    {
        public static void LaunchDiablo3(string gameDir, string serverHost = "127.0.0.1", int serverPort = 1119)
        {
            string launcherExe = Path.Combine(gameDir, "Diablo III Launcher.exe");
            string directGameExe = Path.Combine(gameDir, "Diablo III.exe");

            var startInfo = new ProcessStartInfo
            {
                FileName = File.Exists(launcherExe) ? launcherExe : directGameExe,
                Arguments = $"-launch -aurora:server={serverHost}:{serverPort} -locale enUS",
                WorkingDirectory = gameDir,
                UseShellExecute = false
            };

            Process.Start(startInfo);
        }
    }
}
```

---

## 4. Electron / Node.js Integration

```javascript
const { execFile } = require('child_process');
const path = require('path');

function launchServerClient(gameDirectory, serverIp = '127.0.0.1', serverPort = 1119) {
    const launcherPath = path.join(gameDirectory, 'Diablo III Launcher.exe');
    const args = ['-launch', `-aurora:server=${serverIp}:${serverPort}`, '-locale', 'enUS'];

    const child = execFile(launcherPath, args, { cwd: gameDirectory }, (error) => {
        if (error) {
            console.error('Failed to launch game:', error);
        }
    });

    child.on('spawn', () => {
        console.log('Game process started with server target: ' + serverIp);
    });
}
```

---

## 5. Python Integration

```python
import os
import subprocess

def launch_classic_d3(game_dir: str, server_host: str = "127.0.0.1", server_port: int = 1119):
    exe_path = os.path.join(game_dir, "Diablo III Launcher.exe")
    args = [
        exe_path,
        "-launch",
        f"-aurora:server={server_host}:{server_port}",
        "-locale",
        "enUS"
    ]
    subprocess.Popen(args, cwd=game_dir)

if __name__ == "__main__":
    launch_classic_d3("C:\\Games\\Diablo III Vanilla")
```
