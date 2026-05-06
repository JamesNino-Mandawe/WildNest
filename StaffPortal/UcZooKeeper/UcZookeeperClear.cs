using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperClear : UserControl
    {
        public UcZookeeperClear()
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
                    "Clear Animal",
                    "Prepared control endpoint for future alert-clearance actions.",
                    new List<Control>
                    {
                        StaffPortalUi.MessageCard(
                            "Animal Clearance Needs a Dedicated Status Table",
                            "This page is ready to become the clearance endpoint later, but the current SQL schema does not yet persist animal statuses or health alert clearances.",
                            alert: true)
                    }));
                return;
            }

            StaffPortalDb.EnsureHealthAlertColumns();

            var pending = StaffPortalDb.GetTable(@"
SELECT hr.HealthRecordID AS `Record ID`,
       a.AnimalName AS `Animal`,
       a.Species,
       hr.Status AS `Alert Status`,
       hr.RecordDate AS `Logged`
FROM tbl_healthrecords hr
JOIN tbl_animals a ON a.AnimalID = hr.AnimalID
WHERE hr.IsAlert = 1
  AND hr.IsCleared = 0
ORDER BY hr.RecordDate DESC;");

            pending.Columns["Record ID"].ColumnName = "Health Record ID";
            var displayPending = pending.Copy();
            displayPending.Columns.Add("Queue #", typeof(int)).SetOrdinal(0);
            for (int i = 0; i < displayPending.Rows.Count; i++)
                displayPending.Rows[i]["Queue #"] = i + 1;

            var actionCard = StaffPortalUi.ActionCard("Clear Animal Alert", panel =>
            {
                var info = new Label
                {
                    Text = "Enter an open Health Record ID to clear it and restore encounter eligibility.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var helper = new Label
                {
                    Text = "Use the Queue # only for easier viewing. For clearing, enter the actual Health Record ID shown in the table when the animal has recovered and can safely rejoin encounters.",
                    Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 82)
                };
                var tbId = new TextBox { PlaceholderText = "Health Record ID from open alerts", Font = WildNestUI.FontBody(10f), BorderStyle = BorderStyle.FixedSingle, Location = new Point(18, 108), Width = 220 };
                var btn = WildNestUI.BtnPrimary("Clear Alert", 120, 30);
                btn.Location = new Point(250, 106);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbId.Text.Trim(), out int recordId))
                    {
                        MessageBox.Show("Enter a valid Health Record ID.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? animalIdValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT AnimalID FROM tbl_healthrecords WHERE HealthRecordID=@id AND IsAlert=1 AND IsCleared=0;",
                                new MySqlParameter("@id", recordId));
                            if (animalIdValue == null || animalIdValue == DBNull.Value)
                                throw new InvalidOperationException("No matching open health record was found.");

                            int animalId = Convert.ToInt32(animalIdValue);

                            StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_healthrecords
SET IsCleared = 1,
    ClearedAt = NOW()
WHERE HealthRecordID = @id;",
                                new MySqlParameter("@id", recordId));

                            StaffPortalDb.Execute(conn, tx, @"
UPDATE tbl_animals
SET HealthStatus = 'Healthy',
    IsEncounterEligible = 1
WHERE AnimalID = @animalId;",
                                new MySqlParameter("@animalId", animalId));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to clear alert: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Animal alert cleared and encounter eligibility restored.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(helper);
                panel.Controls.Add(tbId);
                panel.Controls.Add(btn);
            }, 180);

            Controls.Add(StaffPortalUi.BuildPage(
                "Clear Animal",
                "Clear open animal alerts and restore safe encounter access.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "The Queue # is just a visual row number that restarts at 1 whenever the open-alert list refreshes. For actual clearing, use the Health Record ID from the same row, not the Animal ID."),
                    actionCard,
                    StaffPortalUi.GridCard("Open Health Alerts Awaiting Clearance", displayPending, "No open health records to clear right now.")
                }));
        }
    }
}
