using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using ZXing.Common;

namespace Project.UcReception
{
    /// <summary>
    /// Self-contained camera panel that continuously scans for a QR code.
    /// Drop this control onto any form/panel. Wire up QrCodeDetected.
    ///
    /// NuGet packages required:
    ///   AForge.Video              2.2.5
    ///   AForge.Video.DirectShow   2.2.5
    ///   ZXing.Net                 0.16.9  (or ZXing.Net.Bindings.Windows.Compatibility)
    ///
    /// Install in Package Manager Console:
    ///   Install-Package AForge.Video
    ///   Install-Package AForge.Video.DirectShow
    ///   Install-Package ZXing.Net
    /// </summary>
    public sealed class QrCameraScanner : Panel, IDisposable
    {
        // ── Public API ────────────────────────────────────────────────
        /// <summary>Raised on the UI thread when a QR value is decoded.</summary>
        public event Action<string>? QrCodeDetected;

        /// <summary>
        /// Milliseconds to suppress repeated scans of the same code.
        /// Default 3 000 ms prevents double-firing on one scan.
        /// </summary>
        public int DeduplicateCooldownMs { get; set; } = 3000;

        // ── Internals ─────────────────────────────────────────────────
        private FilterInfoCollection?   _devices;
        private VideoCaptureDevice?     _camera;
        private readonly PictureBox     _preview;
        private readonly Label          _statusLabel;
        private readonly BarcodeReader  _reader;

        private string  _lastResult       = string.Empty;
        private DateTime _lastDetectedAt  = DateTime.MinValue;
        private bool    _disposed;

        public QrCameraScanner()
        {
            BackColor   = Color.FromArgb(7, 26, 14);   // WildNest dark green
            BorderStyle = BorderStyle.None;
            Padding     = new Padding(0);

            // Live preview fills the control
            _preview = new PictureBox
            {
                Dock        = DockStyle.Fill,
                SizeMode    = PictureBoxSizeMode.Zoom,
                BackColor   = Color.Black
            };

            // Subtle overlay label at bottom
            _statusLabel = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 32,
                Text      = "🎥  Initializing camera…",
                ForeColor = Color.FromArgb(212, 160, 23),   // WildNest amber
                BackColor = Color.FromArgb(180, 7, 26, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9f, FontStyle.Regular)
            };

            Controls.Add(_preview);
            Controls.Add(_statusLabel);

            // ZXing reader — QR only, try harder = fewer missed frames
            _reader = new BarcodeReader
            {
                AutoRotate = true,
                Options    = new DecodingOptions
                {
                    TryHarder      = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };
        }

        // ── Lifecycle ─────────────────────────────────────────────────

        /// <summary>Enumerate cameras and start the first one found.</summary>
        public void StartCamera(int deviceIndex = 0)
        {
            try
            {
                _devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_devices.Count == 0)
                {
                    SetStatus("⚠  No camera detected.");
                    return;
                }

                var deviceInfo = _devices[Math.Min(deviceIndex, _devices.Count - 1)];
                _camera = new VideoCaptureDevice(deviceInfo.MonikerString);
                _camera.NewFrame += OnNewFrame;
                _camera.Start();
                SetStatus("🎥  Point camera at guest QR code…");
            }
            catch (Exception ex)
            {
                SetStatus($"⚠  Camera error: {ex.Message}");
            }
        }

        /// <summary>Returns display names of all connected cameras.</summary>
        public static string[] GetAvailableCameras()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var names   = new string[devices.Count];
            for (int i = 0; i < devices.Count; i++) names[i] = devices[i].Name;
            return names;
        }

        public void StopCamera()
        {
            if (_camera != null && _camera.IsRunning)
            {
                _camera.SignalToStop();
                _camera.WaitForStop();
            }
        }

        // ── Frame processing ──────────────────────────────────────────

        private void OnNewFrame(object sender, NewFrameEventArgs e)
        {
            if (_disposed) return;

            // Clone the frame so AForge can reclaim it immediately
            Bitmap frame;
            try { frame = (Bitmap)e.Frame.Clone(); }
            catch { return; }

            // Update preview on UI thread (fire-and-forget, skip if busy)
            _preview.BeginInvoke(() =>
            {
                var old = _preview.Image;
                _preview.Image = frame;
                old?.Dispose();
            });

            // Decode QR in a thread-pool task so we never block the camera feed
            Task.Run(() => TryDecode(frame));
        }

        private void TryDecode(Bitmap frame)
        {
            try
            {
                var result = _reader.Decode(frame);
                frame.Dispose();

                if (result == null) return;

                string text = result.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(text)) return;

                // Deduplicate: ignore the same code within cooldown window
                if (string.Equals(text, _lastResult, StringComparison.Ordinal) &&
                    (DateTime.UtcNow - _lastDetectedAt).TotalMilliseconds < DeduplicateCooldownMs)
                    return;

                _lastResult     = text;
                _lastDetectedAt = DateTime.UtcNow;

                SetStatus($"✅  QR detected: {text}");

                // Raise on UI thread
                if (QrCodeDetected != null)
                    BeginInvoke(() => QrCodeDetected.Invoke(text));
            }
            catch
            {
                // Decode errors are expected on blurry frames — silently skip
                try { frame.Dispose(); } catch { /* ignore */ }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void SetStatus(string message)
        {
            try
            {
                if (_statusLabel.IsHandleCreated)
                    _statusLabel.BeginInvoke(() => _statusLabel.Text = message);
            }
            catch { /* control may be disposing */ }
        }

        // ── IDisposable ───────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                StopCamera();
                _camera?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
