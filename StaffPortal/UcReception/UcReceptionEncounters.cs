using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcReception
{
    public partial class UcReceptionEncounters : UserControl
    {
        public UcReceptionEncounters()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            int totalExperiences = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");
            int bookedToday = StaffPortalDb.Count(@"
SELECT COALESCE(SUM(be.Quantity),0)
FROM tbl_bookingexperiences be
JOIN tbl_reservations r ON r.ReservationID = be.ReservationID
WHERE COALESCE(r.VisitDate, r.CheckInDate) = CURDATE();");
            int bookedAllTime = StaffPortalDb.Count("SELECT COALESCE(SUM(Quantity),0) FROM tbl_bookingexperiences;");
            decimal revenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalCost),0) FROM tbl_bookingexperiences;");

            var encounters = StaffPortalDb.GetTable(@"
SELECT e.ExperienceName AS `Experience`,
       e.PricePerPerson AS `Price`,
       e.DurationMinutes AS `Duration (mins)`,
       COALESCE(SUM(be.Quantity),0) AS `Booked Slots`,
       COALESCE(SUM(be.TotalCost),0) AS `Revenue`
FROM tbl_experiences e
LEFT JOIN tbl_bookingexperiences be ON be.ExperienceID = e.ExperienceID
GROUP BY e.ExperienceName, e.PricePerPerson, e.DurationMinutes
ORDER BY `Booked Slots` DESC, e.ExperienceName;");

            var page = StaffPortalUi.BuildPage(
                "Book Encounter",
                "Reception encounter reference using tbl_experiences and booking volume.",
                new List<Control>
                {
                    StaffPortalUi.StatsRow(
                        (totalExperiences.ToString(), "Packages", WildNestUI.Blue),
                        (bookedToday.ToString(), "Booked Slots Today", WildNestUI.Green),
                        (bookedAllTime.ToString(), "Booked Slots Total", WildNestUI.Amber),
                        (StaffPortalUi.Peso(revenue), "Encounter Revenue", WildNestUI.Green)),
                    StaffPortalUi.GridCard("Encounter Catalogue & Demand", encounters)
                });

            Controls.Add(page);
        }
    }
}
