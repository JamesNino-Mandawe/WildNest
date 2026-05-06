using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcAdministrator
{
    public partial class UcAdminUsers : UserControl
    {
        public UcAdminUsers()
        {
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMessage);

            int totalUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users;");
            int activeUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 1;");
            int inactiveUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 0;");
            int adminUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Admin%';");
            int receptionUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Reception%';");
            int fieldOpsUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Tour%' OR Role LIKE '%Zoo%';");

            var roleBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Role), ''), 'Unspecified') AS `Role`,
       COUNT(*) AS `Accounts`,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS `Active`,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS `Inactive`
FROM tbl_users
GROUP BY COALESCE(NULLIF(TRIM(Role), ''), 'Unspecified')
ORDER BY `Accounts` DESC;");

            var users = StaffPortalDb.GetTable(@"
SELECT UserID AS `User ID`,
       FullName AS `Full Name`,
       Username,
       Role,
       ContactNo AS `Contact No.`,
       CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS `Status`
FROM tbl_users
ORDER BY FullName;");

            var page = StaffPortalUi.BuildPage(
                "User Management",
                "Staff account directory from tbl_users.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbMessage, !dbHealthy),
                    StaffPortalUi.StatsRow(
                        (totalUsers.ToString(), "Total Accounts", WildNestUI.Blue),
                        (activeUsers.ToString(), "Active Accounts", WildNestUI.Green),
                        (receptionUsers.ToString(), "Reception Staff", WildNestUI.Amber),
                        (fieldOpsUsers.ToString(), "Tour + Zoo Operations", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Role Access Snapshot",
                        ("Administrators", adminUsers.ToString()),
                        ("Inactive Accounts", inactiveUsers.ToString()),
                        ("Field Operations", fieldOpsUsers.ToString()),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Role Breakdown", roleBreakdown, "No role distribution data available yet."),
                    StaffPortalUi.GridCard("User Accounts", users)
                });

            Controls.Add(page);
        }
    }
}
