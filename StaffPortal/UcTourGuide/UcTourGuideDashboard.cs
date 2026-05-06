using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcTourGuide
{
    public partial class UcTourGuideDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private Button? _activeBtn;
        private Button _btnNavStaffChat = null!;
        private string _displayName = "";
        private string _username = "tourguide";

        public UcTourGuideDashboard()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName = name;
            lblSideUser.Text = name;
            lblSideRole.Text = "Tour Guide / Safari";
        }

        public void SetUsername(string username) =>
            _username = string.IsNullOrWhiteSpace(username) ? "tourguide" : username.Trim();

        private void OnLoad(object? sender, EventArgs e)
        {
            BuildSidebar();
            WireNavButtons();
            WildNestUI.SetActive(ref _activeBtn, btnNavDashboard);
            LoadPage(new UcTourGuideDefaultContent(_displayName));
        }

        private void BuildSidebar()
        {
            _btnNavStaffChat = new Button { Text = "Staff Chat", Name = "btnNavStaffChat" };
            pnlAdminSidebar.Controls.Add(_btnNavStaffChat);

            Button[] navBtns =
            {
                btnNavDashboard, btnNavGroups, btnNavComplete,
                btnNavHistory, _btnNavStaffChat
            };

            WildNestUI.StyleSidebar(
                pnlAdminSidebar, pnlTourGuideContent,
                lblSideTitle, lblSideRole, lblSideUser,
                navBtns, btnSignOut,
                ref _activeBtn);

            btnNavDashboard.Text = "🗓️  My Schedule";
            btnNavGroups.Text = "👥  My Tour Groups";
            btnNavComplete.Text = "✅  Mark Complete";
            btnNavHistory.Text = "📚  Tour History";
            _btnNavStaffChat.Text = "💬  Staff Chat";
        }

        private void WireNavButtons()
        {
            btnNavDashboard.Click += (s, e) => Navigate(btnNavDashboard, new UcTourGuideDefaultContent(_displayName, _username));
            btnNavGroups.Click += (s, e) => Navigate(btnNavGroups, new UcTourGuideGroups(_displayName, _username));
            btnNavComplete.Click += (s, e) => Navigate(btnNavComplete, new UcTourGuideComplete(_displayName, _username));
            btnNavHistory.Click += (s, e) => Navigate(btnNavHistory, new UcTourGuideHistory(_displayName, _username));
            btnSignOut.Click += (s, e) => _parent?.SignOut();
            _btnNavStaffChat.Click += (s, e) =>
                Navigate(_btnNavStaffChat, new Project.UcStaffChat("TourGuide", _displayName, _username));
        }

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            pnlTourGuideContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlTourGuideContent.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
