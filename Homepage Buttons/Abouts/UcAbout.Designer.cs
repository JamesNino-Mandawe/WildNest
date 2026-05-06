namespace Project
{
    partial class UcAbout
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            pnlHero = new System.Windows.Forms.Panel();
            tlpTabs = new System.Windows.Forms.TableLayoutPanel();
            lblImpact = new System.Windows.Forms.Label();
            lblHistory = new System.Windows.Forms.Label();
            lblTeam = new System.Windows.Forms.Label();
            lblMission = new System.Windows.Forms.Label();
            lblOurStory = new System.Windows.Forms.Label();
            pnlFoundedStrip = new System.Windows.Forms.Panel();
            lblHeroSub = new System.Windows.Forms.Label();
            lblHeroTitle = new System.Windows.Forms.Label();
            lblLocation = new System.Windows.Forms.Label();
            pnlStatusBar = new System.Windows.Forms.Panel();
            lblEyebrow = new System.Windows.Forms.Label();
            lblHowBegan = new System.Windows.Forms.Label();
            lblSubtitle = new System.Windows.Forms.Label();
            pnlMissionStatement = new System.Windows.Forms.FlowLayoutPanel();
            pnlConservation = new System.Windows.Forms.Panel();
            pnlEducation = new System.Windows.Forms.Panel();
            pnlCommunity = new System.Windows.Forms.Panel();
            pnlContentArea = new System.Windows.Forms.Panel();

            pnlHero.SuspendLayout();
            tlpTabs.SuspendLayout();
            pnlContentArea.SuspendLayout();
            SuspendLayout();

            // ── pnlHero ── 550 px tall, matching UcVisit exactly
            pnlHero.Controls.Add(tlpTabs);
            pnlHero.Controls.Add(pnlFoundedStrip);
            pnlHero.Controls.Add(lblHeroSub);
            pnlHero.Controls.Add(lblHeroTitle);
            pnlHero.Controls.Add(lblLocation);
            pnlHero.Dock = System.Windows.Forms.DockStyle.Top;
            pnlHero.Location = new System.Drawing.Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new System.Drawing.Size(1262, 550);
            pnlHero.TabIndex = 3;

            // ── tlpTabs ── 96 px pill bar, full width, pinned to hero bottom
            //   Matches UcVisit visit_tabs height (96 px).
            //   5 equal columns for: Our Story | Mission & Values | Our Team | History | Impact
            tlpTabs.ColumnCount = 5;
            tlpTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlpTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlpTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlpTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlpTabs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            tlpTabs.Controls.Add(lblOurStory, 0, 0);
            tlpTabs.Controls.Add(lblMission, 1, 0);
            tlpTabs.Controls.Add(lblTeam, 2, 0);
            tlpTabs.Controls.Add(lblHistory, 3, 0);
            tlpTabs.Controls.Add(lblImpact, 4, 0);
            tlpTabs.Location = new System.Drawing.Point(0, 454);   // 550 - 96 = 454
            tlpTabs.Name = "tlpTabs";
            tlpTabs.RowCount = 1;
            tlpTabs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tlpTabs.Size = new System.Drawing.Size(1262, 96);
            tlpTabs.TabIndex = 6;

            // Tab label cells — DockStyle.Fill so they stretch to column width
            lblOurStory.AutoSize = false; lblOurStory.Dock = System.Windows.Forms.DockStyle.Fill;
            lblOurStory.Name = "lblOurStory"; lblOurStory.TabIndex = 6; lblOurStory.Text = "";

            lblMission.AutoSize = false; lblMission.Dock = System.Windows.Forms.DockStyle.Fill;
            lblMission.Name = "lblMission"; lblMission.TabIndex = 7; lblMission.Text = "";

            lblTeam.AutoSize = false; lblTeam.Dock = System.Windows.Forms.DockStyle.Fill;
            lblTeam.Name = "lblTeam"; lblTeam.TabIndex = 8; lblTeam.Text = "";

            lblHistory.AutoSize = false; lblHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            lblHistory.Name = "lblHistory"; lblHistory.TabIndex = 9; lblHistory.Text = "";

            lblImpact.AutoSize = false; lblImpact.Dock = System.Windows.Forms.DockStyle.Fill;
            lblImpact.Name = "lblImpact"; lblImpact.TabIndex = 10; lblImpact.Text = "";

            // ── Hero content labels ──────────────────────────────────────
            // Usable hero content zone = 550 - 96(tabs) = 454 px.
            // Vertical stack centred inside that zone:
            //   lblLocation   (eyebrow)    y≈120  h=22
            //   lblHeroTitle  (big title)  y≈158  h=100
            //   lblHeroSub    (italic sub) y≈274  h=64
            //   pnlFoundedStrip            y≈350  h=30
            // All horizontally centred around x=631 (1262/2).

            lblLocation.AutoSize = true;
            lblLocation.Location = new System.Drawing.Point(531, 120);   // centred ~
            lblLocation.Name = "lblLocation";
            lblLocation.TabIndex = 2;
            lblLocation.Text = "";

            lblHeroTitle.AutoSize = false;
            lblHeroTitle.Location = new System.Drawing.Point(251, 152);
            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.Size = new System.Drawing.Size(760, 110);  // extra height = no clipping
            lblHeroTitle.TabIndex = 3;
            lblHeroTitle.Text = "";

            lblHeroSub.AutoSize = false;
            lblHeroSub.Location = new System.Drawing.Point(271, 272);
            lblHeroSub.Name = "lblHeroSub";
            lblHeroSub.Size = new System.Drawing.Size(720, 64);
            lblHeroSub.TabIndex = 4;
            lblHeroSub.Text = "";

            // pnlFoundedStrip — pill badge below the sub-title
            pnlFoundedStrip.Location = new System.Drawing.Point(401, 348);
            pnlFoundedStrip.Name = "pnlFoundedStrip";
            pnlFoundedStrip.Size = new System.Drawing.Size(460, 32);
            pnlFoundedStrip.TabIndex = 5;

            // ── pnlStatusBar ── 60 px, matches UcVisit exactly
            pnlStatusBar.BackColor = System.Drawing.Color.FromArgb(17, 43, 28);
            pnlStatusBar.Dock = System.Windows.Forms.DockStyle.Top;
            pnlStatusBar.Location = new System.Drawing.Point(0, 550);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new System.Drawing.Size(1262, 60);
            pnlStatusBar.TabIndex = 5;

            // ── pnlContentArea labels (Our Story tab) ────────────────────
            // These sit inside pnlContentArea which starts at y=610 (550+60).
            // Vertical flow with clear gaps so nothing overlaps:
            //   lblEyebrow   y=32   h=20   ("OUR STORY" tag)
            //   lblHowBegan  y=60   h=48   (large heading)
            //   lblSubtitle  y=114  h=22   (small subtitle)
            //   pnlMissionStatement y=144  h=190
            //   pnlConservation     y=346  h=272
            //   pnlEducation        y=346  h=272  (same row, offset x)
            //   pnlCommunity        y=346  h=272  (same row, offset x)

            lblEyebrow.AutoSize = true;
            lblEyebrow.Location = new System.Drawing.Point(54, 32);
            lblEyebrow.Name = "lblEyebrow";
            lblEyebrow.TabIndex = 6;
            lblEyebrow.Text = "Our Story";

            lblHowBegan.AutoSize = false;
            lblHowBegan.Location = new System.Drawing.Point(54, 58);
            lblHowBegan.Name = "lblHowBegan";
            lblHowBegan.Size = new System.Drawing.Size(700, 50);       // wide enough, never wraps oddly
            lblHowBegan.TabIndex = 7;
            lblHowBegan.Text = "How WildNest Began";

            lblSubtitle.AutoSize = false;
            lblSubtitle.Location = new System.Drawing.Point(54, 114);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new System.Drawing.Size(800, 24);
            lblSubtitle.TabIndex = 8;
            lblSubtitle.Text = "From a single rescued eagle to Southeast Asia's leading wildlife sanctuary";
            lblSubtitle.Click += lblSubtitle_Click;

            // Mission statement flow panel — starts 12 px below subtitle
            pnlMissionStatement.Location = new System.Drawing.Point(48, 146);
            pnlMissionStatement.Name = "pnlMissionStatement";
            pnlMissionStatement.Size = new System.Drawing.Size(1166, 190);
            pnlMissionStatement.TabIndex = 9;

            // Three pillar cards — horizontal row below mission statement
            pnlConservation.Location = new System.Drawing.Point(48, 348);
            pnlConservation.Name = "pnlConservation";
            pnlConservation.Size = new System.Drawing.Size(364, 272);
            pnlConservation.TabIndex = 10;

            pnlEducation.Location = new System.Drawing.Point(432, 348);
            pnlEducation.Name = "pnlEducation";
            pnlEducation.Size = new System.Drawing.Size(364, 272);
            pnlEducation.TabIndex = 11;

            pnlCommunity.Location = new System.Drawing.Point(816, 348);
            pnlCommunity.Name = "pnlCommunity";
            pnlCommunity.Size = new System.Drawing.Size(364, 272);
            pnlCommunity.TabIndex = 12;

            // ── pnlContentArea ── fills remainder below status bar
         
            pnlContentArea.Controls.Add(pnlMissionStatement);
            pnlContentArea.Controls.Add(pnlConservation);
            pnlContentArea.Controls.Add(pnlEducation);
            pnlContentArea.Controls.Add(pnlCommunity);
            pnlContentArea.Controls.Add(lblSubtitle);
            pnlContentArea.Controls.Add(lblHowBegan);
            pnlContentArea.Controls.Add(lblEyebrow);
            pnlContentArea.Dock = System.Windows.Forms.DockStyle.Top;
            pnlContentArea.Location = new System.Drawing.Point(0, 610);
            pnlContentArea.Name = "pnlContentArea";
            pnlContentArea.Size = new System.Drawing.Size(1262, 650);
            pnlContentArea.TabIndex = 13;

            // ── Root UcAbout ─────────────────────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlContentArea);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHero);
            Name = "UcAbout";
            Size = new System.Drawing.Size(1262, 1260);

            pnlHero.ResumeLayout(false);
            pnlHero.PerformLayout();
            tlpTabs.ResumeLayout(false);
            tlpTabs.PerformLayout();
            pnlContentArea.ResumeLayout(false);
            pnlContentArea.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHero;
        private System.Windows.Forms.Label lblHeroSub;
        private System.Windows.Forms.Label lblHeroTitle;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Panel pnlStatusBar;
        private System.Windows.Forms.Panel pnlFoundedStrip;
        private System.Windows.Forms.TableLayoutPanel tlpTabs;
        private System.Windows.Forms.Label lblImpact;
        private System.Windows.Forms.Label lblHistory;
        private System.Windows.Forms.Label lblTeam;
        private System.Windows.Forms.Label lblMission;
        private System.Windows.Forms.Label lblOurStory;
        private System.Windows.Forms.Label lblEyebrow;
        private System.Windows.Forms.Label lblHowBegan;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.FlowLayoutPanel pnlMissionStatement;
        private System.Windows.Forms.Panel pnlConservation;
        private System.Windows.Forms.Panel pnlEducation;
        private System.Windows.Forms.Panel pnlCommunity;
        private System.Windows.Forms.Panel pnlContentArea;
    }
}