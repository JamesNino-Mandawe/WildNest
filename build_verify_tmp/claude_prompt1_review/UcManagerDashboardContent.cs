// ============================================================
//  UcManagerDashboardContent.cs   (namespace Project.UcManager)
//  REPLACES: UcAdminDashboardContent.cs
//
//  CHANGE SUMMARY
//  • Namespace moved to Project.UcManager
//  • Page title updated to "Manager Dashboard"
//  • Overdue-stay alert banner added at the top
//  • Today's revenue KPI card added to the StatsRow
//  • All SQL queries, StaffPortalUi calls, ToTrendPoints — preserved exactly
// ============================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerDashboardContent : UserControl
    {
        public UcManagerDashboardContent()
        {
            Dock      = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load     += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            bool dbOk = StaffPortalDb.CanConnect(out string dbMessage);

            // ── Scalar metrics (identical SQL to original) ─────────────
            int     cabinsAvailable    = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int     activeReservations = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status IN ('Confirmed','Checked-In','Overdue');");
            decimal collected          = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            int     activeUsers        = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 1;");
            int     totalReservations  = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations;");
            int     todayBookings      = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE DATE(CreatedAt) = CURDATE();");
            int     checkedIn          = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Checked-In';");
            int     overdue            = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Overdue';");
            int     pendingPayments    = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status NOT IN ('Paid','Completed','Settled');");
            decimal accommodationRev   = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalAmount),0) FROM tbl_reservations WHERE BookingType IN ('CabinStay','FullStayExperience');");
            decimal visitRev           = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalAmount),0) FROM tbl_reservations WHERE BookingType IN ('DayVisit','ExperienceVisit');");
            int     guestChats         = StaffPortalDb.TableExists("tbl_chat")         ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_chat;")         : 0;
            int     staffChats         = StaffPortalDb.TableExists("tbl_staffmessages") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_staffmessages;") : 0;
            int     totalCabins        = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins;");
            decimal occupancyRate      = totalCabins == 0 ? 0m : Math.Round((activeReservations / (decimal)totalCabins) * 100m, 1);

            // NEW — today's collected revenue for the top KPI row
            decimal todayRevenue = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt) = CURDATE() AND Status IN ('Paid','Completed','Settled');");

            // ── Table queries (identical to original) ──────────────────
            var reservations = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName,' ',g.LastName) AS `Guest`,
       COALESCE(c.CabinName,'No Cabin / Visit Booking') AS `Cabin`,
       COALESCE(DATE_FORMAT(r.CheckInDate,'%Y-%m-%d'),DATE_FORMAT(r.VisitDate,'%Y-%m-%d')) AS `Start`,
       DATE_FORMAT(r.CheckOutDate,'%Y-%m-%d') AS `End`,
       r.BookingType AS `Type`,
       r.Status,
       r.TotalAmount AS `Total Amount`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
ORDER BY r.CreatedAt DESC
LIMIT 12;");

            var bookingTypes = StaffPortalDb.GetTable(@"
SELECT BookingType AS `Booking Type`,
       COUNT(*) AS `Reservations`,
       COALESCE(SUM(TotalAmount),0) AS `Revenue`
FROM tbl_reservations
GROUP BY BookingType
ORDER BY `Reservations` DESC, `Revenue` DESC;");

            var topCabins = StaffPortalDb.GetTable(@"
SELECT COALESCE(c.CabinName,'Walk-In / Visit Booking') AS `Cabin`,
       COUNT(*) AS `Bookings`,
       COALESCE(SUM(r.TotalAmount),0) AS `Revenue`
FROM tbl_reservations r
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
GROUP BY COALESCE(c.CabinName,'Walk-In / Visit Booking')
ORDER BY `Bookings` DESC, `Revenue` DESC
LIMIT 5;");

            var revenueTrend = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(PaidAt,'%Y-%m') AS `Month`,
       COALESCE(SUM(Amount),0) AS `Revenue`
FROM tbl_payments
WHERE PaidAt IS NOT NULL
GROUP BY DATE_FORMAT(PaidAt,'%Y-%m')
ORDER BY `Month` DESC
LIMIT 6;");

            var occupancyByStatus = StaffPortalDb.GetTable(@"
SELECT Status AS `Status`, COUNT(*) AS `Cabins`
FROM tbl_cabins
GROUP BY Status
ORDER BY `Cabins` DESC;");

            // ── Page sections ──────────────────────────────────────────
            var sections = new List<Control>
            {
                // DB health card
                StaffPortalUi.MessageCard(
                    dbOk ? "Database Connection Healthy" : "Database Connection Warning",
                    dbOk ? dbMessage : $"Dashboard could not confirm a healthy connection: {dbMessage}",
                    alert: !dbOk),

                // NEW — overdue-stay operational alert
                WildNestUI.AlertBanner(
                    overdue > 0
                        ? $"⚠  {overdue} reservation(s) are OVERDUE and need immediate follow-up."
                        : "✔  No overdue reservations at this time.",
                    isAlert: overdue > 0),

                // Smart search (unchanged)
                new UcAdminSmartSearch(),

                WildNestUI.AlertBanner(
                    "Manager dashboard is reading live from cabins, reservations, guests, payments, and users.",
                    isAlert: false),

                // KPI row — today's revenue replaces the 4th card from original
                StaffPortalUi.StatsRow(
                    (cabinsAvailable.ToString(),       "Cabins Available",      WildNestUI.Green),
                    (activeReservations.ToString(),    "Active Reservations",   WildNestUI.Blue),
                    (StaffPortalUi.Peso(collected),    "Total Collected",       WildNestUI.Amber),
                    (StaffPortalUi.Peso(todayRevenue), "Revenue Today",         WildNestUI.Green)),

                // Operations snapshot (same as original, unchanged)
                StaffPortalUi.MetricTableCard(
                    "Operations Snapshot",
                    ("Total Reservations",           totalReservations.ToString()),
                    ("Bookings Created Today",       todayBookings.ToString()),
                    ("Currently Checked-In",         checkedIn.ToString()),
                    ("Overdue Stays",                overdue.ToString()),
                    ("Payments Still Pending",       pendingPayments.ToString()),
                    ("Occupancy Rate",               occupancyRate.ToString("N1") + "%"),
                    ("Accommodation Revenue",        StaffPortalUi.Peso(accommodationRev)),
                    ("Visit / Experience Revenue",   StaffPortalUi.Peso(visitRev)),
                    ("Active Staff Users",           activeUsers.ToString()),
                    ("Guest Chat Messages",          guestChats.ToString()),
                    ("Internal Staff Messages",      staffChats.ToString())),

                // Trend charts (unchanged)
                StaffPortalUi.TrendCard(
                    "Monthly Revenue Trend",
                    ToTrendPoints(revenueTrend,      "Month",  "Revenue",  WildNestUI.Amber),
                    "No payment trend data available yet."),

                StaffPortalUi.TrendCard(
                    "Top Booked Cabins",
                    ToTrendPoints(topCabins,         "Cabin",  "Bookings", WildNestUI.Green, valueSuffix: " bookings"),
                    "No cabin booking data available yet."),

                StaffPortalUi.TrendCard(
                    "Cabin Occupancy Snapshot",
                    ToTrendPoints(occupancyByStatus, "Status", "Cabins",   WildNestUI.Blue,  valueSuffix: " cabins"),
                    "No cabin occupancy data available yet."),

                StaffPortalUi.GridCard("Booking Type Analytics",      bookingTypes,  "No booking analytics available yet."),
                StaffPortalUi.GridCard("Recent Reservation Activity", reservations,  "No reservation records found yet.")
            };

            var page = StaffPortalUi.BuildPage(
                "Manager Dashboard",
                $"Live resort overview — wildnest_db  ·  {DateTime.Now:MMMM d, yyyy}",
                sections);

            Controls.Add(page);
        }

        // ── Shared helper (identical to original) ─────────────────────────

        private static IEnumerable<(string Label, decimal Value, string Display, Color Color)> ToTrendPoints(
            DataTable table, string labelColumn, string valueColumn,
            Color color, string valuePrefix = "", string valueSuffix = "")
        {
            return table.Rows.Cast<DataRow>().Select(row =>
            {
                decimal v = row[valueColumn] == DBNull.Value ? 0m : Convert.ToDecimal(row[valueColumn]);
                string  l = Convert.ToString(row[labelColumn]) ?? "N/A";
                return (l, v, $"{valuePrefix}{v:N0}{valueSuffix}".Trim(), color);
            }).ToList();
        }
    }
}
