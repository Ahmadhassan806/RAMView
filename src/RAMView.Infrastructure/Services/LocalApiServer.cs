using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RAMView.Core.Interfaces;
using RAMView.Core.Models;

namespace RAMView.Infrastructure.Services;

/// <summary>
/// Lightweight local HTTP server enabling web frontends and browser visualizers
/// to stream live, real hardware RAM metrics and process data from the user's computer.
/// </summary>
public class LocalApiServer : IDisposable
{
    private readonly IMemoryMonitor _ramMonitor;
    private readonly IProcessProvider _processProvider;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    public int Port { get; }

    public LocalApiServer(IMemoryMonitor ramMonitor, IProcessProvider processProvider, int port = 51888)
    {
        _ramMonitor = ramMonitor ?? throw new ArgumentNullException(nameof(ramMonitor));
        _processProvider = processProvider ?? throw new ArgumentNullException(nameof(processProvider));
        Port = port;
    }

    public void Start()
    {
        if (_listener != null && _listener.IsListening) return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        try
        {
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            // Non-fatal if port is in use or permissions restricted
            System.Diagnostics.Debug.WriteLine($"LocalApiServer start error: {ex.Message}");
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (Exception)
            {
                if (ct.IsCancellationRequested) break;
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        // CORS Headers for browser web apps
        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

        if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            res.StatusCode = (int)HttpStatusCode.NoContent;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

        if (path == "/api/snapshot")
        {
            try
            {
                var mem = await _ramMonitor.GetSystemMemorySnapshotAsync();
                var procs = await _processProvider.GetProcessesAsync(groupProcesses: true, showSystemProcesses: true);

                var payload = new
                {
                    totalPhysicalBytes = mem.TotalPhysicalBytes,
                    usedPhysicalBytes = mem.UsedPhysicalBytes,
                    availablePhysicalBytes = mem.AvailablePhysicalBytes,
                    systemMemoryUsagePercentage = mem.SystemMemoryUsagePercentage,
                    summaryText = mem.SummaryText,
                    formattedTotal = mem.FormattedTotal,
                    formattedUsed = mem.FormattedUsed,
                    formattedAvailable = mem.FormattedAvailable,
                    processes = procs
                };

                byte[] json = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                res.ContentType = "application/json";
                res.StatusCode = (int)HttpStatusCode.OK;
                res.ContentLength64 = json.Length;
                await res.OutputStream.WriteAsync(json);
            }
            catch (Exception ex)
            {
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] err = Encoding.UTF8.GetBytes(ex.Message);
                await res.OutputStream.WriteAsync(err);
            }
        }
        else if (path == "/api/kill" && req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await HandleKillProcessAsync(req, res);
        }
        else if (path == "/api/health")
        {
            res.ContentType = "application/json";
            byte[] ok = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"service\":\"RAMView Local Agent\",\"port\":51888}");
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentLength64 = ok.Length;
            await res.OutputStream.WriteAsync(ok);
        }
        else
        {
            res.StatusCode = (int)HttpStatusCode.NotFound;
        }

        res.Close();
    }

    // Critical system processes that should never be killed from the web UI
    private static readonly HashSet<string> ProtectedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "idle", "smss", "csrss", "wininit", "winlogon", "services",
        "lsass", "lsaiso", "svchost", "fontdrvhost", "dwm", "registry",
        "memory compression", "system idle process", "secure system"
    };

    private async Task HandleKillProcessAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        res.ContentType = "application/json";

        try
        {
            // Read request body for JSON { "pid": 1234 }
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = await reader.ReadToEndAsync();
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("pid", out var pidEl))
            {
                res.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] errMsg = Encoding.UTF8.GetBytes("{\"success\":false,\"error\":\"Missing 'pid' in request body\"}");
                res.ContentLength64 = errMsg.Length;
                await res.OutputStream.WriteAsync(errMsg);
                return;
            }

            int pid = pidEl.GetInt32();

            // Get the process
            System.Diagnostics.Process proc;
            try
            {
                proc = System.Diagnostics.Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                // Process already exited
                byte[] gone = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Process already exited\"}");
                res.StatusCode = (int)HttpStatusCode.OK;
                res.ContentLength64 = gone.Length;
                await res.OutputStream.WriteAsync(gone);
                return;
            }

            // Safety check: block protected system processes
            if (ProtectedProcesses.Contains(proc.ProcessName))
            {
                res.StatusCode = (int)HttpStatusCode.Forbidden;
                byte[] blocked = Encoding.UTF8.GetBytes(
                    $"{{\"success\":false,\"error\":\"Cannot terminate protected system process '{proc.ProcessName}'\"}}");
                res.ContentLength64 = blocked.Length;
                await res.OutputStream.WriteAsync(blocked);
                return;
            }

            // Kill it
            proc.Kill(entireProcessTree: true);
            await proc.WaitForExitAsync(CancellationToken.None);

            byte[] ok = Encoding.UTF8.GetBytes(
                $"{{\"success\":true,\"message\":\"Process '{proc.ProcessName}' (PID {pid}) terminated\"}}");
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentLength64 = ok.Length;
            await res.OutputStream.WriteAsync(ok);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // Access denied — usually elevated/admin processes
            res.StatusCode = (int)HttpStatusCode.Forbidden;
            byte[] denied = Encoding.UTF8.GetBytes(
                $"{{\"success\":false,\"error\":\"Access denied: {ex.Message}. Run RAMView as Administrator to terminate elevated processes.\"}}");
            res.ContentLength64 = denied.Length;
            await res.OutputStream.WriteAsync(denied);
        }
        catch (InvalidOperationException)
        {
            // Process exited between our check and kill
            byte[] gone = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Process already exited\"}");
            res.StatusCode = (int)HttpStatusCode.OK;
            res.ContentLength64 = gone.Length;
            await res.OutputStream.WriteAsync(gone);
        }
        catch (Exception ex)
        {
            res.StatusCode = (int)HttpStatusCode.InternalServerError;
            byte[] err = Encoding.UTF8.GetBytes(
                $"{{\"success\":false,\"error\":\"{ex.Message.Replace("\"", "'")}\"}}");
            res.ContentLength64 = err.Length;
            await res.OutputStream.WriteAsync(err);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
