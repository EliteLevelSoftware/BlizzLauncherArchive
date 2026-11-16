using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public class LauncherModStudioDialog : Form
    {
        private readonly string _launcherFolder;
        private readonly LauncherItem _item;
        private LauncherModConfig _config;

        private TextBox _txtServerName;
        private TextBox _txtServerHost;
        private TextBox _txtServerPort;
        private TextBox _txtTargetExe;
        private TextBox _txtLaunchArgs;
        private TextBox _txtStatusText;
        private TextBox _txtRemainingText;

        private readonly TextBox[] _newsTitleBoxes = new TextBox[4];
        private readonly TextBox[] _newsDateBoxes = new TextBox[4];
        private readonly TextBox[] _commTitleBoxes = new TextBox[4];
        private readonly TextBox[] _commDateBoxes = new TextBox[4];
        private TextBox _logBox;

        public LauncherModStudioDialog(string launcherFolder, LauncherItem item = null)
        {
            _item = item;
            _launcherFolder = NativeLauncherRunner.ResolveGameDirectory(launcherFolder, item);
            _config = LauncherModConfig.LoadOrCreate(_launcherFolder, item);

            Text = $"Private Server Launcher Mod Studio & Recompiler — {Path.GetFileName(_launcherFolder)}";
            ClientSize = new Size(920, 680);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(18, 12, 10);
            ForeColor = Color.FromArgb(235, 210, 175);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9f);

            BuildUi();
            PopulateFields();
        }

        private void BuildUi()
        {
            var header = new Label
            {
                Text = "DECOMPILE, CUSTOMIZE & RECOMPILE PRIVATE SERVER LAUNCHER",
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 208, 142),
                Location = new Point(18, 12),
                AutoSize = true
            };
            Controls.Add(header);

            var subHeader = new Label
            {
                Text = $"Folder: {_launcherFolder}   |   Config: launcher-config.json   |   Assets: Decompiled_Assets\\",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.FromArgb(175, 145, 105),
                Location = new Point(20, 36),
                AutoSize = true
            };
            Controls.Add(subHeader);

            // Left Group: Server & Executable Config
            var grpServer = CreateGroup("1. Private Server, Executable & Load Bar Config", 18, 62, 430, 250);
            int sy = 26;
            _txtServerName = AddLabeledRow(grpServer, "Server / Realm Name:", ref sy);
            _txtServerHost = AddLabeledRow(grpServer, "Server Host / IP:", ref sy);
            _txtServerPort = AddLabeledRow(grpServer, "Server Port:", ref sy);
            _txtTargetExe = AddLabeledRow(grpServer, "Target Game EXE:", ref sy);
            _txtLaunchArgs = AddLabeledRow(grpServer, "Launch CLI Args:", ref sy);
            _txtStatusText = AddLabeledRow(grpServer, "Load Bar Status:", ref sy);
            _txtRemainingText = AddLabeledRow(grpServer, "Download Text:", ref sy);
            Controls.Add(grpServer);

            // Right Group: News & Community Headlines
            var grpNews = CreateGroup("2. Launcher News & Community Feed (Pre-filled & Editable)", 462, 62, 440, 390);
            int ny = 24;
            var lblN = new Label
            {
                Text = "News Headlines (Title + Date):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 190, 115),
                Location = new Point(12, ny),
                AutoSize = true
            };
            grpNews.Controls.Add(lblN);
            ny += 20;

            for (int i = 0; i < 4; i++)
            {
                _newsTitleBoxes[i] = new TextBox
                {
                    Location = new Point(12, ny),
                    Size = new Size(296, 23),
                    BackColor = Color.FromArgb(30, 18, 14),
                    ForeColor = Color.FromArgb(245, 225, 195),
                    BorderStyle = BorderStyle.FixedSingle
                };
                _newsDateBoxes[i] = new TextBox
                {
                    Location = new Point(314, ny),
                    Size = new Size(112, 23),
                    BackColor = Color.FromArgb(30, 18, 14),
                    ForeColor = Color.FromArgb(205, 170, 120),
                    BorderStyle = BorderStyle.FixedSingle
                };
                grpNews.Controls.Add(_newsTitleBoxes[i]);
                grpNews.Controls.Add(_newsDateBoxes[i]);
                ny += 28;
            }

            ny += 6;
            var lblC = new Label
            {
                Text = "Community Links (Title + Date):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 190, 115),
                Location = new Point(12, ny),
                AutoSize = true
            };
            grpNews.Controls.Add(lblC);
            ny += 20;

            for (int i = 0; i < 4; i++)
            {
                _commTitleBoxes[i] = new TextBox
                {
                    Location = new Point(12, ny),
                    Size = new Size(296, 23),
                    BackColor = Color.FromArgb(30, 18, 14),
                    ForeColor = Color.FromArgb(245, 225, 195),
                    BorderStyle = BorderStyle.FixedSingle
                };
                _commDateBoxes[i] = new TextBox
                {
                    Location = new Point(314, ny),
                    Size = new Size(112, 23),
                    BackColor = Color.FromArgb(30, 18, 14),
                    ForeColor = Color.FromArgb(205, 170, 120),
                    BorderStyle = BorderStyle.FixedSingle
                };
                grpNews.Controls.Add(_commTitleBoxes[i]);
                grpNews.Controls.Add(_commDateBoxes[i]);
                ny += 28;
            }
            Controls.Add(grpNews);

            // Left Middle Group: Decompiled Assets Preservation & Modding
            var grpAssets = CreateGroup("3. Decompiled Assets Folder (Modify Textures / UI / HTML)", 18, 322, 430, 130);
            var assetDesc = new Label
            {
                Text = "All 137 embedded Qt4 PNG textures, button states, progress bars, .ui XML layouts, and web/news.html are extracted to Decompiled_Assets\\ alongside the launcher so you can edit them before recompiling!",
                Location = new Point(12, 24),
                Size = new Size(406, 52),
                ForeColor = Color.FromArgb(205, 180, 145)
            };
            grpAssets.Controls.Add(assetDesc);

            var btnDecompile = CreateActionButton("Decompile Assets Now", 12, 82, 195, Color.FromArgb(45, 78, 125));
            btnDecompile.Click += (s, e) =>
            {
                SaveFieldsToConfig();
                int cnt = LauncherAssetDecompiler.DecompileLauncherAssets(_launcherFolder, _item, AppendLog);
                AppendLog($"Decompiled {cnt} embedded resources + web templates into Decompiled_Assets\\");
            };
            grpAssets.Controls.Add(btnDecompile);

            var btnOpenAssets = CreateActionButton("Open Decompiled_Assets\\", 218, 82, 198, Color.FromArgb(62, 42, 28));
            btnOpenAssets.Click += (s, e) =>
            {
                string dir = LauncherAssetDecompiler.GetDecompiledFolder(_launcherFolder);
                if (!Directory.Exists(dir))
                {
                    LauncherAssetDecompiler.DecompileLauncherAssets(_launcherFolder, _item, AppendLog);
                }
                try { Process.Start("explorer.exe", dir); } catch { }
            };
            grpAssets.Controls.Add(btnOpenAssets);
            Controls.Add(grpAssets);

            // Action Bar
            var btnRecompile = CreateActionButton("SAVE CONFIG & RECOMPILE LAUNCHER", 18, 464, 285, Color.FromArgb(175, 52, 18));
            btnRecompile.Height = 38;
            btnRecompile.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnRecompile.Click += (s, e) =>
            {
                SaveFieldsToConfig();
                bool ok = LauncherRecompiler.RecompileLauncher(_launcherFolder, _config, _item, AppendLog);
                if (ok)
                {
                    MessageBox.Show(this,
                        $"Successfully recompiled custom launcher!\n\n" +
                        $"• Custom Binary: {Path.Combine(_launcherFolder, "Blizzard Launcher Custom.exe")}\n" +
                        $"• Config: {LauncherModConfig.GetConfigPath(_launcherFolder)}\n" +
                        $"• Assets: {LauncherAssetDecompiler.GetDecompiledFolder(_launcherFolder)}",
                        "Launcher Recompiled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            Controls.Add(btnRecompile);

            var btnPreview = CreateActionButton("Visually Accurate Preview", 314, 464, 200, Color.FromArgb(128, 84, 22));
            btnPreview.Height = 38;
            btnPreview.Click += (s, e) =>
            {
                SaveFieldsToConfig();
                LauncherAssetDecompiler.GenerateEditableWebAssets(_launcherFolder, LauncherAssetDecompiler.GetDecompiledFolder(_launcherFolder), _config, AppendLog);
                using var preview = new RetroLauncherEmulator(_item ?? LauncherCatalog.AllLaunchers[0], _launcherFolder);
                preview.ShowDialog(this);
            };
            Controls.Add(btnPreview);

            var btnRunNative = CreateActionButton("Launch Native Custom EXE", 524, 464, 205, Color.FromArgb(36, 110, 56));
            btnRunNative.Height = 38;
            btnRunNative.Click += async (s, e) =>
            {
                SaveFieldsToConfig();
                LauncherRecompiler.RecompileLauncher(_launcherFolder, _config, _item, AppendLog);
                await NativeLauncherRunner.LaunchAsync(_launcherFolder, _item, AppendLog);
            };
            Controls.Add(btnRunNative);

            var btnEditJson = CreateActionButton("Open launcher-config.json", 738, 464, 164, Color.FromArgb(52, 38, 28));
            btnEditJson.Height = 38;
            btnEditJson.Click += (s, e) =>
            {
                SaveFieldsToConfig();
                string cfgPath = LauncherModConfig.GetConfigPath(_launcherFolder);
                try { Process.Start(new ProcessStartInfo("notepad.exe", $"\"{cfgPath}\"") { UseShellExecute = true }); } catch { }
            };
            Controls.Add(btnEditJson);

            _logBox = new TextBox
            {
                Location = new Point(18, 514),
                Size = new Size(884, 150),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(12, 8, 6),
                ForeColor = Color.FromArgb(165, 215, 155),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_logBox);

            AppendLog($"Loaded config from {LauncherModConfig.GetConfigPath(_launcherFolder)}");
            AppendLog($"Decompiled assets directory: {LauncherAssetDecompiler.GetDecompiledFolder(_launcherFolder)}");
        }

        private GroupBox CreateGroup(string title, int x, int y, int w, int h)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = Color.FromArgb(240, 198, 130),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }

        private TextBox AddLabeledRow(Control parent, string label, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(12, y + 3),
                Size = new Size(145, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(215, 190, 150)
            };
            var tb = new TextBox
            {
                Location = new Point(160, y),
                Size = new Size(254, 23),
                BackColor = Color.FromArgb(30, 18, 14),
                ForeColor = Color.FromArgb(245, 225, 195),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8.5f)
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(tb);
            y += 30;
            return tb;
        }

        private Button CreateActionButton(string text, int x, int y, int w, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(180, 130, 75);
            return btn;
        }

        private void PopulateFields()
        {
            _txtServerName.Text = _config.ServerName;
            _txtServerHost.Text = _config.ServerHost;
            _txtServerPort.Text = _config.ServerPort.ToString();
            _txtTargetExe.Text = _config.TargetExecutable;
            _txtLaunchArgs.Text = _config.LaunchArguments;
            _txtStatusText.Text = _config.StatusText;
            _txtRemainingText.Text = _config.TotalDownloadRemainingText;

            for (int i = 0; i < 4; i++)
            {
                if (i < _config.NewsItems.Count)
                {
                    _newsTitleBoxes[i].Text = _config.NewsItems[i].Title;
                    _newsDateBoxes[i].Text = _config.NewsItems[i].Date;
                }
                if (i < _config.CommunityItems.Count)
                {
                    _commTitleBoxes[i].Text = _config.CommunityItems[i].Title;
                    _commDateBoxes[i].Text = _config.CommunityItems[i].Date;
                }
            }
        }

        private void SaveFieldsToConfig()
        {
            _config.ServerName = _txtServerName.Text.Trim();
            _config.ServerHost = _txtServerHost.Text.Trim();
            if (int.TryParse(_txtServerPort.Text.Trim(), out int p))
                _config.ServerPort = p;
            _config.TargetExecutable = _txtTargetExe.Text.Trim();
            _config.LaunchArguments = _txtLaunchArgs.Text.Trim();
            _config.StatusText = _txtStatusText.Text.Trim();
            _config.TotalDownloadRemainingText = _txtRemainingText.Text.Trim();

            for (int i = 0; i < 4; i++)
            {
                if (i < _config.NewsItems.Count)
                {
                    _config.NewsItems[i].Title = _newsTitleBoxes[i].Text.Trim();
                    _config.NewsItems[i].Date = _newsDateBoxes[i].Text.Trim();
                }
                if (i < _config.CommunityItems.Count)
                {
                    _config.CommunityItems[i].Title = _commTitleBoxes[i].Text.Trim();
                    _config.CommunityItems[i].Date = _commDateBoxes[i].Text.Trim();
                }
            }

            _config.Save(_launcherFolder);
        }

        private void AppendLog(string msg)
        {
            if (_logBox == null || _logBox.IsDisposed) return;
            if (_logBox.InvokeRequired)
            {
                _logBox.BeginInvoke(new Action<string>(AppendLog), msg);
                return;
            }
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
        }
    }
}
