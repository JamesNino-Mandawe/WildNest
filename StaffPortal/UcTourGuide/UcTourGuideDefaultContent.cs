using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcTourGuide
{
    public class UcTourGuideDefaultContent : UserControl
    {
        private readonly string _displayName;
        private readonly string _username;

        public UcTourGuideDefaultContent(string displayName = "", string username = "")
        {
            _displayName = displayName;
            _username = string.IsNullOrWhiteSpace(username) ? "tourguide" : username.Trim();
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (s, e) => Render();
        }

        private MySqlParameter GuideUserParam => new("@guideUsername", _username);

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.EnsureTourGuideSchedules();

            bool hasSchedules = StaffPortalDb.TableExists("tbl_tourschedules");
            bool hasCompletions = StaffPortalDb.TableExists("tbl_tourcompletions");
            int openQueue = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules
WHERE GuideUserID IS NULL
  AND Status = 'Open for Claim';")
                : 0;

            int todayTours = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate = CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int todayGuests = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COALESCE(SUM(be.Quantity),0)
FROM tbl_tourschedules ts
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate = CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int completedToday = hasCompletions
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE DATE(tc.CompletedAt) = CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int totalScheduled = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int totalCompleted = hasCompletions
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            decimal completionRate = totalScheduled == 0 ? 0m : Math.Round((totalCompleted / (decimal)totalScheduled) * 100m, 1);

            decimal revenue = hasSchedules
                ? StaffPortalDb.Sum(@"
SELECT COALESCE(SUM(be.TotalCost),0)
FROM tbl_tourschedules ts
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0m;

            var manifest = hasSchedules
                ? StaffPortalDb.GetTable(@"
SELECT ts.TourScheduleID AS `Schedule ID`,
       ts.TourDate AS `Tour Date`,
       TIME_FORMAT(ts.StartTime, '%h:%i %p') AS `Start`,
       TIME_FORMAT(ts.EndTime, '%h:%i %p') AS `End`,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Walk-In Guest') AS `Guest`,
       e.ExperienceName AS `Experience`,
       COALESCE(be.Quantity, 0) AS `Attendees`,
       ts.Status
FROM tbl_tourschedules ts
LEFT JOIN tbl_reservations r ON r.ReservationID = ts.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_experiences e ON e.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername)
ORDER BY ts.TourDate DESC, ts.StartTime, `Guest`;", GuideUserParam)
                : new DataTable();

            var completionTrend = hasSchedules && hasCompletions
                ? StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(ts.TourDate, '%Y-%m') AS `Month`,
       COUNT(*) AS `Scheduled`,
       COALESCE(SUM(CASE WHEN tc.TourCompletionID IS NOT NULL THEN 1 ELSE 0 END),0) AS `Completed`
FROM tbl_tourschedules ts
LEFT JOIN tbl_tourcompletions tc ON tc.TourScheduleID = ts.TourScheduleID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername)
GROUP BY DATE_FORMAT(ts.TourDate, '%Y-%m')
ORDER BY `Month` DESC
LIMIT 6;", GuideUserParam)
                : new DataTable();

            string displayName = string.IsNullOrWhiteSpace(_displayName) ? "Tour Guide" : _displayName;

            Controls.Add(StaffPortalUi.BuildPage(
                "My Schedule",
                $"{displayName} - your assigned schedule, tour IDs, guests, and completion progress.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        hasSchedules ? "Guide Assignments Ready" : "Guide Assignment Note",
                        hasSchedules
                            ? "Experience bookings are converted into tour events. Open events stay in the shared queue until a tour guide claims them, then they appear in that guide's personal schedule."
                            : "No schedule table is available yet, so guide assignments cannot be shown correctly.",
                        alert: !hasSchedules),
                    StaffPortalUi.MessageCard(
                        openQueue > 0 ? "Shared Event Queue Active" : "No Open Events Waiting",
                        openQueue > 0
                            ? "There are unclaimed experience events waiting in the shared queue. A guide must claim an event first before it moves into their schedule and completion workflow."
                            : "For a guide to see work here, a booking must include an actual experience and that experience event must either still be open for claim or already claimed by this guide.",
                        alert: openQueue == 0 && totalScheduled == 0),
                    StaffPortalUi.StatsRow(
                        (todayTours.ToString(), "Tours Today", WildNestUI.Green),
                        (todayGuests.ToString(), "Guests Today", WildNestUI.Blue),
                        (completedToday.ToString(), "Completed Today", WildNestUI.Amber),
                        (StaffPortalUi.Peso(revenue), "Experience Revenue", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Guide Completion Snapshot",
                        ("Assigned Schedule Records", totalScheduled.ToString()),
                        ("Completed Tours", totalCompleted.ToString()),
                        ("Completion Rate", completionRate.ToString("N1") + "%"),
                        ("Today's Completed Tours", completedToday.ToString())),
                    StaffPortalUi.TrendCard(
                        "Tour Completion Trend",
                        CompletionTrendPoints(completionTrend),
                        "No tour completion trend data available yet."),
                    StaffPortalUi.GridCard("Assigned Schedule Manifest", manifest, "No schedule records are currently assigned to this tour guide.")
                }));
        }

        private static IEnumerable<(string Label, decimal Value, string Display, Color Color)> CompletionTrendPoints(DataTable table)
        {
            return table.Rows
                .Cast<DataRow>()
                .Select(row =>
                {
                    decimal scheduled = row["Scheduled"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Scheduled"]);
                    decimal completed = row["Completed"] == DBNull.Value ? 0m : Convert.ToDecimal(row["Completed"]);
                    decimal rate = scheduled == 0 ? 0m : Math.Round((completed / scheduled) * 100m, 1);
                    return (
                        Convert.ToString(row["Month"]) ?? "N/A",
                        rate,
                        $"{rate:N1}%",
                        WildNestUI.Blue);
                })
                .ToList();
        }
    }
}
