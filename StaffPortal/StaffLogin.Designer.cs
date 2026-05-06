namespace Project
{
    partial class StaffLogin
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
            picCardLogo = new PictureBox();
            lblBrandCard = new Label();
            lblSubCard = new Label();
            txtUsername = new TextBox();
            txtPassword = new TextBox();
            btnLogin = new Button();
            lblUserTag = new Label();
            lblPassTag = new Label();
            cmbRole = new ComboBox();
            lblRoleTag = new Label();
            ((System.ComponentModel.ISupportInitialize)picCardLogo).BeginInit();
            SuspendLayout();
            // 
            // picCardLogo
            // 
            picCardLogo.Location = new Point(198, 12);
            picCardLogo.Name = "picCardLogo";
            picCardLogo.Size = new Size(65, 61);
            picCardLogo.TabIndex = 0;
            picCardLogo.TabStop = false;
            // 
            // lblBrandCard
            // 
            lblBrandCard.AutoSize = true;
            lblBrandCard.Font = new Font("Georgia", 15F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblBrandCard.Location = new Point(130, 76);
            lblBrandCard.Name = "lblBrandCard";
            lblBrandCard.Size = new Size(204, 30);
            lblBrandCard.TabIndex = 1;
            lblBrandCard.Text = "W I L D N E S T";
            // 
            // lblSubCard
            // 
            lblSubCard.AutoSize = true;
            lblSubCard.Font = new Font("Georgia", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSubCard.Location = new Point(38, 106);
            lblSubCard.Name = "lblSubCard";
            lblSubCard.Size = new Size(381, 20);
            lblSubCard.TabIndex = 2;
            lblSubCard.Text = "STAFF PORTAL · RESORT MANAGEMENT";
            // 
            // txtUsername
            // 
            txtUsername.Location = new Point(38, 204);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(364, 27);
            txtUsername.TabIndex = 3;
            // 
            // txtPassword
            // 
            txtPassword.Location = new Point(38, 269);
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(364, 27);
            txtPassword.TabIndex = 4;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(130, 444);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(184, 45);
            btnLogin.TabIndex = 5;
            btnLogin.Text = "SIGN IN TO PORTAL";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += BtnLogin_Click;
            // 
            // lblUserTag
            // 
            lblUserTag.AutoSize = true;
            lblUserTag.Location = new Point(38, 181);
            lblUserTag.Name = "lblUserTag";
            lblUserTag.Size = new Size(189, 20);
            lblUserTag.TabIndex = 7;
            lblUserTag.Text = "EMPLOYEE ID / USERNAME";
            // 
            // lblPassTag
            // 
            lblPassTag.AutoSize = true;
            lblPassTag.Location = new Point(38, 246);
            lblPassTag.Name = "lblPassTag";
            lblPassTag.Size = new Size(87, 20);
            lblPassTag.TabIndex = 8;
            lblPassTag.Text = "PASSWORD";
            // 
            // cmbRole
            // 
            cmbRole.FormattingEnabled = true;
            cmbRole.Location = new Point(38, 332);
            cmbRole.Name = "cmbRole";
            cmbRole.Size = new Size(364, 28);
            cmbRole.TabIndex = 9;
            // 
            // lblRoleTag
            // 
            lblRoleTag.AutoSize = true;
            lblRoleTag.Location = new Point(38, 309);
            lblRoleTag.Name = "lblRoleTag";
            lblRoleTag.Size = new Size(174, 20);
            lblRoleTag.TabIndex = 10;
            lblRoleTag.Text = "ASSIGNED ACCESS ROLE";
            // 
            // StaffLogin
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(450, 600);
            Controls.Add(lblRoleTag);
            Controls.Add(cmbRole);
            Controls.Add(lblPassTag);
            Controls.Add(lblUserTag);
            Controls.Add(btnLogin);
            Controls.Add(txtPassword);
            Controls.Add(txtUsername);
            Controls.Add(lblSubCard);
            Controls.Add(lblBrandCard);
            Controls.Add(picCardLogo);
            FormBorderStyle = FormBorderStyle.None;
            Name = "StaffLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "StaffLogin";
            ((System.ComponentModel.ISupportInitialize)picCardLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox picCardLogo;
        private Label lblBrandCard;
        private Label lblSubCard;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblUserTag;
        private Label lblPassTag;
        private ComboBox cmbRole;
        private Label lblRoleTag;
    }
}