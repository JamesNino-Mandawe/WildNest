using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcTourGuide
{
    public partial class UcTourGuideComplete : UserControl
    {
        private readonly string _displayName;
        private readonly string _username;

        public UcTourGuideComplete(string displayName = "", string username = "")
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

            if (!hasSchedules || !hasCompletions)
            {
                Controls.Add(StaffPortalUi.BuildPage(
                    "Mark Complete",
                    "Completion workflow requires tour schedules and tour completion tables.",
                    new List<Control>
                    {
                        StaffPortalUi.MessageCard(
                            "Completion State Not Yet Stored in SQL",
                            "This page needs tbl_tourschedules and tbl_tourcompletions to log finished tours properly.",
                            alert: true)
                    }));
                return;
            }

            int dueToday = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourDate <= CURDATE()
  AND ts.Status <> 'Completed'
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam);

            int completedToday = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_tourcompletions tc
JOIN tbl_tourschedules ts ON ts.TourScheduleID = tc.TourScheduleID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE DATE(tc.CompletedAt) = CURDATE()
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);", GuideUserParam);

            var dueForCompletion = StaffPortalDb.GetTable(@"
SELECT ts.TourScheduleID AS `Schedule ID`,
       ts.TourDate AS `Tour Date`,
       TIME_FORMAT(ts.StartTime, '%h:%i %p') AS `Start`,
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
WHERE ts.TourDate <= CURDATE()
  AND ts.Status <> 'Completed'
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername)
ORDER BY ts.TourDate DESC, ts.StartTime;", GuideUserParam);

            var actionCard = StaffPortalUi.ActionCard("Complete Assigned Tour", panel =>
            {
                var info = new Label
                {
                    Text = "Use the Schedule ID from My Schedule or My Tour Groups, then add a short completion note.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var tbId = new TextBox
                {
                    PlaceholderText = "Tour Schedule ID",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 92),
                    Width = 180
                };
                var tbRemarks = new TextBox
                {
                    PlaceholderText = "Completion remarks",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(210, 92),
                    Width = 320
                };
                var btn = WildNestUI.BtnPrimary("Mark Completed", 150, 30);
                btn.Location = new Point(542, 90);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbId.Text.Trim(), out int scheduleId))
                    {
                        MessageBox.Show("Enter a valid Tour Schedule ID.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? statusValue = StaffPortalDb.Scalar(conn, tx, @"
SELECT ts.Status
FROM tbl_tourschedules ts
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.TourScheduleID = @id
  AND LOWER(COALESCE(u.Username, '')) = LOWER(@guideUsername);",
                                new MySqlParameter("@id", scheduleId),
                                new MySqlParameter("@guideUsername", _username));

                            if (statusValue == null || statusValue == DBNull.Value)
                                throw new InvalidOperationException("No matching tour schedule was found for this guide.");

                            string status = Convert.ToString(statusValue) ?? string.Empty;
                            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("This tour is already marked completed.");

                            StaffPortalDb.Execute(conn, tx,
                                "UPDATE tbl_tourschedules SET Status='Completed' WHERE TourScheduleID=@id;",
                                new MySqlParameter("@id", scheduleId));

                            StaffPortalDb.Execute(conn, tx, @"
INSERT INTO tbl_tourcompletions (TourScheduleID, CompletedByUserID, CompletedAt, CompletionStatus, Remarks)
SELECT ts.TourScheduleID, ts.GuideUserID, NOW(), 'Completed', @remarks
FROM tbl_tourschedules ts
WHERE ts.TourScheduleID = @id;",
                                new MySqlParameter("@id", scheduleId),
                                new MySqlParameter("@remarks", string.IsNullOrWhiteSpace(tbRemarks.Text) ? "Completed by tour guide." : tbRemarks.Text.Trim()));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to complete tour: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Tour schedule marked as completed.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(tbId);
                panel.Controls.Add(tbRemarks);
                panel.Controls.Add(btn);
            }, 168);

            Controls.Add(StaffPortalUi.BuildPage(
                "Mark Complete",
                "Only tours assigned to your guide account can be completed here, and each completed tour flows into Tour History.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        dueToday > 0 ? "Completion Entry Ready" : "Nothing To Complete Yet",
                        dueToday > 0
                            ? "Pick the Schedule ID from the table below, then add a short completion remark. Once submitted, the record moves into Tour History."
                            : "This page stays empty until an assigned tour schedule exists for your guide account and its status is not yet Completed.",
                        alert: dueToday == 0),
                    StaffPortalUi.StatsRow(
                        (dueToday.ToString(), "Awaiting Completion", WildNestUI.Amber),
                        (completedToday.ToString(), "Completed Today", WildNestUI.Green)),
                    actionCard,
                    StaffPortalUi.GridCard("Tour Schedules Awaiting Completion", dueForCompletion, "No due schedules are currently waiting for completion.")
                }));
        }
    }
}
