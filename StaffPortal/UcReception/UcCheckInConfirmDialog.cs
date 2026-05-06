using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    internal sealed class UcCheckInConfirmDialog : Form
    {
        private readonly GuestCheckInPreview _preview;

        private UcCheckInConfirmDialog(GuestCheckInPreview preview, bool readOnlyMode)
        {
            _preview = preview;

            Text = readOnlyMode
                ? $"WildNest Guest Preview - {preview.ReservationId}"
                : $"Confirm Guest Check-In - {preview.ReservationId}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(20, 16, 12);
            Size = new Size(1040, readOnlyMode ? 720 : 780);
            MinimumSize = new Size(980, readOnlyMode ? 700 : 760);

            BuildShell(readOnlyMode);
        }

        internal static DialogResult ShowConfirm(IWin32Window owner, string reservationId)
        {
            var preview = LoadPreview(reservationId);
            if (preview == null)
            {
                return StaffPortalUi.ShowEliteMessage(
                    owner,
                    "Guest Not Found",
                    $"No reservation matched {reservationId}. Please verify the reservation reference or rescan the QR code.",
                    StaffPortalUi.MessageTone.Warning,
                    "Close");
            }

            if (preview.Status.Equals("Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                preview.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return StaffPortalUi.ShowEliteMessage(
                    owner,
                    "Check-In Blocked",
                    $"This reservation is marked {preview.Status} and can no longer be checked in.",
                    StaffPortalUi.MessageTone.Warning,
                    "Close");
            }

            bool readOnlyMode =
                preview.Status.Equals("Checked-In", StringComparison.OrdinalIgnoreCase) ||
                preview.Status.Equals("Overdue", StringComparison.OrdinalIgnoreCase);

            using var dialog = new UcCheckInConfirmDialog(preview, readOnlyMode);
            return dialog.ShowDialog(owner);
        }

        private void BuildShell(bool readOnlyMode)
        {
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(26, 20, 16)
            };
            Controls.Add(overlay);

            var shell = new Panel
            {
                Size = new Size(Math.Min(Width - 40, 960), Math.Min(Height - 40, readOnlyMode ? 660 : 724)),
                BackColor = Color.FromArgb(248, 244, 239)
            };
            overlay.Controls.Add(shell);

            overlay.Resize += (s, e) =>
            {
                shell.Location = new Point(
                    Math.Max(20, (overlay.ClientSize.Width - shell.Width) / 2),
                    Math.Max(20, (overlay.ClientSize.Height - shell.Height) / 2));
            };
            shell.Location = new Point(
                Math.Max(20, (overlay.ClientSize.Width - shell.Width) / 2),
                Math.Max(20, (overlay.ClientSize.Height - shell.Height) / 2));

            shell.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var shadow = new SolidBrush(Color.FromArgb(36, 0, 0, 0));
                e.Graphics.FillRectangle(shadow, new Rectangle(10, 12, shell.Width - 20, shell.Height - 20));
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, shell.Width - 1, shell.Height - 1), 18);
                using var fill = new SolidBrush(Color.FromArgb(248, 244, 239));
                using var border = new Pen(Color.FromArgb(230, 223, 212), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 138,
                BackColor = Color.Transparent
            };
            header.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(header.ClientRectangle,
                    Color.FromArgb(9, 32, 17),
                    Color.FromArgb(30, 113, 57),
                    0f);
                e.Graphics.FillRectangle(brush, header.ClientRectangle);
                using var accent = new SolidBrush(Color.FromArgb(30, WildNestUI.Gold));
                e.Graphics.FillEllipse(accent, -28, -18, 86, 86);
                e.Graphics.FillEllipse(accent, header.Width - 86, header.Height - 58, 114, 114);
            };
            shell.Controls.Add(header);

            var title = new Label
            {
                Text = readOnlyMode ? "Guest Already Checked In" : "Guest Arrival Clearance",
                Font = WildNestUI.FontTitle(19f),
                ForeColor = WildNestUI.Cream,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(28, 24)
            };
            header.Controls.Add(title);

            var headerBadge = WildNestUI.Badge(
                readOnlyMode ? "ACTIVE RESERVATION" : "RECEPTION CONTROL",
                readOnlyMode ? BadgeStyle.Blue : BadgeStyle.Green);
            headerBadge.Location = new Point(30, 60);
            header.Controls.Add(headerBadge);

            var subtitle = new Label
            {
                Text = readOnlyMode
                    ? "This reservation is already active in the live arrival workflow."
                    : "Confirm the guest profile, stay details, and payment context before committing check-in.",
                Font = WildNestUI.FontBody(10f),
                ForeColor = Color.FromArgb(222, 248, 244, 239),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(680, 42),
                Location = new Point(30, 84)
            };
            header.Controls.Add(subtitle);

            var crest = new Panel
            {
                Size = new Size(58, 58),
                BackColor = Color.FromArgb(24, 255, 255, 255),
                Location = new Point(shell.Width - 166, 26)
            };
            crest.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, crest.Width - 1, crest.Height - 1), 29);
                using var fill = new SolidBrush(Color.FromArgb(24, 255, 255, 255));
                e.Graphics.FillPath(fill, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    "WN",
                    WildNestUI.FontBold(10f),
                    crest.ClientRectangle,
                    WildNestUI.Gold,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            header.Controls.Add(crest);

            var close = new Button
            {
                Text = "Close",
                FlatStyle = FlatStyle.Flat,
                Font = WildNestUI.FontBold(8.8f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.FromArgb(26, 255, 255, 255),
                Size = new Size(82, 34),
                Location = new Point(shell.Width - 104, 18),
                Cursor = Cursors.Hand
            };
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 255, 255, 255);
            close.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            header.Controls.Add(close);

            header.Resize += (s, e) =>
            {
                crest.Location = new Point(header.Width - 166, 26);
                close.Location = new Point(header.Width - 104, 18);
            };

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(28, 24, 28, 24)
            };
            shell.Controls.Add(content);

            var identityCard = BuildIdentityCard();
            identityCard.Location = new Point(0, 0);
            identityCard.Width = shell.Width - 56;
            content.Controls.Add(identityCard);

            var summaryStrip = BuildSummaryStrip();
            summaryStrip.Location = new Point(0, identityCard.Bottom + 16);
            summaryStrip.Width = shell.Width - 56;
            content.Controls.Add(summaryStrip);

            var detailCard = BuildDetailsCard();
            detailCard.Location = new Point(0, summaryStrip.Bottom + 16);
            detailCard.Width = shell.Width - 56;
            content.Controls.Add(detailCard);

            var footer = new Panel
            {
                Width = shell.Width - 56,
                Height = readOnlyMode ? 84 : 96,
                Location = new Point(0, detailCard.Bottom + 18),
                BackColor = Color.Transparent
            };
            content.Controls.Add(footer);

            var statusNote = new Label
            {
                Text = readOnlyMode
                    ? "This reservation is already running in the active guest queue. No second check-in is allowed from this screen."
                    : "Reception confirmation will set the reservation status to Checked-In and, if applicable, switch the assigned cabin to Occupied.",
                Font = WildNestUI.FontBody(9.3f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(footer.Width - 420, 58),
                Location = new Point(0, 0)
            };
            footer.Controls.Add(statusNote);

            Button? closeOnlyButton = null;
            Button? cancelButton = null;
            Button? confirmButton = null;

            if (readOnlyMode)
            {
                closeOnlyButton = WildNestUI.BtnPrimary("Close Preview", 170, 42);
                closeOnlyButton.Click += (s, e) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                footer.Controls.Add(closeOnlyButton);
            }
            else
            {
                cancelButton = WildNestUI.BtnOutline("Cancel", 122, 42);
                cancelButton.Click += (s, e) =>
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                };
                footer.Controls.Add(cancelButton);

                confirmButton = WildNestUI.BtnPrimary("Confirm Check-In", 172, 42);
                confirmButton.Click += (s, e) =>
                {
                    DialogResult = DialogResult.OK;
                    Close();
                };
                footer.Controls.Add(confirmButton);
                AcceptButton = confirmButton;
            }

            void LayoutFooter()
            {
                const int top = 26;
                const int gap = 10;
                const int rightPadding = 6;

                if (closeOnlyButton != null)
                {
                    closeOnlyButton.Location = new Point(
                        footer.Width - closeOnlyButton.Width - rightPadding,
                        top);
                }

                if (cancelButton != null && confirmButton != null)
                {
                    confirmButton.Location = new Point(
                        footer.Width - confirmButton.Width - rightPadding,
                        top);
                    cancelButton.Location = new Point(
                        confirmButton.Left - gap - cancelButton.Width,
                        top);
                }

                int reservedWidth =
                    closeOnlyButton != null ? closeOnlyButton.Width + 34 :
                    (cancelButton != null && confirmButton != null ? cancelButton.Width + confirmButton.Width + gap + 34 : 320);

                statusNote.Size = new Size(
                    Math.Max(340, footer.Width - reservedWidth),
                    statusNote.Height);
            }

            LayoutFooter();

            content.Resize += (s, e) =>
            {
                int width = content.ClientSize.Width;
                identityCard.Width = width;
                summaryStrip.Width = width;
                detailCard.Width = width;
                footer.Width = width;
                summaryStrip.Location = new Point(0, identityCard.Bottom + 16);
                detailCard.Location = new Point(0, summaryStrip.Bottom + 16);
                footer.Location = new Point(0, detailCard.Bottom + 18);
                LayoutFooter();
            };
        }

        private Panel BuildIdentityCard()
        {
            var card = new Panel
            {
                Height = 144,
                BackColor = Color.White
            };
            StyleCard(card);

            var avatar = new Panel
            {
                Size = new Size(84, 84),
                Location = new Point(24, 28),
                BackColor = Color.Transparent
            };
            avatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new LinearGradientBrush(avatar.ClientRectangle, WildNestUI.Forest, WildNestUI.ForestL, 45f);
                e.Graphics.FillEllipse(brush, 0, 0, avatar.Width - 1, avatar.Height - 1);
                using var ring = new Pen(WildNestUI.Gold, 2f);
                e.Graphics.DrawEllipse(ring, 2, 2, avatar.Width - 5, avatar.Height - 5);
                TextRenderer.DrawText(
                    e.Graphics,
                    BuildInitials(_preview.GuestName),
                    new Font("Georgia", 20f, FontStyle.Bold),
                    avatar.ClientRectangle,
                    WildNestUI.Gold,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            card.Controls.Add(avatar);

            var guestName = new Label
            {
                Text = _preview.GuestName,
                Font = WildNestUI.FontTitle(16f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(128, 24)
            };
            card.Controls.Add(guestName);

            var reservationRef = new Label
            {
                Text = _preview.ReservationId,
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                ForeColor = WildNestUI.Muted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(130, 58)
            };
            card.Controls.Add(reservationRef);

            var bookingType = new Label
            {
                Text = _preview.BookingType + " | " + _preview.StayDescriptor,
                Font = WildNestUI.FontBody(9.6f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(130, 84)
            };
            card.Controls.Add(bookingType);

            var statusBadge = BuildBadge(_preview.Status, ResolveStatusBadge(_preview.Status));
            statusBadge.Location = new Point(card.Width - statusBadge.Width - 24, 28);
            card.Controls.Add(statusBadge);

            card.Resize += (s, e) =>
            {
                statusBadge.Location = new Point(card.Width - statusBadge.Width - 24, 28);
            };

            return card;
        }

        private Panel BuildSummaryStrip()
        {
            var strip = new Panel
            {
                Height = 102,
                BackColor = Color.White
            };
            StyleCard(strip);

            AddSummaryMetric(strip, 24, "Schedule", _preview.DateDisplay, 250);
            AddSummaryMetric(strip, 292, "Guest Count", _preview.GuestCountDisplay, 180);
            AddSummaryMetric(strip, 492, "Payment", _preview.PaymentProfile, 180);

            var paymentBadge = BuildBadge(_preview.PaymentStatus, ResolvePaymentBadge(_preview.PaymentStatus));
            paymentBadge.Location = new Point(strip.Width - paymentBadge.Width - 24, 24);
            strip.Controls.Add(paymentBadge);

            var refLine = new Label
            {
                Text = "Live reservation status and payment context shown above before check-in commit.",
                Font = WildNestUI.FontBody(8.9f),
                ForeColor = WildNestUI.Muted,
                AutoSize = false,
                Size = new Size(300, 32),
                BackColor = Color.Transparent,
                Location = new Point(strip.Width - 324, 54)
            };
            strip.Controls.Add(refLine);

            strip.Resize += (s, e) =>
            {
                paymentBadge.Location = new Point(strip.Width - paymentBadge.Width - 24, 24);
                refLine.Location = new Point(strip.Width - 324, 54);
            };

            return strip;
        }

        private Panel BuildDetailsCard()
        {
            var card = new Panel
            {
                Height = 314,
                BackColor = Color.White
            };
            StyleCard(card);

            AddDetail(card, 24, "Accommodation", _preview.CabinDisplay);
            AddDetail(card, 24, "Stay Window", _preview.DateDisplay, 70);
            AddDetail(card, 24, "Arrival Time", _preview.ArrivalTimeDisplay, 128);
            AddDetail(card, 24, "Special Requests", _preview.SpecialRequests, 186, 4);

            AddDetail(card, 486, "Guest Count", _preview.GuestCountDisplay);
            AddDetail(card, 486, "Total Amount", _preview.TotalAmountDisplay, 70);
            AddDetail(card, 486, "Reservation Status", _preview.Status, 128);
            AddDetail(card, 486, "Payment Profile", _preview.PaymentProfile, 186);
            AddDetail(card, 486, "Payment Status", _preview.PaymentStatus, 244);

            return card;
        }

        private void AddDetail(Panel parent, int x, string label, string value, int y = 24, int lines = 1)
        {
            var caption = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = WildNestUI.FontLabel(8.3f),
                ForeColor = WildNestUI.Amber,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
            parent.Controls.Add(caption);

            var body = new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "None recorded" : value,
                Font = WildNestUI.FontBody(10f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = false,
                BackColor = Color.Transparent,
                Location = new Point(x, y + 20),
                Size = new Size(360, lines > 1 ? 66 : 30)
            };
            parent.Controls.Add(body);
        }

        private void AddSummaryMetric(Panel parent, int x, string label, string value, int width)
        {
            parent.Controls.Add(new Label
            {
                Text = label.ToUpperInvariant(),
                Font = WildNestUI.FontLabel(8.1f),
                ForeColor = WildNestUI.Amber,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, 20)
            });

            parent.Controls.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(value) ? "Not recorded" : value,
                Font = WildNestUI.FontBody(9.6f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = false,
                Size = new Size(width, 38),
                BackColor = Color.Transparent,
                Location = new Point(x, 42)
            });
        }

        private static Label BuildBadge(string text, BadgeStyle style)
        {
            return WildNestUI.Badge(text, style);
        }

        private static BadgeStyle ResolveStatusBadge(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "confirmed" => BadgeStyle.Green,
                "pending" => BadgeStyle.Amber,
                "checked-in" => BadgeStyle.Blue,
                "overdue" => BadgeStyle.Amber,
                "cancelled" => BadgeStyle.Red,
                "checked-out" => BadgeStyle.Gray,
                _ => BadgeStyle.Gray
            };
        }

        private static BadgeStyle ResolvePaymentBadge(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "paid" => BadgeStyle.Green,
                "completed" => BadgeStyle.Green,
                "partial" => BadgeStyle.Blue,
                "pending" => BadgeStyle.Amber,
                "failed" => BadgeStyle.Red,
                "cancelled" => BadgeStyle.Red,
                _ => BadgeStyle.Gray
            };
        }

        private static void StyleCard(Panel panel)
        {
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 16);
                using var fill = new SolidBrush(Color.White);
                using var border = new Pen(Color.FromArgb(227, 221, 212), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "WN";

            string[] parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, 1).ToUpperInvariant();

            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpperInvariant();
        }

        private static GuestCheckInPreview? LoadPreview(string reservationId)
        {
            const string sql = @"
SELECT
    r.ReservationID,
    r.BookingType,
    r.Status,
    r.CheckInDate,
    r.CheckOutDate,
    r.VisitDate,
    r.ArrivalTime,
    r.TotalAmount,
    r.NumAdults,
    r.NumChildren,
    CONCAT(COALESCE(g.FirstName, ''), ' ', COALESCE(g.LastName, '')) AS GuestName,
    COALESCE(c.CabinName, 'Day Visit / Experience') AS CabinName,
    COALESCE(g.SpecialRequests, '') AS SpecialRequests,
    COALESCE(
        (
            SELECT p.PaymentMethod
            FROM tbl_payments p
            WHERE p.ReservationID = r.ReservationID
            ORDER BY COALESCE(p.PaidAt, '1900-01-01') DESC, p.PaymentID DESC
            LIMIT 1
        ),
        'Not yet recorded'
    ) AS PaymentMethod,
    COALESCE(
        (
            SELECT p.Status
            FROM tbl_payments p
            WHERE p.ReservationID = r.ReservationID
            ORDER BY COALESCE(p.PaidAt, '1900-01-01') DESC, p.PaymentID DESC
            LIMIT 1
        ),
        'Pending'
    ) AS PaymentStatus
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.ReservationID = @id
LIMIT 1;";

            var table = StaffPortalDb.GetTable(sql, new MySqlParameter("@id", reservationId));
            if (table.Rows.Count == 0)
                return null;

            DataRow row = table.Rows[0];
            string bookingType = StaffPortalUi.SafeString(row["BookingType"], "Reservation");
            int adults = row["NumAdults"] == DBNull.Value ? 0 : Convert.ToInt32(row["NumAdults"]);
            int children = row["NumChildren"] == DBNull.Value ? 0 : Convert.ToInt32(row["NumChildren"]);

            return new GuestCheckInPreview
            {
                ReservationId = reservationId,
                GuestName = StaffPortalUi.SafeString(row["GuestName"], "Unknown Guest"),
                BookingType = bookingType,
                Status = StaffPortalUi.SafeString(row["Status"], "Confirmed"),
                CabinDisplay = StaffPortalUi.SafeString(row["CabinName"], "Day Visit / Experience"),
                StayDescriptor = BuildStayDescriptor(bookingType),
                DateDisplay = BuildDateDisplay(row, bookingType),
                ArrivalTimeDisplay = BuildArrivalDisplay(row),
                PaymentProfile = StaffPortalUi.SafeString(row["PaymentMethod"], "Not yet recorded"),
                PaymentStatus = StaffPortalUi.SafeString(row["PaymentStatus"], "Pending"),
                TotalAmountDisplay = StaffPortalUi.Peso(row["TotalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["TotalAmount"])),
                SpecialRequests = StaffPortalUi.SafeString(row["SpecialRequests"], "None recorded"),
                GuestCountDisplay = BuildGuestCountDisplay(adults, children)
            };
        }

        private static string BuildArrivalDisplay(DataRow row)
        {
            string arrival = StaffPortalUi.SafeString(row["ArrivalTime"], string.Empty);
            return string.IsNullOrWhiteSpace(arrival) ? "Front desk confirmation" : arrival;
        }

        private static string BuildStayDescriptor(string bookingType)
        {
            if (bookingType.Contains("Day", StringComparison.OrdinalIgnoreCase))
                return "Same-day guest access";
            if (bookingType.Contains("Experience", StringComparison.OrdinalIgnoreCase))
                return "Experience-led arrival";
            return "Resort stay arrival";
        }

        private static string BuildDateDisplay(DataRow row, string bookingType)
        {
            bool stayBooking =
                bookingType.Contains("Stay", StringComparison.OrdinalIgnoreCase) ||
                bookingType.Contains("Cabin", StringComparison.OrdinalIgnoreCase) ||
                bookingType.Contains("Overnight", StringComparison.OrdinalIgnoreCase);

            if (stayBooking && row["CheckInDate"] != DBNull.Value)
            {
                DateTime checkIn = Convert.ToDateTime(row["CheckInDate"]);
                DateTime checkOut = row["CheckOutDate"] == DBNull.Value ? checkIn : Convert.ToDateTime(row["CheckOutDate"]);
                int nights = Math.Max(0, (checkOut - checkIn).Days);
                return $"{checkIn:dddd, MMMM d, yyyy} to {checkOut:dddd, MMMM d, yyyy} ({nights} night{(nights == 1 ? string.Empty : "s")})";
            }

            if (row["VisitDate"] != DBNull.Value)
            {
                DateTime visitDate = Convert.ToDateTime(row["VisitDate"]);
                return visitDate.ToString("dddd, MMMM d, yyyy");
            }

            return "Arrival date not recorded";
        }

        private static string BuildGuestCountDisplay(int adults, int children)
        {
            int total = Math.Max(0, adults) + Math.Max(0, children);
            if (total == 0)
                return "Guest count not recorded";

            return $"{total} guest{(total == 1 ? string.Empty : "s")} ({adults} adult{(adults == 1 ? string.Empty : "s")}, {children} child{(children == 1 ? string.Empty : "ren")})";
        }

        private sealed class GuestCheckInPreview
        {
            internal string ReservationId { get; init; } = string.Empty;
            internal string GuestName { get; init; } = string.Empty;
            internal string BookingType { get; init; } = string.Empty;
            internal string Status { get; init; } = string.Empty;
            internal string CabinDisplay { get; init; } = string.Empty;
            internal string StayDescriptor { get; init; } = string.Empty;
            internal string DateDisplay { get; init; } = string.Empty;
            internal string ArrivalTimeDisplay { get; init; } = string.Empty;
            internal string PaymentProfile { get; init; } = string.Empty;
            internal string PaymentStatus { get; init; } = string.Empty;
            internal string TotalAmountDisplay { get; init; } = string.Empty;
            internal string SpecialRequests { get; init; } = string.Empty;
            internal string GuestCountDisplay { get; init; } = string.Empty;
        }
    }
}
