// ═══════════════════════════════════════════════════════════════════════
// WIRING SNIPPET — paste this into your ConfirmBooking() method
// (or wherever you currently call EmailService.SendConfirmation)
// Replace the single GenerateQrBitmap() + SendConfirmation() call with this.
// ═══════════════════════════════════════════════════════════════════════

// ── After your booking transaction commits ────────────────────────────

// Step 1: Generate the self-contained HTML Booking Pass
//         This creates: %AppData%\WildNest\BookingPasses\WN-2026-0001.html
string? htmlPassPath = null;
try
{
    htmlPassPath = GuestBookingPassGenerator.Generate(
        bookingId:      bookingId,          // e.g. "WN-2026-0001"
        guestName:      guestFullName,      // e.g. "Maria Santos"
        bookingType:    bookingType,        // e.g. "Overnight Stay"
        cabin:          cabinName,          // e.g. "Treetop Cabin 3"  (or "" for day visit)
        dateInfo:       dateInfo,           // e.g. "Saturday, May 10 → May 12, 2026 (2 nights)"
        totalAmount:    totalAmount,        // decimal
        paymentMethod:  paymentMethod,      // e.g. "GCash"
        status:         "Confirmed",
        guestCount:     guestCount,         // int — 0 shows "—"
        specialRequest: specialRequest,     // empty string is fine
        qrBitmap:       EmailService.GenerateQrBitmap(bookingId)  // plain-ID QR for generating pass
    );
}
catch (Exception ex)
{
    // Never crash a booking because the pass file failed to generate — just log it
    ProjectDiagnostics.LogError("BookingConfirm", ex, $"Pass generation failed for {bookingId}");
}

// Step 2: Generate the guest-facing QR that encodes the file:// URL of the pass
//         (falls back to plain booking ID if pass generation failed)
using Bitmap guestQr = EmailService.GenerateQrBitmapForPass(bookingId, htmlPassPath ?? "");

// Step 3: Send confirmation email — attaches both QR PNG and the HTML pass
EmailService.SendConfirmation(
    toEmail:          guestEmail,
    guestName:        guestFullName,
    bookingId:        bookingId,
    bookingType:      bookingType,
    dateInfo:         dateInfo,
    totalAmount:      totalAmount,
    paymentMethod:    paymentMethod,
    qrBitmap:         guestQr,
    htmlPassFilePath: htmlPassPath      // ← NEW param — attaches the HTML file to the email
);

// Step 4 (optional): Show the pass immediately after booking is confirmed
//         Great for demo — staff/kiosk can show the guest their pass on-screen
if (htmlPassPath != null)
    GuestBookingPassGenerator.OpenInBrowser(htmlPassPath);

// ── What happens when guest scans QR on their phone ───────────────────
//
//   QR decoded value: "file:///C:/Users/.../WildNest/BookingPasses/WN-2026-0001.html"
//
//   Phone camera → opens URL → loads the self-contained HTML pass
//   Guest sees:
//     - Their name and initials avatar
//     - Booking reference, type, cabin, dates, amount
//     - Their QR image (embedded as base64)
//     - "Save QR Image" download button
//     - "Works Offline · No Internet Required" badge
//
//   Zero server. Zero internet. Zero MySQL on the phone.
//   100% works for demo and real use on the same local machine/LAN.
//
// ── What the reception scanner (QrCameraScanner) reads ───────────────
//
//   The reception camera scans any QR — it doesn't care if the payload
//   is a file:// URL or a plain booking ID. Either way:
//
//   In UcReceptionCheckIn.ShowConfirmThenCheckIn():
//     // Add this normalizer at the top of the method:
//     if (reservationId.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
//     {
//         // Extract booking ID from filename: WN-2026-0001.html → WN-2026-0001
//         string filename = Path.GetFileNameWithoutExtension(
//             new Uri(reservationId).LocalPath);
//         reservationId = filename;
//     }
//
//   Then ProcessCheckIn() runs as normal with the clean booking ID.
// ═══════════════════════════════════════════════════════════════════════
