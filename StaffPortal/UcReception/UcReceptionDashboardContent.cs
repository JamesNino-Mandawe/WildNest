using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcReception
{
    public class UcReceptionDashboardContent : UserControl
    {
        public UcReceptionDashboardContent()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            bool dbOk = StaffPortalDb.CanConnect(out string dbMessage);

            int arrivals = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckInDate = CURDATE() OR VisitDate = CURDATE();");
            int activeReservations = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status IN ('Confirmed','Checked-In','Overdue');");
            int availableCabins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int guests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests;");
            int expectedDepartures = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckOutDate = CURDATE() AND Status = 'Checked-In';");
            int overdue = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Overdue';");
            int unpaid = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status NOT IN ('Paid','Completed','Settled');");
            decimal todayRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt) = CURDATE();");
            int nightSafariTonight = StaffPortalDb.TableExists("tbl_bookingexperiences") && StaffPortalDb.TableExists("tbl_experiences")
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_bookingexperiences be
JOIN tbl_experiences e ON e.ExperienceID = be.ExperienceID
JOIN tbl_reservations r ON r.ReservationID = be.ReservationID
WHERE COALESCE(r.VisitDate, r.CheckInDate) = CURDATE()
  AND e.ExperienceName LIKE '%Night Safari%';")
                : 0;

            var arrivalsTable = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS `Cabin or Visit`,
       COALESCE(r.ArrivalTime, 'Front Desk Confirmation') AS `Arrival Time`,
       r.BookingType AS `Booking Type`,
       r.Status
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.CheckInDate = CURDATE() OR r.VisitDate = CURDATE()
ORDER BY r.CreatedAt DESC;");

            var activeTable = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(c.CabinName, 'Day Visit / Experience') AS `Cabin or Visit`,
       COALESCE(DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'), DATE_FORMAT(r.VisitDate, '%Y-%m-%d')) AS `Start`,
       DATE_FORMAT(r.CheckOutDate, '%Y-%m-%d') AS `End`,
       r.Status,
       r.TotalAmount AS `Total Amount`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.Status IN ('Confirmed','Checked-In','Overdue')
ORDER BY r.CreatedAt DESC;");

            var cabinStatus = StaffPortalDb.GetTable(@"
SELECT CabinName AS `Cabin`,
       Status,
       PricePerNight AS `Nightly Rate`,
       MaxGuests AS `Capacity`
FROM tbl_cabins
ORDER BY CabinName;");

            var page = StaffPortalUi.BuildPage(
                "Front Desk Dashboard",
                $"Live reception overview from reservations, cabins, and guests - {DateTime.Now:MMMM d, yyyy}",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        dbOk ? "Reception Data Connection Healthy" : "Reception Data Connection Warning",
                        dbOk ? dbMessage : $"The reception dashboard could not confirm a healthy connection: {dbMessage}",
                        alert: !dbOk),
                    StaffPortalUi.MessageCard(
                        nightSafariTonight > 0 ? "Night Safari Sessions Scheduled Tonight" : "No Night Safari Sessions Scheduled Tonight",
                        nightSafariTonight > 0
                            ? $"Reception has {nightSafariTonight} Night Safari booking line(s) tied to tonight's operations. Guests may arrive earlier, but the safari session itself remains scheduled for the evening window."
                            : "No Night Safari sessions are currently tied to today's bookings, so all same-day visits follow the normal daytime arrival flow.",
                        alert: false),
                    StaffPortalUi.StatsRow(
                        (arrivals.ToString(), "Arrivals / Visits Today", WildNestUI.Green),
                        (activeReservations.ToString(), "Active Reservations", WildNestUI.Blue),
                        (availableCabins.ToString(), "Available Cabins", WildNestUI.Amber),
                        (guests.ToString(), "Guest Profiles", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Front Desk Snapshot",
                        ("Expected Departures Today", expectedDepartures.ToString()),
                        ("Overdue Stays Needing Action", overdue.ToString()),
                        ("Payments Pending Follow-up", unpaid.ToString()),
                        ("Night Safari Sessions Tonight", nightSafariTonight.ToString()),
                        ("Revenue Collected Today", StaffPortalUi.Peso(todayRevenue)),
                        ("Guest Profiles in System", guests.ToString()),
                        ("Database Health", dbOk ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Today's Arrivals", arrivalsTable, "No arrivals or day visits scheduled today."),
                    StaffPortalUi.GridCard("Active Front Desk Reservations", activeTable),
                    StaffPortalUi.GridCard("Cabin Status Overview", cabinStatus, "No cabin records found.")
                });

            Controls.Add(page);
        }
    }
}
