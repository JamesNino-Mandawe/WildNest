using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperHealth : UserControl
    {
        public UcZookeeperHealth()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            if (!StaffPortalDb.TableExists("tbl_healthrecords") || !StaffPortalDb.TableExists("tbl_animals"))
            {
                Controls.Add(StaffPortalUi.BuildPage(
                    "Health Records",
                    "Health records depend on a dedicated animal/medical schema that is not in the current database yet.",
                    new List<Control>
                    {
                        StaffPortalUi.MessageCard(
                            "Health Records Table Required",
                            "Your current SQL dump does not include tables for animals, diagnoses, treatments, or health alerts. This user control is ready as the role endpoint, but it needs those tables before true medical record workflows can be wired.",
                            alert: true)
                    }));
                return;
            }

            StaffPortalDb.EnsureHealthAlertColumns();

            int totalRecords = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_healthrecords;");
            int openAlerts = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_healthrecords WHERE IsAlert = 1 AND IsCleared = 0;");

            var records = StaffPortalDb.GetTable(@"
SELECT hr.HealthRecordID AS `Record ID`,
       a.AnimalName AS `Animal`,
       a.Species,
       hr.RecordDate AS `Recorded`,
       hr.Status,
       COALESCE(hr.Diagnosis, '-') AS `Diagnosis`,
       COALESCE(hr.Treatment, '-') AS `Treatment`,
       CASE WHEN hr.IsAlert = 1 THEN 'Active Alert' ELSE 'Medical Record' END AS `Record Type`,
       CASE
           WHEN hr.IsAlert = 1 AND hr.IsCleared = 0 THEN 'Open'
           WHEN hr.IsAlert = 1 AND hr.IsCleared = 1 THEN 'Cleared'
           ELSE '-'
       END AS `Alert State`
FROM tbl_healthrecords hr
JOIN tbl_animals a ON a.AnimalID = hr.AnimalID
ORDER BY hr.RecordDate DESC;");

            var actionCard = StaffPortalUi.ActionCard("Log Health Record", panel =>
            {
                var info = new Label
                {
                    Text = "Create a new animal health record for monitoring or treatment follow-up. Use the Animal ID from the live animal registry snapshot.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var helper = new Label
                {
                    Text = "Example: Animal ID 3, Status Under Observation, Diagnosis Reduced appetite, Treatment Hydration monitoring.",
                    Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 82)
                };
                var tbAnimal = new TextBox { PlaceholderText = "Animal ID (example: 3)", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(18, 108), Width = 150 };
                var tbStatus = new TextBox { PlaceholderText = "Status (Healthy / Under Observation)", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(180, 108), Width = 190 };
                var tbDiagnosis = new TextBox { PlaceholderText = "Diagnosis / observed issue", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(382, 108), Width = 180 };
                var tbTreatment = new TextBox { PlaceholderText = "Treatment or action given", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(574, 108), Width = 180 };
                var btn = WildNestUI.BtnPrimary("Save Record", 120, 30);
                btn.Location = new Point(766, 106);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbAnimal.Text.Trim(), out int animalId))
                    {
                        MessageBox.Show("Enter a valid Animal ID.");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(tbStatus.Text))
                    {
                        MessageBox.Show("Enter the current health status.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? existsValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT COUNT(*) FROM tbl_animals WHERE AnimalID=@id;",
                                new MySqlParameter("@id", animalId));
                            if (existsValue == null || existsValue == DBNull.Value || Convert.ToInt32(existsValue) == 0)
                                throw new InvalidOperationException("Animal ID was not found.");

                            StaffPortalDb.Execute(conn, tx, @"
INSERT INTO tbl_healthrecords (AnimalID, RecordedByUserID, RecordDate, Status, Diagnosis, Treatment, Notes, IsCleared, IsAlert, ClearedAt)
VALUES (@animalId, NULL, NOW(), @status, @diagnosis, @treatment, @notes, 1, 0, NULL);",
                                new MySqlParameter("@animalId", animalId),
                                new MySqlParameter("@status", tbStatus.Text.Trim()),
                                new MySqlParameter("@diagnosis", string.IsNullOrWhiteSpace(tbDiagnosis.Text) ? DBNull.Value : tbDiagnosis.Text.Trim()),
                                new MySqlParameter("@treatment", string.IsNullOrWhiteSpace(tbTreatment.Text) ? DBNull.Value : tbTreatment.Text.Trim()),
                                new MySqlParameter("@notes", DBNull.Value));

                            StaffPortalDb.Execute(conn, tx,
                                "UPDATE tbl_animals SET HealthStatus=@status WHERE AnimalID=@animalId;",
                                new MySqlParameter("@status", tbStatus.Text.Trim()),
                                new MySqlParameter("@animalId", animalId));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to save health record: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Health record saved.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(helper);
                panel.Controls.Add(tbAnimal);
                panel.Controls.Add(tbStatus);
                panel.Controls.Add(tbDiagnosis);
                panel.Controls.Add(tbTreatment);
                panel.Controls.Add(btn);
            }, 180);

            Controls.Add(StaffPortalUi.BuildPage(
                "Health Records",
                "Health records are now backed by tbl_healthrecords and tbl_animals.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "Animal ID comes from the ZooKeeper animal registry. Use this page for detailed medical logging such as diagnosis, treatment, and follow-up observations. These entries are saved as medical history only. Flag Health Alert is the page for active restrictions and encounter blocking."),
                    StaffPortalUi.StatsRow(
                        (totalRecords.ToString(), "Health Records Logged", WildNestUI.Blue),
                        (openAlerts.ToString(), "Active Alerts", WildNestUI.Red)),
                    actionCard,
                    StaffPortalUi.GridCard("Animal Health Records", records)
                }));
        }
    }
}
