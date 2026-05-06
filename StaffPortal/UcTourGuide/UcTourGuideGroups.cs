using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcTourGuide
{
    public partial class UcTourGuideGroups : UserControl
    {
        private readonly string _displayName;
        private readonly string _username;

        public UcTourGuideGroups(string displayName = "", string username = "")
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
            int openQueue = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules
WHERE GuideUserID IS NULL
  AND Status = 'Open for Claim';")
                : 0;

            int upcomingGroups = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate >= CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int guestsUpcoming = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COALESCE(SUM(be.Quantity),0)
FROM tbl_tourschedules ts
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate >= CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            int packages = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");
            int confirmed = hasSchedules
                ? StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.Status IN ('Scheduled','Assigned','In Progress','Pending Completion')
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam)
                : 0;

            var unclaimed = hasSchedules
                ? StaffPortalDb.GetTable(@"
SELECT ts.TourScheduleID AS `Schedule ID`,
       ts.TourDate AS `Scheduled Date`,
       TIME_FORMAT(ts.StartTime, '%h:%i %p') AS `Start`,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Walk-In Guest') AS `Lead Guest`,
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
WHERE ts.GuideUserID IS NULL
  AND ts.Status = 'Open for Claim'
ORDER BY ts.TourDate, ts.StartTime, `Lead Guest`;")
                : new System.Data.DataTable();

            var groups = hasSchedules
                ? StaffPortalDb.GetTable(@"
SELECT ts.TourScheduleID AS `Schedule ID`,
       ts.TourDate AS `Scheduled Date`,
       TIME_FORMAT(ts.StartTime, '%h:%i %p') AS `Start`,
       COALESCE(CONCAT(g.FirstName, ' ', g.LastName), 'Walk-In Guest') AS `Lead Guest`,
       e.ExperienceName AS `Experience`,
       COALESCE(be.Quantity, 0) AS `Attendees`,
       ts.Status,
       COALESCE(ts.Notes, '') AS `Guide Notes`
FROM tbl_tourschedules ts
LEFT JOIN tbl_reservations r ON r.ReservationID = ts.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_experiences e ON e.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_bookingexperiences be
  ON be.ReservationID = ts.ReservationID
 AND be.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate >= CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername)
ORDER BY ts.TourDate, ts.StartTime, `Lead Guest`;", GuideUserParam)
                : new System.Data.DataTable();

            var claimCard = StaffPortalUi.ActionCard("Claim Open Event", panel =>
            {
                var info = new Label
                {
                    Text = "Use the Schedule ID from the shared open-events queue. Once claimed, the event moves into your personal schedule.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var tbId = new TextBox
                {
                    PlaceholderText = "Open Schedule ID",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 94),
                    Width = 190
                };
                var btn = WildNestUI.BtnPrimary("Claim Event", 130, 30);
                btn.Location = new Point(220, 92);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbId.Text.Trim(), out int scheduleId))
                    {
                        MessageBox.Show("Enter a valid Schedule ID.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? guideIdValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT UserID FROM tbl_users WHERE LOWER(Username)=LOWER(@username) AND Role LIKE '%Tour%' LIMIT 1;",
                                new MySqlParameter("@username", _username));
                            if (guideIdValue == null || guideIdValue == DBNull.Value)
                                throw new InvalidOperationException("The current guide account could not be matched to an active tour guide user.");

                            int guideUserId = Convert.ToInt32(guideIdValue);

                            object? tourDateValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT TourDate FROM tbl_tourschedules WHERE TourScheduleID=@id AND GuideUserID IS NULL AND Status='Open for Claim';",
                                new MySqlParameter("@id", scheduleId));
                            if (tourDateValue == null || tourDateValue == DBNull.Value)
                                throw new InvalidOperationException("That event is no longer available in the shared queue.");

                            DateTime tourDate = Convert.ToDateTime(tourDateValue).Date;
                            string status = tourDate < DateTime.Today ? "Pending Completion" : "Scheduled";

                            int updated = StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_tourschedules
SET GuideUserID=@guideUserId,
    Status=@status,
    Notes=CONCAT(COALESCE(Notes,''), ' Claimed by guide ', @username, '.')
WHERE TourScheduleID=@id
  AND GuideUserID IS NULL
  AND Status='Open for Claim';",
                                new MySqlParameter("@guideUserId", guideUserId),
                                new MySqlParameter("@status", status),
                                new MySqlParameter("@username", _username),
                                new MySqlParameter("@id", scheduleId));

                            if (updated == 0)
                                throw new InvalidOperationException("That event was already claimed by another guide.");
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to claim event: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Event claimed successfully. It is now part of your guide schedule.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(tbId);
                panel.Controls.Add(btn);
            }, 160);

            Controls.Add(StaffPortalUi.BuildPage(
                "My Tour Groups",
                hasSchedules
                    ? "Experience bookings first appear in the shared event queue. Once claimed, they become part of your assigned guide groups. The Schedule ID is your official guide reference."
                    : "No tour schedule table exists yet, so no assigned guest groups can be shown.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        hasSchedules ? "Shared Queue + Assigned Groups Ready" : "Guide Grouping Not Available",
                        hasSchedules
                            ? "When a guest books an experience, the system creates an open event first. Any tour guide can claim it, and only then does it appear under that guide's assigned groups."
                            : "The system cannot build guide group manifests until tbl_tourschedules is available.",
                        alert: !hasSchedules),
                    StaffPortalUi.MessageCard(
                        openQueue > 0 ? "Open Events Waiting For Claim" : "No Open Events In Queue",
                        openQueue > 0
                            ? "The table below shows experience events visible to all tour guides. The first guide to claim a Schedule ID becomes the assigned guide for that event."
                            : "The shared queue is currently empty. Only bookings that include an actual experience will appear here.",
                        alert: openQueue == 0 && upcomingGroups == 0),
                    StaffPortalUi.StatsRow(
                        (openQueue.ToString(), "Open Event Queue", WildNestUI.Amber),
                        (upcomingGroups.ToString(), "Upcoming Groups", WildNestUI.Green),
                        (guestsUpcoming.ToString(), "Guests Upcoming", WildNestUI.Blue),
                        (confirmed.ToString(), "Active Claimed Schedules", WildNestUI.Green)),
                    claimCard,
                    StaffPortalUi.GridCard("Shared Open Event Queue", unclaimed, "No open experience events are currently waiting for guide claim."),
                    StaffPortalUi.GridCard("My Claimed Experience Groups", groups, "No future groups are currently assigned to this guide.")
                }));
        }
    }
}
