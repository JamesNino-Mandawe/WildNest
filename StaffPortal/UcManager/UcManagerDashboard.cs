using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Project.UcAdministrator;

namespace Project.UcManager
{
    public partial class UcManagerDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private string _displayName = "Manager";
        private string _username = "manager";
        private Button? _activeBtn;
        private Button? _btnNavStaffChat;

        public UcManagerDashboard()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (_, _) =>
            {
                BuildSidebar();
                Navigate(btnNavDashboard, new UcManagerDashboardContent());
            };
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName = string.IsNullOrWhiteSpace(name) ? "Manager" : name;
            if (!IsDisposed)
            {
                lblSideUser.Text = _displayName;
                lblSideRole.Text = "Resort Manager";
            }
        }

        public void SetUsername(string username) =>
            _username = string.IsNullOrWhiteSpace(username) ? "manager" : username.Trim();

        private void BuildSidebar()
        {
            lblSideTitle.Text = "WILDNEST";
            lblSideRole.Text = "Resort Manager";
            lblSideUser.Text = _displayName;

            _btnNavStaffChat ??= new Button { Name = "btnNavStaffChat", Text = "Staff Chat", AutoSize = false };
            if (!pnlManagerSidebar.Controls.Contains(_btnNavStaffChat))
                pnlManagerSidebar.Controls.Add(_btnNavStaffChat);

            var navButtons = new[]
            {
                btnNavDashboard,
                btnNavCabins,
                btnNavReservations,
                btnNavGuests,
                btnNavAnimals,
                btnNavEncounters,
                btnNavUsers,
                btnNavBills,
                btnNavSalesReports,
                _btnNavStaffChat!
            };

            WildNestUI.StyleSidebar(
                pnlManagerSidebar,
                pnlManagerContent,
                lblSideTitle,
                lblSideRole,
                lblSideUser,
                navButtons,
                btnSignOut,
                ref _activeBtn);

            btnNavUsers.Text = "Staff Accounts";
            btnNavBills.Text = "Billing Reports";
            btnNavSalesReports.Text = "Sales Reports";

            WireNavButtons();
        }

        private void WireNavButtons()
        {
            btnNavDashboard.Click += (_, _) => Navigate(btnNavDashboard, new UcManagerDashboardContent());
            btnNavCabins.Click += (_, _) => Navigate(btnNavCabins, new UcManagerCabins());
            btnNavReservations.Click += (_, _) => Navigate(btnNavReservations, new UcManagerReservations());
            btnNavGuests.Click += (_, _) => Navigate(btnNavGuests, new UcManagerGuests());
            btnNavAnimals.Click += (_, _) => Navigate(btnNavAnimals, new UcManagerAnimals());
            btnNavEncounters.Click += (_, _) => Navigate(btnNavEncounters, new UcManagerEncounters());
            btnNavUsers.Click += (_, _) => Navigate(btnNavUsers, new UcManagerUsers());
            btnNavBills.Click += (_, _) => Navigate(btnNavBills, new UcManagerBills());
            btnNavSalesReports.Click += (_, _) => Navigate(btnNavSalesReports, new UcManagerSalesReports());
            _btnNavStaffChat!.Click += (_, _) => Navigate(_btnNavStaffChat!, new Project.UcStaffChat("Manager", _displayName, _username));
            btnSignOut.Click += (_, _) => _parent?.SignOut();
        }

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            uc.Dock = DockStyle.Fill;
            pnlManagerContent.Controls.Clear();
            pnlManagerContent.Controls.Add(uc);
        }

    }
}
