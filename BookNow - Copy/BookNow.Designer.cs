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
            pnlHero = new System.Windows.Forms.Panel();
            lblHeroSub = new System.Windows.Forms.Label();
            lblHeroTitle = new System.Windows.Forms.Label();
            lblLocation = new System.Windows.Forms.Label();

            pnlStatusBar = new System.Windows.Forms.Panel();
            btnCabinStay = new System.Windows.Forms.Button();
            btnDayVisit = new System.Windows.Forms.Button();
            btnFullStayExperience = new System.Windows.Forms.Button();
            btnExperienceVisit = new System.Windows.Forms.Button();

            pnlContent = new System.Windows.Forms.Panel();
            pnlLeft = new System.Windows.Forms.Panel();
            pnlSummary = new System.Windows.Forms.Panel();

            pnlHero.SuspendLayout();
            pnlStatusBar.SuspendLayout();
            pnlContent.SuspendLayout();
            SuspendLayout();

            // ── pnlHero ──────────────────────────────────────────
            pnlHero.Controls.Add(lblHeroSub);
            pnlHero.Controls.Add(lblHeroTitle);
            pnlHero.Controls.Add(lblLocation);
            pnlHero.Location = new System.Drawing.Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new System.Drawing.Size(1280, 180);
            pnlHero.TabIndex = 2;

            lblLocation.Name = "lblLocation";
            lblLocation.TabIndex = 0;

            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.TabIndex = 1;

            lblHeroSub.Name = "lblHeroSub";
            lblHeroSub.TabIndex = 2;
            lblHeroSub.Click += lblHeroSub_Click;

            // ── pnlStatusBar ─────────────────────────────────────
            pnlStatusBar.Controls.Add(btnCabinStay);
            pnlStatusBar.Controls.Add(btnDayVisit);
            pnlStatusBar.Controls.Add(btnFullStayExperience);
            pnlStatusBar.Controls.Add(btnExperienceVisit);
            pnlStatusBar.Location = new System.Drawing.Point(0, 180);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new System.Drawing.Size(1280, 52);
            pnlStatusBar.TabIndex = 1;

            btnCabinStay.Name = "btnCabinStay";
            btnCabinStay.TabIndex = 0;
            btnCabinStay.UseVisualStyleBackColor = false;

            btnDayVisit.Name = "btnDayVisit";
            btnDayVisit.TabIndex = 1;
            btnDayVisit.UseVisualStyleBackColor = false;

            btnFullStayExperience.Name = "btnFullStayExperience";
            btnFullStayExperience.TabIndex = 2;
            btnFullStayExperience.UseVisualStyleBackColor = false;

            btnExperienceVisit.Name = "btnExperienceVisit";
            btnExperienceVisit.TabIndex = 3;
            btnExperienceVisit.UseVisualStyleBackColor = false;

            // ── pnlContent ───────────────────────────────────────
            pnlContent.Controls.Add(pnlLeft);
            pnlContent.Controls.Add(pnlSummary);
            pnlContent.Location = new System.Drawing.Point(0, 232);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new System.Drawing.Size(1280, 568);
            pnlContent.TabIndex = 0;
            pnlContent.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlContent.Resize += PnlContent_Resize;

            // pnlLeft — scrollable booking form (65%)
            pnlLeft.Name = "pnlLeft";
            pnlLeft.TabIndex = 0;
            pnlLeft.BackColor = System.Drawing.Color.FromArgb(240, 237, 232);
            pnlLeft.AutoScroll = true;

            // pnlSummary — live booking summary card (35%)
            pnlSummary.Name = "pnlSummary";
            pnlSummary.TabIndex = 1;
            pnlSummary.BackColor = System.Drawing.Color.FromArgb(7, 26, 14);

            // ── BookNow UserControl ───────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlContent);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHero);
            Name = "BookNow";
            Size = new System.Drawing.Size(1280, 800);

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

            const int maxWrap = 1400;
            const int gap = 16;
            const int summaryWidth = 300;
            const int outerPad = 14;

            int innerW = System.Math.Min(System.Math.Max(totalW - outerPad * 2, 860), maxWrap);
            int sumW = System.Math.Min(summaryWidth, System.Math.Max(276, (int)System.Math.Round(innerW * 0.215)));
            int leftW = System.Math.Max(680, innerW - sumW - gap);
            if (leftW + gap + sumW > innerW)
            {
                leftW = innerW - sumW - gap;
            }

            int startX = (totalW - (leftW + gap + sumW)) / 2;
            int topY = 16;
            int h = System.Math.Max(totalH - topY - 16, 200);

            pnlLeft.Bounds = new System.Drawing.Rectangle(startX, topY, leftW, h);
            pnlSummary.Bounds = new System.Drawing.Rectangle(startX + leftW + gap, topY, sumW, h);
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
