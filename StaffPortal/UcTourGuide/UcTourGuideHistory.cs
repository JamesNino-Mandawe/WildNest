using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcTourGuide
{
    public partial class UcTourGuideHistory : UserControl
    {
        private readonly string _displayName;
        private readonly string _username;

        public UcTourGuideHistory(string displayName = "", string username = "")
        {
            _displayName = displayName;
            _username = string.IsNullOrWhiteSpace(username) ? "tourguide" : username.Trim();
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private MySqlParameter GuideUserParam => new("@guideUsername", _username);

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.EnsureTourGuideSchedules();

            bool hasSchedules = StaffPortalDb.TableExists("tbl_tourschedules");
            bool hasCompletions = StaffPortalDb.TableExists("tbl_tourcompletions");

            int pastBookings = hasCompletions
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int guestsServed = hasSchedules && hasCompletions
                ? StaffPortalDb.Count(@"
SELECT COALESCE(SUM(be.Quantity),0)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            decimal historicRevenue = hasSchedules && hasCompletions
                ? StaffPortalDb.Sum(@"
SELECT COALESCE(SUM(be.TotalCost),0)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0m;

            int experienceCount = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");

            var history = hasSchedules && hasCompletions
                ? StaffPortalDb.GetTable(@"
SELECT tc.TourCompletionID AS `Completion ID`,
       ts.TourScheduleID AS `Schedule ID`,
       ts.TourDate AS `Tour Date`,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Walk-In Guest') AS `Guest`,
       e.ExperienceName AS `Experience`,
       COALESCE(be.Quantity, 0) AS `Attendees`,
       tc.CompletionStatus AS `Status`,
       tc.Remarks
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_reservations r ON r.ReservationID = ts.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_experiences e ON e.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername)
ORDER BY tc.CompletedAt DESC;", GuideUserParam)
                : new System.Data.DataTable();

            Controls.Add(StaffPortalUi.BuildPage(
                "Tour History",
                "Completed tours assigned to your guide account are tracked here for review and reporting.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        pastBookings > 0 ? "Completed History Loaded" : "No Completed Tours Yet",
                        pastBookings > 0
                            ? "Every completed schedule is stored here with its Schedule ID, guest, experience, and remarks for reporting."
                            : "Tour History will start filling only after a guide marks an assigned tour schedule as Completed in the Mark Complete page.",
                        alert: pastBookings == 0),
                    StaffPortalUi.StatsRow(
                        (pastBookings.ToString(), "Completed Tours", WildNestUI.Blue),
                        (guestsServed.ToString(), "Guests Served", WildNestUI.Green),
                        (StaffPortalUi.Peso(historicRevenue), "Historical Revenue", WildNestUI.Amber),
                        (experienceCount.ToString(), "Experience Types", WildNestUI.Green)),
                    StaffPortalUi.GridCard("Completed Tour History", history, "No completed history is currently recorded for this guide.")
                }));
        }
    }
}
