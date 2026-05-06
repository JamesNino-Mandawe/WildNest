namespace Project.UcAdministrator
{
    partial class UcAdminReservations
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
            pnlReservations = new Panel();
            SuspendLayout();
            // 
            // pnlReservations
            // 
            pnlReservations.Dock = DockStyle.Fill;
            pnlReservations.Location = new Point(0, 0);
            pnlReservations.Name = "pnlReservations";
            pnlReservations.Size = new Size(1262, 710);
            pnlReservations.TabIndex = 3;
            // 
            // UcAdminReservations
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 192, 255);
            Controls.Add(pnlReservations);
            Name = "UcAdminReservations";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlReservations;
    }
}
