using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BlizzLauncherArchive
{
    public sealed class LauncherIntegrationDialog : Form
    {
        private ComboBox _cboLanguage;
        private ComboBox _cboBuild;
        private TextBox _txtServerIp;
        private TextBox _txtServerPort;
        private TextBox _txtCodeOutput;

        public LauncherIntegrationDialog(LauncherItem initialItem = null)
        {
            Text = "Blizzard Launcher Archive — Server Launcher Integration Studio";
            Size = new Size(880, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 20, 26);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            BuildUi(initialItem);
            GenerateCode();
        }

        private void BuildUi(LauncherItem initialItem)
        {
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(26, 30, 42),
                Padding = new Padding(24, 16, 24, 16)
            };
            pnlTop.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(0, 174, 255), 2);
                e.Graphics.DrawLine(p, 0, pnlTop.Height - 1, pnlTop.Width, pnlTop.Height - 1);
            };

            var lblTitle = new Label { Text = "🚀 SERVER LAUNCHER INTEGRATION STUDIO", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 14), AutoSize = true };
            var lblSub = new Label { Text = "Generate automated headless downloader & launcher wrapper code for your community or private server project.", Font = new Font("Segoe UI", 9f), ForeColor = Color.DarkGray, Location = new Point(22, 44), AutoSize = true };
            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSub });

            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblLang = new Label { Text = "1. TARGET LANGUAGE / FRAMEWORK:", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 174, 255) };
            _cboLanguage = new ComboBox
            {
                Location = new Point(20, 40),
                Size = new Size(380, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 32, 44),
                ForeColor = Color.White
            };
            _cboLanguage.Items.AddRange(new object[]
            {
                "C# / .NET (WPF / WinForms / DiIiS Launcher)",
                "Electron / Node.js (Modern Web Desktop Launcher)",
                "Python 3 (PyQt / CLI Script)",
                "PowerShell 7 / Windows PowerShell",
                "Batch / Command Prompt (.bat)",
                "C++ / Win32 (Native High-Performance Wrapper)"
            });
            _cboLanguage.SelectedIndex = 0;
            _cboLanguage.SelectedIndexChanged += (s, e) => GenerateCode();

            var lblBuild = new Label { Text = "2. SELECT HISTORICAL LAUNCHER BUILD:", Location = new Point(430, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(0, 174, 255) };
            _cboBuild = new ComboBox
            {
                Location = new Point(430, 40),
                Size = new Size(400, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(28, 32, 44),
                ForeColor = Color.White
            };
            foreach (var b in LauncherCatalog.AllLaunchers)
            {
                _cboBuild.Items.Add(b);
            }
            if (initialItem != null && _cboBuild.Items.Contains(initialItem))
                _cboBuild.SelectedItem = initialItem;
            else
                _cboBuild.SelectedIndex = 0;
            _cboBuild.SelectedIndexChanged += (s, e) => GenerateCode();

            var lblIp = new Label { Text = "3. CUSTOM SERVER HOST / IP:", Location = new Point(20, 85), AutoSize = true, ForeColor = Color.DarkGray };
            _txtServerIp = new TextBox { Text = "127.0.0.1", Location = new Point(20, 110), Size = new Size(380, 26), BackColor = Color.FromArgb(28, 32, 44), ForeColor = Color.White };
            _txtServerIp.TextChanged += (s, e) => GenerateCode();

            var lblPort = new Label { Text = "4. CUSTOM SERVER PORT:", Location = new Point(430, 85), AutoSize = true, ForeColor = Color.DarkGray };
            _txtServerPort = new TextBox { Text = "1119", Location = new Point(430, 110), Size = new Size(400, 26), BackColor = Color.FromArgb(28, 32, 44), ForeColor = Color.White };
            _txtServerPort.TextChanged += (s, e) => GenerateCode();

            var lblCode = new Label { Text = "GENERATED INTEGRATION CODE (READY TO PASTE):", Location = new Point(20, 155), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(40, 220, 90) };
            _txtCodeOutput = new TextBox
            {
                Location = new Point(20, 180),
                Size = new Size(810, 310),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                BackColor = Color.FromArgb(12, 14, 18),
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9f),
                WordWrap = false
            };

            pnlBody.Controls.AddRange(new Control[] {
                lblLang, _cboLanguage, lblBuild, _cboBuild, lblIp, _txtServerIp, lblPort, _txtServerPort,
                lblCode, _txtCodeOutput
            });

            var pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(22, 26, 36),
                Padding = new Padding(24, 12, 24, 12)
            };

            var btnCopy = new Button
            {
                Text = "📋 Copy Code",
                Location = new Point(570, 12),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(0, 140, 230),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCopy.Click += (s, e) =>
            {
                Clipboard.SetText(_txtCodeOutput.Text);
                MessageBox.Show("Integration code copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(715, 12),
                Size = new Size(100, 34),
                BackColor = Color.FromArgb(45, 52, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Close();

            pnlBottom.Controls.AddRange(new Control[] { btnCopy, btnClose });

            Controls.AddRange(new Control[] { pnlBody, pnlBottom, pnlTop });
        }

        private void GenerateCode()
        {
            var item = _cboBuild.SelectedItem as LauncherItem ?? LauncherCatalog.AllLaunchers[0];
            string ip = _txtServerIp.Text.Trim();
            string port = _txtServerPort.Text.Trim();
            int langIdx = _cboLanguage.SelectedIndex;

            var sb = new StringBuilder();

            switch (langIdx)
            {
                case 0: // C#
                    sb.AppendLine("// Blizzard Launcher Archive — Automated Client Launcher Integration");
                    sb.AppendLine("using System;");
                    sb.AppendLine("using System.Diagnostics;");
                    sb.AppendLine("using System.IO;");
                    sb.AppendLine();
                    sb.AppendLine("namespace MyServerLauncher");
                    sb.AppendLine("{");
                    sb.AppendLine("    public static class GameRunner");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        public const string ExpectedLauncher = \"{item.BinaryFileName}\";");
                    sb.AppendLine($"        public const string ServerAddress = \"{ip}:{port}\";");
                    sb.AppendLine();
                    sb.AppendLine("        public static void Launch(string gameDirectory)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            string exePath = Path.Combine(gameDirectory, ExpectedLauncher);");
                    sb.AppendLine("            var psi = new ProcessStartInfo");
                    sb.AppendLine("            {");
                    sb.AppendLine("                FileName = exePath,");
                    sb.AppendLine($"                Arguments = \"-launch -server {ip} -port {port}\",");
                    sb.AppendLine("                WorkingDirectory = gameDirectory,");
                    sb.AppendLine("                UseShellExecute = true");
                    sb.AppendLine("            };");
                    sb.AppendLine("            Process.Start(psi);");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                    break;

                case 1: // Electron
                    sb.AppendLine("// Blizzard Launcher Archive — Electron / Node.js Integration");
                    sb.AppendLine("const { execFile } = require('child_process');");
                    sb.AppendLine("const path = require('path');");
                    sb.AppendLine();
                    sb.AppendLine("function bootLauncher(gameDir) {");
                    sb.AppendLine($"    const binaryPath = path.join(gameDir, '{item.BinaryFileName}');");
                    sb.AppendLine($"    const args = ['-launch', '-server', '{ip}', '-port', '{port}'];");
                    sb.AppendLine();
                    sb.AppendLine("    execFile(binaryPath, args, { cwd: gameDir }, (err) => {");
                    sb.AppendLine("        if (err) console.error('Launcher boot error:', err);");
                    sb.AppendLine("    });");
                    sb.AppendLine("}");
                    break;

                case 2: // Python
                    sb.AppendLine("# Blizzard Launcher Archive — Python Launcher Wrapper");
                    sb.AppendLine("import subprocess");
                    sb.AppendLine("import os");
                    sb.AppendLine();
                    sb.AppendLine("def launch_game(game_dir):");
                    sb.AppendLine($"    binary = os.path.join(game_dir, '{item.BinaryFileName}')");
                    sb.AppendLine($"    cmd = [binary, '-launch', '-server', '{ip}', '-port', '{port}']");
                    sb.AppendLine("    subprocess.Popen(cmd, cwd=game_dir)");
                    break;

                case 3: // PowerShell
                    sb.AppendLine("# Blizzard Launcher Archive — PowerShell Boot Script");
                    sb.AppendLine($"$GameDir = \"C:\\Games\\Blizzard_Launcher_Archive\\{item.Family}_{item.Category}_{item.Version}_{item.BuildNumber}\"");
                    sb.AppendLine($"$Launcher = Join-Path $GameDir \"{item.BinaryFileName}\"");
                    sb.AppendLine($"Start-Process -FilePath $Launcher -ArgumentList \"-launch -server {ip} -port {port}\" -WorkingDirectory $GameDir");
                    break;

                case 4: // Batch
                    sb.AppendLine("@echo off");
                    sb.AppendLine($"title Booting {item.Title}");
                    sb.AppendLine($"start \"\" \"%~dp0{item.BinaryFileName}\" -launch -server {ip} -port {port}");
                    break;

                case 5: // C++
                    sb.AppendLine("// Blizzard Launcher Archive — Win32 C++ Native Launcher");
                    sb.AppendLine("#include <windows.h>");
                    sb.AppendLine("#include <string>");
                    sb.AppendLine();
                    sb.AppendLine("void BootLauncher(const std::wstring& gameDir) {");
                    sb.AppendLine($"    std::wstring binary = gameDir + L\"\\\\{item.BinaryFileName}\";");
                    sb.AppendLine($"    std::wstring args = L\"-launch -server {ip} -port {port}\";");
                    sb.AppendLine("    ShellExecuteW(NULL, L\"open\", binary.c_str(), args.c_str(), gameDir.c_str(), SW_SHOW);");
                    sb.AppendLine("}");
                    break;
            }

            _txtCodeOutput.Text = sb.ToString();
        }
    }
}
