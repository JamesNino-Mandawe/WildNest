namespace Project
{
    partial class StaffDashboard
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlTopBar = new Panel();
            btnRoleTG = new Button();
            btnRoleZK = new Button();
            btnRoleFD = new Button();
            btnRoleAdmin = new Button();
            pnlSidebar = new Panel();
            pnlContent = new Panel();
            pnlTopBar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTopBar
            // 
            pnlTopBar.Controls.Add(btnRoleTG);
            pnlTopBar.Controls.Add(btnRoleZK);
            pnlTopBar.Controls.Add(btnRoleFD);
            pnlTopBar.Controls.Add(btnRoleAdmin);
            pnlTopBar.Dock = DockStyle.Top;
            pnlTopBar.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            pnlTopBar.Location = new Point(0, 0);
            pnlTopBar.Name = "pnlTopBar";
            pnlTopBar.Size = new Size(1262, 86);
            pnlTopBar.TabIndex = 0;
            // 
            // btnRoleTG
            // 
            btnRoleTG.FlatStyle = FlatStyle.Flat;
            btnRoleTG.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            btnRoleTG.Location = new Point(694, 12);
            btnRoleTG.Name = "btnRoleTG";
            btnRoleTG.Size = new Size(152, 50);
            btnRoleTG.TabIndex = 4;
            btnRoleTG.Text = "Tour Guide";
            btnRoleTG.UseVisualStyleBackColor = true;
            // 
            // btnRoleZK
            // 
            btnRoleZK.FlatStyle = FlatStyle.Flat;
            btnRoleZK.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            btnRoleZK.Location = new Point(536, 12);
            btnRoleZK.Name = "btnRoleZK";
            btnRoleZK.Size = new Size(152, 50);
            btnRoleZK.TabIndex = 2;
            btnRoleZK.Text = "Zoo Keeper";
            btnRoleZK.UseVisualStyleBackColor = true;
            // 
            // btnRoleFD
            // 
            btnRoleFD.FlatStyle = FlatStyle.Flat;
            btnRoleFD.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            btnRoleFD.Location = new Point(378, 12);
            btnRoleFD.Name = "btnRoleFD";
            btnRoleFD.Size = new Size(152, 50);
            btnRoleFD.TabIndex = 1;
            btnRoleFD.Text = "Reception";
            btnRoleFD.UseVisualStyleBackColor = true;
            // 
            // btnRoleAdmin
            // 
            btnRoleAdmin.FlatStyle = FlatStyle.Flat;
            btnRoleAdmin.Font = new Font("Georgia", 10.2F, FontStyle.Bold);
            btnRoleAdmin.Location = new Point(220, 12);
            btnRoleAdmin.Name = "btnRoleAdmin";
            btnRoleAdmin.Size = new Size(152, 50);
            btnRoleAdmin.TabIndex = 0;
            btnRoleAdmin.Text = "Manager";
            btnRoleAdmin.UseVisualStyleBackColor = true;
            // 
            // pnlSidebar
            // 
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Location = new Point(0, 86);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(200, 587);
            pnlSidebar.TabIndex = 1;
            // 
            // pnlContent
            // 
            pnlContent.Location = new Point(206, 94);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(1044, 567);
            pnlContent.TabIndex = 2;
            // 
            // StaffDashboard
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1262, 673);
            Controls.Add(pnlContent);
            Controls.Add(pnlSidebar);
            Controls.Add(pnlTopBar);
            FormBorderStyle = FormBorderStyle.None;
            Name = "StaffDashboard";
            StartPosition = FormStartPosition.CenterParent;
            Text = "StaffDashboard";
            pnlTopBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlTopBar;
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Button btnRoleTG;
        private Button btnRoleZK;
        private Button btnRoleFD;
        private Button btnRoleAdmin;
    }
}
