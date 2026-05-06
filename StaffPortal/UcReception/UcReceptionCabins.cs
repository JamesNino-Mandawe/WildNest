using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcReception
{
    public partial class UcReceptionCabins : UserControl
    {
        public UcReceptionCabins()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            int totalCabins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins;");
            int available = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int unavailable = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status <> 'Available';");
            int todayArrivals = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE CheckInDate = CURDATE();");

            var cabins = StaffPortalDb.GetTable(@"
SELECT c.CabinID,
       c.CabinName AS `Cabin`,
       c.PricePerNight AS `Price / Night`,
       c.MaxGuests AS `Max Guests`,
       c.Status,
       COALESCE(MAX(CASE WHEN r.Status IN ('Confirmed','Checked-In','Overdue') THEN CONCAT(r.ReservationID, ' / ', DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'), ' / ', r.Status) END), 'Open') AS `Current Booking`
FROM tbl_cabins c
LEFT JOIN tbl_reservations r ON r.CabinID = c.CabinID
GROUP BY c.CabinID, c.CabinName, c.PricePerNight, c.MaxGuests, c.Status
ORDER BY c.CabinName;");

            var page = StaffPortalUi.BuildPage(
                "Cabin Availability",
                "Reception-facing cabin overview from tbl_cabins and active reservations.",
                new List<Control>
                {
                    StaffPortalUi.StatsRow(
                        (totalCabins.ToString(), "Total Cabins", WildNestUI.Blue),
                        (available.ToString(), "Available", WildNestUI.Green),
                        (unavailable.ToString(), "Reserved / Unavailable", WildNestUI.Amber),
                        (todayArrivals.ToString(), "Check-Ins Today", WildNestUI.Green)),
                    StaffPortalUi.GridCard("Cabin Availability Board", cabins)
                });

            Controls.Add(page);
        }
    }
}
