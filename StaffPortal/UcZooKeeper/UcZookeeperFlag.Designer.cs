namespace Project.UcZooKeeper
{
    partial class UcZookeeperFlag
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
            pnlFlag = new Panel();
            SuspendLayout();
            // 
            // pnlFlag
            // 
            pnlFlag.BackColor = Color.FromArgb(128, 255, 128);
            pnlFlag.Dock = DockStyle.Fill;
            pnlFlag.Location = new Point(0, 0);
            pnlFlag.Name = "pnlFlag";
            pnlFlag.Size = new Size(1262, 710);
            pnlFlag.TabIndex = 10;
            // 
            // UcZookeeperFlag
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlFlag);
            Name = "UcZookeeperFlag";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlFlag;
    }
}
