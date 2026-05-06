using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    /// <summary>
    /// Reception Check-In — two-tab layout:
    ///   Tab 1: Manual entry  — type Reservation ID → confirm dialog → check in
    ///   Tab 2: QR Camera     — scan QR             → confirm dialog → check in
    ///
    /// Both tabs show UcCheckInConfirmDialog BEFORE any database write.
    /// Staff sees full guest name, initials avatar, cabin, dates, amount,
    /// and special requests — then clicks "Confirm Check-In" or "Cancel".
    ///
    /// ProcessCheckIn() only runs after DialogResult.OK is returned.
    /// </summary>
    public partial class UcReceptionCheckIn : UserControl
    {
        private QrCameraScanner? _scanner;
        private int _activeTabIndex = 0;

        public UcReceptionCheckIn()
        {
            InitializeComponent();
            Dock  = DockStyle.Fill;
            Load += (s, e) => Render();

            VisibleChanged += (s, e) =>
            {
                if (!Visible)                         _scanner?.StopCamera();
                else if (_activeTabIndex == 1) _scanner?.StartCamera();
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Render
        // ─────────────────────────────────────────────────────────────
        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            int arrivalsToday   = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckInDate = CURDATE() OR VisitDate = CURDATE();");
            int checkedIn       = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Checked-In';");
            int confirmed       = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Confirmed';");
            int dayVisitWalkins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE BookingType = 'Day Visit' AND VisitDate = CURDATE();");

            var pendingCheckIns = StaffPortalDb.GetTable(@"
SELECT r.ReservationID                                       AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName)                 AS `Guest`,
       COALESCE(c.CabinName, 'Day Visit / Experience')       AS `Cabin or Visit`,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation')    AS `Arrival Time`,
       r.BookingType                                         AS `Booking Type`,
       r.Status
FROM   tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE  r.Status IN ('Confirmed', 'Pending')
  AND  (r.CheckInDate = CURDATE() OR r.VisitDate = CURDATE())
ORDER BY r.CreatedAt DESC;");

            var actionCard = StaffPortalUi.ActionCard("Process Check-In", panel =>
            {
                var tabControl = new TabControl
                {
                    Location = new Point(18, 54),
                    Width    = panel.Width - 36,
                    Height   = 220,
                    Font     = WildNestUI.FontBody(9.5f),
                    Anchor   = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
                };

                tabControl.TabPages.Add(BuildManualTab());
                tabControl.TabPages.Add(BuildCameraTab());

                if (_activeTabIndex < tabControl.TabPages.Count)
                    tabControl.SelectedIndex = _activeTabIndex;

                tabControl.SelectedIndexChanged += (s, e) =>
                {
                    _activeTabIndex = tabControl.SelectedIndex;
                    if (_activeTabIndex == 1) _scanner?.StartCamera();
                    else                      _scanner?.StopCamera();
                };

                if (_activeTabIndex == 1)
                    tabControl.HandleCreated += (s, e) => _scanner?.StartCamera();

                panel.Controls.Add(tabControl);
            }, 310);

            var page = StaffPortalUi.BuildPage(
                "Check-In",
                "Reception check-in — manual entry or QR camera scan.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "Check-In is for guests arriving today. Use the Manual tab to type a Reservation Ref, " +
                        "or switch to the QR Camera tab and point the guest's QR code at the camera. " +
                        "A full guest preview appears before check-in is committed."),
                    StaffPortalUi.StatsRow(
                        (arrivalsToday.ToString(),   "Due Today",              WildNestUI.Green),
                        (checkedIn.ToString(),        "Already Checked-In",     WildNestUI.Blue),
                        (confirmed.ToString(),        "Confirmed Reservations", WildNestUI.Amber),
                        (dayVisitWalkins.ToString(),  "Day Visits Today",       WildNestUI.Green)),
                    actionCard,
                    StaffPortalUi.GridCard("Pending Check-Ins", pendingCheckIns, "No pending check-ins for today.")
                });

            Controls.Add(page);
        }

        // ─────────────────────────────────────────────────────────────
        // Tab 1 — Manual entry
        // ─────────────────────────────────────────────────────────────
        private TabPage BuildManualTab()
        {
            var tab = new TabPage("⌨  Manual Entry")
            {
                BackColor = Color.White,
                Padding   = new Padding(6)
            };

            var lbl = new Label
            {
                Text      = "Enter a reservation reference to preview the guest and confirm check-in.",
                Font      = WildNestUI.FontBody(10f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(8, 14)
            };
            var helper = new Label
            {
                Text      = "Use the Booking ID from the guest email, QR lookup, or the pending check-ins table below.",
                Font      = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Location  = new Point(8, 34)
            };
            var tb = new TextBox
            {
                PlaceholderText = "Reservation ID  (example: WN-2026-0001)",
                Font            = WildNestUI.FontBody(10f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(8, 62),
                Width           = 270
            };

            var btn = WildNestUI.BtnPrimary("Preview & Check In", 160, 30);
            btn.Location = new Point(290, 60);
            btn.Click   += (s, e) => ShowConfirmThenCheckIn(tb.Text.Trim());

            // Enter key also triggers preview
            tb.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ShowConfirmThenCheckIn(tb.Text.Trim());
                }
            };

            tab.Controls.Add(lbl);
            tab.Controls.Add(helper);
            tab.Controls.Add(tb);
            tab.Controls.Add(btn);
            return tab;
        }

        // ─────────────────────────────────────────────────────────────
        // Tab 2 — QR Camera scanner
        // ─────────────────────────────────────────────────────────────
        private TabPage BuildCameraTab()
        {
            var tab = new TabPage("📷  Scan QR Code")
            {
                BackColor = Color.Black,
                Padding   = new Padding(0)
            };

            if (_scanner == null || _scanner.IsDisposed)
            {
                _scanner = new QrCameraScanner
                {
                    DeduplicateCooldownMs = 4000
                };
            }

            _scanner.Dock  = DockStyle.Left;
            _scanner.Width = 260;

            var resultPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(7, 26, 14),
                Padding   = new Padding(12)
            };

            var scanHint = new Label
            {
                Text      = "Waiting for QR scan…\n\nPoint the guest's QR code at the camera.\nA guest preview will appear for confirmation\nbefore check-in is committed.",
                Font      = WildNestUI.FontBody(10f),
                ForeColor = Color.FromArgb(139, 184, 154),
                BackColor = Color.Transparent,
                AutoSize  = false,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            resultPanel.Controls.Add(scanHint);

            _scanner.QrCodeDetected -= OnQrDetected;
            _scanner.QrCodeDetected += OnQrDetected;

            void OnQrDetected(string reservationId)
            {
                // Pause camera while dialog is open — prevents double scan
                _scanner?.StopCamera();

                scanHint.Text      = $"🔍  QR detected — loading guest…\n\n{reservationId}";
                scanHint.ForeColor = Color.FromArgb(212, 160, 23);

                ShowConfirmThenCheckIn(reservationId);

                // Restore hint and resume camera after dialog closes
                scanHint.Text      = "Waiting for QR scan…\n\nPoint the guest's QR code at the camera.\nA guest preview will appear for confirmation\nbefore check-in is committed.";
                scanHint.ForeColor = Color.FromArgb(139, 184, 154);

                if (_activeTabIndex == 1)
                    _scanner?.StartCamera();
            }

            tab.Controls.Add(resultPanel);
            tab.Controls.Add(_scanner);
            return tab;
        }

        // ─────────────────────────────────────────────────────────────
        // Gate: show premium guest preview, only commit if confirmed
        // ─────────────────────────────────────────────────────────────
        private void ShowConfirmThenCheckIn(string reservationId)
        {
            if (string.IsNullOrWhiteSpace(reservationId))
            {
                MessageBox.Show(
                    "Enter or scan a reservation reference first.",
                    "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = UcCheckInConfirmDialog.ShowConfirm(this, reservationId);

            if (result == DialogResult.OK)
                ProcessCheckIn(reservationId);
        }

        // ─────────────────────────────────────────────────────────────
        // Commit — database transaction (unchanged business logic)
        // ─────────────────────────────────────────────────────────────
        private void ProcessCheckIn(string reservationId)
        {
            try
            {
                StaffPortalDb.ExecuteTransaction((conn, tx) =>
                {
                    object? statusValue = StaffPortalDb.Scalar(conn, tx,
                        "SELECT Status FROM tbl_reservations WHERE ReservationID=@id;",
                        new MySqlParameter("@id", reservationId));

                    if (statusValue == null || statusValue == DBNull.Value)
                        throw new InvalidOperationException("No matching reservation was found.");

                    string currentStatus = Convert.ToString(statusValue) ?? string.Empty;

                    if (string.Equals(currentStatus, "Checked-In", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus, "Overdue",    StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("This reservation is already checked in.");

                    if (string.Equals(currentStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(currentStatus, "Cancelled",   StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Reservations marked '{currentStatus}' cannot be checked in.");

                    StaffPortalDb.Execute(conn, tx,
                        "UPDATE tbl_reservations SET Status='Checked-In' WHERE ReservationID=@id;",
                        new MySqlParameter("@id", reservationId));

                    StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_cabins c
JOIN   tbl_reservations r ON r.CabinID = c.CabinID
SET    c.Status = 'Occupied'
WHERE  r.ReservationID = @id
  AND  r.CabinID IS NOT NULL;",
                        new MySqlParameter("@id", reservationId));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Check-in failed: " + ex.Message,
                    "WildNest — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(
                $"✅  {reservationId} has been successfully checked in.\n\nCabin status updated to Occupied.",
                "WildNest — Checked In",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Render();
        }
    }
}
