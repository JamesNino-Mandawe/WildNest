// ============================================================
//  UcManagerDashboard.cs   (namespace Project.UcManager)
//  REPLACES: UcAdminDashboard.cs  (namespace Project.UcAdministrator)
//
//  CHANGE SUMMARY vs UcAdminDashboard
//  • Namespace + class renamed
//  • lblSideRole shows "Resort Manager"
//  • btnNavUsers   → loads UcManagerUsers   (new, with account creation)
//  • btnNavBills   → loads UcManagerBills   (same data, new namespace)
//  • btnNavSalesReports added → loads UcManagerSalesReports (new)
//  • StaffChat wires "Manager" role string instead of "Administrator"
//  • pnlAdminSidebar/pnlAdminContent renamed to pnlManagerSidebar/pnlManagerContent
//  • Everything else (WireNavButtons, Navigate, LoadPage) identical
// ============================================================

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcManager
{
    public partial class UcManagerDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private Button?         _activeBtn;
        private Button          _btnNavStaffChat = null!;
        private string          _displayName = "Resort Manager";

        public UcManagerDashboard()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName     = name;
            lblSideUser.Text = name;
            lblSideRole.Text = "Resort Manager";
        }

        // ── Startup ───────────────────────────────────────────────────────

        private void OnLoad(object? sender, EventArgs e)
        {
            BuildSidebar();
            WireNavButtons();
            WildNestUI.SetActive(ref _activeBtn, btnNavDashboard);
            LoadPage(new UcManagerDashboardContent());
        }

        // ── Sidebar construction ──────────────────────────────────────────

        private void BuildSidebar()
        {
            // Staff Chat button is created in code (not designer) — same pattern as original
            _btnNavStaffChat = new Button { Text = "Staff Chat", Name = "btnNavStaffChat" };
            pnlManagerSidebar.Controls.Add(_btnNavStaffChat);

            Button[] navBtns =
            {
                btnNavDashboard, btnNavCabins, btnNavReservations,
                btnNavGuests, btnNavAnimals, btnNavEncounters,
                btnNavUsers, btnNavBills, btnNavSalesReports,
                _btnNavStaffChat
            };

            WildNestUI.StyleSidebar(
                pnlManagerSidebar, pnlManagerContent,
                lblSideTitle, lblSideRole, lblSideUser,
                navBtns, btnSignOut,
                ref _activeBtn);

            // Labels & emoji text
            lblSideTitle.Text             = "WILDNEST";
            btnNavDashboard.Text          = "📊  Dashboard";
            btnNavCabins.Text             = "🏡  Cabin Management";
            btnNavReservations.Text       = "🧾  Reservations";
            btnNavGuests.Text             = "👤  Guest Profiles";
            btnNavAnimals.Text            = "🦁  Animal Registry";
            btnNavEncounters.Text         = "🎯  Encounter Packages";
            btnNavUsers.Text              = "👥  Staff Accounts";       // was "User Management"
            btnNavBills.Text              = "💳  Billing & Reports";
            btnNavSalesReports.Text       = "📈  Sales Reports";        // NEW
            _btnNavStaffChat.Text         = "💬  Staff Chat";
            btnSignOut.Text               = "Sign Out";
        }

        // ── Navigation wiring ─────────────────────────────────────────────

        private void WireNavButtons()
        {
            // Pages that haven't changed keep the original Uc* class names
            // (UcAdminCabins, UcAdminReservations etc. stay in Project.UcAdministrator —
            //  we are NOT renaming those; Manager just navigates to them as-is).
            btnNavDashboard.Click    += (s, e) => Navigate(btnNavDashboard,    new UcManagerDashboardContent());
            btnNavCabins.Click       += (s, e) => Navigate(btnNavCabins,       new UcAdminCabins());
            btnNavReservations.Click += (s, e) => Navigate(btnNavReservations, new UcAdminReservations());
            btnNavGuests.Click       += (s, e) => Navigate(btnNavGuests,       new UcAdminGuests());
            btnNavAnimals.Click      += (s, e) => Navigate(btnNavAnimals,      new UcAdminAnimals());
            btnNavEncounters.Click   += (s, e) => Navigate(btnNavEncounters,   new UcAdminEncounters());

            // Replaced / new pages
            btnNavUsers.Click        += (s, e) => Navigate(btnNavUsers,        new UcManagerUsers());
            btnNavBills.Click        += (s, e) => Navigate(btnNavBills,        new UcManagerBills());
            btnNavSalesReports.Click += (s, e) => Navigate(btnNavSalesReports, new UcManagerSalesReports());

            btnSignOut.Click         += (s, e) => _parent?.SignOut();
            _btnNavStaffChat.Click   += (s, e) =>
                Navigate(_btnNavStaffChat, new Project.UcStaffChat("Manager", _displayName));
        }

        // ── Helpers (unchanged pattern) ───────────────────────────────────

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            pnlManagerContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlManagerContent.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
