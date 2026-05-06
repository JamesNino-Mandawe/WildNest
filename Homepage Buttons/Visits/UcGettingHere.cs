using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Visits
{
    public partial class UcGettingHere : UserControl
    {
        static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        static readonly Color C_DARK2 = Color.FromArgb(10, 30, 18);
        static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        static readonly Color C_GREEN_LIGHT = Color.FromArgb(225, 245, 238);
        static readonly Color C_BRD = Color.FromArgb(224, 221, 216);

        private const int PAD_X = 60;
        private const int HEADER_H = 128;
        private const int FOOTER_H = 64;
        private Panel _footer;

        public UcGettingHere()
        {
            InitializeComponent();
            this.BackColor = C_BG;
            this.AutoScroll = false;
            this.AutoScrollMinSize = Size.Empty;
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);
            StyleHeader();
            BuildContent();
            this.Resize += (s, e) => DoLayout();
        }

        // ── Header ────────────────────────────────────────────────────
        private void StyleHeader()
        {
            lblDirections.ForeColor = C_GOLD;
            lblDirections.Font = new Font("Segoe UI", 8f);
            lblDirections.BackColor = Color.Transparent;
            lblDirections.Text = "DIRECTIONS";
            lblDirections.Location = new Point(PAD_X, 26);

            lblGettingToWildNest.ForeColor = Color.FromArgb(26, 26, 26);
            lblGettingToWildNest.Font = new Font("Georgia", 26f, FontStyle.Bold);
            lblGettingToWildNest.BackColor = Color.Transparent;
            lblGettingToWildNest.AutoSize = false;
            lblGettingToWildNest.Size = new Size(580, 46);
            lblGettingToWildNest.Location = new Point(PAD_X, 48);
            lblGettingToWildNest.Text = "Getting to WildNest";

            lblLocation.ForeColor = Color.FromArgb(136, 136, 136);
            lblLocation.Font = new Font("Segoe UI", 9.5f);
            lblLocation.BackColor = Color.Transparent;
            lblLocation.AutoSize = false;
            lblLocation.Size = new Size(660, 22);
            lblLocation.Location = new Point(PAD_X, 98);
            lblLocation.Text = "Located in Carmen, North Cebu — approximately 45 minutes from Cebu City";

            this.Paint += (s, e) =>
            {
                if (this.Width < 200) return;
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(50, 212, 160, 23), 1f),
                    lblDirections.Right + 12, lblDirections.Top + 10,
                    this.Width - PAD_X, lblDirections.Top + 10);
            };
        }

        // ── Build cards ───────────────────────────────────────────────
        private void BuildContent()
        {
            // Transport cards (painted panels, already in designer)
            pnlPrivateCar.BackColor = Color.Transparent;
            pnlPrivateCar.Paint += (s, e) => DrawTransportCard(e.Graphics, pnlPrivateCar,
                "🚗", "By Private Car or Taxi",
                "Most comfortable. Free on-site parking for 200 vehicles.",
                new[] {
                    "Head north from Cebu City via the National Highway (N2)",
                    "Exit at Carmen town proper — follow WildNest directional signage",
                    "Turn right at the WildNest arch gate — 500m down the access road",
                });

            pnlPublicTransport.BackColor = Color.Transparent;
            pnlPublicTransport.Paint += (s, e) => DrawTransportCard(e.Graphics, pnlPublicTransport,
                "🚌", "By Public Transport",
                "Budget-friendly from the North Bus Terminal in Cebu City.",
                new[] {
                    "Take a Carmen-bound bus from the North Bus Terminal (₱60–80)",
                    "Alight at Carmen town center — tell the driver \"WildNest\"",
                    "Take a habal-habal motorcycle taxi to the main gate (~₱30)",
                });

            pnlAddress.BackColor = Color.Transparent;
            pnlAddress.Paint += (s, e) => DrawAddressCard(e.Graphics, pnlAddress);

            // Footer
            _footer = new Panel { Height = FOOTER_H, BackColor = Color.Transparent };
            _footer.Paint += PaintFooter;
            this.Controls.Add(_footer);

            DoLayout();
        }

        // ── Master layout ─────────────────────────────────────────────
        private void DoLayout()
        {
            if (this.Width < 200) return;

            int avail = this.Width - PAD_X * 2;
            int leftW = (int)(avail * 0.52f);
            int rightW = avail - leftW - 24;
            if (leftW < 300) { leftW = 300; rightW = Math.Max(260, avail - 324); }

            // Left column — two stacked transport cards
            int cardH1 = 222, cardH2 = 222;
            pnlPrivateCar.Left = PAD_X;
            pnlPrivateCar.Top = HEADER_H;
            pnlPrivateCar.Width = leftW;
            pnlPrivateCar.Height = cardH1;

            pnlPublicTransport.Left = PAD_X;
            pnlPublicTransport.Top = HEADER_H + cardH1 + 18;
            pnlPublicTransport.Width = leftW;
            pnlPublicTransport.Height = cardH2;

            // Right column — address card spans full left height
            int rightH = cardH1 + 18 + cardH2;
            pnlAddress.Left = PAD_X + leftW + 24;
            pnlAddress.Top = HEADER_H;
            pnlAddress.Width = rightW;
            pnlAddress.Height = rightH;

            int contentEnd = HEADER_H + rightH + 30;
            _footer.Top = contentEnd;
            _footer.Left = 0;
            _footer.Width = Math.Max(this.Width, 200);

            // Scroll stops exactly at footer bottom
            this.Height = _footer.Bottom;
            this.MinimumSize = new Size(0, this.Height);

            // Refresh header rule
            pnlPrivateCar.Invalidate();
            pnlPublicTransport.Invalidate();
            pnlAddress.Invalidate();
            this.Invalidate(new Rectangle(0, 0, this.Width, HEADER_H));
        }

        // ── Transport card painter ─────────────────────────────────────
        private void DrawTransportCard(Graphics g, Panel card, string emoji,
            string title, string sub, string[] steps)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int w = card.Width - 3, h = card.Height - 3;
            if (w < 10 || h < 10) return;

            // Shadow
            using var sh = RoundedRect(new Rectangle(2, 3, w, h), 14);
            g.FillPath(new SolidBrush(Color.FromArgb(14, 0, 0, 0)), sh);

            // White body
            using var path = RoundedRect(new Rectangle(0, 0, w, h), 14);
            g.FillPath(Brushes.White, path);
            g.DrawPath(new Pen(C_BRD, 0.5f), path);

            // Icon box (rounded, green-tinted)
            using var ib = RoundedRect(new Rectangle(18, 16, 46, 46), 12);
            g.FillPath(new SolidBrush(C_GREEN_LIGHT), ib);
            using var iF = new Font("Segoe UI Emoji", 19f);
            var iSz = g.MeasureString(emoji, iF);
            g.DrawString(emoji, iF, Brushes.Black,
                18 + (46 - iSz.Width) / 2f,
                16 + (46 - iSz.Height) / 2f + 2);

            // Title + sub
            g.DrawString(title, new Font("Segoe UI", 10.5f, FontStyle.Bold),
                new SolidBrush(Color.FromArgb(26, 26, 26)), 74, 18);
            g.DrawString(sub, new Font("Segoe UI", 8f),
                new SolidBrush(Color.FromArgb(136, 136, 136)), 74, 40);

            // Divider
            g.DrawLine(new Pen(Color.FromArgb(235, 232, 228), 1f), 18, 76, w - 18, 76);

            // Steps
            int sy = 86;
            for (int i = 0; i < steps.Length; i++)
            {
                // Number badge (dark circle, gold number)
                using var nb = RoundedRect(new Rectangle(18, sy, 22, 22), 11);
                g.FillPath(new SolidBrush(C_DARK), nb);
                using var nF = new Font("Segoe UI", 8f, FontStyle.Bold);
                using var nBr = new SolidBrush(C_GOLD);
                var nSz = g.MeasureString((i + 1).ToString(), nF);
                g.DrawString((i + 1).ToString(), nF, nBr,
                    18 + (22 - nSz.Width) / 2f, sy + (22 - nSz.Height) / 2f);

                // Step text
                g.DrawString(steps[i], new Font("Segoe UI", 8.5f),
                    new SolidBrush(Color.FromArgb(60, 60, 60)),
                    new RectangleF(50, sy + 2, w - 66, 36));

                sy += 40;
                if (i < steps.Length - 1)
                    g.DrawLine(new Pen(Color.FromArgb(242, 240, 237), 0.5f),
                        18, sy + 2, w - 18, sy + 2);
            }
        }

        // ── Address card painter ──────────────────────────────────────
        private void DrawAddressCard(Graphics g, Panel card)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int w = card.Width - 1, h = card.Height - 1;
            if (w < 10 || h < 10) return;

            // Dark gradient card
            using var path = RoundedRect(new Rectangle(0, 0, w, h), 16);
            using var lg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                C_DARK, C_DARK2);
            g.FillPath(lg, path);
            g.DrawPath(new Pen(Color.FromArgb(45, 212, 160, 23), 1f), path);

            // Radial glow
            using var gp = new GraphicsPath();
            gp.AddEllipse(w / 2 - 100, -50, 200, 200);
            using var pgb = new PathGradientBrush(gp);
            pgb.CenterColor = Color.FromArgb(22, 212, 160, 23);
            pgb.SurroundColors = new[] { Color.Transparent };
            g.FillPath(pgb, gp);

            int y = 28;

            // Eyebrow
            g.DrawString("OUR ADDRESS", new Font("Segoe UI", 7.5f),
                new SolidBrush(C_GOLD), 26, y);
            y += 22;

            // Resort name block
            g.DrawString("WildNest Resort\n& Wildlife Experience\nCarmen, Cebu 6019\nPhilippines",
                new Font("Georgia", 14f, FontStyle.Bold),
                new SolidBrush(C_CREAM), 26, y);
            y += 104;

            // Divider
            g.DrawLine(new Pen(Color.FromArgb(28, 212, 160, 23), 0.8f), 26, y, w - 26, y);
            y += 16;

            // Contact rows
            var rows = new[] {
                ("📞", "+63 (32) 555-WILD",         false),
                ("✉️", "hello@wildnest.ph",          true),
                ("🌐", "www.wildnest.ph",            false),
                ("📍", "10.5563° N, 123.9994° E",    true),
                ("🕐", "~45 min from Cebu City",     false),
                ("🅿️", "Free parking — 200 spaces",  false),
            };

            using var rowF = new Font("Segoe UI", 9f);
            using var eF = new Font("Segoe UI Emoji", 11f);
            using var dimB = new SolidBrush(Color.FromArgb(148, 248, 244, 239));
            using var goldB = new SolidBrush(C_GOLD);

            foreach (var (ico, txt, isGold) in rows)
            {
                g.DrawString(ico, eF, dimB, 26, y);
                g.DrawString(txt, rowF, isGold ? goldB : dimB, 54, y + 2);
                y += 30;
            }
        }

        // ── Footer ────────────────────────────────────────────────────
        private void PaintFooter(object s, PaintEventArgs e)
        {
            var fp = (Panel)s;
            var g = e.Graphics;
            using var lg = new LinearGradientBrush(new Point(0, 0), new Point(0, fp.Height),
                C_DARK, C_DARK2);
            g.FillRectangle(lg, fp.ClientRectangle);
            g.FillRectangle(new SolidBrush(Color.FromArgb(50, 212, 160, 23)), 0, 0, fp.Width, 1);

            using var bF = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString("WILDNEST", bF, new SolidBrush(Color.FromArgb(210, 248, 244, 239)),
                PAD_X, (fp.Height - 14) / 2f - 1);

            string copy = "© 2026 WildNest Resort & Wildlife Experience. Carmen, Cebu, Philippines.";
            using var cF = new Font("Segoe UI", 8f);
            using var cB = new SolidBrush(Color.FromArgb(65, 248, 244, 239));
            var cSz = g.MeasureString(copy, cF);
            g.DrawString(copy, cF, cB, fp.Width - cSz.Width - PAD_X, (fp.Height - cSz.Height) / 2f);
        }

        private static GraphicsPath RoundedRect(Rectangle b, int r)
        {
            int d = r * 2;
            var p = new GraphicsPath();
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
