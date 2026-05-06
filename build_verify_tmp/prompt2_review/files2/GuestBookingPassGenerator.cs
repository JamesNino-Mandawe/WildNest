using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;

namespace Project.Accomodations
{
    /// <summary>
    /// Generates a fully self-contained offline HTML Booking Pass for each guest.
    ///
    /// HOW IT WORKS (Option B — no server required):
    ///   1. At booking confirmation time, call Generate() with all booking details.
    ///   2. This class bakes EVERYTHING into a single .html file:
    ///        - Guest name, booking details, dates, amount
    ///        - The QR image as an embedded base64 data URL
    ///        - All CSS and JS inline — zero external dependencies
    ///   3. The file is saved to:
    ///        %AppData%\WildNest\BookingPasses\WN-2026-0001.html
    ///   4. GenerateQrBitmap() in EmailService encodes the file:// URL of this
    ///      HTML file into the QR code instead of just the booking ID.
    ///   5. When a guest scans their QR with any phone camera:
    ///        → Phone opens the HTML file directly
    ///        → Full premium Booking Pass loads instantly
    ///        → Works 100% offline — no internet, no server, no MySQL
    ///
    /// For sharing/emailing:
    ///   The HTML file is also attached to the confirmation email so the guest
    ///   can save it to their phone and open it any time.
    ///
    /// IMPORTANT: The file:// URL only works if the guest's phone can access
    /// the machine that generated the file (e.g. on the same local network,
    /// or if the file is transferred to the guest's device).
    ///
    /// For a demo environment (same machine or LAN), this is perfect.
    /// For production, host the files on a local web server or attach via email.
    /// </summary>
    public static class GuestBookingPassGenerator
    {
        // Output folder — roaming so it survives app updates
        private static readonly string OutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WildNest", "BookingPasses");

        /// <summary>
        /// Generates the self-contained HTML booking pass and returns the full file path.
        /// Throws on failure — caller should catch and log (never crash a booking for this).
        /// </summary>
        public static string Generate(
            string  bookingId,
            string  guestName,
            string  bookingType,
            string  cabin,
            string  dateInfo,
            decimal totalAmount,
            string  paymentMethod,
            string  status,
            int     guestCount,
            string  specialRequest,
            Bitmap  qrBitmap)
        {
            Directory.CreateDirectory(OutputFolder);

            // ── Convert QR bitmap to base64 PNG data URL ──────────────
            string qrDataUrl;
            using (var ms = new MemoryStream())
            {
                qrBitmap.Save(ms, ImageFormat.Png);
                qrDataUrl = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }

            // ── Sanitize all values for HTML injection ────────────────
            string safeId      = H(bookingId);
            string safeName    = H(guestName);
            string safeType    = H(bookingType);
            string safeCabin   = H(string.IsNullOrWhiteSpace(cabin) ? "Day Visit / Experience" : cabin);
            string safeDate    = H(dateInfo);
            string safeAmount  = $"₱{totalAmount:N2}";
            string safePayment = H(paymentMethod);
            string safeStatus  = H(status);
            string safeCount   = guestCount > 0 ? guestCount.ToString() : "—";
            string safeRequest = H(string.IsNullOrWhiteSpace(specialRequest) ? "None" : specialRequest);
            string initials    = GetInitials(guestName);
            string generated   = DateTime.Now.ToString("MMMM d, yyyy  h:mm tt");

            // Status badge colors
            (string sbg, string sfg) = GetStatusColors(status);

            // ── Build the complete self-contained HTML ────────────────
            string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<meta name=""theme-color"" content=""#071a0e"">
<title>WildNest Booking Pass — {safeId}</title>
<style>
/* ── Reset ──────────────────────────────────────────────── */
*,*::before,*::after{{box-sizing:border-box;margin:0;padding:0}}

/* ── Tokens ─────────────────────────────────────────────── */
:root{{
  --ink:#071a0e;--ink2:#0d2e18;--gold:#d4a017;--gold-lt:#e9c052;
  --sage:#8fb89a;--cream:#f8f4ef;--mist:#eef1ee;--white:#ffffff;
  --shadow:rgba(7,26,14,.22);
}}

html{{scroll-behavior:smooth}}
body{{
  font-family:'Segoe UI',system-ui,sans-serif;
  background:var(--ink);
  color:var(--cream);
  min-height:100dvh;
  overflow-x:hidden;
}}

/* ── Background atmosphere ──────────────────────────────── */
body::before{{
  content:'';position:fixed;inset:0;z-index:-2;
  background:
    radial-gradient(ellipse 65% 50% at 12% 8%, rgba(212,160,23,.10) 0%,transparent 55%),
    radial-gradient(ellipse 50% 65% at 90% 88%,rgba(143,184,154,.09) 0%,transparent 55%),
    radial-gradient(ellipse 100% 100% at 50% 50%,var(--ink) 0%,#030c05 100%);
}}
body::after{{
  content:'';position:fixed;inset:0;z-index:-1;pointer-events:none;
  background-image:url(""data:image/svg+xml,%3Csvg width='80' height='80' viewBox='0 0 80 80' xmlns='http://www.w3.org/2000/svg'%3E%3Ccircle cx='40' cy='40' r='1' fill='%238fb89a' opacity='.12'/%3E%3C/svg%3E"");
  background-size:80px 80px;
}}

/* ── Page ───────────────────────────────────────────────── */
.page{{
  display:flex;flex-direction:column;align-items:center;
  padding:44px 18px 72px;gap:0;
}}

/* ── Wordmark ───────────────────────────────────────────── */
.logo{{text-align:center;margin-bottom:36px;animation:slideDown .6s ease both}}
.logo .wm{{
  font-family:Georgia,serif;font-size:clamp(24px,6vw,38px);
  font-weight:700;letter-spacing:6px;color:var(--gold);line-height:1;
}}
.logo .sub{{
  font-size:10px;letter-spacing:2.5px;color:var(--sage);
  margin-top:6px;text-transform:uppercase;
}}

/* ── Boarding pass card ─────────────────────────────────── */
.pass{{
  width:100%;max-width:460px;background:var(--white);
  border-radius:20px;overflow:hidden;color:#1a2e20;
  box-shadow:0 40px 100px var(--shadow),0 0 0 1px rgba(212,160,23,.14);
  animation:riseUp .7s cubic-bezier(.22,1,.36,1) .1s both;
}}

/* ── Pass header ─────────────────────────────────────────── */
.pass-head{{
  background:var(--ink);padding:26px 28px 30px;position:relative;
}}
.pass-head::after{{
  content:'';position:absolute;bottom:-14px;left:0;right:0;
  height:28px;background:var(--white);border-radius:55% 55% 0 0/100%;
}}

/* ── Avatar + name row ──────────────────────────────────── */
.guest-row{{display:flex;align-items:center;gap:16px;}}
.avatar{{
  width:60px;height:60px;border-radius:50%;flex-shrink:0;
  background:linear-gradient(135deg,var(--ink2) 0%,#1a4028 100%);
  border:2px solid var(--gold);
  display:flex;align-items:center;justify-content:center;
  font-family:Georgia,serif;font-size:20px;font-weight:700;
  color:var(--gold);letter-spacing:1px;
}}
.guest-info{{flex:1;min-width:0;}}
.guest-name{{
  font-family:Georgia,serif;font-size:clamp(18px,4vw,24px);
  font-weight:600;color:var(--cream);line-height:1.2;
  white-space:nowrap;overflow:hidden;text-overflow:ellipsis;
}}
.booking-id{{
  font-family:'Courier New',monospace;font-size:12px;
  color:var(--gold-lt);margin-top:4px;letter-spacing:.5px;
}}

/* ── Status pill ────────────────────────────────────────── */
.status-pill{{
  display:inline-flex;align-items:center;gap:6px;
  background:{sbg};border:1px solid {sfg}44;
  border-radius:99px;padding:3px 12px 3px 8px;
  font-size:10px;font-weight:600;letter-spacing:1.4px;
  color:{sfg};text-transform:uppercase;margin-top:10px;
}}
.status-dot{{
  width:6px;height:6px;border-radius:50%;
  background:{sfg};flex-shrink:0;
}}

/* ── Pass body ──────────────────────────────────────────── */
.pass-body{{padding:34px 28px 22px;}}

/* ── Detail rows ────────────────────────────────────────── */
.detail-grid{{
  display:grid;grid-template-columns:1fr 1fr;
  gap:18px 14px;margin-bottom:22px;
}}
.d-item.full{{grid-column:1/-1;}}
.d-lbl{{
  font-size:9px;letter-spacing:1.8px;text-transform:uppercase;
  color:#999;margin-bottom:2px;
}}
.d-val{{
  font-size:13px;font-weight:500;color:#1a2e20;line-height:1.35;
}}
.d-val.amount{{font-size:16px;font-weight:700;color:#071a0e;}}

/* ── Perforated divider ─────────────────────────────────── */
.perf{{
  border:none;border-top:1.5px dashed #ddd;margin:4px 0 22px;
}}

/* ── QR block ───────────────────────────────────────────── */
.qr-block{{
  display:flex;flex-direction:column;align-items:center;gap:12px;
  padding:20px;background:var(--mist);border-radius:14px;margin-bottom:16px;
}}
.qr-block img{{
  width:152px;height:152px;border-radius:8px;
  border:3px solid var(--white);
  box-shadow:0 4px 18px rgba(0,0,0,.1);
  image-rendering:pixelated;display:block;
}}
.qr-note{{font-size:10.5px;color:#999;text-align:center;line-height:1.55;}}
.qr-note strong{{color:#555;}}

/* ── Download button ────────────────────────────────────── */
.btn-save{{
  display:flex;align-items:center;justify-content:center;gap:7px;
  width:100%;padding:12px 0;background:var(--ink);color:var(--gold);
  font-family:'Segoe UI',sans-serif;font-size:12px;font-weight:500;
  letter-spacing:1px;text-transform:uppercase;border:none;
  border-radius:10px;cursor:pointer;text-decoration:none;
  transition:background .18s,transform .12s;margin-bottom:2px;
}}
.btn-save:hover{{background:var(--ink2);transform:translateY(-1px);}}
.btn-save svg{{width:14px;height:14px;flex-shrink:0;}}

/* ── Pass footer ────────────────────────────────────────── */
.pass-foot{{
  padding:14px 28px 20px;font-size:10px;color:#bbb;
  text-align:center;line-height:1.65;border-top:1px solid #f0f0f0;
}}

/* ── Offline badge ──────────────────────────────────────── */
.offline-badge{{
  display:inline-flex;align-items:center;gap:5px;
  background:rgba(143,184,154,.12);border:1px solid rgba(143,184,154,.25);
  border-radius:99px;padding:3px 10px;font-size:9.5px;
  color:var(--sage);letter-spacing:1px;text-transform:uppercase;
  margin-bottom:28px;animation:fadeIn .8s ease .5s both;
}}

/* ── Generation timestamp ───────────────────────────────── */
.generated{{
  font-size:9px;color:#ccc;text-align:center;
  margin-top:20px;letter-spacing:.5px;opacity:.6;
  animation:fadeIn 1s ease 1.2s both;
}}

/* ── Tagline ────────────────────────────────────────────── */
.tagline{{
  margin-top:28px;font-family:Georgia,serif;font-style:italic;
  font-size:13.5px;color:var(--sage);opacity:0;
  animation:fadeIn 1s ease 1s forwards;
}}

/* ── Keyframes ──────────────────────────────────────────── */
@keyframes slideDown{{from{{opacity:0;transform:translateY(-12px)}}to{{opacity:1;transform:none}}}}
@keyframes riseUp{{from{{opacity:0;transform:translateY(24px)}}to{{opacity:1;transform:none}}}}
@keyframes fadeIn{{to{{opacity:.7}}}}

@media(max-width:380px){{
  .detail-grid{{grid-template-columns:1fr}}
  .pass-body,.pass-head{{padding-left:18px;padding-right:18px}}
}}
</style>
</head>
<body>
<div class=""page"">

  <div class=""logo"">
    <div class=""wm"">🦁 WILDNEST</div>
    <div class=""sub"">Zoo Resort &amp; Wildlife Experience · Carmen, Cebu</div>
  </div>

  <div class=""offline-badge"">
    <svg width=""9"" height=""9"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2.5"">
      <path d=""M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.24 12 19.79 19.79 0 0 1 1.17 3.41 2 2 0 0 1 3.14 1.22h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L7.09 8.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 21 15z""/>
    </svg>
    Works Offline · No Internet Required
  </div>

  <div class=""pass"" role=""main"" aria-label=""WildNest Booking Pass"">

    <!-- ── Header ── -->
    <div class=""pass-head"">
      <div class=""guest-row"">
        <div class=""avatar"" aria-hidden=""true"">{initials}</div>
        <div class=""guest-info"">
          <div class=""guest-name"">{safeName}</div>
          <div class=""booking-id"">{safeId}</div>
        </div>
      </div>
      <div class=""status-pill"">
        <span class=""status-dot"" aria-hidden=""true""></span>
        {safeStatus}
      </div>
    </div>

    <!-- ── Body ── -->
    <div class=""pass-body"">

      <div class=""detail-grid"">
        <div class=""d-item"">
          <div class=""d-lbl"">Booking Type</div>
          <div class=""d-val"">{safeType}</div>
        </div>
        <div class=""d-item"">
          <div class=""d-lbl"">Payment</div>
          <div class=""d-val"">{safePayment}</div>
        </div>
        <div class=""d-item full"">
          <div class=""d-lbl"">Date / Stay</div>
          <div class=""d-val"">{safeDate}</div>
        </div>
        <div class=""d-item full"">
          <div class=""d-lbl"">Accommodation</div>
          <div class=""d-val"">{safeCabin}</div>
        </div>
        <div class=""d-item"">
          <div class=""d-lbl"">Total Amount</div>
          <div class=""d-val amount"">{safeAmount}</div>
        </div>
        <div class=""d-item"">
          <div class=""d-lbl"">Guests</div>
          <div class=""d-val"">{safeCount}</div>
        </div>
        <div class=""d-item full"">
          <div class=""d-lbl"">Special Request</div>
          <div class=""d-val"">{safeRequest}</div>
        </div>
      </div>

      <hr class=""perf"" aria-hidden=""true"">

      <!-- ── QR ── -->
      <div class=""qr-block"">
        <img src=""{qrDataUrl}"" alt=""QR code for booking {safeId}"">
        <div class=""qr-note"">
          Present or scan at WildNest Reception.<br>
          Booking ID:&nbsp;<strong>{safeId}</strong>
        </div>
      </div>

      <!-- ── Save QR image ── -->
      <a class=""btn-save""
         href=""{qrDataUrl}""
         download=""WildNest_QR_{safeId}.png""
         title=""Save QR image"">
        <svg viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2""
             stroke-linecap=""round"" stroke-linejoin=""round"" aria-hidden=""true"">
          <path d=""M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4""/>
          <polyline points=""7 10 12 15 17 10""/><line x1=""12"" y1=""15"" x2=""12"" y2=""3""/>
        </svg>
        Save QR Image
      </a>

    </div>

    <div class=""pass-foot"">
      Present this pass at WildNest Reception upon arrival.<br>
      Questions?&nbsp;<strong>zoowildnest@gmail.com</strong>
    </div>

  </div>

  <p class=""tagline"">""Where nature and wonder meet.""</p>
  <p class=""generated"">Pass generated {generated}</p>

</div>
</body>
</html>";

            // ── Write file ────────────────────────────────────────────
            string fileName = $"{SanitizeFilename(bookingId)}.html";
            string filePath = Path.Combine(OutputFolder, fileName);
            File.WriteAllText(filePath, html, Encoding.UTF8);

            return filePath;
        }

        /// <summary>
        /// Returns the full file path for a booking pass if it already exists.
        /// Returns null if it has not been generated yet.
        /// </summary>
        public static string? GetExistingPassPath(string bookingId)
        {
            string path = Path.Combine(OutputFolder, $"{SanitizeFilename(bookingId)}.html");
            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// Opens the booking pass HTML file in the default system browser.
        /// Works on any OS — Windows, macOS, Linux.
        /// </summary>
        public static void OpenInBrowser(string filePath)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = filePath,
                UseShellExecute = true
            });
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string H(string? s) =>
            WebUtility.HtmlEncode(s ?? string.Empty);

        private static string SanitizeFilename(string name) =>
            string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : name[0].ToString().ToUpperInvariant();
        }

        private static (string bg, string fg) GetStatusColors(string status) => status switch
        {
            "Confirmed"    => ("rgba(232,245,233,1)",  "#1b5e20"),
            "Pending"      => ("rgba(255,243,224,1)",  "#e65100"),
            "Checked-In"   => ("rgba(227,242,253,1)",  "#1565c0"),
            "Overdue"      => ("rgba(255,235,238,1)",  "#b71c1c"),
            "Cancelled"    => ("rgba(245,245,245,1)",  "#616161"),
            "Checked-Out"  => ("rgba(245,245,245,1)",  "#616161"),
            _              => ("rgba(232,245,233,1)",  "#1b5e20")
        };
    }
}
