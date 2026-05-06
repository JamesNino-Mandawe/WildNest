using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    public partial class UcReceptionCheckOut : UserControl
    {
        private const string PendingPaymentStatuses = "'Pending','Unpaid','Pending Verification','Pay on Arrival','Confirmed'";

        public UcReceptionCheckOut()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            int dueToday = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckOutDate = CURDATE();");
            int overdue = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Overdue';");
            decimal revenueToday = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt) = CURDATE();");
            int unpaid = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status IN (" + PendingPaymentStatuses + ");");

            var departures = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       c.CabinName AS `Cabin`,
       r.TotalAmount AS `Reservation Total`,
       COALESCE(p.Amount, 0) AS `Payment Amount`,
       COALESCE(p.Status, 'No Payment Record') AS `Payment Status`,
       r.Status AS `Reservation Status`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
LEFT JOIN tbl_payments p ON p.ReservationID = r.ReservationID
WHERE r.CheckOutDate = CURDATE() OR r.Status IN ('Checked-In','Overdue')
ORDER BY r.CheckOutDate, r.CreatedAt DESC;");

            var actionCard = StaffPortalUi.ActionCard("Finalize Check-Out", panel =>
            {
                var info = new Label
                {
                    Text = "Enter a reservation reference and choose the payment result to finalize departure.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var helper = new Label
                {
                    Text = "Use this for guests who are leaving. Reservation ID comes from the departure queue below or from the guest's booking confirmation.",
                    Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 82)
                };
                var tb = new TextBox
                {
                    PlaceholderText = "Reservation ID (example: WN-2026-0001)",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 108),
                    Width = 260
                };
                var cmb = new ComboBox
                {
                    Location = new Point(290, 108),
                    Width = 150,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = WildNestUI.FontBody(10f)
                };
                cmb.Items.AddRange(new object[] { "Paid", "Pending", "Unpaid" });
                cmb.SelectedIndex = 0;

                var btn = WildNestUI.BtnPrimary("Complete Check-Out", 170, 30);
                btn.Location = new Point(452, 106);
                btn.Click += (s, e) =>
                {
                    string reservationId = tb.Text.Trim();
                    if (string.IsNullOrWhiteSpace(reservationId))
                    {
                        MessageBox.Show("Enter a reservation reference first.");
                        return;
                    }

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
                            if (string.Equals(currentStatus, "Checked-Out", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("This reservation has already been checked out.");

                            if (string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("Cancelled reservations cannot be checked out.");

                            StaffPortalDb.Execute(conn, tx,
                                "UPDATE tbl_reservations SET Status='Checked-Out' WHERE ReservationID=@id;",
                                new MySqlParameter("@id", reservationId));

                            object? paymentCountValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT COUNT(*) FROM tbl_payments WHERE ReservationID=@id;",
                                new MySqlParameter("@id", reservationId));
                            int paymentRows = paymentCountValue == null || paymentCountValue == DBNull.Value
                                ? 0
                                : Convert.ToInt32(paymentCountValue);

                            if (paymentRows == 0)
                            {
                                StaffPortalDb.Execute(conn, tx, @"
INSERT INTO tbl_payments (ReservationID, Amount, PaymentMethod, Status, PaidAt)
SELECT ReservationID, TotalAmount, 'Front Desk Settlement', @status,
       CASE WHEN @status='Paid' THEN NOW() ELSE NULL END
FROM tbl_reservations
WHERE ReservationID=@id;",
                                    new MySqlParameter("@id", reservationId),
                                    new MySqlParameter("@status", cmb.Text));
                            }
                            else
                            {
                                StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_payments
SET Status=@status,
    PaidAt = CASE WHEN @status='Paid' THEN NOW() ELSE PaidAt END
WHERE ReservationID=@id;",
                                    new MySqlParameter("@id", reservationId),
                                    new MySqlParameter("@status", cmb.Text));
                            }

                            StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_cabins c
JOIN tbl_reservations r ON r.CabinID = c.CabinID
SET c.Status = 'Available'
WHERE r.ReservationID = @id
  AND r.CabinID IS NOT NULL;",
                                new MySqlParameter("@id", reservationId));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Check-out failed: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Reservation and payment status updated.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(helper);
                panel.Controls.Add(tb);
                panel.Controls.Add(cmb);
                panel.Controls.Add(btn);
            }, 188);

            var page = StaffPortalUi.BuildPage(
                "Check-Out & Billing",
                "Departure and payment control using tbl_reservations and tbl_payments.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "Choose the reservation that is leaving today or already marked Checked-In. Then set the payment result to Paid, Pending, or Unpaid before finalizing departure."),
                    StaffPortalUi.StatsRow(
                        (dueToday.ToString(), "Due to Check Out Today", WildNestUI.Green),
                        (overdue.ToString(), "Overdue Stays", WildNestUI.Red),
                        (StaffPortalUi.Peso(revenueToday), "Revenue Today", WildNestUI.Amber),
                        (unpaid.ToString(), "Pending / Unpaid Payments", WildNestUI.Red)),
                    actionCard,
                    StaffPortalUi.GridCard("Departure & Billing Queue", departures, "No departures matched this view.")
                });

            Controls.Add(page);
        }
    }
}
