// ============================================================
//  UcManagerDashboard.Designer.cs
//  Drop-in replacement for UcAdminDashboard.Designer.cs
//  Rename the existing Designer file to this name, then replace
//  every "UcAdminDashboard" type reference with "UcManagerDashboard"
//  and every "pnlAdminSidebar/pnlAdminContent" with the names below.
//  All other designer-generated code (InitializeComponent wiring)
//  is preserved exactly — only identifiers change.
// ============================================================

namespace Project.UcManager
{
    partial class UcManagerDashboard
    {
        private System.ComponentModel.IContainer components = null;

        // ── Controls declared in Designer ───────────────────────────────

        // Sidebar panel (was pnlAdminSidebar)
        private System.Windows.Forms.Panel pnlManagerSidebar = null!;

        // Content panel (was pnlAdminContent)
        private System.Windows.Forms.Panel pnlManagerContent = null!;

        // Sidebar identity labels
        private System.Windows.Forms.Label lblSideTitle = null!;
        private System.Windows.Forms.Label lblSideRole  = null!;
        private System.Windows.Forms.Label lblSideUser  = null!;

        // Navigation buttons (same count as the old admin sidebar,
        // plus one extra: btnNavSalesReports)
        private System.Windows.Forms.Button btnNavDashboard    = null!;
        private System.Windows.Forms.Button btnNavCabins       = null!;
        private System.Windows.Forms.Button btnNavReservations = null!;
        private System.Windows.Forms.Button btnNavGuests       = null!;
        private System.Windows.Forms.Button btnNavAnimals      = null!;
        private System.Windows.Forms.Button btnNavEncounters   = null!;
        private System.Windows.Forms.Button btnNavUsers        = null!;
        private System.Windows.Forms.Button btnNavBills        = null!;
        private System.Windows.Forms.Button btnNavSalesReports = null!;  // NEW
        private System.Windows.Forms.Button btnSignOut         = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pnlManagerSidebar  = new System.Windows.Forms.Panel();
            pnlManagerContent  = new System.Windows.Forms.Panel();
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
            btnNavSalesReports = new System.Windows.Forms.Button();
            btnSignOut         = new System.Windows.Forms.Button();

            SuspendLayout();

            // ── pnlManagerSidebar ──────────────────────────────────────
            pnlManagerSidebar.BackColor = System.Drawing.Color.FromArgb(7, 26, 14);
            pnlManagerSidebar.Dock      = System.Windows.Forms.DockStyle.Left;
            pnlManagerSidebar.Name      = "pnlManagerSidebar";
            pnlManagerSidebar.Width     = 220;

            // Sidebar labels are added inside BuildSidebar() in code-behind;
            // their properties are set by WildNestUI.StyleSidebar().
            pnlManagerSidebar.Controls.AddRange(new System.Windows.Forms.Control[] {
                lblSideTitle, lblSideRole, lblSideUser,
                btnNavDashboard, btnNavCabins, btnNavReservations,
                btnNavGuests, btnNavAnimals, btnNavEncounters,
                btnNavUsers, btnNavBills, btnNavSalesReports,
                btnSignOut
            });

            // ── pnlManagerContent ──────────────────────────────────────
            pnlManagerContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlManagerContent.Dock      = System.Windows.Forms.DockStyle.Fill;
            pnlManagerContent.Name      = "pnlManagerContent";

            // ── UserControl root ───────────────────────────────────────
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor     = System.Drawing.Color.FromArgb(240, 237, 232);
            Dock          = System.Windows.Forms.DockStyle.Fill;
            Name          = "UcManagerDashboard";

            Controls.Add(pnlManagerContent);
            Controls.Add(pnlManagerSidebar);

            ResumeLayout(false);
        }
    }
}
