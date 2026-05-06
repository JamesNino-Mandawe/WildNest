using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperFeeding : UserControl
    {
        public UcZookeeperFeeding()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            if (!StaffPortalDb.TableExists("tbl_feedings") || !StaffPortalDb.TableExists("tbl_animals"))
            {
                Controls.Add(StaffPortalUi.BuildPage(
                    "Feeding Schedule",
                    "Current schema does not store animal feeding tables yet, so this view shows guest demand timing by wildlife experience.",
                    new List<Control>
                    {
                        StaffPortalUi.MessageCard(
                            "Feeding Table Not Yet Present",
                            "Add a dedicated feeding schedule table later if you want true animal meal planning. For now, this page helps estimate operational timing pressure from experience demand.",
                            alert: true)
                    }));
                return;
            }

            int dueToday = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_feedings WHERE FeedingDate = CURDATE();");
            int completedToday = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_feedings WHERE FeedingDate = CURDATE() AND Status = 'Completed';");
            var animalReference = StaffPortalDb.GetTable(@"
SELECT a.AnimalID AS `Animal ID`,
       a.AnimalName AS `Animal`,
       a.Species,
       a.ZoneName AS `Zone`,
       a.HealthStatus AS `Health`
FROM tbl_animals a
ORDER BY a.AnimalID;");

            var feedings = StaffPortalDb.GetTable(@"
SELECT f.FeedingID AS `Feeding ID`,
       f.FeedingDate AS `Date`,
       TIME_FORMAT(f.FeedingTime, '%h:%i %p') AS `Time`,
       a.AnimalName AS `Animal`,
       a.Species,
       f.FoodItem AS `Food`,
       COALESCE(f.Quantity, '-') AS `Quantity`,
       f.Status
FROM tbl_feedings f
JOIN tbl_animals a ON a.AnimalID = f.AnimalID
ORDER BY f.FeedingDate DESC, f.FeedingTime;");

            var createCard = StaffPortalUi.ActionCard("Create Feeding Schedule", panel =>
            {
                var info = new Label
                {
                    Text = "Use the Animal ID from the reference board below, then create a schedule with status Scheduled or Delayed. After the feeding happens, mark it Completed.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    Location = new Point(18, 62),
                    Size = new Size(930, 34)
                };

                var tbAnimalId = new TextBox
                {
                    PlaceholderText = "Animal ID",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 104),
                    Width = 110
                };

                var dtDate = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Location = new Point(140, 104),
                    Width = 120
                };

                var dtTime = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Time,
                    ShowUpDown = true,
                    Location = new Point(272, 104),
                    Width = 110
                };

                var tbFood = new TextBox
                {
                    PlaceholderText = "Food item",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(394, 104),
                    Width = 160
                };

                var tbQuantity = new TextBox
                {
                    PlaceholderText = "Quantity",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(566, 104),
                    Width = 110
                };

                var cbStatus = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(688, 104),
                    Width = 120
                };
                cbStatus.Items.AddRange(new object[] { "Scheduled", "Delayed", "Completed", "Missed" });
                cbStatus.SelectedIndex = 0;

                var tbNotes = new TextBox
                {
                    PlaceholderText = "Notes",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 142),
                    Width = 790
                };

                var btn = WildNestUI.BtnPrimary("Create Feeding", 150, 30);
                btn.Location = new Point(820, 140);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbAnimalId.Text.Trim(), out int animalId))
                    {
                        MessageBox.Show("Enter a valid Animal ID from the animal reference board.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbFood.Text))
                    {
                        MessageBox.Show("Enter the food item.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(tbQuantity.Text))
                    {
                        MessageBox.Show("Enter the feeding quantity.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? animalExists = StaffPortalDb.Scalar(conn, tx,
                                "SELECT AnimalName FROM tbl_animals WHERE AnimalID=@id;",
                                new MySqlParameter("@id", animalId));
                            if (animalExists == null || animalExists == DBNull.Value)
                                throw new InvalidOperationException("No animal exists for that Animal ID.");

                            object? assignedUser = StaffPortalDb.Scalar(conn, tx,
                                "SELECT MIN(UserID) FROM tbl_users WHERE Role LIKE '%Zoo%' AND COALESCE(IsActive, 1) = 1;");

                            string insertSql = assignedUser == null || assignedUser == DBNull.Value
                                ? @"
INSERT INTO tbl_feedings
    (AnimalID, FeedingDate, FeedingTime, FoodItem, Quantity, Status, Notes, CreatedAt)
VALUES
    (@animalId, @feedingDate, @feedingTime, @foodItem, @quantity, @status, @notes, NOW());"
                                : @"
INSERT INTO tbl_feedings
    (AnimalID, AssignedUserID, FeedingDate, FeedingTime, FoodItem, Quantity, Status, Notes, CreatedAt)
VALUES
    (@animalId, @assignedUserId, @feedingDate, @feedingTime, @foodItem, @quantity, @status, @notes, NOW());";

                            var parameters = new List<MySqlParameter>
                            {
                                new("@animalId", animalId),
                                new("@feedingDate", dtDate.Value.Date),
                                new("@feedingTime", dtTime.Value.TimeOfDay),
                                new("@foodItem", tbFood.Text.Trim()),
                                new("@quantity", tbQuantity.Text.Trim()),
                                new("@status", Convert.ToString(cbStatus.SelectedItem) ?? "Scheduled"),
                                new("@notes", string.IsNullOrWhiteSpace(tbNotes.Text) ? "Created from ZooKeeper feeding schedule." : tbNotes.Text.Trim())
                            };

                            if (assignedUser != null && assignedUser != DBNull.Value)
                                parameters.Insert(1, new MySqlParameter("@assignedUserId", Convert.ToInt32(assignedUser)));

                            StaffPortalDb.Execute(conn, tx, insertSql, parameters.ToArray());
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to create feeding: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Feeding schedule created.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(tbAnimalId);
                panel.Controls.Add(dtDate);
                panel.Controls.Add(dtTime);
                panel.Controls.Add(tbFood);
                panel.Controls.Add(tbQuantity);
                panel.Controls.Add(cbStatus);
                panel.Controls.Add(tbNotes);
                panel.Controls.Add(btn);
            }, 210);

            var actionCard = StaffPortalUi.ActionCard("Update Feeding Status", panel =>
            {
                var info = new Label
                {
                    Text = "Enter a Feeding ID to mark it completed after the feeding is done.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(18, 62)
                };
                var tbId = new TextBox
                {
                    PlaceholderText = "Feeding ID",
                    Font = WildNestUI.FontBody(10f),
                    BorderStyle = BorderStyle.FixedSingle,
                    Location = new Point(18, 92),
                    Width = 160
                };
                var btn = WildNestUI.BtnPrimary("Mark Completed", 150, 30);
                btn.Location = new Point(190, 90);
                btn.Click += (s, e) =>
                {
                    if (!int.TryParse(tbId.Text.Trim(), out int feedingId))
                    {
                        MessageBox.Show("Enter a valid Feeding ID.");
                        return;
                    }

                    try
                    {
                        StaffPortalDb.ExecuteTransaction((conn, tx) =>
                        {
                            object? statusValue = StaffPortalDb.Scalar(conn, tx,
                                "SELECT Status FROM tbl_feedings WHERE FeedingID=@id;",
                                new MySqlParameter("@id", feedingId));
                            if (statusValue == null || statusValue == DBNull.Value)
                                throw new InvalidOperationException("No matching feeding record was found.");

                            string status = Convert.ToString(statusValue) ?? string.Empty;
                            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("That feeding is already marked completed.");

                            StaffPortalDb.Execute(conn, tx,
                                "UPDATE tbl_feedings SET Status='Completed' WHERE FeedingID=@id;",
                                new MySqlParameter("@id", feedingId));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to update feeding: " + ex.Message);
                        return;
                    }

                    MessageBox.Show("Feeding marked as completed.");
                    Render();
                };

                panel.Controls.Add(info);
                panel.Controls.Add(tbId);
                panel.Controls.Add(btn);
            }, 160);

            Controls.Add(StaffPortalUi.BuildPage(
                "Feeding Schedule",
                "Live feeding board powered by tbl_feedings and tbl_animals.",
                new List<Control>
                {
                    StaffPortalUi.StatsRow(
                        (dueToday.ToString(), "Feedings Scheduled Today", WildNestUI.Green),
                        (completedToday.ToString(), "Feedings Completed Today", WildNestUI.Blue)),
                    createCard,
                    actionCard,
                    StaffPortalUi.GridCard("Animal ID Reference", animalReference, "No animals found. Add or seed animals first."),
                    StaffPortalUi.GridCard("Animal Feeding Schedule", feedings)
                }));
        }
    }
}
