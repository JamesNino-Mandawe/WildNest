using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Visits
{
    public partial class UcWhatToExpect : UserControl
    {
        static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        static readonly Color C_DARK2 = Color.FromArgb(10, 30, 18);
        static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        static readonly Color C_BRD = Color.FromArgb(224, 221, 216);
        static readonly Color C_GREEN_LIGHT = Color.FromArgb(225, 245, 238);

        private const int PAD_X = 60;
        private const int HEADER_H = 128;
        private const int FOOTER_H = 64;
        private const int CARD_H = 270;
        private const int GAP = 16;
        private Panel _footer;

        private static readonly (string Icon, string Title, string Desc, string[] Tags)[] CARDS = {
            ("👗", "What to Wear",
             "Closed-toe footwear is required in all wildlife zones. Light, breathable clothing is recommended. Hats and sunscreen are strongly advised — the savanna zone has very little shade during peak hours.",
             new[] { "Closed shoes required", "Light clothing", "Hat recommended" }),

            ("🎒", "What to Bring",
             "Bring a refillable water bottle — hydration stations are at every zone entrance. Keep bags small; free lockers are available at the main gate. A phone with a good zoom lens is ideal for animal photography.",
             new[] { "Water bottle", "Camera", "Small bag only" }),

            ("🚫", "Sanctuary Rules",
             "No flash photography near animals. No feeding outside designated sessions. Loud noise, running, and sudden movements are strictly prohibited in all zones. WildNest is conservation-first — respect our residents.",
             new[] { "No flash photography", "No outside food", "No running" }),

            ("🍽️", "Food & Dining",
             "Two on-site dining options: the Canopy Café (casual, near Zone 3) and the Savanna Grill (full service, lunch and dinner). Outside food is not permitted inside any of the wildlife zones.",
             new[] { "2 dining venues", "Lunch & dinner", "Vegetarian options" }),

            ("♿", "Accessibility",
             "WildNest is fully wheelchair accessible via the main tram route. Dedicated parking, accessible restrooms, and priority boarding are available. Please notify us in advance for tailored assistance.",
             new[] { "Wheelchair accessible", "Priority boarding", "Assistance available" }),

            ("🐾", "Animal Safety",
             "All interactions are ranger-supervised. Maintain a 2-metre minimum distance from all barriers. Children under 12 must stay within arm's reach of a guardian inside all active wildlife zones at all times.",
             new[] { "Ranger supervised", "2m barrier rule", "Guardian required" }),
        };

        public UcWhatToExpect()
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
            lblVisitorGuide.ForeColor = C_GOLD;
            lblVisitorGuide.Font = new Font("Segoe UI", 8f);
            lblVisitorGuide.BackColor = Color.Transparent;
            lblVisitorGuide.Text = "VISITOR GUIDE";
            lblVisitorGuide.Location = new Point(PAD_X, 26);

            lblWhatToExpectInside.ForeColor = Color.FromArgb(26, 26, 26);
            lblWhatToExpectInside.Font = new Font("Georgia", 26f, FontStyle.Bold);
            lblWhatToExpectInside.BackColor = Color.Transparent;
            lblWhatToExpectInside.AutoSize = false;
            lblWhatToExpectInside.Size = new Size(600, 46);
            lblWhatToExpectInside.Location = new Point(PAD_X, 48);
            lblWhatToExpectInside.Text = "What to Expect Inside";

            lblTipsGuidelines.ForeColor = Color.FromArgb(136, 136, 136);
            lblTipsGuidelines.Font = new Font("Segoe UI", 9.5f);
            lblTipsGuidelines.BackColor = Color.Transparent;
            lblTipsGuidelines.AutoSize = false;
            lblTipsGuidelines.Size = new Size(660, 22);
            lblTipsGuidelines.Location = new Point(PAD_X, 98);
            lblTipsGuidelines.Text = "Tips and guidelines to make the most of your WildNest experience";

            this.Paint += (s, e) =>
            {
                if (this.Width < 200) return;
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(50, 212, 160, 23), 1f),
                    lblVisitorGuide.Right + 12, lblVisitorGuide.Top + 10,
                    this.Width - PAD_X, lblVisitorGuide.Top + 10);
            };
        }

        // ── Build ─────────────────────────────────────────────────────
        private void BuildContent()
        {
            // Cards are built into tlpExpectCards (from designer)
            tlpExpectCards.BackColor = Color.Transparent;
            tlpExpectCards.Controls.Clear();

            foreach (var (icon, title, desc, tags) in CARDS)
                tlpExpectCards.Controls.Add(MakeCard(icon, title, desc, tags));

            // Footer
            _footer = new Panel { Height = FOOTER_H, BackColor = Color.Transparent };
            _footer.Paint += PaintFooter;
            this.Controls.Add(_footer);

            DoLayout();
        }

        // ── Layout ────────────────────────────────────────────────────
        private void DoLayout()
        {
            if (this.Width < 200) return;

            int avail = this.Width - PAD_X * 2;

            // TLP: 3 cols × 2 rows
            tlpExpectCards.Left = PAD_X;
            tlpExpectCards.Top = HEADER_H;
            tlpExpectCards.Width = avail;
            tlpExpectCards.Height = CARD_H * 2 + GAP;

            int contentEnd = tlpExpectCards.Bottom + 30;
            _footer.Top = contentEnd;
            _footer.Left = 0;
            _footer.Width = Math.Max(this.Width, 200);

            // Scroll stops exactly at footer bottom
            this.Height = _footer.Bottom;
            this.MinimumSize = new Size(0, this.Height);

            this.Invalidate(new Rectangle(0, 0, this.Width, HEADER_H));
        }

        // ── Card factory ──────────────────────────────────────────────
        private Panel MakeCard(string icon, string title, string desc, string[] tags)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(GAP / 2),
                BackColor = Color.Transparent,
            };
            p.Paint += (s, e) => DrawCard(e.Graphics, p, icon, title, desc, tags);
            return p;
        }

        private void DrawCard(Graphics g, Panel p, string icon, string title,
            string desc, string[] tags)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int w = p.Width - 3, h = p.Height - 3;
            if (w < 10 || h < 10) return;

            // Shadow
            using var sh = RoundedRect(new Rectangle(2, 3, w, h), 14);
            g.FillPath(new SolidBrush(Color.FromArgb(13, 0, 0, 0)), sh);

            // White card
            using var path = RoundedRect(new Rectangle(0, 0, w, h), 14);
            g.FillPath(Brushes.White, path);
            g.DrawPath(new Pen(C_BRD, 0.5f), path);

            int y = 22;

            // Icon background (round, green-tinted)
            using var ibPath = RoundedRect(new Rectangle(20, y, 48, 48), 14);
            g.FillPath(new SolidBrush(C_GREEN_LIGHT), ibPath);
            using var iF = new Font("Segoe UI Emoji", 20f);
            var iSz = g.MeasureString(icon, iF);
            g.DrawString(icon, iF, Brushes.Black,
                20 + (48 - iSz.Width) / 2f,
                y + (48 - iSz.Height) / 2f + 2);
            y += 62;

            // Title
            g.DrawString(title, new Font("Segoe UI", 10.5f, FontStyle.Bold),
                new SolidBrush(Color.FromArgb(26, 26, 26)), 20, y);
            y += 24;

            // Thin gold accent under title
            g.DrawLine(new Pen(Color.FromArgb(60, 212, 160, 23), 1.5f), 20, y, 20 + 30, y);
            y += 10;

            // Description text
            using var dF = new Font("Segoe UI", 8.5f);
            g.DrawString(desc, dF, new SolidBrush(Color.FromArgb(80, 80, 80)),
                new RectangleF(20, y, w - 38, 100));
            y += 104;

            // Tags
            int tx = 20;
            using var tgF = new Font("Segoe UI", 7.5f);
            foreach (var tag in tags)
            {
                var tSz = g.MeasureString(tag, tgF);
                int tw = (int)tSz.Width + 18;
                if (tx + tw > w - 10) { tx = 20; y += 24; }
                using var tgPath = RoundedRect(new Rectangle(tx, y, tw, 22), 11);
                g.FillPath(new SolidBrush(C_BG), tgPath);
                g.DrawPath(new Pen(C_BRD, 0.5f), tgPath);
                g.DrawString(tag, tgF, new SolidBrush(Color.FromArgb(80, 80, 80)),
                    tx + 8, y + 4);
                tx += tw + 6;
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
