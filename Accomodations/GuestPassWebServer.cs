using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Project.Accomodations
{
    internal static class GuestPassWebServer
    {
        private const int Port = 5078;
        private static readonly object Sync = new();
        private static CancellationTokenSource? _cts;
        private static TcpListener? _listener;
        private static bool _started;

        internal static void Start()
        {
            lock (Sync)
            {
                if (_started)
                    return;

                StartCoreNoLock();
            }
        }

        internal static void Stop()
        {
            lock (Sync)
            {
                try
                {
                    _cts?.Cancel();
                    _listener?.Stop();
                }
                catch
                {
                    // Best-effort shutdown only.
                }
                finally
                {
                    _started = false;
                    _listener = null;
                    _cts = null;
                }
            }
        }

        internal static string? GetPublicPassUrl(string reservationId)
        {
            if (!EnsureRunning())
            {
                ProjectDiagnostics.LogWarning("GuestPassWebServer", $"Guest pass server unavailable while generating URL for {reservationId}.");
                return null;
            }

            if (!TryGetLanAddress(out string host))
                return null;

            return $"http://{host}:{Port}/pass/{Uri.EscapeDataString(reservationId)}";
        }

        internal static bool EnsureRunning()
        {
            lock (Sync)
            {
                if (_started && IsHealthyNoLock())
                    return true;

                if (_started)
                    StopCoreNoLock();

                StartCoreNoLock();
                return _started && IsHealthyNoLock();
            }
        }

        private static async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null)
            {
                TcpClient? client = null;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProjectDiagnostics.LogError("GuestPassWebServer", ex, "Guest pass listener accept loop failed.");
                    client?.Dispose();
                }
            }
        }

        private static void StartCoreNoLock()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                _started = true;
                _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
                ProjectDiagnostics.LogInfo("GuestPassWebServer", $"Guest pass server listening on port {Port}.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("GuestPassWebServer", ex, "Guest pass web server failed to start.");
                _started = false;
                _listener = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private static void StopCoreNoLock()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
            }
            catch
            {
                // Best-effort shutdown only.
            }
            finally
            {
                _started = false;
                _listener = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private static bool IsHealthyNoLock()
        {
            try
            {
                using var probe = new TcpClient();
                var connectTask = probe.ConnectAsync(IPAddress.Loopback, Port);
                if (!connectTask.Wait(600))
                    return false;

                return probe.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using var clientScope = client;
            try
            {
                using NetworkStream stream = clientScope.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 2048, leaveOpen: true);

                string? requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(requestLine))
                    return;

                string[] parts = requestLine.Split(' ');
                if (parts.Length < 2)
                {
                    await WriteResponseAsync(stream, "400 Bad Request", "text/plain; charset=utf-8", "Bad Request", token);
                    return;
                }

                string method = parts[0];
                string path = Uri.UnescapeDataString(parts[1]);

                while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
                {
                    // Drain headers only.
                }

                if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, "405 Method Not Allowed", "text/plain; charset=utf-8", "Method Not Allowed", token);
                    return;
                }

                if (string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, "200 OK", "text/plain; charset=utf-8", "WildNest Guest Pass Server", token);
                    return;
                }

                if (!path.StartsWith("/pass/", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteResponseAsync(stream, "404 Not Found", "text/plain; charset=utf-8", "Pass not found", token);
                    return;
                }

                string reservationId = path["/pass/".Length..].Trim('/');
                if (string.IsNullOrWhiteSpace(reservationId) || reservationId.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '-')))
                {
                    await WriteResponseAsync(stream, "400 Bad Request", "text/plain; charset=utf-8", "Invalid reservation reference", token);
                    return;
                }

                string filePath = Path.Combine(GetPassFolder(), reservationId + ".html");
                if (!File.Exists(filePath))
                {
                    await WriteResponseAsync(stream, "404 Not Found", "text/html; charset=utf-8", BuildNotFoundHtml(reservationId), token);
                    return;
                }

                byte[] htmlBytes = await File.ReadAllBytesAsync(filePath, token);
                await WriteBytesAsync(stream, "200 OK", "text/html; charset=utf-8", htmlBytes, token);
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("GuestPassWebServer", ex, "Guest pass request failed.");
            }
        }

        private static async Task WriteResponseAsync(NetworkStream stream, string status, string contentType, string body, CancellationToken token)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            await WriteBytesAsync(stream, status, contentType, bodyBytes, token);
        }

        private static async Task WriteBytesAsync(NetworkStream stream, string status, string contentType, byte[] bodyBytes, CancellationToken token)
        {
            string header =
                $"HTTP/1.1 {status}\r\n" +
                $"Content-Type: {contentType}\r\n" +
                $"Content-Length: {bodyBytes.Length}\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";

            byte[] headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, token);
            await stream.WriteAsync(bodyBytes, token);
            await stream.FlushAsync(token);
        }

        private static bool TryGetLanAddress(out string host)
        {
            host = string.Empty;

            try
            {
                foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                        continue;

                    string ip = address.ToString();
                    if (ip.StartsWith("10.") ||
                        ip.StartsWith("192.168.") ||
                        ip.StartsWith("172.16.") || ip.StartsWith("172.17.") || ip.StartsWith("172.18.") ||
                        ip.StartsWith("172.19.") || ip.StartsWith("172.20.") || ip.StartsWith("172.21.") ||
                        ip.StartsWith("172.22.") || ip.StartsWith("172.23.") || ip.StartsWith("172.24.") ||
                        ip.StartsWith("172.25.") || ip.StartsWith("172.26.") || ip.StartsWith("172.27.") ||
                        ip.StartsWith("172.28.") || ip.StartsWith("172.29.") || ip.StartsWith("172.30.") ||
                        ip.StartsWith("172.31."))
                    {
                        host = ip;
                        return true;
                    }
                }
            }
            catch
            {
                // Fallback to null host.
            }

            return false;
        }

        private static string GetPassFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WildNest",
                "BookingPasses");
        }

        private static string BuildNotFoundHtml(string reservationId)
        {
            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
  <title>WildNest Pass Unavailable</title>
  <style>
    body{{margin:0;font-family:Segoe UI,Arial,sans-serif;background:#f7f3ec;color:#18251d;display:flex;align-items:center;justify-content:center;min-height:100vh;padding:24px}}
    .card{{max-width:560px;background:#fffdfa;border:1px solid #e4d8c6;border-radius:24px;padding:32px;box-shadow:0 20px 48px rgba(7,26,14,.12)}}
    h1{{margin:0 0 10px;font:700 2rem Georgia,serif;color:#071a0e}}
    p{{margin:0;line-height:1.7;color:#5f655f}}
    strong{{color:#8a6010}}
  </style>
</head>
<body>
  <div class='card'>
    <h1>Booking Pass Unavailable</h1>
    <p>The pass for <strong>{WebUtility.HtmlEncode(reservationId)}</strong> is not currently available on this device. Keep the reservation reference ready and present your confirmation email at reception.</p>
  </div>
</body>
</html>";
        }
    }
}
