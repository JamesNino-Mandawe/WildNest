namespace Project.UcAdministrator
{
    partial class UcAdminBills
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
            pnlBills = new Panel();
            SuspendLayout();
            // 
            // pnlBills
            // 
            pnlBills.Dock = DockStyle.Fill;
            pnlBills.Location = new Point(0, 0);
            pnlBills.Name = "pnlBills";
            pnlBills.Size = new Size(1262, 710);
            pnlBills.TabIndex = 2;
            // 
            // UcAdminBills
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 224, 192);
            Controls.Add(pnlBills);
            Name = "UcAdminBills";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlBills;
    }
}
