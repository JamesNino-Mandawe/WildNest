using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcReception
{
    public partial class UcReceptionGuests : UserControl
    {
        public UcReceptionGuests()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            int totalGuests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests;");
            int newToday = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE DATE(CreatedAt) = CURDATE();");
            int repeatGuests = StaffPortalDb.Count(@"
SELECT COUNT(*) FROM (
  SELECT GuestID
  FROM tbl_reservations
  GROUP BY GuestID
  HAVING COUNT(*) > 1
) x;");
            int guestsWithRequests = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests WHERE SpecialRequests IS NOT NULL AND TRIM(SpecialRequests) <> '';");

            var guests = StaffPortalDb.GetTable(@"
SELECT g.GuestID AS `Guest ID`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest Name`,
       g.Email,
       g.Phone AS `Phone`,
       g.Nationality,
       g.ValidIDType AS `ID Type`,
       COUNT(r.ReservationID) AS `Reservations`,
       COALESCE(MAX(r.Status), 'No Booking Yet') AS `Latest Status`
FROM tbl_guests g
LEFT JOIN tbl_reservations r ON r.GuestID = g.GuestID
GROUP BY g.GuestID, g.FirstName, g.LastName, g.Email, g.Phone, g.Nationality, g.ValidIDType
ORDER BY g.CreatedAt DESC;");

            var page = StaffPortalUi.BuildPage(
                "Guest Profiles",
                "Front desk guest lookup using tbl_guests and reservation history.",
                new List<Control>
                {
                    StaffPortalUi.StatsRow(
                        (totalGuests.ToString(), "Guest Profiles", WildNestUI.Blue),
                        (newToday.ToString(), "Added Today", WildNestUI.Green),
                        (repeatGuests.ToString(), "Repeat Guests", WildNestUI.Amber),
                        (guestsWithRequests.ToString(), "With Special Requests", WildNestUI.Green)),
                    StaffPortalUi.GridCard("Guest Directory", guests)
                });

            Controls.Add(page);
        }
    }
}
