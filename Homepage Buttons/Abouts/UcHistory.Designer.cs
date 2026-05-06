namespace Project.Homepage_Buttons.Abouts
{
    partial class UcHistory
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
            lblSectionTag = new System.Windows.Forms.Label();
            lblTitle = new System.Windows.Forms.Label();
            lblSubtitle = new System.Windows.Forms.Label();
            flpTimeline = new System.Windows.Forms.Panel();
            SuspendLayout();

            // lblSectionTag
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new System.Drawing.Point(56, 30);
            lblSectionTag.Name = "lblSectionTag";
            lblSectionTag.TabIndex = 0;
            lblSectionTag.Text = "Our History";

            // lblTitle
            lblTitle.AutoSize = true;
            lblTitle.Location = new System.Drawing.Point(54, 56);
            lblTitle.Name = "lblTitle";
            lblTitle.TabIndex = 1;
            lblTitle.Text = "Three Decades of Conservation";

            // lblSubtitle
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new System.Drawing.Point(56, 96);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.TabIndex = 2;
            lblSubtitle.Text = "From a single rescued eagle to Southeast Asia's leading wildlife sanctuary";

            // flpTimeline
            flpTimeline.Location = new System.Drawing.Point(56, 126);
            flpTimeline.Name = "flpTimeline";
            flpTimeline.Size = new System.Drawing.Size(1150, 566);
            flpTimeline.TabIndex = 3;

            // UcHistory
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(flpTimeline);
            Controls.Add(lblSubtitle);
            Controls.Add(lblTitle);
            Controls.Add(lblSectionTag);
            Name = "UcHistory";
            Size = new System.Drawing.Size(1262, 710);
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Label lblSectionTag;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel flpTimeline;
    }
}