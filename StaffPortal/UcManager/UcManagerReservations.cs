using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerReservations : UserControl
    {
        public UcManagerReservations()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMessage);

            int total = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations;");
            int todayArrivals = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckInDate = CURDATE() OR VisitDate = CURDATE();");
            int checkedIn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Checked-In';");
            int overdue = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Overdue';");
            int confirmed = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Confirmed';");
            int cancelled = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Cancelled';");
            decimal bookedRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalAmount),0) FROM tbl_reservations;");

            var bookingTypeAnalytics = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(r.BookingType),''),'Unspecified') AS `Booking Type`,
       COUNT(*) AS `Reservations`,
       COALESCE(SUM(r.TotalAmount),0) AS `Revenue`,
       COALESCE(AVG(r.TotalAmount),0) AS `Average Ticket`
FROM tbl_reservations r
GROUP BY COALESCE(NULLIF(TRIM(r.BookingType),''),'Unspecified')
ORDER BY `Reservations` DESC;");

            var reservations = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(c.CabinName, 'No Cabin / Visit Booking') AS `Cabin`,
       r.BookingType AS `Booking Type`,
       COALESCE(DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'), DATE_FORMAT(r.VisitDate, '%Y-%m-%d')) AS `Start`,
       DATE_FORMAT(r.CheckOutDate, '%Y-%m-%d') AS `End`,
       r.NumAdults AS `Adults`,
       r.NumChildren AS `Children`,
       r.Status,
       r.TotalAmount AS `Total`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
ORDER BY r.CreatedAt DESC;");

            var page = StaffPortalUi.BuildPage(
                "Reservation Control",
                "Manager-grade oversight of arrivals, occupancy pressure, status mix, and total booking value.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbHealthy
                        ? "Reservation operations are synchronized with live guest and accommodation records."
                        : dbMessage, !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (total.ToString(), "All Reservations", WildNestUI.Blue),
                        (todayArrivals.ToString(), "Arrivals / Visits Today", WildNestUI.Green),
                        (checkedIn.ToString(), "Currently Checked-In", WildNestUI.Amber),
                        (StaffPortalUi.Peso(bookedRevenue), "Booked Revenue", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Reservation Snapshot",
                        ("Confirmed", confirmed.ToString()),
                        ("Checked-In", checkedIn.ToString()),
                        ("Overdue", overdue.ToString()),
                        ("Cancelled", cancelled.ToString()),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Booking Type Analytics", bookingTypeAnalytics, "No booking analytics available yet."),
                    StaffPortalUi.GridCard("Reservation Register", reservations, "No reservation records found.")
                });

            Controls.Add(page);
        }
    }
}
