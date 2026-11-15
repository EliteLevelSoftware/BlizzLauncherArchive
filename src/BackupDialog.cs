using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class BackupDialog : Form
    {
        private readonly string _initialDestination;
        private readonly HttpClient _http;
        private CancellationTokenSource _cts;

        private TextBox _txtDest;
        private Label _lblInventory;
        private CheckBox _chkBinariesOnly;
        private CheckBox _chkBnet;
        private CheckBox _chkD3;
        private CheckBox _chkWow;
        private CheckBox _chkSc2;
        private CheckBox _chkClassic;
        private ProgressBar _pbOverall;
        private Label _lblStatus;
        private TextBox _txtLog;
        private Button _btnStart;
        private Button _btnCancel;

        public BackupDialog(string initialDestination, HttpClient http)
        {
            _initialDestination = initialDestination ?? @"C:\Games\Blizzard_Launcher_Archive";
            _http = http;

            Text = "Blizzard Launcher Archive — Cold Storage & Bulk Archival Suite";
            Size = new Size(860, 680);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(16, 18, 22);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            BuildUi();
            RefreshInventory();
        }

        private void BuildUi()
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(24, 28, 36),
                Padding = new Padding(24, 16, 24, 16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(0, 160, 240), 2);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = "📦 COLD STORAGE & BULK ARCHIVAL ENGINE",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 14),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "Mirror the complete catalog of historical Blizzard launchers for 100% offline cold storage.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DarkGray,
                Location = new Point(22, 44),
                AutoSize = true
            };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSub });

            // Main settings
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            var lblDestPrompt = new Label { Text = "DESTINATION STORAGE DIRECTORY (External Drive / NAS / Local Folder):", Location = new Point(20, 16), AutoSize = true, ForeColor = Color.FromArgb(0, 174, 255), Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _txtDest = new TextBox { Text = _initialDestination, Location = new Point(20, 40), Size = new Size(680, 26), BackColor = Color.FromArgb(28, 32, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            _txtDest.TextChanged += (s, e) => RefreshInventory();

            var btnBrowse = new Button { Text = "Browse...", Location = new Point(710, 38), Size = new Size(100, 28), BackColor = Color.FromArgb(40, 48, 62), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog { SelectedPath = _txtDest.Text };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _txtDest.Text = fbd.SelectedPath;
                }
            };

            _lblInventory = new Label { Text = "Scanning destination inventory...", Location = new Point(20, 78), AutoSize = true, ForeColor = Color.FromArgb(40, 220, 90), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

            // Game family selection checkboxes
            var grpFamilies = new GroupBox
            {
                Text = "SELECT GAME FAMILIES TO INCLUDE IN BACKUP",
                Location = new Point(20, 115),
                Size = new Size(790, 85),
                ForeColor = Color.FromArgb(240, 195, 75),
                BackColor = Color.FromArgb(22, 26, 34)
            };
            _chkBnet = new CheckBox { Text = "Battle.net Desktop App", Checked = true, Location = new Point(20, 30), AutoSize = true, ForeColor = Color.White };
            _chkD3 = new CheckBox { Text = "Diablo III Standalone", Checked = true, Location = new Point(190, 30), AutoSize = true, ForeColor = Color.White };
            _chkWow = new CheckBox { Text = "World of Warcraft", Checked = true, Location = new Point(350, 30), AutoSize = true, ForeColor = Color.White };
            _chkSc2 = new CheckBox { Text = "StarCraft Series", Checked = true, Location = new Point(500, 30), AutoSize = true, ForeColor = Color.White };
            _chkClassic = new CheckBox { Text = "Classic Strategy (War3/D2)", Checked = true, Location = new Point(630, 30), AutoSize = true, ForeColor = Color.White };

            _chkBnet.CheckedChanged += (s, e) => RefreshInventory();
            _chkD3.CheckedChanged += (s, e) => RefreshInventory();
            _chkWow.CheckedChanged += (s, e) => RefreshInventory();
            _chkSc2.CheckedChanged += (s, e) => RefreshInventory();
            _chkClassic.CheckedChanged += (s, e) => RefreshInventory();

            grpFamilies.Controls.AddRange(new Control[] { _chkBnet, _chkD3, _chkWow, _chkSc2, _chkClassic });

            _chkBinariesOnly = new CheckBox
            {
                Text = "⚡ Fast Binaries Only (Skip multi-megabyte SetupWin.mpq / raw skin archives)",
                Checked = false,
                Location = new Point(20, 215),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            _lblStatus = new Label { Text = "Ready to begin archival backup.", Location = new Point(20, 250), AutoSize = true, ForeColor = Color.LightGray };
            _pbOverall = new ProgressBar { Location = new Point(20, 275), Size = new Size(790, 22), Maximum = 100 };

            _txtLog = new TextBox
            {
                Location = new Point(20, 310),
                Size = new Size(790, 180),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            pnlBody.Controls.AddRange(new Control[] {
                lblDestPrompt, _txtDest, btnBrowse, _lblInventory, grpFamilies, _chkBinariesOnly,
                _lblStatus, _pbOverall, _txtLog
            });

            // Bottom bar
            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = Color.FromArgb(20, 24, 30),
                Padding = new Padding(24, 12, 24, 12)
            };

            _btnStart = new Button
            {
                Text = "START BACKUP",
                Location = new Point(540, 14),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(0, 140, 230),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnStart.Click += async (s, e) => await StartBackupAsync();

            _btnCancel = new Button
            {
                Text = "Close",
                Location = new Point(705, 14),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(45, 52, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnCancel.Click += (s, e) =>
            {
                if (_cts != null && !_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                    _btnCancel.Text = "Closing...";
                    _btnCancel.Enabled = false;
                }
                else
                {
                    Close();
                }
            };

            pnlBottom.Controls.AddRange(new Control[] { _btnStart, _btnCancel });

            Controls.AddRange(new Control[] { pnlBody, pnlBottom, pnlTop });
        }

        private IEnumerable<LauncherItem> GetSelectedItems()
        {
            var list = new List<LauncherItem>();
            if (_chkBnet.Checked) list.AddRange(LauncherCatalog.AllLaunchers.Where(x => x.Family == LauncherGameFamily.BattleNet));
            if (_chkD3.Checked) list.AddRange(LauncherCatalog.AllLaunchers.Where(x => x.Family == LauncherGameFamily.Diablo3));
            if (_chkWow.Checked) list.AddRange(LauncherCatalog.AllLaunchers.Where(x => x.Family == LauncherGameFamily.WoW));
            if (_chkSc2.Checked) list.AddRange(LauncherCatalog.AllLaunchers.Where(x => x.Family == LauncherGameFamily.StarCraft));
            if (_chkClassic.Checked) list.AddRange(LauncherCatalog.AllLaunchers.Where(x => x.Family == LauncherGameFamily.Classic));
            return list;
        }

        private void RefreshInventory()
        {
            try
            {
                var selected = GetSelectedItems().ToList();
                var rep = BackupEngine.InspectDestination(_txtDest.Text, selected);
                _lblInventory.Text = $"Destination Status: {rep.PresentBuildsCount}/{rep.TotalBuildsCount} builds present locally ({rep.PresentBytes / 1024.0 / 1024.0:F1} MB) — {rep.NeededBuildsCount} to download ({rep.NeededBytes / 1024.0 / 1024.0:F1} MB needed).";
            }
            catch (Exception ex)
            {
                _lblInventory.Text = $"Notice: {ex.Message}";
            }
        }

        private async Task StartBackupAsync()
        {
            var selected = GetSelectedItems().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one game family to archive.", "No Items Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnStart.Enabled = false;
            _btnCancel.Text = "Cancel";
            _cts = new CancellationTokenSource();

            AppendLog($"Starting Cold Storage Backup of {selected.Count} launcher builds...");

            try
            {
                await BackupEngine.ExecuteBulkArchiveAsync(
                    _http,
                    _txtDest.Text,
                    selected,
                    _chkBinariesOnly.Checked,
                    AppendLog,
                    (pct, status) =>
                    {
                        Invoke(() =>
                        {
                            _pbOverall.Value = (int)Math.Max(0, Math.Min(100, pct));
                            _lblStatus.Text = status;
                        });
                    },
                    _cts.Token);

                MessageBox.Show("All requested launchers have been backed up and verified!", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                AppendLog("[ABORTED] Archival operation cancelled by user.");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Backup encountered an issue: {ex.Message}");
                MessageBox.Show($"Backup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnStart.Enabled = true;
                _btnCancel.Text = "Close";
                RefreshInventory();
            }
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendLog(text));
                return;
            }
            _txtLog.AppendText(text + "\r\n");
        }
    }
}
