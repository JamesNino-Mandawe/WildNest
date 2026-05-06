using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerGuests : UserControl
    {
        public UcManagerGuests()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMessage);

            int totalGuests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests;");
            int newThisMonth = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE YEAR(CreatedAt)=YEAR(CURDATE()) AND MONTH(CreatedAt)=MONTH(CURDATE());");
            int foreignGuests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE Nationality IS NOT NULL AND Nationality <> '' AND Nationality <> 'Filipino';");
            int repeatGuests = StaffPortalDb.Count(@"
SELECT COUNT(*) FROM (
    SELECT GuestID
    FROM tbl_reservations
    GROUP BY GuestID
    HAVING COUNT(*) > 1
) x;");
            int missingPhones = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE Phone IS NULL OR TRIM(Phone) = '';");
            int withRequests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE SpecialRequests IS NOT NULL AND TRIM(SpecialRequests) <> '';");

            var nationalityBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Nationality), ''), 'Unspecified') AS `Nationality`,
       COUNT(*) AS `Guests`
FROM tbl_guests
GROUP BY COALESCE(NULLIF(TRIM(Nationality), ''), 'Unspecified')
ORDER BY `Guests` DESC
LIMIT 10;");

            var guests = StaffPortalDb.GetTable(@"
SELECT g.GuestID AS `Guest ID`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest Name`,
       g.Email,
       g.Phone AS `Contact No.`,
       g.Nationality,
       g.ValidIDType AS `Valid ID`,
       COUNT(r.ReservationID) AS `Reservation Count`,
       DATE_FORMAT(g.CreatedAt, '%Y-%m-%d') AS `Created`
FROM tbl_guests g
LEFT JOIN tbl_reservations r ON r.GuestID = g.GuestID
GROUP BY g.GuestID, g.FirstName, g.LastName, g.Email, g.Phone, g.Nationality, g.ValidIDType, g.CreatedAt
ORDER BY g.CreatedAt DESC;");

            var page = StaffPortalUi.BuildPage(
                "Guest Intelligence",
                "Live guest directory with profile quality, repeat-visit behavior, and nationality composition.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbHealthy
                        ? "Guest records are live and available for service, personalization, and follow-up planning."
                        : dbMessage, !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (totalGuests.ToString(), "Total Guests", WildNestUI.Blue),
                        (newThisMonth.ToString(), "New This Month", WildNestUI.Green),
                        (foreignGuests.ToString(), "Foreign Nationals", WildNestUI.Amber),
                        (repeatGuests.ToString(), "Repeat Guests", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Guest Data Quality",
                        ("Missing Phone Numbers", missingPhones.ToString()),
                        ("Special Requests Logged", withRequests.ToString()),
                        ("Repeat Guests", repeatGuests.ToString()),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Nationality Breakdown", nationalityBreakdown, "No nationality data available yet."),
                    StaffPortalUi.GridCard("Guest Directory", guests, "No guest records found.")
                });

            Controls.Add(page);
        }
    }
}
