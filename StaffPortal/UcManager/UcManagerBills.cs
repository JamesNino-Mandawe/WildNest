using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerBills : UserControl
    {
        private const string PendingPaymentStatuses = "'Pending','Unpaid','Pending Verification','Pay on Arrival','Confirmed'";

        public UcManagerBills()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMessage);

            decimal collected = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            decimal collectedToday = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            decimal pendingValue = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN (" + PendingPaymentStatuses + ");");
            int pendingPayments = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status IN (" + PendingPaymentStatuses + ");");
            int paymentRecords = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments;");
            int unpaidReservations = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_reservations r
LEFT JOIN tbl_payments p ON p.ReservationID = r.ReservationID
WHERE p.PaymentID IS NULL OR p.Status IN (" + PendingPaymentStatuses + @");");

            var paymentStatusBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Status), ''), 'Unspecified') AS `Status`,
       COUNT(*) AS `Records`,
       COALESCE(SUM(Amount),0) AS `Amount`
FROM tbl_payments
GROUP BY COALESCE(NULLIF(TRIM(Status), ''), 'Unspecified')
ORDER BY `Amount` DESC, `Records` DESC;");

            var paymentMethods = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(PaymentMethod), ''), 'Unspecified') AS `Method`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(Amount),0) AS `Collected (PHP)`
FROM tbl_payments
GROUP BY COALESCE(NULLIF(TRIM(PaymentMethod), ''), 'Unspecified')
ORDER BY `Collected (PHP)` DESC;");

            var collectionsByBookingType = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(r.BookingType),''),'Unspecified') AS `Booking Type`,
       COUNT(DISTINCT r.ReservationID) AS `Reservations`,
       COALESCE(SUM(p.Amount),0) AS `Collected (PHP)`,
       COALESCE(SUM(r.TotalAmount),0) AS `Booked Value (PHP)`
FROM tbl_reservations r
LEFT JOIN tbl_payments p
       ON p.ReservationID = r.ReservationID
      AND p.Status IN ('Paid','Completed','Settled')
GROUP BY COALESCE(NULLIF(TRIM(r.BookingType),''),'Unspecified')
ORDER BY `Collected (PHP)` DESC, `Booked Value (PHP)` DESC;");

            var payments = StaffPortalDb.GetTable(@"
SELECT p.PaymentID AS `Payment ID`,
       p.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       p.Amount AS `Amount (PHP)`,
       p.PaymentMethod AS `Payment Method`,
       p.Status,
       DATE_FORMAT(p.PaidAt, '%Y-%m-%d %H:%i') AS `Paid At`
FROM tbl_payments p
LEFT JOIN tbl_reservations r ON r.ReservationID = p.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
ORDER BY p.PaidAt DESC, p.PaymentID DESC;");

            var page = StaffPortalUi.BuildPage(
                "Billing and Collections",
                "Manager-grade payment oversight tied directly to live reservations, guest records, and collection outcomes.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbHealthy
                        ? "Billing controls are synchronized with live payment and reservation records."
                        : $"Billing view could not verify the database connection: {dbMessage}", !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (paymentRecords.ToString(), "Payment Records", WildNestUI.Blue),
                        (StaffPortalUi.Peso(collected), "Collected Overall", WildNestUI.Green),
                        (StaffPortalUi.Peso(pendingValue), "Pending Value", WildNestUI.Amber),
                        (unpaidReservations.ToString(), "Reservations Needing Follow-up", WildNestUI.Red)),
                    StaffPortalUi.MetricTableCard(
                        "Collections Snapshot",
                        ("Collected Today", StaffPortalUi.Peso(collectedToday)),
                        ("Pending Payments", pendingPayments.ToString()),
                        ("Pending Value", StaffPortalUi.Peso(pendingValue)),
                        ("Unpaid Reservations", unpaidReservations.ToString()),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected"),
                        ("Report Generated", DateTime.Now.ToString("MMMM d, yyyy h:mm tt"))),
                    StaffPortalUi.GridCard("Payment Status Breakdown", paymentStatusBreakdown, "No payment status data available yet."),
                    StaffPortalUi.GridCard("Collections by Booking Type", collectionsByBookingType, "No booking collection data available yet."),
                    StaffPortalUi.GridCard("Revenue by Payment Method", paymentMethods, "No payment method data available yet."),
                    StaffPortalUi.GridCard("Payment Ledger", payments, "No payment records found yet.")
                });

            Controls.Add(page);
        }
    }
}
