namespace Project
{
    partial class SplashScreen
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblTitle = new Label();
            lblTagline = new Label();
            lblPercentage = new Label();
            lblStatus = new Label();
            lblVersion = new Label();
            pbLoading = new ProgressBar();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Georgia", 48F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.Location = new Point(112, 86);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(708, 145);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "W I L D N E S T";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTagline
            // 
            lblTagline.Font = new Font("Georgia", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTagline.Location = new Point(277, 192);
            lblTagline.Name = "lblTagline";
            lblTagline.Size = new Size(451, 74);
            lblTagline.TabIndex = 1;
            lblTagline.Text = "ZOO RESORT & WILDLIFE EXPERIENCE";
            lblTagline.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblPercentage
            // 
            lblPercentage.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblPercentage.Location = new Point(349, 317);
            lblPercentage.Name = "lblPercentage";
            lblPercentage.Size = new Size(86, 74);
            lblPercentage.TabIndex = 3;
            lblPercentage.Text = "0%";
            lblPercentage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Arial Narrow", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(349, 253);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(246, 74);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "LOADING CABIN RECORDS...";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblVersion
            // 
            lblVersion.Font = new Font("Arial", 9F, FontStyle.Bold);
            lblVersion.Location = new Point(500, 317);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(99, 74);
            lblVersion.TabIndex = 4;
            lblVersion.Text = "v 1.0.0";
            lblVersion.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pbLoading
            // 
            pbLoading.Location = new Point(409, 317);
            pbLoading.Name = "pbLoading";
            pbLoading.Size = new Size(125, 29);
            pbLoading.TabIndex = 5;
            // 
            // SplashScreen
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1000, 600);
            Controls.Add(pbLoading);
            Controls.Add(lblVersion);
            Controls.Add(lblPercentage);
            Controls.Add(lblStatus);
            Controls.Add(lblTagline);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SplashScreen";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ResumeLayout(false);
        }

        #endregion

        private Label lblTitle;
        private Label lblTagline;
        private Label lblPercentage;
        private Label lblStatus;
        private Label lblVersion;
        private ProgressBar pbLoading;
    }
}
