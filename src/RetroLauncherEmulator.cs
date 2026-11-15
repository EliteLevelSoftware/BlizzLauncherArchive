using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class RetroLauncherEmulator : Form
    {
        private readonly string _gameDirectory;
        private readonly LauncherItem _launcherMeta;

        public RetroLauncherEmulator(string gameDirectory = null, LauncherItem launcherMeta = null)
        {
            _gameDirectory = gameDirectory ?? @"C:\Program Files (x86)\Diablo III";
            _launcherMeta = launcherMeta;

            Text = "Diablo III Launcher (Retro Standalone Archive Player)";
            Size = new Size(820, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(14, 16, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            BuildInterface();
        }

        private void BuildInterface()
        {
            // Top Artwork Banner
            var pnlBanner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                BackColor = Color.FromArgb(20, 8, 8)
            };
            pnlBanner.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using var brush = new LinearGradientBrush(pnlBanner.ClientRectangle, Color.FromArgb(45, 10, 10), Color.FromArgb(14, 16, 20), LinearGradientMode.Vertical);
                g.FillRectangle(brush, pnlBanner.ClientRectangle);

                // Border accent
                using var pen = new Pen(Color.FromArgb(215, 160, 45), 2);
                g.DrawLine(pen, 0, pnlBanner.Height - 1, pnlBanner.Width, pnlBanner.Height - 1);

                // Diablo III stylized logo
                using var fontTitle = new Font("Georgia", 28, FontStyle.Bold);
                using var fontSub = new Font("Segoe UI", 10, FontStyle.Italic);
                using var brushGold = new SolidBrush(Color.FromArgb(235, 185, 75));
                using var brushShadow = new SolidBrush(Color.FromArgb(180, 0, 0, 0));

                g.DrawString("DIABLO  III", fontTitle, brushShadow, 32, 38);
                g.DrawString("DIABLO  III", fontTitle, brushGold, 30, 36);

                string subtitle = _launcherMeta != null
                    ? $"{_launcherMeta.Title} — Build {_launcherMeta.BuildNumber}"
                    : "Standalone Game Launcher & Server Gateway Client";
                g.DrawString(subtitle, fontSub, Brushes.LightGray, 34, 96);
            };

            // Main Content Area
            var pnlCenter = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            // Left side: News / Community feed
            var grpNews = new GroupBox
            {
                Text = "COMMUNITY NEWS & REALM BULLETINS",
                ForeColor = Color.FromArgb(215, 160, 45),
                Location = new Point(24, 196),
                Size = new Size(500, 240),
                BackColor = Color.FromArgb(20, 22, 28)
            };

            var txtNews = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 18, 22),
                ForeColor = Color.Gainsboro,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.25f),
                Text = "⚔️ SANCTUARY SERVER ANNOUNCEMENTS\r\n" +
                       "--------------------------------------------------\r\n" +
                       "• Patch 1.0.8: Cooperative play buffs and Monster Density in Acts I, II, and IV live!\r\n" +
                       "• Paragon Level cap set to 100 with 300% Magic Find / Gold Find bonuses.\r\n" +
                       "• Public Test Realm (PTR) gateway operational.\r\n" +
                       "• Battle.net Authenticator and Real ID friending available.\r\n" +
                       "• Use '-launch' switch to boot game clients directly or connect to custom realm servers."
            };
            grpNews.Controls.Add(txtNews);

            // Right side: Realm Status
            var grpRealms = new GroupBox
            {
                Text = "BATTLE.NET REALM STATUS",
                ForeColor = Color.FromArgb(215, 160, 45),
                Location = new Point(540, 196),
                Size = new Size(244, 240),
                BackColor = Color.FromArgb(20, 22, 28)
            };

            var lblUs = CreateRealmStatusLabel("Americas Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 30);
            var lblEu = CreateRealmStatusLabel("Europe Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 65);
            var lblAsia = CreateRealmStatusLabel("Asia Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 100);
            var lblPtr = CreateRealmStatusLabel("Public Test Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 135);

            var btnOptions = new Button
            {
                Text = "⚙️ GAME PREFERENCES",
                Location = new Point(20, 185),
                Size = new Size(204, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 40, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOptions.FlatAppearance.BorderColor = Color.FromArgb(60, 70, 85);
            btnOptions.Click += (s, e) =>
            {
                MessageBox.Show(
                    $"Configured Game Directory:\r\n{_gameDirectory}\r\n\r\nLaunch Arguments:\r\n-launch -enUS\r\n\r\nAudio & Video settings are preserved in D3Prefs.txt.",
                    "Game Preferences & Realm Configuration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };

            grpRealms.Controls.AddRange(new Control[] { lblUs, lblEu, lblAsia, lblPtr, btnOptions });

            // Bottom Action Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                BackColor = Color.FromArgb(16, 18, 24)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 45, 55), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
            };

            var lblLocation = new Label
            {
                Text = $"Game Path: {_gameDirectory}",
                Location = new Point(24, 16),
                AutoSize = true,
                ForeColor = Color.DarkGray,
                Font = new Font("Segoe UI", 8.5f)
            };

            var lblStreaming = new Label
            {
                Text = "⚡ Streaming Engine: OPTIMAL (100% Core Assets Verified)",
                Location = new Point(24, 38),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 220, 90),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            var btnPlay = new Button
            {
                Text = "▶  PLAY",
                Location = new Point(640, 16),
                Size = new Size(144, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 145, 30),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            btnPlay.Click += (s, e) =>
            {
                string exe64 = Path.Combine(_gameDirectory, "x64", "Diablo III64.exe");
                string exe32 = Path.Combine(_gameDirectory, "Diablo III.exe");
                string targetExe = File.Exists(exe64) ? exe64 : (File.Exists(exe32) ? exe32 : null);

                if (targetExe != null)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = targetExe,
                            Arguments = "-launch",
                            WorkingDirectory = _gameDirectory,
                            UseShellExecute = true
                        });
                        Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not boot game: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"Diablo III executable not found in:\r\n{_gameDirectory}\r\n\r\nPlease select or install a build with Diablo 3 Forever first!",
                        "Executable Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            pnlBottom.Controls.AddRange(new Control[] { lblLocation, lblStreaming, btnPlay });

            Controls.AddRange(new Control[] { grpNews, grpRealms, pnlCenter, pnlBottom, pnlBanner });
        }

        private static Label CreateRealmStatusLabel(string name, string status, Color statusColor, int top)
        {
            var lbl = new Label
            {
                Text = $"{name,-18} {status}",
                ForeColor = statusColor,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(16, top),
                AutoSize = true
            };
            return lbl;
        }
    }
}
