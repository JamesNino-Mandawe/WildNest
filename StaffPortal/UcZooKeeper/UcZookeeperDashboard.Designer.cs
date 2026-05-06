namespace Project.UcZooKeeper
{
    partial class UcZookeeperDashboard
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
            btnNavHealth        = new System.Windows.Forms.Button();
            btnNavFlag          = new System.Windows.Forms.Button();
            btnNavClear         = new System.Windows.Forms.Button();
            btnNavFeeding       = new System.Windows.Forms.Button();
            btnNavLog           = new System.Windows.Forms.Button();
            btnSignOut          = new System.Windows.Forms.Button();
            pnlZookeeperContent = new System.Windows.Forms.Panel();
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
                btnNavDashboard, btnNavHealth, btnNavFlag,
                btnNavClear, btnNavFeeding, btnNavLog, btnSignOut
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
            lblSideRole.Text     = "Zookeeper / Wildlife";
            lblSideRole.TabIndex = 1;

            lblSideUser.AutoSize = true;
            lblSideUser.Location = new System.Drawing.Point(14, 54);
            lblSideUser.Name     = "lblSideUser";
            lblSideUser.Text     = "keeper";
            lblSideUser.TabIndex = 2;

            // ── Nav buttons ───────────────────────────────────────────────────
            btnNavDashboard.Name = "btnNavDashboard"; btnNavDashboard.Text = "My Animals";        btnNavDashboard.TabIndex = 3;
            btnNavHealth.Name    = "btnNavHealth";    btnNavHealth.Text    = "Health Records";     btnNavHealth.TabIndex    = 4;
            btnNavFlag.Name      = "btnNavFlag";      btnNavFlag.Text      = "Flag Health Alert";  btnNavFlag.TabIndex      = 5;
            btnNavClear.Name     = "btnNavClear";     btnNavClear.Text     = "Clear Animal";       btnNavClear.TabIndex     = 6;
            btnNavFeeding.Name   = "btnNavFeeding";   btnNavFeeding.Text   = "Feeding Schedule";   btnNavFeeding.TabIndex   = 7;
            btnNavLog.Name       = "btnNavLog";       btnNavLog.Text       = "Daily Log";          btnNavLog.TabIndex       = 8;
            btnSignOut.Name      = "btnSignOut";      btnSignOut.Text      = "Sign Out";           btnSignOut.TabIndex      = 10;
            btnSignOut.UseVisualStyleBackColor = true;

            // ── Content area ──────────────────────────────────────────────────
            pnlZookeeperContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlZookeeperContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlZookeeperContent.Location  = new System.Drawing.Point(220, 0);
            pnlZookeeperContent.Name      = "pnlZookeeperContent";
            pnlZookeeperContent.Size      = new System.Drawing.Size(1042, 710);
            pnlZookeeperContent.TabIndex  = 1;

            // ── Form ──────────────────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlZookeeperContent);
            Controls.Add(pnlAdminSidebar);
            Name = "UcZookeeperDashboard";
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
        private System.Windows.Forms.Button btnNavHealth;
        private System.Windows.Forms.Button btnNavFlag;
        private System.Windows.Forms.Button btnNavClear;
        private System.Windows.Forms.Button btnNavFeeding;
        private System.Windows.Forms.Button btnNavLog;
        private System.Windows.Forms.Button btnSignOut;
        private System.Windows.Forms.Panel  pnlZookeeperContent;
    }
}
