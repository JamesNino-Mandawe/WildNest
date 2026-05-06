namespace Project.Homepage_Buttons.Visits
{
    partial class UcFaq
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
            lblFrequentlyAsked = new System.Windows.Forms.Label();
            lblCommonQuestions = new System.Windows.Forms.Label();
            lblEverythingGuests = new System.Windows.Forms.Label();
            pnlWereHereToHelp = new System.Windows.Forms.Panel();
            pnlQuickHours = new System.Windows.Forms.Panel();
            SuspendLayout();

            // lblFrequentlyAsked
            lblFrequentlyAsked.AutoSize = true;
            lblFrequentlyAsked.Location = new System.Drawing.Point(60, 26);
            lblFrequentlyAsked.Name = "lblFrequentlyAsked";
            lblFrequentlyAsked.Size = new System.Drawing.Size(145, 20);
            lblFrequentlyAsked.TabIndex = 0;
            lblFrequentlyAsked.Text = "FREQUENTLY ASKED";

            // lblCommonQuestions
            lblCommonQuestions.AutoSize = false;
            lblCommonQuestions.Location = new System.Drawing.Point(60, 48);
            lblCommonQuestions.Name = "lblCommonQuestions";
            lblCommonQuestions.Size = new System.Drawing.Size(600, 46);
            lblCommonQuestions.TabIndex = 1;
            lblCommonQuestions.Text = "Common Questions";

            // lblEverythingGuests
            lblEverythingGuests.AutoSize = false;
            lblEverythingGuests.Location = new System.Drawing.Point(60, 98);
            lblEverythingGuests.Name = "lblEverythingGuests";
            lblEverythingGuests.Size = new System.Drawing.Size(660, 22);
            lblEverythingGuests.TabIndex = 2;
            lblEverythingGuests.Text = "Everything guests ask before their first visit to WildNest";

            // pnlWereHereToHelp
            pnlWereHereToHelp.Location = new System.Drawing.Point(900, 128);
            pnlWereHereToHelp.Name = "pnlWereHereToHelp";
            pnlWereHereToHelp.Size = new System.Drawing.Size(300, 256);
            pnlWereHereToHelp.TabIndex = 3;

            // pnlQuickHours
            pnlQuickHours.Location = new System.Drawing.Point(900, 398);
            pnlQuickHours.Name = "pnlQuickHours";
            pnlQuickHours.Size = new System.Drawing.Size(300, 230);
            pnlQuickHours.TabIndex = 4;

            // UcFaq  — NOTE: flpQuestions intentionally removed
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlQuickHours);
            Controls.Add(pnlWereHereToHelp);
            Controls.Add(lblEverythingGuests);
            Controls.Add(lblCommonQuestions);
            Controls.Add(lblFrequentlyAsked);
            Name = "UcFaq";
            Size = new System.Drawing.Size(1262, 760);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblFrequentlyAsked;
        private System.Windows.Forms.Label lblCommonQuestions;
        private System.Windows.Forms.Label lblEverythingGuests;
        private System.Windows.Forms.Panel pnlWereHereToHelp;
        private System.Windows.Forms.Panel pnlQuickHours;
    }
}