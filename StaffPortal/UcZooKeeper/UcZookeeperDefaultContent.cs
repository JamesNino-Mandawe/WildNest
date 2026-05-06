using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Project.UcZooKeeper
{
    public class UcZookeeperDefaultContent : UserControl
    {
        public UcZookeeperDefaultContent()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool hasAnimals = StaffPortalDb.TableExists("tbl_animals");
            bool hasFeedings = StaffPortalDb.TableExists("tbl_feedings");
            bool hasHealthRecords = StaffPortalDb.TableExists("tbl_healthrecords");
            if (hasHealthRecords)
                StaffPortalDb.EnsureHealthAlertColumns();

            int zooKeepers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Zoo%';");
            int animals = hasAnimals ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_animals;") : 0;
            int feedingsToday = hasFeedings ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_feedings WHERE FeedingDate = CURDATE();") : 0;
            int openHealthAlerts = hasHealthRecords ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_healthrecords WHERE IsAlert = 1 AND IsCleared = 0;") : 0;

            var demand = hasAnimals
                ? StaffPortalDb.GetTable(@"
SELECT a.AnimalID AS `Animal ID`,
       a.AnimalName AS `Animal`,
       a.Species,
       a.ZoneName AS `Zone`,
       a.HealthStatus AS `Health`,
       CASE WHEN a.IsEncounterEligible = 1 THEN 'Eligible' ELSE 'Restricted' END AS `Encounter Status`
FROM tbl_animals a
ORDER BY a.ZoneName, a.AnimalName;")
                : StaffPortalDb.GetTable(@"
SELECT e.ExperienceName AS `Experience`,
       e.DurationMinutes AS `Duration (mins)`,
       COALESCE(SUM(be.Quantity),0) AS `Booked Slots`,
       COALESCE(SUM(be.TotalCost),0) AS `Revenue`
FROM tbl_experiences e
LEFT JOIN tbl_bookingexperiences be ON be.ExperienceID = e.ExperienceID
GROUP BY e.ExperienceName, e.DurationMinutes
ORDER BY `Booked Slots` DESC, e.ExperienceName;");

            var alertByZone = hasHealthRecords && hasAnimals
                ? StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(a.ZoneName,''), 'Unassigned Zone') AS `Zone`,
       COUNT(*) AS `Open Alerts`
FROM tbl_healthrecords hr
JOIN tbl_animals a ON a.AnimalID = hr.AnimalID
WHERE hr.IsAlert = 1
  AND hr.IsCleared = 0
GROUP BY COALESCE(NULLIF(a.ZoneName,''), 'Unassigned Zone')
ORDER BY `Open Alerts` DESC, `Zone`;")
                : new DataTable();

            var encounterEligibility = hasAnimals
                ? StaffPortalDb.GetTable(@"
SELECT CASE WHEN IsEncounterEligible = 1 THEN 'Eligible' ELSE 'Restricted' END AS `Encounter Status`,
       COUNT(*) AS `Animals`
FROM tbl_animals
GROUP BY CASE WHEN IsEncounterEligible = 1 THEN 'Eligible' ELSE 'Restricted' END
ORDER BY `Animals` DESC;")
                : new DataTable();

            Controls.Add(StaffPortalUi.BuildPage(
                "Wildlife Operations",
                hasAnimals
                    ? "ZooKeeper dashboard backed by animal, feeding, and health records."
                    : "ZooKeeper dashboard using the current live schema. Animal-specific registry tables are not present yet.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        hasAnimals ? "Wildlife Registry Active" : "Schema Gap for Animal Operations",
                        hasAnimals
                            ? "Animal registry, feeding, and health tables are now available. Use the Animal ID shown in the Live Animal Registry Snapshot whenever you record health history, flag an active alert, clear an animal, or schedule feeding."
                            : "Your current SQL does not include animal registry, feeding, health record, or enclosure tables yet. This role is therefore using wildlife experience demand and staffing context as the closest operational data until those tables are added.",
                        alert: !hasAnimals),
                    StaffPortalUi.StatsRow(
                        (zooKeepers.ToString(), "ZooKeeper Accounts", WildNestUI.Blue),
                        (animals.ToString(), "Animals Registered", WildNestUI.Green),
                        (feedingsToday.ToString(), "Feedings Today", WildNestUI.Amber),
                        (openHealthAlerts.ToString(), "Open Health Alerts", WildNestUI.Red)),
                    StaffPortalUi.TrendCard(
                        "Open Health Alerts by Zone",
                        ToTrendPoints(alertByZone, "Zone", "Open Alerts", WildNestUI.Red, valueSuffix: " alerts"),
                        "No open health alerts by zone right now."),
                    StaffPortalUi.TrendCard(
                        "Encounter Eligibility Mix",
                        ToTrendPoints(encounterEligibility, "Encounter Status", "Animals", WildNestUI.Green, valueSuffix: " animals"),
                        "No animal eligibility data available yet."),
                    StaffPortalUi.GridCard(hasAnimals ? "Live Animal Registry Snapshot" : "Wildlife Demand Snapshot", demand)
                }));
        }

        private static IEnumerable<(string Label, decimal Value, string Display, Color Color)> ToTrendPoints(
            DataTable table,
            string labelColumn,
            string valueColumn,
            Color color,
            string valuePrefix = "",
            string valueSuffix = "")
        {
            return table.Rows
                .Cast<DataRow>()
                .Select(row =>
                {
                    decimal value = row[valueColumn] == DBNull.Value ? 0m : Convert.ToDecimal(row[valueColumn]);
                    string label = Convert.ToString(row[labelColumn]) ?? "N/A";
                    return (
                        label,
                        value,
                        $"{valuePrefix}{value:N0}{valueSuffix}".Trim(),
                        color);
                })
                .ToList();
        }
    }
}
