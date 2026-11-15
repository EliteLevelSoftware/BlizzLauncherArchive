using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class UpdateDialog : Form
    {
        public UpdateDialog(UpdateInfo info)
        {
            Text = "Software Update Available — Blizzard Launcher Archive";
            Size = new Size(620, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 20, 26);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(24, 30, 42),
                Padding = new Padding(24, 16, 24, 16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(245, 195, 65), 2);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var lblTitle = new Label { Text = "⭐ NEW UPDATE AVAILABLE", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(245, 195, 65), Location = new Point(20, 14), AutoSize = true };
            var lblSub = new Label { Text = $"Current Version: v{info.CurrentVersion}  ⟶  Latest Release: v{info.LatestVersion}", Font = new Font("Segoe UI", 9f), ForeColor = Color.Gainsboro, Location = new Point(22, 44), AutoSize = true };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSub });

            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblNotesTitle = new Label { Text = "RELEASE NOTES & CHANGELOG:", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 174, 255), Location = new Point(20, 14), AutoSize = true };
            var txtNotes = new TextBox
            {
                Location = new Point(20, 38),
                Size = new Size(560, 260),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                Text = string.IsNullOrWhiteSpace(info.ReleaseNotes) ? "New performance optimizations, launcher additions, and integrity enhancements." : info.ReleaseNotes
            };
            pnlBody.Controls.AddRange(new Control[] { lblNotesTitle, txtNotes });

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(22, 26, 36),
                Padding = new Padding(24, 12, 24, 12)
            };

            var btnDownload = new Button
            {
                Text = "Download Update",
                Location = new Point(340, 12),
                Size = new Size(150, 34),
                BackColor = Color.FromArgb(0, 140, 230),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDownload.Click += (s, e) =>
            {
                string url = !string.IsNullOrWhiteSpace(info.DownloadUrl) ? info.DownloadUrl : "https://github.com/EliteLevelSoftware/BlizzLauncherArchive/releases/latest";
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            };

            var btnClose = new Button
            {
                Text = "Later",
                Location = new Point(500, 12),
                Size = new Size(80, 34),
                BackColor = Color.FromArgb(45, 52, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.AddRange(new Control[] { btnDownload, btnClose });

            Controls.AddRange(new Control[] { pnlBody, pnlBottom, pnlTop });
        }
    }
}
