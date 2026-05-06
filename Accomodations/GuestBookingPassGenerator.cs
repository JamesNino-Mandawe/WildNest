using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Project.Accomodations
{
    internal sealed class GuestBookingPassPayload
    {
        internal string BookingId { get; init; } = string.Empty;
        internal string GuestName { get; init; } = string.Empty;
        internal string BookingType { get; init; } = string.Empty;
        internal string DateLabel { get; init; } = string.Empty;
        internal string TotalAmountLabel { get; init; } = string.Empty;
        internal string PaymentMethod { get; init; } = string.Empty;
        internal string GuestCountLabel { get; init; } = string.Empty;
        internal string AccommodationLabel { get; init; } = string.Empty;
        internal string ArrivalLabel { get; init; } = string.Empty;
        internal string TransportLabel { get; init; } = string.Empty;
        internal string AddOnLabel { get; init; } = string.Empty;
        internal string NotesLabel { get; init; } = string.Empty;
    }

    internal static class GuestBookingPassGenerator
    {
        internal static string OutputFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WildNest",
            "BookingPasses");

        internal static string Generate(GuestBookingPassPayload payload, Bitmap qrBitmap)
        {
            Directory.CreateDirectory(OutputFolder);

            string filePath = Path.Combine(OutputFolder, payload.BookingId + ".html");
            File.WriteAllText(filePath, BuildHtml(payload, qrBitmap), Encoding.UTF8);
            return filePath;
        }

        internal static void ClearGeneratedPasses()
        {
            if (!Directory.Exists(OutputFolder))
                return;

            foreach (string file in Directory.GetFiles(OutputFolder, "*.html"))
            {
                try { File.Delete(file); }
                catch { }
            }
        }

        private static string BuildHtml(GuestBookingPassPayload payload, Bitmap qrBitmap)
        {
            string qrDataUrl = ToDataUrl(qrBitmap);
            string initials = BuildInitials(payload.GuestName);

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
  <meta name='theme-color' content='#0a2415'>
  <title>WildNest Booking Pass - {Escape(payload.BookingId)}</title>
  <style>
    :root {{
      --forest:#071a0e;
      --forest-deep:#041108;
      --forest-soft:#1a6a3a;
      --gold:#d4a017;
      --gold-soft:#f1c85f;
      --cream:#f8f4ef;
      --sand:#ebe4d8;
      --text:#18251d;
      --muted:#6e776f;
      --line:#e0d5c3;
      --card:#fffdfa;
    }}
    * {{ box-sizing:border-box; }}
    body {{
      margin:0;
      font-family:'Segoe UI',Arial,sans-serif;
      color:var(--text);
      background:
        radial-gradient(circle at top left, rgba(212,160,23,.18), transparent 18rem),
        radial-gradient(circle at bottom right, rgba(7,26,14,.08), transparent 26rem),
        linear-gradient(180deg, #f7f2ea 0%, #eae2d5 100%);
      min-height:100vh;
      padding:22px 14px 34px;
    }}
    .shell {{
      max-width:1080px;
      margin:0 auto;
      background:rgba(248,244,239,.94);
      border-radius:30px;
      overflow:hidden;
      border:1px solid rgba(222,213,197,.95);
      box-shadow:0 30px 84px rgba(7,26,14,.16);
    }}
    .hero {{
      background:
        radial-gradient(circle at 12% 0%, rgba(212,160,23,.22), transparent 18%),
        radial-gradient(circle at 92% 100%, rgba(212,160,23,.18), transparent 22%),
        linear-gradient(135deg, var(--forest-deep) 0%, var(--forest) 45%, var(--forest-soft) 100%);
      color:var(--cream);
      padding:32px 34px 26px;
      position:relative;
    }}
    .hero::after {{
      content:'';
      position:absolute;
      inset:auto 34px 18px 34px;
      height:1px;
      background:linear-gradient(90deg, transparent, rgba(241,200,95,.7), transparent);
    }}
    .eyebrow {{
      color:var(--gold-soft);
      text-transform:uppercase;
      letter-spacing:.24em;
      font-size:.82rem;
      font-weight:700;
      margin-bottom:10px;
    }}
    .hero-grid {{
      display:grid;
      grid-template-columns:minmax(0,1fr) auto;
      gap:22px;
      align-items:start;
    }}
    .hero h1 {{
      margin:0 0 8px;
      font:700 2.4rem/1.06 Georgia,serif;
    }}
    .hero p {{
      margin:0;
      max-width:650px;
      color:rgba(248,244,239,.88);
      line-height:1.6;
    }}
    .hero-side {{
      display:flex;
      flex-direction:column;
      gap:10px;
      align-items:flex-end;
    }}
    .status-chip, .ref-chip {{
      display:inline-flex;
      align-items:center;
      gap:8px;
      border-radius:999px;
      padding:10px 14px;
      font-weight:700;
      font-size:.88rem;
      letter-spacing:.04em;
      white-space:nowrap;
    }}
    .status-chip {{
      background:rgba(12,38,22,.92);
      color:var(--gold-soft);
      border:1px solid rgba(241,200,95,.25);
    }}
    .ref-chip {{
      background:rgba(248,244,239,.1);
      color:#fff;
      border:1px solid rgba(248,244,239,.18);
      font-family:Consolas,monospace;
    }}
    .content {{
      display:grid;
      grid-template-columns:minmax(0,1.2fr) minmax(320px,.8fr);
      gap:24px;
      padding:28px;
    }}
    .card {{
      background:var(--card);
      border:1px solid var(--line);
      border-radius:22px;
      padding:22px;
      box-shadow:0 12px 30px rgba(24,37,29,.06);
    }}
    .section-title {{
      margin:0 0 14px;
      font:700 1.08rem/1.2 Georgia,serif;
      color:var(--text);
    }}
    .guest {{
      display:flex;
      align-items:center;
      gap:18px;
      margin-bottom:18px;
    }}
    .avatar {{
      width:78px;
      height:78px;
      border-radius:50%;
      display:flex;
      align-items:center;
      justify-content:center;
      font:700 1.4rem/1 Georgia,serif;
      color:var(--gold);
      background:linear-gradient(135deg, var(--forest), var(--forest-soft));
      border:2px solid rgba(212,160,23,.55);
      box-shadow:0 16px 30px rgba(7,26,14,.18);
      flex:0 0 auto;
    }}
    .guest h2 {{
      margin:0;
      font:700 1.85rem/1.1 Georgia,serif;
    }}
    .guest p {{
      margin:.35rem 0 0;
      color:var(--muted);
    }}
    .grid {{
      display:grid;
      grid-template-columns:repeat(2,minmax(0,1fr));
      gap:14px;
    }}
    .item {{
      border:1px solid #e7dccd;
      border-radius:16px;
      background:linear-gradient(180deg,#fff 0%, #f6f0e6 100%);
      padding:14px;
      min-height:84px;
    }}
    .item.full {{
      grid-column:1 / -1;
    }}
    .label {{
      color:#95680f;
      font-size:.78rem;
      font-weight:700;
      letter-spacing:.08em;
      text-transform:uppercase;
      margin-bottom:8px;
    }}
    .value {{
      color:var(--text);
      line-height:1.45;
    }}
    .checklist {{
      margin:18px 0 0;
      padding:0;
      list-style:none;
      display:grid;
      gap:10px;
    }}
    .checklist li {{
      padding:12px 14px;
      border:1px solid #e7dccd;
      border-radius:14px;
      background:linear-gradient(180deg,#fff 0%, #f7f1e8 100%);
      color:#36443a;
    }}
    .qr-wrap {{
      text-align:center;
      display:flex;
      flex-direction:column;
      align-items:center;
      gap:16px;
    }}
    .qr-box {{
      width:min(100%,300px);
      background:#fff;
      border:1px solid #ebdfcf;
      border-radius:22px;
      padding:18px;
      box-shadow:0 18px 38px rgba(24,37,29,.10);
    }}
    .qr-box img {{
      width:100%;
      height:auto;
      display:block;
      border-radius:14px;
    }}
    .download {{
      display:inline-flex;
      justify-content:center;
      align-items:center;
      min-width:220px;
      padding:14px 18px;
      border-radius:16px;
      background:linear-gradient(135deg,#0f6236 0%,#1a7d46 100%);
      color:#fff;
      text-decoration:none;
      font-weight:700;
      box-shadow:0 14px 30px rgba(15,98,54,.24);
    }}
    .support {{
      width:100%;
      padding:16px;
      border-radius:18px;
      border:1px solid #e8dcc9;
      background:linear-gradient(180deg,#fff 0%, #f8f2ea 100%);
      text-align:left;
    }}
    .support strong {{
      display:block;
      margin-bottom:8px;
      color:#7c5405;
    }}
    .fine {{
      color:var(--muted);
      line-height:1.65;
      font-size:.9rem;
    }}
    @media (max-width:860px) {{
      .hero-grid {{ grid-template-columns:1fr; }}
      .hero-side {{ align-items:flex-start; }}
      .content {{ grid-template-columns:1fr; }}
      .grid {{ grid-template-columns:1fr; }}
      .item.full {{ grid-column:auto; }}
    }}
  </style>
</head>
<body>
  <div class='shell'>
    <section class='hero'>
      <div class='hero-grid'>
        <div>
          <div class='eyebrow'>WildNest Premium Pass</div>
          <h1>Guest Arrival Booking Pass</h1>
          <p>This premium pass keeps your confirmed reservation details, QR code, and arrival summary in one polished offline-ready file.</p>
        </div>
        <div class='hero-side'>
          <div class='status-chip'>Confirmed Reservation</div>
          <div class='ref-chip'>{Escape(payload.BookingId)}</div>
        </div>
      </div>
    </section>
    <section class='content'>
      <div class='card'>
        <h3 class='section-title'>Guest Profile</h3>
        <div class='guest'>
          <div class='avatar'>{Escape(initials)}</div>
          <div>
            <h2>{Escape(payload.GuestName)}</h2>
            <p>{Escape(payload.BookingType)} reservation</p>
          </div>
        </div>
        <div class='grid'>
          <div class='item'>
            <div class='label'>Booking Reference</div>
            <div class='value'>{Escape(payload.BookingId)}</div>
          </div>
          <div class='item'>
            <div class='label'>Guest Count</div>
            <div class='value'>{Escape(payload.GuestCountLabel)}</div>
          </div>
          <div class='item full'>
            <div class='label'>Schedule</div>
            <div class='value'>{Escape(payload.DateLabel)}</div>
          </div>
          <div class='item'>
            <div class='label'>Accommodation or Visit</div>
            <div class='value'>{Escape(payload.AccommodationLabel)}</div>
          </div>
          <div class='item'>
            <div class='label'>Arrival Window</div>
            <div class='value'>{Escape(payload.ArrivalLabel)}</div>
          </div>
          <div class='item'>
            <div class='label'>Payment Method</div>
            <div class='value'>{Escape(payload.PaymentMethod)}</div>
          </div>
          <div class='item'>
            <div class='label'>Total Amount</div>
            <div class='value'>{Escape(payload.TotalAmountLabel)}</div>
          </div>
          <div class='item'>
            <div class='label'>Transport</div>
            <div class='value'>{Escape(payload.TransportLabel)}</div>
          </div>
          <div class='item'>
            <div class='label'>Add-Ons</div>
            <div class='value'>{Escape(payload.AddOnLabel)}</div>
          </div>
            <div class='item full'>
              <div class='label'>Guest Notes</div>
              <div class='value'>{Escape(payload.NotesLabel)}</div>
            </div>
          </div>
        <ul class='checklist'>
          <li><strong>Arrival Ready:</strong> Present this QR code at the reception scanner for faster guest recognition and protected check-in.</li>
          <li><strong>Keep This Pass Saved:</strong> This file works as your premium backup copy of the booking confirmation details sent by email.</li>
          <li><strong>Need Assistance?</strong> Bring the booking reference shown here if a manual verification step is needed at the front desk.</li>
        </ul>
      </div>
      <div class='card qr-wrap'>
        <h3 class='section-title' style='margin-bottom:0;'>Scan-Ready Guest QR</h3>
        <div class='qr-box'>
          <img src='{qrDataUrl}' alt='WildNest QR Code'>
        </div>
        <div class='fine'>Present this QR code at the reception scanner or keep the booking reference ready for manual verification.</div>
        <a class='download' download='WildNest_QR_{Escape(payload.BookingId)}.png' href='{qrDataUrl}'>Download QR Image</a>
        <div class='support'>
          <strong>WildNest Arrival Guidance</strong>
          <div class='fine'>Use the QR for fast recognition, keep the reservation reference visible, and arrive with the same details shown in your confirmation email. This layout is designed to feel premium, clear, and presentation-ready on a separate device.</div>
        </div>
      </div>
    </section>
  </div>
</body>
</html>";
        }

        private static string ToDataUrl(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "WN";

            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpperInvariant();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
        }

        private static string Escape(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
