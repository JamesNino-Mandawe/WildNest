using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerCabins : UserControl
    {
        public UcManagerCabins()
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

            int totalCabins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins;");
            int available = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int unavailable = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status <> 'Available';");
            int activeUsage = StaffPortalDb.Count("SELECT COUNT(DISTINCT CabinID) FROM tbl_reservations WHERE CabinID IS NOT NULL AND Status IN ('Confirmed','Checked-In','Overdue');");
            decimal averageRate = StaffPortalDb.Sum("SELECT COALESCE(AVG(PricePerNight),0) FROM tbl_cabins;");
            decimal inventoryValue = StaffPortalDb.Sum("SELECT COALESCE(SUM(PricePerNight),0) FROM tbl_cabins;");
            decimal occupiedRate = totalCabins == 0 ? 0m : System.Math.Round((activeUsage / (decimal)totalCabins) * 100m, 1);

            var cabinStatusBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Status), ''), 'Unspecified') AS `Status`,
       COUNT(*) AS `Cabins`,
       COALESCE(AVG(PricePerNight),0) AS `Avg Rate`
FROM tbl_cabins
GROUP BY COALESCE(NULLIF(TRIM(Status), ''), 'Unspecified')
ORDER BY `Cabins` DESC;");

            var cabins = StaffPortalDb.GetTable(@"
SELECT c.CabinID AS `Cabin ID`,
       c.CabinName AS `Cabin`,
       c.PricePerNight AS `Price / Night`,
       c.MaxGuests AS `Max Guests`,
       c.Status,
       COALESCE(MAX(CASE WHEN r.Status IN ('Confirmed','Checked-In','Overdue') THEN r.ReservationID END), '-') AS `Current Reservation`
FROM tbl_cabins c
LEFT JOIN tbl_reservations r ON r.CabinID = c.CabinID
GROUP BY c.CabinID, c.CabinName, c.PricePerNight, c.MaxGuests, c.Status
ORDER BY c.CabinName;");

            var page = StaffPortalUi.BuildPage(
                "Cabin Operations",
                "Manager visibility into live accommodation inventory, occupancy, and nightly value performance.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbHealthy
                        ? "Accommodation inventory is connected and ready for live operational monitoring."
                        : dbMessage, !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (totalCabins.ToString(), "Total Cabins", WildNestUI.Blue),
                        (available.ToString(), "Available Now", WildNestUI.Green),
                        (unavailable.ToString(), "Reserved / Offline", WildNestUI.Amber),
                        (activeUsage.ToString(), "Cabins in Active Use", WildNestUI.Red)),
                    StaffPortalUi.MetricTableCard(
                        "Accommodation Performance",
                        ("Average Nightly Rate", StaffPortalUi.Peso(averageRate)),
                        ("Inventory Nightly Value", StaffPortalUi.Peso(inventoryValue)),
                        ("Occupancy by Active Use", occupiedRate.ToString("N1") + "%"),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Cabin Status Breakdown", cabinStatusBreakdown, "No cabin status data available yet."),
                    StaffPortalUi.GridCard("Cabin Inventory Register", cabins, "No cabins found.")
                });

            Controls.Add(page);
        }
    }
}
