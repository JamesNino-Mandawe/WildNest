namespace Project.UcTourGuide
{
    partial class UcTourGuideDashboard
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlAdminSidebar     = new System.Windows.Forms.Panel();
            lblSideTitle        = new System.Windows.Forms.Label();
            lblSideRole         = new System.Windows.Forms.Label();
            lblSideUser         = new System.Windows.Forms.Label();
            btnNavDashboard     = new System.Windows.Forms.Button();
            btnNavGroups        = new System.Windows.Forms.Button();
            btnNavComplete      = new System.Windows.Forms.Button();
            btnNavHistory       = new System.Windows.Forms.Button();
            btnSignOut          = new System.Windows.Forms.Button();
            pnlTourGuideContent = new System.Windows.Forms.Panel();
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
                btnNavDashboard, btnNavGroups, btnNavComplete,
                btnNavHistory, btnSignOut
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
            lblSideRole.Text     = "Tour Guide / Safari";
            lblSideRole.TabIndex = 1;

            lblSideUser.AutoSize = true;
            lblSideUser.Location = new System.Drawing.Point(14, 54);
            lblSideUser.Name     = "lblSideUser";
            lblSideUser.Text     = "guide";
            lblSideUser.TabIndex = 2;

            // ── Nav buttons ───────────────────────────────────────────────────
            btnNavDashboard.Name = "btnNavDashboard"; btnNavDashboard.Text = "My Schedule";    btnNavDashboard.TabIndex = 3;
            btnNavGroups.Name    = "btnNavGroups";    btnNavGroups.Text    = "My Tour Groups"; btnNavGroups.TabIndex    = 4;
            btnNavComplete.Name  = "btnNavComplete";  btnNavComplete.Text  = "Mark Complete";  btnNavComplete.TabIndex  = 5;
            btnNavHistory.Name   = "btnNavHistory";   btnNavHistory.Text   = "Tour History";   btnNavHistory.TabIndex   = 6;
            btnSignOut.Name      = "btnSignOut";      btnSignOut.Text      = "Sign Out";       btnSignOut.TabIndex      = 8;
            btnSignOut.UseVisualStyleBackColor = true;

            // ── Content area ──────────────────────────────────────────────────
            pnlTourGuideContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlTourGuideContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlTourGuideContent.Location  = new System.Drawing.Point(220, 0);
            pnlTourGuideContent.Name      = "pnlTourGuideContent";
            pnlTourGuideContent.Size      = new System.Drawing.Size(1042, 710);
            pnlTourGuideContent.TabIndex  = 1;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlTourGuideContent);
            Controls.Add(pnlAdminSidebar);
            Name = "UcTourGuideDashboard";
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
        private System.Windows.Forms.Button btnNavGroups;
        private System.Windows.Forms.Button btnNavComplete;
        private System.Windows.Forms.Button btnNavHistory;
        private System.Windows.Forms.Button btnSignOut;
        private System.Windows.Forms.Panel  pnlTourGuideContent;
    }
}
