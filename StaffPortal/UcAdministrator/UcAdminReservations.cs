using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcAdministrator
{
    public partial class UcAdminReservations : UserControl
    {
        public UcAdminReservations()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
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
            decimal bookedRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalAmount),0) FROM tbl_reservations;");
            int confirmed = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Confirmed';");
            int cancelled = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Cancelled';");

            var bookingTypeAnalytics = StaffPortalDb.GetTable(@"
SELECT r.BookingType AS `Booking Type`,
       COUNT(*) AS `Reservations`,
       COALESCE(SUM(r.TotalAmount),0) AS `Revenue`,
       COALESCE(AVG(r.TotalAmount),0) AS `Average Ticket`
FROM tbl_reservations r
GROUP BY r.BookingType
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
                "Reservations",
                "Full reservation register from tbl_reservations joined with guests and cabins.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbMessage, !dbHealthy),
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
                    StaffPortalUi.GridCard("Reservation Register", reservations)
                });

            Controls.Add(page);
        }
    }
}
