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

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int BANNER_HEIGHT = 196;

        // Embedded Artwork Assets
        private readonly Image _logoImg;
        private readonly Image _bnetBannerImg;
        private readonly Image _d3BannerImg;
        private readonly Image _wowBannerImg;
        private readonly Image _sc2BannerImg;
        private readonly Image _sc1BannerImg;
        private readonly Image _wc3BannerImg;
        private readonly Image _d2BannerImg;
        private readonly Image _hearthstoneBannerImg;

        // Family Selected Items
        private LauncherItem _selectedBnet;
        private LauncherItem _selectedD3;
        private LauncherItem _selectedWow;
        private LauncherItem _selectedSc2;
        private LauncherItem _selectedSc1;
        private LauncherItem _selectedWc3;
        private LauncherItem _selectedD2;
        private LauncherItem _selectedHearthstone;

        // Expansion States
        private bool _bnetExpanded = false;
        private bool _d3Expanded = false;
        private bool _wowExpanded = false;
        private bool _sc2Expanded = false;
        private bool _sc1Expanded = false;
        private bool _wc3Expanded = false;
        private bool _d2Expanded = false;
        private bool _hearthstoneExpanded = false;
        private bool _logExpanded = false;
        private bool _isArchiving = false;
        private bool _chkFastBinariesOnly = false;
        private bool _userEditedTargetDir = false;
        private string _searchFilter = string.Empty;

        private string _statusText = "Ready — Choose Target Location above and click Archive Launcher on any game edition.";
        private double _progressPercent = 100.0;

        // Top Toolbar & Location Bar
        private readonly Panel _topToolbar;
        private readonly Panel _locationBar;
        private readonly TextBox _txtTargetLocation;
        private readonly TextBox _txtSearch;
        private readonly Panel _mainScrollPanel;

        // Card Wrappers and Drawers
        private readonly List<FamilyTileContext> _tiles = new();

        // Bottom Controls
        private readonly Panel _bottomPanel;
        private readonly TextBox _txtLog;
        private CancellationTokenSource _cts;

        // Animation & Updates
        private readonly System.Windows.Forms.Timer _pulseTimer;
        private float _pulsePhase = 0f;
        private UpdateInfo _availableUpdate;
        private bool _hoverUpdateBtn = false;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

        private sealed class FamilyTileContext
        {
            public LauncherGameFamily Family;
            public string BadgeTitle;
            public Color AccentColor;
            public Color RightBgColor;
            public Image BannerImage;
            public Func<LauncherItem> GetSelected;
            public Action<LauncherItem> SetSelected;
            public Func<bool> GetExpanded;
            public Action<bool> SetExpanded;

            public BufferedPanel CardWrapper;
            public BufferedPanel BannerPanel;
            public BufferedPanel DrawerPanel;
            public BufferedPanel ChipsContainer;
            public List<Panel> VersionChips = new();

            public string ActiveTab = "ALL";
            public List<Rectangle> TabRects = new();
            public List<string> TabNames = new();
        }

        public LauncherSelectorForm()
        {
            // Initial item selections (default to newest/milestone in each game family)
            _selectedBnet = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.BattleNet && x.Id == "bnet-latest-2.53.8")
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.BattleNet);
            _selectedD3 = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.Diablo3 && x.Id == "d3-retail-1.0.8a")
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo3);
            _selectedWow = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.WoW && x.Id == "wow-mop-5.0.4")
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.WoW);
            _selectedSc2 = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.StarCraft2 && x.Id == "sc2-hots-2.0.4")
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.StarCraft2);
            _selectedSc1 = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.StarCraftBroodWar)
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.StarCraftBroodWar);
            _selectedWc3 = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.Warcraft3 && x.Id == "wc3-tft-1.26a")
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Warcraft3);
            _selectedD2 = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.Diablo2)
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo2);
            _selectedHearthstone = LauncherCatalog.AllLaunchers.FirstOrDefault(x => x.Family == LauncherGameFamily.Hearthstone)
                ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Hearthstone);

            _logoImg = LoadEmbeddedImage("app_logo.png");
            _bnetBannerImg = LoadEmbeddedImage("bnet_banner.jpg");
            _d3BannerImg = LoadEmbeddedImage("d3_banner.jpg");
            _wowBannerImg = LoadEmbeddedImage("wow_banner.jpg");
            _sc2BannerImg = LoadEmbeddedImage("sc2_banner.jpg");
            _sc1BannerImg = LoadEmbeddedImage("sc1_banner.jpg");
            _wc3BannerImg = LoadEmbeddedImage("wc3_banner.jpg");
            _d2BannerImg = LoadEmbeddedImage("d2_banner.jpg");
            _hearthstoneBannerImg = LoadEmbeddedImage("hearthstone_banner.jpg");

            var appIcon = LoadEmbeddedIcon("app_icon.ico");
            if (appIcon != null)
            {
                Icon = appIcon;
            }

            Text = "Blizzard Launcher Archive — Historical Game & Battle.net Launcher Suite";
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            Size = new Size(980, 720);
            MinimumSize = new Size(980, 622);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(12, 15, 23);
            ForeColor = Color.FromArgb(237, 241, 247);
            Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);
            DoubleBuffered = true;

            // Breathing pulse animation timer (40ms ~ 25 FPS)
            _pulseTimer = new System.Windows.Forms.Timer { Interval = 40 };
            _pulseTimer.Tick += (_, _) =>
            {
                _pulsePhase = (_pulsePhase + 0.08f) % ((float)Math.PI * 2);
                foreach (var ctx in _tiles)
                {
                    ctx.BannerPanel?.Invalidate();
                    if (ctx.GetExpanded())
                    {
                        foreach (var c in ctx.VersionChips) c.Invalidate();
                    }
                }
            };
            _pulseTimer.Start();

            // Background update check
            Task.Run(CheckForAppUpdateAsync);

            // ================= 1. SEAMLESS TOP TOOLBAR (74px) =================
            _topToolbar = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(14, 19, 30)
            };

            int hoverWinBtn = -1; // 0=min, 1=close
            bool hoverBackup = false;
            bool hoverDevKit = false;
            bool hoverRetro = false;
            bool hoverNative = false;

            var updateBtnRect = new Rectangle(280, 22, 110, 30);
            var backupBtnRect = new Rectangle(400, 22, 114, 30);
            var devKitBtnRect = new Rectangle(522, 22, 114, 30);
            var retroBtnRect = new Rectangle(644, 22, 114, 30);
            var nativeBtnRect = new Rectangle(766, 22, 116, 30);
            var minRect = new Rectangle(890, 22, 32, 30);
            var closeRect = new Rectangle(926, 22, 32, 30);

            _topToolbar.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);
                int w = _topToolbar.Width;
                int h = _topToolbar.Height;

                using (var bgBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, Math.Max(1, w), Math.Max(1, h)),
                    Color.FromArgb(22, 29, 44),
                    Color.FromArgb(13, 17, 26),
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(bgBrush, 0, 0, w, h);
                }

                // Soft cyan glow behind Battle.net / Blizzard crest
                using (var glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(14, 6, 62, 62);
                    using var pgb = new PathGradientBrush(glowPath)
                    {
                        CenterColor = Color.FromArgb(75, 0, 180, 255),
                        SurroundColors = new[] { Color.FromArgb(0, 0, 180, 255) }
                    };
                    e.Graphics.FillPath(pgb, glowPath);
                }

                if (_logoImg != null)
                {
                    e.Graphics.DrawImage(_logoImg, new Rectangle(18, 10, 54, 54));
                }

                using (var titleFont = new Font("Georgia", 13f, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(Color.FromArgb(245, 235, 210)))
                {
                    e.Graphics.DrawString("BLIZZARD LAUNCHER ARCHIVE", titleFont, titleBrush, 80, 16);
                }

                using (var subFont = new Font("Segoe UI", 8.25f, FontStyle.Regular))
                using (var subBrush = new SolidBrush(Color.FromArgb(142, 153, 176)))
                {
                    e.Graphics.DrawString("Historical Blizzard Game & Battle.net Client Time Machine", subFont, subBrush, 82, 40);
                }

                // Update Available Pill
                if (_availableUpdate?.UpdateAvailable == true)
                {
                    using var uPath = CreateRoundRect(updateBtnRect, 5);
                    using var uFill = new SolidBrush(_hoverUpdateBtn ? Color.FromArgb(20, 83, 45) : Color.FromArgb(6, 44, 28));
                    using var uBorder = new Pen(_hoverUpdateBtn ? Color.FromArgb(52, 211, 153) : Color.FromArgb(16, 185, 129), 1.4f);
                    e.Graphics.FillPath(uFill, uPath);
                    e.Graphics.DrawPath(uBorder, uPath);

                    using var dotBrush = new SolidBrush(Color.FromArgb(52, 211, 153));
                    e.Graphics.FillEllipse(dotBrush, updateBtnRect.X + 10, updateBtnRect.Y + 11, 8, 8);

                    using var uFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                    using var uBrush = new SolidBrush(_hoverUpdateBtn ? Color.White : Color.FromArgb(209, 250, 229));
                    var textRect = new Rectangle(updateBtnRect.X + 22, updateBtnRect.Y, updateBtnRect.Width - 26, updateBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString($"UPDATE v{_availableUpdate.LatestVersion}", uFont, uBrush, textRect, sf);
                }

                // Backup All Launchers Button
                using (var bkPath = CreateRoundRect(backupBtnRect, 5))
                using (var bkFill = new SolidBrush(hoverBackup ? Color.FromArgb(32, 44, 66) : Color.FromArgb(18, 24, 38)))
                using (var bkBorder = new Pen(hoverBackup ? Color.FromArgb(245, 158, 11) : Color.FromArgb(60, 80, 115), hoverBackup ? 1.4f : 1f))
                {
                    e.Graphics.FillPath(bkFill, bkPath);
                    e.Graphics.DrawPath(bkBorder, bkPath);
                }

                // Crisp vector archive box icon
                int iconX = backupBtnRect.X + 12;
                int iconY = backupBtnRect.Y + 8;
                using (var boxPen = new Pen(hoverBackup ? Color.FromArgb(245, 158, 11) : Color.FromArgb(180, 195, 220), 1.4f))
                {
                    e.Graphics.DrawRectangle(boxPen, iconX, iconY + 3, 14, 9);
                    e.Graphics.DrawLine(boxPen, iconX - 1, iconY + 3, iconX + 15, iconY + 3);
                    e.Graphics.DrawLine(boxPen, iconX + 4, iconY + 7, iconX + 10, iconY + 7);
                }

                using (var bkFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var bkBrush = new SolidBrush(hoverBackup ? Color.FromArgb(254, 243, 199) : Color.FromArgb(226, 232, 240)))
                {
                    var textRect = new Rectangle(backupBtnRect.X + 28, backupBtnRect.Y, backupBtnRect.Width - 32, backupBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("COLD STORAGE", bkFont, bkBrush, textRect, sf);
                }

                // Launcher Dev Kit Button
                using (var devPath = CreateRoundRect(devKitBtnRect, 5))
                using (var devFill = new SolidBrush(hoverDevKit ? Color.FromArgb(24, 48, 86) : Color.FromArgb(16, 22, 35)))
                using (var devBorder = new Pen(hoverDevKit ? Color.FromArgb(59, 130, 246) : Color.FromArgb(45, 65, 95), hoverDevKit ? 1.4f : 1f))
                {
                    e.Graphics.FillPath(devFill, devPath);
                    e.Graphics.DrawPath(devBorder, devPath);
                }

                // Crisp vector code brackets icon
                int devX = devKitBtnRect.X + 10;
                int devY = devKitBtnRect.Y + 8;
                using (var codePen = new Pen(hoverDevKit ? Color.FromArgb(59, 130, 246) : Color.FromArgb(148, 163, 184), 1.4f))
                {
                    e.Graphics.DrawLines(codePen, new[] { new Point(devX + 4, devY + 3), new Point(devX + 1, devY + 7), new Point(devX + 4, devY + 11) });
                    e.Graphics.DrawLines(codePen, new[] { new Point(devX + 10, devY + 3), new Point(devX + 13, devY + 7), new Point(devX + 10, devY + 11) });
                    e.Graphics.DrawLine(codePen, devX + 8, devY + 2, devX + 6, devY + 12);
                }

                using (var devFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var devBrush = new SolidBrush(hoverDevKit ? Color.White : Color.FromArgb(203, 213, 225)))
                {
                    var textRect = new Rectangle(devKitBtnRect.X + 26, devKitBtnRect.Y, devKitBtnRect.Width - 28, devKitBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("LAUNCHER DEV KIT", devFont, devBrush, textRect, sf);
                }

                // Retro Emulator Player Button
                using (var retPath = CreateRoundRect(retroBtnRect, 5))
                using (var retFill = new SolidBrush(hoverRetro ? Color.FromArgb(46, 36, 20) : Color.FromArgb(28, 22, 14)))
                using (var retBorder = new Pen(hoverRetro ? Color.FromArgb(245, 158, 11) : Color.FromArgb(85, 65, 35), hoverRetro ? 1.4f : 1f))
                {
                    e.Graphics.FillPath(retFill, retPath);
                    e.Graphics.DrawPath(retBorder, retPath);
                }

                // Crisp vector play triangle icon
                int rx = retroBtnRect.X + 12;
                int ry = retroBtnRect.Y + 9;
                using (var playBrush = new SolidBrush(hoverRetro ? Color.FromArgb(245, 158, 11) : Color.FromArgb(215, 165, 55)))
                {
                    e.Graphics.FillPolygon(playBrush, new[] { new Point(rx + 2, ry + 2), new Point(rx + 11, ry + 7), new Point(rx + 2, ry + 12) });
                }

                using (var retFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var retBrush = new SolidBrush(hoverRetro ? Color.FromArgb(254, 240, 138) : Color.FromArgb(240, 215, 145)))
                {
                    var textRect = new Rectangle(retroBtnRect.X + 26, retroBtnRect.Y, retroBtnRect.Width - 28, retroBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("RETRO EMULATOR", retFont, retBrush, textRect, sf);
                }

                // Native Launcher Player Button
                using (var natPath = CreateRoundRect(nativeBtnRect, 5))
                using (var natFill = new SolidBrush(hoverNative ? Color.FromArgb(48, 22, 24) : Color.FromArgb(28, 14, 16)))
                using (var natBorder = new Pen(hoverNative ? Color.FromArgb(239, 68, 68) : Color.FromArgb(120, 35, 40), hoverNative ? 1.4f : 1f))
                {
                    e.Graphics.FillPath(natFill, natPath);
                    e.Graphics.DrawPath(natBorder, natPath);
                }

                int nx = nativeBtnRect.X + 11;
                int ny = nativeBtnRect.Y + 8;
                using (var lb = new SolidBrush(hoverNative ? Color.FromArgb(239, 68, 68) : Color.FromArgb(220, 80, 80)))
                {
                    e.Graphics.FillPolygon(lb, new[] {
                        new Point(nx + 6, ny),
                        new Point(nx + 1, ny + 7),
                        new Point(nx + 5, ny + 7),
                        new Point(nx + 3, ny + 14),
                        new Point(nx + 9, ny + 5),
                        new Point(nx + 5, ny + 5)
                    });
                }

                using (var natFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var natBrush = new SolidBrush(hoverNative ? Color.FromArgb(254, 202, 202) : Color.FromArgb(248, 113, 113)))
                {
                    var textRect = new Rectangle(nativeBtnRect.X + 24, nativeBtnRect.Y, nativeBtnRect.Width - 26, nativeBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("NATIVE LAUNCH", natFont, natBrush, textRect, sf);
                }

                // Crisp Vector Window Buttons (Minimize & Close only)
                DrawMinimizeButton(e.Graphics, minRect, hoverWinBtn == 0);
                DrawCloseButton(e.Graphics, closeRect, hoverWinBtn == 1);

                using var framePen = new Pen(Color.FromArgb(38, 48, 68), 1);
                e.Graphics.DrawLine(framePen, 0, 0, w - 1, 0);
                e.Graphics.DrawLine(framePen, 0, 0, 0, h);
                e.Graphics.DrawLine(framePen, w - 1, 0, w - 1, h);
            };

            _topToolbar.MouseMove += (_, e) =>
            {
                int newHover = -1;
                if (minRect.Contains(e.Location)) newHover = 0;
                else if (closeRect.Contains(e.Location)) newHover = 1;

                bool newBackup = backupBtnRect.Contains(e.Location);
                bool newDevKit = devKitBtnRect.Contains(e.Location);
                bool newRetro = retroBtnRect.Contains(e.Location);
                bool newNative = nativeBtnRect.Contains(e.Location);
                bool newUpdate = _availableUpdate?.UpdateAvailable == true && updateBtnRect.Contains(e.Location);
                _topToolbar.Cursor = (newHover >= 0 || newBackup || newDevKit || newRetro || newNative || newUpdate) ? Cursors.Hand : Cursors.Default;

                if (newHover != hoverWinBtn || newBackup != hoverBackup || newDevKit != hoverDevKit || newRetro != hoverRetro || newNative != hoverNative || newUpdate != _hoverUpdateBtn)
                {
                    hoverWinBtn = newHover;
                    hoverBackup = newBackup;
                    hoverDevKit = newDevKit;
                    hoverRetro = newRetro;
                    hoverNative = newNative;
                    _hoverUpdateBtn = newUpdate;
                    _topToolbar.Invalidate();
                }
            };
            _topToolbar.MouseLeave += (_, _) =>
            {
                hoverWinBtn = -1;
                hoverBackup = false;
                hoverDevKit = false;
                hoverRetro = false;
                hoverNative = false;
                _hoverUpdateBtn = false;
                _topToolbar.Invalidate();
            };
            _topToolbar.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (closeRect.Contains(e.Location)) { Close(); return; }
                if (minRect.Contains(e.Location)) { WindowState = FormWindowState.Minimized; return; }
                if (_availableUpdate?.UpdateAvailable == true && updateBtnRect.Contains(e.Location))
                {
                    using var uDlg = new UpdateDialog(_availableUpdate);
                    uDlg.ShowDialog(this);
                    return;
                }
                if (backupBtnRect.Contains(e.Location))
                {
                    using var dlg = new BackupDialog(_txtTargetLocation?.Text ?? @"C:\Games\Blizzard_Launcher_Archive", _http);
                    dlg.ShowDialog(this);
                    return;
                }
                if (devKitBtnRect.Contains(e.Location))
                {
                    using var devDlg = new LauncherIntegrationDialog(_selectedD3 ?? _selectedBnet);
                    devDlg.ShowDialog(this);
                    return;
                }
                if (retroBtnRect.Contains(e.Location))
                {
                    using var rDlg = new RetroLauncherEmulator(_txtTargetLocation?.Text, _selectedD3);
                    rDlg.ShowDialog(this);
                    return;
                }
                if (nativeBtnRect.Contains(e.Location))
                {
                    string targetDir = _txtTargetLocation?.Text ?? @"C:\Games\Blizzard_Launcher_Archive";
                    var item = _selectedD3 ?? LauncherCatalog.AllLaunchers.First(x => x.Family == LauncherGameFamily.Diablo3);
                    string runDir = NativeLauncherRunner.ResolveGameDirectory(targetDir, item);
                    _ = NativeLauncherRunner.LaunchAsync(runDir, item, AppendLog);
                    return;
                }
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            };

            // ================= 2. SEAMLESS TARGET LOCATION BAR (56px) =================
            _locationBar = new BufferedPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(13, 17, 26)
            };

            var outerBarRect = new Rectangle(22, 0, 936, 44);
            var inputBoxRect = new Rectangle(164, 7, 320, 30);
            var browseBtnRect = new Rectangle(494, 7, 122, 30);
            var chkBinRect = new Rectangle(634, 13, 160, 20);
            var searchBoxRect = new Rectangle(804, 7, 142, 30);
            bool hoverBrowse = false;

            _txtTargetLocation = new TextBox
            {
                Location = new Point(inputBoxRect.X + 8, inputBoxRect.Y + 6),
                Width = inputBoxRect.Width - 16,
                BackColor = Color.FromArgb(7, 9, 15),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                TabStop = false,
                Text = @"C:\Games\Blizzard_Launcher_Archive"
            };
            _txtTargetLocation.TextChanged += (_, _) =>
            {
                if (_txtTargetLocation.Focused) _userEditedTargetDir = true;
            };
            _locationBar.Controls.Add(_txtTargetLocation);

            _txtSearch = new TextBox
            {
                Location = new Point(searchBoxRect.X + 8, searchBoxRect.Y + 6),
                Width = searchBoxRect.Width - 16,
                BackColor = Color.FromArgb(7, 9, 15),
                ForeColor = Color.FromArgb(145, 225, 175),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                TabStop = false
            };
            _txtSearch.TextChanged += (_, _) =>
            {
                _searchFilter = _txtSearch.Text.Trim();
                foreach (var ctx in _tiles)
                {
                    PopulateChipsForTile(ctx);
                }
            };
            _locationBar.Controls.Add(_txtSearch);

            _locationBar.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);

                using (var barPath = CreateRoundRect(outerBarRect, 6))
                using (var barFill = new SolidBrush(Color.FromArgb(10, 13, 20)))
                using (var barPen = new Pen(Color.FromArgb(37, 47, 68), 1f))
                {
                    e.Graphics.FillPath(barFill, barPath);
                    e.Graphics.DrawPath(barPen, barPath);
                }

                using (var capFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var capBrush = new SolidBrush(Color.FromArgb(154, 166, 191)))
                {
                    e.Graphics.DrawString("TARGET LOCATION:", capFont, capBrush, 36, 15);
                }

                using (var inPath = CreateRoundRect(inputBoxRect, 4))
                using (var inFill = new SolidBrush(Color.FromArgb(7, 9, 15)))
                using (var inPen = new Pen(_txtTargetLocation.Focused ? Color.FromArgb(56, 139, 253) : Color.FromArgb(40, 50, 72), 1f))
                {
                    e.Graphics.FillPath(inFill, inPath);
                    e.Graphics.DrawPath(inPen, inPath);
                }

                using (var brPath = CreateRoundRect(browseBtnRect, 4))
                using (var brFill = new SolidBrush(hoverBrowse ? Color.FromArgb(43, 55, 82) : Color.FromArgb(32, 41, 61)))
                using (var brPen = new Pen(Color.FromArgb(51, 65, 94), 1f))
                {
                    e.Graphics.FillPath(brFill, brPath);
                    e.Graphics.DrawPath(brPen, brPath);
                }
                using (var brFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
                using (var brTextBrush = new SolidBrush(Color.FromArgb(226, 232, 240)))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Browse Folder...", brFont, brTextBrush, browseBtnRect, sf);
                }

                using (var divPen = new Pen(Color.FromArgb(37, 47, 68), 1f))
                {
                    e.Graphics.DrawLine(divPen, 626, 9, 626, 35);
                }

                DrawModernCheckBox(e.Graphics, chkBinRect.Location, _chkFastBinariesOnly, "Fast Binaries Only");

                // Search box outline
                using (var sPath = CreateRoundRect(searchBoxRect, 4))
                using (var sFill = new SolidBrush(Color.FromArgb(7, 9, 15)))
                using (var sPen = new Pen(_txtSearch.Focused ? Color.FromArgb(56, 139, 253) : Color.FromArgb(40, 50, 72), 1f))
                {
                    e.Graphics.FillPath(sFill, sPath);
                    e.Graphics.DrawPath(sPen, sPath);
                }

                if (string.IsNullOrEmpty(_txtSearch.Text) && !_txtSearch.Focused)
                {
                    using var phFont = new Font("Segoe UI", 8.25f, FontStyle.Italic);
                    using var phBrush = new SolidBrush(Color.FromArgb(100, 115, 140));
                    e.Graphics.DrawString("🔍 Filter builds...", phFont, phBrush, searchBoxRect.X + 8, searchBoxRect.Y + 7);
                }

                using var framePen = new Pen(Color.FromArgb(38, 48, 68), 1);
                e.Graphics.DrawLine(framePen, 0, 0, 0, _locationBar.Height);
                e.Graphics.DrawLine(framePen, _locationBar.Width - 1, 0, _locationBar.Width - 1, _locationBar.Height);
            };

            _txtTargetLocation.GotFocus += (_, _) => _locationBar.Invalidate();
            _txtTargetLocation.LostFocus += (_, _) => _locationBar.Invalidate();
            _txtSearch.GotFocus += (_, _) => _locationBar.Invalidate();
            _txtSearch.LostFocus += (_, _) => _locationBar.Invalidate();

            _locationBar.MouseMove += (_, e) =>
            {
                bool hb = browseBtnRect.Contains(e.Location);
                bool hc = chkBinRect.Contains(e.Location);
                _locationBar.Cursor = (hb || hc) ? Cursors.Hand : Cursors.Default;
                if (hb != hoverBrowse)
                {
                    hoverBrowse = hb;
                    _locationBar.Invalidate();
                }
            };
            _locationBar.MouseLeave += (_, _) =>
            {
                hoverBrowse = false;
                _locationBar.Invalidate();
            };
            _locationBar.MouseClick += (_, e) =>
            {
                if (browseBtnRect.Contains(e.Location))
                {
                    using var fbd = new FolderBrowserDialog
                    {
                        SelectedPath = _txtTargetLocation.Text.Trim(),
                        Description = "Select Target Archive Folder for Blizzard Launcher Archive"
                    };
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                    {
                        _userEditedTargetDir = true;
                        _txtTargetLocation.Text = fbd.SelectedPath;
                    }
                }
                else if (chkBinRect.Contains(e.Location))
                {
                    _chkFastBinariesOnly = !_chkFastBinariesOnly;
                    _locationBar.Invalidate();
                }
            };

            // ================= 3. BOTTOM STATUS & GRADIENT PROGRESS BAR (60px) =================
            _bottomPanel = new BufferedPanel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(9, 12, 18)
            };

            var openFolderRect = new Rectangle(836, 13, 122, 34);
            var viewLogRect = new Rectangle(744, 13, 84, 34);
            var cancelRect = new Rectangle(662, 13, 74, 34);

            bool hoverOpenFolder = false;
            bool hoverViewLog = false;
            bool hoverCancel = false;

            _txtLog = new TextBox
            {
                Location = new Point(22, 58),
                Size = new Size(936, 128),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(7, 9, 15),
                ForeColor = Color.FromArgb(145, 225, 175),
                Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Text = "[Blizzard Launcher Archive] Ready. Preserving 23 milestone launchers across 5 game families.\r\n"
            };
            _bottomPanel.Controls.Add(_txtLog);

            _bottomPanel.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);

                using (var topLinePen = new Pen(Color.FromArgb(32, 40, 58), 1f))
                {
                    e.Graphics.DrawLine(topLinePen, 0, 0, _bottomPanel.Width, 0);
                }

                int barRight = _isArchiving ? 646 : 728;
                int barWidth = barRight - 22;

                using (var stFont = new Font("Segoe UI", 8.75f, FontStyle.Bold))
                using (var stBrush = new SolidBrush(Color.FromArgb(213, 222, 239)))
                using (var pctBrush = new SolidBrush(Color.FromArgb(88, 166, 255)))
                {
                    e.Graphics.DrawString(_statusText, stFont, stBrush, new RectangleF(22, 12, barWidth - 55, 18));
                    var sfRight = new StringFormat { Alignment = StringAlignment.Far };
                    e.Graphics.DrawString($"{_progressPercent:F0}%", stFont, pctBrush, new RectangleF(barRight - 55, 12, 55, 18), sfRight);
                }

                var trackRect = new Rectangle(22, 34, barWidth, 7);
                using (var trackPath = CreateRoundRect(trackRect, 3))
                using (var trackBrush = new SolidBrush(Color.FromArgb(23, 29, 43)))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                }

                int fillW = (int)Math.Round(barWidth * (Math.Clamp(_progressPercent, 0.0, 100.0) / 100.0));
                if (fillW > 4)
                {
                    var fillRect = new Rectangle(22, 34, fillW, 7);
                    using var fillPath = CreateRoundRect(fillRect, 3);
                    using var gradBrush = new LinearGradientBrush(
                        fillRect,
                        Color.FromArgb(0, 116, 224),
                        Color.FromArgb(0, 198, 255),
                        LinearGradientMode.Horizontal);
                    e.Graphics.FillPath(gradBrush, fillPath);
                }

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var btnFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);

                if (_isArchiving)
                {
                    using var cPath = CreateRoundRect(cancelRect, 5);
                    using var cFill = new SolidBrush(hoverCancel ? Color.FromArgb(145, 38, 46) : Color.FromArgb(110, 28, 35));
                    using var cPen = new Pen(Color.FromArgb(175, 65, 75), 1f);
                    e.Graphics.FillPath(cFill, cPath);
                    e.Graphics.DrawPath(cPen, cPath);
                    e.Graphics.DrawString("Cancel", btnFont, Brushes.White, cancelRect, sfCenter);
                }

                using (var lPath = CreateRoundRect(viewLogRect, 5))
                using (var lFill = new SolidBrush(hoverViewLog ? Color.FromArgb(34, 44, 66) : Color.FromArgb(22, 29, 44)))
                using (var lPen = new Pen(Color.FromArgb(42, 54, 78), 1f))
                using (var lText = new SolidBrush(Color.FromArgb(205, 215, 232)))
                {
                    e.Graphics.FillPath(lFill, lPath);
                    e.Graphics.DrawPath(lPen, lPath);
                    e.Graphics.DrawString(_logExpanded ? "Hide Log" : "View Log", btnFont, lText,
                        new Rectangle(viewLogRect.X, viewLogRect.Y, viewLogRect.Width - 14, viewLogRect.Height), sfCenter);
                    DrawChevron(e.Graphics, new Point(viewLogRect.Right - 14, viewLogRect.Y + 17), !_logExpanded, Color.FromArgb(205, 215, 232));
                }

                using (var oPath = CreateRoundRect(openFolderRect, 5))
                using (var oFill = new SolidBrush(hoverOpenFolder ? Color.FromArgb(38, 49, 73) : Color.FromArgb(28, 36, 54)))
                using (var oPen = new Pen(Color.FromArgb(46, 58, 84), 1f))
                using (var oText = new SolidBrush(Color.FromArgb(220, 228, 242)))
                {
                    e.Graphics.FillPath(oFill, oPath);
                    e.Graphics.DrawPath(oPen, oPath);
                    e.Graphics.DrawString("Open Folder →", btnFont, oText, openFolderRect, sfCenter);
                }

                using var framePen = new Pen(Color.FromArgb(38, 48, 68), 1);
                e.Graphics.DrawLine(framePen, 0, 0, 0, _bottomPanel.Height - 1);
                e.Graphics.DrawLine(framePen, _bottomPanel.Width - 1, 0, _bottomPanel.Width - 1, _bottomPanel.Height - 1);
                e.Graphics.DrawLine(framePen, 0, _bottomPanel.Height - 1, _bottomPanel.Width - 1, _bottomPanel.Height - 1);
            };

            _bottomPanel.MouseMove += (_, e) =>
            {
                bool ho = openFolderRect.Contains(e.Location);
                bool hl = viewLogRect.Contains(e.Location);
                bool hc = _isArchiving && cancelRect.Contains(e.Location);
                _bottomPanel.Cursor = (ho || hl || hc) ? Cursors.Hand : Cursors.Default;
                if (ho != hoverOpenFolder || hl != hoverViewLog || hc != hoverCancel)
                {
                    hoverOpenFolder = ho;
                    hoverViewLog = hl;
                    hoverCancel = hc;
                    _bottomPanel.Invalidate();
                }
            };
            _bottomPanel.MouseLeave += (_, _) =>
            {
                hoverOpenFolder = hoverViewLog = hoverCancel = false;
                _bottomPanel.Invalidate();
            };
            _bottomPanel.MouseClick += (_, e) =>
            {
                if (openFolderRect.Contains(e.Location))
                {
                    string dir = _txtTargetLocation.Text.Trim();
                    if (Directory.Exists(dir))
                    {
                        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show(this, $"Folder does not exist yet:\n{dir}", "Blizzard Launcher Archive",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else if (viewLogRect.Contains(e.Location))
                {
                    ToggleLogDrawer();
                }
                else if (_isArchiving && cancelRect.Contains(e.Location))
                {
                    _cts?.Cancel();
                }
            };

            // ================= 4. MAIN SCROLL CONTAINER =================
            _mainScrollPanel = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(12, 15, 23)
            };
            _mainScrollPanel.Paint += (_, e) =>
            {
                using var framePen = new Pen(Color.FromArgb(38, 48, 68), 1);
                e.Graphics.DrawLine(framePen, 0, 0, 0, _mainScrollPanel.Height);
                e.Graphics.DrawLine(framePen, _mainScrollPanel.Width - 1, 0, _mainScrollPanel.Width - 1, _mainScrollPanel.Height);
            };

            // Setup the 8 Game Family Contexts (By Game Name)
            SetupFamilyTiles();

            Controls.Add(_mainScrollPanel);
            Controls.Add(_locationBar);
            Controls.Add(_topToolbar);
            Controls.Add(_bottomPanel);
        }

        private void SetupFamilyTiles()
        {
            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.BattleNet,
                BadgeTitle = "BATTLE.NET • 8 BUILDS",
                AccentColor = Color.FromArgb(56, 139, 253),
                RightBgColor = Color.FromArgb(7, 12, 24),
                BannerImage = _bnetBannerImg,
                GetSelected = () => _selectedBnet,
                SetSelected = (item) => _selectedBnet = item,
                GetExpanded = () => _bnetExpanded,
                SetExpanded = (exp) => _bnetExpanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.Diablo3,
                BadgeTitle = "DIABLO III • 4 BUILDS",
                AccentColor = Color.FromArgb(239, 68, 68),
                RightBgColor = Color.FromArgb(18, 8, 9),
                BannerImage = _d3BannerImg,
                GetSelected = () => _selectedD3,
                SetSelected = (item) => _selectedD3 = item,
                GetExpanded = () => _d3Expanded,
                SetExpanded = (exp) => _d3Expanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.WoW,
                BadgeTitle = "WORLD OF WARCRAFT • 5 BUILDS",
                AccentColor = Color.FromArgb(34, 197, 94),
                RightBgColor = Color.FromArgb(12, 18, 12),
                BannerImage = _wowBannerImg,
                GetSelected = () => _selectedWow,
                SetSelected = (item) => _selectedWow = item,
                GetExpanded = () => _wowExpanded,
                SetExpanded = (exp) => _wowExpanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.StarCraft2,
                BadgeTitle = "STARCRAFT II • 2 BUILDS",
                AccentColor = Color.FromArgb(14, 165, 233),
                RightBgColor = Color.FromArgb(8, 16, 24),
                BannerImage = _sc2BannerImg,
                GetSelected = () => _selectedSc2,
                SetSelected = (item) => _selectedSc2 = item,
                GetExpanded = () => _sc2Expanded,
                SetExpanded = (exp) => _sc2Expanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.StarCraftBroodWar,
                BadgeTitle = "STARCRAFT: BROOD WAR • 1 BUILD",
                AccentColor = Color.FromArgb(16, 185, 129),
                RightBgColor = Color.FromArgb(8, 20, 16),
                BannerImage = _sc1BannerImg,
                GetSelected = () => _selectedSc1,
                SetSelected = (item) => _selectedSc1 = item,
                GetExpanded = () => _sc1Expanded,
                SetExpanded = (exp) => _sc1Expanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.Warcraft3,
                BadgeTitle = "WARCRAFT III • 2 BUILDS",
                AccentColor = Color.FromArgb(168, 85, 247),
                RightBgColor = Color.FromArgb(18, 10, 24),
                BannerImage = _wc3BannerImg,
                GetSelected = () => _selectedWc3,
                SetSelected = (item) => _selectedWc3 = item,
                GetExpanded = () => _wc3Expanded,
                SetExpanded = (exp) => _wc3Expanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.Diablo2,
                BadgeTitle = "DIABLO II • 1 BUILD",
                AccentColor = Color.FromArgb(234, 88, 12),
                RightBgColor = Color.FromArgb(20, 10, 6),
                BannerImage = _d2BannerImg,
                GetSelected = () => _selectedD2,
                SetSelected = (item) => _selectedD2 = item,
                GetExpanded = () => _d2Expanded,
                SetExpanded = (exp) => _d2Expanded = exp
            });

            _tiles.Add(new FamilyTileContext
            {
                Family = LauncherGameFamily.Hearthstone,
                BadgeTitle = "HEARTHSTONE • 1 BUILD",
                AccentColor = Color.FromArgb(245, 158, 11),
                RightBgColor = Color.FromArgb(20, 16, 6),
                BannerImage = _hearthstoneBannerImg,
                GetSelected = () => _selectedHearthstone,
                SetSelected = (item) => _selectedHearthstone = item,
                GetExpanded = () => _hearthstoneExpanded,
                SetExpanded = (exp) => _hearthstoneExpanded = exp
            });

            int startY = 4;
            foreach (var ctx in _tiles)
            {
                ctx.CardWrapper = new BufferedPanel
                {
                    Location = new Point(22, startY),
                    Size = new Size(936, BANNER_HEIGHT),
                    BackColor = Color.FromArgb(12, 15, 23)
                };

                ctx.BannerPanel = CreateHeroBannerPanel(ctx);
                ctx.DrawerPanel = BuildFamilyDrawer(ctx);

                ctx.CardWrapper.Controls.Add(ctx.BannerPanel);
                ctx.CardWrapper.Controls.Add(ctx.DrawerPanel);
                _mainScrollPanel.Controls.Add(ctx.CardWrapper);

                startY += BANNER_HEIGHT + 16;
            }

            ReflowTiles();
        }

        private BufferedPanel CreateHeroBannerPanel(FamilyTileContext ctx)
        {
            var banner = new BufferedPanel
            {
                Location = new Point(0, 0),
                Size = new Size(936, BANNER_HEIGHT),
                BackColor = Color.FromArgb(12, 15, 23)
            };

            var viewMoreRect = new Rectangle(16, BANNER_HEIGHT - 40, 160, 28);
            var dossierBtnRect = new Rectangle(184, BANNER_HEIGHT - 40, 110, 28);
            var retroBtnRect = new Rectangle(302, BANNER_HEIGHT - 40, 140, 28);
            var nativeBtnRect = new Rectangle(450, BANNER_HEIGHT - 40, 176, 28);
            var verBoxRect = new Rectangle(646, 26, 264, 48);
            var installBtnRect = new Rectangle(646, 84, 264, 42);

            bool hoverViewMore = false;
            bool hoverDossier = false;
            bool hoverRetro = false;
            bool hoverNative = false;
            bool hoverVerBox = false;
            bool hoverInstall = false;

            banner.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);

                var cardRect = new Rectangle(0, 0, banner.Width - 1, banner.Height - 1);
                using var cardPath = CreateRoundRect(cardRect, 8);

                var oldClip = e.Graphics.Clip;
                e.Graphics.SetClip(cardPath);

                Color rightBg = ctx.RightBgColor;
                using (var baseBrush = new SolidBrush(rightBg))
                {
                    e.Graphics.FillRectangle(baseBrush, 0, 0, banner.Width, banner.Height);
                }

                Image art = ctx.BannerImage;
                if (art != null)
                {
                    int artAreaWidth = 615;
                    float scale = Math.Max((float)artAreaWidth / art.Width, (float)banner.Height / art.Height);
                    int drawW = (int)Math.Round(art.Width * scale);
                    int drawH = (int)Math.Round(art.Height * scale);
                    int drawX = (artAreaWidth - drawW) / 2;
                    int drawY = (banner.Height - drawH) / 2;

                    e.Graphics.DrawImage(art, new Rectangle(drawX, drawY, drawW, drawH));

                    var fadeRect = new Rectangle(440, 0, 180, banner.Height);
                    using var fadeBrush = new LinearGradientBrush(
                        fadeRect,
                        Color.FromArgb(0, rightBg),
                        rightBg,
                        LinearGradientMode.Horizontal);
                    fadeBrush.WrapMode = WrapMode.TileFlipXY;
                    e.Graphics.FillRectangle(fadeBrush, fadeRect);
                }

                // Game Family Pill Badge in upper left
                int buildCount = LauncherCatalog.AllLaunchers.Count(x => x.Family == ctx.Family);
                using (var bFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                {
                    int badgeW = Math.Max(168, (int)Math.Ceiling(e.Graphics.MeasureString(ctx.BadgeTitle, bFont).Width) + 18);
                    var badgeRect = new Rectangle(16, 12, badgeW, 20);
                    using var bPath = CreateRoundRect(badgeRect, 3);
                    using var bFill = new SolidBrush(ctx.AccentColor);
                    e.Graphics.FillPath(bFill, bPath);
                    var sfBadge = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(ctx.BadgeTitle, bFont, Brushes.Black, badgeRect, sfBadge);
                }

                // View More button (toggles drawer)
                bool isExp = ctx.GetExpanded();
                string vmText = isExp
                    ? $"Hide {buildCount} Builds"
                    : $"View More (Choose from {buildCount} Builds)";

                using (var vmPath = CreateRoundRect(viewMoreRect, 4))
                using (var vmFill = new SolidBrush(hoverViewMore ? Color.FromArgb(230, 24, 34, 52) : Color.FromArgb(215, 12, 16, 26)))
                using (var vmPen = new Pen(hoverViewMore ? Color.FromArgb(88, 166, 255) : Color.FromArgb(65, 255, 255, 255), 1f))
                using (var vmFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var vmTextBrush = new SolidBrush(hoverViewMore ? Color.White : Color.FromArgb(220, 228, 242)))
                {
                    e.Graphics.FillPath(vmFill, vmPath);
                    e.Graphics.DrawPath(vmPen, vmPath);
                    DrawChevron(e.Graphics, new Point(viewMoreRect.X + 16, viewMoreRect.Y + 14), isExp, hoverViewMore ? Color.White : Color.FromArgb(220, 228, 242));
                    var sfVm = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(vmText, vmFont, vmTextBrush, new Rectangle(viewMoreRect.X + 14, viewMoreRect.Y, viewMoreRect.Width - 18, viewMoreRect.Height), sfVm);
                }

                // Historical Dossier Button
                using (var dPath = CreateRoundRect(dossierBtnRect, 4))
                using (var dFill = new SolidBrush(hoverDossier ? Color.FromArgb(230, 36, 48, 70) : Color.FromArgb(215, 14, 18, 28)))
                using (var dPen = new Pen(hoverDossier ? Color.FromArgb(245, 158, 11) : Color.FromArgb(60, 75, 105), 1f))
                using (var dFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var dTextBrush = new SolidBrush(hoverDossier ? Color.FromArgb(254, 240, 138) : Color.FromArgb(203, 213, 225)))
                {
                    e.Graphics.FillPath(dFill, dPath);
                    e.Graphics.DrawPath(dPen, dPath);

                    int sx = dossierBtnRect.X + 10, sy = dossierBtnRect.Y + 7;
                    using var sp = new Pen(hoverDossier ? Color.FromArgb(245, 158, 11) : Color.FromArgb(160, 175, 200), 1.2f);
                    e.Graphics.DrawRectangle(sp, sx, sy, 11, 13);
                    e.Graphics.DrawLine(sp, sx + 2, sy + 3, sx + 9, sy + 3);
                    e.Graphics.DrawLine(sp, sx + 2, sy + 6, sx + 9, sy + 6);
                    e.Graphics.DrawLine(sp, sx + 2, sy + 9, sx + 6, sy + 9);

                    var textRect = new Rectangle(dossierBtnRect.X + 22, dossierBtnRect.Y, dossierBtnRect.Width - 24, dossierBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Historical Dossier", dFont, dTextBrush, textRect, sf);
                }

                // Retro Player Button (available for all launchers, specialized for D3)
                using (var rPath = CreateRoundRect(retroBtnRect, 4))
                using (var rFill = new SolidBrush(hoverRetro ? Color.FromArgb(230, 48, 38, 22) : Color.FromArgb(215, 22, 18, 12)))
                using (var rPen = new Pen(hoverRetro ? Color.FromArgb(245, 158, 11) : Color.FromArgb(75, 60, 35), 1f))
                using (var rFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var rTextBrush = new SolidBrush(hoverRetro ? Color.FromArgb(254, 240, 138) : Color.FromArgb(225, 200, 135)))
                {
                    e.Graphics.FillPath(rFill, rPath);
                    e.Graphics.DrawPath(rPen, rPath);

                    int px = retroBtnRect.X + 10, py = retroBtnRect.Y + 8;
                    using var pb = new SolidBrush(hoverRetro ? Color.FromArgb(245, 158, 11) : Color.FromArgb(225, 200, 135));
                    e.Graphics.FillPolygon(pb, new[] { new Point(px, py), new Point(px + 9, py + 5), new Point(px, py + 10) });

                    var textRect = new Rectangle(retroBtnRect.X + 20, retroBtnRect.Y, retroBtnRect.Width - 22, retroBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Retro Play", rFont, rTextBrush, textRect, sf);
                }

                // Native Launcher Player Button (Authentic Qt Desktop Window)
                using (var nPath = CreateRoundRect(nativeBtnRect, 4))
                using (var nFill = new SolidBrush(hoverNative ? Color.FromArgb(230, 48, 22, 24) : Color.FromArgb(215, 26, 14, 16)))
                using (var nPen = new Pen(hoverNative ? Color.FromArgb(239, 68, 68) : Color.FromArgb(120, 35, 40), 1f))
                using (var nFont = new Font("Segoe UI", 8.25f, FontStyle.Bold))
                using (var nTextBrush = new SolidBrush(hoverNative ? Color.FromArgb(254, 202, 202) : Color.FromArgb(248, 113, 113)))
                {
                    e.Graphics.FillPath(nFill, nPath);
                    e.Graphics.DrawPath(nPen, nPath);

                    int nx = nativeBtnRect.X + 10, ny = nativeBtnRect.Y + 7;
                    using var lb = new SolidBrush(hoverNative ? Color.FromArgb(239, 68, 68) : Color.FromArgb(220, 80, 80));
                    e.Graphics.FillPolygon(lb, new[] {
                        new Point(nx + 6, ny),
                        new Point(nx + 1, ny + 7),
                        new Point(nx + 5, ny + 7),
                        new Point(nx + 3, ny + 14),
                        new Point(nx + 9, ny + 5),
                        new Point(nx + 5, ny + 5)
                    });

                    var textRect = new Rectangle(nativeBtnRect.X + 18, nativeBtnRect.Y, nativeBtnRect.Width - 20, nativeBtnRect.Height);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("⚡ Native Launch", nFont, nTextBrush, textRect, sf);
                }

                // Right Side: Version Selection Box
                using (var vbPath = CreateRoundRect(verBoxRect, 5))
                using (var vbFill = new SolidBrush(Color.FromArgb(12, 16, 25)))
                using (var vbPen = new Pen(hoverVerBox ? Color.FromArgb(79, 96, 130) : Color.FromArgb(46, 56, 77), 1f))
                {
                    e.Graphics.FillPath(vbFill, vbPath);
                    e.Graphics.DrawPath(vbPen, vbPath);
                }

                using (var capFont = new Font("Segoe UI", 7.25f, FontStyle.Bold))
                using (var capBrush = new SolidBrush(Color.FromArgb(139, 149, 171)))
                {
                    e.Graphics.DrawString("VERSION (DEFAULTS TO NEWEST)", capFont, capBrush, verBoxRect.X + 11, verBoxRect.Y + 7);
                }

                LauncherItem curItem = ctx.GetSelected();
                string verLabel = curItem != null ? $"{curItem.Version} (Build {curItem.BuildNumber})" : "Select...";

                using (var valFont = new Font("Segoe UI", 9.25f, FontStyle.Bold))
                {
                    e.Graphics.DrawString(verLabel, valFont, Brushes.White, verBoxRect.X + 11, verBoxRect.Y + 23);
                }

                // Live status pill inside Version box
                DrawStatusPill(e.Graphics, new Point(verBoxRect.Right - 84, verBoxRect.Y + 16), CdnAvailability.Ok);
                DrawChevron(e.Graphics, new Point(verBoxRect.Right - 16, verBoxRect.Y + 24), isExp, Color.FromArgb(148, 163, 184));

                // Archive Launcher Button
                var glowRect = new Rectangle(installBtnRect.X, installBtnRect.Y + 3, installBtnRect.Width, installBtnRect.Height);
                using (var gPath = CreateRoundRect(glowRect, 6))
                using (var gFill = new SolidBrush(Color.FromArgb(65, 0, 116, 224)))
                {
                    e.Graphics.FillPath(gFill, gPath);
                }

                Color btnColor = _isArchiving
                    ? Color.FromArgb(35, 65, 105)
                    : (hoverInstall ? Color.FromArgb(26, 136, 240) : Color.FromArgb(0, 116, 224));

                using (var iPath = CreateRoundRect(installBtnRect, 5))
                using (var iFill = new SolidBrush(btnColor))
                using (var iPen = new Pen(Color.FromArgb(60, 160, 255), 1f))
                using (var iFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
                {
                    e.Graphics.FillPath(iFill, iPath);
                    e.Graphics.DrawPath(iPen, iPath);
                    var sfInst = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Archive Launcher", iFont, Brushes.White, installBtnRect, sfInst);
                }

                // Subtitle note
                using (var noteFont = new Font("Segoe UI", 8f, FontStyle.Regular))
                using (var noteBrush = new SolidBrush(Color.FromArgb(147, 159, 184)))
                {
                    var sfNote = new StringFormat { Alignment = StringAlignment.Center };
                    e.Graphics.DrawString("Saves directly into Target Location above", noteFont, noteBrush,
                        new RectangleF(installBtnRect.X, installBtnRect.Bottom + 8, installBtnRect.Width, 18), sfNote);
                }

                e.Graphics.Clip = oldClip;

                using var borderPen = new Pen(Color.FromArgb(40, 52, 76), 1f);
                e.Graphics.DrawPath(borderPen, cardPath);
            };

            banner.MouseMove += (_, e) =>
            {
                bool hvm = viewMoreRect.Contains(e.Location);
                bool hdo = dossierBtnRect.Contains(e.Location);
                bool hret = retroBtnRect.Contains(e.Location);
                bool hnat = nativeBtnRect.Contains(e.Location);
                bool hvb = verBoxRect.Contains(e.Location);
                bool hin = !_isArchiving && installBtnRect.Contains(e.Location);
                banner.Cursor = (hvm || hdo || hret || hnat || hvb || hin) ? Cursors.Hand : Cursors.Default;
                if (hvm != hoverViewMore || hdo != hoverDossier || hret != hoverRetro || hnat != hoverNative || hvb != hoverVerBox || hin != hoverInstall)
                {
                    hoverViewMore = hvm;
                    hoverDossier = hdo;
                    hoverRetro = hret;
                    hoverNative = hnat;
                    hoverVerBox = hvb;
                    hoverInstall = hin;
                    banner.Invalidate();
                }
            };
            banner.MouseLeave += (_, _) =>
            {
                hoverViewMore = hoverDossier = hoverRetro = hoverNative = hoverVerBox = hoverInstall = false;
                banner.Invalidate();
            };
            banner.MouseClick += async (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                if (dossierBtnRect.Contains(e.Location))
                {
                    var target = ctx.GetSelected();
                    if (target != null)
                    {
                        using var dlg = new LauncherDossierDialog(target);
                        dlg.ShowDialog(this);
                    }
                    return;
                }

                if (retroBtnRect.Contains(e.Location))
                {
                    var target = ctx.GetSelected();
                    using var dlg = new RetroLauncherEmulator(_txtTargetLocation?.Text, target);
                    dlg.ShowDialog(this);
                    return;
                }

                if (nativeBtnRect.Contains(e.Location))
                {
                    var target = ctx.GetSelected();
                    if (target != null)
                    {
                        string targetDir = _txtTargetLocation?.Text ?? @"C:\Games\Blizzard_Launcher_Archive";
                        string runDir = NativeLauncherRunner.ResolveGameDirectory(targetDir, target);
                        _ = NativeLauncherRunner.LaunchAsync(runDir, target, AppendLog);
                    }
                    return;
                }

                if (viewMoreRect.Contains(e.Location) || verBoxRect.Contains(e.Location))
                {
                    ToggleTileDrawer(ctx);
                }
                else if (!_isArchiving && installBtnRect.Contains(e.Location))
                {
                    var target = ctx.GetSelected();
                    if (target != null)
                    {
                        await StartArchivePipelineAsync(target);
                    }
                }
            };

            return banner;
        }

        private BufferedPanel BuildFamilyDrawer(FamilyTileContext ctx)
        {
            var drawer = new BufferedPanel
            {
                Location = new Point(0, BANNER_HEIGHT),
                Size = new Size(936, 300),
                BackColor = Color.FromArgb(11, 14, 22),
                Visible = false
            };
            ctx.DrawerPanel = drawer;

            ctx.ChipsContainer = new BufferedPanel
            {
                Location = new Point(0, 72),
                Size = new Size(936, 220),
                BackColor = Color.Transparent
            };
            drawer.Controls.Add(ctx.ChipsContainer);

            // Configure tabs for this family
            ctx.TabNames.Clear();
            ctx.TabNames.Add("ALL");

            var categories = LauncherCatalog.AllLaunchers
                .Where(x => x.Family == ctx.Family)
                .Select(x => x.Category.ToString().ToUpperInvariant())
                .Distinct()
                .ToList();

            foreach (var cat in categories)
            {
                if (!ctx.TabNames.Contains(cat)) ctx.TabNames.Add(cat);
            }

            int tabX = 20;
            ctx.TabRects.Clear();
            foreach (var tab in ctx.TabNames)
            {
                int tabW = 120;
                ctx.TabRects.Add(new Rectangle(tabX, 32, tabW, 30));
                tabX += tabW + 8;
            }

            drawer.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);
                using var pen = new Pen(Color.FromArgb(36, 45, 66), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, drawer.Width - 1, drawer.Height - 1);

                int count = LauncherCatalog.AllLaunchers.Count(x => x.Family == ctx.Family);
                using var hdrFont = new Font("Segoe UI", 8.25f, FontStyle.Bold);
                using var hdrBrush = new SolidBrush(Color.FromArgb(154, 166, 191));
                string gameName = (ctx.GetSelected()?.GameName ?? ctx.Family.ToString()).ToUpperInvariant();
                e.Graphics.DrawString($"CHOOSE {gameName} BUILD ({count} BUILDS ARCHIVED):", hdrFont, hdrBrush, 20, 11);

                // Draw segmented category tabs
                for (int i = 0; i < ctx.TabNames.Count; i++)
                {
                    string tabName = ctx.TabNames[i];
                    Rectangle rect = ctx.TabRects[i];
                    bool isSelected = ctx.ActiveTab.Equals(tabName, StringComparison.OrdinalIgnoreCase);
                    DrawDrawerTab(e.Graphics, rect, tabName, isSelected, ctx.AccentColor);
                }
            };

            drawer.MouseMove += (_, e) =>
            {
                bool hover = ctx.TabRects.Any(r => r.Contains(e.Location));
                drawer.Cursor = hover ? Cursors.Hand : Cursors.Default;
            };

            drawer.MouseClick += (_, e) =>
            {
                for (int i = 0; i < ctx.TabRects.Count; i++)
                {
                    if (ctx.TabRects[i].Contains(e.Location))
                    {
                        string targetTab = ctx.TabNames[i];
                        if (!ctx.ActiveTab.Equals(targetTab, StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.ActiveTab = targetTab;
                            PopulateChipsForTile(ctx);
                            drawer.Invalidate();
                        }
                        break;
                    }
                }
            };

            PopulateChipsForTile(ctx);
            return drawer;
        }

        private void PopulateChipsForTile(FamilyTileContext ctx)
        {
            if (ctx.ChipsContainer == null) return;

            ctx.ChipsContainer.Controls.Clear();
            ctx.VersionChips.Clear();

            var items = LauncherCatalog.AllLaunchers.Where(x => x.Family == ctx.Family).ToList();

            if (!ctx.ActiveTab.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(x => x.Category.ToString().Equals(ctx.ActiveTab, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                items = items.Where(x =>
                    x.Title.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.Version.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.BuildNumber.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                    x.Summary.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int rows = Math.Max(1, (int)Math.Ceiling(items.Count / 3.0));
            int colWidth = 292;
            int rowHeight = 46;
            int containerHeight = rows * (rowHeight + 6) + 8;
            ctx.ChipsContainer.Height = containerHeight;

            int drawerHeight = 80 + containerHeight;
            if (ctx.DrawerPanel != null) ctx.DrawerPanel.Height = drawerHeight;

            if (ctx.GetExpanded() && ctx.CardWrapper != null)
            {
                ctx.CardWrapper.Height = BANNER_HEIGHT + drawerHeight;
                ReflowTiles();
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int col = i % 3;
                int row = i / 3;
                string badge = item.Category.ToString().ToUpperInvariant();

                var chip = CreateVersionChip(
                    item,
                    badge,
                    new Point(20 + col * (colWidth + 9), row * (rowHeight + 6)),
                    new Size(colWidth, rowHeight),
                    isSelected: () => ReferenceEquals(item, ctx.GetSelected()),
                    onClick: () =>
                    {
                        ctx.SetSelected(item);
                        if (!_userEditedTargetDir)
                        {
                            _txtTargetLocation.Text = @"C:\Games\Blizzard_Launcher_Archive";
                        }
                        foreach (var c in ctx.VersionChips) c.Invalidate();
                        ctx.BannerPanel.Invalidate();
                    });

                ctx.VersionChips.Add(chip);
                ctx.ChipsContainer.Controls.Add(chip);
            }
        }

        private static void DrawDrawerTab(Graphics g, Rectangle rect, string text, bool isSelected, Color activeAccent)
        {
            using var path = CreateRoundRect(rect, 4);
            using var fill = new SolidBrush(isSelected ? Color.FromArgb(28, 38, 56) : Color.FromArgb(14, 18, 26));
            using var pen = new Pen(isSelected ? activeAccent : Color.FromArgb(36, 45, 66), isSelected ? 1.5f : 1f);

            g.FillPath(fill, path);
            g.DrawPath(pen, path);

            using (var dotBrush = new SolidBrush(isSelected ? activeAccent : Color.FromArgb(100, 116, 139)))
            {
                g.FillEllipse(dotBrush, rect.X + 12, rect.Y + (rect.Height - 6) / 2, 6, 6);
            }

            using var font = new Font("Segoe UI", 8.25f, isSelected ? FontStyle.Bold : FontStyle.Regular);
            using var brush = new SolidBrush(isSelected ? Color.White : Color.FromArgb(148, 163, 184));
            var textRect = new Rectangle(rect.X + 22, rect.Y, rect.Width - 26, rect.Height);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, brush, textRect, sf);
        }

        private Panel CreateVersionChip(
            LauncherItem item,
            string badgeText,
            Point location,
            Size size,
            Func<bool> isSelected,
            Action onClick)
        {
            var chip = new BufferedPanel
            {
                Location = location,
                Size = size,
                BackColor = Color.FromArgb(11, 14, 22),
                Cursor = Cursors.Hand
            };
            bool hover = false;

            chip.Paint += (_, e) =>
            {
                ConfigureGraphics(e.Graphics);
                bool sel = isSelected();
                var r = new Rectangle(0, 0, chip.Width - 1, chip.Height - 1);
                using var path = CreateRoundRect(r, 5);
                using var fill = new SolidBrush(sel
                    ? Color.FromArgb(18, 42, 74)
                    : (hover ? Color.FromArgb(23, 30, 46) : Color.FromArgb(19, 24, 37)));
                using var pen = new Pen(sel ? Color.FromArgb(0, 132, 255) : (hover ? Color.FromArgb(65, 81, 115) : Color.FromArgb(37, 46, 66)), 1f);

                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);

                using var topFont = new Font("Segoe UI", 8.75f, FontStyle.Bold);
                e.Graphics.DrawString($"{item.Version} (Build {item.BuildNumber})", topFont, Brushes.White, 10, 6);

                using var tagFont = new Font("Segoe UI", 7f, FontStyle.Bold);
                int tagW = Math.Max(46, (int)e.Graphics.MeasureString(badgeText, tagFont).Width + 10);
                var tagRect = new Rectangle(chip.Width - tagW - 8, 7, tagW, 16);
                using var tagPath = CreateRoundRect(tagRect, 3);
                using var tagFill = new SolidBrush(sel ? Color.FromArgb(0, 116, 224) : Color.FromArgb(35, 44, 63));
                using var tagTextBrush = new SolidBrush(sel ? Color.White : Color.FromArgb(154, 176, 212));
                e.Graphics.FillPath(tagFill, tagPath);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(badgeText, tagFont, tagTextBrush, tagRect, sf);

                // Status pill in chip placed cleanly to left of badge
                DrawStatusPill(e.Graphics, new Point(tagRect.Left - 66, 7), CdnAvailability.Ok);

                using var subFont = new Font("Segoe UI", 7.75f, FontStyle.Regular);
                using var subBrush = new SolidBrush(Color.FromArgb(135, 147, 171));
                e.Graphics.DrawString(item.Summary, subFont, subBrush, new RectangleF(10, 24, chip.Width - 18, 18));
            };

            var chipTip = new ToolTip();
            chipTip.SetToolTip(chip, $"{item.DisplayTitle}\n• Left-Click: Select & activate this launcher\n• Right-Click: Open Historical Dossier");

            chip.MouseEnter += (_, _) => { hover = true; chip.Invalidate(); };
            chip.MouseLeave += (_, _) => { hover = false; chip.Invalidate(); };
            chip.MouseClick += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    using var dlg = new LauncherDossierDialog(item);
                    dlg.ShowDialog(this);
                }
                else
                {
                    onClick();
                }
            };

            return chip;
        }

        private void DrawStatusPill(Graphics g, Point location, CdnAvailability status)
        {
            var pillRect = new Rectangle(location.X, location.Y, 60, 16);
            using var pillPath = CreateRoundRect(pillRect, 3);

            Color bg = Color.FromArgb(40, 46, 160, 67);
            Color border = Color.FromArgb(120, 63, 185, 80);
            Color text = Color.FromArgb(86, 211, 100);
            Color dot = Color.FromArgb(63, 185, 80);
            string label = "READY";

            using (var bFill = new SolidBrush(bg))
            using (var bPen = new Pen(border, 1f))
            {
                g.FillPath(bFill, pillPath);
                g.DrawPath(bPen, pillPath);
            }

            float breathe = 0.5f + 0.5f * (float)Math.Sin(_pulsePhase);
            int dotAlpha = (int)(110 + 145 * breathe);

            using (var dBrush = new SolidBrush(Color.FromArgb(dotAlpha, dot)))
            {
                g.FillEllipse(dBrush, location.X + 6, location.Y + 5, 6, 6);
            }

            if (breathe > 0.3f)
            {
                int glowAlpha = (int)(70 * breathe);
                using var gBrush = new SolidBrush(Color.FromArgb(glowAlpha, dot));
                g.FillEllipse(gBrush, location.X + 4, location.Y + 3, 10, 10);
            }

            using (var fFont = new Font("Segoe UI", 6.75f, FontStyle.Bold))
            using (var fBrush = new SolidBrush(text))
            {
                var textRect = new Rectangle(location.X + 13, location.Y, pillRect.Width - 14, pillRect.Height);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(label, fFont, fBrush, textRect, sf);
            }
        }

        private void ToggleTileDrawer(FamilyTileContext ctx)
        {
            bool nextState = !ctx.GetExpanded();
            ctx.SetExpanded(nextState);
            ctx.DrawerPanel.Visible = nextState;
            ctx.CardWrapper.Height = nextState ? (BANNER_HEIGHT + ctx.DrawerPanel.Height) : BANNER_HEIGHT;
            ctx.BannerPanel.Invalidate();
            ReflowTiles();
        }

        public void CaptureSnapshot(string outPath, int expandTileIndex = -1)
        {
            if (expandTileIndex >= 0 && expandTileIndex < _tiles.Count)
            {
                var targetTile = _tiles[expandTileIndex];
                if (!targetTile.GetExpanded()) ToggleTileDrawer(targetTile);
            }
            ReflowTiles();
            for (int i = 0; i < 20; i++)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }

            using var bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
            bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void ReflowTiles()
        {
            int curY = 4;
            foreach (var ctx in _tiles)
            {
                ctx.CardWrapper.Top = curY;
                curY += ctx.CardWrapper.Height + 16;
            }
            _mainScrollPanel.AutoScrollMinSize = new Size(0, curY + 20);
        }

        private void ToggleLogDrawer()
        {
            _logExpanded = !_logExpanded;
            _txtLog.Visible = _logExpanded;
            _bottomPanel.Height = _logExpanded ? 196 : 60;
            _bottomPanel.Invalidate();
        }

        private async Task StartArchivePipelineAsync(LauncherItem item)
        {
            string targetDir = Path.GetFullPath(_txtTargetLocation.Text.Trim());
            bool binariesOnly = _chkFastBinariesOnly;

            _isArchiving = true;
            foreach (var ctx in _tiles) ctx.BannerPanel?.Invalidate();
            _bottomPanel.Invalidate();
            _txtLog.Clear();

            AppendLog($"=== Blizzard Launcher Archive: Preserving {item.Title} ===");
            AppendLog($"Version: {item.Version} (Build {item.BuildNumber})");
            AppendLog($"Destination: {targetDir}");
            UpdateProgress(0, $"Archiving {item.Title} -> {targetDir}");

            _cts = new CancellationTokenSource();
            try
            {
                bool success = await Task.Run(() => LauncherDownloader.DownloadLauncherAsync(
                    _http,
                    item,
                    targetDir,
                    binariesOnly,
                    AppendLog,
                    UpdateProgress,
                    _cts.Token));

                if (success)
                {
                    UpdateProgress(100, $"Completed: {item.Title} ready in {targetDir}");
                    MessageBox.Show(
                        this,
                        $"{item.Title} (Build {item.BuildNumber}) is archived and ready in:\n\n{targetDir}",
                        "Blizzard Launcher Archive — Archival Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    UpdateProgress(0, $"Failed to archive {item.Title}");
                    MessageBox.Show(
                        this,
                        $"Failed to archive {item.Title}. Check log console for details.",
                        "Archival Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (OperationCanceledException)
            {
                UpdateProgress(0, "Archival operation cancelled by user.");
                AppendLog("[WARN] User cancelled download.");
            }
            catch (Exception ex)
            {
                UpdateProgress(0, $"Error: {ex.Message}");
                AppendLog($"[ERROR] {ex}");
            }
            finally
            {
                _isArchiving = false;
                foreach (var ctx in _tiles) ctx.BannerPanel?.Invalidate();
                _bottomPanel.Invalidate();
            }
        }

        private void UpdateProgress(double percent, string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateProgress(percent, status)));
                return;
            }

            _progressPercent = Math.Clamp(percent, 0.0, 100.0);
            _statusText = status;
            _bottomPanel.Invalidate();
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _txtLog.AppendText($"[{timestamp}] {message}\r\n");
        }

        private async Task CheckForAppUpdateAsync()
        {
            try
            {
                _availableUpdate = await UpdateChecker.CheckForUpdatesAsync(_http);
                if (_availableUpdate?.UpdateAvailable == true)
                {
                    if (InvokeRequired) BeginInvoke(new Action(() => _topToolbar.Invalidate()));
                    else _topToolbar.Invalidate();
                }
            }
            catch { }
        }

        // ================= DRAWING UTILITIES (IDENTICAL TO D3FOREVER) =================
        private static void ConfigureGraphics(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private static GraphicsPath CreateRoundRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (rect.Width <= 0 || rect.Height <= 0) return path;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawMinimizeButton(Graphics g, Rectangle r, bool hover)
        {
            if (hover)
            {
                using var path = CreateRoundRect(r, 4);
                using var fill = new SolidBrush(Color.FromArgb(32, 42, 60));
                g.FillPath(fill, path);
            }
            using var p = new Pen(hover ? Color.White : Color.FromArgb(148, 163, 184), 1.5f);
            int midY = r.Y + r.Height / 2 + 2;
            g.DrawLine(p, r.X + 10, midY, r.Right - 10, midY);
        }

        private static void DrawCloseButton(Graphics g, Rectangle r, bool hover)
        {
            if (hover)
            {
                using var path = CreateRoundRect(r, 4);
                using var fill = new SolidBrush(Color.FromArgb(190, 40, 50));
                g.FillPath(fill, path);
            }
            using var p = new Pen(hover ? Color.White : Color.FromArgb(148, 163, 184), 1.5f);
            g.DrawLine(p, r.X + 10, r.Y + 9, r.Right - 10, r.Bottom - 9);
            g.DrawLine(p, r.Right - 10, r.Y + 9, r.X + 10, r.Bottom - 9);
        }

        private static void DrawChevron(Graphics g, Point pt, bool expanded, Color c)
        {
            using var p = new Pen(c, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            if (expanded)
            {
                g.DrawLine(p, pt.X - 4, pt.Y + 2, pt.X, pt.Y - 2);
                g.DrawLine(p, pt.X, pt.Y - 2, pt.X + 4, pt.Y + 2);
            }
            else
            {
                g.DrawLine(p, pt.X - 4, pt.Y - 2, pt.X, pt.Y + 2);
                g.DrawLine(p, pt.X, pt.Y + 2, pt.X + 4, pt.Y - 2);
            }
        }

        private static void DrawModernCheckBox(Graphics g, Point pt, bool isChecked, string label)
        {
            var boxRect = new Rectangle(pt.X, pt.Y, 16, 16);
            using (var bPath = CreateRoundRect(boxRect, 3))
            using (var bFill = new SolidBrush(isChecked ? Color.FromArgb(56, 139, 253) : Color.FromArgb(14, 18, 26)))
            using (var bPen = new Pen(isChecked ? Color.FromArgb(88, 166, 255) : Color.FromArgb(55, 68, 95), 1.2f))
            {
                g.FillPath(bFill, bPath);
                g.DrawPath(bPen, bPath);
            }

            if (isChecked)
            {
                using var checkPen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(checkPen, new[]
                {
                    new Point(boxRect.X + 3, boxRect.Y + 8),
                    new Point(boxRect.X + 6, boxRect.Y + 12),
                    new Point(boxRect.X + 12, boxRect.Y + 4)
                });
            }

            using var font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb(203, 213, 225));
            g.DrawString(label, font, brush, pt.X + 22, pt.Y);
        }

        private static Image LoadEmbeddedImage(string resourceName)
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream != null) return Image.FromStream(stream);
            }
            catch { }
            return null;
        }

        private static Icon LoadEmbeddedIcon(string resourceName)
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream != null) return new Icon(stream);
            }
            catch { }
            return null;
        }
    }
}
