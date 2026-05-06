using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcAdministrator
{
    public partial class UcAdminAnimals : UserControl
    {
        public UcAdminAnimals()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
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
                    "Animal Registry Not Yet in Current SQL Schema",
                    "Your current database does not include tbl_animals, feeding, health, or enclosure tables yet. This page is using experience-demand data as the closest live wildlife operations view until those animal-specific tables are added.",
                    alert: true));
            }

            sections.Add(StaffPortalUi.GridCard("Wildlife Experience Demand", experienceDemand));

            var page = StaffPortalUi.BuildPage(
                "Animal Registry / Wildlife Operations",
                hasAnimalTable
                    ? "Animal operations page using the current SQL schema."
                    : "Animal-specific tables are missing, so this page shows wildlife demand proxies from the current schema.",
                sections);

            Controls.Add(page);
        }
    }
}
