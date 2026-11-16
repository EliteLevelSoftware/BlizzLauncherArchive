using System;
using System.Collections.Generic;
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
        private LauncherModConfig _config;

        private Image _masterImage;
        private Image _btnPlayOff, _btnPlayOver, _btnPlayDown;
        private Image _btnCloseOff, _btnCloseOver, _btnCloseDown;
        private Image _btnMinOff, _btnMinOver, _btnMinDown;
        private Image _newsThumbImg, _commThumbImg;

        private Rectangle _rcClose = new Rectangle(760, 84, 25, 23);
        private Rectangle _rcMin = new Rectangle(734, 84, 25, 23);
        private Rectangle _rcOptions = new Rectangle(76, 84, 70, 22);
        private Rectangle _rcInfo = new Rectangle(164, 84, 46, 22);
        private Rectangle _rcHelp = new Rectangle(222, 84, 48, 22);
        private Rectangle _rcModStudio = new Rectangle(282, 84, 110, 22);
        private Rectangle _rcPlay = new Rectangle(564, 412, 192, 68);

        private Rectangle _rcNewsAll = new Rectangle(685, 193, 68, 18);
        private Rectangle _rcCommAll = new Rectangle(685, 304, 68, 18);
        private Rectangle _rcNewsThumb = new Rectangle(388, 220, 52, 52);
        private Rectangle _rcCommThumb = new Rectangle(388, 330, 52, 52);
        private readonly Rectangle[] _rcNewsItems = new Rectangle[4];
        private readonly Rectangle[] _rcCommItems = new Rectangle[4];
        private Rectangle _rcProgressBar = new Rectangle(68, 434, 476, 32);

        private int _hoveredId = 0;
        private bool _isMouseDown = false;
        private double _animProgress = 0.18;
        private readonly System.Windows.Forms.Timer _animTimer;

        public RetroLauncherEmulator(LauncherItem launcherMeta, string gameDirectory = null)
            : this(gameDirectory, launcherMeta)
        {
        }

        public RetroLauncherEmulator(string gameDirectory = null, LauncherItem launcherMeta = null)
        {
            _launcherMeta = launcherMeta;
            _family = launcherMeta?.Family ?? LauncherGameFamily.Diablo3;

            ResolveGameLocation(gameDirectory);
            _config = LauncherModConfig.LoadOrCreate(_gameDirectory, _launcherMeta);

            Text = $"{_config.WindowTitle} — Interactive Launcher Preview ({_launcherMeta?.Version ?? "1.1.0.1272"})";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            for (int i = 0; i < 4; i++)
            {
                _rcNewsItems[i] = new Rectangle(448, 218 + i * 16, 306, 16);
                _rcCommItems[i] = new Rectangle(448, 328 + i * 16, 306, 16);
            }

            LoadAssets();

            if (_masterImage != null && _family == LauncherGameFamily.Diablo3)
            {
                Size = new Size(_masterImage.Width, _masterImage.Height);
                BackColor = Color.Magenta;
                TransparencyKey = Color.Magenta;
            }
            else
            {
                Size = new Size(828, 608);
                BackColor = Color.FromArgb(14, 10, 8);
                ForeColor = Color.White;
            }

            _animTimer = new System.Windows.Forms.Timer { Interval = 35 };
            _animTimer.Tick += (s, e) =>
            {
                double target = Math.Clamp(_config.ProgressPercentage / 100.0, 0.05, 1.0);
                if (_animProgress < target)
                {
                    _animProgress = Math.Min(target, _animProgress + 0.018);
                    Invalidate(new Rectangle(50, 390, 510, 110));
                }
                else
                {
                    _animTimer.Stop();
                }
            };
            _animTimer.Start();
        }

        private void ResolveGameLocation(string preferredDir)
        {
            _gameDirectory = NativeLauncherRunner.ResolveGameDirectory(preferredDir, _launcherMeta);
            string exeName = _config?.TargetExecutable ?? "Diablo III.exe";
            string candidate = Path.Combine(_gameDirectory, exeName);
            if (File.Exists(candidate) && new FileInfo(candidate).Length > 0)
            {
                _resolvedExecutable = candidate;
                _isClientless = false;
            }
            else
            {
                _resolvedExecutable = null;
                _isClientless = true;
            }
        }

        private void LoadAssets()
        {
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("d3_retro_launcher.png");
                if (stream != null)
                {
                    _masterImage = Image.FromStream(stream);
                }
            }
            catch { }

            string decompDir = LauncherAssetDecompiler.GetDecompiledFolder(_gameDirectory);
            if (!Directory.Exists(decompDir))
            {
                try { LauncherAssetDecompiler.DecompileLauncherAssets(_gameDirectory, _launcherMeta); } catch { }
            }

            _btnPlayOff = TryLoadDecompiledImage(decompDir, "d3/btn_large_off.png", "btn_large_off.png");
            _btnPlayOver = TryLoadDecompiledImage(decompDir, "d3/btn_large_over.png", "btn_large_over.png");
            _btnPlayDown = TryLoadDecompiledImage(decompDir, "d3/btn_large_down.png", "btn_large_down.png");

            _btnCloseOff = TryLoadDecompiledImage(decompDir, "d3/btn_close_off.png", "btn_close_off.png");
            _btnCloseOver = TryLoadDecompiledImage(decompDir, "d3/btn_close_over.png", "btn_close_over.png");
            _btnCloseDown = TryLoadDecompiledImage(decompDir, "d3/btn_close_down.png", "btn_close_down.png");

            _btnMinOff = TryLoadDecompiledImage(decompDir, "d3/btn_min_off.png", "btn_min_off.png");
            _btnMinOver = TryLoadDecompiledImage(decompDir, "d3/btn_min_over.png", "btn_min_over.png");
            _btnMinDown = TryLoadDecompiledImage(decompDir, "d3/btn_min_down.png", "btn_min_down.png");

            _newsThumbImg = TryLoadDecompiledImage(decompDir, "web/news_thumb.png");
            _commThumbImg = TryLoadDecompiledImage(decompDir, "web/comm_thumb.png");
        }

        private static Image TryLoadDecompiledImage(string decompDir, params string[] relPaths)
        {
            foreach (string rel in relPaths)
            {
                string full = Path.Combine(decompDir, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full))
                {
                    try
                    {
                        using var fs = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        return Image.FromStream(fs);
                    }
                    catch { }
                }
            }
            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (_masterImage != null && _family == LauncherGameFamily.Diablo3)
            {
                g.Clear(Color.Magenta);
                g.DrawImage(_masterImage, 0, 0, Width, Height);
            }
            else
            {
                DrawCustomFamilyBackdrop(g);
            }

            // 1. Top Menu Bar ("MOD STUDIO" button + hover highlights on OPTIONS, INFO, HELP)
            DrawTopMenuAndWindowButtons(g);

            // 2. Dynamic News & Community Section (renders live items from launcher-config.json + thumbnails + hover states)
            DrawDynamicNewsAndCommunity(g);

            // 3. Dynamic 3-Stage Animated Progress Bar + PLAY Button with real decompiled hover/down textures
            DrawAnimatedProgressBarAndPlayButton(g);

            // 4. Footer Server & Build Status Indicator
            string statusLine = $"● SERVER: {_config.ServerName.ToUpperInvariant()} ({_config.ServerHost}:{_config.ServerPort})  |  BUILD: {_launcherMeta?.BuildNumber ?? "16603"}";
            using var fontFooter = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var brushFooter = new SolidBrush(Color.FromArgb(215, 70, 225, 110));
            g.DrawString(statusLine, fontFooter, brushFooter, 68, 514);
        }

        private void DrawCustomFamilyBackdrop(Graphics g)
        {
            Color cTop = _family switch
            {
                LauncherGameFamily.StarCraft2 or LauncherGameFamily.StarCraftBroodWar => Color.FromArgb(12, 26, 48),
                LauncherGameFamily.WoW or LauncherGameFamily.Warcraft3 => Color.FromArgb(34, 24, 12),
                LauncherGameFamily.Hearthstone => Color.FromArgb(46, 30, 14),
                _ => Color.FromArgb(36, 12, 8)
            };
            using (var bg = new LinearGradientBrush(ClientRectangle, cTop, Color.FromArgb(10, 8, 8), 90f))
            {
                g.FillRectangle(bg, ClientRectangle);
            }
            using (var borderPen = new Pen(Color.FromArgb(165, 120, 62), 2))
            {
                g.DrawRectangle(borderPen, 24, 68, Width - 48, Height - 100);
            }
            using (var menuBg = new SolidBrush(Color.FromArgb(200, 18, 12, 10)))
            {
                g.FillRectangle(menuBg, 26, 78, Width - 52, 34);
            }
            using (var titleFont = new Font("Georgia", 20f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(245, 210, 135)))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(_launcherMeta?.Title ?? _config.WindowTitle, titleFont, titleBrush, new RectangleF(0, 24, Width, 38), sf);
            }
            using (var menuFont = new Font("Georgia", 9.5f, FontStyle.Bold))
            using (var menuBrush = new SolidBrush(Color.FromArgb(220, 195, 145)))
            {
                g.DrawString("OPTIONS", menuFont, menuBrush, _rcOptions.Left, _rcOptions.Top + 3);
                g.DrawString("INFO", menuFont, menuBrush, _rcInfo.Left, _rcInfo.Top + 3);
                g.DrawString("HELP", menuFont, menuBrush, _rcHelp.Left, _rcHelp.Top + 3);
            }

            // Left hero panel for non-D3 families
            var heroRect = new Rectangle(44, 124, 315, 270);
            using (var hb = new LinearGradientBrush(heroRect, Color.FromArgb(80, cTop.R + 40, cTop.G + 30, cTop.B + 20), Color.FromArgb(220, 12, 10, 10), 45f))
            {
                g.FillRectangle(hb, heroRect);
            }
            using (var hp = new Pen(Color.FromArgb(120, 90, 50), 1))
            {
                g.DrawRectangle(hp, heroRect);
            }
            using (var hf = new Font("Georgia", 13f, FontStyle.Bold))
            using (var htb = new SolidBrush(Color.FromArgb(235, 200, 140)))
            {
                g.DrawString($"{_launcherMeta?.ReleaseDate ?? "Classic Era"}\n{_launcherMeta?.Version ?? ""}", hf, htb, new RectangleF(60, 220, 280, 70));
            }
        }

        private void DrawTopMenuAndWindowButtons(Graphics g)
        {
            // Draw "MOD STUDIO" pill on menu bar
            using (var modBg = new SolidBrush(_hoveredId == 60 ? Color.FromArgb(180, 195, 72, 24) : Color.FromArgb(130, 95, 36, 16)))
            using (var modPen = new Pen(Color.FromArgb(210, 195, 140, 75), 1))
            using (var modFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                g.FillRectangle(modBg, _rcModStudio);
                g.DrawRectangle(modPen, _rcModStudio);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("⚙ MOD & RECOMPILE", modFont, Brushes.AntiqueWhite, _rcModStudio, sf);
            }

            if (_hoveredId == 3) DrawMenuUnderline(g, _rcOptions);
            else if (_hoveredId == 4) DrawMenuUnderline(g, _rcInfo);
            else if (_hoveredId == 5) DrawMenuUnderline(g, _rcHelp);

            // Minimize & Close button states from Decompiled_Assets
            if (_hoveredId == 1)
            {
                Image closeImg = _isMouseDown ? (_btnCloseDown ?? _btnCloseOver) : _btnCloseOver;
                if (closeImg != null) g.DrawImage(closeImg, _rcClose);
                else
                {
                    using var b = new SolidBrush(Color.FromArgb(90, 255, 60, 40));
                    g.FillRectangle(b, _rcClose);
                }
            }
            else if (_hoveredId == 2)
            {
                Image minImg = _isMouseDown ? (_btnMinDown ?? _btnMinOver) : _btnMinOver;
                if (minImg != null) g.DrawImage(minImg, _rcMin);
                else
                {
                    using var b = new SolidBrush(Color.FromArgb(80, 255, 190, 60));
                    g.FillRectangle(b, _rcMin);
                }
            }
        }

        private static void DrawMenuUnderline(Graphics g, Rectangle rc)
        {
            using var glow = new SolidBrush(Color.FromArgb(45, 255, 190, 80));
            g.FillRectangle(glow, rc);
            using var pen = new Pen(Color.FromArgb(230, 245, 195, 95), 2f);
            g.DrawLine(pen, rc.Left + 4, rc.Bottom - 2, rc.Right - 4, rc.Bottom - 2);
        }

        private void DrawDynamicNewsAndCommunity(Graphics g)
        {
            // Cover the right-side News/Community region cleanly so custom launcher-config.json text renders crisp and live
            var newsPanelRect = new Rectangle(380, 184, 382, 212);
            using (var bgBrush = new LinearGradientBrush(newsPanelRect, Color.FromArgb(245, 26, 11, 7), Color.FromArgb(250, 18, 7, 5), 90f))
            {
                g.FillRectangle(bgBrush, newsPanelRect);
            }

            using var headerFont = new Font("Georgia", 13f, FontStyle.Regular);
            using var viewAllFont = new Font("Arial", 8.5f, FontStyle.Bold);
            using var itemFont = new Font("Arial", 8.5f, FontStyle.Regular);
            using var itemHoverFont = new Font("Arial", 8.5f, FontStyle.Underline);
            using var linePen = new Pen(Color.FromArgb(180, 78, 44, 24), 1f);
            using var thumbBorder = new Pen(Color.FromArgb(220, 110, 76, 40), 1f);

            // --- NEWS SECTION ---
            g.DrawString("News", headerFont, new SolidBrush(Color.FromArgb(245, 232, 208)), 386, 188);
            Color viewAllNewsCol = _hoveredId == 7 ? Color.FromArgb(255, 165, 85) : Color.FromArgb(216, 98, 28);
            g.DrawString("> View All", viewAllFont, new SolidBrush(viewAllNewsCol), _rcNewsAll.Left, _rcNewsAll.Top + 2);
            g.DrawLine(linePen, 388, 211, 754, 211);

            if (_newsThumbImg != null)
                g.DrawImage(_newsThumbImg, _rcNewsThumb);
            g.DrawRectangle(thumbBorder, _rcNewsThumb);

            for (int i = 0; i < Math.Min(4, _config.NewsItems.Count); i++)
            {
                var n = _config.NewsItems[i];
                bool hovered = (_hoveredId == 10 + i);
                var rc = _rcNewsItems[i];
                using var titleBrush = new SolidBrush(hovered ? Color.FromArgb(255, 230, 165) : Color.FromArgb(176, 141, 87));
                using var dateBrush = new SolidBrush(hovered ? Color.FromArgb(225, 190, 130) : Color.FromArgb(125, 99, 64));
                g.DrawString("• " + n.Title, hovered ? itemHoverFont : itemFont, titleBrush, rc.Left, rc.Top);
                var sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(n.Date, itemFont, dateBrush, new RectangleF(rc.Left, rc.Top, rc.Width, rc.Height), sfRight);
            }

            // --- COMMUNITY SECTION ---
            g.DrawString("Community", headerFont, new SolidBrush(Color.FromArgb(245, 232, 208)), 386, 298);
            Color viewAllCommCol = _hoveredId == 8 ? Color.FromArgb(255, 165, 85) : Color.FromArgb(216, 98, 28);
            g.DrawString("> View All", viewAllFont, new SolidBrush(viewAllCommCol), _rcCommAll.Left, _rcCommAll.Top + 2);
            g.DrawLine(linePen, 388, 321, 754, 321);

            if (_commThumbImg != null)
                g.DrawImage(_commThumbImg, _rcCommThumb);
            g.DrawRectangle(thumbBorder, _rcCommThumb);

            for (int i = 0; i < Math.Min(4, _config.CommunityItems.Count); i++)
            {
                var c = _config.CommunityItems[i];
                bool hovered = (_hoveredId == 20 + i);
                var rc = _rcCommItems[i];
                using var titleBrush = new SolidBrush(hovered ? Color.FromArgb(255, 230, 165) : Color.FromArgb(176, 141, 87));
                using var dateBrush = new SolidBrush(hovered ? Color.FromArgb(225, 190, 130) : Color.FromArgb(125, 99, 64));
                g.DrawString("• " + c.Title, hovered ? itemHoverFont : itemFont, titleBrush, rc.Left, rc.Top);
                var sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(c.Date, itemFont, dateBrush, new RectangleF(rc.Left, rc.Top, rc.Width, rc.Height), sfRight);
            }
        }

        private void DrawAnimatedProgressBarAndPlayButton(Graphics g)
        {
            // Cover the progress bar & status text zone so animated load bar & custom stage labels render cleanly
            var barZone = new Rectangle(58, 396, 496, 92);
            using (var zoneBg = new SolidBrush(Color.FromArgb(248, 17, 7, 5)))
            {
                g.FillRectangle(zoneBg, barZone);
            }

            using var numFont = new Font("Georgia", 18f, FontStyle.Regular);
            using var stageFont = new Font("Georgia", 8.5f, FontStyle.Bold);
            using var subFont = new Font("Georgia", 8f, FontStyle.Regular);

            // Stage 1: SETUP
            g.DrawString("1", numFont, new SolidBrush(Color.FromArgb(235, 65, 18)), 88, 398);
            g.DrawString(_config.Stage1Title, stageFont, new SolidBrush(Color.FromArgb(235, 65, 18)), 112, 398);
            g.DrawString(_config.Stage1SubText, subFont, new SolidBrush(Color.FromArgb(150, 125, 95)), 112, 413);

            // Stage 2: AVAILABLE
            Color s2Col = _animProgress >= 0.35 ? Color.FromArgb(215, 165, 45) : Color.FromArgb(125, 98, 42);
            g.DrawString($"2 {_config.Stage2Title}", stageFont, new SolidBrush(s2Col), 248, 398);
            g.DrawString(_config.Stage2SubText, subFont, new SolidBrush(Color.FromArgb(125, 105, 78)), 248, 413);

            // Stage 3: PLAYABLE
            Color s3Col = _animProgress >= 0.85 ? Color.FromArgb(75, 195, 75) : Color.FromArgb(62, 115, 58);
            g.DrawString($"3 {_config.Stage3Title}", stageFont, new SolidBrush(s3Col), 414, 398);
            g.DrawString(_config.Stage3SubText, subFont, new SolidBrush(Color.FromArgb(125, 105, 78)), 414, 413);

            // Ornate Progress Bar Track
            var trackRect = new Rectangle(72, 438, 468, 22);
            using (var trackBg = new SolidBrush(Color.FromArgb(26, 8, 6)))
            {
                g.FillRectangle(trackBg, trackRect);
            }

            int fillWidth = Math.Max(12, (int)(trackRect.Width * _animProgress));
            var fillRect = new Rectangle(trackRect.X + 2, trackRect.Y + 2, Math.Min(trackRect.Width - 4, fillWidth), trackRect.Height - 4);
            Color fillStart = _animProgress >= 0.99 ? Color.FromArgb(185, 38, 12) : Color.FromArgb(155, 20, 6);
            Color fillEnd = _animProgress >= 0.99 ? Color.FromArgb(255, 145, 35) : Color.FromArgb(245, 95, 18);
            using (var barGrad = new LinearGradientBrush(fillRect, fillStart, fillEnd, 0f))
            {
                g.FillRectangle(barGrad, fillRect);
            }

            // Threshold Pointers
            using (var yellowPtr = new SolidBrush(Color.FromArgb(215, 175, 45)))
            {
                int px1 = trackRect.X + (int)(trackRect.Width * 0.34);
                g.FillPolygon(yellowPtr, new[] { new Point(px1, trackRect.Bottom - 1), new Point(px1 - 5, trackRect.Bottom + 5), new Point(px1 + 5, trackRect.Bottom + 5) });
            }
            using (var greenPtr = new SolidBrush(Color.FromArgb(65, 185, 65)))
            {
                int px2 = trackRect.X + (int)(trackRect.Width * 0.91);
                g.FillPolygon(greenPtr, new[] { new Point(px2, trackRect.Bottom - 1), new Point(px2 - 5, trackRect.Bottom + 5), new Point(px2 + 5, trackRect.Bottom + 5) });
            }

            using (var framePen = new Pen(_hoveredId == 9 ? Color.FromArgb(215, 165, 90) : Color.FromArgb(118, 92, 64), 1.5f))
            {
                g.DrawRectangle(framePen, trackRect);
            }

            // Status Text under Progress Bar
            string bottomStatus = _animProgress < 0.99
                ? $"STREAMING GAME DATA ({(int)(_animProgress * 100)}%) — CLICK BAR TO REPLAY"
                : $"{_config.StatusText}   •   {_config.TotalDownloadRemainingText}";
            using (var statusFont = new Font("Georgia", 8f, FontStyle.Regular))
            using (var statusBrush = new SolidBrush(Color.FromArgb(215, 178, 92)))
            {
                var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString(bottomStatus, statusFont, statusBrush, new RectangleF(trackRect.X, trackRect.Bottom + 7, trackRect.Width, 18), sfCenter);
            }

            // PLAY Button (using real decompiled sprites from Decompiled_Assets/d3/btn_large_off/over/down.png)
            Image playSprite = _hoveredId == 6
                ? (_isMouseDown ? (_btnPlayDown ?? _btnPlayOver) : (_btnPlayOver ?? _btnPlayOff))
                : _btnPlayOff;

            if (playSprite != null)
            {
                g.DrawImage(playSprite, _rcPlay);
            }
            else
            {
                using var pb = new LinearGradientBrush(_rcPlay, Color.FromArgb(140, 32, 14), Color.FromArgb(70, 14, 6), 90f);
                g.FillRectangle(pb, _rcPlay);
                using var pp = new Pen(Color.FromArgb(185, 140, 78), 1.5f);
                g.DrawRectangle(pp, _rcPlay);
            }

            using var playFont = new Font("Georgia", 18f, FontStyle.Regular);
            Color playTextCol = _hoveredId == 6 ? Color.FromArgb(255, 245, 205) : Color.FromArgb(245, 205, 145);
            using var playBrush = new SolidBrush(playTextCol);
            var sfPlay = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(_config.PlayButtonText, playFont, playBrush, _rcPlay, sfPlay);
        }

        private int HitTest(Point pt)
        {
            if (_rcClose.Contains(pt)) return 1;
            if (_rcMin.Contains(pt)) return 2;
            if (_rcOptions.Contains(pt)) return 3;
            if (_rcInfo.Contains(pt)) return 4;
            if (_rcHelp.Contains(pt)) return 5;
            if (_rcModStudio.Contains(pt)) return 60;
            if (_rcPlay.Contains(pt)) return 6;
            if (_rcNewsAll.Contains(pt) || _rcNewsThumb.Contains(pt)) return 7;
            if (_rcCommAll.Contains(pt) || _rcCommThumb.Contains(pt)) return 8;
            if (_rcProgressBar.Contains(pt)) return 9;

            for (int i = 0; i < 4; i++)
            {
                if (_rcNewsItems[i].Contains(pt)) return 10 + i;
                if (_rcCommItems[i].Contains(pt)) return 20 + i;
            }
            return 0;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hit = HitTest(e.Location);
            Cursor = hit != 0 ? Cursors.Hand : Cursors.Default;
            if (hit != _hoveredId)
            {
                _hoveredId = hit;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            int hit = HitTest(e.Location);
            if (hit != 0)
            {
                _isMouseDown = true;
                Invalidate();
                return;
            }

            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_isMouseDown) return;
            _isMouseDown = false;
            Invalidate();

            int hit = HitTest(e.Location);
            if (hit == 1) Close();
            else if (hit == 2) WindowState = FormWindowState.Minimized;
            else if (hit == 3) OpenMockSite("status");
            else if (hit == 4) OpenMockSite("patchnotes");
            else if (hit == 5) OpenMockSite("patchnotes");
            else if (hit == 60) OpenModStudio();
            else if (hit == 6) HandlePlayClick();
            else if (hit == 7) OpenMockSite("news");
            else if (hit == 8) OpenMockSite("community");
            else if (hit == 9)
            {
                // Replay load bar animation when progress bar is clicked
                _animProgress = 0.05;
                _animTimer.Start();
            }
            else if (hit >= 10 && hit < 14)
            {
                int idx = hit - 10;
                string title = idx < _config.NewsItems.Count ? _config.NewsItems[idx].Title : null;
                OpenMockSite("news", title);
            }
            else if (hit >= 20 && hit < 24)
            {
                int idx = hit - 20;
                string title = idx < _config.CommunityItems.Count ? _config.CommunityItems[idx].Title : null;
                OpenMockSite("community", title);
            }
        }

        private void OpenMockSite(string section, string highlightTitle = null)
        {
            using var dlg = new MockWebsiteDialog(_config, _gameDirectory, section, highlightTitle);
            dlg.ShowDialog(this);
            _config = LauncherModConfig.LoadOrCreate(_gameDirectory, _launcherMeta);
            Invalidate();
        }

        private void OpenModStudio()
        {
            using var dlg = new LauncherModStudioDialog(_gameDirectory, _launcherMeta);
            dlg.ShowDialog(this);
            _config = LauncherModConfig.LoadOrCreate(_gameDirectory, _launcherMeta);
            LoadAssets();
            Invalidate();
        }

        private void HandlePlayClick()
        {
            ResolveGameLocation(_gameDirectory);
            if (!_isClientless && !string.IsNullOrWhiteSpace(_resolvedExecutable) && File.Exists(_resolvedExecutable))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _resolvedExecutable,
                        Arguments = _config.LaunchArguments,
                        WorkingDirectory = _gameDirectory,
                        UseShellExecute = true
                    });
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not boot game client: {ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            OpenMockSite("status");
        }
    }
}
