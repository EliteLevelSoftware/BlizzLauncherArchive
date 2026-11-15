using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class RetroLauncherEmulator : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private string _gameDirectory;
        private readonly LauncherItem _launcherMeta;
        private bool _isClientless;
        private string _resolvedExecutable;
        private readonly LauncherGameFamily _family;

        private Image _masterImage;
        private Rectangle _rcClose = new Rectangle(760, 84, 25, 23);
        private Rectangle _rcMin = new Rectangle(734, 84, 25, 23);
        private Rectangle _rcOptions = new Rectangle(76, 84, 70, 22);
        private Rectangle _rcInfo = new Rectangle(164, 84, 46, 22);
        private Rectangle _rcHelp = new Rectangle(222, 84, 48, 22);
        private Rectangle _rcPlay = new Rectangle(564, 436, 192, 68);

        // News & Community areas
        private Rectangle _rcNewsAll = new Rectangle(680, 218, 70, 18);
        private Rectangle _rcCommAll = new Rectangle(680, 378, 70, 18);
        private Rectangle _rcProgressBar = new Rectangle(70, 435, 480, 68);

        private int _hoveredButton = 0; // 1: Close, 2: Min, 3: Options, 4: Info, 5: Help, 6: Play, 7: NewsAll, 8: CommAll, 9: Progress
        private readonly ToolTip _toolTip;

        public RetroLauncherEmulator(string gameDirectory = null, LauncherItem launcherMeta = null)
        {
            _launcherMeta = launcherMeta;
            _family = launcherMeta?.Family ?? LauncherGameFamily.Diablo3;

            ResolveGameLocation(gameDirectory);

            Text = $"{GetFamilyTitle(_family)} Launcher (Retro Preservation Player)";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _toolTip = new ToolTip
            {
                BackColor = Color.FromArgb(20, 22, 28),
                ForeColor = Color.FromArgb(215, 160, 45),
                OwnerDraw = true
            };
            _toolTip.Draw += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(24, 26, 32)), e.Bounds);
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(215, 160, 45), 1), new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
                using var font = new Font("Segoe UI", 9f);
                e.Graphics.DrawString(e.ToolTipText, font, Brushes.LightGray, new PointF(e.Bounds.X + 6, e.Bounds.Y + 4));
            };

            LoadMasterAsset();

            if (_masterImage != null)
            {
                Size = new Size(_masterImage.Width, _masterImage.Height);
                BackColor = Color.Magenta;
                TransparencyKey = Color.Magenta;
            }
            else
            {
                Size = new Size(840, 580);
                BackColor = Color.FromArgb(14, 16, 20);
                ForeColor = Color.White;
            }
        }

        private void ResolveGameLocation(string preferredDir)
        {
            string[] candidateExes = GetCandidateExeNames(_family);

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

        private void LoadMasterAsset()
        {
            string resName = _family == LauncherGameFamily.Diablo3 ? "d3_retro_launcher.png" : null;

            if (resName != null)
            {
                try
                {
                    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        _masterImage = Image.FromStream(stream);
                    }
                }
                catch { }
            }

            if (_masterImage == null && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "d3_retro_launcher.png")))
            {
                try
                {
                    _masterImage = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "d3_retro_launcher.png"));
                }
                catch { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_masterImage != null)
            {
                // Clear to Magenta transparency key
                g.Clear(Color.Magenta);
                g.DrawImage(_masterImage, 0, 0, Width, Height);

                // Draw Interactive Hover Overlays
                if (_hoveredButton == 1) // Close
                {
                    using var brush = new SolidBrush(Color.FromArgb(70, 220, 40, 40));
                    g.FillRectangle(brush, _rcClose);
                    using var pen = new Pen(Color.FromArgb(200, 255, 60, 60), 1.5f);
                    g.DrawRectangle(pen, _rcClose);
                }
                else if (_hoveredButton == 2) // Min
                {
                    using var brush = new SolidBrush(Color.FromArgb(60, 200, 160, 40));
                    g.FillRectangle(brush, _rcMin);
                    using var pen = new Pen(Color.FromArgb(200, 220, 180, 50), 1.5f);
                    g.DrawRectangle(pen, _rcMin);
                }
                else if (_hoveredButton == 3) // Options
                {
                    using var pen = new Pen(Color.FromArgb(220, 215, 160, 45), 2);
                    g.DrawLine(pen, _rcOptions.Left + 4, _rcOptions.Bottom - 2, _rcOptions.Right - 4, _rcOptions.Bottom - 2);
                }
                else if (_hoveredButton == 4) // Info
                {
                    using var pen = new Pen(Color.FromArgb(220, 215, 160, 45), 2);
                    g.DrawLine(pen, _rcInfo.Left + 4, _rcInfo.Bottom - 2, _rcInfo.Right - 4, _rcInfo.Bottom - 2);
                }
                else if (_hoveredButton == 5) // Help
                {
                    using var pen = new Pen(Color.FromArgb(220, 215, 160, 45), 2);
                    g.DrawLine(pen, _rcHelp.Left + 4, _rcHelp.Bottom - 2, _rcHelp.Right - 4, _rcHelp.Bottom - 2);
                }
                else if (_hoveredButton == 6) // PLAY
                {
                    using var pen = new Pen(Color.FromArgb(180, 255, 180, 50), 2.5f);
                    using var brush = new SolidBrush(Color.FromArgb(35, 255, 180, 40));
                    g.FillRectangle(brush, _rcPlay);
                    g.DrawRectangle(pen, _rcPlay);
                }
                else if (_hoveredButton == 7) // News View All
                {
                    using var pen = new Pen(Color.FromArgb(200, 255, 120, 40), 1.5f);
                    g.DrawLine(pen, _rcNewsAll.Left, _rcNewsAll.Bottom, _rcNewsAll.Right, _rcNewsAll.Bottom);
                }
                else if (_hoveredButton == 8) // Community View All
                {
                    using var pen = new Pen(Color.FromArgb(200, 255, 120, 40), 1.5f);
                    g.DrawLine(pen, _rcCommAll.Left, _rcCommAll.Bottom, _rcCommAll.Right, _rcCommAll.Bottom);
                }
                else if (_hoveredButton == 9) // Progress bar
                {
                    using var pen = new Pen(Color.FromArgb(100, 200, 60, 30), 1.5f);
                    g.DrawRectangle(pen, _rcProgressBar);
                }

                // If in Clientless mode, draw a discreet mode indicator under the frame
                string modeText = _isClientless ? "● STANDALONE PRESERVATION MODE (CLIENTLESS)" : "● LINKED GAME CLIENT ACTIVE";
                Color modeCol = _isClientless ? Color.FromArgb(200, 80, 180, 255) : Color.FromArgb(200, 50, 230, 100);
                using var fontMode = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                using var brushMode = new SolidBrush(modeCol);
                g.DrawString(modeText, fontMode, brushMode, 76, 526);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int prevHover = _hoveredButton;

            if (_rcClose.Contains(e.Location)) _hoveredButton = 1;
            else if (_rcMin.Contains(e.Location)) _hoveredButton = 2;
            else if (_rcOptions.Contains(e.Location)) _hoveredButton = 3;
            else if (_rcInfo.Contains(e.Location)) _hoveredButton = 4;
            else if (_rcHelp.Contains(e.Location)) _hoveredButton = 5;
            else if (_rcPlay.Contains(e.Location)) _hoveredButton = 6;
            else if (_rcNewsAll.Contains(e.Location)) _hoveredButton = 7;
            else if (_rcCommAll.Contains(e.Location)) _hoveredButton = 8;
            else if (_rcProgressBar.Contains(e.Location)) _hoveredButton = 9;
            else _hoveredButton = 0;

            if (_hoveredButton != 0)
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }

            if (prevHover != _hoveredButton)
            {
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                if (_rcClose.Contains(e.Location))
                {
                    Close();
                    return;
                }
                if (_rcMin.Contains(e.Location))
                {
                    WindowState = FormWindowState.Minimized;
                    return;
                }
                if (_rcOptions.Contains(e.Location))
                {
                    ShowPreferencesDialog();
                    return;
                }
                if (_rcInfo.Contains(e.Location))
                {
                    ShowInfoDialog();
                    return;
                }
                if (_rcHelp.Contains(e.Location))
                {
                    ShowHelpDialog();
                    return;
                }
                if (_rcPlay.Contains(e.Location))
                {
                    HandlePlayClick();
                    return;
                }
                if (_rcNewsAll.Contains(e.Location) || _rcCommAll.Contains(e.Location))
                {
                    ShowNewsDialog();
                    return;
                }
                if (_rcProgressBar.Contains(e.Location))
                {
                    ShowStreamingEngineStatus();
                    return;
                }

                // Drag window
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
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
                ShowClientlessSandbox();
            }
        }

        private void ShowClientlessSandbox()
        {
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }

            using var sandboxDlg = new Form
            {
                Text = "Diablo III — Sanctuary Gateway Session Simulator",
                Size = new Size(760, 480),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(14, 16, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(28, 12, 12)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var brushGold = new SolidBrush(Color.FromArgb(235, 185, 75));
                using var font = new Font("Georgia", 16, FontStyle.Bold);
                e.Graphics.DrawString("DIABLO III — SANCTUARY GATEWAY SIMULATOR", font, brushGold, 20, 14);
                using var fontSub = new Font("Segoe UI", 9, FontStyle.Italic);
                e.Graphics.DrawString("Authentic 2011/2012 Vintage Launcher Preserved Environment", fontSub, Brushes.LightGray, 22, 42);
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
                Text = "========================================================================\r\n" +
                       " [SANCTUARY GATEWAY SESSION ESTABLISHED]\r\n" +
                       "========================================================================\r\n" +
                       " • Product Identity  : Diablo III (D3 / Retail 1.1.0 / Beta)\r\n" +
                       " • Mode              : 100% Clientless Standalone Preservation\r\n" +
                       " • Gateway Loopback  : 127.0.0.1:1119 (Agent Handshake Successful)\r\n" +
                       " • Realm Server      : Americas Gateway (Session ID: 0x7E1A90B2)\r\n" +
                       " • Core Streaming    : 100% Core Assets Verified (Setup & Playable)\r\n" +
                       " • Audio & Video     : Initialized (D3Prefs Emulated)\r\n" +
                       "------------------------------------------------------------------------\r\n" +
                       " STATUS: The classic Diablo III launcher interface is running\r\n" +
                       " in complete offline preservation mode. No 15 GB retail game files\r\n" +
                       " are required to experience, inspect, and enjoy this vintage artwork.\r\n\r\n" +
                       " To connect a full Diablo III game client, click 'Link Game Client' below\r\n" +
                       " or open the OPTIONS menu anytime.\r\n" +
                       "========================================================================\r\n"
            };

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(18, 20, 26)
            };

            var btnLink = new Button
            {
                Text = "📁 Link Full Game Client...",
                Location = new Point(20, 14),
                Size = new Size(190, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(35, 45, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnLink.Click += (s, e) =>
            {
                if (PromptLinkClient())
                {
                    sandboxDlg.Close();
                }
            };

            var btnClose = new Button
            {
                Text = "Close Sandbox",
                Location = new Point(590, 14),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => sandboxDlg.Close();

            pnlBottom.Controls.AddRange(new Control[] { btnLink, btnClose });
            sandboxDlg.Controls.AddRange(new Control[] { txtConsole, pnlBottom, pnlTop });
            sandboxDlg.ShowDialog(this);
        }

        private bool PromptLinkClient()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Locate Diablo III Executable (Diablo III.exe)",
                Filter = "Diablo III Executable (*.exe)|*.exe|All Files (*.*)|*.*",
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
                    Invalidate();
                    return true;
                }
            }
            return false;
        }

        private void ShowPreferencesDialog()
        {
            using var prefDlg = new Form
            {
                Text = "Diablo III Launcher — Game Preferences & Realm Gateway",
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
                Text = "Sanctuary Gateway Configuration",
                Location = new Point(20, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(215, 160, 45)
            };

            var lblPath = new Label
            {
                Text = $"Current Game Directory:\r\n{(_isClientless ? "[Standalone Preservation Mode — Clientless]" : _gameDirectory)}",
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
                    Invalidate();
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
                lblPath.Text = "Current Game Directory:\r\n[Standalone Preservation Mode — Clientless]";
                Invalidate();
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

        private void ShowInfoDialog()
        {
            MessageBox.Show(
                "Diablo III Launcher (Preservation Suite v1.0.4)\r\n\r\n" +
                "• Architecture: Standalone Historical Retro Player\r\n" +
                "• Original Build: Retail 1.1.0 (Build 16603) / Beta Milestone\r\n" +
                "• Release Target: May 15, 2012 Sanctuary Launch Lineage\r\n\r\n" +
                "Preserved by Elite Level Software.",
                "Diablo III Launcher Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ShowHelpDialog()
        {
            MessageBox.Show(
                "Diablo III Launcher Help & Diagnostics:\r\n\r\n" +
                "• Drag the window by clicking anywhere on the gothic metallic frame.\r\n" +
                "• Click PLAY to boot into the clientless sandbox or launch a linked game.\r\n" +
                "• Open OPTIONS to point to your Diablo III installation on any drive.\r\n" +
                "• 100% offline standalone mode is active.",
                "Diablo III Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ShowNewsDialog()
        {
            MessageBox.Show(
                "Diablo III Historical News Bulletin (September 2011):\r\n\r\n" +
                "• Gold AH Added to Friends & Family (Sep 16, 2011)\r\n" +
                "• Diablo III Beta Patch 3 Live (Sep 14, 2011)\r\n" +
                "• The Shared Stash & Artisans Deep Dive (Sep 13, 2011)\r\n" +
                "• Community Site Levels Up With Skill Calculator (Sep 13, 2011)\r\n" +
                "• Beta Opt-in and FAQ Operational (Sep 8, 2011)",
                "Sanctuary Community News & Bulletins",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ShowStreamingEngineStatus()
        {
            MessageBox.Show(
                "Blizzard Streaming Engine Diagnostics:\r\n\r\n" +
                "• Stage 1 (Setup)     : 933 MB / 933 MB (100% Complete)\r\n" +
                "• Stage 2 (Available) : 1.57 GB / 1.57 GB (Verified)\r\n" +
                "• Stage 3 (Playable)  : 210 MB / 210 MB (Optimal)\r\n\r\n" +
                "Core streaming status: READY TO PLAY.",
                "Streaming Engine Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
