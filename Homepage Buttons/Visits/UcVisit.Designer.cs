namespace Project
{
    partial class UcVisit
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            pnlHero = new Panel();
            visit_tabs = new TableLayoutPanel();
            lblHours = new Label();
            lblGettingHere = new Label();
            lblWhatToExpect = new Label();
            lblFAQ = new Label();
            lblHeroSub = new Label();
            lblHeroTitle = new Label();
            lblLocation = new Label();
            pnlStatusBar = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            lblCabinStatus = new Label();
            lblSafariTimer = new Label();
            lblAnimalCount = new Label();
            pnlContentArea = new Panel();
            pnlReserve = new Panel();
            pnlHoursContent = new Panel();
            pnlRegularHours = new Panel();
            pnlSpecialHours = new Panel();
            lblTodayOpen = new Label();
            lblLastEntry = new Label();
            lblWhenToVisit = new Label();
            lblOpeningHours = new Label();

            pnlHero.SuspendLayout();
            visit_tabs.SuspendLayout();
            pnlStatusBar.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            pnlContentArea.SuspendLayout();
            pnlHoursContent.SuspendLayout();
            SuspendLayout();

            // ── pnlHero ── (matches HomePage hero = 550px)
            pnlHero.Controls.Add(visit_tabs);
            pnlHero.Controls.Add(lblHeroSub);
            pnlHero.Controls.Add(lblHeroTitle);
            pnlHero.Controls.Add(lblLocation);
            pnlHero.Dock = DockStyle.Top;
            pnlHero.Location = new Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new Size(1262, 550);
            pnlHero.TabIndex = 0;

            // ── visit_tabs ── (tab bar pill, centred lower in hero)
            visit_tabs.ColumnCount = 4;
            visit_tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            visit_tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            visit_tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            visit_tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            visit_tabs.Controls.Add(lblHours, 0, 0);
            visit_tabs.Controls.Add(lblGettingHere, 1, 0);
            visit_tabs.Controls.Add(lblWhatToExpect, 2, 0);
            visit_tabs.Controls.Add(lblFAQ, 3, 0);
            visit_tabs.Location = new Point(231, 426);
            visit_tabs.Name = "visit_tabs";
            visit_tabs.RowCount = 1;
            visit_tabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            visit_tabs.Size = new Size(800, 96);
            visit_tabs.TabIndex = 3;

            // ── tab labels ──
            lblHours.AutoSize = true; lblHours.Dock = DockStyle.Fill;
            lblHours.Name = "lblHours"; lblHours.TabIndex = 0;
            lblHours.Text = "Hours"; lblHours.TextAlign = ContentAlignment.MiddleCenter;

            lblGettingHere.AutoSize = true; lblGettingHere.Dock = DockStyle.Fill;
            lblGettingHere.Name = "lblGettingHere"; lblGettingHere.TabIndex = 1;
            lblGettingHere.Text = "Getting Here"; lblGettingHere.TextAlign = ContentAlignment.MiddleCenter;

            lblWhatToExpect.AutoSize = true; lblWhatToExpect.Dock = DockStyle.Fill;
            lblWhatToExpect.Name = "lblWhatToExpect"; lblWhatToExpect.TabIndex = 2;
            lblWhatToExpect.Text = "What to Expect"; lblWhatToExpect.TextAlign = ContentAlignment.MiddleCenter;

            lblFAQ.AutoSize = true; lblFAQ.Dock = DockStyle.Fill;
            lblFAQ.Name = "lblFAQ"; lblFAQ.TabIndex = 3;
            lblFAQ.Text = "FAQ"; lblFAQ.TextAlign = ContentAlignment.MiddleCenter;

            // ── hero text labels ──
            lblLocation.AutoSize = true;
            lblLocation.Location = new Point(487, 175);
            lblLocation.Name = "lblLocation"; lblLocation.TabIndex = 0;
            lblLocation.Text = "CARMEN, CEBU — PHILIPPINES";

            lblHeroTitle.Location = new Point(231, 220);
            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.Size = new Size(800, 80);
            lblHeroTitle.TabIndex = 1; lblHeroTitle.Text = "";

            lblHeroSub.AutoSize = true;
            lblHeroSub.Location = new Point(320, 314);
            lblHeroSub.Name = "lblHeroSub"; lblHeroSub.TabIndex = 2;
            lblHeroSub.Text = "Everything you need before arriving at WildNest. Select a topic to get started.";

            // ── pnlStatusBar ── (matches HomePage statusbar = 60px)
            pnlStatusBar.BackColor = Color.FromArgb(17, 43, 28);
            pnlStatusBar.Controls.Add(tableLayoutPanel1);
            pnlStatusBar.Dock = DockStyle.Top;
            pnlStatusBar.Location = new Point(0, 550);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new Size(1262, 60);
            pnlStatusBar.TabIndex = 1;

            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tableLayoutPanel1.Controls.Add(lblCabinStatus, 0, 0);
            tableLayoutPanel1.Controls.Add(lblSafariTimer, 1, 0);
            tableLayoutPanel1.Controls.Add(lblAnimalCount, 2, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1262, 60);
            tableLayoutPanel1.TabIndex = 0;

            lblCabinStatus.AutoSize = true; lblCabinStatus.Dock = DockStyle.Fill;
            lblCabinStatus.Font = new Font("Arial Rounded MT Bold", 12F);
            lblCabinStatus.Name = "lblCabinStatus"; lblCabinStatus.TabIndex = 0;
            lblCabinStatus.Text = "NA"; lblCabinStatus.TextAlign = ContentAlignment.MiddleCenter;

            lblSafariTimer.AutoSize = true; lblSafariTimer.Dock = DockStyle.Fill;
            lblSafariTimer.Font = new Font("Arial Rounded MT Bold", 12F);
            lblSafariTimer.Name = "lblSafariTimer"; lblSafariTimer.TabIndex = 1;
            lblSafariTimer.Text = "NA"; lblSafariTimer.TextAlign = ContentAlignment.MiddleCenter;

            lblAnimalCount.AutoSize = true; lblAnimalCount.Dock = DockStyle.Fill;
            lblAnimalCount.Font = new Font("Arial Rounded MT Bold", 12F);
            lblAnimalCount.Name = "lblAnimalCount"; lblAnimalCount.TabIndex = 2;
            lblAnimalCount.Text = "NA"; lblAnimalCount.TextAlign = ContentAlignment.MiddleCenter;

            // ── pnlContentArea ── (fills remaining space)
            
            pnlContentArea.BackColor = Color.FromArgb(240, 237, 232);
            pnlContentArea.Controls.Add(pnlReserve);
            pnlContentArea.Controls.Add(pnlHoursContent);
            pnlContentArea.Controls.Add(lblTodayOpen);
            pnlContentArea.Controls.Add(lblLastEntry);
            pnlContentArea.Controls.Add(lblWhenToVisit);
            pnlContentArea.Controls.Add(lblOpeningHours);
            pnlContentArea.Dock = DockStyle.Top;
            pnlContentArea.Size = new Size(1262, 750); // enough to show all content
            pnlContentArea.Location = new Point(0, 610);
            pnlContentArea.Name = "pnlContentArea";
            pnlContentArea.TabIndex = 2;

            // ── hours panel labels ──
            lblOpeningHours.AutoSize = true;
            lblOpeningHours.Location = new Point(60, 38);
            lblOpeningHours.Name = "lblOpeningHours"; lblOpeningHours.TabIndex = 0;
            lblOpeningHours.Text = "OPENING HOURS";

            lblWhenToVisit.Location = new Point(60, 60);
            lblWhenToVisit.Name = "lblWhenToVisit";
            lblWhenToVisit.Size = new Size(460, 46);
            lblWhenToVisit.TabIndex = 1; lblWhenToVisit.Text = "When to Visit";

            lblLastEntry.AutoSize = true;
            lblLastEntry.Location = new Point(60, 110);
            lblLastEntry.Name = "lblLastEntry"; lblLastEntry.TabIndex = 2;
            lblLastEntry.Text = "Last entry is 1 hour before closing — special sessions require advance booking";

            lblTodayOpen.Location = new Point(60, 136);
            lblTodayOpen.Name = "lblTodayOpen";
            lblTodayOpen.Size = new Size(320, 30);
            lblTodayOpen.TabIndex = 3;

            // ── pnlHoursContent ── (holds two side-by-side cards)
            pnlHoursContent.BackColor = Color.Transparent;
            pnlHoursContent.Controls.Add(pnlRegularHours);
            pnlHoursContent.Controls.Add(pnlSpecialHours);
            pnlHoursContent.Location = new Point(60, 178);
            pnlHoursContent.Name = "pnlHoursContent";
            pnlHoursContent.Size = new Size(1142, 390);
            pnlHoursContent.TabIndex = 4;

            pnlRegularHours.Location = new Point(0, 0);
            pnlRegularHours.Name = "pnlRegularHours";
            pnlRegularHours.Size = new Size(560, 378);
            pnlRegularHours.TabIndex = 0;

            pnlSpecialHours.Location = new Point(582, 0);
            pnlSpecialHours.Name = "pnlSpecialHours";
            pnlSpecialHours.Size = new Size(560, 378);
            pnlSpecialHours.TabIndex = 1;

            // ── CTA / Reserve strip ──
            pnlReserve.BackColor = Color.Transparent;
            pnlReserve.Location = new Point(60, 590);
            pnlReserve.Name = "pnlReserve";
            pnlReserve.Size = new Size(1142, 130);
            pnlReserve.TabIndex = 5;

            // ── UcVisit root ──
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlContentArea);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHero);
            Name = "UcVisit";
            Size = new Size(1262, 900);

            pnlHero.ResumeLayout(false);
            pnlHero.PerformLayout();
            visit_tabs.ResumeLayout(false);
            visit_tabs.PerformLayout();
            pnlStatusBar.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            pnlContentArea.ResumeLayout(false);
            pnlContentArea.PerformLayout();
            pnlHoursContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHero;
        private Label lblHeroSub;
        private Label lblHeroTitle;
        private Label lblLocation;
        private Panel pnlStatusBar;
        private TableLayoutPanel tableLayoutPanel1;
        private Label lblCabinStatus;
        private Label lblSafariTimer;
        private Label lblAnimalCount;
        private TableLayoutPanel visit_tabs;
        private Label lblFAQ;
        private Label lblWhatToExpect;
        private Label lblGettingHere;
        private Label lblHours;
        private Panel pnlContentArea;
        private Label lblOpeningHours;
        private Label lblWhenToVisit;
        private Label lblTodayOpen;
        private Label lblLastEntry;
        private Panel pnlHoursContent;
        private Panel pnlSpecialHours;
        private Panel pnlRegularHours;
        private Panel pnlReserve;
    }
}