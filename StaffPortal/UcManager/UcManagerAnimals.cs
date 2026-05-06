using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerAnimals : UserControl
    {
        public UcManagerAnimals()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool hasAnimalTable = StaffPortalDb.TableExists("tbl_animals");
            int zooKeepers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Zoo%';");
            int experiences = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");
            int todayWildlifeDemand = StaffPortalDb.Count(@"
SELECT COALESCE(SUM(be.Quantity),0)
FROM tbl_bookingexperiences be
JOIN tbl_reservations r ON r.ReservationID = be.ReservationID
WHERE COALESCE(r.VisitDate, r.CheckInDate) = CURDATE();");
            decimal encounterRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalCost),0) FROM tbl_bookingexperiences;");

            var experienceDemand = StaffPortalDb.GetTable(@"
SELECT e.ExperienceName AS `Experience`,
       e.DurationMinutes AS `Duration (mins)`,
       COALESCE(SUM(be.Quantity),0) AS `Booked Slots`,
       COALESCE(SUM(be.TotalCost),0) AS `Revenue`
FROM tbl_experiences e
LEFT JOIN tbl_bookingexperiences be ON be.ExperienceID = e.ExperienceID
GROUP BY e.ExperienceName, e.DurationMinutes
ORDER BY `Booked Slots` DESC, e.ExperienceName;");

            var sections = new List<Control>
            {
                StaffPortalUi.StatsRow(
                    (zooKeepers.ToString(), "ZooKeeper Accounts", WildNestUI.Blue),
                    (experiences.ToString(), "Wildlife Experiences", WildNestUI.Green),
                    (todayWildlifeDemand.ToString(), "Guest Slots Today", WildNestUI.Amber),
                    (StaffPortalUi.Peso(encounterRevenue), "Encounter Revenue", WildNestUI.Green))
            };

            if (!hasAnimalTable)
            {
                sections.Add(StaffPortalUi.MessageCard(
                    "Wildlife Registry Needs Schema Expansion",
                    "This manager view can already monitor wildlife demand and encounter revenue. For full registry operations, the animal-specific tables should remain active and populated so the manager can see the complete sanctuary picture.",
                    alert: true));
            }

            sections.Add(StaffPortalUi.GridCard("Wildlife Experience Demand", experienceDemand, "No wildlife experience data available."));

            var page = StaffPortalUi.BuildPage(
                "Wildlife Operations",
                hasAnimalTable
                    ? "Manager visibility into wildlife demand, encounter performance, and operational support capacity."
                    : "Manager demand view for wildlife-related experiences while deeper registry data is limited.",
                sections);

            Controls.Add(page);
        }
    }
}
