using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    /// <summary>
    /// Opens wildnest_booking_pass.html in a WebView2 popup and injects
    /// live booking data from the database.
    ///
    /// NuGet required:
    ///   Microsoft.Web.WebView2  (latest stable)
    ///   Install-Package Microsoft.Web.WebView2
    ///
    /// Place wildnest_booking_pass.html in the project root and set:
    ///   Build Action              → Content
    ///   Copy to Output Directory  → Copy if newer
    /// </summary>
    public static class BookingPassViewer
    {
        public static void Show(IWin32Window owner, string reservationId, Bitmap? qrBitmap = null)
        {
            // ── 1. Fetch booking row — fully parameterized ────────────
            var row = StaffPortalDb.GetTable(
                @"SELECT r.ReservationID, r.BookingType, r.Status,
                         r.CheckInDate, r.CheckOutDate, r.VisitDate,
                         r.TotalAmount, r.PaymentMethod,
                         CONCAT(g.FirstName,' ',g.LastName) AS GuestName,
                         COALESCE(c.CabinName,'Day Visit / Experience') AS Cabin
                  FROM   tbl_reservations r
                  LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
                  LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
                  WHERE  r.ReservationID = @id
                  LIMIT  1;",
                new MySqlParameter("@id", reservationId));

            if (row == null || row.Rows.Count == 0)
            {
                MessageBox.Show(
                    $"Booking not found: {reservationId}",
                    "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dr = row.Rows[0];

            string guestName     = dr["GuestName"]?.ToString()     ?? "Guest";
            string bookingType   = dr["BookingType"]?.ToString()   ?? string.Empty;
            string cabin         = dr["Cabin"]?.ToString()         ?? string.Empty;
            string status        = dr["Status"]?.ToString()        ?? "Confirmed";
            string paymentMethod = dr["PaymentMethod"]?.ToString() ?? string.Empty;
            decimal totalAmount  = Convert.ToDecimal(dr["TotalAmount"] ?? 0m);
            string dateInfo      = BuildDateInfo(dr, bookingType);

            // ── 2. QR bitmap → base64 data URL ────────────────────────
            bool ownedQr  = qrBitmap == null;
            Bitmap qr     = qrBitmap ?? EmailService.GenerateQrBitmap(reservationId);
            string qrDataUrl;
            try
            {
                using var ms = new MemoryStream();
                qr.Save(ms, ImageFormat.Png);
                qrDataUrl = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
            finally
            {
                // Only dispose if we generated it; caller owns theirs
                if (ownedQr) qr.Dispose();
            }

            // ── 3. Serialize payload for HTML injection ───────────────
            var payload = new
            {
                bookingId     = reservationId,
                guestName,
                bookingType,
                cabin,
                dateInfo,
                totalAmount,
                paymentMethod,
                status,
                qrDataUrl
            };
            string json = JsonSerializer.Serialize(payload,
                new JsonSerializerOptions { WriteIndented = false });

            // ── 4. Locate HTML file ───────────────────────────────────
            string htmlPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "wildnest_booking_pass.html");

            if (!File.Exists(htmlPath))
            {
                MessageBox.Show(
                    $"Booking pass page not found:\n{htmlPath}\n\n" +
                    "Set wildnest_booking_pass.html → Copy to Output Directory: Copy if newer.",
                    "WildNest Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── 5. Open WebView2 popup ────────────────────────────────
            var form = new Form
            {
                Text          = $"WildNest Booking Pass — {reservationId}",
                Size          = new Size(560, 800),
                MinimumSize   = new Size(420, 640),
                StartPosition = FormStartPosition.CenterParent,
                BackColor     = Color.FromArgb(7, 26, 14),
                Icon          = SystemIcons.Information
            };

            var webView = new WebView2 { Dock = DockStyle.Fill };
            form.Controls.Add(webView);

            form.Load += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async(null);

                    // Inject data before document script runs — correct WebView2 pattern
                    await webView.CoreWebView2
                        .AddScriptToExecuteOnDocumentCreatedAsync(
                            $"window.WILDNEST_BOOKING = {json};");

                    webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "WebView2 initialization failed:\n" + ex.Message + "\n\n" +
                        "Download the WebView2 Runtime from:\n" +
                        "https://developer.microsoft.com/microsoft-edge/webview2",
                        "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    form.Close();   // No orphaned window
                }
            };

            form.Show(owner);
        }

        // ─────────────────────────────────────────────────────────────

        private static string BuildDateInfo(System.Data.DataRow dr, string bookingType)
        {
            try
            {
                bool isOvernight =
                    bookingType.Contains("Overnight", StringComparison.OrdinalIgnoreCase) ||
                    bookingType.Contains("Stay",      StringComparison.OrdinalIgnoreCase);

                if (isOvernight)
                {
                    var ci = Convert.ToDateTime(dr["CheckInDate"]);
                    var co = Convert.ToDateTime(dr["CheckOutDate"]);
                    return $"{ci:MMMM d} – {co:MMMM d, yyyy}";
                }

                var visit = Convert.ToDateTime(dr["VisitDate"]);
                return visit.ToString("MMMM d, yyyy");
            }
            catch { return string.Empty; }
        }
    }
}
