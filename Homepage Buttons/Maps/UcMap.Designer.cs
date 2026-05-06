namespace Project
{
    partial class UcMap
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            pnlMapContainer = new Panel();
            SuspendLayout();
            // 
            // pnlMapContainer
            // 
            pnlMapContainer.Dock = DockStyle.Fill;
            pnlMapContainer.Location = new Point(0, 0);
            pnlMapContainer.Name = "pnlMapContainer";
            pnlMapContainer.Size = new Size(1262, 710);
            pnlMapContainer.TabIndex = 0;
            // 
            // UcMap
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlMapContainer);
            Name = "UcMap";
            Size = new Size(1262, 710);
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlMapContainer;
    }
}