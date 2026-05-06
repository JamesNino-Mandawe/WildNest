namespace Project.UcZooKeeper
{
    partial class UcZookeeperFeeding
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
            pnlFeeding = new Panel();
            SuspendLayout();
            // 
            // pnlFeeding
            // 
            pnlFeeding.Dock = DockStyle.Fill;
            pnlFeeding.Location = new Point(0, 0);
            pnlFeeding.Name = "pnlFeeding";
            pnlFeeding.Size = new Size(1262, 710);
            pnlFeeding.TabIndex = 11;
            // 
            // UcZookeeperFeeding
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 128, 0);
            Controls.Add(pnlFeeding);
            Name = "UcZookeeperFeeding";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlFeeding;
    }
}
