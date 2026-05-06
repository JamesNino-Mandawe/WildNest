using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using QRCoder;

namespace Project.Accomodations
{
    public static class EmailService
    {
        const string DEFAULT_SMTP_HOST = "smtp.gmail.com";
        const int DEFAULT_SMTP_PORT = 587;
        const string DEFAULT_SMTP_USER = "zoowildnest@gmail.com";
        const string DEFAULT_SMTP_PASS = "vwdiyofzqrxetkoh";
        const string DEFAULT_SENDER_NAME = "WildNest Zoo Resort";

        static string SmtpHost => ReadSetting("WILDNEST_SMTP_HOST", DEFAULT_SMTP_HOST);
        static int SmtpPort => int.TryParse(ReadSetting("WILDNEST_SMTP_PORT", DEFAULT_SMTP_PORT.ToString()), out int port) ? port : DEFAULT_SMTP_PORT;
        static string SmtpUser => ReadSetting("WILDNEST_SMTP_USER", DEFAULT_SMTP_USER);
        static string SmtpPass => ReadSetting("WILDNEST_SMTP_PASS", DEFAULT_SMTP_PASS);
        static string SenderName => ReadSetting("WILDNEST_SENDER_NAME", DEFAULT_SENDER_NAME);

        static string ReadSetting(string key, string fallback)
        {
            string? value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        static void EnsureSmtpReady()
        {
            if (string.IsNullOrWhiteSpace(SmtpHost) ||
                string.IsNullOrWhiteSpace(SmtpUser) ||
                string.IsNullOrWhiteSpace(SmtpPass))
            {
                throw new InvalidOperationException(
                    "Email settings are incomplete. Configure WILDNEST_SMTP_HOST, WILDNEST_SMTP_USER, and WILDNEST_SMTP_PASS.");
            }
        }

        public static Bitmap GenerateQrBitmap(string bookingId)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(bookingId, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new QRCode(qrData);
            return qrCode.GetGraphic(14, Color.Black, Color.White, true);
        }

        public static EmailVerificationResult SendConfirmation(
            string toEmail,
            string guestName,
            string bookingId,
            string bookingType,
            string dateInfo,
            decimal totalAmount,
            string paymentMethod,
            Bitmap qrBitmap,
            string? bookingPassFilePath = null,
            string? guestPassUrl = null)
        {
            try
            {
                EnsureSmtpReady();

                if (string.IsNullOrWhiteSpace(bookingPassFilePath))
                {
                    bookingPassFilePath = GuestBookingPassGenerator.Generate(
                        new GuestBookingPassPayload
                        {
                            BookingId = bookingId,
                            GuestName = guestName,
                            BookingType = bookingType,
                            DateLabel = dateInfo,
                            TotalAmountLabel = $"PHP {totalAmount:N2}",
                            PaymentMethod = paymentMethod,
                            GuestCountLabel = "See reservation details",
                            AccommodationLabel = bookingType,
                            ArrivalLabel = "Please follow your booking confirmation schedule",
                            TransportLabel = "Confirm with reception if needed",
                            AddOnLabel = "See reservation inclusions",
                            NotesLabel = "Keep this premium pass and your QR code ready for arrival."
                        },
                        (Bitmap)qrBitmap.Clone());
                }

                byte[] qrBytes;
                using (var ms = new MemoryStream())
                {
                    qrBitmap.Save(ms, ImageFormat.Png);
                    qrBytes = ms.ToArray();
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SmtpUser));
                message.To.Add(new MailboxAddress(guestName, toEmail));
                message.Subject = $"WildNest Booking Confirmed - {bookingId}";

                string premiumPassNotice = string.IsNullOrWhiteSpace(bookingPassFilePath)
                    ? string.Empty
                    : @"
            <div style='background:#fff4d6;border:1px solid #e4c36b;border-radius:18px;padding:18px 22px;margin:22px 0;font-size:13px;color:#5f4200;line-height:1.7;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#8a6010;margin-bottom:6px;'>ATTACHED GUEST PASS</div>
              <strong>Save the attached premium booking pass</strong> for a cleaner arrival presentation with your QR code, reservation details, and reception guidance.
            </div>";

                string openPassNotice = string.IsNullOrWhiteSpace(guestPassUrl)
                    ? @"
            <div style='background:#f8f2e8;border:1px solid #e2d3bb;border-radius:18px;padding:18px 22px;margin:22px 0;font-size:13px;color:#5b564e;line-height:1.7;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#8a6010;margin-bottom:6px;'>PHONE ACCESS NOTE</div>
              The phone-ready booking pass link is not available at the moment. Your reservation QR still works for reception recognition, and the attached premium booking pass remains the best guest-facing copy to keep on your device.
            </div>"
                    : $@"
            <div style='background:#eef7ff;border:1px solid #c8dff7;border-radius:18px;padding:18px 22px;margin:22px 0;font-size:13px;color:#18466f;line-height:1.7;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#0b5fc1;margin-bottom:6px;'>PHONE-READY BOOKING PASS</div>
              Scan the QR code in this email or use the secure link below to open your hosted WildNest booking pass from any device or network.
              <div style='margin-top:14px;'>
                <a href='{guestPassUrl}' style='display:inline-block;background:#0b5fc1;color:#ffffff;text-decoration:none;font-weight:700;padding:12px 18px;border-radius:999px;'>Open Premium Booking Pass</a>
              </div>
            </div>";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f1ece4;font-family:Segoe UI,Arial,sans-serif;color:#18251d;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1ece4;padding:34px 0;'>
    <tr><td align='center'>
      <table width='580' cellpadding='0' cellspacing='0' style='background:#fffdfa;border-radius:24px;overflow:hidden;border:1px solid #e6d9c5;box-shadow:0 22px 54px rgba(18,28,20,0.14);'>
        <tr>
          <td style='background:#071a0e;padding:34px 42px 26px 42px;text-align:center;border-bottom:1px solid rgba(212,160,23,0.26);'>
            <div style='display:inline-block;background:#133321;border:1px solid rgba(212,160,23,0.38);border-radius:999px;padding:8px 18px;color:#d4a017;font-size:11px;font-weight:800;letter-spacing:2.2px;'>PREMIUM BOOKING CONFIRMATION</div>
            <div style='font-size:34px;font-weight:800;color:#f8f4ef;letter-spacing:6px;margin-top:18px;'>WILDNEST</div>
            <div style='color:#8fb89a;font-size:13px;margin-top:8px;letter-spacing:1.2px;'>ZOO RESORT · WILDLIFE EXPERIENCE · CARMEN, CEBU</div>
          </td>
        </tr>
        <tr>
          <td style='background:#0d2114;padding:22px 42px 28px 42px;text-align:left;'>
            <div style='font-size:13px;font-weight:700;color:#d4a017;letter-spacing:2px;'>RESERVATION APPROVED</div>
            <div style='font-size:28px;line-height:1.2;font-weight:800;color:#f8f4ef;margin-top:10px;'>Your WildNest experience is confirmed.</div>
            <div style='color:#b5c1b5;font-size:14px;margin-top:8px;line-height:1.7;'>Keep this email, your reservation reference, and the attached premium booking pass ready for a smooth arrival.</div>
          </td>
        </tr>
        <tr>
          <td style='padding:34px 42px 18px 42px;'>
            <p style='color:#243126;font-size:15px;line-height:1.7;margin:0;'>Dear <strong>{guestName}</strong>,</p>
            <p style='color:#5b6258;font-size:14px;line-height:1.8;margin:14px 0 0 0;'>
              Thank you for choosing WildNest. Your reservation is now active in our guest system. Below is your arrival-ready booking reference, payment summary, and premium QR pass.
            </p>
            <table width='100%' cellpadding='0' cellspacing='0' style='background:#071a0e;border-radius:20px;margin:26px 0 18px 0;overflow:hidden;'>
              <tr>
                <td style='padding:22px 26px 20px 26px;'>
                  <div style='color:rgba(248,244,239,0.55);font-size:11px;font-weight:800;letter-spacing:2px;'>BOOKING REFERENCE</div>
                  <div style='color:#f8f4ef;font-size:29px;font-weight:800;font-family:Georgia,serif;margin-top:6px;line-height:1.2;'>{bookingId}</div>
                  <div style='color:#d4a017;font-size:12px;margin-top:8px;letter-spacing:1.2px;'>Use this reference for guest portal access, front-desk lookup, and arrival confirmation.</div>
                </td>
              </tr>
            </table>

            <table width='100%' cellpadding='0' cellspacing='0' style='margin:0 0 20px 0;'>
              <tr>
                <td width='50%' style='padding-right:8px;vertical-align:top;'>
                  <table width='100%' cellpadding='0' cellspacing='0' style='border:1px solid #e5dbce;border-radius:18px;background:#fffaf2;overflow:hidden;'>
                    <tr><td style='padding:18px 18px 6px 18px;color:#8a6010;font-size:11px;font-weight:800;letter-spacing:1.8px;'>BOOKING TYPE</td></tr>
                    <tr><td style='padding:0 18px 18px 18px;color:#18251d;font-size:20px;font-weight:700;'>{bookingType}</td></tr>
                  </table>
                </td>
                <td width='50%' style='padding-left:8px;vertical-align:top;'>
                  <table width='100%' cellpadding='0' cellspacing='0' style='border:1px solid #dce6db;border-radius:18px;background:#f6fbf7;overflow:hidden;'>
                    <tr><td style='padding:18px 18px 6px 18px;color:#1f6a43;font-size:11px;font-weight:800;letter-spacing:1.8px;'>PAYMENT METHOD</td></tr>
                    <tr><td style='padding:0 18px 18px 18px;color:#18251d;font-size:20px;font-weight:700;'>{paymentMethod}</td></tr>
                  </table>
                </td>
              </tr>
            </table>

            <table width='100%' cellpadding='0' cellspacing='0' style='border:1px solid #e8ddd0;border-radius:20px;background:#ffffff;overflow:hidden;'>
              <tr>
                <td style='padding:18px 22px;border-bottom:1px solid #f0e7db;'>
                  <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#8a6010;'>STAY SUMMARY</div>
                  <div style='color:#495247;font-size:13px;margin-top:6px;line-height:1.7;'>{dateInfo}</div>
                </td>
              </tr>
              <tr>
                <td style='padding:20px 22px 18px 22px;'>
                  <table width='100%' cellpadding='0' cellspacing='0'>
                    <tr>
                      <td style='color:#7c857a;font-size:12px;font-weight:700;'>TOTAL AMOUNT</td>
                      <td align='right' style='color:#18251d;font-size:26px;font-weight:800;font-family:Georgia,serif;'>PHP {totalAmount:N2}</td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>

            <div style='text-align:center;margin:28px 0 18px 0;padding:22px 20px;border-radius:20px;background:#f8fbf8;border:1px solid #dce7dd;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#1f6a43;margin-bottom:8px;'>ARRIVAL QR PASS</div>
              <div style='font-size:13px;color:#4f5b50;margin-bottom:16px;line-height:1.7;'>
                Scan this on a phone to open your premium booking pass when the WildNest desk application is active, or present it at reception for arrival recognition.
              </div>
              <img src='cid:qrcode' width='180' height='180' style='border:1px solid #ddd;border-radius:8px;padding:8px;background:#fff;' alt='QR Code'/>
              <div style='font-size:11px;color:#7c857a;margin-top:10px;'>QR Code for booking {bookingId}</div>
            </div>

            <div style='background:#eff6ff;border:1px solid #c8dff7;border-radius:18px;padding:18px 22px;margin:22px 0;font-size:13px;color:#19456d;line-height:1.8;'>
              <div style='font-size:11px;font-weight:800;letter-spacing:1.8px;color:#0b5fc1;margin-bottom:8px;'>HOW TO USE THIS CONFIRMATION</div>
              1. Enter booking reference <strong>{bookingId}</strong> together with this email address in the guest portal.<br/>
              2. Scan the QR to open the premium booking pass on any phone or network.<br/>
              3. Keep the attached premium pass for an even cleaner arrival presentation.<br/>
              4. Present the QR code at reception for recognition and check-in guidance.
            </div>
            {openPassNotice}
            {premiumPassNotice}
          </td>
        </tr>
        <tr>
          <td style='background:#071a0e;padding:22px 42px;text-align:center;'>
            <div style='color:#8fb89a;font-size:12px;'>WildNest Zoo Resort · Carmen, Cebu, Philippines</div>
            <div style='color:#4a6741;font-size:11px;margin-top:6px;'>This is an automated booking confirmation from the WildNest guest system.</div>
          </td>
        </tr>
      </table>
    </td></tr>
  </table>
</body>
</html>"
                };

                var qrStream = new MemoryStream(qrBytes);
                var inlineImg = builder.LinkedResources.Add("qrcode.png", qrStream, new ContentType("image", "png"));
                inlineImg.ContentId = "qrcode";
                inlineImg.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);

                var attachStream = new MemoryStream(qrBytes);
                builder.Attachments.Add($"WildNest_QR_{bookingId}.png", attachStream, new ContentType("image", "png"));

                if (!string.IsNullOrWhiteSpace(bookingPassFilePath) && File.Exists(bookingPassFilePath))
                    builder.Attachments.Add(bookingPassFilePath);

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                client.Connect(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                client.Authenticate(SmtpUser, SmtpPass);
                client.Send(message);
                client.Disconnect(true);
                ProjectDiagnostics.LogInfo("EmailService", $"Confirmation email sent for booking {bookingId} to {toEmail}.");
                return new EmailVerificationResult(true, "Confirmation email sent.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("EmailService", ex, $"Confirmation email failed for booking {bookingId}");
                return new EmailVerificationResult(false, ex.Message);
            }
        }

        internal static EmailVerificationResult SendVerificationCode(
            string toEmail,
            string guestName,
            string verificationCode,
            int validMinutes)
        {
            try
            {
                EnsureSmtpReady();
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(SenderName, SmtpUser));
                message.To.Add(new MailboxAddress(guestName, toEmail));
                message.Subject = "WildNest Verification Code";

                var safeGuestName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(guestName) ? "WildNest Guest" : guestName.Trim());
                var safeCode = WebUtility.HtmlEncode(verificationCode);
                var validityText = validMinutes <= 1 ? "60 seconds" : $"{validMinutes} minutes";

                var builder = new BodyBuilder
                {
                    HtmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:24px;background:#0b0f0d;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background:#111713;border:1px solid #edf0ed;border-radius:18px;overflow:hidden;box-shadow:0 18px 50px rgba(0,0,0,0.35);'>
        <tr>
          <td style='background:#071a0e;padding:30px 40px;border-bottom:1px solid rgba(212,160,23,0.45);'>
            <div style='font-size:30px;font-weight:800;color:#d4a017;letter-spacing:4px;'>WILDNEST</div>
            <div style='color:#d9ded8;font-size:18px;margin-top:12px;'>Email Verification</div>
            <div style='color:#8da594;font-size:12px;margin-top:6px;'>Required before a booking can be confirmed</div>
          </td>
        </tr>
        <tr>
          <td style='padding:34px 40px;color:#edf0ed;'>
            <p style='margin:0 0 22px 0;font-size:16px;'>Hello <strong>{safeGuestName}</strong>,</p>
            <p style='line-height:1.7;color:#c9d2cb;font-size:14px;margin:0;'>Your verification code is:</p>
            <div style='margin:28px 0;padding:24px 24px;border-radius:16px;background:#4d4c54;border:2px solid #d4a017;text-align:center;'>
              <div style='color:#e9c052;font-size:42px;font-weight:800;letter-spacing:14px;'>{safeCode}</div>
            </div>
            <p style='font-size:16px;line-height:1.7;color:#edf0ed;margin:0 0 22px 0;'>This code expires in <strong>{validityText}</strong>.</p>
            <div style='background:#18251d;border-left:4px solid #d4a017;border-radius:10px;padding:14px 16px;font-size:13px;color:#b9c8bd;line-height:1.6;'>
              If you did not request this, ignore this email. No WildNest booking will be confirmed unless the correct code is entered in the booking form.
            </div>
          </td>
        </tr>
        <tr>
          <td style='background:#071a0e;padding:18px 40px;text-align:center;color:#738a79;font-size:11px;'>
            WildNest Zoo Resort - secure booking verification
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
                ProjectDiagnostics.LogError("EmailService", ex, $"Verification email failed for {toEmail}");
                return new EmailVerificationResult(false, ex.Message);
            }
        }
    }
}
