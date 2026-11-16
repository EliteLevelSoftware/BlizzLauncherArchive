using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public class MockWebsiteDialog : Form
    {
        private readonly LauncherModConfig _config;
        private readonly string _launcherFolder;
        private readonly string _initialSection;
        private readonly string _highlightTitle;
        private Panel _contentPanel;
        private Label _urlBarLabel;

        public MockWebsiteDialog(LauncherModConfig config, string launcherFolder, string initialSection = "news", string highlightTitle = null)
        {
            _config = config ?? LauncherModConfig.CreateDefault();
            _launcherFolder = launcherFolder;
            _initialSection = initialSection ?? "news";
            _highlightTitle = highlightTitle;

            Text = $"{_config.ServerName} — Battle.net Sanctuary Community Portal (Mock Site)";
            ClientSize = new Size(880, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(16, 8, 6);
            ForeColor = Color.FromArgb(225, 198, 153);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            BuildUi();
            SwitchTab(_initialSection);
        }

        private void BuildUi()
        {
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 86,
                BackColor = Color.FromArgb(28, 12, 8)
            };
            topBar.Paint += (s, e) =>
            {
                using var brush = new LinearGradientBrush(topBar.ClientRectangle, Color.FromArgb(48, 18, 10), Color.FromArgb(20, 8, 5), 90f);
                e.Graphics.FillRectangle(brush, topBar.ClientRectangle);
                using var pen = new Pen(Color.FromArgb(110, 68, 34), 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };

            var titleLbl = new Label
            {
                Text = $"SANCTUARY COMMUNITY PORTAL — {_config.ServerName.ToUpperInvariant()}",
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 215, 155),
                BackColor = Color.Transparent,
                Location = new Point(18, 10),
                AutoSize = true
            };
            topBar.Controls.Add(titleLbl);

            _urlBarLabel = new Label
            {
                Text = $"URL: {_config.BattleNetPortalUrl}  |  Realm Gateway: {_config.ServerHost}:{_config.ServerPort} (ONLINE)",
                Font = new Font("Consolas", 8.5f),
                ForeColor = Color.FromArgb(175, 140, 95),
                BackColor = Color.Transparent,
                Location = new Point(20, 34),
                AutoSize = true
            };
            topBar.Controls.Add(_urlBarLabel);

            string[] tabs = { "News & Articles", "Community & Fan Art", "Realm & Server Status", "Patch Notes & Help" };
            string[] keys = { "news", "community", "status", "patchnotes" };
            int bx = 18;
            for (int i = 0; i < tabs.Length; i++)
            {
                string key = keys[i];
                var btn = new Button
                {
                    Text = tabs[i],
                    Location = new Point(bx, 54),
                    Size = new Size(165, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(52, 24, 14),
                    ForeColor = Color.FromArgb(240, 205, 145),
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(120, 74, 36);
                btn.Click += (s, e) => SwitchTab(key);
                topBar.Controls.Add(btn);
                bx += 172;
            }

            if (!string.IsNullOrWhiteSpace(_launcherFolder))
            {
                var editBtn = new Button
                {
                    Text = "Edit in Mod Studio...",
                    Location = new Point(718, 54),
                    Size = new Size(144, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(145, 48, 18),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                editBtn.FlatAppearance.BorderColor = Color.FromArgb(220, 130, 60);
                editBtn.Click += (s, e) =>
                {
                    using var dlg = new LauncherModStudioDialog(_launcherFolder);
                    dlg.ShowDialog(this);
                    SwitchTab(_initialSection);
                };
                topBar.Controls.Add(editBtn);
            }

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(16, 8, 6)
            };

            Controls.Add(_contentPanel);
            Controls.Add(topBar);
        }

        private void SwitchTab(string section)
        {
            _contentPanel.Controls.Clear();
            int y = 14;

            if (section == "news")
            {
                _urlBarLabel.Text = $"URL: http://127.0.0.1:{_config.LocalWebFeedPort}/d3/news  |  Server: {_config.ServerHost}:{_config.ServerPort}";
                AddSectionHeading("LATEST DIABLO III & PRIVATE SERVER NEWS", ref y);
                foreach (var item in _config.NewsItems)
                {
                    bool highlight = !string.IsNullOrEmpty(_highlightTitle) &&
                                     item.Title.IndexOf(_highlightTitle, StringComparison.OrdinalIgnoreCase) >= 0;
                    AddArticleCard(item.Title, item.Date, item.Url, item.Summary, highlight, ref y);
                }
            }
            else if (section == "community")
            {
                _urlBarLabel.Text = $"URL: http://127.0.0.1:{_config.LocalWebFeedPort}/d3/community  |  Server: {_config.ServerHost}:{_config.ServerPort}";
                AddSectionHeading("COMMUNITY SPOTLIGHT, CONTESTS & FAN ART", ref y);
                foreach (var item in _config.CommunityItems)
                {
                    bool highlight = !string.IsNullOrEmpty(_highlightTitle) &&
                                     item.Title.IndexOf(_highlightTitle, StringComparison.OrdinalIgnoreCase) >= 0;
                    AddArticleCard(item.Title, item.Date, item.Url, item.Summary, highlight, ref y);
                }
            }
            else if (section == "status")
            {
                _urlBarLabel.Text = $"URL: http://127.0.0.1:{_config.LocalWebFeedPort}/d3/status  |  Realm: {_config.ServerHost}:{_config.ServerPort}";
                AddSectionHeading("PRIVATE SERVER & REALM CONNECTION CONFIGURATION", ref y);
                AddArticleCard(
                    $"{_config.ServerName} — Gateway Active",
                    "ONLINE (12 ms)",
                    $"bnet://{_config.ServerHost}:{_config.ServerPort}",
                    $"Target Executable: {_config.TargetExecutable}\n" +
                    $"Launch Arguments: {_config.LaunchArguments}\n" +
                    $"Patch Server URL: {_config.PatchServerUrl}\n" +
                    $"Launcher Status Text: {_config.StatusText}\n" +
                    $"Stage 1: {_config.Stage1Title} {_config.Stage1SubText}  |  Stage 2: {_config.Stage2Title} {_config.Stage2SubText}  |  Stage 3: {_config.Stage3Title} {_config.Stage3SubText}",
                    true,
                    ref y);
            }
            else
            {
                _urlBarLabel.Text = $"URL: http://127.0.0.1:{_config.LocalWebFeedPort}/d3/patchnotes  |  Build Archive";
                AddSectionHeading("HISTORICAL PATCH NOTES & LAUNCHER OPTIONS", ref y);
                AddArticleCard(
                    "Blizzard Streaming Launcher — Preservation & Modding Notes",
                    "Build 888 / 16603",
                    _config.PatchServerUrl,
                    "• 3-Stage Streaming Bar (Setup, Available, Playable) active.\n" +
                    "• Embedded Qt4 UI textures, .ui XML layouts, and WebKit HTML extracted to Decompiled_Assets/.\n" +
                    "• Private server operators can edit launcher-config.json and any PNG/HTML in Decompiled_Assets/ and recompile!",
                    true,
                    ref y);
            }
        }

        private void AddSectionHeading(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Georgia", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(238, 196, 128),
                Location = new Point(18, y),
                AutoSize = true
            };
            _contentPanel.Controls.Add(lbl);
            y += 30;
        }

        private void AddArticleCard(string title, string date, string url, string body, bool highlighted, ref int y)
        {
            var card = new Panel
            {
                Location = new Point(18, y),
                Size = new Size(820, 106),
                BackColor = highlighted ? Color.FromArgb(44, 20, 12) : Color.FromArgb(26, 12, 8)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(highlighted ? Color.FromArgb(220, 125, 45) : Color.FromArgb(78, 44, 22), highlighted ? 2 : 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var tLbl = new Label
            {
                Text = title,
                Font = new Font("Georgia", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 212, 148),
                Location = new Point(14, 10),
                AutoSize = true
            };
            card.Controls.Add(tLbl);

            var dLbl = new Label
            {
                Text = date,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(190, 140, 82),
                Location = new Point(680, 10),
                Size = new Size(125, 18),
                TextAlign = ContentAlignment.TopRight
            };
            card.Controls.Add(dLbl);

            var bLbl = new Label
            {
                Text = body,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(205, 180, 142),
                Location = new Point(14, 34),
                Size = new Size(790, 50)
            };
            card.Controls.Add(bLbl);

            var uLbl = new Label
            {
                Text = $"Link: {url}",
                Font = new Font("Consolas", 8f),
                ForeColor = Color.FromArgb(145, 110, 72),
                Location = new Point(14, 84),
                AutoSize = true
            };
            card.Controls.Add(uLbl);

            _contentPanel.Controls.Add(card);
            y += 116;
        }
    }
}
