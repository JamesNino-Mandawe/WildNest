using Project.StaffPortal.UcReception;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcReception
{
    public partial class UcReceptionDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private Button? _activeBtn;
        private Button _btnNavGuestChat = null!;
        private Button _btnNavStaffChat = null!;
        private string _displayName = "Reception";
        private string _username = "reception";

        public UcReceptionDashboard()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName = name;
            lblSideUser.Text = name;
            lblSideRole.Text = "Front Desk Operations";
        }

        public void SetUsername(string username) =>
            _username = string.IsNullOrWhiteSpace(username) ? "reception" : username.Trim();

        private void OnLoad(object? sender, EventArgs e)
        {
            BuildSidebar();
            WireNavButtons();
            WildNestUI.SetActive(ref _activeBtn, btnNavDashboard);
            LoadPage(new UcReceptionDashboardContent());
        }

        private void BuildSidebar()
        {
            _btnNavGuestChat = new Button { Text = "Guest Chat", Name = "btnNavGuestChat" };
            _btnNavStaffChat = new Button { Text = "Staff Chat", Name = "btnNavStaffChat" };

            pnlReceptionSidebar.Controls.Add(_btnNavGuestChat);
            pnlReceptionSidebar.Controls.Add(_btnNavStaffChat);

            Button[] navBtns =
            {
                btnNavDashboard,
                btnNavNewBooking,
                btnNavCheckIn,
                btnNavCheckOut,
                btnNavGuests,
                btnNavCabins,
                btnNavEncounters,
                _btnNavGuestChat,
                _btnNavStaffChat
            };

            WildNestUI.StyleSidebar(
                pnlReceptionSidebar,
                pnlReceptionContent,
                lblSideTitle,
                lblSideRole,
                lblSideUser,
                navBtns,
                btnSignOut,
                ref _activeBtn);

            btnNavDashboard.Text = "📊  Dashboard";
            btnNavNewBooking.Text = "➕  New Booking";
            btnNavCheckIn.Text = "✅  Check-In";
            btnNavCheckOut.Text = "🧾  Check-Out & Billing";
            btnNavGuests.Text = "👤  Guest Profiles";
            btnNavCabins.Text = "🏡  Cabin Availability";
            btnNavEncounters.Text = "🎯  Book Encounter";
            _btnNavGuestChat.Text = "💬  Guest Chat";
            _btnNavStaffChat.Text = "🔒  Staff Chat";
        }

        private void WireNavButtons()
        {
            btnNavDashboard.Click += (s, e) => Navigate(btnNavDashboard, new UcReceptionDashboardContent());
            btnNavNewBooking.Click += (s, e) => Navigate(btnNavNewBooking, new UcReceptionNewBooking());
            btnNavCheckIn.Click += (s, e) => Navigate(btnNavCheckIn, new UcReceptionCheckIn());
            btnNavCheckOut.Click += (s, e) => Navigate(btnNavCheckOut, new UcReceptionCheckOut());
            btnNavGuests.Click += (s, e) => Navigate(btnNavGuests, new UcReceptionGuests());
            btnNavCabins.Click += (s, e) => Navigate(btnNavCabins, new UcReceptionCabins());
            btnNavEncounters.Click += (s, e) => Navigate(btnNavEncounters, new UcReceptionEncounters());
            btnSignOut.Click += (s, e) => _parent?.SignOut();

            _btnNavGuestChat.Click += (s, e) => Navigate(_btnNavGuestChat, new UcReceptionChat(_username, _displayName));
            _btnNavStaffChat.Click += (s, e) => Navigate(_btnNavStaffChat, new Project.UcStaffChat("Reception", _displayName, _username));
        }

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            pnlReceptionContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlReceptionContent.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
