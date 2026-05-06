namespace Project.Homepage_Buttons.Abouts
{
    partial class UcTeam
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
            lblSubtitle = new System.Windows.Forms.Label();
            lblTitle = new System.Windows.Forms.Label();
            lblSectionTag = new System.Windows.Forms.Label();
            tlpTeam = new System.Windows.Forms.TableLayoutPanel();
            SuspendLayout();

            // lblSectionTag
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new System.Drawing.Point(56, 30);
            lblSectionTag.Name = "lblSectionTag";
            lblSectionTag.TabIndex = 11;
            lblSectionTag.Text = "Our People";

            // lblTitle
            lblTitle.AutoSize = true;
            lblTitle.Location = new System.Drawing.Point(54, 56);
            lblTitle.Name = "lblTitle";
            lblTitle.TabIndex = 12;
            lblTitle.Text = "The Rangers Behind the Sanctuary";

            // lblSubtitle
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new System.Drawing.Point(56, 96);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.TabIndex = 13;
            lblSubtitle.Text = "Certified wildlife professionals — most of them from Carmen itself";

            // tlpTeam
            tlpTeam.ColumnCount = 4;
            tlpTeam.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlpTeam.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlpTeam.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlpTeam.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tlpTeam.Location = new System.Drawing.Point(56, 126);
            tlpTeam.Name = "tlpTeam";
            tlpTeam.RowCount = 2;
            tlpTeam.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tlpTeam.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tlpTeam.Size = new System.Drawing.Size(1150, 430);
            tlpTeam.TabIndex = 14;

            // UcTeam
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tlpTeam);
            Controls.Add(lblSubtitle);
            Controls.Add(lblTitle);
            Controls.Add(lblSectionTag);
            Name = "UcTeam";
            Size = new System.Drawing.Size(1262, 710);
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion

        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSectionTag;
        private System.Windows.Forms.TableLayoutPanel tlpTeam;
    }
}