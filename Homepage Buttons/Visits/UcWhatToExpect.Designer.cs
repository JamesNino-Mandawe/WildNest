namespace Project.Homepage_Buttons.Visits
{
    partial class UcWhatToExpect
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            lblWhatToExpectInside = new System.Windows.Forms.Label();
            lblVisitorGuide = new System.Windows.Forms.Label();
            lblTipsGuidelines = new System.Windows.Forms.Label();
            tlpExpectCards = new System.Windows.Forms.TableLayoutPanel();
            SuspendLayout();

            // lblVisitorGuide
            lblVisitorGuide.AutoSize = true;
            lblVisitorGuide.Location = new System.Drawing.Point(53, 22);
            lblVisitorGuide.Name = "lblVisitorGuide";
            lblVisitorGuide.Size = new System.Drawing.Size(94, 20);
            lblVisitorGuide.TabIndex = 1;
            lblVisitorGuide.Text = "VISITOR GUIDE";

            // lblWhatToExpectInside
            lblWhatToExpectInside.AutoSize = false;
            lblWhatToExpectInside.Location = new System.Drawing.Point(53, 46);
            lblWhatToExpectInside.Name = "lblWhatToExpectInside";
            lblWhatToExpectInside.Size = new System.Drawing.Size(520, 36);
            lblWhatToExpectInside.TabIndex = 0;
            lblWhatToExpectInside.Text = "What to Expect Inside";

            // lblTipsGuidelines
            lblTipsGuidelines.AutoSize = false;
            lblTipsGuidelines.Location = new System.Drawing.Point(53, 86);
            lblTipsGuidelines.Name = "lblTipsGuidelines";
            lblTipsGuidelines.Size = new System.Drawing.Size(620, 20);
            lblTipsGuidelines.TabIndex = 2;
            lblTipsGuidelines.Text = "Tips and guidelines to make the most of your WildNest experience";

            // tlpExpectCards
            tlpExpectCards.ColumnCount = 3;
            tlpExpectCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tlpExpectCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tlpExpectCards.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            tlpExpectCards.Location = new System.Drawing.Point(53, 130);
            tlpExpectCards.Name = "tlpExpectCards";
            tlpExpectCards.RowCount = 2;
            tlpExpectCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tlpExpectCards.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tlpExpectCards.Size = new System.Drawing.Size(1156, 540);
            tlpExpectCards.TabIndex = 3;
            tlpExpectCards.BackColor = System.Drawing.Color.Transparent;

            // UcWhatToExpect
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tlpExpectCards);
            Controls.Add(lblTipsGuidelines);
            Controls.Add(lblWhatToExpectInside);
            Controls.Add(lblVisitorGuide);
            Name = "UcWhatToExpect";
            Size = new System.Drawing.Size(1262, 710);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblWhatToExpectInside;
        private System.Windows.Forms.Label lblVisitorGuide;
        private System.Windows.Forms.Label lblTipsGuidelines;
        private System.Windows.Forms.TableLayoutPanel tlpExpectCards;
    }
}