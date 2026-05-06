namespace Project.Booking
{
    partial class DayVisit
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
            pnlConfirm = new Panel();
            pnlPayment = new Panel();
            pnlGuestInfo = new Panel();
            pnlVisitDetails = new Panel();
            pnlContent.SuspendLayout();
            SuspendLayout();
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.White;
            pnlContent.Controls.Add(pnlVisitDetails);
            pnlContent.Controls.Add(pnlGuestInfo);
            pnlContent.Controls.Add(pnlPayment);
            pnlContent.Controls.Add(pnlConfirm);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 0);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(845, 860);
            pnlContent.TabIndex = 7;
            // 
            // pnlConfirm
            // 
            pnlConfirm.Location = new Point(0, 0);
            pnlConfirm.Name = "pnlConfirm";
            pnlConfirm.Size = new Size(845, 860);
            pnlConfirm.TabIndex = 0;
            pnlConfirm.Visible = false;
            // 
            // pnlPayment
            // 
            pnlPayment.Location = new Point(0, 0);
            pnlPayment.Name = "pnlPayment";
            pnlPayment.Size = new Size(845, 860);
            pnlPayment.TabIndex = 1;
            pnlPayment.Visible = false;
            // 
            // pnlGuestInfo
            // 
            pnlGuestInfo.Location = new Point(0, 0);
            pnlGuestInfo.Name = "pnlGuestInfo";
            pnlGuestInfo.Size = new Size(845, 860);
            pnlGuestInfo.TabIndex = 2;
            pnlGuestInfo.Visible = false;
            // 
            // pnlVisitDetails
            // 
            pnlVisitDetails.Location = new Point(0, 0);
            pnlVisitDetails.Name = "pnlVisitDetails";
            pnlVisitDetails.Size = new Size(845, 860);
            pnlVisitDetails.TabIndex = 3;
            // 
            // DayVisit
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(pnlContent);
            Name = "DayVisit";
            Size = new Size(845, 860);
            pnlContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlContent;
        private Panel pnlConfirm;
        private Panel pnlVisitDetails;
        private Panel pnlGuestInfo;
        private Panel pnlPayment;
    }
}