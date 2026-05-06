using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperFlag : UserControl
    {
        public UcZookeeperFlag()
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
                    "Flag Health Alert",
                    "Prepared control endpoint for future health alerts.",
                    new List<Control>
                    {
                        StaffPortalUi.MessageCard(
                            "Health Alert Workflow Needs Animal Tables",
                            "To make this page transactional, the project needs tables for animals, health alerts, and encounter suspension rules. Right now the schema can support staff chat escalation, but not persistent animal alert tracking.",
                            alert: true)
                    }));
                return;
            }

            StaffPortalDb.EnsureHealthAlertColumns();

            var openAlerts = StaffPortalDb.GetTable(@"
SELECT hr.HealthRecordID AS `Record ID`,
       a.AnimalName AS `Animal`,
       a.Species,
       hr.Status,
       hr.RecordDate AS `Logged`,
       CASE WHEN hr.IsCleared = 1 THEN 'Cleared' ELSE 'Open' END AS `Alert State`
FROM tbl_healthrecords hr
JOIN tbl_animals a ON a.AnimalID = hr.AnimalID
WHERE hr.IsAlert = 1
  AND hr.IsCleared = 0
ORDER BY hr.RecordDate DESC;");

            var actionCard = StaffPortalUi.ActionCard("Raise Health Alert", panel =>
            {
                var info = new Label
                {
                    Text = "Flag an animal for restricted encounters and create an open health alert.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var helper = new Label
                {
                    Text = "Use this only for current open issues. Example: Animal ID 3, Alert status Restricted, Reason Limping observed during morning round.",
                    Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 82)
                };
                var tbAnimal = new TextBox { PlaceholderText = "Animal ID (example: 3)", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(18, 108), Width = 150 };
                var tbStatus = new TextBox { PlaceholderText = "Alert status (Restricted / Under Observation)", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(180, 108), Width = 220 };
                var tbNotes = new TextBox { PlaceholderText = "Reason / notes for staff", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(412, 108), Width = 300 };
                var btn = WildNestUI.BtnPrimary("Flag Alert", 120, 30);
                btn.Location = new Point(724, 106);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbAnimal.Text.Trim(), out int animalId))
                    {
                        MessageBox.Show("Enter a valid Animal ID.");
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(tbStatus.Text))
                    {
                        MessageBox.Show("Enter the alert status.");
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
VALUES (@animalId, NULL, NOW(), @status, NULL, NULL, @notes, 0, 1, NULL);",
                                new MySqlParameter("@animalId", animalId),
                                new MySqlParameter("@status", tbStatus.Text.Trim()),
                                new MySqlParameter("@notes", string.IsNullOrWhiteSpace(tbNotes.Text) ? "Encounter temporarily restricted." : tbNotes.Text.Trim()));

                            StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_animals
SET HealthStatus=@status,
    IsEncounterEligible=0
WHERE AnimalID=@animalId;",
                                new MySqlParameter("@status", tbStatus.Text.Trim()),
                                new MySqlParameter("@animalId", animalId));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to flag health alert: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Animal alert flagged and encounters restricted.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(helper);
                panel.Controls.Add(tbAnimal);
                panel.Controls.Add(tbStatus);
                panel.Controls.Add(tbNotes);
                panel.Controls.Add(btn);
            }, 180);

            Controls.Add(StaffPortalUi.BuildPage(
                "Flag Health Alert",
                "Raise and monitor open animal health alerts.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "Flag Health Alert is for active warning states only. Choose an Animal ID from the animal registry, enter the current alert status, and explain why the animal should be restricted from encounters."),
                    actionCard,
                    StaffPortalUi.GridCard("Open Animal Health Alerts", openAlerts, "No open animal alerts right now.")
                }));
        }
    }
}
