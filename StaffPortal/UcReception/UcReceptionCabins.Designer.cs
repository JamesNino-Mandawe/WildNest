namespace Project.UcReception
{
    partial class UcReceptionCabins
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
            pnlCabins = new Panel();
            SuspendLayout();
            // 
            // pnlCabins
            // 
            pnlCabins.BackColor = Color.FromArgb(255, 192, 192);
            pnlCabins.Dock = DockStyle.Fill;
            pnlCabins.Location = new Point(0, 0);
            pnlCabins.Name = "pnlCabins";
            pnlCabins.Size = new Size(1262, 710);
            pnlCabins.TabIndex = 4;
            // 
            // UcReceptionCabins
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlCabins);
            Name = "UcReceptionCabins";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlCabins;
    }
}
