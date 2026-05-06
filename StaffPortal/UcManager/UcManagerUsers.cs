using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcManager
{
    public class UcManagerUsers : UserControl
    {
        public UcManagerUsers()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMsg);

            int totalUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users;");
            int activeUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 1;");
            int inactiveUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 0;");
            int managerUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role IN ('Manager','Administrator');");
            int receptionUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Reception%';");
            int zooKeeperUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Zoo%';");
            int tourGuideUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Tour%';");
            int fieldOpsUsers = zooKeeperUsers + tourGuideUsers;
            int legacyAdminUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role = 'Administrator';");
            int missingContacts = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE COALESCE(NULLIF(TRIM(ContactNo),''),'') = '';");

            var roleBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Role),''),'Unspecified') AS `Role`,
       COUNT(*) AS `Accounts`,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS `Active`,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS `Inactive`
FROM tbl_users
GROUP BY COALESCE(NULLIF(TRIM(Role),''),'Unspecified')
ORDER BY `Accounts` DESC;");

            var users = StaffPortalDb.GetTable(@"
SELECT UserID AS `User ID`,
       FullName AS `Full Name`,
       Username,
       Role,
       ContactNo AS `Contact No.`,
       CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS `Status`,
       CASE
           WHEN Role IN ('Manager','Administrator') THEN 'Executive Control'
           WHEN Role LIKE '%Reception%' THEN 'Reception Portal'
           WHEN Role LIKE '%Zoo%' THEN 'ZooKeeper Portal'
           WHEN Role LIKE '%Tour%' THEN 'TourGuide Portal'
           ELSE 'Unmapped Role'
       END AS `Portal Route`
FROM tbl_users
ORDER BY FullName;");

            var inactiveQueue = StaffPortalDb.GetTable(@"
SELECT FullName AS `Staff`,
       Username,
       Role,
       COALESCE(NULLIF(TRIM(ContactNo),''),'No contact line') AS `Contact`,
       'Inactive' AS `Current Status`
FROM tbl_users
WHERE IsActive = 0
ORDER BY Role, FullName;");

            var roleCoverage = StaffPortalDb.GetTable(@"
SELECT 'Manager' AS `Role`,
       COUNT(*) AS `Accounts`,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS `Active`,
       'Executive oversight, analytics, staff provisioning' AS `Operational Scope`
FROM tbl_users
WHERE Role IN ('Manager','Administrator')
UNION ALL
SELECT 'Reception',
       COUNT(*),
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END),
       'Guest arrival, verification, communication, front desk actions'
FROM tbl_users
WHERE Role LIKE '%Reception%'
UNION ALL
SELECT 'ZooKeeper',
       COUNT(*),
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END),
       'Animal registry, feeding schedules, health records, alerts'
FROM tbl_users
WHERE Role LIKE '%Zoo%'
UNION ALL
SELECT 'TourGuide',
       COUNT(*),
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END),
       'Tour schedules, group handling, completion tracking'
FROM tbl_users
WHERE Role LIKE '%Tour%';");

            var actionRow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            var btnCreate = ActionButton("Create Staff Account", WildNestUI.Green);
            var btnToggle = ActionButton("Toggle Active / Inactive", WildNestUI.Amber);
            var btnReset = ActionButton("Reset Password", WildNestUI.Blue);
            var btnDelete = ActionButton("Delete / Archive Staff", Color.FromArgb(172, 46, 34));
            btnCreate.Click += (_, _) => ShowCreateDialog();
            btnToggle.Click += (_, _) => ShowToggleDialog();
            btnReset.Click += (_, _) => ShowResetDialog();
            btnDelete.Click += (_, _) => ShowDeleteDialog();
            actionRow.Controls.AddRange(new Control[] { btnCreate, btnToggle, btnReset, btnDelete });

            var page = StaffPortalUi.BuildPage(
                "Staff Command Center",
                "Premium manager account provisioning for Reception, ZooKeeper, and TourGuide operations.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(
                        dbHealthy
                            ? "Manager account controls are connected. New staff credentials, status changes, and password resets are ready for operational use."
                            : dbMsg,
                        !dbHealthy),
                    StaffPortalUi.MessageCard(
                        "Manager Governance Layer",
                        legacyAdminUsers > 0
                            ? $"Legacy Administrator records are still supported for safe compatibility, but they are governed as Manager-level accounts in the live workflow. Current legacy records detected: {legacyAdminUsers}."
                            : "The live staff structure is already centered on the Manager role. Reception, ZooKeeper, and TourGuide accounts are supervised from one premium governance console.",
                        alert: false),
                    actionRow,
                    StaffPortalUi.StatsRow(
                        (totalUsers.ToString(), "Total Accounts", WildNestUI.Blue),
                        (activeUsers.ToString(), "Active Accounts", WildNestUI.Green),
                        (managerUsers.ToString(), "Protected Manager Accounts", WildNestUI.Amber),
                        (fieldOpsUsers.ToString(), "Field Operations Staff", WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Account Oversight",
                        ("Manager Accounts", managerUsers.ToString()),
                        ("Reception Staff", receptionUsers.ToString()),
                        ("ZooKeeper Staff", zooKeeperUsers.ToString()),
                        ("TourGuide Staff", tourGuideUsers.ToString()),
                        ("Inactive Accounts", inactiveUsers.ToString()),
                        ("Missing Contact Lines", missingContacts.ToString()),
                        ("Legacy Admin Mapping", legacyAdminUsers.ToString()),
                        ("DB Health", dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Operational Role Coverage", roleCoverage, "No role coverage data available."),
                    StaffPortalUi.GridCard("Role Breakdown", roleBreakdown, "No role data available."),
                    StaffPortalUi.GridCard("Inactive Follow-up Queue", inactiveQueue, "No inactive staff accounts right now."),
                    StaffPortalUi.GridCard("Live Staff Directory", users, "No staff accounts found.")
                });

            Controls.Add(page);
        }

        private void ShowCreateDialog()
        {
            using var dlg = BuildManagerModal(
                "Create Staff Account",
                "Provision a polished operational login for reception, zoo keeping, or tour guiding.",
                new Size(1000, 1020),
                out Panel body);

            Panel content = CreateManagerDialogSurface(body, out Panel footer);
            content.AutoScroll = false;

            const int width = 700;
            int y = 0;
            var formPanel = CreateCenteredDialogPanel(content, width, 662);

            var txtFullName = CreateManagerTextField(formPanel, ref y, width, "Full Name", "Official staff display name visible across dashboards and chat.", false);
            var txtUsername = CreateManagerTextField(formPanel, ref y, width, "Username", "Unique login name. Use a concise account handle without spaces.", false);
            var txtPass = CreateManagerTextField(formPanel, ref y, width, "Password", "Temporary sign-in credential. Staff can change this after first access.", true);
            var txtContact = CreateManagerTextField(formPanel, ref y, width, "Contact No.", "Mobile number or internal contact line used for coordination.", false);

            var cmbRole = BuildManagerComboBox(492);
            cmbRole.Items.AddRange(new object[] { "Reception", "ZooKeeper", "TourGuide" });
            cmbRole.SelectedIndex = 0;
            var roleBlock = BuildManagerField(width, "Portal Role", "Choose which staff workspace should open after successful login.", cmbRole);
            roleBlock.Location = new Point(0, y);
            formPanel.Controls.Add(roleBlock);
            y += roleBlock.Height + 10;

            var note = BuildManagerNote(width, "Accounts are created active by default. Use the status control below to pause staff access when needed.");
            note.Location = new Point(4, y);
            formPanel.Controls.Add(note);
            y += note.Height;
            formPanel.Height = y;

            var btnSave = BuildManagerActionButton("Create Account", WildNestUI.Green);
            LayoutDialogFooter(footer, btnSave);
            CenterDialogPanel(content, formPanel, 18, 34);

            btnSave.Click += (_, _) =>
            {
                string fullName = txtFullName.Text.Trim();
                string username = txtUsername.Text.Trim();
                string password = txtPass.Text;
                string contact = txtContact.Text.Trim();
                string role = cmbRole.Text;

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    Warn(this, "Full name, username, and password are required.");
                    return;
                }

                int exists = StaffPortalDb.Count(
                    "SELECT COUNT(*) FROM tbl_users WHERE LOWER(Username) = LOWER(@u);",
                    new MySqlParameter("@u", username));

                if (exists > 0)
                {
                    Warn(this, "That username is already taken.");
                    return;
                }

                try
                {
                    StaffPortalDb.Execute(
                        @"INSERT INTO tbl_users (FullName, Username, PasswordHash, Role, ContactNo, IsActive)
                          VALUES (@fn, @un, @pw, @role, @cn, 1);",
                        new MySqlParameter("@fn", fullName),
                        new MySqlParameter("@un", username),
                        new MySqlParameter("@pw", HashPassword(password)),
                        new MySqlParameter("@role", role),
                        new MySqlParameter("@cn", contact));

                    Info(this, $"Account created for {fullName} ({role}).");
                    dlg.Close();
                    Render();
                }
                catch (Exception ex)
                {
                    Warn(this, "Failed to create account:\n" + ex.Message);
                }
            };

            txtFullName.Focus();
            dlg.ShowDialog(this);
        }

        private void ShowToggleDialog()
        {
            using var dlg = BuildManagerModal(
                "Toggle Account Status",
                "Temporarily allow or suspend staff access without deleting operational history.",
                new Size(920, 680),
                out Panel body);

            Panel content = CreateManagerDialogSurface(body, out Panel footer);
            content.AutoScroll = false;

            const int width = 640;
            int y = 0;
            var formPanel = CreateCenteredDialogPanel(content, width, 276);

            var txtUser = CreateManagerTextField(formPanel, ref y, width, "Username", "Enter the exact username of the Reception, ZooKeeper, or TourGuide account.", false);

            var impact = BuildManagerNote(width, "Impact summary: staff login access changes immediately, but operational history and linked records remain intact.");
            impact.Location = new Point(4, y);
            formPanel.Controls.Add(impact);
            y += impact.Height + 4;

            var protect = BuildManagerNote(width, "Manager accounts are protected and cannot be toggled from this control.");
            protect.Location = new Point(4, y);
            formPanel.Controls.Add(protect);
            y += protect.Height;
            formPanel.Height = y;

            var btnGo = BuildManagerActionButton("Toggle Status", WildNestUI.Amber);
            LayoutDialogFooter(footer, btnGo);
            CenterDialogPanel(content, formPanel, 28, 52);

            btnGo.Click += (_, _) =>
            {
                string username = txtUser.Text.Trim();
                if (string.IsNullOrWhiteSpace(username))
                {
                    Warn(this, "Enter a username first.");
                    return;
                }

                if (IsManagerAccount(username))
                {
                    Warn(this, "Manager accounts cannot be modified here.");
                    return;
                }

                try
                {
                    StaffPortalDb.Execute(
                        "UPDATE tbl_users SET IsActive = IF(IsActive = 1, 0, 1) WHERE LOWER(Username) = LOWER(@u);",
                        new MySqlParameter("@u", username));

                    int newStatus = StaffPortalDb.Count(
                        "SELECT COUNT(*) FROM tbl_users WHERE LOWER(Username) = LOWER(@u) AND IsActive = 1;",
                        new MySqlParameter("@u", username));

                    Info(this, $"Account '{username}' is now {(newStatus > 0 ? "Active" : "Inactive")}.");
                    dlg.Close();
                    Render();
                }
                catch (Exception ex)
                {
                    Warn(this, "Error: " + ex.Message);
                }
            };

            txtUser.Focus();
            dlg.ShowDialog(this);
        }

        private void ShowResetDialog()
        {
            using var dlg = BuildManagerModal(
                "Reset Staff Password",
                "Replace an existing staff password with a secure new credential from the manager console.",
                new Size(940, 740),
                out Panel body);

            Panel content = CreateManagerDialogSurface(body, out Panel footer);
            content.AutoScroll = false;

            const int width = 660;
            int y = 0;
            var formPanel = CreateCenteredDialogPanel(content, width, 398);

            var txtUser = CreateManagerTextField(formPanel, ref y, width, "Username", "Enter the exact staff username that needs a credential update.", false);
            var txtNewPw = CreateManagerTextField(formPanel, ref y, width, "New Password", "Choose a strong replacement password before giving access back to the staff member.", true);

            var security = BuildManagerNote(width, "Security note: choose a temporary but strong password, then instruct the staff member to replace it after regaining access.");
            security.Location = new Point(4, y);
            formPanel.Controls.Add(security);
            y += security.Height + 4;

            var protect = BuildManagerNote(width, "Manager passwords are protected and cannot be reset from this action box.");
            protect.Location = new Point(4, y);
            formPanel.Controls.Add(protect);
            y += protect.Height;
            formPanel.Height = y;

            var btnGo = BuildManagerActionButton("Reset Password", WildNestUI.Blue);
            LayoutDialogFooter(footer, btnGo);
            CenterDialogPanel(content, formPanel, 26, 50);

            btnGo.Click += (_, _) =>
            {
                string username = txtUser.Text.Trim();
                string password = txtNewPw.Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    Warn(this, "Both fields are required.");
                    return;
                }

                if (IsManagerAccount(username))
                {
                    Warn(this, "Manager passwords cannot be reset here.");
                    return;
                }

                try
                {
                    StaffPortalDb.Execute(
                        "UPDATE tbl_users SET PasswordHash = @pw WHERE LOWER(Username) = LOWER(@u);",
                        new MySqlParameter("@pw", HashPassword(password)),
                        new MySqlParameter("@u", username));

                    Info(this, $"Password reset for '{username}'.");
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    Warn(this, "Error: " + ex.Message);
                }
            };

            txtUser.Focus();
            dlg.ShowDialog(this);
        }

        private void ShowDeleteDialog()
        {
            using var dlg = BuildManagerModal(
                "Delete or Archive Staff",
                "Remove an unused staff account, or archive it safely if it already owns operational history.",
                new Size(940, 720),
                out Panel body);

            Panel content = CreateManagerDialogSurface(body, out Panel footer);
            content.AutoScroll = false;

            const int width = 660;
            int y = 0;
            var formPanel = CreateCenteredDialogPanel(content, width, 344);

            var txtUser = CreateManagerTextField(formPanel, ref y, width, "Username", "Enter the exact staff username to remove. Manager accounts remain protected.", false);

            var note = BuildManagerNote(width, "If the account has feeding, health, tour, or completion history, WildNest will keep the history safe and archive the account by setting it inactive instead of deleting it.");
            note.Location = new Point(4, y);
            formPanel.Controls.Add(note);
            y += note.Height + 4;

            var safeguard = BuildManagerNote(width, "This action does not touch manager accounts, guest reservations, or other portals.");
            safeguard.Location = new Point(4, y);
            formPanel.Controls.Add(safeguard);
            y += safeguard.Height;
            formPanel.Height = y;

            var btnGo = BuildManagerActionButton("Delete or Archive", Color.FromArgb(172, 46, 34));
            LayoutDialogFooter(footer, btnGo);
            CenterDialogPanel(content, formPanel, 26, 48);

            btnGo.Click += (_, _) =>
            {
                string username = txtUser.Text.Trim();
                if (string.IsNullOrWhiteSpace(username))
                {
                    Warn(this, "Enter a username first.");
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Proceed with delete/archive for '{username}'?\n\nIf the account has linked operational history, WildNest will archive it instead of deleting it.",
                    "WildNest Manager",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                    return;

                try
                {
                    if (!StaffPortalDb.TryDeleteOrArchiveStaff(username, out string message))
                    {
                        Warn(this, message);
                        return;
                    }

                    Info(this, message);
                    dlg.Close();
                    Render();
                }
                catch (Exception ex)
                {
                    Warn(this, "Unable to process the staff removal request:\n" + ex.Message);
                }
            };

            txtUser.Focus();
            dlg.ShowDialog(this);
        }

        private static Panel CreateManagerDialogSurface(Panel body, out Panel footer)
        {
            body.Controls.Clear();

            var footerWrap = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 118,
                BackColor = Color.FromArgb(251, 248, 242),
                Padding = new Padding(0, 20, 0, 18)
            };
            body.Controls.Add(footerWrap);

            footer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            footerWrap.Controls.Add(footer);

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(22, 18, 22, 16),
                AutoScroll = true
            };
            body.Controls.Add(content);

            return content;
        }

        private static Panel CreateCenteredDialogPanel(Control host, int width, int initialHeight)
        {
            var panel = new Panel
            {
                Size = new Size(width, initialHeight),
                BackColor = Color.Transparent
            };
            host.Controls.Add(panel);
            return panel;
        }

        private static void CenterDialogPanel(Control host, Control panel, int minTop, int maxTop)
        {
            void ApplyLayout()
            {
                int left = Math.Max(0, (host.ClientSize.Width - panel.Width) / 2);
                int centeredTop = (host.ClientSize.Height - panel.Height) / 2;
                int top = Math.Max(minTop, Math.Min(centeredTop, maxTop));
                panel.Location = new Point(left, top);
            }

            host.Resize += (_, _) => ApplyLayout();
            ApplyLayout();
        }

        private static TextBox CreateCompactTextField(Control parent, ref int y, int width, string label, string hint, bool usePassword = false)
        {
            var input = BuildManagerTextBox(width - 32, usePassword);
            var block = CreateCompactFieldBlock(label, hint, input, width, ref y);
            parent.Controls.Add(block);
            return input;
        }

        private static Panel CreateCompactFieldBlock(string label, string hint, Control input, int width, ref int y)
        {
            var block = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, input is ComboBox ? 142 : 136),
                BackColor = Color.Transparent
            };

            block.Controls.Add(new Label
            {
                Text = label.ToUpperInvariant(),
                Location = new Point(6, 0),
                Size = new Size(width - 12, 24),
                Font = WildNestUI.FontLabel(9f),
                ForeColor = Color.FromArgb(140, 97, 16),
                BackColor = Color.Transparent
            });

            block.Controls.Add(new Label
            {
                Text = hint,
                Location = new Point(6, 28),
                Size = new Size(width - 12, 38),
                Font = WildNestUI.FontBody(9f),
                ForeColor = Color.FromArgb(122, 119, 112),
                BackColor = Color.Transparent
            });

            var field = InputSurface(0, 80, width, 46);
            if (input is TextBox txt)
            {
                txt.Location = new Point(16, 12);
                txt.Width = field.Width - 32;
            }
            else if (input is ComboBox cmb)
            {
                cmb.Location = new Point(12, 8);
                cmb.Width = field.Width - 24;
                cmb.Height = 30;
                cmb.IntegralHeight = false;
                cmb.DropDownHeight = 220;
            }

            field.Controls.Add(input);
            block.Controls.Add(field);

            y += block.Height + 16;
            return block;
        }

        private static void LayoutDialogFooter(Panel footer, Button actionButton)
        {
            footer.Controls.Clear();
            footer.Controls.Add(actionButton);

            void LayoutButton()
            {
                actionButton.Location = new Point(
                    Math.Max(0, (footer.ClientSize.Width - actionButton.Width) / 2),
                    Math.Max(0, (footer.ClientSize.Height - actionButton.Height) / 2));
            }

            footer.Resize += (_, _) => LayoutButton();
            LayoutButton();
        }

        private static void CenterDialogContent(Panel content, int contentWidth)
        {
            void LayoutChildren()
            {
                int left = Math.Max(0, (content.ClientSize.Width - contentWidth) / 2);
                foreach (Control child in content.Controls)
                {
                    child.Left = left;
                    child.Width = contentWidth;
                }
            }

            content.Resize += (_, _) => LayoutChildren();
            LayoutChildren();
        }

        private static Form BuildManagerModal(string title, string subtitle, Size size, out Panel body)
        {
            var dlg = new Form
            {
                Text = title + " - WildNest",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                ClientSize = size,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                AutoScaleMode = AutoScaleMode.None,
                BackColor = Color.FromArgb(228, 221, 206)
            };

            var frame = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(228, 221, 206)
            };
            dlg.Controls.Add(frame);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(251, 248, 242)
            };
            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var border = new Pen(Color.FromArgb(201, 190, 167), 1f);
                e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);
            };
            frame.Controls.Add(card);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 156,
                BackColor = WildNestUI.Green
            };
            header.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fill = new SolidBrush(WildNestUI.Green);
                e.Graphics.FillRectangle(fill, header.ClientRectangle);
                using var accent = new SolidBrush(Color.FromArgb(50, 135, 160, 67));
                e.Graphics.FillEllipse(accent, -30, -20, 110, 110);
                e.Graphics.FillEllipse(accent, header.Width - 110, 70, 132, 132);
            };
            card.Controls.Add(header);

            var titleLbl = new Label
            {
                Text = title,
                AutoSize = false,
                Location = new Point(40, 28),
                Size = new Size(size.Width - 270, 44),
                Font = WildNestUI.FontTitle(18f),
                ForeColor = Color.FromArgb(255, 249, 238),
                BackColor = Color.Transparent
            };
            header.Controls.Add(titleLbl);

            var subtitleLbl = new Label
            {
                Text = subtitle,
                AutoSize = false,
                Location = new Point(42, 82),
                Size = new Size(size.Width - 280, 36),
                Font = WildNestUI.FontBody(10.2f),
                ForeColor = Color.FromArgb(244, 236, 221),
                BackColor = Color.Transparent
            };
            header.Controls.Add(subtitleLbl);

            string projectRoot = Path.GetFullPath(Path.Combine(Application.StartupPath, "..", "..", ".."));
            string logoPath = Path.Combine(projectRoot, "Resources", "Logo.png");
            if (File.Exists(logoPath))
            {
                var logoWrap = new Panel
                {
                    Size = new Size(88, 88),
                    BackColor = Color.Transparent,
                    Location = new Point(header.Width - 180, 16),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                logoWrap.Paint += (_, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var fill = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
                    using var border = new Pen(Color.FromArgb(60, 255, 255, 255), 1.2f);
                    e.Graphics.FillEllipse(fill, 0, 0, logoWrap.Width - 1, logoWrap.Height - 1);
                    e.Graphics.DrawEllipse(border, 0, 0, logoWrap.Width - 1, logoWrap.Height - 1);
                };

                var logo = new PictureBox
                {
                    Size = new Size(62, 62),
                    Location = new Point(13, 13),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent,
                    Image = Image.FromFile(logoPath)
                };
                logoWrap.Controls.Add(logo);
                header.Controls.Add(logoWrap);
                logoWrap.BringToFront();
            }

            var close = new Button
            {
                Text = "X",
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(246, 241, 231),
                ForeColor = WildNestUI.Green,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - 62, 18)
            };
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 225, 208);
            close.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 212, 194);
            close.Click += (_, _) => dlg.Close();
            card.Controls.Add(close);
            close.BringToFront();
            card.Resize += (_, _) => close.Location = new Point(card.Width - 62, 18);

            body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30, 24, 30, 22),
                BackColor = Color.FromArgb(251, 248, 242),
                AutoScroll = false
            };
            card.Controls.Add(body);
            body.BringToFront();
            return dlg;
        }

        private static Panel BuildManagerInfoCard(int width, string title, string message)
        {
            var panel = new Panel
            {
                Size = new Size(width, 96),
                BackColor = Color.FromArgb(242, 247, 240)
            };
            panel.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(242, 247, 240));
                using var border = new Pen(Color.FromArgb(202, 221, 206), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            panel.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(18, 14),
                Size = new Size(width - 36, 22),
                Font = WildNestUI.FontLabel(9.5f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.Transparent
            });

            panel.Controls.Add(new Label
            {
                Text = message,
                Location = new Point(18, 38),
                Size = new Size(width - 36, 42),
                Font = WildNestUI.FontBody(9.4f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            return panel;
        }

        private static TextBox CreateManagerTextField(Control parent, ref int y, int width, string label, string hint, bool usePassword)
        {
            var input = BuildManagerTextBox(width - 32, usePassword);
            var block = BuildManagerField(width, label, hint, input);
            block.Location = new Point(0, y);
            parent.Controls.Add(block);
            y += block.Height + 14;
            return input;
        }

        private static Panel BuildManagerField(int width, string label, string hint, Control input)
        {
            var block = new Panel
            {
                Size = new Size(width, 118),
                BackColor = Color.Transparent
            };

            block.Controls.Add(new Label
            {
                Text = label.ToUpperInvariant(),
                Location = new Point(4, 0),
                Size = new Size(width - 8, 20),
                Font = WildNestUI.FontLabel(8.7f),
                ForeColor = Color.FromArgb(140, 97, 16),
                BackColor = Color.Transparent
            });

            block.Controls.Add(new Label
            {
                Text = hint,
                Location = new Point(4, 24),
                Size = new Size(width - 8, 36),
                Font = WildNestUI.FontBody(9.2f),
                ForeColor = Color.FromArgb(122, 119, 112),
                BackColor = Color.Transparent
            });

            var surface = new Panel
            {
                Location = new Point(0, 66),
                Size = new Size(width, 46),
                BackColor = Color.FromArgb(255, 253, 249)
            };
            surface.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, surface.Width - 1, surface.Height - 1), 13);
                using var fill = new SolidBrush(Color.FromArgb(255, 253, 249));
                using var border = new Pen(Color.FromArgb(216, 201, 176), 1.2f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            if (input is TextBox txt)
            {
                txt.Location = new Point(18, 12);
            }
            else if (input is ComboBox combo)
            {
                combo.Location = new Point(14, 7);
                combo.Width = surface.Width - 28;
            }

            surface.Controls.Add(input);
            block.Controls.Add(surface);
            return block;
        }

        private static TextBox BuildManagerTextBox(int width, bool usePassword)
        {
            return new TextBox
            {
                Width = width,
                Height = 24,
                BorderStyle = BorderStyle.None,
                Font = WildNestUI.FontBody(11f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.FromArgb(255, 253, 249),
                PasswordChar = usePassword ? '*' : '\0'
            };
        }

        private static ComboBox BuildManagerComboBox(int width)
        {
            return new ComboBox
            {
                Width = width,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = WildNestUI.FontBody(10.5f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 253, 249)
            };
        }

        private static Label BuildManagerNote(int width, string text)
        {
            return new Label
            {
                Text = text,
                Size = new Size(width, 36),
                Font = WildNestUI.FontBody(8.95f),
                ForeColor = Color.FromArgb(123, 119, 112),
                BackColor = Color.Transparent
            };
        }

        private static Button BuildManagerActionButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(240, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = WildNestUI.FontLabel(10.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(bg);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.DarkDark(bg);
            return btn;
        }

        private static Panel CreateDialogCanvas(Control body)
        {
            var canvas = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent
            };
            body.Controls.Add(canvas);
            canvas.BringToFront();
            return canvas;
        }

        private static TextBox CreateManualTextField(Control parent, ref int y, string label, string hint, bool usePassword = false)
        {
            var txt = StyledTextBox(0, 0, 492, usePassword);
            var block = CreateFieldBlock(label, hint, txt);
            block.Location = new Point(0, y);
            parent.Controls.Add(block);
            y += block.Height + 16;
            return txt;
        }

        private static Panel BuildManagerFormCanvas(Control body, out Panel content, out Panel footer)
        {
            body.Controls.Clear();
            body.Padding = new Padding(30, 26, 30, 24);

            footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.FromArgb(249, 247, 243)
            };
            body.Controls.Add(footer);

            var viewport = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                BackColor = Color.Transparent
            };
            body.Controls.Add(viewport);

            content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            viewport.Controls.Add(content);
            return viewport;
        }

        private static Control BuildManagerLeadNote(string title, string text, int width, ref int y)
        {
            var note = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, 78),
                BackColor = Color.FromArgb(239, 245, 238)
            };
            note.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, note.Width - 1, note.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(239, 245, 238));
                using var border = new Pen(Color.FromArgb(204, 220, 205), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            note.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(18, 12),
                Size = new Size(width - 36, 20),
                Font = WildNestUI.FontLabel(9.4f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.Transparent
            });

            note.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(18, 34),
                Size = new Size(width - 36, 28),
                Font = WildNestUI.FontBody(9.25f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            y += note.Height + 16;
            return note;
        }

        private static Panel CreateExecutiveFieldBlock(string label, string hint, Control input, int width, ref int y)
        {
            var block = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(width, 98),
                BackColor = Color.Transparent
            };

            block.Controls.Add(new Label
            {
                Text = label.ToUpperInvariant(),
                Location = new Point(4, 0),
                Size = new Size(width - 8, 18),
                Font = WildNestUI.FontLabel(8.9f),
                ForeColor = Color.FromArgb(138, 98, 18),
                BackColor = Color.Transparent
            });

            block.Controls.Add(new Label
            {
                Text = hint,
                Location = new Point(4, 20),
                Size = new Size(width - 8, 24),
                Font = WildNestUI.FontBody(8.95f),
                ForeColor = Color.FromArgb(122, 119, 112),
                BackColor = Color.Transparent
            });

            var field = InputSurface(4, 50, width - 8, 44);
            if (input is TextBox txt)
            {
                txt.Location = new Point(16, 11);
                txt.Width = field.Width - 32;
            }
            else if (input is ComboBox cmb)
            {
                cmb.Location = new Point(12, 6);
                cmb.Width = field.Width - 24;
                cmb.Height = 30;
                cmb.IntegralHeight = false;
                cmb.DropDownHeight = 220;
            }

            field.Controls.Add(input);
            block.Controls.Add(field);

            y += block.Height + 12;
            return block;
        }

        private static Control BuildExecutiveFootNote(string text, int width, ref int y)
        {
            var note = new Label
            {
                Text = text,
                Location = new Point(4, y),
                Size = new Size(width - 8, 34),
                Font = WildNestUI.FontBody(8.85f),
                ForeColor = Color.FromArgb(122, 119, 112),
                BackColor = Color.Transparent
            };
            y += note.Height + 8;
            return note;
        }

        private static bool IsManagerAccount(string username) =>
            StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_users WHERE LOWER(Username) = LOWER(@u) AND Role IN ('Manager','Administrator');",
                new MySqlParameter("@u", username)) > 0;

        private static FlowLayoutPanel PrepareDialogStack(Control body, out Panel footer, bool allowScroll, int footerHeight)
        {
            body.Controls.Clear();
            body.Padding = new Padding(34, 24, 34, 24);

            footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = footerHeight,
                BackColor = Color.FromArgb(251, 248, 242),
                Padding = new Padding(0, 18, 0, 10)
            };
            body.Controls.Add(footer);

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = allowScroll,
                AutoSize = false,
                Padding = new Padding(0, 0, 8, 6),
                BackColor = Color.Transparent
            };

            void ResizeChildren()
            {
                int usableWidth = Math.Max(620, stack.ClientSize.Width - (allowScroll ? SystemInformation.VerticalScrollBarWidth : 0) - 8);
                foreach (Control child in stack.Controls)
                {
                    child.Width = usableWidth;
                }
            }

            stack.Resize += (_, _) => ResizeChildren();
            stack.ControlAdded += (_, e) =>
            {
                if (e.Control.Margin == Padding.Empty)
                    e.Control.Margin = new Padding(0, 0, 0, 16);
                ResizeChildren();
            };

            body.Controls.Add(stack);
            stack.BringToFront();
            footer.BringToFront();
            ResizeChildren();
            return stack;
        }

        private static void AddFooterAction(Panel footer, Button actionButton)
        {
            footer.Controls.Clear();
            footer.Controls.Add(actionButton);

            void LayoutButton()
            {
                actionButton.Location = new Point(
                    Math.Max(0, footer.ClientSize.Width - actionButton.Width),
                    Math.Max(0, (footer.ClientSize.Height - actionButton.Height) / 2));
            }

            footer.Resize += (_, _) => LayoutButton();
            LayoutButton();
        }

        private static string HashPassword(string plain)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static Form BuildDialog(string title, string subtitle, int w, int h)
        {
            var dialog = StaffPortalUi.BuildEliteDialog(title, subtitle, new Size(w, h));
            dialog.Text = title + " - WildNest";
            return dialog;
        }

        private static Control DialogBody(Form dlg)
        {
            static Control? FindBody(Control root)
            {
                foreach (Control child in root.Controls)
                {
                    if (string.Equals(child.Name, "EliteDialogBody", StringComparison.Ordinal))
                        return child;

                    var nested = FindBody(child);
                    if (nested != null)
                        return nested;
                }

                return null;
            }

            return FindBody(dlg) ?? dlg;
        }

        private static FlowLayoutPanel BuildDialogStack(Control body, bool autoScroll = true, Padding? padding = null)
        {
            Panel? footer = body.Controls.OfType<Panel>().FirstOrDefault(p => Convert.ToString(p.Tag) == "DialogFooter");
            if (footer == null)
            {
                footer = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 98,
                    Padding = new Padding(0, 20, 0, 20),
                    BackColor = Color.FromArgb(249, 247, 243),
                    Tag = "DialogFooter"
                };
                body.Controls.Add(footer);
            }

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = autoScroll,
                AutoSize = false,
                Padding = padding ?? new Padding(8, 12, 8, 18),
                BackColor = Color.Transparent
            };

            void ResizeChildren()
            {
                int scrollAllowance = autoScroll ? SystemInformation.VerticalScrollBarWidth : 0;
                int usableWidth = Math.Max(600, stack.ClientSize.Width - scrollAllowance - 26);
                foreach (Control child in stack.Controls)
                    child.Width = usableWidth;
            }

            stack.Resize += (_, _) => ResizeChildren();
            stack.ControlAdded += (_, e) =>
            {
                e.Control.Margin = new Padding(0, 0, 0, e.Control.Margin.Bottom == 0 ? 16 : e.Control.Margin.Bottom);
                ResizeChildren();
            };

            body.Controls.Add(stack);
            stack.BringToFront();
            footer.BringToFront();
            ResizeChildren();
            return stack;
        }

        private static TextBox CreateTextField(FlowLayoutPanel stack, string label, string hint, bool usePassword = false)
        {
            var txt = StyledTextBox(0, 0, 492, usePassword);
            stack.Controls.Add(CreateFieldBlock(label, hint, txt));
            return txt;
        }

        private static Panel CreateFieldBlock(string label, string hint, Control input)
        {
            var block = new Panel
            {
                Width = 640,
                Height = input is ComboBox ? 156 : 150,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Location = new Point(4, 0),
                AutoSize = true,
                Font = WildNestUI.FontLabel(8.25f),
                ForeColor = Color.FromArgb(126, 98, 35),
                BackColor = Color.Transparent
            };
            block.Controls.Add(lbl);

            var helper = new Label
            {
                Text = hint,
                Location = new Point(4, 22),
                Size = new Size(628, 48),
                Font = WildNestUI.FontBody(8.85f),
                ForeColor = Color.FromArgb(126, 123, 118),
                BackColor = Color.Transparent
            };
            block.Controls.Add(helper);

            var field = InputSurface(4, 92, 628, 50);
            if (input is TextBox txt)
            {
                txt.Location = new Point(16, 13);
                txt.Width = field.Width - 32;
            }
            else if (input is ComboBox cmb)
            {
                cmb.Location = new Point(12, 8);
                cmb.Width = field.Width - 24;
                cmb.Height = 32;
                cmb.IntegralHeight = false;
                cmb.DropDownHeight = 220;
            }

            field.Controls.Add(input);
            block.Controls.Add(field);

            void ResizeField()
            {
                helper.Width = Math.Max(220, block.Width - 12);
                field.Width = Math.Max(220, block.Width - 12);
                field.Top = 92;
                if (input is TextBox resizedTxt)
                    resizedTxt.Width = field.Width - 32;
                else if (input is ComboBox resizedCombo)
                    resizedCombo.Width = field.Width - 24;
            }

            block.Resize += (_, _) => ResizeField();
            ResizeField();
            return block;
        }

        private static Panel DialogInfoBand(string title, string message)
        {
            var band = new Panel
            {
                Width = 568,
                Height = 104,
                Margin = new Padding(0, 0, 0, 20),
                BackColor = Color.FromArgb(238, 245, 239)
            };
            band.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, band.Width - 1, band.Height - 1), 14);
                using var fill = new SolidBrush(Color.FromArgb(238, 245, 239));
                using var border = new Pen(Color.FromArgb(196, 220, 201), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            var titleLbl = new Label
            {
                Text = title,
                Location = new Point(18, 14),
                AutoSize = true,
                Font = WildNestUI.FontLabel(9.25f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.Transparent
            };
            band.Controls.Add(titleLbl);

            var msgLbl = new Label
            {
                Text = message,
                Location = new Point(18, 34),
                Size = new Size(532, 52),
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            };
            band.Controls.Add(msgLbl);
            return band;
        }

        private static Label CreateFooterNote(string text)
        {
            return new Label
            {
                Text = text,
                Width = 640,
                Height = 62,
                Margin = new Padding(0, 0, 0, 14),
                Font = WildNestUI.FontBody(8.75f),
                ForeColor = Color.FromArgb(126, 123, 118),
                BackColor = Color.Transparent
            };
        }

        private static TextBox CreateCompactCreateTextField(FlowLayoutPanel stack, string label, string hint, bool usePassword = false)
        {
            var txt = StyledTextBox(0, 0, 492, usePassword);
            stack.Controls.Add(CreateCompactCreateFieldBlock(label, hint, txt));
            return txt;
        }

        private static Panel CreateCompactCreateFieldBlock(string label, string hint, Control input)
        {
            var isCombo = input is ComboBox;
            var block = new Panel
            {
                Width = 640,
                Height = isCombo ? 104 : 100,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Location = new Point(4, 0),
                AutoSize = true,
                Font = WildNestUI.FontLabel(8.2f),
                ForeColor = Color.FromArgb(126, 98, 35),
                BackColor = Color.Transparent
            };
            block.Controls.Add(lbl);

            var helper = new Label
            {
                Text = hint,
                Location = new Point(4, 18),
                Size = new Size(628, 26),
                Font = WildNestUI.FontBody(8.45f),
                ForeColor = Color.FromArgb(126, 123, 118),
                BackColor = Color.Transparent
            };
            block.Controls.Add(helper);

            var field = InputSurface(4, 52, 628, 42);
            if (input is TextBox txt)
            {
                txt.Location = new Point(16, 10);
                txt.Width = field.Width - 32;
            }
            else if (input is ComboBox cmb)
            {
                cmb.Location = new Point(12, 5);
                cmb.Width = field.Width - 24;
                cmb.Height = 28;
                cmb.IntegralHeight = false;
                cmb.DropDownHeight = 220;
            }

            field.Controls.Add(input);
            block.Controls.Add(field);

            void ResizeField()
            {
                helper.Width = Math.Max(220, block.Width - 12);
                field.Width = Math.Max(220, block.Width - 12);
                if (input is TextBox resizedTxt)
                    resizedTxt.Width = field.Width - 32;
                else if (input is ComboBox resizedCombo)
                    resizedCombo.Width = field.Width - 24;
            }

            block.Resize += (_, _) => ResizeField();
            ResizeField();
            return block;
        }

        private static Panel CreateCompactInfoBand(string title, string message)
        {
            var band = new Panel
            {
                Width = 568,
                Height = 68,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.FromArgb(238, 245, 239)
            };
            band.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, band.Width - 1, band.Height - 1), 14);
                using var fill = new SolidBrush(Color.FromArgb(238, 245, 239));
                using var border = new Pen(Color.FromArgb(196, 220, 201), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            band.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(18, 10),
                AutoSize = true,
                Font = WildNestUI.FontLabel(8.95f),
                ForeColor = WildNestUI.Green,
                BackColor = Color.Transparent
            });

            band.Controls.Add(new Label
            {
                Text = message,
                Location = new Point(18, 28),
                Size = new Size(532, 24),
                Font = WildNestUI.FontBody(8.45f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            });

            return band;
        }

        private static Label CreateCompactFooterNote(string text)
        {
            return new Label
            {
                Text = text,
                Width = 640,
                Height = 28,
                Margin = new Padding(0, 0, 0, 6),
                Font = WildNestUI.FontBody(8.35f),
                ForeColor = Color.FromArgb(126, 123, 118),
                BackColor = Color.Transparent
            };
        }

        private static TextBox StyledTextBox(int x, int y, int width, bool usePassword = false)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = width,
                Height = 26,
                Font = WildNestUI.FontBody(10.8f),
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(255, 253, 249),
                ForeColor = WildNestUI.TextDark,
                PasswordChar = usePassword ? '*' : '\0'
            };
        }

        private static Panel InputSurface(int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.FromArgb(255, 253, 249)
            };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 12);
                using var fill = new SolidBrush(Color.FromArgb(255, 253, 249));
                using var border = new Pen(Color.FromArgb(214, 201, 180), 1.2f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            return panel;
        }

        private static Button DialogButton(Form dlg, string text, Color bg)
        {
            var body = DialogBody(dlg);
            var btn = new Button
            {
                Text = text,
                Size = new Size(240, 48),
                Margin = new Padding(0),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = WildNestUI.FontLabel(10.25f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(bg);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.DarkDark(bg);
            Panel? footer = body.Controls.OfType<Panel>().FirstOrDefault(p => Convert.ToString(p.Tag) == "DialogFooter");
            if (footer == null)
            {
                footer = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 98,
                    Padding = new Padding(0, 20, 0, 20),
                    BackColor = Color.FromArgb(249, 247, 243),
                    Tag = "DialogFooter"
                };
                body.Controls.Add(footer);
                footer.BringToFront();
            }

            footer.Controls.Clear();
            footer.Controls.Add(btn);
            void CenterButton()
            {
                btn.Location = new Point(Math.Max(0, (footer.ClientSize.Width - btn.Width) / 2), Math.Max(0, (footer.ClientSize.Height - btn.Height) / 2));
            }
            footer.Resize += (_, _) => CenterButton();
            CenterButton();
            return btn;
        }

        private static Button ActionButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(220, 38),
                Margin = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font = WildNestUI.FontLabel(9.5f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(bg);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.DarkDark(bg);
            return btn;
        }

        private static void Warn(IWin32Window owner, string msg) =>
            StaffPortalUi.ShowEliteMessage(owner, "WildNest Manager", msg, StaffPortalUi.MessageTone.Warning);

        private static void Info(IWin32Window owner, string msg) =>
            StaffPortalUi.ShowEliteMessage(owner, "WildNest Manager", msg, StaffPortalUi.MessageTone.Success);
    }
}

