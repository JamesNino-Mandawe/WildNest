using System.Collections.Generic;
using System.Windows.Forms;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperLog : UserControl
    {
        public UcZookeeperLog()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool hasHealthRecords = StaffPortalDb.TableExists("tbl_healthrecords");
            bool hasAnimals = StaffPortalDb.TableExists("tbl_animals");

            var rows = hasHealthRecords && hasAnimals
                ? StaffPortalDb.GetTable(@"
SELECT hr.RecordDate AS `Logged`,
       a.AnimalName AS `Animal`,
       hr.Status,
       CASE WHEN hr.IsCleared = 1 THEN 'Cleared' ELSE 'Open' END AS `Alert State`,
       COALESCE(hr.Notes, '-') AS `Notes`
FROM tbl_healthrecords hr
JOIN tbl_animals a ON a.AnimalID = hr.AnimalID
ORDER BY hr.RecordDate DESC;")
                : StaffPortalDb.GetTable(@"
SELECT FullName AS `Staff`,
       Role,
       ContactNo AS `Contact`,
       CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS `Status`
FROM tbl_users
WHERE Role LIKE '%Zoo%' OR Role LIKE '%Administrator%'
ORDER BY Role, FullName;");

            Controls.Add(StaffPortalUi.BuildPage(
                "Daily Log",
                hasHealthRecords && hasAnimals
                    ? "Operational animal health log from the live wildlife registry."
                    : "Current schema has no zookeeper log table yet, so this page tracks the relevant staff endpoints currently available.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        hasHealthRecords && hasAnimals ? "Live Wildlife Log" : "Daily Log Table Not Yet Present",
                        hasHealthRecords && hasAnimals
                            ? "Health records are now being used as the operational wildlife activity log until a fuller keeper observation table is added."
                            : "Add a daily wildlife log table later if you want keepers to persist notes, observations, and incident records. Until then, this page documents the currently active zoo/admin staff endpoints.",
                        alert: !(hasHealthRecords && hasAnimals)),
                    StaffPortalUi.GridCard(hasHealthRecords && hasAnimals ? "Recent Wildlife Health Activity" : "Available Wildlife Operations Staff", rows)
                }));
        }
    }
}
