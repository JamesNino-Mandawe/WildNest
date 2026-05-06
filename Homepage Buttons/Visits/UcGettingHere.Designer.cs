namespace Project.Homepage_Buttons.Visits
{
    partial class UcGettingHere
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            lblDirections = new System.Windows.Forms.Label();
            lblGettingToWildNest = new System.Windows.Forms.Label();
            lblLocation = new System.Windows.Forms.Label();
            pnlPrivateCar = new System.Windows.Forms.Panel();
            pnlPublicTransport = new System.Windows.Forms.Panel();
            pnlAddress = new System.Windows.Forms.Panel();
            SuspendLayout();

            // lblDirections
            lblDirections.AutoSize = true;
            lblDirections.Location = new System.Drawing.Point(60, 22);
            lblDirections.Name = "lblDirections";
            lblDirections.Size = new System.Drawing.Size(76, 20);
            lblDirections.TabIndex = 0;
            lblDirections.Text = "DIRECTIONS";

            // lblGettingToWildNest
            lblGettingToWildNest.AutoSize = false;
            lblGettingToWildNest.Location = new System.Drawing.Point(60, 46);
            lblGettingToWildNest.Name = "lblGettingToWildNest";
            lblGettingToWildNest.Size = new System.Drawing.Size(500, 36);
            lblGettingToWildNest.TabIndex = 1;
            lblGettingToWildNest.Text = "Getting to WildNest";

            // lblLocation
            lblLocation.AutoSize = false;
            lblLocation.Location = new System.Drawing.Point(60, 86);
            lblLocation.Name = "lblLocation";
            lblLocation.Size = new System.Drawing.Size(600, 20);
            lblLocation.TabIndex = 2;
            lblLocation.Text = "Located in Carmen, North Cebu — approximately 45 minutes from Cebu City";

            // pnlPrivateCar
            pnlPrivateCar.Location = new System.Drawing.Point(60, 120);
            pnlPrivateCar.Name = "pnlPrivateCar";
            pnlPrivateCar.Size = new System.Drawing.Size(480, 210);
            pnlPrivateCar.TabIndex = 3;

            // pnlPublicTransport
            pnlPublicTransport.Location = new System.Drawing.Point(60, 348);
            pnlPublicTransport.Name = "pnlPublicTransport";
            pnlPublicTransport.Size = new System.Drawing.Size(480, 210);
            pnlPublicTransport.TabIndex = 4;

            // pnlAddress
            pnlAddress.Location = new System.Drawing.Point(558, 120);
            pnlAddress.Name = "pnlAddress";
            pnlAddress.Size = new System.Drawing.Size(380, 446);
            pnlAddress.TabIndex = 5;

            // UcGettingHere
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlAddress);
            Controls.Add(pnlPublicTransport);
            Controls.Add(pnlPrivateCar);
            Controls.Add(lblLocation);
            Controls.Add(lblGettingToWildNest);
            Controls.Add(lblDirections);
            Name = "UcGettingHere";
            Size = new System.Drawing.Size(1262, 710);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblDirections;
        private System.Windows.Forms.Label lblGettingToWildNest;
        private System.Windows.Forms.Label lblLocation;
        private System.Windows.Forms.Panel pnlPrivateCar;
        private System.Windows.Forms.Panel pnlPublicTransport;
        private System.Windows.Forms.Panel pnlAddress;
    }
}