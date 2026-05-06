namespace Project.UcReception
{
    partial class UcReceptionEncounters
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
            pnlEncounters = new Panel();
            SuspendLayout();
            // 
            // pnlEncounters
            // 
            pnlEncounters.BackColor = Color.FromArgb(192, 192, 255);
            pnlEncounters.Dock = DockStyle.Fill;
            pnlEncounters.Location = new Point(0, 0);
            pnlEncounters.Name = "pnlEncounters";
            pnlEncounters.Size = new Size(1262, 710);
            pnlEncounters.TabIndex = 4;
            // 
            // UcReceptionEncounters
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlEncounters);
            Name = "UcReceptionEncounters";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlEncounters;
    }
}
