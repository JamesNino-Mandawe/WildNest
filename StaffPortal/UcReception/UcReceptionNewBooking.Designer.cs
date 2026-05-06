namespace Project.UcReception
{
    partial class UcReceptionNewBooking
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
            pnlNewBooking = new Panel();
            SuspendLayout();
            // 
            // pnlNewBooking
            // 
            pnlNewBooking.BackColor = Color.FromArgb(255, 255, 192);
            pnlNewBooking.Dock = DockStyle.Fill;
            pnlNewBooking.Location = new Point(0, 0);
            pnlNewBooking.Name = "pnlNewBooking";
            pnlNewBooking.Size = new Size(1262, 710);
            pnlNewBooking.TabIndex = 4;
            // 
            // UcReceptionNewBooking
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlNewBooking);
            Name = "UcReceptionNewBooking";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlNewBooking;
    }
}
