namespace Project.UcAdministrator
{
    partial class UcAdminAnimals
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
            pnlAnimals = new Panel();
            SuspendLayout();
            // 
            // pnlAnimals
            // 
            pnlAnimals.BackColor = Color.FromArgb(255, 192, 192);
            pnlAnimals.Dock = DockStyle.Fill;
            pnlAnimals.Location = new Point(0, 0);
            pnlAnimals.Name = "pnlAnimals";
            pnlAnimals.Size = new Size(1262, 710);
            pnlAnimals.TabIndex = 2;
            // 
            // UcAdminAnimals
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlAnimals);
            Name = "UcAdminAnimals";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlAnimals;
    }
}
