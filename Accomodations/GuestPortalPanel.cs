using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.Accomodations
{
    // ═══════════════════════════════════════════════════════════════
    //  GuestPortalPanel  — live data from all 4 booking types
    //
    //  USAGE inside MyAccomodation.cs (replace old portal creation):
    //
    //    _card.Visible = false;
    //    _lblBack.Visible = false;
    //    this.BackColor = Color.FromArgb(235, 231, 225);
    //
    //    var portal = new GuestPortalPanel(guestEmail, bookingId);
    //    portal.OnSignOut += () => {
    //        this.Controls.Remove(portal);
    //        portal.Dispose();
    //        _card.Visible    = true;
    //        _lblBack.Visible = true;
    //        this.BackColor   = Color.FromArgb(6, 20, 11);
    //        _tbRef.Clear(); _tbEmail.Clear();
    //    };
    //    this.Controls.Add(portal);
    //    portal.BringToFront();
    // ═══════════════════════════════════════════════════════════════

    public class GuestPortalPanel : Panel
    {
        // ── Palette ──────────────────────────────────────────────────
        static readonly Color C_Forest = Color.FromArgb(7, 26, 14);
        static readonly Color C_ForestMid = Color.FromArgb(13, 40, 24);
        static readonly Color C_ForestLight = Color.FromArgb(27, 67, 50);
        static readonly Color C_Gold = Color.FromArgb(212, 160, 23);
        static readonly Color C_Cream = Color.FromArgb(248, 244, 239);
        static readonly Color C_Sand = Color.FromArgb(235, 231, 225);
        static readonly Color C_White = Color.White;
        static readonly Color C_Text = Color.FromArgb(26, 26, 26);
        static readonly Color C_Muted = Color.FromArgb(107, 101, 96);
        static readonly Color C_Dim = Color.FromArgb(155, 148, 144);
        static readonly Color C_Border = Color.FromArgb(221, 221, 221);
        static readonly Color C_SuccessBg = Color.FromArgb(225, 245, 238);
        static readonly Color C_Success = Color.FromArgb(8, 80, 65);
        static readonly Color C_WarnBg = Color.FromArgb(254, 243, 226);
        static readonly Color C_Warn = Color.FromArgb(100, 56, 6);
        static readonly Color C_DangerBg = Color.FromArgb(254, 228, 226);
        static readonly Color C_Danger = Color.FromArgb(100, 20, 10);

        const string CONN =
            "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";

        // ── State ─────────────────────────────────────────────────────
        readonly string _email;
        readonly string _initialBookingId;   // the one used to log in
        int _activeTab = 0;               // 0=Upcoming 1=Past 2=Experiences 3=All

        // ── Guest meta (loaded once) ──────────────────────────────────
        string _guestName = "";
        string _guestEmail = "";
        int _totalBookings = 0;

        // ── Controls ─────────────────────────────────────────────────
        Label _lblName = null!;
        Label _lblEmail = null!;
        Label _lblStats = null!;
        FlowLayoutPanel _flpCards = null!;
        Button[] _tabBtns = null!;

        public event Action? OnSignOut;

        // ─────────────────────────────────────────────────────────────
        public GuestPortalPanel(string guestEmail, string initialBookingId)
        {
            _email = guestEmail;
            _initialBookingId = initialBookingId;

            Dock = DockStyle.Fill;
            BackColor = C_Sand;

            Build();
            LoadGuestMeta();
            LoadTab(0);
        }

        // ═════════════════════════════════════════════════════════════
        //  BUILD SHELL
        // ═════════════════════════════════════════════════════════════
        void Build()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = C_Sand
            };
            Controls.Add(scroll);

            // Stack: hero → quick-stats → tab bar → cards
            var stack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 40)
            };
            scroll.Controls.Add(stack);

            scroll.Resize += (s, e) =>
            {
                stack.Width = scroll.ClientSize.Width;
            };

            // ── Hero ──────────────────────────────────────────────
            var hero = BuildHero();
            stack.Controls.Add(hero);

            // ── Stats strip ───────────────────────────────────────
            var stats = BuildStatsStrip();
            stats.Margin = new Padding(0, 0, 0, 0);
            stack.Controls.Add(stats);

            // ── Tab bar ───────────────────────────────────────────
            var tabBar = BuildTabBar();
            tabBar.Margin = new Padding(0, 0, 0, 0);
            stack.Controls.Add(tabBar);

            // ── Cards container ───────────────────────────────────
            _flpCards = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 16, 20, 20)
            };
            stack.Controls.Add(_flpCards);

            stack.Resize += (s, e) =>
            {
                foreach (Control c in stack.Controls)
                    c.Width = stack.Width;
                _flpCards.Width = stack.Width;
            };
        }

        // ── HERO BANNER ───────────────────────────────────────────────
        Panel BuildHero()
        {
            var hero = new Panel { Height = 110, BackColor = C_Forest };
            hero.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new LinearGradientBrush(
                    new Point(0, 0), new Point(0, hero.Height), C_Forest, C_ForestMid);
                g.FillRectangle(br, hero.ClientRectangle);
                // gold glow
                DrawGlow(g, hero.Width / 2f, hero.Height, hero.Width * 0.5f,
                    Color.FromArgb(18, 212, 160, 23));
            };

            _lblName = new Label
            {
                Text = "Welcome back",
                Font = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = C_Cream,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(24, 20)
            };
            hero.Controls.Add(_lblName);

            _lblEmail = new Label
            {
                Text = _email,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(130, C_Cream),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(26, 50)
            };
            hero.Controls.Add(_lblEmail);

            // Sign-out button
            var btnOut = new Button
            {
                Text = "Sign Out",
                Size = new Size(88, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, C_Gold),
                ForeColor = C_Gold,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOut.FlatAppearance.BorderColor = Color.FromArgb(60, C_Gold);
            btnOut.Click += (s, e) => OnSignOut?.Invoke();
            hero.Controls.Add(btnOut);
            hero.Resize += (s, e) =>
                btnOut.Location = new Point(hero.Width - btnOut.Width - 16, 16);

            return hero;
        }

        // ── STATS STRIP ───────────────────────────────────────────────
        Panel BuildStatsStrip()
        {
            var strip = new Panel { Height = 60, BackColor = C_ForestLight };

            _lblStats = new Label
            {
                Text = "Loading reservations…",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, C_Cream),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(24, 20)
            };
            strip.Controls.Add(_lblStats);
            return strip;
        }

        // ── TAB BAR ───────────────────────────────────────────────────
        Panel BuildTabBar()
        {
            var bar = new Panel { Height = 46, BackColor = C_White };
            bar.Paint += (s, e) =>
            {
                using var pen = new Pen(C_Border, 1f);
                e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };

            string[] tabs = { "Upcoming", "Past Stays", "Experiences", "All Bookings" };
            _tabBtns = new Button[tabs.Length];
            int x = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Text = tabs[i],
                    Size = new Size(120, 46),
                    Location = new Point(x, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f),
                    BackColor = Color.Transparent,
                    ForeColor = i == 0 ? C_Forest : C_Muted,
                    Cursor = Cursors.Hand,
                    Tag = i == 0
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Paint += (s, e) =>
                {
                    if ((bool)btn.Tag)
                    {
                        using var pen = new Pen(C_Gold, 2.5f);
                        e.Graphics.DrawLine(pen, 4, btn.Height - 2, btn.Width - 4, btn.Height - 2);
                    }
                };
                btn.Click += (s, e) =>
                {
                    _activeTab = idx;
                    foreach (var b in _tabBtns)
                    {
                        b.Tag = false;
                        b.ForeColor = C_Muted;
                        b.Invalidate();
                    }
                    btn.Tag = true;
                    btn.ForeColor = C_Forest;
                    btn.Invalidate();
                    LoadTab(idx);
                };
                _tabBtns[i] = btn;
                bar.Controls.Add(btn);
                x += 120;
            }
            return bar;
        }

        // ═════════════════════════════════════════════════════════════
        //  DATA LOADING
        // ═════════════════════════════════════════════════════════════
        void LoadGuestMeta()
        {
            try
            {
                using var conn = new MySqlConnection(CONN);
                conn.Open();

                // Get guest name from the email used to log in
                var cmd = new MySqlCommand(
                    "SELECT FirstName, LastName FROM tbl_Guests WHERE Email = @em LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@em", _email);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    _guestName = $"{r["FirstName"]} {r["LastName"]}";
                    _guestEmail = _email;
                    _lblName.Text = $"Welcome back, {r["FirstName"]}";
                }
            }
            catch { /* silently ignore — name stays "Welcome back" */ }
        }

        void LoadTab(int tab)
        {
            _flpCards.Controls.Clear();

            var rows = QueryReservations(tab);

            if (rows.Count == 0)
            {
                var empty = new Label
                {
                    Text = tab == 0 ? "No upcoming reservations."
                              : tab == 1 ? "No past stays yet."
                              : tab == 2 ? "No experience bookings yet."
                              : "No bookings found.",
                    Font = new Font("Segoe UI", 10f),
                    ForeColor = C_Dim,
                    AutoSize = true,
                    Margin = new Padding(8, 24, 0, 0)
                };
                _flpCards.Controls.Add(empty);
            }
            else
            {
                _totalBookings = rows.Count;
                _lblStats.Text =
                    $"📋  {rows.Count} reservation{(rows.Count != 1 ? "s" : "")} found  ·  " +
                    $"Email: {_email}";

                foreach (var row in rows)
                    _flpCards.Controls.Add(BuildCard(row));
            }
        }

        // ── SQL QUERY ─────────────────────────────────────────────────
        List<ReservationRow> QueryReservations(int tab)
        {
            var list = new List<ReservationRow>();
            try
            {
                using var conn = new MySqlConnection(CONN);
                conn.Open();

                // Determine date filter
                string dateFilter = tab switch
                {
                    0 => // Upcoming — check-in/visit date >= today
                         "AND COALESCE(r.CheckInDate, r.VisitDate) >= CURDATE()",
                    1 => // Past — check-out/visit date < today
                         "AND COALESCE(r.CheckOutDate, r.VisitDate) < CURDATE()",
                    2 => // Experiences only
                         "AND r.BookingType IN ('ExperienceVisit','FullStayExperience')",
                    _ => "" // All
                };

                string sql = $@"
                    SELECT
                        r.ReservationID,
                        r.BookingType,
                        r.CheckInDate,
                        r.CheckOutDate,
                        r.VisitDate,
                        r.NumAdults,
                        r.NumChildren,
                        r.TotalAmount,
                        r.Status,
                        r.ArrivalTime,
                        r.ModeOfTransport,
                        COALESCE(c.CabinName, '') AS CabinName,
                        g.FirstName,
                        g.LastName,
                        g.Email,
                        g.Phone,
                        p.PaymentMethod,
                        p.Status AS PaymentStatus
                    FROM tbl_Reservations r
                    JOIN tbl_Guests g ON g.GuestID = r.GuestID
                    LEFT JOIN tbl_Cabins c ON c.CabinID = r.CabinID
                    LEFT JOIN tbl_Payments p ON p.ReservationID = r.ReservationID
                    WHERE g.Email = @em
                    {dateFilter}
                    ORDER BY COALESCE(r.CheckInDate, r.VisitDate) DESC;";

                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@em", _email);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new ReservationRow
                    {
                        ReservationId = r["ReservationID"].ToString()!,
                        BookingType = r["BookingType"].ToString()!,
                        CheckIn = r["CheckInDate"] == DBNull.Value ? null : Convert.ToDateTime(r["CheckInDate"]),
                        CheckOut = r["CheckOutDate"] == DBNull.Value ? null : Convert.ToDateTime(r["CheckOutDate"]),
                        VisitDate = r["VisitDate"] == DBNull.Value ? null : Convert.ToDateTime(r["VisitDate"]),
                        NumAdults = r["NumAdults"] == DBNull.Value ? 0 : Convert.ToInt32(r["NumAdults"]),
                        NumChildren = r["NumChildren"] == DBNull.Value ? 0 : Convert.ToInt32(r["NumChildren"]),
                        TotalAmount = r["TotalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["TotalAmount"]),
                        Status = r["Status"].ToString()!,
                        ArrivalTime = r["ArrivalTime"].ToString()!,
                        ModeOfTransport = r["ModeOfTransport"].ToString()!,
                        CabinName = r["CabinName"].ToString()!,
                        GuestName = $"{r["FirstName"]} {r["LastName"]}",
                        PaymentMethod = r["PaymentMethod"].ToString()!,
                        PaymentStatus = r["PaymentStatus"].ToString()!,
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading reservations: " + ex.Message);
            }
            return list;
        }

        // ═════════════════════════════════════════════════════════════
        //  BUILD ONE BOOKING CARD
        // ═════════════════════════════════════════════════════════════
        Panel BuildCard(ReservationRow row)
        {
            // Status colour
            bool isPast = (row.CheckOut ?? row.VisitDate ?? DateTime.Today) < DateTime.Today;
            bool isCancelled = row.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
            Color statusBg = isCancelled ? C_DangerBg
                             : isPast ? C_WarnBg
                             : C_SuccessBg;
            Color statusFg = isCancelled ? C_Danger
                             : isPast ? C_Warn
                             : C_Success;
            string statusTxt = isCancelled ? "Cancelled"
                             : isPast ? "Completed"
                             : "Confirmed";

            // Booking type icon + label
            (string icon, string label) = row.BookingType switch
            {
                "CabinStay" => ("🏕️", "Cabin Stay"),
                "DayVisit" => ("☀️", "Day Visit"),
                "ExperienceVisit" => ("🦁", "Experience"),
                "FullStayExperience" => ("🌿", "Full Stay + Experience"),
                _ => ("📋", row.BookingType)
            };

            // Date string
            string dateStr;
            if (row.CheckIn.HasValue && row.CheckOut.HasValue)
            {
                int nights = (row.CheckOut.Value - row.CheckIn.Value).Days;
                dateStr = $"{row.CheckIn:MMM dd} – {row.CheckOut:MMM dd, yyyy}  ·  {nights} night{(nights != 1 ? "s" : "")}";
            }
            else if (row.VisitDate.HasValue)
                dateStr = row.VisitDate.Value.ToString("MMM dd, yyyy") + "  ·  Day visit";
            else
                dateStr = "—";

            // Guests string
            string guestsStr = $"{row.NumAdults} adult{(row.NumAdults != 1 ? "s" : "")}";
            if (row.NumChildren > 0)
                guestsStr += $", {row.NumChildren} child{(row.NumChildren != 1 ? "ren" : "")}";

            // ── Card panel ────────────────────────────────────────
            var card = new Panel
            {
                Width = _flpCards.Width - 40,
                Height = 180,
                BackColor = C_White,
                Margin = new Padding(0, 0, 0, 14),
                Cursor = Cursors.Default
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                using var b = new SolidBrush(C_White);
                e.Graphics.FillPath(b, path);
                using var pen = new Pen(C_Border, 1f);
                e.Graphics.DrawPath(pen, path);
            };

            // Left colour stripe
            var stripe = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(5, card.Height),
                BackColor = isCancelled ? C_Danger : isPast ? Color.FromArgb(180, 140, 80) : C_Success
            };
            stripe.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundPath(new Rectangle(0, 0, stripe.Width - 1, stripe.Height - 1), 4);
                using var b = new SolidBrush(stripe.BackColor);
                e.Graphics.FillPath(b, path);
            };
            card.Controls.Add(stripe);

            // Icon box
            var iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 20f),
                Location = new Point(16, 16),
                Size = new Size(48, 48),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(iconLbl);

            // Main info block
            int mx = 72;
            AddLbl(card, label, mx, 14, new Font("Segoe UI", 8f, FontStyle.Bold), C_Muted);
            AddLbl(card, row.ReservationId, mx, 32, new Font("Georgia", 12f, FontStyle.Bold), C_Text);
            AddLbl(card, dateStr, mx, 58, new Font("Segoe UI", 8.8f), C_Muted);

            if (!string.IsNullOrEmpty(row.CabinName))
                AddLbl(card, $"🏕  {row.CabinName}", mx, 78, new Font("Segoe UI", 8.5f), C_Text);

            AddLbl(card, $"👥  {guestsStr}", mx, 98, new Font("Segoe UI", 8.5f), C_Muted);
            AddLbl(card, $"💳  {row.PaymentMethod}", mx, 116, new Font("Segoe UI", 8.5f), C_Muted);

            if (!string.IsNullOrEmpty(row.ArrivalTime))
                AddLbl(card, $"🕐  Arrival: {row.ArrivalTime}", mx, 134, new Font("Segoe UI", 8.5f), C_Muted);

            if (!string.IsNullOrEmpty(row.ModeOfTransport))
                AddLbl(card, $"🚗  Transport: {row.ModeOfTransport}", mx, 152, new Font("Segoe UI", 8.5f), C_Muted);

            // Total amount (top-right)
            AddLbl(card, $"₱{row.TotalAmount:N0}",
                card.Width - 120, 14, new Font("Georgia", 13f, FontStyle.Bold), C_Text);
            AddLbl(card, "Total",
                card.Width - 120, 36, new Font("Segoe UI", 7.5f), C_Dim);

            // Status badge (bottom-right)
            var badge = new Label
            {
                Text = statusTxt,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = statusFg,
                BackColor = statusBg,
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3)
            };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var b = new SolidBrush(statusBg);
                e.Graphics.FillRoundedRectangle(b, 0, 0, badge.Width, badge.Height, 10f);
            };
            card.Controls.Add(badge);

            card.Resize += (s, e) =>
            {
                badge.Location = new Point(card.Width - badge.Width - 12, card.Height - badge.Height - 12);
                stripe.Height = card.Height;
            };

            // Adjust card height based on how many lines we show
            int lines = 5;
            if (!string.IsNullOrEmpty(row.CabinName)) lines++;
            if (!string.IsNullOrEmpty(row.ArrivalTime)) lines++;
            if (!string.IsNullOrEmpty(row.ModeOfTransport)) lines++;
            card.Height = 40 + lines * 20 + 20;

            // Re-wire resize after final height
            card.PerformLayout();

            return card;
        }

        // ═════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════
        static void AddLbl(Panel p, string text, int x, int y, Font font, Color fore)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = font,
                ForeColor = fore,
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.Transparent
            });
        }

        static GraphicsPath RoundPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        static void DrawGlow(Graphics g, float cx, float cy, float radius, Color color)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using var br = new PathGradientBrush(path);
            br.CenterColor = color;
            br.SurroundColors = new[] { Color.FromArgb(0, color) };
            g.FillPath(br, path);
        }

        // ── Data model ───────────────────────────────────────────────
        class ReservationRow
        {
            public string ReservationId = "";
            public string BookingType = "";
            public DateTime? CheckIn = null;
            public DateTime? CheckOut = null;
            public DateTime? VisitDate = null;
            public int NumAdults = 0;
            public int NumChildren = 0;
            public decimal TotalAmount = 0;
            public string Status = "";
            public string ArrivalTime = "";
            public string ModeOfTransport = "";
            public string CabinName = "";
            public string GuestName = "";
            public string PaymentMethod = "";
            public string PaymentStatus = "";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  GraphicsExtensions  (rounded rect for GDI+)
    // ═══════════════════════════════════════════════════════════════
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(
            this Graphics g, Brush brush,
            float x, float y, float w, float h, float r)
        {
            float d = r * 2;
            using var path = new GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseAllFigures();
            g.FillPath(brush, path);
        }
    }
}