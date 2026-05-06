namespace Project.Booking
{
    partial class ExperienceVisit
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
            pnlContent = new Panel();
            pnlExperiences = new Panel();
            pnlSchedule = new Panel();
            pnlGuestInfo = new Panel();
            pnlPayment = new Panel();
            pnlConfirm = new Panel();
            pnlContent.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.White;
            pnlContent.Controls.Add(pnlExperiences);
            pnlContent.Controls.Add(pnlSchedule);
            pnlContent.Controls.Add(pnlGuestInfo);
            pnlContent.Controls.Add(pnlPayment);
            pnlContent.Controls.Add(pnlConfirm);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 0);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(845, 860);
            pnlContent.TabIndex = 7;
            // 
            // pnlExperiences
            // 
            pnlExperiences.Location = new Point(0, 0);
            pnlExperiences.Name = "pnlExperiences";
            pnlExperiences.Size = new Size(845, 860);
            pnlExperiences.TabIndex = 7;
            // 
            // pnlSchedule
            // 
            pnlSchedule.Location = new Point(0, 0);
            pnlSchedule.Name = "pnlSchedule";
            pnlSchedule.Size = new Size(845, 860);
            pnlSchedule.TabIndex = 6;
            pnlSchedule.Visible = false;
            // 
            // pnlGuestInfo
            // 
            pnlGuestInfo.Location = new Point(0, 0);
            pnlGuestInfo.Name = "pnlGuestInfo";
            pnlGuestInfo.Size = new Size(845, 860);
            pnlGuestInfo.TabIndex = 5;
            pnlGuestInfo.Visible = false;
            // 
            // pnlPayment
            // 
            pnlPayment.Location = new Point(0, 0);
            pnlPayment.Name = "pnlPayment";
            pnlPayment.Size = new Size(845, 860);
            pnlPayment.TabIndex = 4;
            pnlPayment.Visible = false;
            // 
            // pnlConfirm
            // 
            pnlConfirm.Location = new Point(0, 0);
            pnlConfirm.Name = "pnlConfirm";
            pnlConfirm.Size = new Size(845, 860);
            pnlConfirm.TabIndex = 3;
            pnlConfirm.Visible = false;
            // 
            // ExperienceVisit
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(pnlContent);
            Name = "ExperienceVisit";
            Size = new Size(845, 860);
            pnlContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlContent;
        private Panel pnlExperiences;
        private Panel pnlSchedule;
        private Panel pnlGuestInfo;
        private Panel pnlPayment;
        private Panel pnlConfirm;
    }
}