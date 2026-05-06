namespace Project
{
    partial class MyAccomodation
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // All controls declared — fully built in code-behind (StyleXxx methods)
            // Designer only wires up the shell; runtime painting handles visuals.

            SuspendLayout();

            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(520, 820);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Name = "MyAccomodation";
            Text = "WildNest — My Accommodation";
            DoubleBuffered = true;

            ResumeLayout(false);
        }
    }
}