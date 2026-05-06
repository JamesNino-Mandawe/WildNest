namespace Project
{
    partial class BookNow
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
            lblHeroSub = new Label();
            lblHeroTitle = new Label();
            lblLocation = new Label();
            pnlStatusBar = new Panel();
            btnCabinStay = new Button();
            btnDayVisit = new Button();
            btnFullStayExperience = new Button();
            btnExperienceVisit = new Button();
            pnlContent = new Panel();
            pnlLeft = new Panel();
            pnlSummary = new Panel();
            pnlHero.SuspendLayout();
            pnlStatusBar.SuspendLayout();
            pnlContent.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHero
            // 
            pnlHero.Controls.Add(lblHeroSub);
            pnlHero.Controls.Add(lblHeroTitle);
            pnlHero.Controls.Add(lblLocation);
            pnlHero.Location = new Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new Size(1280, 180);
            pnlHero.TabIndex = 2;
            // 
            // lblHeroSub
            // 
            lblHeroSub.Location = new Point(0, 0);
            lblHeroSub.Name = "lblHeroSub";
            lblHeroSub.Size = new Size(100, 23);
            lblHeroSub.TabIndex = 2;
            lblHeroSub.Click += lblHeroSub_Click;
            // 
            // lblHeroTitle
            // 
            lblHeroTitle.Location = new Point(0, 0);
            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.Size = new Size(100, 23);
            lblHeroTitle.TabIndex = 1;
            // 
            // lblLocation
            // 
            lblLocation.Location = new Point(0, 0);
            lblLocation.Name = "lblLocation";
            lblLocation.Size = new Size(100, 23);
            lblLocation.TabIndex = 0;
            // 
            // pnlStatusBar
            // 
            pnlStatusBar.Controls.Add(btnCabinStay);
            pnlStatusBar.Controls.Add(btnDayVisit);
            pnlStatusBar.Controls.Add(btnFullStayExperience);
            pnlStatusBar.Controls.Add(btnExperienceVisit);
            pnlStatusBar.Location = new Point(0, 180);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new Size(1280, 52);
            pnlStatusBar.TabIndex = 1;
            // 
            // btnCabinStay
            // 
            btnCabinStay.Location = new Point(0, 0);
            btnCabinStay.Name = "btnCabinStay";
            btnCabinStay.Size = new Size(75, 23);
            btnCabinStay.TabIndex = 0;
            btnCabinStay.UseVisualStyleBackColor = false;
            // 
            // btnDayVisit
            // 
            btnDayVisit.Location = new Point(0, 0);
            btnDayVisit.Name = "btnDayVisit";
            btnDayVisit.Size = new Size(75, 23);
            btnDayVisit.TabIndex = 1;
            btnDayVisit.UseVisualStyleBackColor = false;
            // 
            // btnFullStayExperience
            // 
            btnFullStayExperience.Location = new Point(0, 0);
            btnFullStayExperience.Name = "btnFullStayExperience";
            btnFullStayExperience.Size = new Size(75, 23);
            btnFullStayExperience.TabIndex = 2;
            btnFullStayExperience.UseVisualStyleBackColor = false;
            // 
            // btnExperienceVisit
            // 
            btnExperienceVisit.Location = new Point(0, 0);
            btnExperienceVisit.Name = "btnExperienceVisit";
            btnExperienceVisit.Size = new Size(75, 23);
            btnExperienceVisit.TabIndex = 3;
            btnExperienceVisit.UseVisualStyleBackColor = false;
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.FromArgb(240, 237, 232);
            pnlContent.Controls.Add(pnlLeft);
            pnlContent.Controls.Add(pnlSummary);
            pnlContent.Location = new Point(0, 232);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(1280, 568);
            pnlContent.TabIndex = 0;
            pnlContent.Resize += PnlContent_Resize;
            // 
            // pnlLeft
            // 
            pnlLeft.AutoScroll = false;
            pnlLeft.BackColor = Color.FromArgb(240, 237, 232);
            pnlLeft.Location = new Point(0, 0);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(200, 100);
            pnlLeft.TabIndex = 0;
            // 
            // pnlSummary
            // 
            pnlSummary.BackColor = Color.FromArgb(7, 26, 14);
            pnlSummary.Location = new Point(0, 0);
            pnlSummary.Name = "pnlSummary";
            pnlSummary.Size = new Size(200, 100);
            pnlSummary.TabIndex = 1;
            // 
            // BookNow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlContent);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHero);
            Name = "BookNow";
            Size = new Size(1280, 800);
            pnlHero.ResumeLayout(false);
            pnlStatusBar.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        // ── Content layout: match HTML main-wrap proportions ─────
        private void PnlContent_Resize(object sender, System.EventArgs e) => ApplyContentLayout();

        private void ApplyContentLayout()
        {
            int totalW = pnlContent.ClientSize.Width;
            int totalH = pnlContent.ClientSize.Height;

            const int maxWrap = 1420;
            const int gap = 24;
            const int outerPad = 26;

            int totalAvailable = System.Math.Max(760, totalW - outerPad * 2);
            int innerW = System.Math.Min(totalAvailable, maxWrap);
            int sumW = System.Math.Max(340, System.Math.Min(390, (int)System.Math.Round(innerW * 0.26)));
            int leftW = innerW - sumW - gap;
            if (leftW < 720 && innerW >= 720 + gap + 300)
            {
                leftW = 720;
                sumW = innerW - leftW - gap;
            }
            if (sumW < 300)
            {
                sumW = 300;
                leftW = System.Math.Max(420, innerW - sumW - gap);
            }

            int startX = System.Math.Max(outerPad, (totalW - (leftW + gap + sumW)) / 2);
            int topY = 16;
            int h = System.Math.Max(totalH - topY - 16, 200);

            pnlLeft.Bounds = new System.Drawing.Rectangle(startX, topY, leftW, h);
            pnlSummary.Bounds = new System.Drawing.Rectangle(startX + leftW + gap, topY, sumW, h);

            foreach (System.Windows.Forms.Control child in pnlLeft.Controls)
            {
                if (!child.Visible)
                    continue;

                child.PerformLayout();
                if (child is Project.Booking.CabinStay cabin) cabin.TryBuild();
                else if (child is Project.Booking.DayVisit dayVisit) dayVisit.TryBuild();
                else if (child is Project.Booking.ExperienceVisit experience) experience.TryBuild();
                else if (child is Project.Booking.FullStayExperience fullStay) fullStay.TryBuild();
            }
        }
        #endregion

        private System.Windows.Forms.Panel pnlHero;
        private System.Windows.Forms.Label lblHeroSub;
        private System.Windows.Forms.Label lblHeroTitle;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Panel pnlStatusBar;
        private System.Windows.Forms.Button btnExperienceVisit;
        private System.Windows.Forms.Button btnFullStayExperience;
        private System.Windows.Forms.Button btnDayVisit;
        private System.Windows.Forms.Button btnCabinStay;
        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlSummary;
    }
}
