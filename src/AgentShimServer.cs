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
        private bool _agentDatPatched = false;

        public int Port => _port;
        public bool IsRunning => _listener?.IsListening == true;

        public AgentShimServer(int preferredPort = 0)
        {
            _agentDatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Battle.net", "Agent", "Agent.dat");
            _port = preferredPort > 0 ? preferredPort : 11190;
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

                // Legacy Agent 1.x response payload
                if (rawUrl.Contains("/gamesession", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{\"status\":\"ok\"}";
                }
                else if (rawUrl.Contains("/version", StringComparison.OrdinalIgnoreCase))
                {
                    responseJson = "{\"version\":\"1.1.0.1272\",\"status\":\"ok\"}";
                }
                else
                {
                    // Default /agent response
                    responseJson = "{\"status\":\"ok\",\"playable\":true,\"installed\":true,\"progress\":1.0,\"download_progress\":1.0,\"update_progress\":1.0,\"state\":1004}";
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
