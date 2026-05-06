using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace Project.UcReception
{
    internal sealed class QrCameraScanner : Panel
    {
        private readonly PictureBox _preview;
        private readonly Label _statusLabel;
        private readonly Label _liveBadge;
        private readonly ZXing.Windows.Compatibility.BarcodeReader _reader;

        private FilterInfoCollection? _devices;
        private VideoCaptureDevice? _camera;
        private string _lastResult = string.Empty;
        private DateTime _lastDetectedAtUtc = DateTime.MinValue;
        private bool _starting;
        private bool _disposed;

        internal event Action<string>? QrCodeDetected;

        internal int DeduplicateCooldownMs { get; set; } = 3000;

        internal QrCameraScanner()
        {
            BackColor = Color.FromArgb(7, 26, 14);
            Padding = new Padding(0);

            _preview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            _liveBadge = new Label
            {
                Text = "CAMERA STANDBY",
                AutoSize = false,
                Size = new Size(132, 28),
                Location = new Point(14, 12),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = WildNestUI.FontLabel(8.2f),
                ForeColor = WildNestUI.Gold,
                BackColor = Color.FromArgb(188, 7, 26, 14)
            };
            _liveBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, _liveBadge.Width - 1, _liveBadge.Height - 1), 14);
                using var fill = new SolidBrush(_liveBadge.BackColor);
                using var border = new Pen(Color.FromArgb(64, WildNestUI.Gold), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    _liveBadge.Text,
                    _liveBadge.Font,
                    _liveBadge.ClientRectangle,
                    _liveBadge.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                Text = "Camera standby. Point the guest QR code inside the frame.",
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.FromArgb(204, 7, 26, 14),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _reader = new ZXing.Windows.Compatibility.BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PureBarcode = false,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };

            Controls.Add(_preview);
            Controls.Add(_liveBadge);
            Controls.Add(_statusLabel);
            Paint += PaintScannerChrome;
        }

        internal void StartCamera(int deviceIndex = 0)
        {
            if (_disposed || _starting)
                return;

            if (_camera != null && _camera.IsRunning)
            {
                SetStatus("Camera live. Scan the guest QR code.");
                return;
            }

            _starting = true;

            try
            {
                _devices ??= new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (_devices.Count == 0)
                {
                    SetLiveBadge("NO CAMERA", WildNestUI.Red);
                    SetStatus("No camera detected on this device.");
                    return;
                }

                int safeIndex = Math.Max(0, Math.Min(deviceIndex, _devices.Count - 1));
                _camera = new VideoCaptureDevice(_devices[safeIndex].MonikerString);
                _camera.NewFrame += OnNewFrame;
                _camera.Start();
                SetLiveBadge("SCANNER LIVE", WildNestUI.Green);
                SetStatus("Camera live. Scan the guest QR code.");
            }
            catch (Exception ex)
            {
                SetLiveBadge("CAMERA ERROR", WildNestUI.Red);
                SetStatus("Camera unavailable: " + ex.Message);
            }
            finally
            {
                _starting = false;
            }
        }

        internal void StopCamera()
        {
            if (_camera == null)
                return;

            try
            {
                _camera.NewFrame -= OnNewFrame;
                if (_camera.IsRunning)
                {
                    _camera.SignalToStop();
                    _camera.WaitForStop();
                }
            }
            catch
            {
                // Best-effort camera shutdown only.
            }
            finally
            {
                _camera = null;
                SetLiveBadge("CAMERA STANDBY", WildNestUI.Gold);
            }
        }

        internal void ShowRecognition(string guestOrReservationLabel)
        {
            string safeText = string.IsNullOrWhiteSpace(guestOrReservationLabel)
                ? "Recognizing reservation..."
                : guestOrReservationLabel.Trim();

            SetLiveBadge("RECOGNIZING", WildNestUI.Amber);
            SetStatus(safeText);
        }

        private void OnNewFrame(object sender, NewFrameEventArgs e)
        {
            if (_disposed)
                return;

            Bitmap decodeFrame;
            Bitmap previewFrame;
            try
            {
                decodeFrame = (Bitmap)e.Frame.Clone();
                previewFrame = (Bitmap)e.Frame.Clone();
            }
            catch
            {
                return;
            }

            if (_preview.IsHandleCreated)
            {
                _preview.BeginInvoke(new Action<Bitmap>(frameForUi =>
                {
                    var old = _preview.Image;
                    _preview.Image = frameForUi;
                    old?.Dispose();
                }), previewFrame);
            }
            else
            {
                previewFrame.Dispose();
            }

            Task.Run(() => TryDecode(decodeFrame));
        }

        private void TryDecode(Bitmap frame)
        {
            try
            {
                var result = DecodeBestEffort(frame);
                if (result == null)
                    return;

                string text = (result.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return;

                if (string.Equals(text, _lastResult, StringComparison.OrdinalIgnoreCase) &&
                    (DateTime.UtcNow - _lastDetectedAtUtc).TotalMilliseconds < DeduplicateCooldownMs)
                {
                    return;
                }

                _lastResult = text;
                _lastDetectedAtUtc = DateTime.UtcNow;
                SetLiveBadge("QR DETECTED", WildNestUI.Blue);
                SetStatus("QR detected. Recognizing guest reservation...");

                if (QrCodeDetected != null && IsHandleCreated)
                {
                    BeginInvoke(new Action(() => QrCodeDetected?.Invoke(text)));
                }
            }
            catch
            {
                // Blurry frames are normal; skip quietly.
            }
            finally
            {
                frame.Dispose();
            }
        }

        private Result? DecodeBestEffort(Bitmap frame)
        {
            var direct = _reader.Decode(frame);
            if (direct != null)
                return direct;

            foreach (double scale in new[] { 0.86, 0.72, 0.58 })
            {
                int cropW = (int)(frame.Width * scale);
                int cropH = (int)(frame.Height * scale);
                if (cropW < 120 || cropH < 120)
                    continue;

                int x = (frame.Width - cropW) / 2;
                int y = (frame.Height - cropH) / 2;
                Rectangle cropRect = new Rectangle(x, y, cropW, cropH);

                using var cropped = frame.Clone(cropRect, frame.PixelFormat);
                var croppedResult = _reader.Decode(cropped);
                if (croppedResult != null)
                    return croppedResult;
            }

            return null;
        }

        private void SetStatus(string text)
        {
            if (_statusLabel.IsHandleCreated)
            {
                _statusLabel.BeginInvoke(new Action(() => _statusLabel.Text = text));
            }
        }

        private void SetLiveBadge(string text, Color accent)
        {
            if (_liveBadge.IsHandleCreated)
            {
                _liveBadge.BeginInvoke(new Action(() =>
                {
                    _liveBadge.Text = text;
                    _liveBadge.ForeColor = accent;
                    _liveBadge.BackColor = Color.FromArgb(196, 7, 26, 14);
                    _liveBadge.Invalidate();
                }));
            }
        }

        private void PaintScannerChrome(object? sender, PaintEventArgs e)
        {
            if (Width < 40 || Height < 40)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var outer = WildNestUI.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 20);
            using var border = new Pen(Color.FromArgb(88, WildNestUI.Gold), 1.4f);
            e.Graphics.DrawPath(border, outer);

            Rectangle frame = new Rectangle(28, 24, Math.Max(120, Width - 56), Math.Max(90, Height - 88));
            frame.Height = Math.Max(90, frame.Height - _statusLabel.Height + 18);

            using var glass = new SolidBrush(Color.FromArgb(18, 255, 255, 255));
            using var framePath = WildNestUI.RoundRect(frame, 18);
            e.Graphics.FillPath(glass, framePath);

            DrawCorner(e.Graphics, frame.Left, frame.Top, true, true);
            DrawCorner(e.Graphics, frame.Right, frame.Top, false, true);
            DrawCorner(e.Graphics, frame.Left, frame.Bottom, true, false);
            DrawCorner(e.Graphics, frame.Right, frame.Bottom, false, false);

            var cueRect = new Rectangle(frame.Left + 16, frame.Bottom - 38, frame.Width - 32, 20);
            TextRenderer.DrawText(
                e.Graphics,
                "Align the guest QR code inside the live frame",
                WildNestUI.FontBody(8.8f),
                cueRect,
                Color.FromArgb(220, WildNestUI.Cream),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void DrawCorner(Graphics g, int x, int y, bool left, bool top)
        {
            const int len = 24;
            const int inset = 2;
            using var pen = new Pen(WildNestUI.Gold, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            int hx1 = left ? x + inset : x - inset;
            int hx2 = left ? x + len : x - len;
            int vy1 = top ? y + inset : y - inset;
            int vy2 = top ? y + len : y - len;

            g.DrawLine(pen, hx1, y, hx2, y);
            g.DrawLine(pen, x, vy1, x, vy2);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
                StopCamera();
                var old = _preview.Image;
                _preview.Image = null;
                old?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
