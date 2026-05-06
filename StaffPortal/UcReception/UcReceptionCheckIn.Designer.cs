namespace Project.UcReception
{
    partial class UcReceptionCheckIn
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
            pnlCheckIn = new Panel();
            SuspendLayout();
            // 
            // pnlCheckIn
            // 
            pnlCheckIn.Dock = DockStyle.Fill;
            pnlCheckIn.Location = new Point(0, 0);
            pnlCheckIn.Name = "pnlCheckIn";
            pnlCheckIn.Size = new Size(1262, 710);
            pnlCheckIn.TabIndex = 4;
            // 
            // UcReceptionCheckIn
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 224, 192);
            Controls.Add(pnlCheckIn);
            Name = "UcReceptionCheckIn";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlCheckIn;
    }
}
