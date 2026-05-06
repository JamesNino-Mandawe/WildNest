// ============================================================
//  StaffLogin.cs
//  CHANGE SUMMARY
//  • "Administrator" in the role dropdown is now "Manager"
//  • CanonicalRole maps "administrator"/"admin" → "Manager"
//    (so any legacy DB row with Role = 'Administrator' still logs in)
//  • Demo account updated: user "manager" / pass "manager123"
//  • No visual or layout changes — all paint helpers preserved
// ============================================================

using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Project
{
    public partial class StaffLogin : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        public string? SelectedRole { get; private set; }

        private const int CardW  = 420;
        private const int CardH  = 680;
        private const int InputH = 46;
        private const int Pad    = 36;

        private Panel? pnlCard;
        private Panel? pnlUserFake;
        private Panel? pnlPassFake;
        private Panel? pnlRoleFake;
        private Panel? pnlDivider;
        private Label? lblBack;

        // ── Demo / fallback accounts ──────────────────────────────────────
        // "manager" replaces the old "admin" demo account.
        // The role string must match exactly what CanonicalRole() returns.
        private static readonly (string user, string pass, string role, string display)[] Accounts =
        {
            ("manager", "manager123", "Manager",   "Resort Manager"),
            ("maria",   "maria123",   "Reception", "Maria Santos"),
            ("santos",  "santos123",  "Reception", "Santos Rivera"),
            ("jose",    "jose123",    "ZooKeeper", "Jose Reyes"),
            ("reyes",   "reyes123",   "ZooKeeper", "Reyes Dela Cruz"),
            ("ana",     "ana123",     "TourGuide", "Ana Cruz"),
            ("cruz",    "cruz123",    "TourGuide", "Cruz Gomez"),
        };

        public StaffLogin()
        {
            InitializeComponent();
            BuildLogin();
        }

        private void BuildLogin()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState     = FormWindowState.Maximized;
            BackColor       = Color.FromArgb(5, 18, 10);
            DoubleBuffered  = true;

            Opacity = 0;
            var fade = new System.Windows.Forms.Timer { Interval = 10 };
            fade.Tick += (s, e) => { if (Opacity < 1) Opacity += 0.05; else fade.Stop(); };
            fade.Start();

            pnlCard = new Panel { Size = new Size(CardW, CardH), BackColor = Color.Transparent };
            pnlCard.Paint += PnlCard_Paint;

            picCardLogo.SizeMode  = PictureBoxSizeMode.Zoom;
            picCardLogo.Size      = new Size(80, 80);
            picCardLogo.BackColor = Color.Transparent;
            picCardLogo.Image     = Properties.Resources.Logo;

            lblBrandCard.Text      = "WILDNEST";
            lblBrandCard.Font      = new Font("Georgia", 22, FontStyle.Bold);
            lblBrandCard.ForeColor = Color.FromArgb(248, 244, 239);
            lblBrandCard.BackColor = Color.Transparent;
            lblBrandCard.AutoSize  = true;

            StyleTag(lblSubCard, "STAFF PORTAL  ·  RESORT MANAGEMENT");
            lblSubCard.BackColor = Color.Transparent;
            lblSubCard.AutoSize  = true;

            StyleTag(lblUserTag, "Employee ID / Username");
            StyleTag(lblPassTag, "Password");
            StyleTag(lblRoleTag, "Role");

            StyleInput(txtUsername);
            pnlUserFake = WrapInput(txtUsername);

            StyleInput(txtPassword);
            txtPassword.PasswordChar = '●';
            pnlPassFake = WrapInput(txtPassword);

            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.FlatStyle     = FlatStyle.Flat;
            cmbRole.BackColor     = Color.FromArgb(20, 46, 28);
            cmbRole.ForeColor     = Color.FromArgb(248, 244, 239);
            cmbRole.Font          = new Font("Segoe UI", 11);
            cmbRole.DrawMode      = DrawMode.OwnerDrawFixed;
            cmbRole.ItemHeight    = 26;
            cmbRole.DrawItem     += CmbRole_DrawItem;
            cmbRole.Items.Clear();
            // ── KEY CHANGE: "Manager" is now the top-tier role ──
            cmbRole.Items.AddRange(new object[] { "Manager", "Reception", "ZooKeeper", "TourGuide" });
            cmbRole.SelectedIndex = 0;
            pnlRoleFake = WrapCombo(cmbRole);

            pnlDivider = new Panel { Height = 1, BackColor = Color.FromArgb(50, 212, 160, 23) };

            btnLogin.Text      = "";
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.BackColor = Color.Transparent;
            btnLogin.Cursor    = Cursors.Hand;
            btnLogin.Height    = 46;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Paint += BtnLogin_Paint;
            btnLogin.Click += BtnLogin_Click;

            lblBack = new Label
            {
                Text      = "Back to WildNest Homepage",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(90, 248, 244, 239),
                BackColor = Color.Transparent,
                AutoSize  = true,
                Cursor    = Cursors.Hand
            };
            lblBack.MouseEnter += (s, e) => lblBack.ForeColor = Color.FromArgb(210, 248, 244, 239);
            lblBack.MouseLeave += (s, e) => lblBack.ForeColor = Color.FromArgb(90, 248, 244, 239);
            lblBack.Click      += LblBack_Click;

            pnlCard.Controls.AddRange(new Control[]
            {
                picCardLogo, lblBrandCard, lblSubCard,
                lblUserTag,  pnlUserFake,
                lblPassTag,  pnlPassFake,
                lblRoleTag,  pnlRoleFake,
                pnlDivider,  btnLogin, lblBack
            });

            Controls.Clear();
            Controls.Add(pnlCard);

            Load   += (s, e) => LayoutCard();
            Resize += (s, e) => LayoutCard();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Login logic
        // ─────────────────────────────────────────────────────────────────

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim().ToLower();
            string pass = txtPassword.Text;
            string role = CanonicalRole(cmbRole.Text);

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ShowError("Please enter your username and password.");
                return;
            }

            string? dbError = null;

            try
            {
                using var conn = new MySqlConnection(StaffPortalDb.ConnString);
                conn.Open();

                const string query = @"
SELECT FullName, PasswordHash, Role
FROM tbl_users
WHERE LOWER(Username) = LOWER(@u)
  AND IsActive = 1;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", user);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string dbRole   = CanonicalRole(reader["Role"]?.ToString() ?? "");
                    string dbPass   = reader["PasswordHash"]?.ToString() ?? "";

                    if (dbRole != role || !VerifyPassword(pass, dbPass))
                        continue;

                    string displayName = reader["FullName"]?.ToString() ?? role;
                    LaunchDashboard(role, displayName);
                    return;
                }
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
                ProjectDiagnostics.LogError("StaffLogin", ex, "DB auth");
            }

            if (TryDemoLogin(user, pass, role, out string demoName))
            {
                LaunchDashboard(role, demoName);
                return;
            }

            string suffix = string.IsNullOrWhiteSpace(dbError) ? "" : "\n\nDB detail: " + dbError;
            ShowError("Invalid credentials, inactive account, or role mismatch." + suffix);
        }

        private void LaunchDashboard(string role, string displayName)
        {
            var dashboard = new StaffDashboard(role, displayName);
            dashboard.Show();
            Hide();
        }

        private void ShowError(string msg) =>
            MessageBox.Show(msg, "WildNest Staff Portal",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private static bool TryDemoLogin(string user, string pass, string role, out string displayName)
        {
            foreach (var a in Accounts)
            {
                if (string.Equals(a.user, user, StringComparison.OrdinalIgnoreCase)
                    && a.pass == pass
                    && CanonicalRole(a.role) == role)
                {
                    displayName = a.display;
                    return true;
                }
            }
            displayName = "";
            return false;
        }

        // ── KEY CHANGE: "Administrator" / "admin" → "Manager" ─────────────
        // This means any tbl_users row where Role = 'Administrator' will
        // automatically resolve to the Manager portal — no DB migration needed.
        internal static string CanonicalRole(string role)
        {
            string n = Normalize(role);
            return n switch
            {
                "manager" or "administrator" or "admin" => "Manager",
                "reception" or "frontdesk"              => "Reception",
                "zookeeper" or "keeper"                 => "ZooKeeper",
                "tourguide" or "guide"                  => "TourGuide",
                _                                       => role.Trim()
            };
        }

        private static string Normalize(string role)
        {
            var sb = new StringBuilder();
            foreach (char c in role)
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        private static bool VerifyPassword(string typed, string stored)
        {
            stored = (stored ?? "").Trim();
            if (stored.Length == 0) return false;
            if (stored == typed) return true;

            string h = Sha256Hex(typed);
            return stored.Equals(h, StringComparison.OrdinalIgnoreCase)
                || stored.Equals("sha256:" + h, StringComparison.OrdinalIgnoreCase);
        }

        private static string Sha256Hex(string value)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Layout  (unchanged from original)
        // ─────────────────────────────────────────────────────────────────

        private void LayoutCard()
        {
            if (pnlCard == null || pnlUserFake == null || pnlPassFake == null
                || pnlRoleFake == null || pnlDivider == null || lblBack == null) return;

            pnlCard.Location = new Point(
                (ClientSize.Width  - CardW) / 2,
                (ClientSize.Height - CardH) / 2);

            int inputW = CardW - Pad * 2;
            int x = Pad;

            picCardLogo.Location  = new Point((CardW - picCardLogo.Width) / 2, 18);
            lblBrandCard.Location = new Point((CardW - lblBrandCard.PreferredWidth) / 2, picCardLogo.Bottom + 8);
            lblSubCard.Location   = new Point((CardW - lblSubCard.PreferredWidth)   / 2, lblBrandCard.Bottom + 2);

            int yLbl1 = lblSubCard.Bottom + 28;
            lblUserTag.Location  = new Point(x, yLbl1);
            int yBox1 = yLbl1 + lblUserTag.PreferredHeight + 6;
            pnlUserFake.Location = new Point(x, yBox1);
            pnlUserFake.Size     = new Size(inputW, InputH);
            txtUsername.Location = new Point(12, (InputH - txtUsername.Height) / 2);
            txtUsername.Width    = inputW - 24;

            int yLbl2 = yBox1 + InputH + 16;
            lblPassTag.Location  = new Point(x, yLbl2);
            int yBox2 = yLbl2 + lblPassTag.PreferredHeight + 6;
            pnlPassFake.Location = new Point(x, yBox2);
            pnlPassFake.Size     = new Size(inputW, InputH);
            txtPassword.Location = new Point(12, (InputH - txtPassword.Height) / 2);
            txtPassword.Width    = inputW - 24;

            int yLbl3 = yBox2 + InputH + 16;
            lblRoleTag.Location  = new Point(x, yLbl3);
            int yBox3 = yLbl3 + lblRoleTag.PreferredHeight + 6;
            pnlRoleFake.Location = new Point(x, yBox3);
            pnlRoleFake.Size     = new Size(inputW, InputH);
            cmbRole.Location     = new Point(10, (InputH - cmbRole.Height) / 2);
            cmbRole.Width        = inputW - 20;

            int yDiv = yBox3 + InputH + 22;
            pnlDivider.Location = new Point(x, yDiv);
            pnlDivider.Size     = new Size(inputW, 1);

            int yBtn = yDiv + 18;
            btnLogin.Location = new Point(x, yBtn);
            btnLogin.Size     = new Size(inputW, InputH);

            lblBack.Location = new Point((CardW - lblBack.PreferredWidth) / 2, btnLogin.Bottom + 18);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Paint helpers  (unchanged from original)
        // ─────────────────────────────────────────────────────────────────

        private void CmbRole_DrawItem(object? sender, DrawItemEventArgs de)
        {
            if (de.Index < 0) return;
            bool sel = (de.State & DrawItemState.Selected) != 0;
            bool hot = (de.State & DrawItemState.HotLight) != 0;
            Color bg = (sel || hot) ? Color.FromArgb(40, 212, 160, 23) : Color.FromArgb(13, 40, 24);
            de.Graphics.FillRectangle(new SolidBrush(bg), de.Bounds);
            TextRenderer.DrawText(de.Graphics, cmbRole.Items[de.Index]?.ToString(),
                new Font("Segoe UI", 10),
                new Rectangle(de.Bounds.X + 10, de.Bounds.Y, de.Bounds.Width - 10, de.Bounds.Height),
                Color.FromArgb(248, 244, 239),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void PnlCard_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = pnlCard!.ClientRectangle;
            rc.Inflate(-1, -1);
            using var path = BuildRoundPath(rc, 18);
            using var bg   = new SolidBrush(Color.FromArgb(230, 13, 40, 24));
            g.FillPath(bg, path);
            using var pen = new Pen(Color.FromArgb(40, 212, 160, 23), 1.5f);
            g.DrawPath(pen, path);
        }

        private void BtnLogin_Paint(object? sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = btnLogin.ClientRectangle;
            rc.Inflate(-1, -1);
            using var path = BuildRoundPath(rc, 10);
            using var fill = new LinearGradientBrush(rc,
                Color.FromArgb(212, 160, 23), Color.FromArgb(180, 130, 10), 90f);
            g.FillPath(fill, path);
            TextRenderer.DrawText(g, "Sign In",
                new Font("Georgia", 12, FontStyle.Bold),
                btnLogin.ClientRectangle,
                Color.FromArgb(10, 32, 18),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static void StyleTag(Label lbl, string text)
        {
            lbl.Text      = text;
            lbl.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(140, 248, 244, 239);
            lbl.BackColor = Color.Transparent;
            lbl.AutoSize  = true;
        }

        private static void StyleInput(TextBox txt)
        {
            txt.BorderStyle = BorderStyle.None;
            txt.BackColor   = Color.FromArgb(20, 46, 28);
            txt.ForeColor   = Color.FromArgb(248, 244, 239);
            txt.Font        = new Font("Segoe UI", 11);
        }

        private static Panel WrapInput(TextBox txt)
        {
            var fc   = Color.FromArgb(20, 46, 28);
            var fake = new Panel { BackColor = fc };
            fake.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = fake.ClientRectangle; r.Inflate(-1, -1);
                using var path = BuildRoundPath(r, 8);
                using var fill = new SolidBrush(fc);
                pe.Graphics.FillPath(fill, path);
                bool f     = txt.Focused;
                Color bc   = f ? Color.FromArgb(212, 160, 23) : Color.FromArgb(60, 255, 255, 255);
                float bw   = f ? 1.8f : 1f;
                using var p2 = BuildRoundPath(r, 8);
                using var pen = new Pen(bc, bw);
                pe.Graphics.DrawPath(pen, p2);
            };
            fake.Resize += (s, ev) =>
            {
                if (fake.Width > 0 && fake.Height > 0)
                    fake.Region = System.Drawing.Region.FromHrgn(
                        CreateRoundRectRgn(0, 0, fake.Width, fake.Height, 10, 10));
            };
            txt.Enter += (s, e) => fake.Invalidate();
            txt.Leave += (s, e) => fake.Invalidate();
            fake.Controls.Add(txt);
            return fake;
        }

        private static Panel WrapCombo(ComboBox cmb)
        {
            var fc   = Color.FromArgb(20, 46, 28);
            var fake = new Panel { BackColor = fc };
            fake.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = fake.ClientRectangle; r.Inflate(-1, -1);
                using var path = BuildRoundPath(r, 8);
                using var fill = new SolidBrush(fc);
                pe.Graphics.FillPath(fill, path);
                bool f   = cmb.Focused || cmb.DroppedDown;
                Color bc = f ? Color.FromArgb(212, 160, 23) : Color.FromArgb(60, 255, 255, 255);
                float bw = f ? 1.8f : 1f;
                using var p2 = BuildRoundPath(r, 8);
                using var pen = new Pen(bc, bw);
                pe.Graphics.DrawPath(pen, p2);
            };
            fake.Resize += (s, ev) =>
            {
                if (fake.Width > 0 && fake.Height > 0)
                    fake.Region = System.Drawing.Region.FromHrgn(
                        CreateRoundRectRgn(0, 0, fake.Width, fake.Height, 10, 10));
            };
            cmb.FlatStyle     = FlatStyle.Flat;
            cmb.BackColor     = fc;
            cmb.ForeColor     = Color.FromArgb(248, 244, 239);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Enter        += (s, e) => fake.Invalidate();
            cmb.Leave        += (s, e) => fake.Invalidate();
            cmb.DropDown     += (s, e) => fake.Invalidate();
            cmb.DropDownClosed += (s, e) => fake.Invalidate();
            fake.Controls.Add(cmb);
            return fake;
        }

        private static GraphicsPath BuildRoundPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X,            r.Y,             radius, radius, 180, 90);
            path.AddArc(r.Right-radius, r.Y,             radius, radius, 270, 90);
            path.AddArc(r.Right-radius, r.Bottom-radius, radius, radius,   0, 90);
            path.AddArc(r.X,            r.Bottom-radius, radius, radius,  90, 90);
            path.CloseFigure();
            return path;
        }

        private void LblBack_Click(object? sender, EventArgs e)
        {
            foreach (Form f in Application.OpenForms)
                if (f is HomePage) { f.Show(); break; }
            Close();
        }
    }
}
