namespace Project.UcTourGuide
{
    partial class UcTourGuideHistory
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlHistory = new Panel();
            SuspendLayout();
            // 
            // pnlHistory
            // 
            pnlHistory.BackColor = Color.Lime;
            pnlHistory.Dock = DockStyle.Fill;
            pnlHistory.Location = new Point(0, 0);
            pnlHistory.Name = "pnlHistory";
            pnlHistory.Size = new Size(1262, 710);
            pnlHistory.TabIndex = 11;
            // 
            // UcTourGuideHistory
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlHistory);
            Name = "UcTourGuideHistory";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHistory;
    }
}
