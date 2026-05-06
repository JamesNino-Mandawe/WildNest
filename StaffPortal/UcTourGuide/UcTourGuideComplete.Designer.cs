namespace Project.UcTourGuide
{
    partial class UcTourGuideComplete
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
            pnlComplete = new Panel();
            SuspendLayout();
            // 
            // pnlComplete
            // 
            pnlComplete.Dock = DockStyle.Fill;
            pnlComplete.Location = new Point(0, 0);
            pnlComplete.Name = "pnlComplete";
            pnlComplete.Size = new Size(1262, 710);
            pnlComplete.TabIndex = 11;
            // 
            // UcTourGuideComplete
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Cyan;
            Controls.Add(pnlComplete);
            Name = "UcTourGuideComplete";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlComplete;
    }
}
