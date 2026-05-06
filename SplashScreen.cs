using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Project
{
    public partial class SplashScreen : Form
    {
        private int currentStep = 0;
        private int pauseCounter = 0;
        private PictureBox picLogo;
        private Timer stepTimer;
      

        private string[] stepTexts = {
            "Establishing Secure Connection...",
            "Syncing Cabin Inventory...",
            "Fetching Wildlife Schedules...",
            "Analyzing Animal Health Data...",
            "Finalizing Environment..."
        };

        public SplashScreen()
        {
            InitializeComponent();
            // --- LOGO IMPLEMENTATION ---
            picLogo = new PictureBox();
            picLogo.Image = AppAssetLoader.LoadImage("Logo", "Resources", "Logo.png");
                                                       // --- UPDATED LOGO SETTINGS ---
            picLogo.Size = new Size(120, 120);    // Increase from 65 to 100
            picLogo.Location = new Point(40, 40);  // Adjusted for more balanced spacing
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            // -----------------------------
            picLogo.BackColor = Color.Transparent;
            this.Controls.Add(picLogo);
            picLogo.BringToFront();
            // ---------------------------
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1000, 600);
            this.BackColor = Color.FromArgb(5, 18, 10);
            this.DoubleBuffered = true;

            // Background image
            // Load and green-tint the background
            try
            {
                using Image? raw = AppAssetLoader.LoadImage("bg", "Resources", "bg.jpg");
                if (raw != null)
                {
                    Bitmap tinted = new Bitmap(this.Width > 0 ? this.Width : 1000,
                                               this.Height > 0 ? this.Height : 600);
                    using (Graphics gr = Graphics.FromImage(tinted))
                    {
                        // Draw original image
                        gr.DrawImage(raw, 0, 0, tinted.Width, tinted.Height);

                        // Heavy green overlay to shift blue mountains to green
                        using (SolidBrush g1 = new SolidBrush(
                            Color.FromArgb(130, 0, 60, 20)))
                            gr.FillRectangle(g1, 0, 0, tinted.Width, tinted.Height);

                        // Dark nature overlay
                        using (SolidBrush g2 = new SolidBrush(
                            Color.FromArgb(80, 5, 18, 10)))
                            gr.FillRectangle(g2, 0, 0, tinted.Width, tinted.Height);

                        // Bottom darkening — makes loading area readable
                        using (LinearGradientBrush bottomFade =
                            new LinearGradientBrush(
                                new Point(0, tinted.Height / 2),
                                new Point(0, tinted.Height),
                                Color.FromArgb(0, 0, 0, 0),
                                Color.FromArgb(180, 5, 18, 10)))
                            gr.FillRectangle(bottomFade,
                                0, tinted.Height / 2,
                                tinted.Width, tinted.Height / 2);
                    }
                    this.BackgroundImage = tinted;
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch { }
            // Title
            StyleLabel(lblTitle, "Georgia", 52,
                Color.FromArgb(248, 244, 239), true);
            lblTitle.Height = 150;
            lblTitle.Location = new Point(0, 150);
            lblTitle.Text = "W I L D N E S T";

            // Tagline
            StyleLabel(lblTagline, "Georgia", 14,
                Color.FromArgb(212, 160, 23), false);
            lblTagline.Location = new Point(0, 265);
            lblTagline.Text = "ZOO RESORT  ·  WILDLIFE EXPERIENCE";

            // Status
            StyleLabel(lblStatus, "Arial", 9,
                Color.FromArgb(150, 150, 150), false);
            lblStatus.Location = new Point(0, 430);

            // Progress Bar
            pbLoading.Visible = true;
            pbLoading.Size = new Size(500, 2);
            pbLoading.Location = new Point(250, 480);
            pbLoading.Style = ProgressBarStyle.Continuous;

            // Percentage
            StyleLabel(lblPercentage, "Arial", 8,
                Color.FromArgb(212, 160, 23), true);
            lblPercentage.TextAlign = ContentAlignment.MiddleLeft;
            lblPercentage.Size = new Size(100, 20);
            lblPercentage.Location = new Point(250, 490);

            // Version
            StyleLabel(lblVersion, "Arial", 8,
                Color.FromArgb(80, 80, 80), false);
            lblVersion.TextAlign = ContentAlignment.MiddleRight;
            lblVersion.Size = new Size(100, 20);
            lblVersion.Location = new Point(650, 490);
            lblVersion.Text = "V 1.0.0";

            // Step Timer
            stepTimer = new Timer { Interval = 1200 };
            stepTimer.Tick += StepTimer_Tick;

            this.Load += (s, e) =>
            {
                // This makes the logo "see through" to the background image
                picLogo.Parent = this;
                picLogo.BackColor = Color.Transparent;

                SetProgressBarColor(pbLoading, Color.FromArgb(212, 160, 23));
                stepTimer.Start();
            };
            // --------------------------------------------
        } // This bracket ends the SplashScreen() constructor

        private void StyleLabel(Label lbl, string font, int size,
            Color color, bool isBold)
        {
            lbl.AutoSize = false;
            lbl.Width = this.Width;
            lbl.Height = 45;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.ForeColor = color;
            lbl.BackColor = Color.Transparent;
            lbl.Font = new Font(font, size,
                isBold ? FontStyle.Bold : FontStyle.Regular);
            lbl.UseCompatibleTextRendering = true;
        }

        private void StepTimer_Tick(object sender, EventArgs e)
        {
            if (currentStep < stepTexts.Length)
            {
                int progressSnap = (currentStep + 1) * 20;
                pbLoading.Value = progressSnap;
                lblPercentage.Text = progressSnap + "%";
                lblStatus.Text = stepTexts[currentStep].ToUpper();
                currentStep++;
                this.Invalidate();
            }
            else
            {
                lblStatus.Text = "ACCESS GRANTED. WELCOME.";
                lblStatus.ForeColor = Color.FromArgb(168, 200, 138);
                pauseCounter++;
                if (pauseCounter >= 2)
                {
                    stepTimer.Stop();
                    new HomePage().Show();
                    this.Hide();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // ── 1. BACKGROUND IMAGE ──
            // ── 1. BACKGROUND IMAGE ──
            using (SolidBrush greenTint = new SolidBrush(
                 Color.FromArgb(100, 5, 40, 15)))
                g.FillRectangle(greenTint, 0, 0, this.Width, this.Height);

            using (SolidBrush darkOverlay = new SolidBrush(
                Color.FromArgb(80, 0, 20, 5)))
                g.FillRectangle(darkOverlay, 0, 0, this.Width, this.Height);

            // Bottom dark fade — makes dots, bar, text readable
            using (LinearGradientBrush bottomFade = new LinearGradientBrush(
                new Point(0, this.Height - 200),
                new Point(0, this.Height),
                Color.FromArgb(0, 5, 18, 10),
                Color.FromArgb(220, 5, 18, 10)))
                g.FillRectangle(bottomFade,
                    0, this.Height - 200,
                    this.Width, 200);
            // ── 2. CORNER BRACKETS ──
            using (Pen goldPen = new Pen(
                Color.FromArgb(80, 212, 160, 23), 1.5f))
            {
                int m = 40; int s = 45;
                g.DrawLine(goldPen, m, m + s, m, m);
                g.DrawLine(goldPen, m, m, m + s, m);
                g.DrawLine(goldPen, Width - m - s, m, Width - m, m);
                g.DrawLine(goldPen, Width - m, m, Width - m, m + s);
                g.DrawLine(goldPen, m, Height - m - s, m, Height - m);
                g.DrawLine(goldPen, m, Height - m, m + s, Height - m);
                g.DrawLine(goldPen, Width - m - s, Height - m,
                    Width - m, Height - m);
                g.DrawLine(goldPen, Width - m, Height - m,
                    Width - m, Height - m - s);
            }

            // ── 3. GOLD ORNAMENT LINES ──
            // Line 1 — sits 18px ABOVE the title label top edge
            int line1Y = lblTitle.Top - 18;

            // Line 2 — sits 18px BELOW the tagline label bottom edge
            int line2Y = lblTagline.Bottom + 18;

            DrawOrnamentLine(g, this.Width / 2, line1Y);
            DrawOrnamentLine(g, this.Width / 2, line2Y);

            // ── 4. STEP DOTS ──
            int totalDots = 5;
            int dotGap = 15;
            int dotSize = 6;
            int totalGroupWidth = (totalDots * dotSize)
                + ((totalDots - 1) * dotGap);
            int startX = (Width / 2) - (totalGroupWidth / 2);
            int dotY = 505;

            for (int i = 1; i <= totalDots; i++)
            {
                int x = startX + ((i - 1) * (dotSize + dotGap));
                Color dotColor = (currentStep >= i)
                    ? Color.FromArgb(255, 212, 160, 23)
                    : Color.FromArgb(30, 255, 255, 255);

                using (SolidBrush brush = new SolidBrush(dotColor))
                    g.FillEllipse(brush, x, dotY, dotSize, dotSize);

                if (currentStep >= i)
                {
                    using (SolidBrush glow = new SolidBrush(
                        Color.FromArgb(60, 212, 160, 23)))
                        g.FillEllipse(glow,
                            x - 3, dotY - 3, dotSize + 6, dotSize + 6);
                }
            }
        }

        private void DrawOrnamentLine(Graphics g, int centerX, int y)
        {
            int lineWidth = 160;
            int gap = 18;

            // Left line — fades from transparent to gold toward center
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                new Point(centerX - lineWidth, y),
                new Point(centerX - gap, y),
                Color.FromArgb(0, 212, 160, 23),
                Color.FromArgb(180, 212, 160, 23)))
            using (Pen lp = new Pen(lgb, 1f))
                g.DrawLine(lp,
                    centerX - lineWidth, y, centerX - gap, y);

            // Right line — fades from gold to transparent away from center
            using (LinearGradientBrush lgb = new LinearGradientBrush(
                new Point(centerX + gap, y),
                new Point(centerX + lineWidth, y),
                Color.FromArgb(180, 212, 160, 23),
                Color.FromArgb(0, 212, 160, 23)))
            using (Pen rp = new Pen(lgb, 1f))
                g.DrawLine(rp,
                    centerX + gap, y, centerX + lineWidth, y);

            // Center diamond
            int ds = 4;
            Point[] diamond = {
                new Point(centerX,      y - ds),
                new Point(centerX + ds, y),
                new Point(centerX,      y + ds),
                new Point(centerX - ds, y)
            };
            using (SolidBrush db = new SolidBrush(
                Color.FromArgb(180, 212, 160, 23)))
                g.FillPolygon(db, diamond);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void SetProgressBarColor(ProgressBar pb, Color color)
        {
            if (pb.IsHandleCreated)
                SendMessage(pb.Handle, 0x040A, IntPtr.Zero,
                    (IntPtr)ColorTranslator.ToWin32(color));
        }
    }
}
