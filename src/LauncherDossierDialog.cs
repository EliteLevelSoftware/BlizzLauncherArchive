using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class LauncherDossierDialog : Form
    {
        public LauncherDossierDialog(LauncherItem item)
        {
            var dossier = LauncherDossierDatabase.GetDossier(item.Id);

            Text = $"Dossier — {item.Title} (Build {item.BuildNumber})";
            Size = new Size(680, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 20, 26);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            // Top Header
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(26, 30, 40),
                Padding = new Padding(24, 16, 24, 16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(240, 185, 45), 2);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = item.Title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 195, 65),
                Location = new Point(20, 14),
                AutoSize = true
            };
            var lblMeta = new Label
            {
                Text = $"Family: {item.Family} | Category: {item.Category} | Version: {item.Version} | Build: {item.BuildNumber} | Released: {item.ReleaseDate}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DarkGray,
                Location = new Point(22, 44),
                AutoSize = true
            };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblMeta });

            // Main scroll body
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            int curY = 16;

            // Headline
            var lblHeadline = new Label
            {
                Text = dossier.Headline,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, curY),
                Size = new Size(610, 48)
            };
            pnlBody.Controls.Add(lblHeadline);
            curY += 56;

            // Badges flow
            var flowBadges = new FlowLayoutPanel
            {
                Location = new Point(20, curY),
                Size = new Size(610, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            foreach (var b in dossier.MilestoneBadges)
            {
                var lblBadge = new Label
                {
                    Text = b,
                    BackColor = Color.FromArgb(40, 50, 70),
                    ForeColor = Color.LightSkyBlue,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Padding = new Padding(8, 4, 8, 4),
                    Margin = new Padding(0, 0, 8, 0),
                    AutoSize = true
                };
                flowBadges.Controls.Add(lblBadge);
            }
            pnlBody.Controls.Add(flowBadges);
            curY += 46;

            // Era Context
            var lblContextTitle = new Label { Text = "HISTORICAL CONTEXT & ERA:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 174, 255), Location = new Point(20, curY), AutoSize = true };
            pnlBody.Controls.Add(lblContextTitle);
            curY += 24;

            var lblContext = new Label
            {
                Text = dossier.EraContext,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gainsboro,
                Location = new Point(20, curY),
                Size = new Size(610, 68)
            };
            pnlBody.Controls.Add(lblContext);
            curY += 76;

            // Key Highlights
            var lblHighlightsTitle = new Label { Text = "KEY ARCHIVAL HIGHLIGHTS:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 220, 90), Location = new Point(20, curY), AutoSize = true };
            pnlBody.Controls.Add(lblHighlightsTitle);
            curY += 24;

            var txtHighlights = new TextBox
            {
                Location = new Point(20, curY),
                Size = new Size(610, 90),
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None,
                Text = string.Join("\r\n", dossier.KeyHighlights.ConvertAll(h => $"•  {h}"))
            };
            pnlBody.Controls.Add(txtHighlights);
            curY += 100;

            // Quote / Trivia
            if (!string.IsNullOrWhiteSpace(dossier.FamousQuoteOrTrivia))
            {
                var lblQuote = new Label
                {
                    Text = dossier.FamousQuoteOrTrivia,
                    Font = new Font("Georgia", 9.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(245, 195, 65),
                    Location = new Point(20, curY),
                    Size = new Size(610, 48)
                };
                pnlBody.Controls.Add(lblQuote);
            }

            // Bottom bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(22, 26, 34),
                Padding = new Padding(24, 12, 24, 12)
            };

            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(540, 12),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(45, 52, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.Add(btnClose);

            Controls.AddRange(new Control[] { pnlBody, pnlBottom, pnlTop });
        }
    }
}
