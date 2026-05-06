using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    /// <summary>
    /// Premium guest preview dialog shown BEFORE committing check-in.
    ///
    /// Flow:
    ///   QR scanned / ID entered
    ///     → UcCheckInConfirmDialog.ShowConfirm(parent, reservationId)
    ///         → fetches full guest + reservation row
    ///         → renders premium guest card
    ///         → staff reviews and clicks "Confirm Check-In" or "Cancel"
    ///     → returns DialogResult.OK  → caller calls ProcessCheckIn()
    ///        returns DialogResult.Cancel → do nothing
    ///
    /// This class is SELF-CONTAINED — just call ShowConfirm().
    /// It never writes to the database; that stays in ProcessCheckIn().
    /// </summary>
    public sealed class UcCheckInConfirmDialog : Form
    {
        // ── Design tokens — mirrors WildNest brand ────────────────────
        private static readonly Color InkDark    = Color.FromArgb(7,   26,  14);
        private static readonly Color InkMid     = Color.FromArgb(13,  46,  24);
        private static readonly Color Gold       = Color.FromArgb(212, 160, 23);
        private static readonly Color GoldLight  = Color.FromArgb(233, 192, 82);
        private static readonly Color Sage       = Color.FromArgb(143, 184, 154);
        private static readonly Color Cream      = Color.FromArgb(248, 244, 239);
        private static readonly Color Mist       = Color.FromArgb(232, 237, 233);
        private static readonly Color CardWhite  = Color.FromArgb(255, 255, 255);
        private static readonly Color TextDark   = Color.FromArgb(18,  42,  26);
        private static readonly Color TextMuted  = Color.FromArgb(110, 130, 115);

        // Status badge colours
        private static readonly Color StatusConfirmedBg   = Color.FromArgb(232, 245, 233);
        private static readonly Color StatusConfirmedFg   = Color.FromArgb(27,  94,  32);
        private static readonly Color StatusPendingBg     = Color.FromArgb(255, 243, 224);
        private static readonly Color StatusPendingFg     = Color.FromArgb(230, 81,  0);
        private static readonly Color StatusAlreadyBg     = Color.FromArgb(227, 242, 253);
        private static readonly Color StatusAlreadyFg     = Color.FromArgb(21,  101, 192);

        // ── Guest data struct ─────────────────────────────────────────
        private readonly GuestCardData _data;

        // ─────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Fetches guest data and shows the confirmation dialog.
        /// Returns DialogResult.OK if staff confirmed, Cancel otherwise.
        /// Returns DialogResult.Abort if the reservation is already invalid.
        /// </summary>
        public static DialogResult ShowConfirm(IWin32Window owner, string reservationId)
        {
            var data = FetchGuestData(reservationId);

            if (data == null)
            {
                MessageBox.Show(
                    $"No reservation found for: {reservationId}\n\nPlease verify the Booking ID.",
                    "WildNest — Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return DialogResult.Abort;
            }

            // Already checked in — show info dialog instead of confirm
            if (data.Status.Equals("Checked-In", StringComparison.OrdinalIgnoreCase) ||
                data.Status.Equals("Overdue",    StringComparison.OrdinalIgnoreCase))
            {
                using var info = new UcCheckInConfirmDialog(data, readOnly: true);
                info.ShowDialog(owner);
                return DialogResult.Abort;
            }

            // Blocked statuses — inform and return
            if (data.Status.Equals("Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                data.Status.Equals("Cancelled",   StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"This reservation is marked '{data.Status}' and cannot be checked in.",
                    "WildNest — Cannot Check In",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return DialogResult.Abort;
            }

            using var dlg = new UcCheckInConfirmDialog(data, readOnly: false);
            return dlg.ShowDialog(owner);
        }

        // ─────────────────────────────────────────────────────────────
        // Constructor — builds the entire UI programmatically
        // ─────────────────────────────────────────────────────────────

        private UcCheckInConfirmDialog(GuestCardData data, bool readOnly)
        {
            _data = data;

            // ── Form chrome ───────────────────────────────────────────
            Text            = readOnly
                ? $"WildNest — Already Checked In  ·  {data.ReservationId}"
                : $"WildNest — Confirm Check-In  ·  {data.ReservationId}";
            Size            = new Size(580, 640);
            MinimumSize     = new Size(520, 580);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = InkDark;
            Padding         = new Padding(0);

            BuildLayout(readOnly);
        }

        // ─────────────────────────────────────────────────────────────
        // Layout builder
        // ─────────────────────────────────────────────────────────────

        private void BuildLayout(bool readOnly)
        {
            // ── Outer scroll panel (handles small screens) ────────────
            var scroll = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = InkDark,
                Padding    = new Padding(28, 24, 28, 24)
            };
            Controls.Add(scroll);

            int y = 24;

            // ── Header strip ──────────────────────────────────────────
            var header = new Panel
            {
                Location  = new Point(28, y),
                Width     = ClientSize.Width - 56,
                Height    = 72,
                BackColor = Color.Transparent
            };

            // WildNest wordmark
            var wordmark = new Label
            {
                Text      = "🦁  WILDNEST",
                Font      = new Font("Georgia", 18f, FontStyle.Bold),
                ForeColor = Gold,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(0, 8)
            };
            var subline = new Label
            {
                Text      = "Zoo Resort & Wildlife Experience  ·  Reception Check-In",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Sage,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(2, 38)
            };
            header.Controls.Add(wordmark);
            header.Controls.Add(subline);
            scroll.Controls.Add(header);
            y += 88;

            // ── Gold divider ──────────────────────────────────────────
            scroll.Controls.Add(MakeHRule(28, y, ClientSize.Width - 56));
            y += 16;

            // ── Guest card ────────────────────────────────────────────
            var card = BuildGuestCard(readOnly);
            card.Location = new Point(28, y);
            card.Width    = ClientSize.Width - 56;
            scroll.Controls.Add(card);
            y += card.Height + 18;

            // ── Booking details card ──────────────────────────────────
            var detailCard = BuildDetailsCard();
            detailCard.Location = new Point(28, y);
            detailCard.Width    = ClientSize.Width - 56;
            scroll.Controls.Add(detailCard);
            y += detailCard.Height + 22;

            // ── Action buttons ────────────────────────────────────────
            if (!readOnly)
            {
                var btnConfirm = MakeButton(
                    "✔   Confirm Check-In",
                    Color.FromArgb(27, 94, 32),
                    Color.FromArgb(200, 230, 201),
                    new Point(28, y),
                    (ClientSize.Width - 56 - 12) / 2,
                    48);
                btnConfirm.Font   = new Font("Segoe UI Semibold", 11f, FontStyle.Bold);
                btnConfirm.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

                var btnCancel = MakeButton(
                    "✕   Cancel",
                    Color.FromArgb(60, 60, 60),
                    Color.FromArgb(200, 200, 200),
                    new Point(28 + btnConfirm.Width + 12, y),
                    (ClientSize.Width - 56 - 12) / 2,
                    48);
                btnCancel.Font   = new Font("Segoe UI", 10f, FontStyle.Regular);
                btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                scroll.Controls.Add(btnConfirm);
                scroll.Controls.Add(btnCancel);
                y += 60;
            }
            else
            {
                var btnClose = MakeButton(
                    "Close",
                    InkMid,
                    Sage,
                    new Point(28, y),
                    ClientSize.Width - 56,
                    44);
                btnClose.Click += (s, e) => Close();
                scroll.Controls.Add(btnClose);
                y += 56;
            }

            // ── Footer note ───────────────────────────────────────────
            var footer = new Label
            {
                Text      = "WildNest Zoo Resort  ·  Carmen, Cebu, Philippines  ·  This action is logged to the reservation audit trail.",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 110, 88),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Width     = ClientSize.Width - 56,
                Height    = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(28, y)
            };
            scroll.Controls.Add(footer);
        }

        // ─────────────────────────────────────────────────────────────
        // Guest identity card (top card)
        // ─────────────────────────────────────────────────────────────

        private Panel BuildGuestCard(bool readOnly)
        {
            var card = new Panel
            {
                Height    = 130,
                BackColor = CardWhite,
                Padding   = new Padding(20)
            };
            MakeRoundedPanel(card, 12);

            // Left: avatar circle with initials
            int avatarSize = 72;
            var avatar = new Panel
            {
                Size      = new Size(avatarSize, avatarSize),
                Location  = new Point(20, (card.Height - avatarSize) / 2),
                BackColor = Color.Transparent
            };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Gradient circle
                using var brush = new LinearGradientBrush(
                    new Rectangle(0, 0, avatarSize, avatarSize),
                    InkDark, InkMid, 135f);
                g.FillEllipse(brush, 0, 0, avatarSize - 1, avatarSize - 1);
                // Gold ring
                using var pen = new Pen(Gold, 2f);
                g.DrawEllipse(pen, 1, 1, avatarSize - 3, avatarSize - 3);
                // Initials
                string initials = GetInitials(_data.GuestName);
                using var font  = new Font("Georgia", 22f, FontStyle.Bold);
                var sz           = g.MeasureString(initials, font);
                g.DrawString(initials, font, new SolidBrush(Gold),
                    (avatarSize - sz.Width) / 2f,
                    (avatarSize - sz.Height) / 2f);
            };
            card.Controls.Add(avatar);

            // Right: name, ref, status badge
            int rx = avatarSize + 32;

            var nameLabel = new Label
            {
                Text      = _data.GuestName,
                Font      = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = TextDark,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(rx, 22)
            };

            var refLabel = new Label
            {
                Text      = _data.ReservationId,
                Font      = new Font("Consolas", 10f, FontStyle.Regular),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(rx, nameLabel.Bottom + 4)
            };

            // Status badge
            (Color bg, Color fg) = GetStatusColors(_data.Status);
            var badge = new Label
            {
                Text      = "  " + _data.Status.ToUpperInvariant() + "  ",
                Font      = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                AutoSize  = true,
                Location  = new Point(rx, refLabel.Bottom + 8),
                Padding   = new Padding(2)
            };
            MakeRoundedLabel(badge, 6);

            // Read-only notice
            if (readOnly)
            {
                var notice = new Label
                {
                    Text      = "ℹ  This guest is already checked in.",
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = StatusAlreadyFg,
                    BackColor = StatusAlreadyBg,
                    AutoSize  = true,
                    Location  = new Point(rx, badge.Bottom + 8),
                    Padding   = new Padding(4, 2, 4, 2)
                };
                card.Controls.Add(notice);
            }

            card.Controls.Add(nameLabel);
            card.Controls.Add(refLabel);
            card.Controls.Add(badge);
            return card;
        }

        // ─────────────────────────────────────────────────────────────
        // Booking detail card (bottom card)
        // ─────────────────────────────────────────────────────────────

        private Panel BuildDetailsCard()
        {
            // Calculate row count to set card height
            var rows = new (string Label, string Value)[]
            {
                ("Booking Type",    _data.BookingType),
                ("Accommodation",   _data.Cabin),
                ("Date / Stay",     _data.DateInfo),
                ("Arrival Time",    _data.ArrivalTime),
                ("Total Amount",    $"₱{_data.TotalAmount:N2}"),
                ("Payment Method",  _data.PaymentMethod),
                ("Booking Status",  _data.Status),
                ("No. of Guests",   _data.GuestCount > 0 ? _data.GuestCount.ToString() : "—"),
                ("Special Request", _data.SpecialRequest),
            };

            int rowH  = 38;
            int padV  = 20;
            int cardH = padV + rows.Length * rowH + padV + 10;

            var card = new Panel
            {
                Height    = cardH,
                BackColor = CardWhite,
                Padding   = new Padding(0)
            };
            MakeRoundedPanel(card, 12);

            // Section title
            var title = new Label
            {
                Text      = "BOOKING DETAILS",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(20, 14)
            };
            card.Controls.Add(title);

            // Thin separator
            var sep = MakeHRule(0, 34, card.Width);
            sep.BackColor = Mist;
            card.Controls.Add(sep);

            int y = 40;
            bool alt = false;
            foreach (var (lbl, val) in rows)
            {
                string display = string.IsNullOrWhiteSpace(val) ? "—" : val;

                var row = new Panel
                {
                    Location  = new Point(0, y),
                    Height    = rowH,
                    BackColor = alt ? Color.FromArgb(250, 252, 250) : CardWhite
                };
                // Row will be sized in Resize handler
                card.Resize += (s, e) => row.Width = card.Width;
                row.Width = card.Width;

                var lblCtrl = new Label
                {
                    Text      = lbl,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                    ForeColor = TextMuted,
                    BackColor = Color.Transparent,
                    AutoSize  = false,
                    Width     = 148,
                    Height    = rowH,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location  = new Point(20, 0)
                };

                bool isAmount = lbl == "Total Amount";
                var valCtrl = new Label
                {
                    Text      = display,
                    Font      = isAmount
                        ? new Font("Segoe UI Semibold", 11f, FontStyle.Bold)
                        : new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = isAmount ? TextDark : TextDark,
                    BackColor = Color.Transparent,
                    AutoSize  = false,
                    Height    = rowH,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location  = new Point(172, 0)
                };
                // Value fills remaining width
                card.Resize += (s, e) => valCtrl.Width = card.Width - 172 - 20;
                valCtrl.Width = card.Width - 172 - 20;

                row.Controls.Add(lblCtrl);
                row.Controls.Add(valCtrl);
                card.Controls.Add(row);
                y   += rowH;
                alt  = !alt;
            }

            return card;
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers — UI primitives
        // ─────────────────────────────────────────────────────────────

        private static Panel MakeHRule(int x, int y, int width)
        {
            return new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(width, 1),
                BackColor = Color.FromArgb(212, 160, 23)   // gold rule
            };
        }

        private static Button MakeButton(string text, Color backColor, Color foreColor,
                                          Point location, int width, int height)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = location,
                Size      = new Size(width, height),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 10f, FontStyle.Regular)
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor =
                ControlPaint.Light(backColor, 0.15f);

            // Rounded corners via Region on Paint
            btn.Paint += (s, e) =>
            {
                var b = (Button)s!;
                using var path = RoundedRect(new Rectangle(0, 0, b.Width - 1, b.Height - 1), 8);
                b.Region = new Region(path);
            };
            return btn;
        }

        private static void MakeRoundedPanel(Panel p, int radius)
        {
            p.Paint += (s, e) =>
            {
                var panel = (Panel)s!;
                using var path = RoundedRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), radius);
                panel.Region = new Region(path);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(230, 230, 230), 1f);
                e.Graphics.DrawPath(pen, path);
            };
        }

        private static void MakeRoundedLabel(Label lbl, int radius)
        {
            lbl.Paint += (s, e) =>
            {
                var l = (Label)s!;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = RoundedRect(new Rectangle(0, 0, l.Width - 1, l.Height - 1), radius);
                using var brush = new SolidBrush(l.BackColor);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawString(l.Text, l.Font, new SolidBrush(l.ForeColor), 2f, 2f);
                l.Region = new Region(path);
            };
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d    = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X,                       bounds.Y,                        d, d, 180, 90);
            path.AddArc(bounds.X + bounds.Width - d,    bounds.Y,                        d, d, 270, 90);
            path.AddArc(bounds.X + bounds.Width - d,    bounds.Y + bounds.Height - d,    d, d,   0, 90);
            path.AddArc(bounds.X,                       bounds.Y + bounds.Height - d,    d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        private static (Color bg, Color fg) GetStatusColors(string status) => status switch
        {
            "Confirmed"   => (StatusConfirmedBg, StatusConfirmedFg),
            "Pending"     => (StatusPendingBg,   StatusPendingFg),
            "Checked-In"  => (StatusAlreadyBg,   StatusAlreadyFg),
            "Overdue"     => (Color.FromArgb(255, 243, 224), Color.FromArgb(230, 81, 0)),
            "Cancelled"   => (Color.FromArgb(245, 245, 245), Color.FromArgb(97,  97, 97)),
            _             => (StatusConfirmedBg, StatusConfirmedFg)
        };

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
                : name[0].ToString().ToUpperInvariant();
        }

        // ─────────────────────────────────────────────────────────────
        // Database fetch — parameterized, single query
        // ─────────────────────────────────────────────────────────────

        private static GuestCardData? FetchGuestData(string reservationId)
        {
            var table = StaffPortalDb.GetTable(@"
SELECT
    r.ReservationID,
    r.BookingType,
    r.Status,
    r.CheckInDate,
    r.CheckOutDate,
    r.VisitDate,
    r.ArrivalTime,
    r.TotalAmount,
    r.PaymentMethod,
    r.GuestCount,
    r.SpecialRequest,
    CONCAT(g.FirstName, ' ', g.LastName)              AS GuestName,
    COALESCE(c.CabinName, 'Day Visit / Experience')   AS Cabin
FROM   tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE  r.ReservationID = @id
LIMIT  1;",
                new MySqlParameter("@id", reservationId));

            if (table == null || table.Rows.Count == 0) return null;

            var dr = table.Rows[0];

            string bookingType = dr["BookingType"]?.ToString() ?? string.Empty;
            string dateInfo    = BuildDateInfo(dr, bookingType);

            return new GuestCardData
            {
                ReservationId  = reservationId,
                GuestName      = dr["GuestName"]?.ToString()      ?? "Unknown Guest",
                BookingType    = bookingType,
                Cabin          = dr["Cabin"]?.ToString()          ?? "—",
                DateInfo       = dateInfo,
                ArrivalTime    = dr["ArrivalTime"]?.ToString()    ?? "Front Desk Confirmation",
                TotalAmount    = Convert.ToDecimal(dr["TotalAmount"]    ?? 0m),
                PaymentMethod  = dr["PaymentMethod"]?.ToString()  ?? "—",
                Status         = dr["Status"]?.ToString()         ?? "Confirmed",
                GuestCount     = dr["GuestCount"]  == DBNull.Value ? 0 : Convert.ToInt32(dr["GuestCount"]),
                SpecialRequest = dr["SpecialRequest"]?.ToString() ?? string.Empty
            };
        }

        private static string BuildDateInfo(System.Data.DataRow dr, string bookingType)
        {
            try
            {
                bool overnight =
                    bookingType.Contains("Overnight", StringComparison.OrdinalIgnoreCase) ||
                    bookingType.Contains("Stay",      StringComparison.OrdinalIgnoreCase);

                if (overnight)
                {
                    var ci = Convert.ToDateTime(dr["CheckInDate"]);
                    var co = Convert.ToDateTime(dr["CheckOutDate"]);
                    int nights = (co - ci).Days;
                    return $"{ci:dddd, MMMM d} → {co:MMMM d, yyyy}  ({nights} night{(nights == 1 ? "" : "s")})";
                }

                var visit = Convert.ToDateTime(dr["VisitDate"]);
                return visit.ToString("dddd, MMMM d, yyyy");
            }
            catch { return "—"; }
        }

        // ─────────────────────────────────────────────────────────────
        // Data model
        // ─────────────────────────────────────────────────────────────

        private sealed class GuestCardData
        {
            public string  ReservationId  { get; init; } = string.Empty;
            public string  GuestName      { get; init; } = string.Empty;
            public string  BookingType    { get; init; } = string.Empty;
            public string  Cabin          { get; init; } = string.Empty;
            public string  DateInfo       { get; init; } = string.Empty;
            public string  ArrivalTime    { get; init; } = string.Empty;
            public decimal TotalAmount    { get; init; }
            public string  PaymentMethod  { get; init; } = string.Empty;
            public string  Status         { get; init; } = string.Empty;
            public int     GuestCount     { get; init; }
            public string  SpecialRequest { get; init; } = string.Empty;
        }
    }
}
