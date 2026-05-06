namespace Project.UcTourGuide
{
    partial class UcTourGuideGroups
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
            pnlGroups = new Panel();
            SuspendLayout();
            // 
            // pnlGroups
            // 
            pnlGroups.BackColor = Color.FromArgb(255, 128, 0);
            pnlGroups.Dock = DockStyle.Fill;
            pnlGroups.Location = new Point(0, 0);
            pnlGroups.Name = "pnlGroups";
            pnlGroups.Size = new Size(1262, 710);
            pnlGroups.TabIndex = 11;
            // 
            // UcTourGuideGroups
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlGroups);
            Name = "UcTourGuideGroups";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlGroups;
    }
}
