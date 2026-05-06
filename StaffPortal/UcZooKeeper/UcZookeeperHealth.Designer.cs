namespace Project.UcZooKeeper
{
    partial class UcZookeeperHealth
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
            pnlHealth = new Panel();
            SuspendLayout();
            // 
            // pnlHealth
            // 
            pnlHealth.BackColor = Color.FromArgb(255, 128, 255);
            pnlHealth.Dock = DockStyle.Fill;
            pnlHealth.Location = new Point(0, 0);
            pnlHealth.Name = "pnlHealth";
            pnlHealth.Size = new Size(1262, 710);
            pnlHealth.TabIndex = 10;
            // 
            // UcZookeeperHealth
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlHealth);
            Name = "UcZookeeperHealth";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHealth;
    }
}
