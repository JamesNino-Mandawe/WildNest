using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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

        private const int CardW = 420;
        private const int CardH = 680;
        private const int InputH = 46;
        private const int Pad = 36;

        private Panel? pnlCard;
        private Panel? pnlUserFake;
        private Panel? pnlPassFake;
        private Panel? pnlRoleFake;
        private Panel? pnlDivider;
        private Label? lblBack;

        // ── Hardcoded accounts ──────────────────────────────────────────────
        private static readonly (string user, string pass, string role, string display)[] Accounts =
        {
            ("manager","manager123","Manager",             "Resort Manager"),
            ("admin",  "admin123",  "Manager",             "Resort Manager"),
            ("maria",  "maria123",  "Reception",           "Maria Santos"),
            ("santos", "santos123", "Reception",           "Santos Rivera"),
            ("jose",   "jose123",   "ZooKeeper",           "Jose Reyes"),
            ("reyes",  "reyes123",  "ZooKeeper",           "Reyes Dela Cruz"),
            ("ana",    "ana123",    "TourGuide",           "Ana Cruz"),
            ("cruz",   "cruz123",   "TourGuide",           "Cruz Gomez"),
        };

        public StaffLogin()
        {
            InitializeComponent();
            BuildLogin();
        }

        private void BuildLogin()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(5, 18, 10);
            this.DoubleBuffered = true;

            this.Opacity = 0;
            var fade = new System.Windows.Forms.Timer { Interval = 10 };
            fade.Tick += (s, e) =>
            {
                if (this.Opacity < 1) this.Opacity += 0.05;
                else fade.Stop();
            };
            fade.Start();

            pnlCard = new Panel { Size = new Size(CardW, CardH), BackColor = Color.Transparent };
            pnlCard.Paint += PnlCard_Paint;

            picCardLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picCardLogo.Size = new Size(80, 80);
            picCardLogo.BackColor = Color.Transparent;
            picCardLogo.Image = AppAssetLoader.LoadImage("Logo", "Resources", "Logo.png");

            lblBrandCard.Text = "WILDNEST";
            lblBrandCard.Font = new Font("Georgia", 22, FontStyle.Bold);
            lblBrandCard.ForeColor = Color.FromArgb(248, 244, 239);
            lblBrandCard.BackColor = Color.Transparent;
            lblBrandCard.AutoSize = true;

            StyleTag(lblSubCard, "STAFF PORTAL  ·  RESORT MANAGEMENT");
            lblSubCard.BackColor = Color.Transparent;
            lblSubCard.AutoSize = true;

            StyleTag(lblUserTag, "Employee ID / Username");
            StyleTag(lblPassTag, "Password");
            StyleTag(lblRoleTag, "Role");

            StyleInput(txtUsername);
            pnlUserFake = WrapInput(txtUsername);

            StyleInput(txtPassword);
            txtPassword.PasswordChar = '●';
            pnlPassFake = WrapInput(txtPassword);

            cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRole.FlatStyle = FlatStyle.Flat;
            cmbRole.BackColor = Color.FromArgb(20, 46, 28);
            cmbRole.ForeColor = Color.FromArgb(248, 244, 239);
            cmbRole.Font = new Font("Segoe UI", 11);
            cmbRole.DrawMode = DrawMode.OwnerDrawFixed;
            cmbRole.ItemHeight = 26;
            cmbRole.DrawItem += CmbRole_DrawItem;
            cmbRole.Items.Clear();
            cmbRole.Items.AddRange(new string[] {
                 "Manager", "Reception", "ZooKeeper", "TourGuide"
                });
            cmbRole.SelectedIndex = 0;
            pnlRoleFake = WrapCombo(cmbRole);

            pnlDivider = new Panel { Height = 1, BackColor = Color.FromArgb(50, 212, 160, 23) };

            btnLogin.Text = "";
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.BackColor = Color.Transparent;
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Height = 46;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Paint += BtnLogin_Paint;
            btnLogin.Click += BtnLogin_Click;

            lblBack = new Label
            {
                Text = "Back to WildNest Homepage",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(90, 248, 244, 239),
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblBack.MouseEnter += (s, e) => lblBack.ForeColor = Color.FromArgb(210, 248, 244, 239);
            lblBack.MouseLeave += (s, e) => lblBack.ForeColor = Color.FromArgb(90, 248, 244, 239);
            lblBack.Click += LblBack_Click;

            pnlCard.Controls.AddRange(new Control[]
            {
                picCardLogo, lblBrandCard, lblSubCard,
                lblUserTag,  pnlUserFake,
                lblPassTag,  pnlPassFake,
                lblRoleTag,  pnlRoleFake,
                pnlDivider,  btnLogin,    lblBack
            });

            this.Controls.Clear();
            this.Controls.Add(pnlCard);

            this.Load += (s, e) => LayoutCard();
            this.Resize += (s, e) => LayoutCard();
        }

        private void CmbRole_DrawItem(object? sender, DrawItemEventArgs de)
        {
            if (de.Index < 0) return;
            bool isSelected = (de.State & DrawItemState.Selected) != 0;
            bool isHovered = (de.State & DrawItemState.HotLight) != 0;
            Color bg = (isSelected || isHovered)
                ? Color.FromArgb(40, 212, 160, 23)
                : Color.FromArgb(13, 40, 24);
            de.Graphics.FillRectangle(new SolidBrush(bg), de.Bounds);
            TextRenderer.DrawText(
                de.Graphics,
                cmbRole.Items[de.Index].ToString(),
                new Font("Segoe UI", 10),
                new Rectangle(de.Bounds.X + 10, de.Bounds.Y, de.Bounds.Width - 10, de.Bounds.Height),
                Color.FromArgb(248, 244, 239),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        private void LayoutCard()
        {
            if (pnlCard == null || pnlUserFake == null || pnlPassFake == null || pnlRoleFake == null || pnlDivider == null || lblBack == null)
                return;

            pnlCard.Location = new Point(
                (this.ClientSize.Width - CardW) / 2,
                (this.ClientSize.Height - CardH) / 2);

            int inputW = CardW - Pad * 2;
            int x = Pad;

            picCardLogo.Location = new Point((CardW - picCardLogo.Width) / 2, 18);
            lblBrandCard.Location = new Point((CardW - lblBrandCard.PreferredWidth) / 2, picCardLogo.Bottom + 8);
            lblSubCard.Location = new Point((CardW - lblSubCard.PreferredWidth) / 2, lblBrandCard.Bottom + 2);

            int yLbl1 = lblSubCard.Bottom + 28;
            lblUserTag.Location = new Point(x, yLbl1);
            int yBox1 = yLbl1 + lblUserTag.PreferredHeight + 6;
            pnlUserFake.Location = new Point(x, yBox1);
            pnlUserFake.Size = new Size(inputW, InputH);
            txtUsername.Location = new Point(12, (InputH - txtUsername.Height) / 2);
            txtUsername.Width = inputW - 24;

            int yLbl2 = yBox1 + InputH + 16;
            lblPassTag.Location = new Point(x, yLbl2);
            int yBox2 = yLbl2 + lblPassTag.PreferredHeight + 6;
            pnlPassFake.Location = new Point(x, yBox2);
            pnlPassFake.Size = new Size(inputW, InputH);
            txtPassword.Location = new Point(12, (InputH - txtPassword.Height) / 2);
            txtPassword.Width = inputW - 24;

            int yLbl3 = yBox2 + InputH + 16;
            lblRoleTag.Location = new Point(x, yLbl3);
            int yBox3 = yLbl3 + lblRoleTag.PreferredHeight + 6;
            pnlRoleFake.Location = new Point(x, yBox3);
            pnlRoleFake.Size = new Size(inputW, InputH);
            cmbRole.Location = new Point(2, (InputH - cmbRole.Height) / 2);
            cmbRole.Width = inputW - 4;

            int yDiv = yBox3 + InputH + 22;
            pnlDivider.Location = new Point(x, yDiv);
            pnlDivider.Width = inputW;

            btnLogin.Location = new Point(x, yDiv + 16);
            btnLogin.Width = inputW;

            lblBack.Location = new Point(
                (CardW - lblBack.PreferredWidth) / 2,
                btnLogin.Bottom + 14);

            pnlCard.Invalidate();
        }

        private void PnlCard_Paint(object? sender, PaintEventArgs e)
        {
            if (pnlCard == null) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlCard.ClientRectangle; r.Inflate(-1, -1);
            using (var path = RoundedPath(r, 20))
            using (var fill = new SolidBrush(Color.FromArgb(13, 40, 24)))
                g.FillPath(fill, path);
            using (var glow = new LinearGradientBrush(
                new Rectangle(0, 0, pnlCard.Width, pnlCard.Height / 2),
                Color.FromArgb(26, 212, 160, 23),
                Color.Transparent,
                90f))
                g.FillRectangle(glow, 0, 0, pnlCard.Width, pnlCard.Height / 2);
            using (var path = RoundedPath(r, 20))
            using (var pen = new Pen(Color.FromArgb(80, 212, 160, 23), 1.2f))
                g.DrawPath(pen, path);
        }

        private void BtnLogin_Paint(object? sender, PaintEventArgs pe)
        {
            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pe.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = btnLogin.ClientRectangle; r.Inflate(-1, -1);
            using (var path = RoundedPath(r, 10))
            using (var fill = new SolidBrush(Color.FromArgb(212, 160, 23)))
                pe.Graphics.FillPath(fill, path);
            TextRenderer.DrawText(pe.Graphics, "Sign In to Portal",
                new Font("Segoe UI", 11, FontStyle.Bold),
                btnLogin.ClientRectangle,
                Color.FromArgb(26, 46, 10),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static void StyleTag(Label lbl, string text)
        {
            lbl.Text = text.ToUpper();
            lbl.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(200, 212, 160, 23);
            lbl.BackColor = Color.Transparent;
            lbl.AutoSize = true;
        }

        private static void StyleInput(TextBox txt)
        {
            txt.BackColor = Color.FromArgb(20, 46, 28);
            txt.ForeColor = Color.FromArgb(248, 244, 239);
            txt.BorderStyle = BorderStyle.None;
            txt.Font = new Font("Segoe UI", 11);
        }

        private void AttachFocusGlow(TextBox txt, Panel wrapper)
        {
            txt.Enter += (s, e) => wrapper.Invalidate();
            txt.Leave += (s, e) => wrapper.Invalidate();
            wrapper.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = wrapper.ClientRectangle; r.Inflate(-1, -1);
                using (var path = RoundedPath(r, 8))
                using (var fill = new SolidBrush(Color.FromArgb(20, 46, 28)))
                    pe.Graphics.FillPath(fill, path);
                bool focused = txt.Focused;
                Color borderColor = focused ? Color.FromArgb(212, 160, 23) : Color.FromArgb(31, 255, 255, 255);
                float borderWidth = focused ? 1.8f : 1f;
                using (var path = RoundedPath(r, 8))
                using (var pen = new Pen(borderColor, borderWidth))
                    pe.Graphics.DrawPath(pen, path);
            };
        }

        private Panel WrapInput(TextBox txt)
        {
            var fake = new Panel { BackColor = Color.FromArgb(20, 46, 28) };
            fake.Resize += (s, ev) =>
            {
                if (fake.Width > 0 && fake.Height > 0)
                    fake.Region = System.Drawing.Region.FromHrgn(
                        CreateRoundRectRgn(0, 0, fake.Width, fake.Height, 10, 10));
            };
            fake.Controls.Add(txt);
            AttachFocusGlow(txt, fake);
            return fake;
        }

        private Panel WrapCombo(ComboBox cmb)
        {
            var fillColor = Color.FromArgb(20, 46, 28);
            var fake = new Panel { BackColor = fillColor };
            fake.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var r = fake.ClientRectangle; r.Inflate(-1, -1);
                using (var path = RoundedPath(r, 8))
                using (var fill = new SolidBrush(fillColor))
                    pe.Graphics.FillPath(fill, path);
                bool focused = cmb.Focused || cmb.DroppedDown;
                Color borderColor = focused ? Color.FromArgb(212, 160, 23) : Color.FromArgb(60, 255, 255, 255);
                float borderWidth = focused ? 1.8f : 1f;
                using (var path = RoundedPath(r, 8))
                using (var pen = new Pen(borderColor, borderWidth))
                    pe.Graphics.DrawPath(pen, path);
            };
            fake.Resize += (s, ev) =>
            {
                if (fake.Width > 0 && fake.Height > 0)
                    fake.Region = System.Drawing.Region.FromHrgn(
                        CreateRoundRectRgn(0, 0, fake.Width, fake.Height, 10, 10));
            };
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = fillColor;
            cmb.ForeColor = Color.FromArgb(248, 244, 239);
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Enter += (s, e) => fake.Invalidate();
            cmb.Leave += (s, e) => fake.Invalidate();
            cmb.DropDown += (s, e) => fake.Invalidate();
            cmb.DropDownClosed += (s, e) => fake.Invalidate();
            fake.Controls.Add(cmb);
            return fake;
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void LblBack_Click(object? sender, EventArgs e)
        {
            foreach (Form f in Application.OpenForms)
                if (f is HomePage) { f.Show(); break; }
            this.Close();
        }

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

            string connStr = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;";
            string? dbError = null;

            try
            {
                using var conn = new MySqlConnection(connStr);
                conn.Open();

                string query = @"
SELECT FullName, Username, PasswordHash, Role
FROM tbl_users
WHERE LOWER(Username) = LOWER(@u)
  AND IsActive = 1;";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", user);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string storedRole = CanonicalRole(reader["Role"]?.ToString() ?? "");
                    string storedPassword = reader["PasswordHash"]?.ToString() ?? "";

                    if (storedRole != role || !VerifyPassword(pass, storedPassword))
                    {
                        continue;
                    }

                    string displayName = reader["FullName"]?.ToString() ?? role;

                    string username = reader["Username"]?.ToString() ?? user;
                    var dashboard = new StaffDashboard(role, displayName, username);
                    WireDashboardLifetime(dashboard);
                    dashboard.Show();
                    this.Hide();
                    return;
                }
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
                ProjectDiagnostics.LogError("StaffLogin", ex, "Database authentication");
            }

            if (TryDemoLogin(user, pass, role, out string demoDisplayName))
            {
                var dashboard = new StaffDashboard(role, demoDisplayName, user);
                WireDashboardLifetime(dashboard);
                dashboard.Show();
                this.Hide();
                return;
            }

            string suffix = string.IsNullOrWhiteSpace(dbError)
                ? ""
                : "\n\nDatabase detail: " + dbError;
            ShowError("Invalid credentials, inactive account, or role mismatch. Please check the username, password, and selected role." + suffix);
        }

        private void ShowError(string msg)
        {
            MessageBox.Show(msg, "WildNest Staff Portal",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static bool TryDemoLogin(string user, string pass, string role, out string displayName)
        {
            foreach (var account in Accounts)
            {
                if (string.Equals(account.user, user, StringComparison.OrdinalIgnoreCase) &&
                    account.pass == pass &&
                    CanonicalRole(account.role) == role)
                {
                    displayName = account.display;
                    return true;
                }
            }

            displayName = "";
            return false;
        }

        private static string CanonicalRole(string role)
        {
            string normalized = NormalizeRole(role);
            return normalized switch
            {
                "manager" or "administrator" or "admin" => "Manager",
                "reception" or "frontdesk" => "Reception",
                "zookeeper" or "keeper" => "ZooKeeper",
                "tourguide" or "guide" => "TourGuide",
                _ => role.Trim()
            };
        }

        private static string NormalizeRole(string role)
        {
            var sb = new StringBuilder();
            foreach (char c in role)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        private static bool VerifyPassword(string typedPassword, string storedPassword)
        {
            string stored = (storedPassword ?? "").Trim();
            if (stored.Length == 0)
            {
                return false;
            }

            if (stored == typedPassword)
            {
                return true;
            }

            string sha256 = ComputeSha256Hex(typedPassword);
            return stored.Equals(sha256, StringComparison.OrdinalIgnoreCase) ||
                   stored.Equals("sha256:" + sha256, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256Hex(string value)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private void WireDashboardLifetime(StaffDashboard dashboard)
        {
            dashboard.Owner = this;
            dashboard.FormClosed += (s, e) =>
            {
                if (IsDisposed)
                    return;

                if (Visible)
                    return;

                foreach (Form form in Application.OpenForms)
                {
                    if (form is HomePage home && !home.IsDisposed)
                    {
                        home.Show();
                        home.WindowState = FormWindowState.Maximized;
                        home.Activate();
                        break;
                    }
                }

                Close();
            };
        }

        // stub — designer wired this, keep it empty
       
    }
}
