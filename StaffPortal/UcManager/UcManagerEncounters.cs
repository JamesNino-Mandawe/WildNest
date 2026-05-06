using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerEncounters : UserControl
    {
        public UcManagerEncounters()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            int experienceCount = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");
            int bookedSlots = StaffPortalDb.Count("SELECT COALESCE(SUM(Quantity),0) FROM tbl_bookingexperiences;");
            decimal revenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalCost),0) FROM tbl_bookingexperiences;");
            int averageDuration = StaffPortalDb.Count("SELECT COALESCE(AVG(DurationMinutes),0) FROM tbl_experiences;");

            var encounters = StaffPortalDb.GetTable(@"
SELECT e.ExperienceID AS `Experience ID`,
       e.ExperienceName AS `Experience`,
       e.PricePerPerson AS `Price Per Person`,
       e.DurationMinutes AS `Duration (mins)`,
       COALESCE(SUM(be.Quantity), 0) AS `Booked Slots`,
       COALESCE(SUM(be.TotalCost), 0) AS `Revenue`
FROM tbl_experiences e
LEFT JOIN tbl_bookingexperiences be ON be.ExperienceID = e.ExperienceID
GROUP BY e.ExperienceID, e.ExperienceName, e.PricePerPerson, e.DurationMinutes
ORDER BY e.ExperienceName;");

            var monthlyEncounterRevenue = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(COALESCE(r.CreatedAt, CURDATE()), '%Y-%m') AS `Month`,
       COALESCE(SUM(be.TotalCost),0) AS `Revenue`
FROM tbl_bookingexperiences be
JOIN tbl_reservations r ON r.ReservationID = be.ReservationID
GROUP BY DATE_FORMAT(COALESCE(r.CreatedAt, CURDATE()), '%Y-%m')
ORDER BY `Month` DESC
LIMIT 6;");

            var topExperiences = StaffPortalDb.GetTable(@"
SELECT e.ExperienceName AS `Experience`,
       COALESCE(SUM(be.Quantity),0) AS `Booked Slots`
FROM tbl_experiences e
LEFT JOIN tbl_bookingexperiences be ON be.ExperienceID = e.ExperienceID
GROUP BY e.ExperienceName
ORDER BY `Booked Slots` DESC, e.ExperienceName
LIMIT 5;");

            var page = StaffPortalUi.BuildPage(
                "Experience Portfolio",
                "Manager oversight of encounter demand, experience revenue, and package performance.",
                new List<Control>
                {
                    StaffPortalUi.StatsRow(
                        (experienceCount.ToString(), "Experience Packages", WildNestUI.Blue),
                        (bookedSlots.ToString(), "Booked Slots", WildNestUI.Green),
                        (StaffPortalUi.Peso(revenue), "Encounter Revenue", WildNestUI.Amber),
                        ($"{averageDuration} min", "Average Duration", WildNestUI.Green)),
                    StaffPortalUi.TrendCard(
                        "Most Popular Experiences",
                        ToTrendPoints(topExperiences, "Experience", "Booked Slots", WildNestUI.Green, valueSuffix: " slots"),
                        "No experience demand data available yet."),
                    StaffPortalUi.TrendCard(
                        "Monthly Experience Revenue",
                        ToTrendPoints(monthlyEncounterRevenue, "Month", "Revenue", WildNestUI.Amber, valuePrefix: "PHP "),
                        "No encounter revenue trend available yet."),
                    StaffPortalUi.GridCard("Experience Performance", encounters, "No experience records found.")
                });

            Controls.Add(page);
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
                    return (label, value, $"{valuePrefix}{value:N0}{valueSuffix}".Trim(), color);
                })
                .ToList();
        }
    }
}
