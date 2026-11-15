using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }

    public sealed class LauncherSelectorForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int BANNER_HEIGHT = 160;

        // Embedded Art
        private readonly Image _logoImg;
        private readonly Image _bnetBannerImg;
        private readonly Image _d3BannerImg;
        private readonly Image _wowBannerImg;
        private readonly Image _sc2BannerImg;
        private readonly Image _classicBannerImg;

        // Selected items per family
        private LauncherItem _selectedBnet;
        private LauncherItem _selectedD3;
        private LauncherItem _selectedWow;
        private LauncherItem _selectedSc2;
        private LauncherItem _selectedClassic;

        // Expansion states
        private bool _bnetExpanded = true;
        private bool _d3Expanded = true;
        private bool _wowExpanded = false;
        private bool _sc2Expanded = false;
        private bool _classicExpanded = false;
        private bool _logExpanded = false;
        private bool _isArchiving = false;

        // Options
        private bool _chkFastBinariesOnly = false;
        private string _searchFilter = string.Empty;

        // Controls
        private readonly TextBox _txtTargetLocation;
        private readonly Panel _mainScrollPanel;
        private readonly ProgressBar _progressBar;
        private readonly Label _lblStatus;
        private readonly TextBox _txtLog;
        private readonly Panel _logPanel;
        private readonly Button _btnToggleLog;

        // Breathing pulse animation
        private readonly System.Windows.Forms.Timer _pulseTimer;
        private float _pulsePhase = 0f;

        // Network
        private readonly HttpClient _http = new();
        private CancellationTokenSource _cts;

        public LauncherSelectorForm()
        {
            Text = "Blizzard Launcher Archive — Historical Game & Battle.net Launcher Suite";
            Size = new Size(1160, 840);
            MinimumSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(14, 16, 22);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            // Set icon
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app_icon.ico");
                if (stream != null) Icon = new Icon(stream);
            }
            catch { }

            // Load images
            _logoImg = LoadEmbeddedImage("app_logo.png");
            _bnetBannerImg = LoadEmbeddedImage("bnet_banner.jpg");
            _d3BannerImg = LoadEmbeddedImage("d3_banner.jpg");
            _wowBannerImg = LoadEmbeddedImage("wow_banner.jpg");
            _sc2BannerImg = LoadEmbeddedImage("sc2_banner.jpg");
            _classicBannerImg = LoadEmbeddedImage("classic_banner.jpg");

            // Initial selections
            _selectedBnet = LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.BattleNet);
            _selectedD3 = LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo3);
            _selectedWow = LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.WoW);
            _selectedSc2 = LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.StarCraft);
            _selectedClassic = LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Classic);

            // Animation timer
            _pulseTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _pulseTimer.Tick += (s, e) =>
            {
                _pulsePhase += 0.08f;
                if (_pulsePhase > MathF.PI * 2) _pulsePhase -= MathF.PI * 2;
                Invalidate(true);
            };
            _pulseTimer.Start();

            // 1. Top Toolbar
            var topToolbar = CreateTopToolbar();

            // 2. Location & Search Bar
            var locationBar = CreateLocationAndSearchBar(out _txtTargetLocation);

            // 3. Bottom Status & Log Panel
            var bottomBar = CreateBottomBar(out _lblStatus, out _progressBar, out _btnToggleLog, out _logPanel, out _txtLog);

            // 4. Main Scrollable Canvas with 5 Game Family Tiles
            _mainScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(14, 16, 22),
                Padding = new Padding(24, 12, 24, 24)
            };

            RebuildFamilyTiles();

            Controls.AddRange(new Control[] { _mainScrollPanel, _logPanel, bottomBar, locationBar, topToolbar });

            // Check updates in background
            Task.Run(async () =>
            {
                var up = await UpdateChecker.CheckForUpdatesAsync(_http);
                if (up.UpdateAvailable)
                {
                    Invoke(() =>
                    {
                        _lblStatus.Text = $"⭐ Update Available: v{up.LatestVersion} (Click Check Updates on top bar to view).";
                    });
                }
            });
        }

        private Panel CreateTopToolbar()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(20, 24, 34),
                Padding = new Padding(20, 10, 20, 10)
            };
            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(0, 160, 240), 2);
                e.Graphics.DrawLine(p, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            };

            var picLogo = new PictureBox
            {
                Image = _logoImg,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(48, 48),
                Location = new Point(16, 10)
            };

            var lblTitle = new Label
            {
                Text = "BLIZZARD LAUNCHER ARCHIVE",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(72, 12),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "Standalone Game Launcher & Battle.net Client Time Machine — by Elite Level Software",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(0, 190, 255),
                Location = new Point(74, 38),
                AutoSize = true
            };

            var btnDevKit = CreateHeaderButton("🚀 LAUNCHER DEV KIT", 520, (s, e) =>
            {
                using var dlg = new LauncherIntegrationDialog(_selectedD3 ?? _selectedBnet);
                dlg.ShowDialog(this);
            });

            var btnBackup = CreateHeaderButton("📦 BULK ARCHIVAL", 690, (s, e) =>
            {
                using var dlg = new BackupDialog(_txtTargetLocation.Text, _http);
                dlg.ShowDialog(this);
            });

            var btnRetro = CreateHeaderButton("🎨 RETRO LAUNCHER", 840, (s, e) =>
            {
                using var dlg = new RetroLauncherEmulator(_txtTargetLocation.Text, _selectedD3);
                dlg.ShowDialog(this);
            });

            var btnUpdate = CreateHeaderButton("🔄 UPDATES", 995, async (s, e) =>
            {
                var info = await UpdateChecker.CheckForUpdatesAsync(_http);
                using var dlg = new UpdateDialog(info);
                dlg.ShowDialog(this);
            });

            pnl.Controls.AddRange(new Control[] { picLogo, lblTitle, lblSub, btnDevKit, btnBackup, btnRetro, btnUpdate });
            return pnl;
        }

        private Button CreateHeaderButton(string text, int left, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(left, 16),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(32, 40, 56),
                ForeColor = Color.FromArgb(235, 240, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(50, 70, 95);
            btn.Click += onClick;
            return btn;
        }

        private Panel CreateLocationAndSearchBar(out TextBox txtLocation)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(16, 20, 28),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblTarget = new Label
            {
                Text = "STORAGE LOCATION:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 195, 75),
                Location = new Point(20, 22),
                AutoSize = true
            };

            txtLocation = new TextBox
            {
                Text = @"C:\Games\Blizzard_Launcher_Archive",
                Location = new Point(155, 18),
                Size = new Size(420, 26),
                BackColor = Color.FromArgb(24, 28, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(585, 17),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(38, 46, 62),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(60, 72, 95);
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog { SelectedPath = _txtTargetLocation.Text };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtTargetLocation.Text = fbd.SelectedPath;
                }
            };

            var chkFast = new CheckBox
            {
                Text = "⚡ Fast Binaries Only",
                Location = new Point(680, 20),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            chkFast.CheckedChanged += (s, e) => _chkFastBinariesOnly = chkFast.Checked;

            var lblSearch = new Label
            {
                Text = "🔍 SEARCH:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.DarkGray,
                Location = new Point(840, 22),
                AutoSize = true
            };

            var txtSearch = new TextBox
            {
                Location = new Point(915, 18),
                Size = new Size(200, 26),
                BackColor = Color.FromArgb(24, 28, 40),
                ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) =>
            {
                _searchFilter = txtSearch.Text.Trim();
                RebuildFamilyTiles();
            };

            pnl.Controls.AddRange(new Control[] { lblTarget, txtLocation, btnBrowse, chkFast, lblSearch, txtSearch });
            return pnl;
        }

        private Panel CreateBottomBar(out Label lblStatus, out ProgressBar pb, out Button btnLogToggle, out Panel logPanel, out TextBox txtLog)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Color.FromArgb(18, 22, 30),
                Padding = new Padding(20, 10, 20, 10)
            };

            lblStatus = new Label
            {
                Text = "Ready — Select any launcher from the game tiles above to archive.",
                Location = new Point(20, 16),
                AutoSize = true,
                ForeColor = Color.Gainsboro,
                Font = new Font("Segoe UI", 9f)
            };

            pb = new ProgressBar
            {
                Location = new Point(560, 16),
                Size = new Size(420, 22),
                Maximum = 100
            };

            btnLogToggle = new Button
            {
                Text = "Logs ▼",
                Location = new Point(995, 14),
                Size = new Size(90, 26),
                BackColor = Color.FromArgb(32, 38, 52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnLogToggle.FlatAppearance.BorderColor = Color.FromArgb(50, 65, 88);

            logPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Visible = false,
                BackColor = Color.FromArgb(12, 14, 18),
                Padding = new Padding(20, 8, 20, 8)
            };

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };
            logPanel.Controls.Add(txtLog);

            var toggleRef = btnLogToggle;
            var panelRef = logPanel;
            btnLogToggle.Click += (s, e) =>
            {
                _logExpanded = !_logExpanded;
                panelRef.Visible = _logExpanded;
                toggleRef.Text = _logExpanded ? "Logs ▲" : "Logs ▼";
            };

            pnl.Controls.AddRange(new Control[] { lblStatus, pb, btnLogToggle });
            return pnl;
        }

        private void RebuildFamilyTiles()
        {
            _mainScrollPanel.SuspendLayout();
            _mainScrollPanel.Controls.Clear();

            int curY = 12;

            // 1. Battle.net Desktop App Tile
            curY += AddGameFamilyTile(
                LauncherGameFamily.BattleNet,
                "BATTLE.NET DESKTOP APPLICATION",
                "The unified flagship platform from 2013 Open Beta to Battle.net 2.0",
                _bnetBannerImg,
                _bnetExpanded,
                () => { _bnetExpanded = !_bnetExpanded; RebuildFamilyTiles(); },
                _selectedBnet,
                item => { _selectedBnet = item; RebuildFamilyTiles(); },
                curY);

            // 2. Diablo III Standalone Launchers Tile
            curY += AddGameFamilyTile(
                LauncherGameFamily.Diablo3,
                "DIABLO III STANDALONE LAUNCHERS",
                "Classic 2011–2014 standalone game launchers with SetupWin MPQ skins",
                _d3BannerImg,
                _d3Expanded,
                () => { _d3Expanded = !_d3Expanded; RebuildFamilyTiles(); },
                _selectedD3,
                item => { _selectedD3 = item; RebuildFamilyTiles(); },
                curY);

            // 3. World of Warcraft Launchers Tile
            curY += AddGameFamilyTile(
                LauncherGameFamily.WoW,
                "WORLD OF WARCRAFT EXPANSION LAUNCHERS",
                "Standalone expansion launchers from Vanilla (2004) through Mists of Pandaria",
                _wowBannerImg,
                _wowExpanded,
                () => { _wowExpanded = !_wowExpanded; RebuildFamilyTiles(); },
                _selectedWow,
                item => { _selectedWow = item; RebuildFamilyTiles(); },
                curY);

            // 4. StarCraft Series Launchers Tile
            curY += AddGameFamilyTile(
                LauncherGameFamily.StarCraft,
                "STARCRAFT II & BROOD WAR LAUNCHERS",
                "Wings of Liberty, Heart of the Swarm, and classic Brood War launchers",
                _sc2BannerImg,
                _sc2Expanded,
                () => { _sc2Expanded = !_sc2Expanded; RebuildFamilyTiles(); },
                _selectedSc2,
                item => { _selectedSc2 = item; RebuildFamilyTiles(); },
                curY);

            // 5. Classic Strategy Launchers Tile
            curY += AddGameFamilyTile(
                LauncherGameFamily.Classic,
                "CLASSIC STRATEGY & GATEWAY LAUNCHERS",
                "Warcraft III, Diablo II Lord of Destruction & Hearthstone Beta",
                _classicBannerImg,
                _classicExpanded,
                () => { _classicExpanded = !_classicExpanded; RebuildFamilyTiles(); },
                _selectedClassic,
                item => { _selectedClassic = item; RebuildFamilyTiles(); },
                curY);

            _mainScrollPanel.ResumeLayout();
        }

        private int AddGameFamilyTile(
            LauncherGameFamily family,
            string title,
            string subtitle,
            Image bannerImg,
            bool isExpanded,
            Action toggleExpand,
            LauncherItem selectedItem,
            Action<LauncherItem> onSelect,
            int top)
        {
            var items = LauncherCatalog.AllLaunchers.Where(x => x.Family == family).ToList();
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                items = items.Where(x =>
                    x.Title.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.Version.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.BuildNumber.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.Summary.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int tileWidth = _mainScrollPanel.Width - 64;
            if (tileWidth < 900) tileWidth = 900;

            var card = new BufferedPanel
            {
                Location = new Point(20, top),
                Width = tileWidth,
                BackColor = Color.FromArgb(20, 24, 32)
            };

            // Banner Panel
            var pnlBanner = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = BANNER_HEIGHT,
                BackgroundImage = bannerImg,
                BackgroundImageLayout = ImageLayout.Stretch,
                Cursor = Cursors.Hand
            };
            pnlBanner.Click += (s, e) => toggleExpand();

            // Right-Side Action Card
            var pnlAction = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = Color.FromArgb(220, 16, 20, 28),
                Padding = new Padding(16, 12, 16, 12)
            };

            var lblCurSel = new Label
            {
                Text = $"SELECTED: {(selectedItem != null ? selectedItem.Title : "None")}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 195, 65),
                Location = new Point(12, 10),
                Size = new Size(296, 36)
            };

            var lblDetails = new Label
            {
                Text = selectedItem != null ? $"v{selectedItem.Version} (Build {selectedItem.BuildNumber}) | {selectedItem.ApproxSizeBytes / 1024.0 / 1024.0:F1} MB" : "",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.LightSkyBlue,
                Location = new Point(12, 46),
                AutoSize = true
            };

            var btnArchive = new Button
            {
                Text = "⬇️ ARCHIVE LAUNCHER",
                Location = new Point(12, 74),
                Size = new Size(296, 38),
                BackColor = Color.FromArgb(0, 130, 220),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnArchive.FlatAppearance.BorderSize = 0;
            btnArchive.Click += async (s, e) =>
            {
                if (selectedItem != null)
                {
                    await ExecuteArchiveItemAsync(selectedItem);
                }
            };

            var btnDossier = new Button
            {
                Text = "📖 Historical Dossier",
                Location = new Point(12, 118),
                Size = new Size(140, 28),
                BackColor = Color.FromArgb(36, 44, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand
            };
            btnDossier.Click += (s, e) =>
            {
                if (selectedItem != null)
                {
                    using var dlg = new LauncherDossierDialog(selectedItem);
                    dlg.ShowDialog(this);
                }
            };

            var btnRetroTest = new Button
            {
                Text = "▶ Retro Play",
                Location = new Point(160, 118),
                Size = new Size(148, 28),
                BackColor = Color.FromArgb(45, 38, 28),
                ForeColor = Color.FromArgb(245, 195, 65),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRetroTest.Click += (s, e) =>
            {
                using var dlg = new RetroLauncherEmulator(_txtTargetLocation.Text, selectedItem);
                dlg.ShowDialog(this);
            };

            pnlAction.Controls.AddRange(new Control[] { lblCurSel, lblDetails, btnArchive, btnDossier, btnRetroTest });
            pnlBanner.Controls.Add(pnlAction);

            card.Controls.Add(pnlBanner);

            // Drawer for Version Chips
            int drawerHeight = 0;
            if (isExpanded && items.Count > 0)
            {
                var pnlDrawer = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    BackColor = Color.FromArgb(16, 20, 26),
                    Padding = new Padding(16, 12, 16, 16)
                };

                foreach (var item in items)
                {
                    bool isSelected = selectedItem != null && selectedItem.Id == item.Id;
                    var chip = CreateVersionChip(item, isSelected, () => onSelect(item));
                    pnlDrawer.Controls.Add(chip);
                }

                card.Controls.Add(pnlDrawer);
                pnlDrawer.BringToFront();
                pnlBanner.SendToBack();

                drawerHeight = pnlDrawer.PreferredSize.Height;
            }

            int totalCardHeight = BANNER_HEIGHT + drawerHeight + 10;
            card.Height = totalCardHeight;

            _mainScrollPanel.Controls.Add(card);
            return totalCardHeight + 16;
        }

        private Panel CreateVersionChip(LauncherItem item, bool isSelected, Action onClick)
        {
            var chip = new BufferedPanel
            {
                Width = 240,
                Height = 64,
                Margin = new Padding(6, 6, 6, 6),
                BackColor = isSelected ? Color.FromArgb(32, 54, 82) : Color.FromArgb(24, 28, 38),
                Cursor = Cursors.Hand
            };

            chip.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Border
                using var pen = new Pen(isSelected ? Color.FromArgb(0, 180, 255) : Color.FromArgb(42, 50, 68), isSelected ? 2 : 1);
                g.DrawRectangle(pen, 0, 0, chip.Width - 1, chip.Height - 1);

                // Animated breathing green status dot
                float alpha = (MathF.Sin(_pulsePhase) + 1f) / 2f * 0.4f + 0.6f;
                int dotAlpha = (int)(alpha * 255);
                using var dotBrush = new SolidBrush(Color.FromArgb(dotAlpha, 40, 220, 90));
                g.FillEllipse(dotBrush, 12, 12, 10, 10);

                // Version text
                using var fontBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var fontSmall = new Font("Segoe UI", 8f);
                using var brushText = new SolidBrush(isSelected ? Color.White : Color.Gainsboro);
                using var brushDim = new SolidBrush(Color.DarkGray);

                g.DrawString($"v{item.Version} (Build {item.BuildNumber})", fontBold, brushText, 28, 8);
                g.DrawString($"{item.ReleaseDate} • {item.ApproxSizeBytes / 1024.0 / 1024.0:F1} MB", fontSmall, brushDim, 28, 28);
                g.DrawString($"[{item.Category}] {item.BinaryFileName}", fontSmall, Brushes.LightSkyBlue, 12, 45);
            };

            chip.Click += (s, e) => onClick();
            return chip;
        }

        private async Task ExecuteArchiveItemAsync(LauncherItem item)
        {
            if (_isArchiving) return;
            _isArchiving = true;
            _cts = new CancellationTokenSource();

            AppendLog($"\n>>> INITIATING ARCHIVAL: {item.DisplayTitle}");
            _lblStatus.Text = $"Archiving {item.Title}...";

            try
            {
                bool success = await LauncherDownloader.DownloadLauncherAsync(
                    _http,
                    item,
                    _txtTargetLocation.Text,
                    _chkFastBinariesOnly,
                    AppendLog,
                    (pct, status) =>
                    {
                        Invoke(() =>
                        {
                            _progressBar.Value = (int)Math.Max(0, Math.Min(100, pct));
                            _lblStatus.Text = status;
                        });
                    },
                    _cts.Token);

                if (success)
                {
                    _lblStatus.Text = $"Archived & Verified: {item.Title}";
                    MessageBox.Show(
                        $"Successfully archived and verified {item.DisplayTitle}!\r\n\r\nSaved to:\r\n{Path.Combine(_txtTargetLocation.Text, $"{item.Family}_{item.Category}_{item.Version}_{item.BuildNumber}")}",
                        "Archive Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Failed to archive launcher: {ex.Message}");
                _lblStatus.Text = $"Archival Error: {ex.Message}";
                MessageBox.Show($"Error archiving {item.Title}: {ex.Message}", "Archival Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isArchiving = false;
            }
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendLog(msg));
                return;
            }
            _txtLog.AppendText(msg + "\r\n");
        }

        private static Image LoadEmbeddedImage(string resourceName)
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return Image.FromStream(stream);
                }
            }
            catch { }

            // Fallback bitmap if embedded resource not found
            var bmp = new Bitmap(100, 100);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.FromArgb(24, 28, 36));
            return bmp;
        }
    }
}
