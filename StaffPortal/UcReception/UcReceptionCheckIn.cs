using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    public partial class UcReceptionCheckIn : UserControl
    {
        private sealed class CheckInSuccessSnapshot
        {
            public string ReservationId { get; init; } = string.Empty;
            public string GuestName { get; init; } = string.Empty;
            public string BookingType { get; init; } = string.Empty;
            public string StayLabel { get; init; } = string.Empty;
            public string DateDisplay { get; init; } = string.Empty;
            public string ArrivalTime { get; init; } = string.Empty;
            public string TotalAmount { get; init; } = string.Empty;
            public string PaymentNote { get; init; } = string.Empty;
        }

        private sealed class CheckInPaymentSnapshot
        {
            public string PaymentMethod { get; init; } = "Not yet recorded";
            public string PaymentStatus { get; init; } = "Pending";
            public decimal PaymentAmount { get; init; }
            public bool RequiresFrontDeskSettlement { get; init; }
            public bool RequiresSettlementBeforeEntry { get; init; }
        }

        private enum ReceptionPaymentDecision
        {
            Cancel,
            CollectNow,
            ContinueWithBalance
        }

        private sealed class ReceptionSettlementResult
        {
            public ReceptionPaymentDecision Decision { get; init; } = ReceptionPaymentDecision.Cancel;
            public string CollectedMethod { get; init; } = "Pay at Resort";
        }

        private QrCameraScanner? _scanner;
        private int _selectedTabIndex;
        private bool _arrivalFlowInProgress;

        public UcReceptionCheckIn()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
            VisibleChanged += (s, e) =>
            {
                if (!Visible)
                    _scanner?.StopCamera();
                else if (_selectedTabIndex == 1)
                    _scanner?.StartCamera();
            };
        }

        private void Render()
        {
            _scanner?.StopCamera();
            _scanner?.Dispose();
            _scanner = null;

            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            int arrivalsToday = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_reservations
WHERE Status IN ('Confirmed','Pending')
  AND (
        ((BookingType = 'Cabin Stay' OR BookingType = 'Full Stay + Experience' OR BookingType = 'FullStay' OR BookingType = 'FullStayExperience')
            AND CheckInDate = CURDATE())
        OR
        ((BookingType <> 'Cabin Stay' AND BookingType <> 'Full Stay + Experience' AND BookingType <> 'FullStay' AND BookingType <> 'FullStayExperience')
            AND VisitDate = CURDATE())
      );");
            int checkedIn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Checked-In';");
            int confirmed = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Confirmed';");
            int dayVisitWalkins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status IN ('Confirmed','Pending') AND BookingType = 'Day Visit' AND VisitDate = CURDATE();");

            var pendingCheckIns = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS `Cabin or Visit`,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation') AS `Arrival Time`,
       r.BookingType AS `Booking Type`,
       r.Status
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.Status IN ('Confirmed','Pending')
  AND (r.CheckInDate = CURDATE() OR r.VisitDate = CURDATE())
ORDER BY r.CreatedAt DESC;");

            var actionCard = StaffPortalUi.ActionCard("Arrival Processing Console", BuildActionConsole, 520);

            var page = StaffPortalUi.BuildPage(
                "Check-In",
                "Reception check-in flow with manual entry, live QR scanning, and guest preview confirmation.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "Arrival Workflow",
                        "Reception can now process arrivals in two ways: type the reservation reference manually or scan the guest QR code live. Both routes open a premium guest preview first, then commit the check-in only after confirmation."),
                    StaffPortalUi.StatsRow(
                        (arrivalsToday.ToString(), "Due Today", WildNestUI.Green),
                        (checkedIn.ToString(), "Already Checked-In", WildNestUI.Blue),
                        (confirmed.ToString(), "Confirmed Reservations", WildNestUI.Amber),
                        (dayVisitWalkins.ToString(), "Day Visits Today", WildNestUI.Green)),
                    actionCard,
                    StaffPortalUi.GridCard("Pending Check-Ins", pendingCheckIns, "No pending check-ins for today.")
                });

            Controls.Add(page);
        }

        private void BuildActionConsole(Panel panel)
        {
            var modeBadge = new Label
            {
                Text = string.Empty,
                Font = WildNestUI.FontLabel(8.6f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.FromArgb(20, WildNestUI.Green),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(228, 34),
                Location = new Point(18, 52)
            };
            modeBadge.Tag = "Reception Arrival Modes";
            modeBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, modeBadge.Width - 1, modeBadge.Height - 1), 14);
                using var fill = new SolidBrush(modeBadge.BackColor);
                using var border = new Pen(Color.FromArgb(72, WildNestUI.Green), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    Convert.ToString(modeBadge.Tag) ?? string.Empty,
                    modeBadge.Font,
                    modeBadge.ClientRectangle,
                    modeBadge.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            panel.Controls.Add(modeBadge);

            var intro = new Label
            {
                Text = "Choose a reception mode below. Both modes use the same protected database transaction after the guest preview is approved.",
                Font = WildNestUI.FontBody(9.5f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(panel.Width - 36, 32),
                Location = new Point(18, 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panel.Controls.Add(intro);

            var tabStage = new Panel
            {
                Location = new Point(18, 138),
                Size = new Size(panel.Width - 36, panel.Height - 146),
                BackColor = Color.FromArgb(250, 247, 241),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tabStage.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, tabStage.Width - 1, tabStage.Height - 1), 18);
                using var fill = new SolidBrush(Color.FromArgb(250, 247, 241));
                using var border = new Pen(Color.FromArgb(228, 221, 209), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            panel.Controls.Add(tabStage);

            var tabs = new TabControl
            {
                Location = new Point(14, 14),
                Size = new Size(tabStage.Width - 28, tabStage.Height - 28),
                Font = WildNestUI.FontBody(9.4f),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(168, 36),
                Padding = new Point(18, 6)
            };
            tabs.DrawItem += DrawArrivalTab;
            tabs.TabPages.Add(BuildManualTab());
            tabs.TabPages.Add(BuildCameraTab());
            tabs.SelectedIndex = Math.Max(0, Math.Min(_selectedTabIndex, tabs.TabPages.Count - 1));
            tabs.SelectedIndexChanged += (s, e) =>
            {
                _selectedTabIndex = tabs.SelectedIndex;
                if (_selectedTabIndex == 1)
                    _scanner?.StartCamera();
                else
                    _scanner?.StopCamera();
            };

            tabs.HandleCreated += (s, e) =>
            {
                if (_selectedTabIndex == 1 && Visible)
                    BeginInvoke(new Action(() => _scanner?.StartCamera()));
            };

                tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            tabStage.Controls.Add(tabs);
        }

        private TabPage BuildManualTab()
        {
            var tab = new TabPage("Manual Entry")
            {
                BackColor = WildNestUI.Cream
            };

            var shell = new Panel
            {
                Location = new Point(16, 16),
                Size = new Size(tab.ClientSize.Width - 32, tab.ClientSize.Height - 32),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tab.Controls.Add(shell);

            var infoRail = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(236, 292),
                BackColor = WildNestUI.Forest
            };
            infoRail.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, infoRail.Width - 1, infoRail.Height - 1), 20);
                using var fill = new LinearGradientBrush(infoRail.ClientRectangle, WildNestUI.Forest, WildNestUI.ForestL, 90f);
                using var border = new Pen(Color.FromArgb(54, WildNestUI.Gold), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                using var accent = new SolidBrush(Color.FromArgb(34, WildNestUI.Gold));
                e.Graphics.FillEllipse(accent, -24, -20, 94, 94);
                e.Graphics.FillEllipse(accent, infoRail.Width - 90, infoRail.Height - 72, 110, 110);
            };
            shell.Controls.Add(infoRail);

            infoRail.Controls.Add(new Label
            {
                Text = "Manual Arrival",
                Font = WildNestUI.FontTitle(15f),
                ForeColor = WildNestUI.Cream,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(22, 24)
            });

            infoRail.Controls.Add(new Label
            {
                Text = "Best for printed confirmations, verbal inquiries, or desk-side assistance when the QR is not immediately available.",
                Font = WildNestUI.FontBody(9.2f),
                ForeColor = Color.FromArgb(226, WildNestUI.Cream),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(188, 72),
                Location = new Point(22, 58)
            });

            AddManualStep(infoRail, 22, 156, "1", "Enter reservation ID");
            AddManualStep(infoRail, 22, 196, "2", "Preview guest profile");
            AddManualStep(infoRail, 22, 236, "3", "Confirm protected check-in");

            var formCard = new Panel
            {
                Location = new Point(254, 0),
                Size = new Size(Math.Max(360, shell.Width - 254), 292),
                BackColor = Color.FromArgb(252, 250, 246)
            };
            formCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, formCard.Width - 1, formCard.Height - 1), 20);
                using var fill = new SolidBrush(Color.FromArgb(252, 250, 246));
                using var border = new Pen(Color.FromArgb(228, 219, 206), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            shell.Controls.Add(formCard);

            var title = new Label
            {
                Text = "Manual Reservation Lookup",
                Font = WildNestUI.FontBold(11f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(22, 22)
            };
            formCard.Controls.Add(title);

            var helper = new Label
            {
                Text = "Enter the reservation reference from the guest email, printed document, or front-desk inquiry. The guest preview opens before any status change is committed.",
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                AutoSize = false,
                Size = new Size(548, 40),
                BackColor = Color.Transparent,
                Location = new Point(22, 48)
            };
            formCard.Controls.Add(helper);

            var reservationBox = new TextBox
            {
                PlaceholderText = "Reservation ID (example: WN-2026-0001)",
                Font = WildNestUI.FontBody(11f),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(22, 112),
                Size = new Size(370, 36)
            };

            var previewButton = WildNestUI.BtnPrimary("Preview Guest", 140, 34);
            previewButton.Location = new Point(406, 112);
            previewButton.Click += (s, e) => PreviewAndCheckIn(reservationBox.Text);

            reservationBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    PreviewAndCheckIn(reservationBox.Text);
                }
            };

            var note = new Label
            {
                Text = "Tip: the guest preview shows booking type, cabin or visit type, stay dates, guest count, payment profile, and special requests before check-in is finalized.",
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Blue,
                AutoSize = false,
                Size = new Size(548, 56),
                BackColor = Color.Transparent,
                Location = new Point(22, 176)
            };

            formCard.Controls.Add(reservationBox);
            formCard.Controls.Add(previewButton);
            formCard.Controls.Add(note);

            void LayoutManual()
            {
                shell.Size = new Size(Math.Max(840, tab.ClientSize.Width - 32), Math.Max(280, tab.ClientSize.Height - 32));
                int contentHeight = Math.Min(shell.Height, 292);
                infoRail.Height = contentHeight;
                infoRail.Width = shell.Width >= 940 ? 236 : 212;
                formCard.Location = new Point(infoRail.Right + 18, 0);
                formCard.Size = new Size(Math.Max(360, shell.Width - formCard.Location.X), contentHeight);
                helper.Size = new Size(formCard.Width - 44, 40);
                previewButton.Location = new Point(formCard.Width - previewButton.Width - 22, 112);
                reservationBox.Width = Math.Max(240, previewButton.Left - 36);
                note.Size = new Size(formCard.Width - 44, 56);
            }

            tab.Resize += (s, e) => LayoutManual();
            LayoutManual();
            return tab;
        }

        private TabPage BuildCameraTab()
        {
            var tab = new TabPage("Scan QR")
            {
                BackColor = WildNestUI.Cream
            };

            var shell = new Panel
            {
                Location = new Point(16, 16),
                Size = new Size(tab.ClientSize.Width - 32, tab.ClientSize.Height - 32),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            tab.Controls.Add(shell);

            _scanner = new QrCameraScanner
            {
                Location = new Point(0, 0),
                Size = new Size(430, 286),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var sideCard = new Panel
            {
                Location = new Point(448, 0),
                Size = new Size(376, 286),
                BackColor = Color.FromArgb(248, 244, 239)
            };
            sideCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, sideCard.Width - 1, sideCard.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(248, 244, 239));
                using var border = new Pen(Color.FromArgb(230, 223, 212), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            var sideTitle = new Label
            {
                Text = "Live QR Reception Scanner",
                Font = WildNestUI.FontTitle(14f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(20, 18)
            };
            sideCard.Controls.Add(sideTitle);

            var sideText = new Label
            {
                Text = "Point the guest QR code at the camera. Once a valid reservation reference is decoded, the system pauses the feed and opens the guest clearance preview automatically.",
                Font = WildNestUI.FontBody(9.1f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(332, 74),
                Location = new Point(20, 52)
            };
            sideCard.Controls.Add(sideText);

            var flowBadge = new Label
            {
                Text = string.Empty,
                Font = WildNestUI.FontLabel(8.4f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.FromArgb(20, WildNestUI.Green),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(210, 28),
                Location = new Point(20, 142)
            };
            flowBadge.Tag = "Scan -> Preview -> Confirm";
            flowBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, flowBadge.Width - 1, flowBadge.Height - 1), 14);
                using var fill = new SolidBrush(flowBadge.BackColor);
                using var border = new Pen(Color.FromArgb(72, WildNestUI.Green), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    Convert.ToString(flowBadge.Tag) ?? string.Empty,
                    flowBadge.Font,
                    flowBadge.ClientRectangle,
                    flowBadge.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            sideCard.Controls.Add(flowBadge);

            var scannerPointOne = AddScannerPoint(sideCard, 20, 184, "Front-desk safe", "Scans without committing immediately. A guest preview still appears first.");
            var scannerPointTwo = AddScannerPoint(sideCard, 20, 248, "Compatibility", "Raw reservation IDs and embedded references are both accepted.");

            var compatibility = new Label
            {
                Text = "Arrival automation feels strongest when staff keeps the QR centered in the live frame for one second before moving the phone away.",
                Font = WildNestUI.FontBody(8.6f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(332, 38),
                Location = new Point(20, 316)
            };
            sideCard.Controls.Add(compatibility);

            _scanner.QrCodeDetected += payload =>
            {
                _scanner?.StopCamera();
                string reservationId = NormalizeReservationId(payload);
                string displayName = LookupGuestDisplayName(reservationId);
                _scanner?.ShowRecognition(string.IsNullOrWhiteSpace(displayName)
                    ? $"Recognizing reservation {reservationId}..."
                    : $"Recognizing guest {displayName}...");
                PreviewAndCheckIn(payload);
            };

            shell.Controls.Add(_scanner);
            shell.Controls.Add(sideCard);

            void LayoutCamera()
            {
                shell.Size = new Size(Math.Max(840, tab.ClientSize.Width - 32), Math.Max(300, tab.ClientSize.Height - 32));
                if (shell.Width >= 960)
                {
                    _scanner.Location = new Point(0, 0);
                    _scanner.Size = new Size(430, Math.Min(shell.Height, 286));
                    sideCard.Location = new Point(_scanner.Right + 18, 0);
                    sideCard.Size = new Size(Math.Max(320, shell.Width - sideCard.Location.X), _scanner.Height);
                }
                else
                {
                    _scanner.Location = new Point(0, 0);
                    _scanner.Size = new Size(shell.Width, 224);
                    sideCard.Location = new Point(0, _scanner.Bottom + 16);
                    sideCard.Size = new Size(shell.Width, Math.Max(220, shell.Height - sideCard.Location.Y));
                }

                sideText.Size = new Size(sideCard.Width - 44, 74);
                flowBadge.Location = new Point(20, sideText.Bottom + 14);
                scannerPointOne.Title.Location = new Point(20, flowBadge.Bottom + 16);
                scannerPointOne.Body.Location = new Point(20, scannerPointOne.Title.Bottom + 4);
                scannerPointOne.Body.Size = new Size(sideCard.Width - 44, 34);
                scannerPointTwo.Title.Location = new Point(20, scannerPointOne.Body.Bottom + 14);
                scannerPointTwo.Body.Location = new Point(20, scannerPointTwo.Title.Bottom + 4);
                scannerPointTwo.Body.Size = new Size(sideCard.Width - 44, 34);
                compatibility.Location = new Point(20, scannerPointTwo.Body.Bottom + 16);
                compatibility.Size = new Size(sideCard.Width - 44, 42);
            }

            tab.Resize += (s, e) => LayoutCamera();
            LayoutCamera();
            return tab;
        }

        private static void DrawArrivalTab(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabs || e.Index < 0 || e.Index >= tabs.TabPages.Count)
                return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle rect = e.Bounds;
            rect.Inflate(-4, -4);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = WildNestUI.RoundRect(rect, 14);
            using var fill = new SolidBrush(selected ? WildNestUI.Forest : Color.FromArgb(245, 240, 232));
            using var border = new Pen(selected ? Color.FromArgb(74, WildNestUI.Gold) : Color.FromArgb(220, 210, 198), 1f);
            using var textBrush = new SolidBrush(selected ? WildNestUI.Gold : WildNestUI.TextDark);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(
                e.Graphics,
                tabs.TabPages[e.Index].Text,
                selected ? WildNestUI.FontBold(9.6f) : WildNestUI.FontBody(9.4f),
                rect,
                selected ? WildNestUI.Gold : WildNestUI.TextDark,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static void AddManualStep(Control parent, int x, int y, string number, string text)
        {
            var step = new Label
            {
                Text = string.Empty,
                Font = WildNestUI.FontBold(8.8f),
                ForeColor = WildNestUI.Forest,
                BackColor = WildNestUI.Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(x, y)
            };
            step.Tag = number;
            step.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, step.Width - 1, step.Height - 1), 11);
                using var fill = new SolidBrush(WildNestUI.Gold);
                e.Graphics.FillPath(fill, path);
                TextRenderer.DrawText(
                    e.Graphics,
                    Convert.ToString(step.Tag) ?? string.Empty,
                    step.Font,
                    step.ClientRectangle,
                    step.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            parent.Controls.Add(step);

            parent.Controls.Add(new Label
            {
                Text = text,
                Font = WildNestUI.FontBody(9f),
                ForeColor = Color.FromArgb(226, WildNestUI.Cream),
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x + 34, y + 3)
            });
        }

        private static (Label Title, Label Body) AddScannerPoint(Control parent, int x, int y, string title, string text)
        {
            var titleLabel = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = WildNestUI.FontLabel(8f),
                ForeColor = WildNestUI.Amber,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(x, y)
            };
            parent.Controls.Add(titleLabel);

            var bodyLabel = new Label
            {
                Text = text,
                Font = WildNestUI.FontBody(8.8f),
                ForeColor = WildNestUI.TextDark,
                AutoSize = false,
                Size = new Size(308, 34),
                BackColor = Color.Transparent,
                Location = new Point(x, y + 16)
            };
            parent.Controls.Add(bodyLabel);
            return (titleLabel, bodyLabel);
        }

        private void PreviewAndCheckIn(string rawReservationInput)
        {
            if (_arrivalFlowInProgress)
                return;

            _arrivalFlowInProgress = true;
            try
            {
                string reservationId = NormalizeReservationId(rawReservationInput);
                if (string.IsNullOrWhiteSpace(reservationId))
                {
                    StaffPortalUi.ShowEliteMessage(
                        this,
                        "Reservation Required",
                        "Enter or scan a reservation reference first.",
                        StaffPortalUi.MessageTone.Warning,
                        "Close");
                    return;
                }

                DialogResult previewResult = UcCheckInConfirmDialog.ShowConfirm(this, reservationId);
                if (previewResult == DialogResult.OK)
                    ProcessCheckIn(reservationId);
            }
            finally
            {
                _arrivalFlowInProgress = false;
                ResumeScannerIfCameraMode();
            }
        }

        private static string LookupGuestDisplayName(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
                return string.Empty;

            try
            {
                object? value = StaffPortalDb.Scalar(@"
SELECT CONCAT(g.FirstName, ' ', g.LastName)
FROM tbl_reservations r
JOIN tbl_guests g ON g.GuestID = r.GuestID
WHERE r.ReservationID = @id
LIMIT 1;",
                    new MySqlParameter("@id", reservationId));

                return Convert.ToString(value) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string NormalizeReservationId(string rawReservationInput)
        {
            string value = (rawReservationInput ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(value);
                    var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    string queryId = (qs["i"] ?? qs["id"] ?? qs["r"] ?? qs["ref"] ?? qs["reservationId"] ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(queryId))
                        return queryId.ToUpperInvariant();
                }
                catch
                {
                    // Fall back to regex extraction below.
                }
            }

            var match = Regex.Match(value, @"WN-[A-Z0-9]+(?:-[A-Z0-9]+)+", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Value.ToUpperInvariant();

            if (value.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string localPath = new Uri(value).LocalPath;
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(localPath);
                    match = Regex.Match(fileName, @"WN-[A-Z0-9]+(?:-[A-Z0-9]+)+", RegexOptions.IgnoreCase);
                    if (match.Success)
                        return match.Value.ToUpperInvariant();
                }
                catch
                {
                    // Let the raw text fall through to the database validation path.
                }
            }

            return value.ToUpperInvariant();
        }

        private void ProcessCheckIn(string reservationId)
        {
            CheckInSuccessSnapshot? successSnapshot = null;
            CheckInPaymentSnapshot paymentSnapshot = LoadCheckInPaymentSnapshot(reservationId);
            bool directClearedArrival = !paymentSnapshot.RequiresFrontDeskSettlement;
            ReceptionSettlementResult paymentResult = new()
            {
                Decision = ReceptionPaymentDecision.ContinueWithBalance,
                CollectedMethod = paymentSnapshot.PaymentMethod
            };

            if (paymentSnapshot.RequiresFrontDeskSettlement)
            {
                paymentResult = PromptReceptionPaymentDecision(reservationId, paymentSnapshot);
                if (paymentResult.Decision == ReceptionPaymentDecision.Cancel)
                    return;

                if (paymentResult.Decision == ReceptionPaymentDecision.CollectNow)
                {
                    StaffPortalUi.ShowEliteMessage(
                        this,
                        "Payment Received",
                        $"{paymentResult.CollectedMethod} payment was received successfully. Click continue to finalize the guest check-in.",
                        StaffPortalUi.MessageTone.Success,
                        "Continue");
                }

                if (paymentSnapshot.RequiresSettlementBeforeEntry &&
                    paymentResult.Decision != ReceptionPaymentDecision.CollectNow)
                {
                    StaffPortalUi.ShowEliteMessage(
                        this,
                        "Payment Needed First",
                        "This reservation is marked Pay at Resort. Reception must collect the payment before the guest can be checked in for this visit.",
                        StaffPortalUi.MessageTone.Warning,
                        "Close");
                    return;
                }
            }

            try
            {
                StaffPortalDb.ExecuteTransaction((conn, tx) =>
                {
                    using var cmd = new MySqlCommand(@"
SELECT r.Status,
       BookingType,
       CheckInDate,
       VisitDate,
       CheckOutDate,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS StayLabel,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Guest') AS GuestName,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation') AS ArrivalTime,
       COALESCE(r.TotalAmount, 0) AS TotalAmount
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.ReservationID=@id;", conn, tx);
                    cmd.Parameters.AddWithValue("@id", reservationId);

                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("No matching reservation was found.");

                    string currentStatus = Convert.ToString(reader["Status"]) ?? string.Empty;
                    string bookingType = Convert.ToString(reader["BookingType"]) ?? string.Empty;
                    string stayLabel = Convert.ToString(reader["StayLabel"]) ?? "Day Visit / Experience";
                    string guestName = Convert.ToString(reader["GuestName"]) ?? "Guest";
                    string arrivalTime = Convert.ToString(reader["ArrivalTime"]) ?? "Front Desk Confirmation";
                    decimal totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                    DateTime today = DateTime.Today;
                    DateTime now = DateTime.Now;

                    DateTime? checkInDate = reader["CheckInDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["CheckInDate"]).Date;
                    DateTime? visitDate = reader["VisitDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["VisitDate"]).Date;
                    DateTime? checkOutDate = reader["CheckOutDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["CheckOutDate"]).Date;

                    string dateDisplay = IsStayBooking(bookingType)
                        ? (checkInDate.HasValue && checkOutDate.HasValue
                            ? $"{checkInDate.Value:MMMM d, yyyy} to {checkOutDate.Value:MMMM d, yyyy}"
                            : checkInDate?.ToString("MMMM d, yyyy") ?? "Front desk confirmation")
                        : (visitDate?.ToString("MMMM d, yyyy") ?? "Visit date not recorded");

                    reader.Close();

                    if (directClearedArrival)
                    {
                        if (string.Equals(currentStatus, "Checked-In", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(currentStatus, "Overdue", StringComparison.OrdinalIgnoreCase))
                        {
                            successSnapshot = new CheckInSuccessSnapshot
                            {
                                ReservationId = reservationId,
                                GuestName = guestName,
                                BookingType = bookingType,
                                StayLabel = stayLabel,
                                DateDisplay = dateDisplay,
                                ArrivalTime = arrivalTime,
                                TotalAmount = StaffPortalUi.Peso(totalAmount),
                                PaymentNote = "Payment already cleared. Reception can allow entry directly."
                            };
                            return;
                        }

                        if (string.Equals(currentStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"Reservations marked '{currentStatus}' cannot be checked in.");
                        }

                        successSnapshot = new CheckInSuccessSnapshot
                        {
                            ReservationId = reservationId,
                            GuestName = guestName,
                            BookingType = bookingType,
                            StayLabel = stayLabel,
                            DateDisplay = dateDisplay,
                            ArrivalTime = arrivalTime,
                            TotalAmount = StaffPortalUi.Peso(totalAmount),
                            PaymentNote = "Payment already cleared. Guest can proceed directly through reception check-in."
                        };

                        StaffPortalDb.Execute(conn, tx,
                            "UPDATE tbl_reservations SET Status='Checked-In' WHERE ReservationID=@id;",
                            new MySqlParameter("@id", reservationId));

                        StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_payments
SET Status = 'Paid',
    PaidAt = COALESCE(PaidAt, NOW())
WHERE ReservationID = @id
  AND COALESCE(PaymentMethod, '') <> 'Pay at Resort';",
                            new MySqlParameter("@id", reservationId));

                        StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_cabins c
JOIN tbl_reservations r ON r.CabinID = c.CabinID
SET c.Status = 'Occupied'
WHERE r.ReservationID = @id
  AND r.CabinID IS NOT NULL;",
                            new MySqlParameter("@id", reservationId));
                        return;
                    }

                    if (IsStayBooking(bookingType))
                    {
                        if (checkInDate.HasValue && today < checkInDate.Value)
                        {
                            throw new InvalidOperationException(
                                $"This guest is scheduled to check in on {checkInDate.Value:MMMM d, yyyy}. Early check-in is not allowed yet.");
                        }
                    }
                    else
                    {
                        if (visitDate.HasValue && today < visitDate.Value)
                        {
                            throw new InvalidOperationException(
                                $"This visit is scheduled for {visitDate.Value:MMMM d, yyyy}. The guest cannot be checked in before the visit date.");
                        }
                    }

                    if (!directClearedArrival && TryResolveScheduledArrival(arrivalTime, out TimeSpan scheduledArrival))
                    {
                        TimeSpan checkInOpens = scheduledArrival.Subtract(TimeSpan.FromMinutes(5));
                        if (checkInOpens < TimeSpan.Zero)
                            checkInOpens = scheduledArrival;

                        if (now.TimeOfDay < checkInOpens)
                        {
                            string returnTime = DateTime.Today.Add(scheduledArrival).ToString("h:mm tt");
                            string message = IsStayBooking(bookingType)
                                ? $"Check-in is too early. This guest is expected at {returnTime}. Please come back at that time."
                                : $"Check-in is too early. This visit is scheduled for {returnTime}. Please come back at that time.";
                            bool continueEarly = PromptCheckInOverride(
                                "Early Arrival Detected",
                                message + " Reception can still continue now if you are intentionally overriding the arrival time for a live front-desk clearance.");
                            if (!continueEarly)
                                throw new InvalidOperationException(message);
                        }
                    }

                    var linkedExperiences = LoadReservationExperienceNames(conn, tx, reservationId);
                    var sessionState = EvaluateExperienceSessionState(bookingType, linkedExperiences, now);
                    if (!directClearedArrival && sessionState.BlockCheckIn)
                    {
                        bool continueSessionOverride = PromptCheckInOverride(
                            "Scheduled Session Restriction",
                            sessionState.BlockMessage + " Reception can still continue now if this needs a supervised manual override.");
                        if (!continueSessionOverride)
                            throw new InvalidOperationException(sessionState.BlockMessage);
                    }

                    if (!string.IsNullOrWhiteSpace(sessionState.SessionNote))
                    {
                        arrivalTime = string.Equals(arrivalTime, "Front Desk Confirmation", StringComparison.OrdinalIgnoreCase)
                            ? sessionState.SessionNote
                            : $"{arrivalTime} â€¢ {sessionState.SessionNote}";
                    }

                    successSnapshot = new CheckInSuccessSnapshot
                    {
                        ReservationId = reservationId,
                        GuestName = guestName,
                        BookingType = bookingType,
                        StayLabel = stayLabel,
                        DateDisplay = dateDisplay,
                        ArrivalTime = arrivalTime,
                        TotalAmount = StaffPortalUi.Peso(totalAmount),
                        PaymentNote = BuildPaymentSuccessNote(paymentSnapshot, paymentResult)
                    };

                    object? statusValue = currentStatus;
                    if (statusValue == null || statusValue == DBNull.Value)
                        throw new InvalidOperationException("No matching reservation was found.");

                    currentStatus = Convert.ToString(statusValue) ?? string.Empty;
                    if (string.Equals(currentStatus, "Checked-In", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus, "Overdue", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("This reservation is already checked in. If the stay is overdue, finalize it from Check-Out and Billing.");

                    if (string.Equals(currentStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Reservations marked '{currentStatus}' cannot be checked in.");
                    StaffPortalDb.Execute(conn, tx,
                        "UPDATE tbl_reservations SET Status='Checked-In' WHERE ReservationID=@id;",
                        new MySqlParameter("@id", reservationId));

                    if (paymentResult.Decision == ReceptionPaymentDecision.CollectNow)
                    {
                        StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_payments
SET Status = 'Paid',
    PaidAt = NOW(),
    PaymentMethod = @method
WHERE ReservationID = @id;",
                            new MySqlParameter("@id", reservationId),
                            new MySqlParameter("@method", paymentResult.CollectedMethod));
                    }
                    else if (!paymentSnapshot.RequiresFrontDeskSettlement)
                    {
                        StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_payments
SET Status = 'Paid',
    PaidAt = COALESCE(PaidAt, NOW())
WHERE ReservationID = @id
  AND COALESCE(PaymentMethod, '') <> 'Pay at Resort';",
                            new MySqlParameter("@id", reservationId));
                    }

                    StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_cabins c
JOIN tbl_reservations r ON r.CabinID = c.CabinID
SET c.Status = 'Occupied'
WHERE r.ReservationID = @id
  AND r.CabinID IS NOT NULL;",
                        new MySqlParameter("@id", reservationId));
                });
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("UcReceptionCheckIn", ex, $"ProcessCheckIn failed for {reservationId}");

                if (directClearedArrival &&
                    TryForceDirectCheckInFallback(reservationId, out CheckInSuccessSnapshot forcedSnapshot))
                {
                    ShowCheckInSuccessDialog(forcedSnapshot);
                    Render();
                    return;
                }

                if (TryLoadExistingCheckedInSnapshot(reservationId, out CheckInSuccessSnapshot recoveredSnapshot))
                {
                    ShowCheckInSuccessDialog(recoveredSnapshot);
                    Render();
                    return;
                }

                StaffPortalUi.ShowEliteMessage(
                    this,
                    "Check-In Failed",
                    "The reservation could not be checked in. " +
                    (string.IsNullOrWhiteSpace(ex.Message)
                        ? "Please review the booking status, payment setup, or reservation timing and try again."
                        : ex.Message),
                    StaffPortalUi.MessageTone.Error,
                    "Close");
                return;
            }

            ShowCheckInSuccessDialog(successSnapshot ?? new CheckInSuccessSnapshot
            {
                ReservationId = reservationId,
                GuestName = "Guest",
                BookingType = "Reservation",
                StayLabel = "WildNest Arrival",
                DateDisplay = DateTime.Now.ToString("MMMM d, yyyy"),
                ArrivalTime = DateTime.Now.ToString("h:mm tt"),
                TotalAmount = "Captured in booking profile"
            });
            Render();
        }

        private void ResumeScannerIfCameraMode()
        {
            if (_selectedTabIndex != 1 || !Visible || _scanner == null)
                return;

            if (IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(() => _scanner?.StartCamera()));
        }

        private bool TryForceDirectCheckInFallback(string reservationId, out CheckInSuccessSnapshot snapshot)
        {
            snapshot = default!;

            try
            {
                CheckInSuccessSnapshot? created = null;
                StaffPortalDb.ExecuteTransaction((conn, tx) =>
                {
                    using var cmd = new MySqlCommand(@"
SELECT r.Status,
       r.BookingType,
       r.CheckInDate,
       r.VisitDate,
       r.CheckOutDate,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS StayLabel,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Guest') AS GuestName,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation') AS ArrivalTime,
       COALESCE(r.TotalAmount, 0) AS TotalAmount
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.ReservationID=@id;", conn, tx);
                    cmd.Parameters.AddWithValue("@id", reservationId);

                    using var reader = cmd.ExecuteReader();
                    if (!reader.Read())
                        return;

                    string currentStatus = Convert.ToString(reader["Status"]) ?? string.Empty;
                    if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    string bookingType = Convert.ToString(reader["BookingType"]) ?? "Reservation";
                    string stayLabel = Convert.ToString(reader["StayLabel"]) ?? "WildNest Arrival";
                    string guestName = Convert.ToString(reader["GuestName"]) ?? "Guest";
                    string arrivalTime = Convert.ToString(reader["ArrivalTime"]) ?? "Front Desk Confirmation";
                    decimal totalAmount = Convert.ToDecimal(reader["TotalAmount"]);

                    DateTime? checkInDate = reader["CheckInDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["CheckInDate"]).Date;
                    DateTime? visitDate = reader["VisitDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["VisitDate"]).Date;
                    DateTime? checkOutDate = reader["CheckOutDate"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["CheckOutDate"]).Date;

                    string dateDisplay = IsStayBooking(bookingType)
                        ? (checkInDate.HasValue && checkOutDate.HasValue
                            ? $"{checkInDate.Value:MMMM d, yyyy} to {checkOutDate.Value:MMMM d, yyyy}"
                            : checkInDate?.ToString("MMMM d, yyyy") ?? "Front desk confirmation")
                        : (visitDate?.ToString("MMMM d, yyyy") ?? "Visit date not recorded");

                    reader.Close();

                    StaffPortalDb.Execute(conn, tx,
                        "UPDATE tbl_reservations SET Status='Checked-In' WHERE ReservationID=@id;",
                        new MySqlParameter("@id", reservationId));

                    StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_payments
SET Status = 'Paid',
    PaidAt = COALESCE(PaidAt, NOW())
WHERE ReservationID = @id
  AND COALESCE(PaymentMethod, '') <> 'Pay at Resort';",
                        new MySqlParameter("@id", reservationId));

                    StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_cabins c
JOIN tbl_reservations r ON r.CabinID = c.CabinID
SET c.Status = 'Occupied'
WHERE r.ReservationID = @id
  AND r.CabinID IS NOT NULL;",
                        new MySqlParameter("@id", reservationId));

                    created = new CheckInSuccessSnapshot
                    {
                        ReservationId = reservationId,
                        GuestName = guestName,
                        BookingType = bookingType,
                        StayLabel = stayLabel,
                        DateDisplay = dateDisplay,
                        ArrivalTime = arrivalTime,
                        TotalAmount = StaffPortalUi.Peso(totalAmount),
                        PaymentNote = "Direct reception clearance applied. Guest can proceed immediately."
                    };
                });

                if (created != null)
                {
                    snapshot = created;
                    return true;
                }
            }
            catch (Exception fallbackEx)
            {
                ProjectDiagnostics.LogError("UcReceptionCheckIn", fallbackEx, $"Direct fallback check-in failed for {reservationId}");
            }

            return false;
        }

        private bool PromptCheckInOverride(string title, string message)
        {
            using var dialog = StaffPortalUi.BuildEliteDialog(
                title,
                "Reception override confirmation",
                new Size(560, 340));

            var shell = dialog.Controls[0].Controls[0];
            var body = shell.Controls["EliteDialogBody"] as Panel ?? shell;
            body.Controls.Clear();
            body.Padding = new Padding(24, 20, 24, 22);

            var badge = WildNestUI.Badge("RECEPTION OVERRIDE", BadgeStyle.Amber);
            badge.Location = new Point(0, 0);
            body.Controls.Add(badge);

            var messageLabel = new Label
            {
                Text = message,
                Location = new Point(0, 44),
                Size = new Size(470, 132),
                Font = WildNestUI.FontBody(9.8f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            };
            body.Controls.Add(messageLabel);

            var noteLabel = new Label
            {
                Text = "Choose Continue to let reception complete check-in anyway, or Cancel to keep the current booking restrictions.",
                Location = new Point(0, 184),
                Size = new Size(470, 42),
                Font = WildNestUI.FontBody(8.9f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            };
            body.Controls.Add(noteLabel);

            bool approved = false;

            var cancelBtn = WildNestUI.BtnOutline("Cancel", 110, 38);
            cancelBtn.Location = new Point(238, 242);
            cancelBtn.Click += (_, _) =>
            {
                dialog.DialogResult = DialogResult.Cancel;
                dialog.Close();
            };
            body.Controls.Add(cancelBtn);

            var continueBtn = WildNestUI.BtnPrimary("Continue Anyway", 156, 38);
            continueBtn.Location = new Point(358, 242);
            continueBtn.Click += (_, _) =>
            {
                approved = true;
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            body.Controls.Add(continueBtn);

            dialog.AcceptButton = continueBtn;
            dialog.CancelButton = cancelBtn;
            dialog.ShowDialog(this);
            return approved;
        }

        private static bool TryLoadExistingCheckedInSnapshot(string reservationId, out CheckInSuccessSnapshot snapshot)
        {
            snapshot = new CheckInSuccessSnapshot();
            DataTable table = StaffPortalDb.GetTable(@"
SELECT r.Status,
       r.ReservationID,
       r.BookingType,
       r.CheckInDate,
       r.VisitDate,
       r.CheckOutDate,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS StayLabel,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Guest') AS GuestName,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation') AS ArrivalTime,
       COALESCE(r.TotalAmount, 0) AS TotalAmount
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.ReservationID = @id
LIMIT 1;",
                new MySqlParameter("@id", reservationId));

            if (table.Rows.Count == 0)
                return false;

            DataRow row = table.Rows[0];
            string status = Convert.ToString(row["Status"]) ?? string.Empty;
            if (!status.Equals("Checked-In", StringComparison.OrdinalIgnoreCase))
                return false;

            string bookingType = Convert.ToString(row["BookingType"]) ?? "Reservation";
            DateTime? checkInDate = row["CheckInDate"] == DBNull.Value ? null : Convert.ToDateTime(row["CheckInDate"]).Date;
            DateTime? visitDate = row["VisitDate"] == DBNull.Value ? null : Convert.ToDateTime(row["VisitDate"]).Date;
            DateTime? checkOutDate = row["CheckOutDate"] == DBNull.Value ? null : Convert.ToDateTime(row["CheckOutDate"]).Date;

            string dateDisplay = IsStayBooking(bookingType)
                ? (checkInDate.HasValue && checkOutDate.HasValue
                    ? $"{checkInDate.Value:MMMM d, yyyy} to {checkOutDate.Value:MMMM d, yyyy}"
                    : checkInDate?.ToString("MMMM d, yyyy") ?? "Front desk confirmation")
                : (visitDate?.ToString("MMMM d, yyyy") ?? "Visit date not recorded");

            snapshot = new CheckInSuccessSnapshot
            {
                ReservationId = Convert.ToString(row["ReservationID"]) ?? reservationId,
                GuestName = Convert.ToString(row["GuestName"]) ?? "Guest",
                BookingType = bookingType,
                StayLabel = Convert.ToString(row["StayLabel"]) ?? "WildNest Arrival",
                DateDisplay = dateDisplay,
                ArrivalTime = Convert.ToString(row["ArrivalTime"]) ?? "Front Desk Confirmation",
                TotalAmount = StaffPortalUi.Peso(row["TotalAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["TotalAmount"])),
                PaymentNote = "The reservation was already processed successfully and is now marked as checked in."
            };
            return true;
        }

        private void ShowCheckInSuccessDialogLegacy(CheckInSuccessSnapshot snapshot)
        {
            using var dialog = StaffPortalUi.BuildEliteDialog(
                "Arrival Cleared",
                "WildNest reception check-in completed successfully.",
                new Size(560, 372));

            var shell = dialog.Controls[0].Controls[0];
            var body = shell.Controls["EliteDialogBody"] as Panel ?? shell;
            body.Controls.Clear();
            body.Padding = new Padding(24, 18, 24, 22);

            var badge = WildNestUI.Badge("CHECK-IN COMPLETE", BadgeStyle.Green);
            badge.Location = new Point(0, 0);
            body.Controls.Add(badge);

            var heroCard = new Panel
            {
                Location = new Point(0, 38),
                Size = new Size(482, 104),
                BackColor = Color.FromArgb(244, 251, 247)
            };
            heroCard.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, heroCard.Width - 1, heroCard.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(244, 251, 247));
                using var border = new Pen(Color.FromArgb(88, WildNestUI.Green), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            body.Controls.Add(heroCard);

            heroCard.Controls.Add(new Label
            {
                Text = snapshot.GuestName,
                AutoSize = true,
                Location = new Point(18, 16),
                Font = WildNestUI.FontTitle(17f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            heroCard.Controls.Add(new Label
            {
                Text = snapshot.ReservationId,
                AutoSize = true,
                Location = new Point(18, 46),
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            });

            heroCard.Controls.Add(new Label
            {
                Text = $"{snapshot.BookingType} â€¢ {snapshot.StayLabel}",
                AutoSize = false,
                Size = new Size(446, 22),
                Location = new Point(18, 68),
                Font = WildNestUI.FontBody(9.5f),
                ForeColor = WildNestUI.Forest,
                BackColor = Color.Transparent
            });

            var details = new[]
            {
                ("Schedule", snapshot.DateDisplay),
                ("Arrival", snapshot.ArrivalTime),
                ("Booking Total", snapshot.TotalAmount)
            };

            for (int i = 0; i < details.Length; i++)
            {
                var metric = new Panel
                {
                    Size = new Size(150, 78),
                    Location = new Point(160 * i, 160),
                    BackColor = Color.White
                };
                metric.Paint += (_, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = WildNestUI.RoundRect(new Rectangle(0, 0, metric.Width - 1, metric.Height - 1), 14);
                    using var fill = new SolidBrush(Color.White);
                    using var border = new Pen(Color.FromArgb(226, 219, 207), 1f);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                };
                body.Controls.Add(metric);

                metric.Controls.Add(new Label
                {
                    Text = details[i].Item1.ToUpperInvariant(),
                    Font = WildNestUI.FontLabel(8f),
                    ForeColor = WildNestUI.Amber,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Location = new Point(14, 12)
                });

                metric.Controls.Add(new Label
                {
                    Text = details[i].Item2,
                    Font = WildNestUI.FontBody(9.2f),
                    ForeColor = WildNestUI.TextDark,
                    AutoSize = false,
                    Size = new Size(122, 38),
                    BackColor = Color.Transparent,
                    Location = new Point(14, 30)
                });
            }

            var note = new Label
            {
                Text = string.IsNullOrWhiteSpace(snapshot.PaymentNote)
                    ? "The reservation is now live in the resort workflow. Cabin occupancy has been synchronized where applicable, and the reception desk can continue with the next arrival."
                    : snapshot.PaymentNote,
                Location = new Point(0, 252),
                Size = new Size(482, 42),
                Font = WildNestUI.FontBody(9.1f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            };
            body.Controls.Add(note);

            var btn = WildNestUI.BtnPrimary("Done", 126, 38);
            btn.Location = new Point(356, 300);
            btn.DialogResult = DialogResult.OK;
            body.Controls.Add(btn);
            dialog.AcceptButton = btn;
            dialog.ShowDialog(this);
        }

        private void ShowCheckInSuccessDialog(CheckInSuccessSnapshot snapshot)
        {
            const int bodyWidth = 540;
            const int heroWidth = 500;
            const int metricWidth = 162;
            const int metricHeight = 92;
            const int metricGap = 10;
            int metricGroupWidth = (metricWidth * 3) + (metricGap * 2);
            int heroLeft = (bodyWidth - heroWidth) / 2;
            int metricLeft = (bodyWidth - metricGroupWidth) / 2;

            using var dialog = StaffPortalUi.BuildEliteDialog(
                "Arrival Cleared",
                "WildNest reception check-in completed successfully.",
                new Size(628, 452));

            var shell = dialog.Controls[0].Controls[0];
            var body = shell.Controls["EliteDialogBody"] as Panel ?? shell;
            body.Controls.Clear();
            body.Padding = new Padding(26, 18, 26, 24);

            var badge = WildNestUI.Badge("CHECK-IN COMPLETE", BadgeStyle.Green);
            badge.Location = new Point((bodyWidth - badge.Width) / 2, 0);
            body.Controls.Add(badge);

            var heroCard = new Panel
            {
                Location = new Point(heroLeft, 38),
                Size = new Size(heroWidth, 120),
                BackColor = Color.FromArgb(244, 251, 247)
            };
            heroCard.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, heroCard.Width - 1, heroCard.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(244, 251, 247));
                using var border = new Pen(Color.FromArgb(88, WildNestUI.Green), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            body.Controls.Add(heroCard);

            heroCard.Controls.Add(new Label
            {
                Text = snapshot.GuestName,
                AutoSize = false,
                Size = new Size(heroWidth - 36, 34),
                Location = new Point(18, 16),
                Font = WildNestUI.FontTitle(17f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            });

            heroCard.Controls.Add(new Label
            {
                Text = snapshot.ReservationId,
                AutoSize = false,
                Size = new Size(heroWidth - 36, 22),
                Location = new Point(18, 50),
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            });

            heroCard.Controls.Add(new Label
            {
                Text = $"{snapshot.BookingType} - {snapshot.StayLabel}",
                AutoSize = false,
                Size = new Size(heroWidth - 36, 32),
                Location = new Point(18, 80),
                Font = WildNestUI.FontBody(9.5f),
                ForeColor = WildNestUI.Forest,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            });

            var details = new[]
            {
                ("Schedule", snapshot.DateDisplay),
                ("Arrival", snapshot.ArrivalTime),
                ("Booking Total", snapshot.TotalAmount)
            };

            for (int i = 0; i < details.Length; i++)
            {
                var metric = new Panel
                {
                    Size = new Size(metricWidth, metricHeight),
                    Location = new Point(metricLeft + ((metricWidth + metricGap) * i), 178),
                    BackColor = Color.White
                };
                metric.Paint += (_, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = WildNestUI.RoundRect(new Rectangle(0, 0, metric.Width - 1, metric.Height - 1), 14);
                    using var fill = new SolidBrush(Color.White);
                    using var border = new Pen(Color.FromArgb(226, 219, 207), 1f);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                };
                body.Controls.Add(metric);

                metric.Controls.Add(new Label
                {
                    Text = details[i].Item1.ToUpperInvariant(),
                    Font = WildNestUI.FontLabel(8f),
                    ForeColor = WildNestUI.Amber,
                    AutoSize = false,
                    Size = new Size(metricWidth - 24, 16),
                    BackColor = Color.Transparent,
                    Location = new Point(12, 12),
                    TextAlign = ContentAlignment.MiddleCenter
                });

                metric.Controls.Add(new Label
                {
                    Text = details[i].Item2,
                    Font = WildNestUI.FontBody(9.2f),
                    ForeColor = WildNestUI.TextDark,
                    AutoSize = false,
                    Size = new Size(metricWidth - 22, 50),
                    BackColor = Color.Transparent,
                    Location = new Point(11, 28),
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }

            var note = new Label
            {
                Text = string.IsNullOrWhiteSpace(snapshot.PaymentNote)
                    ? "The reservation is now live in the resort workflow. Cabin occupancy has been synchronized where applicable, and the reception desk can continue with the next arrival."
                    : snapshot.PaymentNote,
                Location = new Point(12, 294),
                Size = new Size(bodyWidth - 24, 44),
                Font = WildNestUI.FontBody(9.1f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            body.Controls.Add(note);

            var btn = WildNestUI.BtnPrimary("Done", 126, 38);
            btn.Location = new Point((bodyWidth - btn.Width) / 2, 350);
            btn.DialogResult = DialogResult.OK;
            body.Controls.Add(btn);
            dialog.AcceptButton = btn;
            dialog.ShowDialog(this);
        }

        private static bool IsStayBooking(string bookingType)
        {
            string value = (bookingType ?? string.Empty).Trim();
            return value.Equals("Cabin Stay", StringComparison.OrdinalIgnoreCase)
                || value.Equals("Full Stay + Experience", StringComparison.OrdinalIgnoreCase)
                || value.Equals("FullStay", StringComparison.OrdinalIgnoreCase)
                || value.Equals("FullStayExperience", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDayVisitBooking(string bookingType)
        {
            string value = (bookingType ?? string.Empty).Trim();
            return value.Equals("Day Visit", StringComparison.OrdinalIgnoreCase)
                || value.Equals("DayVisit", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> LoadReservationExperienceNames(MySqlConnection conn, MySqlTransaction tx, string reservationId)
        {
            var names = new List<string>();
            using var cmd = new MySqlCommand(@"
SELECT e.ExperienceName
FROM tbl_bookingexperiences be
JOIN tbl_experiences e ON e.ExperienceID = be.ExperienceID
WHERE be.ReservationID = @id
ORDER BY e.ExperienceName;", conn, tx);
            cmd.Parameters.AddWithValue("@id", reservationId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string name = Convert.ToString(reader["ExperienceName"]) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name.Trim());
            }

            return names;
        }

        private static ExperienceSessionState EvaluateExperienceSessionState(string bookingType, IReadOnlyCollection<string> linkedExperiences, DateTime now)
        {
            if (linkedExperiences.Count == 0)
                return ExperienceSessionState.Empty;

            bool hasNightSafari = linkedExperiences.Any(IsNightSafariExperience);
            bool hasNonNightExperience = linkedExperiences.Any(name => !IsNightSafariExperience(name));

            if (!hasNightSafari)
                return ExperienceSessionState.Empty;

            TimeSpan nightSafariCheckInOpens = new(17, 30, 0);
            string tonightNote = "Night Safari remains scheduled for tonight at 7:30 PM.";

            if (!IsStayBooking(bookingType) && !IsDayVisitBooking(bookingType) && !hasNonNightExperience && now.TimeOfDay < nightSafariCheckInOpens)
            {
                return new ExperienceSessionState
                {
                    BlockCheckIn = true,
                    BlockMessage = "This Night Safari booking is scheduled for tonight. Reception check-in for this session opens at 5:30 PM.",
                    SessionNote = tonightNote
                };
            }

            if (IsDayVisitBooking(bookingType))
            {
                return new ExperienceSessionState
                {
                    BlockCheckIn = false,
                    SessionNote = tonightNote
                };
            }

            if (hasNightSafari && hasNonNightExperience)
            {
                return new ExperienceSessionState
                {
                    BlockCheckIn = false,
                    SessionNote = tonightNote
                };
            }

            return ExperienceSessionState.Empty;
        }

        private CheckInPaymentSnapshot LoadCheckInPaymentSnapshot(string reservationId)
        {
            const string sql = @"
SELECT
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
    ) AS PaymentStatus,
    COALESCE(
        (
            SELECT p.Amount
            FROM tbl_payments p
            WHERE p.ReservationID = r.ReservationID
            ORDER BY COALESCE(p.PaidAt, '1900-01-01') DESC, p.PaymentID DESC
            LIMIT 1
        ),
        r.TotalAmount
    ) AS PaymentAmount,
    r.BookingType
FROM tbl_reservations r
WHERE r.ReservationID = @id
LIMIT 1;";

            var table = StaffPortalDb.GetTable(sql, new MySqlParameter("@id", reservationId));
            if (table.Rows.Count == 0)
                return new CheckInPaymentSnapshot();

            DataRow row = table.Rows[0];
            string bookingType = Convert.ToString(row["BookingType"]) ?? string.Empty;
            string paymentMethod = Convert.ToString(row["PaymentMethod"]) ?? "Not yet recorded";
            string paymentStatus = Convert.ToString(row["PaymentStatus"]) ?? "Pending";
            decimal paymentAmount = row["PaymentAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(row["PaymentAmount"]);

            bool isPayAtResort =
                paymentMethod.Equals("Pay at Resort", StringComparison.OrdinalIgnoreCase) ||
                paymentStatus.Equals("Pay on Arrival", StringComparison.OrdinalIgnoreCase);

            if (!isPayAtResort &&
                (string.IsNullOrWhiteSpace(paymentStatus) ||
                 paymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
                 paymentStatus.Equals("Pending Verification", StringComparison.OrdinalIgnoreCase)))
            {
                paymentStatus = "Paid";
            }

            bool alreadyCollected =
                paymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                paymentStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                paymentStatus.Equals("Settled", StringComparison.OrdinalIgnoreCase);

            return new CheckInPaymentSnapshot
            {
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                PaymentAmount = paymentAmount,
                RequiresFrontDeskSettlement = isPayAtResort && !alreadyCollected,
                RequiresSettlementBeforeEntry = !IsStayBooking(bookingType)
            };
        }

        private ReceptionSettlementResult PromptReceptionPaymentDecision(string reservationId, CheckInPaymentSnapshot paymentSnapshot)
        {
            using var dialog = StaffPortalUi.BuildEliteDialog(
                "Front Desk Payment Required",
                "This booking is set to Pay at Resort. Reception should choose the actual on-site settlement method before check-in continues.",
                new Size(560, paymentSnapshot.RequiresSettlementBeforeEntry ? 430 : 458));

            var shell = dialog.Controls[0].Controls[0];
            var body = shell.Controls["EliteDialogBody"] as Panel ?? shell;
            body.Controls.Clear();
            body.Padding = new Padding(24, 18, 24, 22);

            var badge = WildNestUI.Badge(
                paymentSnapshot.RequiresSettlementBeforeEntry ? "ENTRY PAYMENT REQUIRED" : "PAYMENT DUE ON ARRIVAL",
                paymentSnapshot.RequiresSettlementBeforeEntry ? BadgeStyle.Amber : BadgeStyle.Blue);
            badge.Location = new Point(0, 0);
            body.Controls.Add(badge);

            body.Controls.Add(new Label
            {
                Text = reservationId,
                AutoSize = true,
                Location = new Point(0, 36),
                Font = new Font("Consolas", 10f, FontStyle.Regular),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            });

            body.Controls.Add(new Label
            {
                Text = $"Payment method: {paymentSnapshot.PaymentMethod}\nCurrent status: {paymentSnapshot.PaymentStatus}\nAmount due: {StaffPortalUi.Peso(paymentSnapshot.PaymentAmount)}",
                Location = new Point(0, 64),
                Size = new Size(484, 64),
                Font = WildNestUI.FontBody(9.8f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            body.Controls.Add(new Label
            {
                Text = paymentSnapshot.RequiresSettlementBeforeEntry
                    ? "Because this is a visit-style booking, reception should collect the payment before allowing entry. Choose the actual on-site settlement method below, then confirm collection."
                    : "Because this is a stay booking, reception may either collect the payment now using an on-site method or continue check-in with the balance still due for later settlement.",
                Location = new Point(0, 138),
                Size = new Size(484, 56),
                Font = WildNestUI.FontBody(9.3f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            });

            body.Controls.Add(new Label
            {
                Text = "On-Site Settlement Method",
                Location = new Point(0, 206),
                Size = new Size(220, 20),
                Font = WildNestUI.FontBold(9.3f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            string selectedMethod = "Front Desk Cash";

            RadioButton cashOption = BuildSettlementOption("Cash at Desk", "Fastest walk-in payment path", 0, 234, true);
            RadioButton cardOption = BuildSettlementOption("Card Terminal", "Credit or debit processed at reception", 0, 272, false);
            RadioButton gcashOption = BuildSettlementOption("GCash / Maya QR", "Guest scans the on-site QR and reception verifies it", 0, 310, false);

            cashOption.CheckedChanged += (_, _) => { if (cashOption.Checked) selectedMethod = "Front Desk Cash"; };
            cardOption.CheckedChanged += (_, _) => { if (cardOption.Checked) selectedMethod = "Front Desk Card"; };
            gcashOption.CheckedChanged += (_, _) => { if (gcashOption.Checked) selectedMethod = "Front Desk GCash / Maya"; };

            body.Controls.Add(cashOption);
            body.Controls.Add(cardOption);
            body.Controls.Add(gcashOption);

            ReceptionSettlementResult result = new();
            int buttonY = paymentSnapshot.RequiresSettlementBeforeEntry ? 362 : 390;

            var collectBtn = WildNestUI.BtnPrimary("Collect Payment", 146, 38);
            collectBtn.Location = new Point(338, buttonY);
            collectBtn.Click += (_, _) =>
            {
                result = new ReceptionSettlementResult
                {
                    Decision = ReceptionPaymentDecision.CollectNow,
                    CollectedMethod = selectedMethod
                };
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            body.Controls.Add(collectBtn);

            if (!paymentSnapshot.RequiresSettlementBeforeEntry)
            {
                var continueBtn = WildNestUI.BtnOutline("Keep Balance Due", 138, 38);
                continueBtn.Location = new Point(190, buttonY);
                continueBtn.Click += (_, _) =>
                {
                    result = new ReceptionSettlementResult
                    {
                        Decision = ReceptionPaymentDecision.ContinueWithBalance,
                        CollectedMethod = paymentSnapshot.PaymentMethod
                    };
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };
                body.Controls.Add(continueBtn);
            }

            var cancelBtn = WildNestUI.BtnOutline("Cancel", 96, 38);
            cancelBtn.Location = new Point(paymentSnapshot.RequiresSettlementBeforeEntry ? 232 : 86, buttonY);
            cancelBtn.Click += (_, _) =>
            {
                result = new ReceptionSettlementResult
                {
                    Decision = ReceptionPaymentDecision.Cancel,
                    CollectedMethod = paymentSnapshot.PaymentMethod
                };
                dialog.DialogResult = DialogResult.Cancel;
                dialog.Close();
            };
            body.Controls.Add(cancelBtn);

            dialog.ShowDialog(this);
            return result;
        }

        private static RadioButton BuildSettlementOption(string title, string subtitle, int x, int y, bool isChecked)
        {
            return new RadioButton
            {
                Text = $"{title}  —  {subtitle}",
                Location = new Point(x, y),
                Size = new Size(484, 28),
                AutoSize = false,
                Checked = isChecked,
                Font = WildNestUI.FontBody(9.2f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            };
        }

        private static string BuildPaymentSuccessNote(CheckInPaymentSnapshot paymentSnapshot, ReceptionSettlementResult result)
        {
            if (paymentSnapshot.RequiresFrontDeskSettlement)
            {
                if (result.Decision == ReceptionPaymentDecision.CollectNow)
                {
                    return $"The reservation is now live in the resort workflow, and front desk payment has been marked as collected for {StaffPortalUi.Peso(paymentSnapshot.PaymentAmount)} using {result.CollectedMethod}.";
                }

                if (result.Decision == ReceptionPaymentDecision.ContinueWithBalance)
                {
                    return $"The reservation is now live in the resort workflow. Payment remains due at the front desk for {StaffPortalUi.Peso(paymentSnapshot.PaymentAmount)}, so checkout and billing should settle the remaining balance.";
                }
            }

            return string.Empty;
        }

        private static bool IsNightSafariExperience(string experienceName)
        {
            string value = (experienceName ?? string.Empty).Trim();
            return value.IndexOf("Night Safari", StringComparison.OrdinalIgnoreCase) >= 0;
        }

                private static bool TryResolveScheduledArrival(string rawValue, out TimeSpan time)
        {
            time = default;
            string value = (rawValue ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "Front Desk Confirmation", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "To be confirmed", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "See confirmation", StringComparison.OrdinalIgnoreCase))
                return false;

            Match match = Regex.Match(value, @"\b\d{1,2}:\d{2}(?::\d{2})?\s*(AM|PM)\b", RegexOptions.IgnoreCase);
            string firstSegment = match.Success
                ? match.Value.Trim()
                : value
                    .Split(new[] { '•', '|', '–', '-' }, StringSplitOptions.RemoveEmptyEntries)[0]
                    .Trim();

            string[] formats =
            {
                "h:mm tt",
                "hh:mm tt",
                "h:mmtt",
                "hh:mmtt",
                "h:mm:ss tt",
                "hh:mm:ss tt",
                "H:mm",
                "HH:mm",
                "H:mm:ss",
                "HH:mm:ss"
            };

            if (DateTime.TryParseExact(firstSegment, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                time = parsed.TimeOfDay;
                return true;
            }

            return false;
        }

        private sealed class ExperienceSessionState
        {
            internal static readonly ExperienceSessionState Empty = new();
            internal bool BlockCheckIn { get; init; }
            internal string BlockMessage { get; init; } = string.Empty;
            internal string SessionNote { get; init; } = string.Empty;
        }
    }
}

