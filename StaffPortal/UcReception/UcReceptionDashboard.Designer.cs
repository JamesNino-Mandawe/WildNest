namespace Project.UcReception
{
    partial class UcReceptionDashboard
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
            pnlReceptionContent = new Panel();
            pnlReceptionSidebar = new Panel();
            btnSignOut = new Button();
            btnNavEncounters = new Button();
            btnNavCabins = new Button();
            btnNavGuests = new Button();
            btnNavCheckOut = new Button();
            btnNavCheckIn = new Button();
            btnNavNewBooking = new Button();
            btnNavDashboard = new Button();
            lblSideUser = new Label();
            lblSideRole = new Label();
            lblSideTitle = new Label();
            pnlReceptionSidebar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlReceptionContent
            // 
            pnlReceptionContent.BackColor = Color.Blue;
            pnlReceptionContent.Dock = DockStyle.Fill;
            pnlReceptionContent.ForeColor = Color.Black;
            pnlReceptionContent.Location = new Point(269, 0);
            pnlReceptionContent.Name = "pnlReceptionContent";
            pnlReceptionContent.Size = new Size(993, 710);
            pnlReceptionContent.TabIndex = 3;
            // 
            // pnlReceptionSidebar
            // 
            pnlReceptionSidebar.Controls.Add(btnSignOut);
            pnlReceptionSidebar.Controls.Add(btnNavEncounters);
            pnlReceptionSidebar.Controls.Add(btnNavCabins);
            pnlReceptionSidebar.Controls.Add(btnNavGuests);
            pnlReceptionSidebar.Controls.Add(btnNavCheckOut);
            pnlReceptionSidebar.Controls.Add(btnNavCheckIn);
            pnlReceptionSidebar.Controls.Add(btnNavNewBooking);
            pnlReceptionSidebar.Controls.Add(btnNavDashboard);
            pnlReceptionSidebar.Controls.Add(lblSideUser);
            pnlReceptionSidebar.Controls.Add(lblSideRole);
            pnlReceptionSidebar.Controls.Add(lblSideTitle);
            pnlReceptionSidebar.Dock = DockStyle.Left;
            pnlReceptionSidebar.ForeColor = Color.Transparent;
            pnlReceptionSidebar.Location = new Point(0, 0);
            pnlReceptionSidebar.Name = "pnlReceptionSidebar";
            pnlReceptionSidebar.Size = new Size(269, 710);
            pnlReceptionSidebar.TabIndex = 2;
            // 
            // btnSignOut
            // 
            btnSignOut.ForeColor = Color.Black;
            btnSignOut.Location = new Point(19, 645);
            btnSignOut.Name = "btnSignOut";
            btnSignOut.Size = new Size(231, 51);
            btnSignOut.TabIndex = 4;
            btnSignOut.Text = "Sign Out";
            btnSignOut.UseVisualStyleBackColor = true;
            // 
            // btnNavEncounters
            // 
            btnNavEncounters.ForeColor = Color.Black;
            btnNavEncounters.Location = new Point(19, 438);
            btnNavEncounters.Name = "btnNavEncounters";
            btnNavEncounters.Size = new Size(231, 51);
            btnNavEncounters.TabIndex = 9;
            btnNavEncounters.Text = "Book Encounter";
            btnNavEncounters.UseVisualStyleBackColor = true;
            // 
            // btnNavCabins
            // 
            btnNavCabins.ForeColor = Color.Black;
            btnNavCabins.Location = new Point(19, 381);
            btnNavCabins.Name = "btnNavCabins";
            btnNavCabins.Size = new Size(231, 51);
            btnNavCabins.TabIndex = 8;
            btnNavCabins.Text = "Cabin Availability";
            btnNavCabins.UseVisualStyleBackColor = true;
            // 
            // btnNavGuests
            // 
            btnNavGuests.ForeColor = Color.Black;
            btnNavGuests.Location = new Point(19, 324);
            btnNavGuests.Name = "btnNavGuests";
            btnNavGuests.Size = new Size(231, 51);
            btnNavGuests.TabIndex = 7;
            btnNavGuests.Text = "Guest Profiles";
            btnNavGuests.UseVisualStyleBackColor = true;
            // 
            // btnNavCheckOut
            // 
            btnNavCheckOut.ForeColor = Color.Black;
            btnNavCheckOut.Location = new Point(19, 267);
            btnNavCheckOut.Name = "btnNavCheckOut";
            btnNavCheckOut.Size = new Size(231, 51);
            btnNavCheckOut.TabIndex = 6;
            btnNavCheckOut.Text = "Check-Out and Billing";
            btnNavCheckOut.UseVisualStyleBackColor = true;
            // 
            // btnNavCheckIn
            // 
            btnNavCheckIn.ForeColor = Color.Black;
            btnNavCheckIn.Location = new Point(19, 210);
            btnNavCheckIn.Name = "btnNavCheckIn";
            btnNavCheckIn.Size = new Size(231, 51);
            btnNavCheckIn.TabIndex = 5;
            btnNavCheckIn.Text = "Check-In";
            btnNavCheckIn.UseVisualStyleBackColor = true;
            // 
            // btnNavNewBooking
            // 
            btnNavNewBooking.ForeColor = Color.Black;
            btnNavNewBooking.Location = new Point(19, 153);
            btnNavNewBooking.Name = "btnNavNewBooking";
            btnNavNewBooking.Size = new Size(231, 51);
            btnNavNewBooking.TabIndex = 4;
            btnNavNewBooking.Text = "New Booking";
            btnNavNewBooking.UseVisualStyleBackColor = true;
            // 
            // btnNavDashboard
            // 
            btnNavDashboard.ForeColor = Color.Black;
            btnNavDashboard.Location = new Point(19, 96);
            btnNavDashboard.Name = "btnNavDashboard";
            btnNavDashboard.Size = new Size(231, 51);
            btnNavDashboard.TabIndex = 3;
            btnNavDashboard.Text = "DashBoard";
            btnNavDashboard.UseVisualStyleBackColor = true;
            // 
            // lblSideUser
            // 
            lblSideUser.AutoSize = true;
            lblSideUser.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            lblSideUser.ForeColor = Color.Black;
            lblSideUser.Location = new Point(78, 56);
            lblSideUser.Name = "lblSideUser";
            lblSideUser.Size = new Size(68, 20);
            lblSideUser.TabIndex = 2;
            lblSideUser.Text = "admin";
            // 
            // lblSideRole
            // 
            lblSideRole.AutoSize = true;
            lblSideRole.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            lblSideRole.ForeColor = Color.Black;
            lblSideRole.Location = new Point(19, 36);
            lblSideRole.Name = "lblSideRole";
            lblSideRole.Size = new Size(214, 20);
            lblSideRole.TabIndex = 1;
            lblSideRole.Text = "Front Desk Operations";
            // 
            // lblSideTitle
            // 
            lblSideTitle.AutoSize = true;
            lblSideTitle.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            lblSideTitle.ForeColor = Color.Black;
            lblSideTitle.Location = new Point(62, 16);
            lblSideTitle.Name = "lblSideTitle";
            lblSideTitle.Size = new Size(112, 20);
            lblSideTitle.TabIndex = 0;
            lblSideTitle.Text = "WILDNEST";
            // 
            // UcReceptionDashboard
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlReceptionContent);
            Controls.Add(pnlReceptionSidebar);
            Name = "UcReceptionDashboard";
            Size = new Size(1262, 710);
            pnlReceptionSidebar.ResumeLayout(false);
            pnlReceptionSidebar.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlReceptionContent;
        private Panel pnlReceptionSidebar;
        private Button btnSignOut;
        private Button btnNavEncounters;
        private Button btnNavCabins;
        private Button btnNavGuests;
        private Button btnNavCheckOut;
        private Button btnNavCheckIn;
        private Button btnNavNewBooking;
        private Button btnNavDashboard;
        private Label lblSideUser;
        private Label lblSideRole;
        private Label lblSideTitle;
    }
}
