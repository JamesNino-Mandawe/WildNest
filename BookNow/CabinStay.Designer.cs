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
            pnlCabin.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            pnlCabin.Visible = false;
            pnlCabin.Location = new System.Drawing.Point(0, 80);
            pnlCabin.Size = new System.Drawing.Size(800, 600);
            pnlCabin.Name = "pnlCabin";
            pnlCabin.TabIndex = 0;

            pnlGuestInfo.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            pnlGuestInfo.Visible = false;
            pnlGuestInfo.Location = new System.Drawing.Point(0, 80);
            pnlGuestInfo.Size = new System.Drawing.Size(800, 600);
            pnlGuestInfo.Name = "pnlGuestInfo";
            pnlGuestInfo.TabIndex = 1;

            pnlPayment.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            pnlPayment.Visible = false;
            pnlPayment.Location = new System.Drawing.Point(0, 80);
            pnlPayment.Size = new System.Drawing.Size(800, 600);
            pnlPayment.Name = "pnlPayment";
            pnlPayment.TabIndex = 2;

            pnlConfirm.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            pnlConfirm.Visible = false;
            pnlConfirm.Location = new System.Drawing.Point(0, 80);
            pnlConfirm.Size = new System.Drawing.Size(800, 600);
            pnlConfirm.Name = "pnlConfirm";
            pnlConfirm.TabIndex = 3;

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
