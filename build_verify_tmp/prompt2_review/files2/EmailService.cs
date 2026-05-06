using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using QRCoder;

namespace Project.Accomodations
{
    /// <summary>
    /// Sends booking confirmation emails with QR code and HTML Booking Pass attached.
    /// Uses MailKit + QRCoder NuGet packages.
    ///
    /// UPGRADE — Option B (offline self-contained QR landing):
    ///   GenerateQrBitmapForPass() now encodes the file:// URL of the guest's
    ///   self-contained HTML Booking Pass instead of just the booking ID.
    ///   When a guest scans the QR on any phone camera, the phone opens the
    ///   full premium Booking Pass page — no internet, no server required.
    ///
    ///   The HTML pass file is also attached to the confirmation email so the
    ///   guest can save it to their phone and open it any time, anywhere.
    ///
    /// SMTP SETUP:
    ///   Gmail  : host=smtp.gmail.com, port=587, use an App Password
    ///   Outlook: host=smtp.office365.com, port=587
    ///   Yahoo  : host=smtp.mail.yahoo.com, port=587
    /// </summary>
    public static class EmailService
    {
        // ── SMTP credentials ──────────────────────────────────────────
        const string DEFAULT_SMTP_HOST = "smtp.gmail.com";
        const int    DEFAULT_SMTP_PORT = 587;
        const string DEFAULT_SMTP_USER = "zoowildnest@gmail.com";
        const string DEFAULT_SMTP_PASS = "vwdiyofzqrxetkoh";
        const string DEFAULT_SENDER_NAME = "WildNest Zoo Resort";

        static string SmtpHost   => ReadSetting("WILDNEST_SMTP_HOST",   DEFAULT_SMTP_HOST);
        static int    SmtpPort   => int.TryParse(ReadSetting("WILDNEST_SMTP_PORT", DEFAULT_SMTP_PORT.ToString()), out int p) ? p : DEFAULT_SMTP_PORT;
        static string SmtpUser   => ReadSetting("WILDNEST_SMTP_USER",   DEFAULT_SMTP_USER);
        static string SmtpPass   => ReadSetting("WILDNEST_SMTP_PASS",   DEFAULT_SMTP_PASS);
        static string SenderName => ReadSetting("WILDNEST_SENDER_NAME", DEFAULT_SENDER_NAME);

        static string ReadSetting(string key, string fallback)
        {
            string? v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(v) ? fallback : v.Trim();
        }

        static void EnsureSmtpReady()
        {
            if (string.IsNullOrWhiteSpace(SmtpHost) ||
                string.IsNullOrWhiteSpace(SmtpUser) ||
                string.IsNullOrWhiteSpace(SmtpPass))
                throw new InvalidOperationException(
                    "Email settings are incomplete. Configure WILDNEST_SMTP_HOST, " +
                    "WILDNEST_SMTP_USER, and WILDNEST_SMTP_PASS.");
        }

        // ─────────────────────────────────────────────────────────────
        // QR Generation — TWO methods, clear contract
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// ORIGINAL — encodes just the booking ID string.
        /// Used by: reception camera scanner (ZXing reads the ID directly).
        /// Preserved exactly — do NOT change this method.
        /// </summary>
        public static Bitmap GenerateQrBitmap(string bookingId)
        {
            using var gen  = new QRCodeGenerator();
            using var data = gen.CreateQrCode(bookingId, QRCodeGenerator.ECCLevel.Q);
            using var code = new QRCode(data);
            return code.GetGraphic(6, Color.Black, Color.White, true);
        }

        /// <summary>
        /// NEW — encodes the file:// URL of the guest's self-contained HTML pass.
        /// Used by: confirmation email QR and printed/displayed QR for guests.
        ///
        /// When scanned on a phone:
        ///   → Phone camera opens the URL
        ///   → Full premium Booking Pass loads from the local HTML file
        ///   → Works 100% offline — no internet, no server
        ///
        /// Falls back to GenerateQrBitmap(bookingId) if the HTML file
        /// does not exist yet (e.g. generation failed).
        /// </summary>
        public static Bitmap GenerateQrBitmapForPass(string bookingId, string htmlPassFilePath)
        {
            string payload = string.IsNullOrWhiteSpace(htmlPassFilePath) || !File.Exists(htmlPassFilePath)
                ? bookingId                                         // fallback: plain ID
                : new Uri(htmlPassFilePath).AbsoluteUri;           // file:///C:/Users/.../WN-2026-0001.html

            using var gen  = new QRCodeGenerator();
            using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var code = new QRCode(data);
            return code.GetGraphic(6, Color.Black, Color.White, true);
        }

        // ─────────────────────────────────────────────────────────────
        // Confirmation email
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends booking confirmation email.
        ///   - QR PNG attached (inline in email + downloadable)
        ///   - Self-contained HTML Booking Pass attached (optional — if path provided)
        ///   - Call this right after ConfirmBooking() succeeds
        /// </summary>
        public static EmailVerificationResult SendConfirmation(
            string  toEmail,
            string  guestName,
            string  bookingId,
            string  bookingType,
            string  dateInfo,
            decimal totalAmount,
            string  paymentMethod,
            Bitmap  qrBitmap,
            string? htmlPassFilePath = null)   // ← NEW optional param — attach the HTML pass
        {
            try
            {
                EnsureSmtpReady();

                // Convert QR bitmap to bytes
                byte[] qrBytes;
                using (var ms = new MemoryStream())
                {
                    qrBitmap.Save(ms, ImageFormat.Png);
                    qrBytes = ms.ToArray();
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SmtpUser));
                message.To.Add(new MailboxAddress(guestName, toEmail));
                message.Subject = $"WildNest Booking Confirmed — {bookingId}";

                var builder = new BodyBuilder();

                builder.HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f4f4f4;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:30px 0;'>
    <tr><td align='center'>
      <table width='520' cellpadding='0' cellspacing='0'
             style='background:#ffffff;border-radius:12px;overflow:hidden;
                    box-shadow:0 2px 12px rgba(0,0,0,0.1);'>

        <!-- Header -->
        <tr>
          <td style='background:#071a0e;padding:32px 40px;text-align:center;'>
            <div style='font-size:28px;font-weight:bold;color:#d4a017;letter-spacing:3px;'>
              🦁 WILDNEST
            </div>
            <div style='color:#8fb89a;font-size:13px;margin-top:6px;'>
              Zoo Resort &amp; Wildlife Experience · Carmen, Cebu
            </div>
          </td>
        </tr>

        <!-- Success badge -->
        <tr>
          <td style='background:#e8f5e9;padding:20px 40px;text-align:center;
                     border-bottom:1px solid #c8e6c9;'>
            <div style='font-size:36px;'>✅</div>
            <div style='font-size:20px;font-weight:bold;color:#1b5e20;margin-top:8px;'>
              Booking Confirmed!
            </div>
            <div style='color:#388e3c;font-size:13px;margin-top:4px;'>
              Your reservation has been successfully processed.
            </div>
          </td>
        </tr>

        <!-- Body -->
        <tr>
          <td style='padding:30px 40px;'>
            <p style='color:#333;font-size:15px;'>
              Dear <strong>{WebUtility.HtmlEncode(guestName)}</strong>,
            </p>
            <p style='color:#555;font-size:14px;line-height:1.6;'>
              Thank you for choosing WildNest! Your booking is confirmed.
              Find your Booking ID and QR Code below — present them at Reception upon arrival.
            </p>

            <!-- Booking ID box -->
            <table width='100%' cellpadding='0' cellspacing='0'
                   style='background:#071a0e;border-radius:10px;margin:20px 0;'>
              <tr>
                <td style='padding:18px 24px;'>
                  <div style='color:rgba(248,244,239,0.5);font-size:11px;
                              font-weight:bold;letter-spacing:2px;'>
                    YOUR BOOKING ID
                  </div>
                  <div style='color:#d4a017;font-size:24px;font-weight:bold;
                              font-family:Georgia,serif;margin-top:4px;'>
                    {WebUtility.HtmlEncode(bookingId)}
                  </div>
                </td>
              </tr>
            </table>

            <!-- Details table -->
            <table width='100%' cellpadding='8' cellspacing='0'
                   style='border:1px solid #e0e0e0;border-radius:8px;font-size:13px;'>
              <tr style='background:#f9f9f9;'>
                <td style='color:#888;font-weight:bold;padding:10px 16px;width:40%;'>Booking Type</td>
                <td style='color:#333;padding:10px 16px;'>{WebUtility.HtmlEncode(bookingType)}</td>
              </tr>
              <tr>
                <td style='color:#888;font-weight:bold;padding:10px 16px;'>Date</td>
                <td style='color:#333;padding:10px 16px;'>{WebUtility.HtmlEncode(dateInfo)}</td>
              </tr>
              <tr style='background:#f9f9f9;'>
                <td style='color:#888;font-weight:bold;padding:10px 16px;'>Total Amount</td>
                <td style='color:#333;font-weight:bold;padding:10px 16px;'>
                  ₱{totalAmount:N2}
                </td>
              </tr>
              <tr>
                <td style='color:#888;font-weight:bold;padding:10px 16px;'>Payment Method</td>
                <td style='color:#333;padding:10px 16px;'>{WebUtility.HtmlEncode(paymentMethod)}</td>
              </tr>
            </table>

            <!-- QR section -->
            <div style='text-align:center;margin:28px 0 16px;'>
              <div style='font-size:13px;color:#555;margin-bottom:12px;'>
                📱 <strong>Your QR Code</strong> — Scan to open your Booking Pass
                or present at Reception for check-in
              </div>
              <img src='cid:qrcode' width='180' height='180'
                   style='border:1px solid #ddd;border-radius:8px;padding:8px;background:#fff;'
                   alt='QR Code'/>
              <div style='font-size:11px;color:#999;margin-top:8px;'>
                QR Code for Booking {WebUtility.HtmlEncode(bookingId)}
              </div>
            </div>

            <!-- Booking pass note -->
            {(htmlPassFilePath != null ? @"
            <div style='background:#f0f7ff;border-left:4px solid #1565c0;border-radius:6px;
                        padding:14px 18px;margin:20px 0;font-size:13px;color:#1a237e;'>
              <strong>📎 Your Booking Pass is attached!</strong><br/>
              Open the attached <em>WildNest_BookingPass_" + WebUtility.HtmlEncode(bookingId) + @".html</em> file
              on your phone or computer for a full interactive Booking Pass —
              works completely offline, no internet needed.
            </div>" : @"
            <div style='background:#f0f7ff;border-left:4px solid #1565c0;border-radius:6px;
                        padding:14px 18px;margin:20px 0;font-size:13px;color:#1a237e;'>
              <strong>How to check in:</strong><br/>
              Option 1 — Show your Booking ID <strong>" + WebUtility.HtmlEncode(bookingId) + @"</strong> at Reception<br/>
              Option 2 — Scan the QR code attached above
            </div>")}
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background:#071a0e;padding:20px 40px;text-align:center;'>
            <div style='color:#8fb89a;font-size:12px;'>
              WildNest Zoo Resort · Carmen, Cebu, Philippines
            </div>
            <div style='color:#4a6741;font-size:11px;margin-top:4px;'>
              This is an automated confirmation email.
            </div>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

                // ── Attach QR as inline image + downloadable ──────────
                var qrInlineStream = new MemoryStream(qrBytes);
                var inlineImg      = builder.LinkedResources.Add(
                    "qrcode.png", qrInlineStream, new ContentType("image", "png"));
                inlineImg.ContentId          = "qrcode";
                inlineImg.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);

                var qrAttachStream = new MemoryStream(qrBytes);
                builder.Attachments.Add(
                    $"WildNest_QR_{bookingId}.png", qrAttachStream,
                    new ContentType("image", "png"));

                // ── Attach self-contained HTML Booking Pass ───────────
                if (!string.IsNullOrWhiteSpace(htmlPassFilePath) &&
                    File.Exists(htmlPassFilePath))
                {
                    byte[] htmlBytes = File.ReadAllBytes(htmlPassFilePath);
                    var htmlStream   = new MemoryStream(htmlBytes);
                    builder.Attachments.Add(
                        $"WildNest_BookingPass_{bookingId}.html",
                        htmlStream,
                        new ContentType("text", "html"));
                }

                message.Body = builder.ToMessageBody();

                // ── Send ──────────────────────────────────────────────
                using var client = new SmtpClient();
                client.Connect(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                client.Authenticate(SmtpUser, SmtpPass);
                client.Send(message);
                client.Disconnect(true);

                ProjectDiagnostics.LogInfo("EmailService",
                    $"Confirmation email sent for booking {bookingId} to {toEmail}.");
                return new EmailVerificationResult(true, "Confirmation email sent.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("EmailService", ex,
                    $"Confirmation email failed for booking {bookingId}");
                return new EmailVerificationResult(false, ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Verification code email — UNCHANGED
        // ─────────────────────────────────────────────────────────────

        internal static EmailVerificationResult SendVerificationCode(
            string toEmail,
            string guestName,
            string verificationCode,
            int    validMinutes)
        {
            try
            {
                EnsureSmtpReady();
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SmtpUser));
                message.To.Add(new MailboxAddress(guestName, toEmail));
                message.Subject = "WildNest Verification Code";

                var safeGuestName  = WebUtility.HtmlEncode(
                    string.IsNullOrWhiteSpace(guestName) ? "WildNest Guest" : guestName.Trim());
                var safeCode       = WebUtility.HtmlEncode(verificationCode);
                var validityText   = validMinutes <= 1 ? "60 seconds" : $"{validMinutes} minutes";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:24px;background:#0b0f0d;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0'
             style='background:#111713;border:1px solid #edf0ed;border-radius:18px;
                    overflow:hidden;box-shadow:0 18px 50px rgba(0,0,0,0.35);'>
        <tr>
          <td style='background:#071a0e;padding:30px 40px;
                     border-bottom:1px solid rgba(212,160,23,0.45);'>
            <div style='font-size:30px;font-weight:800;color:#d4a017;letter-spacing:4px;'>
              WILDNEST
            </div>
            <div style='color:#d9ded8;font-size:18px;margin-top:12px;'>
              Email Verification
            </div>
            <div style='color:#8da594;font-size:12px;margin-top:6px;'>
              Required before a booking can be confirmed
            </div>
          </td>
        </tr>
        <tr>
          <td style='padding:34px 40px;color:#edf0ed;'>
            <p style='margin:0 0 22px 0;font-size:16px;'>
              Hello <strong>{safeGuestName}</strong>,
            </p>
            <p style='line-height:1.7;color:#c9d2cb;font-size:14px;margin:0;'>
              Your verification code is:
            </p>
            <div style='margin:28px 0;padding:24px;border-radius:16px;
                        background:#4d4c54;border:2px solid #d4a017;text-align:center;'>
              <div style='color:#e9c052;font-size:42px;font-weight:800;letter-spacing:14px;'>
                {safeCode}
              </div>
            </div>
            <p style='font-size:16px;line-height:1.7;color:#edf0ed;margin:0 0 22px 0;'>
              This code expires in <strong>{validityText}</strong>.
            </p>
            <div style='background:#18251d;border-left:4px solid #d4a017;border-radius:10px;
                        padding:14px 16px;font-size:13px;color:#b9c8bd;line-height:1.6;'>
              If you did not request this, ignore this email. No WildNest booking
              will be confirmed unless the correct code is entered in the booking form.
            </div>
          </td>
        </tr>
        <tr>
          <td style='background:#071a0e;padding:18px 40px;text-align:center;
                     color:#738a79;font-size:11px;'>
            WildNest Zoo Resort — secure booking verification
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>"
                };

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                client.Connect(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                client.Authenticate(SmtpUser, SmtpPass);
                client.Send(message);
                client.Disconnect(true);
                return new EmailVerificationResult(true, "Verification code sent.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("EmailService", ex,
                    $"Verification email failed for {toEmail}");
                return new EmailVerificationResult(false, ex.Message);
            }
        }
    }
}
