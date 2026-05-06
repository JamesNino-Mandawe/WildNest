using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcAdministrator
{
    public partial class UcAdminDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private Button? _activeBtn;
        private Button _btnNavStaffChat = null!;
        private string _displayName = "Admin User";
        private string _username = "administrator";

        public UcAdminDashboard()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName = name;
            _username = string.IsNullOrWhiteSpace(name) ? "administrator" : name.Trim().ToLowerInvariant().Replace(" ", "");
            lblSideUser.Text = name;
            lblSideRole.Text = "System Administrator";
        }

        private void OnLoad(object? sender, EventArgs e)
        {
            BuildSidebar();
            WireNavButtons();
            WildNestUI.SetActive(ref _activeBtn, btnNavDashboard);
            LoadPage(new UcAdminDashboardContent());
        }

        private void BuildSidebar()
        {
            _btnNavStaffChat = new Button { Text = "Staff Chat", Name = "btnNavStaffChat" };
            pnlAdminSidebar.Controls.Add(_btnNavStaffChat);

            Button[] navBtns =
            {
                btnNavDashboard, btnNavCabins, btnNavReservations,
                btnNavGuests, btnNavAnimals, btnNavEncounters,
                btnNavUsers, btnNavBills, _btnNavStaffChat
            };

            WildNestUI.StyleSidebar(
                pnlAdminSidebar, pnlAdminContent,
                lblSideTitle, lblSideRole, lblSideUser,
                navBtns, btnSignOut,
                ref _activeBtn);

            btnNavDashboard.Text = "📊  Dashboard";
            btnNavCabins.Text = "🏡  Cabin Management";
            btnNavReservations.Text = "🧾  Reservations";
            btnNavGuests.Text = "👤  Guest Profiles";
            btnNavAnimals.Text = "🦁  Animal Registry";
            btnNavEncounters.Text = "🎯  Encounter Packages";
            btnNavUsers.Text = "👥  User Management";
            btnNavBills.Text = "💳  Billing & Reports";
            _btnNavStaffChat.Text = "💬  Staff Chat";
        }

        private void WireNavButtons()
        {
            btnNavDashboard.Click += (s, e) => Navigate(btnNavDashboard, new UcAdminDashboardContent());
            btnNavCabins.Click += (s, e) => Navigate(btnNavCabins, new UcAdminCabins());
            btnNavReservations.Click += (s, e) => Navigate(btnNavReservations, new UcAdminReservations());
            btnNavGuests.Click += (s, e) => Navigate(btnNavGuests, new UcAdminGuests());
            btnNavAnimals.Click += (s, e) => Navigate(btnNavAnimals, new UcAdminAnimals());
            btnNavEncounters.Click += (s, e) => Navigate(btnNavEncounters, new UcAdminEncounters());
            btnNavUsers.Click += (s, e) => Navigate(btnNavUsers, new UcAdminUsers());
            btnNavBills.Click += (s, e) => Navigate(btnNavBills, new UcAdminBills());
            btnSignOut.Click += (s, e) => _parent?.SignOut();
            _btnNavStaffChat.Click += (s, e) =>
                Navigate(_btnNavStaffChat, new Project.UcStaffChat("Administrator", _displayName, _username));
        }

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            pnlAdminContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlAdminContent.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
