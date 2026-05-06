namespace Project.UcZooKeeper
{
    partial class UcZookeeperLog
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
            pnlLog = new Panel();
            SuspendLayout();
            // 
            // pnlLog
            // 
            pnlLog.Dock = DockStyle.Fill;
            pnlLog.Location = new Point(0, 0);
            pnlLog.Name = "pnlLog";
            pnlLog.Size = new Size(1262, 710);
            pnlLog.TabIndex = 11;
            // 
            // UcZookeeperLog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Olive;
            Controls.Add(pnlLog);
            Name = "UcZookeeperLog";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlLog;
    }
}
