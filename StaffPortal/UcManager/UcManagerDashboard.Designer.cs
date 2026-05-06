namespace Project.UcManager
{
    partial class UcManagerDashboard
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlManagerSidebar    = new System.Windows.Forms.Panel();
            lblSideTitle         = new System.Windows.Forms.Label();
            lblSideRole          = new System.Windows.Forms.Label();
            lblSideUser          = new System.Windows.Forms.Label();
            btnNavDashboard      = new System.Windows.Forms.Button();
            btnNavCabins         = new System.Windows.Forms.Button();
            btnNavReservations   = new System.Windows.Forms.Button();
            btnNavGuests         = new System.Windows.Forms.Button();
            btnNavAnimals        = new System.Windows.Forms.Button();
            btnNavEncounters     = new System.Windows.Forms.Button();
            btnNavUsers          = new System.Windows.Forms.Button();
            btnNavBills          = new System.Windows.Forms.Button();
            btnNavSalesReports   = new System.Windows.Forms.Button();
            btnSignOut           = new System.Windows.Forms.Button();
            pnlManagerContent    = new System.Windows.Forms.Panel();
            pnlManagerSidebar.SuspendLayout();
            SuspendLayout();

            pnlManagerSidebar.BackColor = System.Drawing.Color.FromArgb(7, 26, 14);
            pnlManagerSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            pnlManagerSidebar.Location = new System.Drawing.Point(0, 0);
            pnlManagerSidebar.Name = "pnlManagerSidebar";
            pnlManagerSidebar.Size = new System.Drawing.Size(220, 710);
            pnlManagerSidebar.TabIndex = 0;
            pnlManagerSidebar.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblSideTitle, lblSideRole, lblSideUser,
                btnNavDashboard, btnNavCabins, btnNavReservations, btnNavGuests,
                btnNavAnimals, btnNavEncounters, btnNavUsers, btnNavBills, btnNavSalesReports, btnSignOut
            });

            lblSideTitle.AutoSize = true;
            lblSideTitle.Location = new System.Drawing.Point(14, 14);
            lblSideTitle.Name = "lblSideTitle";
            lblSideTitle.Text = "WILDNEST";

            lblSideRole.AutoSize = true;
            lblSideRole.Location = new System.Drawing.Point(14, 36);
            lblSideRole.Name = "lblSideRole";
            lblSideRole.Text = "Resort Manager";

            lblSideUser.AutoSize = true;
            lblSideUser.Location = new System.Drawing.Point(14, 54);
            lblSideUser.Name = "lblSideUser";
            lblSideUser.Text = "manager";

            var btns = new System.Windows.Forms.Button[]
            {
                btnNavDashboard, btnNavCabins, btnNavReservations, btnNavGuests,
                btnNavAnimals, btnNavEncounters, btnNavUsers, btnNavBills, btnNavSalesReports
            };
            for (int i = 0; i < btns.Length; i++)
            {
                btns[i].TabIndex = 3 + i;
                btns[i].UseVisualStyleBackColor = true;
            }

            btnNavDashboard.Name = "btnNavDashboard";
            btnNavDashboard.Text = "Dashboard";
            btnNavCabins.Name = "btnNavCabins";
            btnNavCabins.Text = "Cabin Management";
            btnNavReservations.Name = "btnNavReservations";
            btnNavReservations.Text = "Reservations";
            btnNavGuests.Name = "btnNavGuests";
            btnNavGuests.Text = "Guest Profiles";
            btnNavAnimals.Name = "btnNavAnimals";
            btnNavAnimals.Text = "Animal Registry";
            btnNavEncounters.Name = "btnNavEncounters";
            btnNavEncounters.Text = "Encounter Packages";
            btnNavUsers.Name = "btnNavUsers";
            btnNavUsers.Text = "Staff Accounts";
            btnNavBills.Name = "btnNavBills";
            btnNavBills.Text = "Billing Reports";
            btnNavSalesReports.Name = "btnNavSalesReports";
            btnNavSalesReports.Text = "Sales Reports";

            btnSignOut.Name = "btnSignOut";
            btnSignOut.Text = "Sign Out";
            btnSignOut.TabIndex = 12;
            btnSignOut.UseVisualStyleBackColor = true;

            pnlManagerContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlManagerContent.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlManagerContent.Location = new System.Drawing.Point(220, 0);
            pnlManagerContent.Name = "pnlManagerContent";
            pnlManagerContent.Size = new System.Drawing.Size(1042, 710);
            pnlManagerContent.TabIndex = 1;

            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlManagerContent);
            Controls.Add(pnlManagerSidebar);
            Name = "UcManagerDashboard";
            Size = new System.Drawing.Size(1262, 710);
            pnlManagerSidebar.ResumeLayout(false);
            pnlManagerSidebar.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlManagerSidebar;
        private System.Windows.Forms.Label lblSideTitle;
        private System.Windows.Forms.Label lblSideRole;
        private System.Windows.Forms.Label lblSideUser;
        private System.Windows.Forms.Button btnNavDashboard;
        private System.Windows.Forms.Button btnNavCabins;
        private System.Windows.Forms.Button btnNavReservations;
        private System.Windows.Forms.Button btnNavGuests;
        private System.Windows.Forms.Button btnNavAnimals;
        private System.Windows.Forms.Button btnNavEncounters;
        private System.Windows.Forms.Button btnNavUsers;
        private System.Windows.Forms.Button btnNavBills;
        private System.Windows.Forms.Button btnNavSalesReports;
        private System.Windows.Forms.Button btnSignOut;
        private System.Windows.Forms.Panel pnlManagerContent;
    }
}
