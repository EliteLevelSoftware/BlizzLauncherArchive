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

        public AgentShimServer(int preferredPort = 29505, string gameDirectory = null)
        {
            _agentDatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent", "Agent.dat");
            _port = preferredPort > 0 ? preferredPort : 29505;
            _gameDirectory = string.IsNullOrWhiteSpace(gameDirectory)
                ? @"C:\Games\Blizzard_Launcher_Archive\Diablo3_Retail_1.1.0.1272_16603"
                : Path.GetFullPath(gameDirectory);
        }

        public void Start()
        {
            if (IsRunning) return;

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
                    _port = 29505 + attempt;
                }
            }

            if (!_listener.IsListening)
            {
                throw new InvalidOperationException($"Could not bind Agent Shim HTTP listener on port {_port}.");
            }

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
                string pathOnly = rawUrl.Split('?')[0];
                string decompiledDir = LauncherAssetDecompiler.GetDecompiledFolder(_gameDirectory);
                string webDir = Path.Combine(decompiledDir, "web");
                var config = LauncherModConfig.LoadOrCreate(_gameDirectory);

                // Ensure web assets exist
                if (!Directory.Exists(webDir) || !File.Exists(Path.Combine(webDir, "news_thumb.png")))
                {
                    LauncherAssetDecompiler.GenerateEditableWebAssets(_gameDirectory, decompiledDir, config);
                }

                // 1. Serve PNG/image assets from Decompiled_Assets/web/
                if (pathOnly.StartsWith("/d3/assets/", StringComparison.OrdinalIgnoreCase))
                {
                    string fileName = Path.GetFileName(pathOnly);
                    string assetPath = Path.Combine(webDir, fileName);
                    if (File.Exists(assetPath))
                    {
                        byte[] imgBytes = File.ReadAllBytes(assetPath);
                        SendBytes(ctx, imgBytes, "image/png");
                        return;
                    }
                }

                // 2. Serve Launcher News & Community feed into QWebView (/D3/enUS/launcher/news)
                if (pathOnly.IndexOf("/launcher/news", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    pathOnly.Equals("/news", StringComparison.OrdinalIgnoreCase))
                {
                    string customNewsHtml = Path.Combine(webDir, "news.html");
                    string html = File.Exists(customNewsHtml)
                        ? File.ReadAllText(customNewsHtml, Encoding.UTF8)
                        : LauncherAssetDecompiler.BuildLauncherNewsHtml(config);
                    SendBytes(ctx, Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8");
                    return;
                }

                // 3. Serve interactive Mock Website / Article / Community pages inside QWebView
                if (pathOnly.StartsWith("/d3/", StringComparison.OrdinalIgnoreCase) ||
                    pathOnly.IndexOf("/launcher/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string html = LauncherAssetDecompiler.BuildMockPortalHtml(config, "SANCTUARY COMMUNITY & NEWS ARCHIVE");
                    SendBytes(ctx, Encoding.UTF8.GetBytes(html), "text/html; charset=utf-8");
                    return;
                }

                // 4. Serve Agent REST endpoints
                string responseJson = "{\"state\":1004,\"playable\":true}";
                SendBytes(ctx, Encoding.UTF8.GetBytes(responseJson), "application/json");
            }
            catch
            {
                try { ctx.Response.Abort(); } catch { }
            }
        }

        private static void SendBytes(HttpListenerContext ctx, byte[] buf, string contentType)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.Headers["Connection"] = "close";
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.Close();
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
