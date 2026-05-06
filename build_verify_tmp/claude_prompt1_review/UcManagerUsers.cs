// ============================================================
//  UcManagerUsers.cs   (namespace Project.UcManager)
//  REPLACES: UcAdminUsers.cs
//
//  NEW vs original
//  • Three action buttons: Create Account, Toggle Active, Reset Password
//  • Create Account dialog — Manager can only create Reception/ZooKeeper/TourGuide
//  • Toggle Active/Inactive dialog — blocked for Manager rows
//  • Reset Password dialog — blocked for Manager rows
//  • SHA-256 hashing matches VerifyPassword() in StaffLogin
//  • All read-only stats/grid cards preserved from original
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcManager
{
    public partial class UcManagerUsers : UserControl
    {
        public UcManagerUsers()
        {
            Dock  = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        // ── Main page render ──────────────────────────────────────────────

        private void Render()
        {
            Controls.Clear();

            bool dbHealthy = StaffPortalDb.CanConnect(out string dbMsg);

            int totalUsers     = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users;");
            int activeUsers    = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 1;");
            int inactiveUsers  = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 0;");
            int adminUsers     = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role IN ('Manager','Administrator');");
            int receptionUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Reception%';");
            int fieldOpsUsers  = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE Role LIKE '%Tour%' OR Role LIKE '%Zoo%';");

            var roleBreakdown = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Role),''),'Unspecified') AS `Role`,
       COUNT(*) AS `Accounts`,
       SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS `Active`,
       SUM(CASE WHEN IsActive=0 THEN 1 ELSE 0 END) AS `Inactive`
FROM tbl_users
GROUP BY COALESCE(NULLIF(TRIM(Role),''),'Unspecified')
ORDER BY `Accounts` DESC;");

            var users = StaffPortalDb.GetTable(@"
SELECT UserID      AS `User ID`,
       FullName    AS `Full Name`,
       Username,
       Role,
       ContactNo   AS `Contact No.`,
       CASE WHEN IsActive=1 THEN 'Active' ELSE 'Inactive' END AS `Status`
FROM tbl_users
ORDER BY FullName;");

            // ── Action button row ──────────────────────────────────────
            var btnCreate = ActionButton("＋  Create Staff Account", WildNestUI.Green);
            var btnToggle = ActionButton("⏸  Toggle Active / Inactive", WildNestUI.Amber);
            var btnReset  = ActionButton("🔑  Reset Password", WildNestUI.Blue);

            btnCreate.Click += (s, e) => ShowCreateDialog();
            btnToggle.Click += (s, e) => ShowToggleDialog();
            btnReset.Click  += (s, e) => ShowResetDialog();

            var actionRow = new FlowLayoutPanel
            {
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 0, 0, 10)
            };
            actionRow.Controls.AddRange(new Control[] { btnCreate, btnToggle, btnReset });

            var page = StaffPortalUi.BuildPage(
                "Staff Account Management",
                "Create and manage staff accounts for Reception, ZooKeeper, and TourGuide roles.",
                new List<Control>
                {
                    StaffPortalUi.AlertBanner(dbMsg, !dbHealthy),
                    actionRow,
                    StaffPortalUi.StatsRow(
                        (totalUsers.ToString(),     "Total Accounts",       WildNestUI.Blue),
                        (activeUsers.ToString(),    "Active Accounts",      WildNestUI.Green),
                        (inactiveUsers.ToString(),  "Inactive Accounts",    WildNestUI.Amber),
                        (fieldOpsUsers.ToString(),  "Tour + Zoo Operations",WildNestUI.Green)),
                    StaffPortalUi.MetricTableCard(
                        "Account Snapshot",
                        ("Manager Accounts",   adminUsers.ToString()),
                        ("Reception Staff",    receptionUsers.ToString()),
                        ("Inactive Accounts",  inactiveUsers.ToString()),
                        ("Field Operations",   fieldOpsUsers.ToString()),
                        ("DB Health",          dbHealthy ? "Connected" : "Disconnected")),
                    StaffPortalUi.GridCard("Role Breakdown",    roleBreakdown, "No role data available."),
                    StaffPortalUi.GridCard("All Staff Accounts", users,        "No staff accounts found.")
                });

            Controls.Add(page);
        }

        // ── Dialog: Create Staff Account ──────────────────────────────────

        private void ShowCreateDialog()
        {
            using var dlg = BuildDialog("Create Staff Account", 440, 400);

            int y = 20;
            var (_, txtFullName) = Row(dlg, ref y, "Full Name");
            var (_, txtUsername) = Row(dlg, ref y, "Username");

            // Password row (masked)
            RowLabel(dlg, "Password", y);
            var txtPass = new TextBox
            {
                Location     = new Point(150, y), Width = 260,
                Font         = WildNestUI.FontBody(10f),
                PasswordChar = '●'
            };
            dlg.Controls.Add(txtPass);
            y += 38;

            var (_, txtContact) = Row(dlg, ref y, "Contact No.");

            RowLabel(dlg, "Role", y);
            var cmbRole = new ComboBox
            {
                Location      = new Point(150, y), Width = 260,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = WildNestUI.FontBody(10f)
            };
            // Managers may only create staff-level roles
            cmbRole.Items.AddRange(new object[] { "Reception", "ZooKeeper", "TourGuide" });
            cmbRole.SelectedIndex = 0;
            dlg.Controls.Add(cmbRole);
            y += 46;

            var btnSave = DialogButton(dlg, "Create Account", WildNestUI.Green, y);
            btnSave.Click += (s, e) =>
            {
                string fn   = txtFullName.Text.Trim();
                string un   = txtUsername.Text.Trim();
                string pw   = txtPass.Text;
                string cont = txtContact.Text.Trim();
                string role = cmbRole.Text;

                if (string.IsNullOrWhiteSpace(fn) || string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(pw))
                {
                    Warn("Full name, username, and password are required.");
                    return;
                }

                int exists = StaffPortalDb.Count(
                    "SELECT COUNT(*) FROM tbl_users WHERE LOWER(Username) = LOWER(@u);",
                    new MySqlParameter("@u", un));

                if (exists > 0) { Warn("That username is already taken."); return; }

                try
                {
                    StaffPortalDb.Execute(
                        @"INSERT INTO tbl_users (FullName, Username, PasswordHash, Role, ContactNo, IsActive, CreatedAt)
                          VALUES (@fn, @un, @pw, @role, @cn, 1, NOW());",
                        new MySqlParameter("@fn",   fn),
                        new MySqlParameter("@un",   un),
                        new MySqlParameter("@pw",   HashPw(pw)),
                        new MySqlParameter("@role", role),
                        new MySqlParameter("@cn",   cont));

                    Info($"Account created for {fn} ({role}).");
                    dlg.Close();
                    Render();
                }
                catch (Exception ex) { Warn("Failed to create account:\n" + ex.Message); }
            };

            dlg.ShowDialog(this);
        }

        // ── Dialog: Toggle Active/Inactive ────────────────────────────────

        private void ShowToggleDialog()
        {
            using var dlg = BuildDialog("Toggle Account Status", 420, 200);

            int y = 20;
            var (_, txtUser) = Row(dlg, ref y, "Username");
            var btnGo = DialogButton(dlg, "Toggle Status", WildNestUI.Amber, y);

            btnGo.Click += (s, e) =>
            {
                string un = txtUser.Text.Trim();
                if (string.IsNullOrWhiteSpace(un)) return;

                if (IsManagerAccount(un)) { Warn("Manager accounts cannot be modified here."); return; }

                try
                {
                    StaffPortalDb.Execute(
                        "UPDATE tbl_users SET IsActive = IF(IsActive=1,0,1) WHERE LOWER(Username) = LOWER(@u);",
                        new MySqlParameter("@u", un));

                    int newStatus = StaffPortalDb.Count(
                        "SELECT IsActive FROM tbl_users WHERE LOWER(Username) = LOWER(@u);",
                        new MySqlParameter("@u", un));

                    Info($"Account '{un}' is now {(newStatus == 1 ? "Active" : "Inactive")}.");
                    dlg.Close();
                    Render();
                }
                catch (Exception ex) { Warn("Error: " + ex.Message); }
            };

            dlg.ShowDialog(this);
        }

        // ── Dialog: Reset Password ────────────────────────────────────────

        private void ShowResetDialog()
        {
            using var dlg = BuildDialog("Reset Staff Password", 420, 240);

            int y = 20;
            var (_, txtUser) = Row(dlg, ref y, "Username");

            RowLabel(dlg, "New Password", y);
            var txtNewPw = new TextBox
            {
                Location     = new Point(150, y), Width = 240,
                Font         = WildNestUI.FontBody(10f),
                PasswordChar = '●'
            };
            dlg.Controls.Add(txtNewPw);
            y += 46;

            var btnGo = DialogButton(dlg, "Reset Password", WildNestUI.Blue, y);
            btnGo.Click += (s, e) =>
            {
                string un = txtUser.Text.Trim();
                string pw = txtNewPw.Text;

                if (string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(pw))
                {
                    Warn("Both fields are required.");
                    return;
                }

                if (IsManagerAccount(un)) { Warn("Manager passwords cannot be reset here."); return; }

                try
                {
                    StaffPortalDb.Execute(
                        "UPDATE tbl_users SET PasswordHash = @pw WHERE LOWER(Username) = LOWER(@u);",
                        new MySqlParameter("@pw", HashPw(pw)),
                        new MySqlParameter("@u",  un));

                    Info($"Password reset for '{un}'.");
                    dlg.Close();
                }
                catch (Exception ex) { Warn("Error: " + ex.Message); }
            };

            dlg.ShowDialog(this);
        }

        // ── Utilities ─────────────────────────────────────────────────────

        private static bool IsManagerAccount(string username) =>
            StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_users WHERE LOWER(Username)=LOWER(@u) AND Role IN ('Manager','Administrator');",
                new MySqlParameter("@u", username)) > 0;

        private static string HashPw(string plain)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ── Dialog / control builders ─────────────────────────────────────

        private static Form BuildDialog(string title, int w, int h) => new Form
        {
            Text            = title + "  —  WildNest",
            Size            = new Size(w, h),
            StartPosition   = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox     = false,
            MinimizeBox     = false,
            BackColor       = WildNestUI.Sand
        };

        private static (Label lbl, TextBox txt) Row(Form dlg, ref int y, string label)
        {
            var lbl = RowLabel(dlg, label, y);
            var txt = new TextBox { Location = new Point(150, y), Width = 240, Font = WildNestUI.FontBody(10f) };
            dlg.Controls.Add(txt);
            y += 38;
            return (lbl, txt);
        }

        private static Label RowLabel(Form dlg, string text, int y)
        {
            var lbl = new Label
            {
                Text     = text + ":",
                Location = new Point(20, y + 3),
                AutoSize = true,
                Font     = WildNestUI.FontLabel(9f),
                ForeColor = WildNestUI.Muted
            };
            dlg.Controls.Add(lbl);
            return lbl;
        }

        private static Button DialogButton(Form dlg, string text, Color bg, int y)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(20, y),
                Size      = new Size(380, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font      = WildNestUI.FontLabel(10f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            dlg.Controls.Add(btn);
            return btn;
        }

        private static Button ActionButton(string text, Color bg)
        {
            var btn = new Button
            {
                Text      = text,
                Size      = new Size(220, 38),
                Margin    = new Padding(0, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = bg,
                ForeColor = Color.White,
                Font      = WildNestUI.FontLabel(9.5f),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static void Warn(string msg) =>
            MessageBox.Show(msg, "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private static void Info(string msg) =>
            MessageBox.Show(msg, "WildNest", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
