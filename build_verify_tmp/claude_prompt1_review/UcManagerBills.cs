// ============================================================
//  UcManagerBills.cs   (namespace Project.UcManager)
//  REPLACES: UcAdminBills.cs
//
//  CHANGE SUMMARY
//  • Namespace moved to Project.UcManager
//  • Class renamed UcManagerBills
//  • Page subtitle updated
//  • All SQL queries and StaffPortalUi calls: unchanged
// ============================================================

using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public partial class UcManagerBills : UserControl
    {
        public UcManagerBills()
        {
            Dock  = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMsg);

            decimal collected          = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            int     pendingPayments    = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status IN ('Pending','Unpaid');");
            int     paymentRecords     = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments;");
            int     unpaidReservations = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_reservations r
LEFT JOIN tbl_payments p ON p.ReservationID = r.ReservationID
WHERE p.PaymentID IS NULL OR p.Status IN ('Pending','Unpaid');");
            decimal collectedToday     = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");

            var statusBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Status),''),'Unspecified') AS `Status`,
       COUNT(*) AS `Records`,
       COALESCE(SUM(Amount),0) AS `Amount`
FROM tbl_payments
GROUP BY COALESCE(NULLIF(TRIM(Status),''),'Unspecified')
ORDER BY `Records` DESC;");

            var payments = StaffPortalDb.GetTable(@"
SELECT p.PaymentID      AS `Payment ID`,
       p.ReservationID  AS `Reservation Ref`,
       CONCAT(g.FirstName,' ',g.LastName) AS `Guest`,
       p.Amount,
       p.PaymentMethod  AS `Payment Method`,
       p.Status,
       DATE_FORMAT(p.PaidAt,'%Y-%m-%d %H:%i') AS `Paid At`
FROM tbl_payments p
LEFT JOIN tbl_reservations r ON r.ReservationID = p.ReservationID
LEFT JOIN tbl_guests g       ON g.GuestID       = r.GuestID
ORDER BY p.PaidAt DESC, p.PaymentID DESC;");

            var page = StaffPortalUi.BuildPage(
                "Billing & Reports",
                "Payment tracking from tbl_payments tied to live reservation records.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbMsg, !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (paymentRecords.ToString(),          "Payment Records",                WildNestUI.Blue),
                        (StaffPortalUi.Peso(collected),      "Total Collected",                WildNestUI.Green),
                        (pendingPayments.ToString(),         "Pending / Unpaid",               WildNestUI.Amber),
                        (unpaidReservations.ToString(),      "Reservations Needing Follow-up", WildNestUI.Red)),
                    StaffPortalUi.MetricTableCard(
                        "Billing Snapshot",
                        ("Collected Today",     StaffPortalUi.Peso(collectedToday)),
                        ("Pending Payments",    pendingPayments.ToString()),
                        ("Unpaid Reservations", unpaidReservations.ToString()),
                        ("DB Health",           dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Payment Status Breakdown", statusBreakdown, "No payment status data available."),
                    StaffPortalUi.GridCard("Payment Ledger",           payments,        "No payment records found yet.")
                });

            Controls.Add(page);
        }
    }
}
