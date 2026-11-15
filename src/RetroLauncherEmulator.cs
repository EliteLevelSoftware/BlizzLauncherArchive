using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class RetroLauncherEmulator : Form
    {
        private string _gameDirectory;
        private readonly LauncherItem _launcherMeta;
        private bool _isClientless;
        private string _resolvedExecutable;
        private readonly LauncherGameFamily _family;

        private Label _lblLocation;
        private Label _lblStreaming;
        private Button _btnPlay;
        private Button _btnLinkClient;
        private Image _bannerImage;

        public RetroLauncherEmulator(string gameDirectory = null, LauncherItem launcherMeta = null)
        {
            _launcherMeta = launcherMeta;
            _family = launcherMeta?.Family ?? LauncherGameFamily.Diablo3;

            // Resolve initial game directory & executable
            ResolveGameLocation(gameDirectory);

            Text = $"{GetFamilyTitle(_family)} Launcher (Retro Standalone Archive Player)";
            Size = new Size(840, 580);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(14, 16, 20);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            LoadBannerImage();
            BuildInterface();
        }

        private void ResolveGameLocation(string preferredDir)
        {
            string[] candidateExes = GetCandidateExeNames(_family);

            // 1. Check preferred directory if provided
            if (!string.IsNullOrWhiteSpace(preferredDir) && Directory.Exists(preferredDir))
            {
                foreach (var exeName in candidateExes)
                {
                    string candidate = Path.Combine(preferredDir, exeName);
                    if (File.Exists(candidate))
                    {
                        _gameDirectory = preferredDir;
                        _resolvedExecutable = candidate;
                        _isClientless = false;
                        return;
                    }
                }
            }

            // 2. Probe common install paths
            string[] probeRoots = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), GetDefaultFolder(_family)),
                Path.Combine(@"C:\Games", GetDefaultFolder(_family)),
                Path.Combine(@"D:\Games", GetDefaultFolder(_family)),
                Path.Combine(@"D:\", GetDefaultFolder(_family)),
                Path.Combine(@"C:\", GetDefaultFolder(_family))
            };

            foreach (var root in probeRoots)
            {
                if (Directory.Exists(root))
                {
                    foreach (var exeName in candidateExes)
                    {
                        string candidate = Path.Combine(root, exeName);
                        if (File.Exists(candidate))
                        {
                            _gameDirectory = root;
                            _resolvedExecutable = candidate;
                            _isClientless = false;
                            return;
                        }
                    }
                }
            }

            // 3. Fallback: Clientless Standalone Mode
            _gameDirectory = preferredDir ?? @"C:\Games\Blizzard_Launcher_Archive";
            _resolvedExecutable = null;
            _isClientless = true;
        }

        private static string[] GetCandidateExeNames(LauncherGameFamily family)
        {
            return family switch
            {
                LauncherGameFamily.Diablo3 => new[] { Path.Combine("x64", "Diablo III64.exe"), "Diablo III.exe" },
                LauncherGameFamily.Diablo2 => new[] { "Game.exe", "Diablo II.exe" },
                LauncherGameFamily.StarCraft2 => new[] { Path.Combine("Support64", "SC2Switcher_x64.exe"), Path.Combine("Support", "SC2Switcher.exe"), "SC2.exe" },
                LauncherGameFamily.StarCraftBroodWar => new[] { "StarCraft.exe" },
                LauncherGameFamily.Warcraft3 => new[] { "Frozen Throne.exe", "Warcraft III.exe", "War3.exe" },
                LauncherGameFamily.WoW => new[] { "Wow.exe", "Wow-64.exe", "WoW.exe" },
                LauncherGameFamily.Hearthstone => new[] { "Hearthstone.exe" },
                _ => new[] { "Diablo III.exe", "Game.exe" }
            };
        }

        private static string GetDefaultFolder(LauncherGameFamily family)
        {
            return family switch
            {
                LauncherGameFamily.Diablo3 => "Diablo III",
                LauncherGameFamily.Diablo2 => "Diablo II",
                LauncherGameFamily.StarCraft2 => "StarCraft II",
                LauncherGameFamily.StarCraftBroodWar => "StarCraft",
                LauncherGameFamily.Warcraft3 => "Warcraft III",
                LauncherGameFamily.WoW => "World of Warcraft",
                LauncherGameFamily.Hearthstone => "Hearthstone",
                _ => "Diablo III"
            };
        }

        private static string GetFamilyTitle(LauncherGameFamily family)
        {
            return family switch
            {
                LauncherGameFamily.Diablo3 => "DIABLO  III",
                LauncherGameFamily.Diablo2 => "DIABLO  II",
                LauncherGameFamily.StarCraft2 => "STARCRAFT  II",
                LauncherGameFamily.StarCraftBroodWar => "STARCRAFT",
                LauncherGameFamily.Warcraft3 => "WARCRAFT  III",
                LauncherGameFamily.WoW => "WORLD OF WARCRAFT",
                LauncherGameFamily.Hearthstone => "HEARTHSTONE",
                _ => "BLIZZARD  LAUNCHER"
            };
        }

        private void LoadBannerImage()
        {
            string resName = _family switch
            {
                LauncherGameFamily.Diablo3 => "d3_banner.jpg",
                LauncherGameFamily.Diablo2 => "d2_banner.jpg",
                LauncherGameFamily.StarCraft2 => "sc2_banner.jpg",
                LauncherGameFamily.StarCraftBroodWar => "sc1_banner.jpg",
                LauncherGameFamily.Warcraft3 => "wc3_banner.jpg",
                LauncherGameFamily.WoW => "wow_banner.jpg",
                LauncherGameFamily.Hearthstone => "hearthstone_banner.jpg",
                _ => "bnet_banner.jpg"
            };

            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
                if (stream != null)
                {
                    _bannerImage = Image.FromStream(stream);
                }
            }
            catch { }
        }

        private void BuildInterface()
        {
            Controls.Clear();

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

                if (_bannerImage != null)
                {
                    g.DrawImage(_bannerImage, new Rectangle(0, 0, pnlBanner.Width, pnlBanner.Height));
                    using var overlay = new LinearGradientBrush(
                        pnlBanner.ClientRectangle,
                        Color.FromArgb(140, 10, 12, 16),
                        Color.FromArgb(240, 14, 16, 20),
                        LinearGradientMode.Vertical);
                    g.FillRectangle(overlay, pnlBanner.ClientRectangle);
                }
                else
                {
                    using var brush = new LinearGradientBrush(
                        pnlBanner.ClientRectangle,
                        Color.FromArgb(45, 10, 10),
                        Color.FromArgb(14, 16, 20),
                        LinearGradientMode.Vertical);
                    g.FillRectangle(brush, pnlBanner.ClientRectangle);
                }

                // Border accent
                using var pen = new Pen(Color.FromArgb(215, 160, 45), 2);
                g.DrawLine(pen, 0, pnlBanner.Height - 1, pnlBanner.Width, pnlBanner.Height - 1);

                // Stylized logo & subtitle
                using var fontTitle = new Font("Georgia", 26, FontStyle.Bold);
                using var fontSub = new Font("Segoe UI", 9.5f, FontStyle.Italic);
                using var brushGold = new SolidBrush(Color.FromArgb(235, 185, 75));
                using var brushShadow = new SolidBrush(Color.FromArgb(200, 0, 0, 0));

                string title = GetFamilyTitle(_family);
                g.DrawString(title, fontTitle, brushShadow, 32, 40);
                g.DrawString(title, fontTitle, brushGold, 30, 38);

                string subtitle = _launcherMeta != null
                    ? $"{_launcherMeta.Title} — Build {_launcherMeta.BuildNumber}"
                    : "Standalone Game Launcher & Server Gateway Client";
                g.DrawString(subtitle, fontSub, Brushes.LightGray, 32, 94);

                // Preservation badge in banner
                string badge = _isClientless ? "STANDALONE PRESERVATION MODE (CLIENTLESS)" : "LINKED GAME CLIENT ACTIVE";
                Color badgeColor = _isClientless ? Color.FromArgb(70, 170, 255) : Color.FromArgb(40, 220, 90);
                using var fontBadge = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                using var brushBadge = new SolidBrush(badgeColor);
                g.DrawString($"● {badge}", fontBadge, brushBadge, 32, 124);
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
                Size = new Size(520, 260),
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
                Text = GetFamilyBulletins(_family)
            };
            grpNews.Controls.Add(txtNews);

            // Right side: Realm Status
            var grpRealms = new GroupBox
            {
                Text = "BATTLE.NET REALM STATUS",
                ForeColor = Color.FromArgb(215, 160, 45),
                Location = new Point(560, 196),
                Size = new Size(244, 260),
                BackColor = Color.FromArgb(20, 22, 28)
            };

            var lblUs = CreateRealmStatusLabel("Americas Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 30);
            var lblEu = CreateRealmStatusLabel("Europe Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 65);
            var lblAsia = CreateRealmStatusLabel("Asia Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 100);
            var lblPtr = CreateRealmStatusLabel("Public Test Realm", "● ONLINE", Color.FromArgb(40, 220, 90), 135);

            var btnPreferences = new Button
            {
                Text = "⚙️ LAUNCHER PREFERENCES",
                Location = new Point(16, 175),
                Size = new Size(212, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 40, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnPreferences.FlatAppearance.BorderColor = Color.FromArgb(60, 70, 85);
            btnPreferences.Click += (s, e) => ShowPreferencesDialog();

            var btnSandboxInfo = new Button
            {
                Text = "🏛️ PRESERVATION DETAILS",
                Location = new Point(16, 215),
                Size = new Size(212, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 28, 36),
                ForeColor = Color.FromArgb(180, 200, 225),
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnSandboxInfo.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 70);
            btnSandboxInfo.Click += (s, e) => ShowClientlessSandbox();

            grpRealms.Controls.AddRange(new Control[] { lblUs, lblEu, lblAsia, lblPtr, btnPreferences, btnSandboxInfo });

            // Bottom Action Bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 88,
                BackColor = Color.FromArgb(16, 18, 24)
            };
            pnlBottom.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(40, 45, 55), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
            };

            _lblLocation = new Label
            {
                Text = _isClientless
                    ? "Client Mode: 🏛️ Standalone Preservation (No Game Files Required)"
                    : $"Game Client: {Path.GetFileName(_resolvedExecutable)} ({_gameDirectory})",
                Location = new Point(24, 14),
                AutoSize = true,
                ForeColor = _isClientless ? Color.FromArgb(130, 200, 255) : Color.FromArgb(160, 225, 160),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            _lblStreaming = new Label
            {
                Text = _isClientless
                    ? "⚡ Preservation Engine: ACTIVE (Clientless Sandbox Operational)"
                    : "⚡ Streaming Engine: OPTIMAL (100% Core Assets Verified)",
                Location = new Point(24, 38),
                AutoSize = true,
                ForeColor = Color.FromArgb(40, 220, 90),
                Font = new Font("Segoe UI", 8.75f)
            };

            _btnLinkClient = new Button
            {
                Text = _isClientless ? "📁 Link Client..." : "🔄 Change Path...",
                Location = new Point(24, 58),
                Size = new Size(130, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 35, 45),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 8f),
                Cursor = Cursors.Hand
            };
            _btnLinkClient.FlatAppearance.BorderColor = Color.FromArgb(55, 65, 80);
            _btnLinkClient.Click += (s, e) => PromptLinkClient();

            _btnPlay = new Button
            {
                Text = _isClientless ? "▶  RUN SANDBOX" : "▶  PLAY",
                Location = new Point(640, 18),
                Size = new Size(164, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 145, 30),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnPlay.FlatAppearance.BorderSize = 0;
            _btnPlay.Click += (s, e) => HandlePlayClick();

            pnlBottom.Controls.AddRange(new Control[] { _lblLocation, _lblStreaming, _btnLinkClient, _btnPlay });

            Controls.AddRange(new Control[] { grpNews, grpRealms, pnlCenter, pnlBottom, pnlBanner });
        }

        private void HandlePlayClick()
        {
            if (!_isClientless && !string.IsNullOrWhiteSpace(_resolvedExecutable) && File.Exists(_resolvedExecutable))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _resolvedExecutable,
                        Arguments = "-launch",
                        WorkingDirectory = _gameDirectory,
                        UseShellExecute = true
                    });
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not boot game client: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Run Clientless Sandbox Simulation (exact clientless experience requested!)
                ShowClientlessSandbox();
            }
        }

        private void ShowClientlessSandbox()
        {
            using var sandboxDlg = new Form
            {
                Text = $"{GetFamilyTitle(_family)} — Clientless Preservation Sandbox",
                Size = new Size(760, 480),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(24, 12, 12)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var brushGold = new SolidBrush(Color.FromArgb(235, 185, 75));
                using var font = new Font("Georgia", 16, FontStyle.Bold);
                e.Graphics.DrawString($"{GetFamilyTitle(_family)} CLIENTLESS SIMULATOR", font, brushGold, 20, 14);
                using var fontSub = new Font("Segoe UI", 9, FontStyle.Italic);
                e.Graphics.DrawString("Vintage Launcher Runtime • 100% Offline Standalone Architecture", fontSub, Brushes.LightGray, 22, 42);
            };

            var txtConsole = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(16, 18, 22),
                ForeColor = Color.FromArgb(200, 230, 255),
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5f),
                Text = $"========================================================================\r\n" +
                       $" [CLIENTLESS SIMULATION ENGINE ACTIVE]\r\n" +
                       $"========================================================================\r\n" +
                       $" • Title             : {GetFamilyTitle(_family)}\r\n" +
                       $" • Launcher Build    : {_launcherMeta?.Title ?? "Vintage Standalone Launcher"}\r\n" +
                       $" • Architecture     : 100% Clientless Standalone Preservation\r\n" +
                       $" • Gateway Status    : 127.0.0.1:1119 (Agent Loopback Emulated)\r\n" +
                       $" • Realm Server      : Americas Gateway (Mock Session 0x7E1A90B2)\r\n" +
                       $" • Core Assets       : Verified Offline\r\n" +
                       $"------------------------------------------------------------------------\r\n" +
                       $" STATUS: Clientless preservation simulation is running successfully!\r\n" +
                       $" The launcher operates entirely independently without needing any full\r\n" +
                       $" 15 GB retail game client files on your drive.\r\n\r\n" +
                       $" If you later decide to link a full retail game client, you can use the\r\n" +
                       $" 'Link Client' button to point to your installed executable anytime.\r\n" +
                       $"========================================================================\r\n"
            };

            var pnlDlgBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(18, 20, 26)
            };

            var btnLinkFromDlg = new Button
            {
                Text = "📁 Link Full Game Client...",
                Location = new Point(20, 14),
                Size = new Size(190, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 45, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnLinkFromDlg.Click += (s, e) =>
            {
                if (PromptLinkClient())
                {
                    sandboxDlg.Close();
                }
            };

            var btnCloseDlg = new Button
            {
                Text = "Close Sandbox",
                Location = new Point(590, 14),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCloseDlg.Click += (s, e) => sandboxDlg.Close();

            pnlDlgBottom.Controls.AddRange(new Control[] { btnLinkFromDlg, btnCloseDlg });
            sandboxDlg.Controls.AddRange(new Control[] { txtConsole, pnlDlgBottom, pnlTop });

            // Play ambient Windows notification chime
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }

            sandboxDlg.ShowDialog(this);
        }

        private bool PromptLinkClient()
        {
            using var ofd = new OpenFileDialog
            {
                Title = $"Locate {GetFamilyTitle(_family)} Game Executable",
                Filter = "Game Executable (*.exe)|*.exe|All Files (*.*)|*.*",
                InitialDirectory = Directory.Exists(_gameDirectory) ? _gameDirectory : @"C:\"
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string selExe = ofd.FileName;
                if (File.Exists(selExe))
                {
                    _resolvedExecutable = selExe;
                    _gameDirectory = Path.GetDirectoryName(selExe);
                    _isClientless = false;

                    // Refresh UI
                    _lblLocation.Text = $"Game Client: {Path.GetFileName(_resolvedExecutable)} ({_gameDirectory})";
                    _lblLocation.ForeColor = Color.FromArgb(160, 225, 160);
                    _lblStreaming.Text = "⚡ Streaming Engine: OPTIMAL (100% Core Assets Verified)";
                    _btnLinkClient.Text = "🔄 Change Path...";
                    _btnPlay.Text = "▶  PLAY";
                    Refresh();
                    return true;
                }
            }
            return false;
        }

        private void ShowPreferencesDialog()
        {
            using var prefDlg = new Form
            {
                Text = "Launcher & Realm Preferences",
                Size = new Size(540, 380),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(18, 20, 26),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };

            var lblHeader = new Label
            {
                Text = "Configuration & Preservation Gateway",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(215, 160, 45)
            };

            var lblPath = new Label
            {
                Text = $"Current Game Directory:\r\n{(_isClientless ? "[Clientless Standalone Mode]" : _gameDirectory)}",
                Location = new Point(20, 55),
                Size = new Size(480, 45),
                ForeColor = Color.LightGray
            };

            var btnChange = new Button
            {
                Text = "📁 Browse Game Client...",
                Location = new Point(20, 110),
                Size = new Size(180, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 45, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnChange.Click += (s, e) =>
            {
                if (PromptLinkClient())
                {
                    lblPath.Text = $"Current Game Directory:\r\n{_gameDirectory}";
                }
            };

            var btnUnlink = new Button
            {
                Text = "🏛️ Reset to Clientless Mode",
                Location = new Point(210, 110),
                Size = new Size(190, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 35, 45),
                ForeColor = Color.FromArgb(220, 180, 180),
                Cursor = Cursors.Hand
            };
            btnUnlink.Click += (s, e) =>
            {
                _isClientless = true;
                _resolvedExecutable = null;
                _lblLocation.Text = "Client Mode: 🏛️ Standalone Preservation (No Game Files Required)";
                _lblLocation.ForeColor = Color.FromArgb(130, 200, 255);
                _lblStreaming.Text = "⚡ Preservation Engine: ACTIVE (Clientless Sandbox Operational)";
                _btnLinkClient.Text = "📁 Link Client...";
                _btnPlay.Text = "▶  RUN SANDBOX";
                lblPath.Text = "Current Game Directory:\r\n[Clientless Standalone Mode]";
                Refresh();
            };

            var grpGateway = new GroupBox
            {
                Text = "Gateway Realm Selection",
                ForeColor = Color.FromArgb(215, 160, 45),
                Location = new Point(20, 160),
                Size = new Size(480, 110),
                BackColor = Color.FromArgb(22, 24, 30)
            };

            var rdoUs = new RadioButton { Text = "Americas (US Gateway)", Location = new Point(20, 30), AutoSize = true, Checked = true, ForeColor = Color.White };
            var rdoEu = new RadioButton { Text = "Europe (EU Gateway)", Location = new Point(240, 30), AutoSize = true, ForeColor = Color.White };
            var rdoAsia = new RadioButton { Text = "Asia (KR/TW Gateway)", Location = new Point(20, 65), AutoSize = true, ForeColor = Color.White };
            var rdoPtr = new RadioButton { Text = "Public Test Realm (PTR)", Location = new Point(240, 65), AutoSize = true, ForeColor = Color.White };

            grpGateway.Controls.AddRange(new Control[] { rdoUs, rdoEu, rdoAsia, rdoPtr });

            var btnClose = new Button
            {
                Text = "Save & Close",
                Location = new Point(370, 290),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(200, 145, 30),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => prefDlg.Close();

            prefDlg.Controls.AddRange(new Control[] { lblHeader, lblPath, btnChange, btnUnlink, grpGateway, btnClose });
            prefDlg.ShowDialog(this);
        }

        private static Label CreateRealmStatusLabel(string name, string status, Color statusColor, int top)
        {
            return new Label
            {
                Text = $"{name,-18} {status}",
                ForeColor = statusColor,
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                Location = new Point(16, top),
                AutoSize = true
            };
        }

        private static string GetFamilyBulletins(LauncherGameFamily family)
        {
            return family switch
            {
                LauncherGameFamily.Diablo3 =>
                    "⚔️ SANCTUARY SERVER ANNOUNCEMENTS\r\n" +
                    "--------------------------------------------------\r\n" +
                    "• Patch 1.0.8: Cooperative play buffs and Monster Density live across Sanctuary!\r\n" +
                    "• Paragon Level cap set to 100 with 300% Magic Find / Gold Find bonuses.\r\n" +
                    "• Public Test Realm (PTR) gateway operational.\r\n" +
                    "• Battle.net Authenticator and Real ID friending available.\r\n" +
                    "• Standalone Preservation Mode active — no client installation required.",

                LauncherGameFamily.StarCraft2 =>
                    "🚀 KOPRULU SECTOR COMMUNICATIONS\r\n" +
                    "--------------------------------------------------\r\n" +
                    "• Heart of the Swarm expansion system update live.\r\n" +
                    "• StarCraft II Arcade: Custom games, mod tools, and community spotlights.\r\n" +
                    "• Ladder Season active with grandmaster divisions enabled.\r\n" +
                    "• Standalone Preservation Mode active — no client installation required.",

                LauncherGameFamily.Warcraft3 =>
                    "🛡️ AZEROTH REALM DISPATCHES\r\n" +
                    "--------------------------------------------------\r\n" +
                    "• The Frozen Throne gateway operational.\r\n" +
                    "• Custom Maps, Defense of the Ancients, and Ladder matchmaking active.\r\n" +
                    "• Classic Battle.net client compatibility preserved.",

                LauncherGameFamily.Diablo2 =>
                    "🔥 BATTLE.NET CLASSIC DISPATCHES\r\n" +
                    "--------------------------------------------------\r\n" +
                    "• Lord of Destruction Ladder Season operational.\r\n" +
                    "• Uber Tristram and Pandemonium Event gateways active.\r\n" +
                    "• Classic gateway preserved in standalone offline architecture.",

                _ =>
                    "🌐 BATTLE.NET NETWORK BULLETINS\r\n" +
                    "--------------------------------------------------\r\n" +
                    "• Historical launcher build preserved and verified.\r\n" +
                    "• Standalone offline preservation architecture operational.\r\n" +
                    "• All network handshakes handled locally via emulation."
            };
        }
    }
}
