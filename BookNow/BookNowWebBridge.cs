using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Project.Accomodations;

namespace Project.Booking
{
    internal sealed class BookNowWebBridge
    {
        private const string ConnStr = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";
        private const string PublicGuestPassBaseUrl = "https://wildnest-guest-pass.vercel.app/";

        private readonly Dictionary<string, PendingVerification> _pendingCodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _verifiedEmails = new(StringComparer.OrdinalIgnoreCase);

        public async Task<string> HandleAsync(string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;
                string action = GetString(root, "action");

                return action switch
                {
                    "confirmBooking" => await StartVerificationAsync(root),
                    "verifyAndConfirm" => await VerifyAndConfirmAsync(root),
                    _ => Reply(false, "Unknown booking action.")
                };
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("BookNowWebBridge", ex, "Unhandled web booking bridge error.");
                return Reply(false, "Booking failed: " + ex.Message);
            }
        }

        private async Task<string> StartVerificationAsync(JsonElement root)
        {
            var request = BookingWebRequest.From(root);
            ValidateRequest(request);

            if (_verifiedEmails.Contains(request.Email))
                return await SaveBookingAsync(request);

            string code = EmailSecurity.CreateVerificationCode();
            _pendingCodes[request.Email] = new PendingVerification
            {
                Code = code,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(EmailSecurity.VerificationCodeValidMinutes),
                Attempts = 0
            };

            var result = await Task.Run(() =>
                EmailService.SendVerificationCode(
                    request.Email,
                    request.GuestFullName,
                    code,
                    EmailSecurity.VerificationCodeValidMinutes));

            if (!result.Success)
                return Reply(false, "Could not send verification email: " + result.Message);

            return JsonSerializer.Serialize(new
            {
                success = true,
                status = "verification_required",
                email = EmailSecurity.MaskEmail(request.Email),
                message = $"A 6-digit verification code was sent to {EmailSecurity.MaskEmail(request.Email)}."
            });
        }

        private async Task<string> VerifyAndConfirmAsync(JsonElement root)
        {
            var request = BookingWebRequest.From(root);
            ValidateRequest(request);

            string code = GetString(root, "code").Trim();
            if (!_pendingCodes.TryGetValue(request.Email, out var pending))
                return Reply(false, "Please request a fresh verification code first.");

            if (DateTime.UtcNow > pending.ExpiresUtc)
            {
                _pendingCodes.Remove(request.Email);
                return Reply(false, "Verification code expired. Please confirm again to receive a new code.");
            }

            if (!string.Equals(code, pending.Code, StringComparison.Ordinal))
            {
                pending.Attempts++;
                if (pending.Attempts >= EmailSecurity.MaxVerificationAttempts)
                {
                    _pendingCodes.Remove(request.Email);
                    return Reply(false, "Too many incorrect code attempts. Please request a fresh code.");
                }

                int left = EmailSecurity.MaxVerificationAttempts - pending.Attempts;
                return Reply(false, $"Incorrect verification code. {left} attempt(s) left.");
            }

            _pendingCodes.Remove(request.Email);
            _verifiedEmails.Add(request.Email);
            return await SaveBookingAsync(request);
        }

        private static void ValidateRequest(BookingWebRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                throw new InvalidOperationException("First name is required.");
            if (string.IsNullOrWhiteSpace(request.LastName))
                throw new InvalidOperationException("Last name is required.");
            if (!EmailSecurity.TryNormalizeAndValidate(request.Email, out var normalizedEmail, out var emailError))
                throw new InvalidOperationException(emailError);

            request.Email = normalizedEmail;

            if (request.Adults < 1)
                throw new InvalidOperationException("At least one adult guest is required.");

            if ((request.Kind == BookingKind.CabinStay || request.Kind == BookingKind.FullStay) &&
                string.IsNullOrWhiteSpace(request.CabinName))
                throw new InvalidOperationException("Please select a cabin first.");

            if ((request.Kind == BookingKind.ExperienceVisit || request.Kind == BookingKind.FullStay) &&
                request.Experiences.Count == 0)
                throw new InvalidOperationException("Please select at least one experience.");

            if (request.Kind == BookingKind.CabinStay || request.Kind == BookingKind.FullStay)
            {
                if (request.CheckInDate == null || request.CheckOutDate == null || request.CheckOutDate <= request.CheckInDate)
                    throw new InvalidOperationException("Please choose valid check-in and check-out dates.");
            }
            else if (request.VisitDate == null)
            {
                throw new InvalidOperationException("Please choose a visit date.");
            }
        }

        private static async Task<string> SaveBookingAsync(BookingWebRequest request)
        {
            return await Task.Run(() =>
            {
                MySqlTransaction? tx = null;
                string bookingId = BookingIdGenerator.NewId();

                try
                {
                    using var conn = new MySqlConnection(ConnStr);
                    conn.Open();
                    tx = conn.BeginTransaction();

                    int guestId = BookingPersistence.GetOrCreateGuest(
                        conn,
                        tx,
                        request.FirstName,
                        request.LastName,
                        request.Email,
                        request.Phone,
                        request.Nationality,
                        request.ValidIdType,
                        request.SpecialRequests);

                    int? cabinId = null;
                    if (!string.IsNullOrWhiteSpace(request.CabinName))
                        cabinId = BookingPersistence.ResolveCabinId(conn, tx, request.CabinName);

                    decimal total = request.CalculateServerTotal();
                    InsertReservation(conn, tx, request, bookingId, guestId, cabinId, total);

                    foreach (var item in request.Experiences.Concat(request.Addons))
                    {
                        int? experienceId = BookingPersistence.ResolveExperienceId(conn, tx, item.Name);
                        if (experienceId.HasValue)
                            BookingPersistence.InsertExperienceLink(conn, tx, bookingId, experienceId.Value, Math.Max(1, item.Quantity));
                    }

                    BookingPersistence.InsertPayment(conn, tx, bookingId, total, request.PaymentMethod);
                    tx.Commit();

                    using var receptionQr = EmailService.GenerateQrBitmap(bookingId);
                    string bookingPassFilePath = GuestBookingPassGenerator.Generate(
                        BuildGuestPassPayload(request, bookingId, total),
                        receptionQr);

                    string? guestPassUrl = BuildHostedGuestPassUrl(request, bookingId, total);
                    using var guestQr = EmailService.GenerateQrBitmap(guestPassUrl ?? bookingId);
                    string qrDataUrl = ToDataUrl(guestQr);

                    _ = Task.Run(() =>
                    {
                        using var emailQr = EmailService.GenerateQrBitmap(guestPassUrl ?? bookingId);
                        var emailResult = EmailService.SendConfirmation(
                            request.Email,
                            request.GuestFullName,
                            bookingId,
                            request.EmailBookingType,
                            request.DateInfo,
                            total,
                            request.PaymentMethod,
                            emailQr,
                            bookingPassFilePath,
                            guestPassUrl);

                        if (!emailResult.Success)
                            ProjectDiagnostics.LogWarning("BookNowWebBridge", $"Booking {bookingId} saved but confirmation email failed: {emailResult.Message}");
                    });

                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        status = "confirmed",
                        reservationId = bookingId,
                        total = total.ToString("N2", CultureInfo.InvariantCulture),
                        qrDataUrl,
                        message = "Booking confirmed. Your Booking ID and QR Code were generated, and the confirmation email is being sent."
                    });
                }
                catch
                {
                    try { tx?.Rollback(); } catch { }
                    throw;
                }
            });
        }

        private static void InsertReservation(MySqlConnection conn, MySqlTransaction tx, BookingWebRequest request, string bookingId, int guestId, int? cabinId, decimal total)
        {
            using var cmd = new MySqlCommand(@"
INSERT INTO tbl_Reservations
    (ReservationID, GuestID, CabinID, BookingType,
     CheckInDate, CheckOutDate, VisitDate,
     NumAdults, NumChildren, TotalAmount, Status, CreatedAt,
     ArrivalTime, ModeOfTransport)
VALUES
    (@rid, @gid, @cid, @type,
     @checkIn, @checkOut, @visitDate,
     @adults, @children, @total, 'Confirmed', NOW(),
     @arrival, @transport);", conn, tx);

            cmd.Parameters.AddWithValue("@rid", bookingId);
            cmd.Parameters.AddWithValue("@gid", guestId);
            cmd.Parameters.AddWithValue("@cid", cabinId.HasValue ? cabinId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@type", request.DatabaseBookingType);
            cmd.Parameters.AddWithValue("@checkIn", request.CheckInDate.HasValue ? request.CheckInDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@checkOut", request.CheckOutDate.HasValue ? request.CheckOutDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@visitDate", request.VisitDate.HasValue ? request.VisitDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@adults", request.Adults);
            cmd.Parameters.AddWithValue("@children", request.Children);
            cmd.Parameters.AddWithValue("@total", total);
            cmd.Parameters.AddWithValue("@arrival", request.ArrivalTime);
            cmd.Parameters.AddWithValue("@transport", request.Transport);
            cmd.ExecuteNonQuery();
        }

        private static string ToDataUrl(System.Drawing.Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }

        private static string? BuildHostedGuestPassUrl(BookingWebRequest request, string bookingId, decimal total)
        {
            if (string.IsNullOrWhiteSpace(PublicGuestPassBaseUrl))
                return null;

            string guestCountLabel = $"{request.Adults}A";
            if (request.Children > 0)
                guestCountLabel += $"{request.Children}C";

            string primaryItem = request.Kind switch
            {
                BookingKind.CabinStay => request.CabinName,
                  BookingKind.FullStay => string.IsNullOrWhiteSpace(request.CabinName)
                      ? "Full Stay"
                      : request.CabinName,
                BookingKind.DayVisit => request.Zones.Count > 0
                    ? string.Join(", ", request.Zones.Select(z => z.Name))
                    : "Day Visit Access",
                BookingKind.ExperienceVisit => request.Experiences.Count > 0
                    ? string.Join(", ", request.Experiences.Select(e => e.Name))
                    : "WildNest Experience",
                _ => request.EmailBookingType
            };

            string compactDateLabel = request.Kind switch
            {
                BookingKind.CabinStay or BookingKind.FullStay
                    when request.CheckInDate.HasValue && request.CheckOutDate.HasValue
                      => $"{request.CheckInDate:yyyyMMdd}-{request.CheckOutDate:yyyyMMdd}",
                  _ when request.VisitDate.HasValue
                      => request.VisitDate.Value.ToString("yyyyMMdd"),
                  _ => request.DateInfo
              };

            string bookingTypeCode = request.Kind switch
            {
                BookingKind.CabinStay => "CS",
                BookingKind.DayVisit => "DV",
                BookingKind.ExperienceVisit => "EO",
                BookingKind.FullStay => "FX",
                _ => "BK"
            };

            string paymentCode = request.PaymentMethod.Trim().ToLowerInvariant() switch
            {
                "pay at resort" => "R",
                "credit / debit card" => "C",
                "gcash / maya" => "G",
                "bank transfer" => "B",
                _ => "P"
            };

            var compactPayload = new Dictionary<string, string>
            {
                  ["g"] = request.GuestFullName,
                  ["t"] = bookingTypeCode,
                  ["dw"] = compactDateLabel,
                  ["gc"] = guestCountLabel,
                  ["pi"] = primaryItem,
                  ["pm"] = paymentCode,
                  ["ta"] = total.ToString("0"),
                  ["st"] = "C"
              };

            string payloadJson = JsonSerializer.Serialize(
                compactPayload.Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                              .ToDictionary(pair => pair.Key, pair => pair.Value));
            string packed = ToBase64Url(payloadJson);

            return $"{PublicGuestPassBaseUrl.TrimEnd('/')}/?i={Uri.EscapeDataString(bookingId)}&d={packed}";
        }

        private static string ToBase64Url(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static GuestBookingPassPayload BuildGuestPassPayload(BookingWebRequest request, string bookingId, decimal total)
        {
            string guestCountLabel = $"{request.Adults} adult{(request.Adults == 1 ? string.Empty : "s")}";
            if (request.Children > 0)
                guestCountLabel += $", {request.Children} child{(request.Children == 1 ? string.Empty : "ren")}";

            string accommodationLabel = request.Kind switch
            {
                BookingKind.CabinStay => request.CabinName,
                BookingKind.FullStay => string.IsNullOrWhiteSpace(request.CabinName)
                    ? "Full stay accommodation"
                    : request.CabinName,
                BookingKind.DayVisit => request.Zones.Count > 0
                    ? "Day visit zones: " + string.Join(", ", request.Zones.Select(z => z.Name))
                    : "Day visit access",
                BookingKind.ExperienceVisit => request.Experiences.Count > 0
                    ? string.Join(", ", request.Experiences.Select(x => x.Name))
                    : "Experience visit",
                _ => "WildNest booking"
            };

            string arrivalLabel = request.Kind switch
            {
                BookingKind.CabinStay or BookingKind.FullStay => string.IsNullOrWhiteSpace(request.ArrivalTime)
                    ? "Arrival time to be confirmed at reception"
                    : request.ArrivalTime,
                _ => request.VisitDate.HasValue
                    ? request.VisitDate.Value.ToString("dddd, MMM dd, yyyy")
                    : "Visit schedule to be confirmed"
            };

            string addOnLabel = BuildListLabel(request.Addons.Select(x => x.Name));
            string notes = string.IsNullOrWhiteSpace(request.SpecialRequests)
                ? "Present this pass or your reservation reference during arrival."
                : "Guest requests: " + request.SpecialRequests;

            return new GuestBookingPassPayload
            {
                BookingId = bookingId,
                GuestName = request.GuestFullName,
                BookingType = request.EmailBookingType,
                DateLabel = request.DateInfo,
                TotalAmountLabel = $"PHP {total:N2}",
                PaymentMethod = request.PaymentMethod,
                GuestCountLabel = guestCountLabel,
                AccommodationLabel = accommodationLabel,
                ArrivalLabel = arrivalLabel,
                TransportLabel = string.IsNullOrWhiteSpace(request.Transport) ? "To be confirmed at arrival" : request.Transport,
                AddOnLabel = string.IsNullOrWhiteSpace(addOnLabel) ? "No optional add-ons recorded" : addOnLabel,
                NotesLabel = notes
            };
        }

        private static string BuildListLabel(IEnumerable<string> values)
        {
            return string.Join(", ", values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string Reply(bool success, string message)
        {
            return JsonSerializer.Serialize(new { success, message });
        }

        private static string GetString(JsonElement root, string name)
        {
            return root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? string.Empty
                : string.Empty;
        }

        private sealed class PendingVerification
        {
            public string Code { get; init; } = string.Empty;
            public DateTime ExpiresUtc { get; init; }
            public int Attempts { get; set; }
        }
    }

    internal enum BookingKind
    {
        CabinStay,
        DayVisit,
        ExperienceVisit,
        FullStay
    }

    internal sealed class BookingWebRequest
    {
        public BookingKind Kind { get; private init; }
        public string FirstName { get; private init; } = string.Empty;
        public string LastName { get; private init; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; private init; } = string.Empty;
        public string Nationality { get; private init; } = string.Empty;
        public string ValidIdType { get; private init; } = string.Empty;
        public string SpecialRequests { get; private init; } = string.Empty;
        public string CabinName { get; private init; } = string.Empty;
        public decimal CabinRate { get; private init; }
        public DateTime? CheckInDate { get; private init; }
        public DateTime? CheckOutDate { get; private init; }
        public DateTime? VisitDate { get; private init; }
        public int Adults { get; private init; }
        public int Children { get; private init; }
        public string ArrivalTime { get; private init; } = string.Empty;
        public string Transport { get; private init; } = string.Empty;
        public string PaymentMethod { get; private init; } = "Credit / Debit Card";
        public List<BookingLineItem> Addons { get; private init; } = new();
        public List<BookingLineItem> Experiences { get; private init; } = new();
        public List<BookingLineItem> Zones { get; private init; } = new();

        public string GuestFullName => $"{FirstName} {LastName}".Trim();

        public string DatabaseBookingType => Kind switch
        {
            BookingKind.CabinStay => "CabinStay",
            BookingKind.DayVisit => "DayVisit",
            BookingKind.ExperienceVisit => "ExperienceVisit",
            BookingKind.FullStay => "FullStay",
            _ => "Booking"
        };

        public string EmailBookingType => Kind switch
        {
            BookingKind.CabinStay => "Cabin Stay",
            BookingKind.DayVisit => "Day Visit",
            BookingKind.ExperienceVisit => "Experience Visit",
            BookingKind.FullStay => "Full Stay + Experience",
            _ => "Booking"
        };

        public string DateInfo => Kind switch
        {
            BookingKind.CabinStay or BookingKind.FullStay => $"Check-in: {CheckInDate:MMM dd, yyyy} - Check-out: {CheckOutDate:MMM dd, yyyy}",
            _ => $"Visit date: {VisitDate:MMM dd, yyyy}"
        };

        public int Nights => CheckInDate.HasValue && CheckOutDate.HasValue
            ? Math.Max(1, (CheckOutDate.Value.Date - CheckInDate.Value.Date).Days)
            : 1;

        public decimal CalculateServerTotal()
        {
            decimal total = 0m;

            if (Kind == BookingKind.CabinStay || Kind == BookingKind.FullStay)
                total += CabinRate * Nights;

            if (Kind == BookingKind.DayVisit)
                total += Adults * 750m + Children * 450m;

            if (Kind == BookingKind.CabinStay || Kind == BookingKind.FullStay)
                total += Children * 500m;

            total += Addons.Sum(x => x.Price * Math.Max(1, x.Quantity));
            decimal zoneBase = Zones.Sum(x => x.Price);
            total += zoneBase * Math.Max(1, Adults);
            total += zoneBase * (Children * 0.5m);
            total += Experiences.Sum(x => x.Price * Math.Max(1, Adults));
            total += Kind == BookingKind.CabinStay || Kind == BookingKind.FullStay ? 400m : 200m;

            return total;
        }

        public static BookingWebRequest From(JsonElement root)
        {
            string prefix = GetString(root, "prefix");
            var payload = root.TryGetProperty("payload", out var p) ? p : root;
            var state = payload.TryGetProperty("state", out var s) ? s : default;
            var guest = payload.TryGetProperty("guest", out var g) ? g : default;

            return new BookingWebRequest
            {
                Kind = prefix switch
                {
                    "cb" => BookingKind.CabinStay,
                    "dv" => BookingKind.DayVisit,
                    "eo" => BookingKind.ExperienceVisit,
                    "fs" => BookingKind.FullStay,
                    _ => throw new InvalidOperationException("Unknown booking type.")
                },
                FirstName = GetString(guest, "firstName"),
                LastName = GetString(guest, "lastName"),
                Email = GetString(guest, "email"),
                Phone = GetString(guest, "phone"),
                Nationality = GetString(guest, "nationality"),
                ValidIdType = GetString(guest, "validIdType"),
                SpecialRequests = GetString(guest, "specialRequests"),
                CabinName = GetString(state, "cabin"),
                CabinRate = GetDecimal(state, "price"),
                CheckInDate = GetDate(state, "checkin") ?? GetDate(payload, "checkInDate"),
                CheckOutDate = GetDate(state, "checkout") ?? GetDate(payload, "checkOutDate"),
                VisitDate = GetDate(payload, "visitDate") ?? GetDate(state, "date"),
                Adults = Math.Max(1, GetInt(payload, "adults", 2)),
                Children = Math.Max(0, GetInt(payload, "children", 0)),
                ArrivalTime = GetString(payload, "arrivalTime"),
                Transport = GetString(payload, "transport"),
                PaymentMethod = string.IsNullOrWhiteSpace(GetString(payload, "paymentMethod")) ? "Credit / Debit Card" : GetString(payload, "paymentMethod"),
                Addons = ReadItems(state, "addons"),
                Experiences = ReadItems(state, "experiences"),
                Zones = ReadItems(state, "zones", zoneUsesFee: true)
            };
        }

        private static List<BookingLineItem> ReadItems(JsonElement root, string name, bool zoneUsesFee = false)
        {
            var list = new List<BookingLineItem>();
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty(name, out var arr) ||
                arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in arr.EnumerateArray())
            {
                string itemName = GetString(item, "name");
                if (string.IsNullOrWhiteSpace(itemName))
                    continue;

                list.Add(new BookingLineItem
                {
                    Name = itemName,
                    Price = zoneUsesFee ? GetDecimal(item, "fee") : GetDecimal(item, "price"),
                    Quantity = 1
                });
            }

            return list;
        }

        private static string GetString(JsonElement root, string name)
        {
            return root.ValueKind == JsonValueKind.Object &&
                   root.TryGetProperty(name, out var prop) &&
                   prop.ValueKind == JsonValueKind.String
                ? prop.GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static int GetInt(JsonElement root, string name, int fallback)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var prop))
                return fallback;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int number))
                return number;
            return int.TryParse(prop.ToString(), out number) ? number : fallback;
        }

        private static decimal GetDecimal(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out var prop))
                return 0m;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out decimal number))
                return number;
            return decimal.TryParse(prop.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number) ? number : 0m;
        }

        private static DateTime? GetDate(JsonElement root, string name)
        {
            string raw = GetString(root, name);
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
                ? date.Date
                : null;
        }
    }

    internal sealed class BookingLineItem
    {
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Quantity { get; init; } = 1;
    }
}
