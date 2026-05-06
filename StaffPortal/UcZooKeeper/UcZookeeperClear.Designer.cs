namespace Project.UcZooKeeper
{
    partial class UcZookeeperClear
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
            pnlClear = new Panel();
            SuspendLayout();
            // 
            // pnlClear
            // 
            pnlClear.Dock = DockStyle.Fill;
            pnlClear.Location = new Point(0, 0);
            pnlClear.Name = "pnlClear";
            pnlClear.Size = new Size(1262, 710);
            pnlClear.TabIndex = 10;
            // 
            // UcZookeeperClear
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Lime;
            Controls.Add(pnlClear);
            Name = "UcZookeeperClear";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlClear;
    }
}
