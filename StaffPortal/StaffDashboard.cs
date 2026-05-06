using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Project.UcAdministrator;
using Project.UcManager;
using Project.UcReception;
using Project.UcTourGuide;
using Project.UcZooKeeper;

namespace Project
{
    public partial class StaffDashboard : Form
    {
        private string _role;
        private string _displayName;
        private string _username;
        private Label? _lblBrand;
        private Label? _lblContext;

        private static readonly Color Gold = Color.FromArgb(212, 160, 23);
        private static readonly Color DarkGreen = Color.FromArgb(7, 26, 14);
        private static readonly Color DimWhite = Color.FromArgb(60, 248, 244, 239);

        public StaffDashboard(string role, string displayName, string username)
        {
            InitializeComponent();
            _role = CanonicalRole(role);
            _displayName = NormalizeDisplayName(_role, displayName);
            _username = string.IsNullOrWhiteSpace(username) ? NormalizeRole(displayName) : username.Trim();
            Load += StaffDashboard_Load;
        }

        private void StaffDashboard_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;

            pnlTopBar.BackColor = DarkGreen;
            pnlSidebar.BackColor = DarkGreen;
            pnlContent.BackColor = Color.FromArgb(240, 237, 232);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, pnlTopBar.Bottom);
            pnlContent.Padding = Padding.Empty;
            pnlContent.Margin = Padding.Empty;

            pnlTopBar.Paint += (s, pe) =>
            {
                using var gradient = new LinearGradientBrush(
                    new Rectangle(0, 0, pnlTopBar.Width, pnlTopBar.Height),
                    Color.FromArgb(10, 32, 18),
                    DarkGreen,
                    90f);
                using var line = new Pen(Color.FromArgb(38, 212, 160, 23), 2);
                pe.Graphics.FillRectangle(gradient, pnlTopBar.ClientRectangle);
                pe.Graphics.DrawLine(line, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };

            pnlSidebar.Visible = false;
            pnlSidebar.Width = 0;

            StaffPortalDb.EnsureWildlifeSeed();
            StaffPortalDb.EnsureTourGuideSchedules();
            BuildHeaderChrome();
            LockRoleTabs();
            LoadRoleDashboard();
        }

        private void BuildHeaderChrome()
        {
            _lblBrand ??= new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 244, 239),
                Text = "WILDNEST"
            };

            _lblContext ??= new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Gold,
                Text = $"{_role.ToUpper()} PORTAL - {_displayName}"
            };

            if (!pnlTopBar.Controls.Contains(_lblBrand))
            {
                pnlTopBar.Controls.Add(_lblBrand);
            }

            if (!pnlTopBar.Controls.Contains(_lblContext))
            {
                pnlTopBar.Controls.Add(_lblContext);
            }

            _lblBrand.Location = new Point(22, 18);
            _lblContext.Location = new Point(24, 48);

            int x = 330;
            int gap = 12;
            Button[] tabs = { btnRoleAdmin, btnRoleFD, btnRoleZK, btnRoleTG };
            foreach (var btn in tabs)
            {
                btn.Location = new Point(x, 18);
                btn.Size = new Size(150, 42);
                btn.BackColor = Color.Transparent;
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                x += btn.Width + gap;
            }
        }

        private void LockRoleTabs()
        {
            btnRoleAdmin.Tag = "Manager";
            btnRoleAdmin.Text = "Manager";
            btnRoleFD.Tag = "Reception";
            btnRoleZK.Tag = "ZooKeeper";
            btnRoleTG.Tag = "TourGuide";

            foreach (Control c in pnlTopBar.Controls)
            {
                if (c is not Button btn)
                {
                    continue;
                }

                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;

                if (btn.Tag?.ToString() == _role)
                {
                    btn.ForeColor = Gold;
                    btn.Font = new Font("Georgia", 10.2f, FontStyle.Bold);
                    btn.Cursor = Cursors.Default;
                    btn.Enabled = true;
                    btn.Paint -= ActiveRoleButtonPaint;
                    btn.Paint += ActiveRoleButtonPaint;
                }
                else
                {
                    btn.ForeColor = DimWhite;
                    btn.Enabled = false;
                    btn.Cursor = Cursors.No;
                    btn.Paint -= ActiveRoleButtonPaint;
                }
            }
        }

        private void ActiveRoleButtonPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = WildNestUI.RoundRect(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 18);
            using var fill = new SolidBrush(Color.FromArgb(24, 212, 160, 23));
            using var pen = new Pen(Color.FromArgb(70, 212, 160, 23), 1f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
            TextRenderer.DrawText(
                e.Graphics,
                btn.Text,
                btn.Font,
                btn.ClientRectangle,
                btn.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void LoadRoleDashboard()
        {
            pnlContent.Controls.Clear();
            pnlSidebar.Controls.Clear();

            UserControl? uc = null;
            switch (_role)
            {
                case "Manager":
                    var m = new UcManagerDashboard();
                    m.SetParentDashboard(this);
                    m.SetDisplayName(_displayName);
                    m.SetUsername(_username);
                    uc = m;
                    break;

                case "Reception":
                    var r = new UcReceptionDashboard();
                    r.SetParentDashboard(this);
                    r.SetDisplayName(_displayName);
                    r.SetUsername(_username);
                    uc = r;
                    break;

                case "TourGuide":
                    var t = new UcTourGuideDashboard();
                    t.SetParentDashboard(this);
                    t.SetDisplayName(_displayName);
                    t.SetUsername(_username);
                    uc = t;
                    break;

                case "ZooKeeper":
                    var z = new UcZookeeperDashboard();
                    z.SetParentDashboard(this);
                    z.SetDisplayName(_displayName);
                    z.SetUsername(_username);
                    uc = z;
                    break;
            }

            if (uc == null)
            {
                return;
            }

            uc.Dock = DockStyle.Fill;
            uc.Margin = Padding.Empty;
            uc.Padding = Padding.Empty;
            pnlContent.Controls.Add(uc);
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
            var sb = new System.Text.StringBuilder();
            foreach (char c in role)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        private static string NormalizeDisplayName(string role, string displayName)
        {
            string cleaned = (displayName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleaned))
                return role;

            if (role == "Manager")
            {
                string normalized = NormalizeRole(cleaned);
                if (normalized is "admin" or "administrator" or "adminuser")
                    return "Resort Manager";
            }

            return cleaned;
        }

        public void SignOut()
        {
            StaffLogin? login = null;
            var dashboardsToClose = new System.Collections.Generic.List<StaffDashboard>();

            foreach (Form f in Application.OpenForms)
            {
                if (f is StaffLogin existingLogin)
                {
                    login = existingLogin;
                }
                else if (f is StaffDashboard dashboard && dashboard != this)
                {
                    dashboardsToClose.Add(dashboard);
                }
            }

            login ??= new StaffLogin();
            login.Show();
            login.WindowState = FormWindowState.Maximized;
            login.Activate();
            login.BringToFront();

            foreach (var dashboard in dashboardsToClose)
            {
                dashboard.Close();
            }

            BeginInvoke((Action)Close);
        }
    }
}
