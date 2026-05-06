using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MySql.Data.MySqlClient;

// ════════════════════════════════════════════════════════════════════
//  GuestPortalWebView
//
//  HOW TO USE in MyAccomodation.cs
//  (replace the old GuestPortalPanel block at end of LookupAndOpenPortal):
//
//    _card.Visible    = false;
//    _lblBack.Visible = false;
//    this.BackColor   = System.Drawing.Color.FromArgb(235, 231, 225);
//
//    var portal = new GuestPortalWebView();
//    portal.OnSignOut += () =>
//    {
//        this.Controls.Remove(portal);
//        portal.Dispose();
//        _card.Visible    = true;
//        _lblBack.Visible = true;
//        this.BackColor   = System.Drawing.Color.FromArgb(6, 20, 11);
//        _tbRef.Clear();
//        _tbEmail.Clear();
//    };
//    this.Controls.Add(portal);
//    portal.BringToFront();
//    portal.OpenPortalDirectly(guestEmail, bookingId);   // skip HTML lookup screen
//
//  OR — just show the HTML portal login screen (guest types in Booking ID + email):
//    portal.ShowLoginScreen();
//
// ════════════════════════════════════════════════════════════════════

namespace Project.Accomodations
{
    public class GuestPortalWebView : Panel
    {
        const string CONN =
            "server=localhost;user=root;database=wildnest_db;" +
            "password=Natsudragneel_525;Allow User Variables=True;";

        WebView2 _web = null!;
        public event Action? OnSignOut;

        // Chat state
        string _chatReservationId = "";
        string _chatGuestName = "";
        int _lastChatId = 0;
        System.Windows.Forms.Timer _chatTimer = null!;

        public GuestPortalWebView()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(235, 231, 225);

            _web = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_web);

            // Wire up after WebView2 core is ready
            _web.CoreWebView2InitializationCompleted += OnCoreReady;
            _ = _web.EnsureCoreWebView2Async();
        }

        // ── Called once WebView2 engine is ready ──────────────────────
        void OnCoreReady(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            // Listen for messages from JavaScript (lookup requests, sign-out)
            _web.CoreWebView2.WebMessageReceived += OnWebMessage;

            string htmlPath = Path.Combine(Application.StartupPath, "wildnest_portal.html");
            if (!File.Exists(htmlPath))
                htmlPath = Path.Combine(Application.StartupPath, "Accomodations", "wildnest_portal.html");

            if (!File.Exists(htmlPath))
            {
                MessageBox.Show(
                    $"Portal HTML not found at:\n{htmlPath}\n\n" +
                    "Make sure Accomodations\\wildnest_portal.html is in your project with\n" +
                    "Copy to Output Directory = Copy Always.",
                    "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _web.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        }

        // ── Message from JavaScript ───────────────────────────────────
        void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                using var normalizedDoc = NormalizeWebMessage(doc.RootElement);
                var root = normalizedDoc.RootElement;
                string act = root.GetProperty("action").GetString() ?? "";

                if (act == "lookup")
                {
                    string refId = root.GetProperty("ref").GetString() ?? "";
                    string email = root.GetProperty("email").GetString() ?? "";
                    LookupAndInject(refId, email);
                }
                else if (act == "signout")
                {
                    Invoke(() => OnSignOut?.Invoke());
                }
                else if (act == "chat_send")
                {
                    string msg = root.GetProperty("message").GetString() ?? "";
                    string rid = root.GetProperty("reservationId").GetString() ?? "";
                    string gname = root.GetProperty("guestName").GetString() ?? "";
                    if (!string.IsNullOrEmpty(msg) && !string.IsNullOrEmpty(rid))
                        Task.Run(() => SaveGuestChatMessage(rid, gname, msg));
                }
                else if (act == "chat_init")
                {
                    _chatReservationId = root.GetProperty("reservationId").GetString() ?? "";
                    _chatGuestName = root.GetProperty("guestName").GetString() ?? "";
                    _lastChatId = 0;
                    StartChatPolling();
                    Task.Run(() => LoadChatHistory(_chatReservationId));
                }
                else if (act == "save_html")
                {
                    string fileName = root.GetProperty("filename").GetString() ?? "WildNest-Receipt.html";
                    string html = root.GetProperty("html").GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(html))
                        SavePortalHtml(fileName, html);
                }
                else if (act == "save_html_base64")
                {
                    string fileName = root.GetProperty("filename").GetString() ?? "WildNest-Receipt.html";
                    string base64 = root.GetProperty("htmlBase64").GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(base64))
                        SavePortalHtmlBase64(fileName, base64);
                }
                else if (act == "save_pdf_base64")
                {
                    string fileName = root.GetProperty("filename").GetString() ?? "WildNest-Receipt.pdf";
                    string base64 = root.GetProperty("htmlBase64").GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(base64))
                        SavePortalPdfBase64(fileName, base64);
                }
            }
            catch { /* malformed message — ignore */ }
        }

        static JsonDocument NormalizeWebMessage(JsonElement root)
        {
            // WebView2 can deliver postMessage(JSON.stringify(obj)) as a JSON
            // string, while postMessage(obj) arrives as an object. Accept both.
            if (root.ValueKind == JsonValueKind.String)
                return JsonDocument.Parse(root.GetString() ?? "{}");

            return JsonDocument.Parse(root.GetRawText());
        }

        void SavePortalHtml(string fileName, string html)
        {
            try
            {
                string safeName = SanitizeFileName(fileName);
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "WildNest Downloads");
                Directory.CreateDirectory(folder);

                string savePath = Path.Combine(folder, safeName);
                File.WriteAllText(savePath, html, new UTF8Encoding(false));

                string script = $"portalDownloadSaved({JsonSerializer.Serialize(savePath)});";
                InvokeJs(script);
            }
            catch (Exception ex)
            {
                string script = $"portalDownloadFailed({JsonSerializer.Serialize(ex.Message)});";
                InvokeJs(script);
            }
        }

        void SavePortalHtmlBase64(string fileName, string htmlBase64)
        {
            try
            {
                string safeName = SanitizeFileName(fileName);
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "WildNest Downloads");
                Directory.CreateDirectory(folder);

                byte[] bytes = Convert.FromBase64String(htmlBase64);
                string html = Encoding.UTF8.GetString(bytes);

                string savePath = Path.Combine(folder, safeName);
                File.WriteAllText(savePath, html, new UTF8Encoding(false));

                string script = $"portalDownloadSaved({JsonSerializer.Serialize(savePath)});";
                InvokeJs(script);
            }
            catch (Exception ex)
            {
                string script = $"portalDownloadFailed({JsonSerializer.Serialize(ex.Message)});";
                InvokeJs(script);
            }
        }

        async void SavePortalPdfBase64(string fileName, string htmlBase64)
        {
            try
            {
                string safeName = SanitizeFileName(fileName);
                if (!safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    safeName = Path.ChangeExtension(safeName, ".pdf");

                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "WildNest Downloads");
                Directory.CreateDirectory(folder);

                byte[] bytes = Convert.FromBase64String(htmlBase64);
                string html = Encoding.UTF8.GetString(bytes);
                string savePath = Path.Combine(folder, safeName);

                await SavePdfFromHtmlAsync(savePath, html);
                InvokeJs($"portalDownloadSaved({JsonSerializer.Serialize(savePath)});");
            }
            catch (Exception ex)
            {
                InvokeJs($"portalDownloadFailed({JsonSerializer.Serialize(ex.Message)});");
            }
        }

        static string SanitizeFileName(string fileName)
        {
            string safe = fileName;
            foreach (char c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '-');

            if (string.IsNullOrWhiteSpace(Path.GetExtension(safe)))
                safe += ".html";

            return string.IsNullOrWhiteSpace(safe) ? "WildNest-Receipt.html" : safe;
        }

        async Task SavePdfFromHtmlAsync(string savePath, string html)
        {
            if (InvokeRequired)
            {
                var tcs = new TaskCompletionSource<object?>();
                Invoke(new Action(async () =>
                {
                    try
                    {
                        await SavePdfFromHtmlCoreAsync(savePath, html);
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
                await tcs.Task;
                return;
            }

            await SavePdfFromHtmlCoreAsync(savePath, html);
        }

        async Task SavePdfFromHtmlCoreAsync(string savePath, string html)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "WildNestPortalPdf", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                using var host = new Form
                {
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(-32000, -32000),
                    Size = new Size(1120, 1400),
                    FormBorderStyle = FormBorderStyle.None,
                    Opacity = 0
                };

                using var tempWeb = new WebView2 { Dock = DockStyle.Fill };
                host.Controls.Add(tempWeb);
                host.Show();
                host.Hide();

                await tempWeb.EnsureCoreWebView2Async();

                var navTcs = new TaskCompletionSource<bool>();
                void NavigationCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
                {
                    navTcs.TrySetResult(e.IsSuccess);
                }

                tempWeb.CoreWebView2.NavigationCompleted += NavigationCompleted;
                try
                {
                    tempWeb.CoreWebView2.NavigateToString(html);
                    bool ok = await navTcs.Task;
                    if (!ok)
                        throw new InvalidOperationException("The receipt preview could not be prepared for PDF export.");
                }
                finally
                {
                    tempWeb.CoreWebView2.NavigationCompleted -= NavigationCompleted;
                }

                await Task.Delay(450);
                bool printed = await tempWeb.CoreWebView2.PrintToPdfAsync(savePath);
                if (!printed || !File.Exists(savePath))
                    throw new InvalidOperationException("The PDF file could not be generated.");

                host.Close();
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch { }
            }
        }

        // ── Public: skip HTML login, open portal directly (called after
        //   MyAccomodation already verified the booking ID + email) ────
        public void OpenPortalDirectly(string email, string bookingId)
        {
            // Wait for navigation to finish, then inject
            _web.NavigationCompleted += (s, e) => LookupAndInject(bookingId, email);
        }

        public void ShowLoginScreen()
        {
            // Just show the HTML as-is — guest enters their own ref + email
        }

        // ── MySQL lookup → build JSON → inject into HTML ──────────────
        void LookupAndInject(string bookingId, string email)
        {
            try
            {
                using var conn = new MySqlConnection(CONN);
                conn.Open();

                // ── 1. Get guest info ──────────────────────────────────
                var gCmd = new MySqlCommand(
                    "SELECT GuestID, FirstName, LastName, Email, Phone, Nationality " +
                    "FROM tbl_Guests WHERE Email = @em LIMIT 1;", conn);
                gCmd.Parameters.AddWithValue("@em", email);

                string firstName = "", lastName = "", guestEmail = email, phone = "", nationality = "";
                int guestId = 0;

                using (var gr = gCmd.ExecuteReader())
                {
                    if (gr.Read())
                    {
                        guestId = gr.GetInt32("GuestID");
                        firstName = gr["FirstName"].ToString()!;
                        lastName = gr["LastName"].ToString()!;
                        guestEmail = gr["Email"].ToString()!;
                        phone = gr["Phone"].ToString()!;
                        nationality = gr["Nationality"].ToString()!;
                    }
                }

                if (guestId == 0)
                {
                    InvokeJs($"showLookupError('No guest found for this email address.')");
                    return;
                }

                // ── 2. Get all reservations for this guest ─────────────
                string resSql = @"
                    SELECT
                        r.ReservationID, r.BookingType,
                        r.CheckInDate,   r.CheckOutDate,  r.VisitDate,
                        r.NumAdults,     r.NumChildren,
                        r.TotalAmount,   r.Status,
                        r.ArrivalTime,   r.ModeOfTransport,
                        COALESCE(g.SpecialRequests, '') AS SpecialRequests,
                        COALESCE(c.CabinName,'') AS CabinName,
                        p.PaymentMethod,
                        p.Status AS PaymentStatus,
                        p.PaidAt
                    FROM tbl_Reservations r
                    JOIN tbl_Guests g   ON g.GuestID  = r.GuestID
                    LEFT JOIN tbl_Cabins c   ON c.CabinID  = r.CabinID
                    LEFT JOIN tbl_Payments p ON p.ReservationID = r.ReservationID
                    WHERE r.GuestID = @gid
                    ORDER BY COALESCE(r.CheckInDate, r.VisitDate) DESC;";

                var rCmd = new MySqlCommand(resSql, conn);
                rCmd.Parameters.AddWithValue("@gid", guestId);

                var bookings = new List<Dictionary<string, object?>>();

                using (var rr = rCmd.ExecuteReader())
                {
                    while (rr.Read())
                    {
                        string rid = rr["ReservationID"].ToString()!;
                        string btype = rr["BookingType"].ToString()!;
                        string status = rr["Status"].ToString()!.ToLower();
                        string cabin = rr["CabinName"].ToString()!;
                        string payment = rr["PaymentMethod"].ToString()!;
                        string arrival = rr["ArrivalTime"].ToString()!;
                        string transport = rr["ModeOfTransport"].ToString()!;
                        string specialReq = rr["SpecialRequests"].ToString()!;
                        string paymentStatus = rr["PaymentStatus"].ToString()!;
                        decimal total = rr["TotalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(rr["TotalAmount"]);
                        int adults = rr["NumAdults"] == DBNull.Value ? 0 : Convert.ToInt32(rr["NumAdults"]);
                        int children = rr["NumChildren"] == DBNull.Value ? 0 : Convert.ToInt32(rr["NumChildren"]);

                        DateTime? checkIn = rr["CheckInDate"] == DBNull.Value ? null : Convert.ToDateTime(rr["CheckInDate"]);
                        DateTime? checkOut = rr["CheckOutDate"] == DBNull.Value ? null : Convert.ToDateTime(rr["CheckOutDate"]);
                        DateTime? visitDate = rr["VisitDate"] == DBNull.Value ? null : Convert.ToDateTime(rr["VisitDate"]);
                        DateTime? paidAt = rr["PaidAt"] == DBNull.Value ? null : Convert.ToDateTime(rr["PaidAt"]);

                        int nights = checkIn.HasValue && checkOut.HasValue
                            ? Math.Max(0, (checkOut.Value - checkIn.Value).Days) : 0;

                        // Derive HTML-compatible bookingType label
                        string htmlType = btype switch
                        {
                            "CabinStay" => "Cabin Stay",
                            "DayVisit" => "Day Visit",
                            "ExperienceVisit" => "Experience Only",
                            "FullStayExperience" => "Full Stay + Experience",
                            _ => btype
                        };

                        // Derive icon + colours
                        string icon = btype switch
                        {
                            "CabinStay" => "🏕",
                            "DayVisit" => "🎟",
                            "ExperienceVisit" => "🦁",
                            "FullStayExperience" => "⭐",
                            _ => "📋"
                        };
                        string accent = btype switch
                        {
                            "CabinStay" => "#1B4332",
                            "DayVisit" => "#0F6E56",
                            "ExperienceVisit" => "#3a4a00",
                            "FullStayExperience" => "#5a3200",
                            _ => "#1B4332"
                        };
                        string iconBg = btype switch
                        {
                            "CabinStay" => "linear-gradient(135deg,#071a0e,#1B4332)",
                            "DayVisit" => "linear-gradient(135deg,#082010,#0F6E56)",
                            "ExperienceVisit" => "linear-gradient(135deg,#1a1f00,#3a4a00)",
                            "FullStayExperience" => "linear-gradient(135deg,#1a0800,#5a3200)",
                            _ => "linear-gradient(135deg,#071a0e,#1B4332)"
                        };

                        // HTML status value
                        string htmlStatus = status switch
                        {
                            "cancelled" => "cancelled",
                            "checked-in" => "checkedin",
                            "overdue" => "overdue",
                            "checked-out" => "completed",
                            "completed" => "completed",
                            _ => (checkOut ?? visitDate ?? DateTime.Today) < DateTime.Today
                                ? "completed"
                                : "upcoming"
                        };

                        // Zone label
                        string zone = string.IsNullOrEmpty(cabin)
                            ? btype == "DayVisit" ? "All Zones · Day Pass"
                             : btype == "ExperienceVisit" ? "Safari Zone · Experience"
                             : btype == "FullStayExperience" ? "Primate Park · Safari"
                             : "WildNest Resort"
                            : $"Cabin · {cabin}";

                        // Name label
                        string name = string.IsNullOrEmpty(cabin)
                            ? htmlType
                            : cabin;

                        // Amenities (generic per type)
                        var amenities = btype switch
                        {
                            "CabinStay" => new[] { "🛏 Private Cabin", "🚿 En-suite Bath", "🌿 Forest View", "🍳 Breakfast Option", "🦜 Wildlife Access" },
                            "DayVisit" => new[] { "🗺 All Zone Access", "🦁 Animal Viewing", "🌿 Trail Access", "🅿 Parking Included" },
                            "ExperienceVisit" => new[] { "🦁 Guided Experience", "📸 Photo Opportunity", "🌿 Expert Ranger", "🎫 Entry Pass" },
                            "FullStayExperience" => new[] { "🛏 Private Cabin", "🦁 Safari Experience", "🌿 Forest Trail", "🍳 Breakfast", "📸 Photo Pass" },
                            _ => new[] { "WildNest Experience" }
                        };

                        string displayArrival = string.IsNullOrWhiteSpace(arrival)
                            ? (btype == "CabinStay" || btype == "FullStayExperience" ? "To be confirmed" : "See confirmation")
                            : arrival;

                        string displayCheckOutTime = btype == "CabinStay" || btype == "FullStayExperience"
                            ? "11:00 AM"
                            : "—";

                        var bk = new Dictionary<string, object?>
                        {
                            ["ref"] = rid,
                            ["bookingType"] = htmlType,
                            ["type"] = htmlType,
                            ["name"] = name,
                            ["icon"] = icon,
                            ["accentColor"] = accent,
                            ["iconBg"] = iconBg,
                            ["zone"] = zone,
                            ["checkIn"] = checkIn?.ToString("yyyy-MM-dd") ?? visitDate?.ToString("yyyy-MM-dd"),
                            ["checkOut"] = checkOut?.ToString("yyyy-MM-dd") ?? visitDate?.ToString("yyyy-MM-dd"),
                            ["nights"] = nights,
                            ["guests"] = adults + children,
                            ["status"] = htmlStatus,
                            ["rawStatus"] = status,
                            ["price"] = nights > 0 ? Math.Round((double)total / Math.Max(nights, 1)) : adults + children > 0 ? Math.Round((double)total / Math.Max(adults + children, 1)) : (object)0,
                            ["total"] = (int)total,
                            ["amenities"] = amenities,
                            ["addons"] = new string[0],
                            ["payment"] = string.IsNullOrEmpty(payment) ? "—" : payment,
                            ["paymentStatus"] = string.IsNullOrEmpty(paymentStatus) ? "Pending" : paymentStatus,
                            ["bookedOn"] = paidAt?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"),
                            ["checkInTime"] = displayArrival,
                            ["checkOutTime"] = displayCheckOutTime,
                            ["slotTime"] = displayArrival,
                            ["specialRequests"] = string.IsNullOrEmpty(specialReq) ? "None" : specialReq,
                            ["directions"] = "Carmen, Cebu · 40 min from Cebu City",
                            ["resortContact"] = "+63 32 XXX-XXXX",
                            ["transport"] = transport,
                        };

                        bk["addons"] = FetchReservationAddons(rid);

                        bookings.Add(bk);
                    }
                }

                if (bookings.Count == 0)
                {
                    InvokeJs("showLookupError('No reservations found for this account.')");
                    return;
                }

                var matchedBooking = bookings.Find(b =>
                    string.Equals(b["ref"]?.ToString(), bookingId, StringComparison.OrdinalIgnoreCase))
                    ?? bookings[0];

                bool hasMultipleBookings = bookings.Count >= 2;
                bool hasCompletedStay = bookings.Exists(b =>
                    string.Equals(b["status"]?.ToString(), "completed", StringComparison.OrdinalIgnoreCase));

                string portalTier = (hasMultipleBookings || hasCompletedStay)
                    ? "premium"
                    : "limited";

                var bookingsForPortal = portalTier == "limited"
                    ? new List<Dictionary<string, object?>> { matchedBooking }
                    : bookings;

                // ── 3. Build guest object matching HTML's expected shape ─
                string fullName = $"{firstName} {lastName}".Trim();
                string initials = GetInitials(fullName);
                int totalNights = 0;
                int totalSpend = 0;
                foreach (var b in bookingsForPortal)
                {
                    totalNights += (int)(b["nights"] ?? 0);
                    totalSpend += (int)(b["total"] ?? 0);
                }

                var guestObj = new Dictionary<string, object?>
                {
                    ["name"] = fullName,
                    ["email"] = guestEmail,
                    ["phone"] = phone,
                    ["initials"] = initials,
                    ["nationality"] = nationality,
                    ["portalTier"] = portalTier,
                    ["entryRef"] = matchedBooking["ref"]?.ToString() ?? bookingId,
                    ["bookings"] = bookingsForPortal,
                    ["fullBookingCount"] = bookings.Count,
                    ["totalNights"] = totalNights,
                    ["totalSpend"] = totalSpend,
                };

                // ── 4. Serialize and inject ────────────────────────────
                string json = JsonSerializer.Serialize(guestObj);
                // Escape backticks to avoid JS template literal issues
                json = json.Replace("`", "\\`");
                InvokeJs($"injectGuestData({json})");
            }
            catch (Exception ex)
            {
                InvokeJs($"showLookupError('Database error: {EscapeJs(ex.Message)}')");
            }
        }

        string[] FetchReservationAddons(string reservationId)
        {
            var addons = new List<string>();

            // Use a short-lived connection so this lookup never conflicts with
            // the main reservation reader that is still streaming rows.
            using var conn = new MySqlConnection(CONN);
            conn.Open();

            var cmd = new MySqlCommand(
                "SELECT e.ExperienceName FROM tbl_BookingExperiences be " +
                "JOIN tbl_Experiences e ON e.ExperienceID = be.ExperienceID " +
                "WHERE be.ReservationID = @rid;", conn);
            cmd.Parameters.AddWithValue("@rid", reservationId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                addons.Add(reader["ExperienceName"].ToString()!);

            return addons.ToArray();
        }

        // ── Safely call JS on the UI thread ──────────────────────────
        void InvokeJs(string script)
        {
            if (_web.IsDisposed) return;
            if (InvokeRequired)
                Invoke(() => _web.CoreWebView2?.ExecuteScriptAsync(script));
            else
                _web.CoreWebView2?.ExecuteScriptAsync(script);
        }

        static string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
        }

        static string EscapeJs(string s) =>
            s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", " ").Replace("\r", "");

        // ── CHAT METHODS ─────────────────────────────────────────────
        void SaveGuestChatMessage(string reservationId, string guestName, string message)
        {
            try
            {
                using var conn = new MySqlConnection(CONN);
                conn.Open();
                var cmd = new MySqlCommand(
                    "INSERT INTO tbl_Chat (ReservationID, GuestName, SenderRole, Message, SentAt, IsRead) " +
                    "VALUES (@rid, @gname, 'guest', @msg, NOW(), 0);", conn);
                cmd.Parameters.AddWithValue("@rid", reservationId);
                cmd.Parameters.AddWithValue("@gname", guestName);
                cmd.Parameters.AddWithValue("@msg", message);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                InvokeJs($"chatError('{EscapeJs(ex.Message)}')");
            }
        }

        void LoadChatHistory(string reservationId)
        {
            try
            {
                var msgs = new List<object>();
                using var conn = new MySqlConnection(CONN);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT ChatID, SenderRole, Message, SentAt FROM tbl_Chat " +
                    "WHERE ReservationID=@rid ORDER BY SentAt ASC;", conn);
                cmd.Parameters.AddWithValue("@rid", reservationId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int cid = Convert.ToInt32(reader["ChatID"]);
                    _lastChatId = Math.Max(_lastChatId, cid);
                    msgs.Add(new
                    {
                        role = reader["SenderRole"].ToString(),
                        message = reader["Message"].ToString(),
                        time = Convert.ToDateTime(reader["SentAt"]).ToString("hh:mm tt")
                    });
                }
                string json = JsonSerializer.Serialize(msgs);
                InvokeJs($"loadChatHistory({json})");
            }
            catch { }
        }

        void PollChatMessages()
        {
            if (string.IsNullOrEmpty(_chatReservationId)) return;
            try
            {
                var newMsgs = new List<object>();
                using var conn = new MySqlConnection(CONN);
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT ChatID, SenderRole, Message, SentAt FROM tbl_Chat " +
                    "WHERE ReservationID=@rid AND ChatID > @lastId ORDER BY SentAt ASC;", conn);
                cmd.Parameters.AddWithValue("@rid", _chatReservationId);
                cmd.Parameters.AddWithValue("@lastId", _lastChatId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int cid = Convert.ToInt32(reader["ChatID"]);
                    _lastChatId = Math.Max(_lastChatId, cid);
                    newMsgs.Add(new
                    {
                        role = reader["SenderRole"].ToString(),
                        message = reader["Message"].ToString(),
                        time = Convert.ToDateTime(reader["SentAt"]).ToString("hh:mm tt")
                    });
                }
                if (newMsgs.Count > 0)
                {
                    string json = JsonSerializer.Serialize(newMsgs);
                    InvokeJs($"appendChatMessages({json})");
                }
            }
            catch { }
        }

        void StartChatPolling()
        {
            if (_chatTimer != null) return;
            Invoke(() =>
            {
                _chatTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                _chatTimer.Tick += (s, e) => Task.Run(PollChatMessages);
                _chatTimer.Start();
            });
        }

        protected override void Dispose(bool disposing)
        {
            _chatTimer?.Stop();
            _chatTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}
