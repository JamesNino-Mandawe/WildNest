namespace Project.Booking
{
    partial class CabinStay
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
            pnlContent = new System.Windows.Forms.Panel();
            pnlCabin = new System.Windows.Forms.Panel();
            pnlGuestInfo = new System.Windows.Forms.Panel();
            pnlPayment = new System.Windows.Forms.Panel();
            pnlConfirm = new System.Windows.Forms.Panel();

            pnlContent.SuspendLayout();
            SuspendLayout();

            // ── pnlContent ────────────────────────────────────────
            pnlContent.BackColor = System.Drawing.Color.FromArgb(248, 244, 239);
            pnlContent.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlContent.Name = "pnlContent";
            pnlContent.TabIndex = 0;
            pnlContent.Controls.Add(pnlConfirm);
            pnlContent.Controls.Add(pnlPayment);
            pnlContent.Controls.Add(pnlGuestInfo);
            pnlContent.Controls.Add(pnlCabin);

            // ── Step panels ───────────────────────────────────────
            // Placed below the 80 px step-bar that BuildStepBar() creates at runtime.
            // They are sized to fill the remaining space via the Resize handler.
            const int STEPBAR_H = 80;

            foreach (var pnl in new[]
            { pnlCabin, pnlGuestInfo, pnlPayment, pnlConfirm })
            {
                pnl.Anchor = System.Windows.Forms.AnchorStyles.Left
                             | System.Windows.Forms.AnchorStyles.Right
                             | System.Windows.Forms.AnchorStyles.Top
                             | System.Windows.Forms.AnchorStyles.Bottom;
                pnl.Visible = false;
                pnl.Location = new System.Drawing.Point(0, STEPBAR_H);
                pnl.Size = new System.Drawing.Size(800, 600);
            }

            pnlCabin.Name = "pnlCabin"; pnlCabin.TabIndex = 0;
            pnlGuestInfo.Name = "pnlGuestInfo"; pnlGuestInfo.TabIndex = 1;
            pnlPayment.Name = "pnlPayment"; pnlPayment.TabIndex = 2;
            pnlConfirm.Name = "pnlConfirm"; pnlConfirm.TabIndex = 3;

            // Keep step panels sized to pnlContent minus the step-bar height
            pnlContent.Resize += (s, e) =>
            {
                int w = pnlContent.ClientSize.Width;
                int h = System.Math.Max(pnlContent.ClientSize.Height - STEPBAR_H, 100);
                foreach (var pnl in new[]
                { pnlCabin, pnlGuestInfo, pnlPayment, pnlConfirm })
                    pnl.Size = new System.Drawing.Size(w, h);
            };

            // ── CabinStay UserControl ─────────────────────────────
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(248, 244, 239);
            Controls.Add(pnlContent);
            Name = "CabinStay";
            // No fixed size — parent pnlLeft dictates via Dock.Fill

            pnlContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlContent;
        private System.Windows.Forms.Panel pnlCabin;
        private System.Windows.Forms.Panel pnlGuestInfo;
        private System.Windows.Forms.Panel pnlPayment;
        private System.Windows.Forms.Panel pnlConfirm;
    }
}