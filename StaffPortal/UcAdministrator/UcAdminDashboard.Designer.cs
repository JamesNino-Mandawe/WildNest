namespace Project.UcAdministrator
{
    partial class UcAdminDashboard
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlAdminSidebar    = new System.Windows.Forms.Panel();
            lblSideTitle       = new System.Windows.Forms.Label();
            lblSideRole        = new System.Windows.Forms.Label();
            lblSideUser        = new System.Windows.Forms.Label();
            btnNavDashboard    = new System.Windows.Forms.Button();
            btnNavCabins       = new System.Windows.Forms.Button();
            btnNavReservations = new System.Windows.Forms.Button();
            btnNavGuests       = new System.Windows.Forms.Button();
            btnNavAnimals      = new System.Windows.Forms.Button();
            btnNavEncounters   = new System.Windows.Forms.Button();
            btnNavUsers        = new System.Windows.Forms.Button();
            btnNavBills        = new System.Windows.Forms.Button();
            btnSignOut         = new System.Windows.Forms.Button();
            pnlAdminContent    = new System.Windows.Forms.Panel();
            pnlAdminSidebar.SuspendLayout();
            SuspendLayout();

            // ── Sidebar ───────────────────────────────────────────────────────
            pnlAdminSidebar.BackColor = System.Drawing.Color.FromArgb(7, 26, 14);
            pnlAdminSidebar.Dock      = System.Windows.Forms.DockStyle.Left;
            pnlAdminSidebar.Location  = new System.Drawing.Point(0, 0);
            pnlAdminSidebar.Name      = "pnlAdminSidebar";
            pnlAdminSidebar.Size      = new System.Drawing.Size(220, 710);
            pnlAdminSidebar.TabIndex  = 0;
            pnlAdminSidebar.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblSideTitle, lblSideRole, lblSideUser,
                btnNavDashboard, btnNavCabins, btnNavReservations,
                btnNavGuests, btnNavAnimals, btnNavEncounters,
                btnNavUsers, btnNavBills, btnSignOut
            });

            // ── Labels ────────────────────────────────────────────────────────
            lblSideTitle.AutoSize = true;
            lblSideTitle.Location = new System.Drawing.Point(14, 14);
            lblSideTitle.Name     = "lblSideTitle";
            lblSideTitle.Text     = "WILDNEST";
            lblSideTitle.TabIndex = 0;

            lblSideRole.AutoSize = true;
            lblSideRole.Location = new System.Drawing.Point(14, 36);
            lblSideRole.Name     = "lblSideRole";
            lblSideRole.Text     = "System Administrator";
            lblSideRole.TabIndex = 1;

            lblSideUser.AutoSize = true;
            lblSideUser.Location = new System.Drawing.Point(14, 54);
            lblSideUser.Name     = "lblSideUser";
            lblSideUser.Text     = "admin";
            lblSideUser.TabIndex = 2;

            // ── Nav buttons (unstyled here; OnLoad applies WildNestUI.StyleSidebar) ──
            int[] btnTabIndex = { 3, 4, 5, 6, 7, 8, 9, 10 };
            var   btns = new System.Windows.Forms.Button[]
            {
                btnNavDashboard, btnNavCabins, btnNavReservations,
                btnNavGuests,    btnNavAnimals, btnNavEncounters,
                btnNavUsers,     btnNavBills
            };
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].TabIndex = btnTabIndex[i];
                btns[i].UseVisualStyleBackColor = true;
            }

            btnNavDashboard.Name    = "btnNavDashboard";    btnNavDashboard.Text    = "Dashboard";
            btnNavCabins.Name       = "btnNavCabins";       btnNavCabins.Text       = "Cabin Management";
            btnNavReservations.Name = "btnNavReservations"; btnNavReservations.Text = "Reservations";
            btnNavGuests.Name       = "btnNavGuests";       btnNavGuests.Text       = "Guest Profiles";
            btnNavAnimals.Name      = "btnNavAnimals";      btnNavAnimals.Text      = "Animal Registry";
            btnNavEncounters.Name   = "btnNavEncounters";   btnNavEncounters.Text   = "Encounter Packages";
            btnNavUsers.Name        = "btnNavUsers";        btnNavUsers.Text        = "User Management";
            btnNavBills.Name        = "btnNavBills";        btnNavBills.Text        = "Billing & Reports";

            // ── Sign out ──────────────────────────────────────────────────────
            btnSignOut.Name     = "btnSignOut";
            btnSignOut.Text     = "Sign Out";
            btnSignOut.TabIndex = 11;
            btnSignOut.UseVisualStyleBackColor = true;

            // ── Content area ──────────────────────────────────────────────────
            pnlAdminContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlAdminContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlAdminContent.Location  = new System.Drawing.Point(220, 0);
            pnlAdminContent.Name      = "pnlAdminContent";
            pnlAdminContent.Size      = new System.Drawing.Size(1042, 710);
            pnlAdminContent.TabIndex  = 1;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlAdminContent);
            Controls.Add(pnlAdminSidebar);
            Name = "UcAdminDashboard";
            Size = new System.Drawing.Size(1262, 710);
            pnlAdminSidebar.ResumeLayout(false);
            pnlAdminSidebar.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel  pnlAdminSidebar;
        private System.Windows.Forms.Label  lblSideTitle;
        private System.Windows.Forms.Label  lblSideRole;
        private System.Windows.Forms.Label  lblSideUser;
        private System.Windows.Forms.Button btnNavDashboard;
        private System.Windows.Forms.Button btnNavCabins;
        private System.Windows.Forms.Button btnNavReservations;
        private System.Windows.Forms.Button btnNavGuests;
        private System.Windows.Forms.Button btnNavAnimals;
        private System.Windows.Forms.Button btnNavEncounters;
        private System.Windows.Forms.Button btnNavUsers;
        private System.Windows.Forms.Button btnNavBills;
        private System.Windows.Forms.Button btnSignOut;
        private System.Windows.Forms.Panel  pnlAdminContent;
    }
}
