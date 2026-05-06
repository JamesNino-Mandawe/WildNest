using System;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcZooKeeper
{
    public partial class UcZookeeperDashboard : UserControl
    {
        private StaffDashboard? _parent;
        private Button? _activeBtn;
        private Button _btnNavStaffChat = null!;
        private string _displayName = "";
        private string _username = "zookeeper";

        public UcZookeeperDashboard()
        {
            InitializeComponent();
            Load += OnLoad;
        }

        public void SetParentDashboard(StaffDashboard parent) => _parent = parent;

        public void SetDisplayName(string name)
        {
            _displayName = name;
            lblSideUser.Text = name;
            lblSideRole.Text = "Zookeeper / Wildlife";
        }

        public void SetUsername(string username) =>
            _username = string.IsNullOrWhiteSpace(username) ? "zookeeper" : username.Trim();

        private void OnLoad(object? sender, EventArgs e)
        {
            BuildSidebar();
            WireNavButtons();
            WildNestUI.SetActive(ref _activeBtn, btnNavDashboard);
            LoadPage(new UcZookeeperDefaultContent());
        }

        private void BuildSidebar()
        {
            _btnNavStaffChat = new Button { Text = "Staff Chat", Name = "btnNavStaffChat" };
            pnlAdminSidebar.Controls.Add(_btnNavStaffChat);

            Button[] navBtns =
            {
                btnNavDashboard, btnNavHealth, btnNavFlag,
                btnNavClear, btnNavFeeding, btnNavLog,
                _btnNavStaffChat
            };

            WildNestUI.StyleSidebar(
                pnlAdminSidebar, pnlZookeeperContent,
                lblSideTitle, lblSideRole, lblSideUser,
                navBtns, btnSignOut,
                ref _activeBtn);

            btnNavDashboard.Text = "🦁  My Animals";
            btnNavHealth.Text = "🩺  Health Records";
            btnNavFlag.Text = "🚩  Flag Health Alert";
            btnNavClear.Text = "✅  Clear Animal";
            btnNavFeeding.Text = "🥬  Feeding Schedule";
            btnNavLog.Text = "📋  Daily Log";
            _btnNavStaffChat.Text = "💬  Staff Chat";
        }

        private void WireNavButtons()
        {
            btnNavDashboard.Click += (s, e) => Navigate(btnNavDashboard, new UcZookeeperDefaultContent());
            btnNavHealth.Click += (s, e) => Navigate(btnNavHealth, new UcZookeeperHealth());
            btnNavFlag.Click += (s, e) => Navigate(btnNavFlag, new UcZookeeperFlag());
            btnNavClear.Click += (s, e) => Navigate(btnNavClear, new UcZookeeperClear());
            btnNavFeeding.Click += (s, e) => Navigate(btnNavFeeding, new UcZookeeperFeeding());
            btnNavLog.Click += (s, e) => Navigate(btnNavLog, new UcZookeeperLog());
            btnSignOut.Click += (s, e) => _parent?.SignOut();
            _btnNavStaffChat.Click += (s, e) =>
                Navigate(_btnNavStaffChat, new Project.UcStaffChat("ZooKeeper", _displayName, _username));
        }

        private void Navigate(Button btn, UserControl uc)
        {
            WildNestUI.SetActive(ref _activeBtn, btn);
            LoadPage(uc);
        }

        private void LoadPage(UserControl uc)
        {
            pnlZookeeperContent.Controls.Clear();
            uc.Dock = DockStyle.Fill;
            pnlZookeeperContent.Controls.Add(uc);
            uc.BringToFront();
        }
    }
}
