// ============================================================
//  StaffDashboard.cs
//  CHANGE SUMMARY
//  • using Project.UcAdministrator  →  using Project.UcManager
//  • case "Administrator" in switch  →  case "Manager"
//  • btnRoleAdmin.Tag = "Manager" (was "Administrator")
//  • Top-bar context label uses friendly role text via RoleLabel()
//  • All visual chrome, layout, SignOut() — unchanged
// ============================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Project.UcManager;        // ← was Project.UcAdministrator
using Project.UcReception;
using Project.UcTourGuide;
using Project.UcZooKeeper;

namespace Project
{
    public partial class StaffDashboard : Form
    {
        private string _role;
        private string _displayName;
        private Label? _lblBrand;
        private Label? _lblContext;

        private static readonly Color Gold      = Color.FromArgb(212, 160, 23);
        private static readonly Color DarkGreen = Color.FromArgb(7, 26, 14);
        private static readonly Color DimWhite  = Color.FromArgb(60, 248, 244, 239);

        public StaffDashboard(string role, string displayName)
        {
            InitializeComponent();
            _role        = role;
            _displayName = displayName;
            Load        += StaffDashboard_Load;
        }

        private void StaffDashboard_Load(object sender, EventArgs e)
        {
            WindowState     = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;

            pnlTopBar.BackColor  = DarkGreen;
            pnlSidebar.BackColor = DarkGreen;
            pnlContent.BackColor = Color.FromArgb(240, 237, 232);
            pnlContent.Dock      = DockStyle.Fill;
            pnlContent.Location  = new Point(0, pnlTopBar.Bottom);
            pnlContent.Padding   = Padding.Empty;
            pnlContent.Margin    = Padding.Empty;

            pnlTopBar.Paint += (s, pe) =>
            {
                using var gradient = new LinearGradientBrush(
                    new Rectangle(0, 0, pnlTopBar.Width, pnlTopBar.Height),
                    Color.FromArgb(10, 32, 18), DarkGreen, 90f);
                using var line = new Pen(Color.FromArgb(38, 212, 160, 23), 2);
                pe.Graphics.FillRectangle(gradient, pnlTopBar.ClientRectangle);
                pe.Graphics.DrawLine(line, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1);
            };

            pnlSidebar.Visible = false;
            pnlSidebar.Width   = 0;

            StaffPortalDb.EnsureWildlifeSeed();
            StaffPortalDb.EnsureTourGuideSchedules();
            BuildHeaderChrome();
            LockRoleTabs();
            LoadRoleDashboard();
        }

        // ── Header ────────────────────────────────────────────────────────

        private void BuildHeaderChrome()
        {
            _lblBrand ??= new Label
            {
                AutoSize  = true,
                BackColor = Color.Transparent,
                Font      = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 244, 239),
                Text      = "WILDNEST"
            };

            _lblContext ??= new Label
            {
                AutoSize  = true,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Gold,
                // e.g. "MANAGER PORTAL  ·  Resort Manager"
                Text      = $"{RoleLabel(_role).ToUpper()} PORTAL  ·  {_displayName}"
            };

            if (!pnlTopBar.Controls.Contains(_lblBrand))
                pnlTopBar.Controls.Add(_lblBrand);
            if (!pnlTopBar.Controls.Contains(_lblContext))
                pnlTopBar.Controls.Add(_lblContext);

            _lblBrand.Location   = new Point(22, 18);
            _lblContext.Location = new Point(24, 48);

            int x = 330, gap = 12;
            foreach (Button btn in new[] { btnRoleAdmin, btnRoleFD, btnRoleZK, btnRoleTG })
            {
                btn.Location = new Point(x, 18);
                btn.Size     = new Size(150, 42);
                btn.BackColor = Color.Transparent;
                btn.FlatAppearance.BorderSize         = 0;
                btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                x += btn.Width + gap;
            }
        }

        private static string RoleLabel(string role) => role switch
        {
            "Manager"   => "Manager",
            "Reception" => "Reception",
            "ZooKeeper" => "ZooKeeper",
            "TourGuide" => "Tour Guide",
            _           => role
        };

        // ── Tab locking ───────────────────────────────────────────────────

        private void LockRoleTabs()
        {
            // btnRoleAdmin now represents Manager (not Administrator)
            btnRoleAdmin.Tag  = "Manager";
            btnRoleAdmin.Text = "Manager";
            btnRoleFD.Tag     = "Reception";
            btnRoleFD.Text    = "Reception";
            btnRoleZK.Tag     = "ZooKeeper";
            btnRoleZK.Text    = "ZooKeeper";
            btnRoleTG.Tag     = "TourGuide";
            btnRoleTG.Text    = "Tour Guide";

            foreach (Control c in pnlTopBar.Controls)
            {
                if (c is not Button btn) continue;

                btn.FlatStyle               = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor               = Color.Transparent;

                if (btn.Tag?.ToString() == _role)
                {
                    btn.ForeColor = Gold;
                    btn.Font      = new Font("Georgia", 10.2f, FontStyle.Bold);
                    btn.Cursor    = Cursors.Default;
                    btn.Enabled   = true;
                    btn.Paint    -= ActiveTabPaint;
                    btn.Paint    += ActiveTabPaint;
                }
                else
                {
                    btn.ForeColor = DimWhite;
                    btn.Enabled   = false;
                    btn.Cursor    = Cursors.No;
                    btn.Paint    -= ActiveTabPaint;
                }
            }
        }

        private void ActiveTabPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = WildNestUI.RoundRect(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 18);
            using var fill = new SolidBrush(Color.FromArgb(24, 212, 160, 23));
            using var pen  = new Pen(Color.FromArgb(70, 212, 160, 23), 1f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(pen, path);
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle,
                btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ── Role routing ──────────────────────────────────────────────────

        private void LoadRoleDashboard()
        {
            pnlContent.Controls.Clear();
            pnlSidebar.Controls.Clear();

            UserControl? uc = _role switch
            {
                "Manager" => CreateManager(),       // ← was "Administrator"
                "Reception" => CreateReception(),
                "TourGuide" => CreateTourGuide(),
                "ZooKeeper" => CreateZooKeeper(),
                _ => null
            };

            if (uc == null) return;
            uc.Dock    = DockStyle.Fill;
            uc.Margin  = Padding.Empty;
            uc.Padding = Padding.Empty;
            pnlContent.Controls.Add(uc);
        }

        private UserControl CreateManager()
        {
            var m = new UcManagerDashboard();   // ← new class
            m.SetParentDashboard(this);
            m.SetDisplayName(_displayName);
            return m;
        }
        private UserControl CreateReception()
        {
            var r = new UcReceptionDashboard();
            r.SetParentDashboard(this);
            r.SetDisplayName(_displayName);
            return r;
        }
        private UserControl CreateTourGuide()
        {
            var t = new UcTourGuideDashboard();
            t.SetParentDashboard(this);
            t.SetDisplayName(_displayName);
            return t;
        }
        private UserControl CreateZooKeeper()
        {
            var z = new UcZookeeperDashboard();
            z.SetParentDashboard(this);
            z.SetDisplayName(_displayName);
            return z;
        }

        // ── Sign-out (unchanged) ──────────────────────────────────────────

        public void SignOut()
        {
            StaffLogin? login = null;
            var toClose = new System.Collections.Generic.List<StaffDashboard>();

            foreach (Form f in Application.OpenForms)
            {
                if (f is StaffLogin l) login = l;
                else if (f is StaffDashboard d && d != this) toClose.Add(d);
            }

            login ??= new StaffLogin();
            login.Show();
            login.WindowState = FormWindowState.Maximized;
            login.Activate();
            login.BringToFront();

            foreach (var d in toClose) d.Close();
            BeginInvoke((Action)Close);
        }
    }
}
