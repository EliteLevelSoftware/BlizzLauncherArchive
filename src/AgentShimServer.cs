using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlizzLauncherArchive
{
    public sealed class AgentShimServer : IDisposable
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _listenTask;
        private int _port;
        private string _originalAgentDatContent;
        private readonly string _agentDatPath;
        private readonly string _gameDirectory;
        private bool _agentDatPatched = false;

        public int Port => _port;
        public bool IsRunning => _listener?.IsListening == true;

        public AgentShimServer(int preferredPort = 0, string gameDirectory = null)
        {
            _agentDatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent", "Agent.dat");
            _port = preferredPort > 0 ? preferredPort : 11195;
            _gameDirectory = string.IsNullOrWhiteSpace(gameDirectory)
                ? @"C:\Games\Blizzard_Launcher_Archive\Diablo3_Retail_1.1.0.1272_16603"
                : Path.GetFullPath(gameDirectory);
        }

        public void Start()
        {
            if (IsRunning) return;

            // Attempt to bind
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                    _listener.Start();
                    break;
                }
                catch
                {
                    _listener?.Close();
                    _port = 11190 + new Random().Next(10, 500);
                }
            }

            if (!_listener.IsListening)
            {
                throw new InvalidOperationException($"Could not bind Agent Shim HTTP listener on port {_port}.");
            }

            // Backup existing Agent.dat and set our port
            try
            {
                if (File.Exists(_agentDatPath))
                {
                    _originalAgentDatContent = File.ReadAllText(_agentDatPath).Trim();
                }
                string agentDir = Path.GetDirectoryName(_agentDatPath);
                if (!Directory.Exists(agentDir)) Directory.CreateDirectory(agentDir);
                File.WriteAllText(_agentDatPath, _port.ToString());
                _agentDatPatched = true;
            }
            catch
            {
                // Non-critical if unable to write Agent.dat
            }

            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(ctx));
                }
                catch
                {
                    if (ct.IsCancellationRequested) break;
                }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                string rawUrl = ctx.Request.RawUrl ?? string.Empty;
                string responseJson;
                string formattedGameDir = _gameDirectory.Replace('\\', '/');

                if (rawUrl.Equals("/agent", StringComparison.OrdinalIgnoreCase) || rawUrl.StartsWith("/agent?", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{\"authorization\":\"1234567890\",\"opt_in_feedback\":true,\"pid\":1234,\"region\":\"us\",\"session\":\"1234567890\",\"state\":1004,\"type\":\"retail\",\"version\":\"1.0.0.524\"}";
                }
                else if (rawUrl.StartsWith("/game/Client", StringComparison.OrdinalIgnoreCase) || rawUrl.StartsWith("/game/client", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = $"{{\"uid\":\"client\",\"patch_url\":\"http://127.0.0.1:{_port}/patch\",\"config\":{{\"supports_multibox\":false,\"patch_url\":\"http://127.0.0.1:{_port}/patch\",\"product\":\"Clnt\",\"update_method\":\"client update\",\"binary_version_path\":\"Blizzard Launcher.exe\",\"binary_launch_path\":\"Blizzard Launcher.exe\",\"preinstalled\":true,\"install_dir\":\"C:/ProgramData/Battle.net/Client\"}}}}";
                }
                else if (rawUrl.StartsWith("/game", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = $"{{\"uid\":\"d3\",\"product\":\"d3\",\"installed\":true,\"playable\":true,\"version\":\"1.1.0.1272\",\"binary_launch_path\":\"Diablo III.exe\",\"install_dir\":\"{formattedGameDir}\",\"patch_url\":\"http://127.0.0.1:{_port}/patch\",\"update_total\":0,\"update_current\":0,\"download_total\":0,\"download_current\":0}}";
                }
                else if (rawUrl.StartsWith("/install", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{\"installed\":true,\"playable\":true,\"progress\":1.0,\"state\":1004}";
                }
                else if (rawUrl.StartsWith("/update", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{\"state\":1004,\"progress\":1.0,\"playable\":true,\"download_current\":100,\"download_total\":100,\"update_current\":100,\"update_total\":100}";
                }
                else if (rawUrl.StartsWith("/gamesession", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{}";
                }
                else
                {
                    responseJson = "{}";
                }

                byte[] buf = Encoding.UTF8.GetBytes(responseJson);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = buf.Length;
                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                ctx.Response.Close();
            }
            catch
            {
                try { ctx.Response.Abort(); } catch { }
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }

            // Restore Agent.dat
            if (_agentDatPatched && File.Exists(_agentDatPath))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_originalAgentDatContent))
                    {
                        File.WriteAllText(_agentDatPath, _originalAgentDatContent);
                    }
                }
                catch { }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
