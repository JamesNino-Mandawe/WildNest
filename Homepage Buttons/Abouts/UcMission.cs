using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Abouts
{
    public partial class UcMission : UserControl
    {
        private int _active = 0;
        private readonly Color _gold = Color.FromArgb(212, 160, 23);

        private static readonly string[] _btnIcons = { "🌍", "🌱", "📋", "✦", "🤲" };
        private static readonly string[] _btnLabels = { "Our Vision", "Stewardship", "Transparency", "Excellence", "Inclusion" };
        private static readonly string[] _titles = {
            "A World Where Wildlife Thrives Alongside People",
            "We Are Guardians, Not Owners",
            "Radical Honesty About What We Do",
            "World-Class or Nothing",
            "Conservation Belongs to Everyone"
        };
        private static readonly string[] _bodies = {
            "We envision a future where wildlife sanctuaries are not exceptions — they are the standard. WildNest exists to prove that conservation and human experience can coexist at the highest level of quality.",
            "Every animal at WildNest is a wild being first. Every policy, every enclosure, every schedule is designed around what the animal needs — not what a visitor wants to see.",
            "WildNest publishes its full conservation data annually. We disclose every peso spent. We admit when rehabilitation fails. Transparency is the only currency that matters in conservation.",
            "We refuse to accept that conservation and quality are in conflict. Our facilities, ranger training, dining and guest experiences are benchmarked against the best wildlife sanctuaries in the world.",
            "Wildlife preservation cannot be the exclusive domain of the privileged. WildNest partners with local schools, offers free community days, and funds full scholarships for Carmen students."
        };
        private static readonly string[][] _pts = {
            new[]{ "Rewilding degraded habitats across North Cebu by 2035", "Training 500 wildlife rangers by 2030", "Establishing a regional conservation research institute", "Zero captive-bred animals — all residents are rescued" },
            new[]{ "No performance feeding or animal interaction shows", "Natural habitat design for all 8 wildlife zones", "Every decision reviewed by wildlife veterinarians", "Animal welfare officer with full veto authority" },
            new[]{ "Annual Conservation Impact Report published every March", "100% revenue allocation breakdown published publicly", "All rehabilitation success/failure rates disclosed", "Open-data partnerships with UP Visayas and IUCN" },
            new[]{ "Rangers certified by international wildlife standards", "Facilities designed by award-winning eco architects", "Cuisine sourced from local conservation-partner farms", "4.9★ average guest rating across 2,400+ reviews" },
            new[]{ "Free community day: first Sunday of every month", "12 full university scholarships awarded annually", "40+ schools visited per year", "Fully accessible facilities — all mobility levels welcome" }
        };

        private Button[] _btns;

        public UcMission()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 237, 232);
            DoubleBuffered = true;
            _btns = new[] { btnOurVision, btnStewardship, btnTransparency, btnExcellence, btnInclusion };
            StyleHeader();
            LayoutButtons();
            StyleContent();
            Paint += BgPaint;
        }

        // ── Header: labels at correct y positions, no overlap ─────────
        private void StyleHeader()
        {
            // y=22  eyebrow (small bold gold)
            lblSectionTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblSectionTag.ForeColor = Color.FromArgb(180, 140, 10);
            lblSectionTag.BackColor = Color.Transparent;
            lblSectionTag.Text = "MISSION & VALUES";
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new Point(48, 22);

            // y=46  title (Georgia 26)
            lblTitle.Font = new Font("Georgia", 26, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(26, 26, 26);
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Text = "What Guides Every Decision";
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(46, 46);

            // y=84  subtitle (Segoe 10)
            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(140, 140, 140);
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Text = "Select a value to see how it shapes everything WildNest does";
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(48, 84);
        }

        // ── Pill bar (y=110, h=66) + 5 buttons ───────────────────────
        // Pill bar is SMALLER than before — height reduced to 66px
        private void LayoutButtons()
        {
            const int BAR_X = 48, BAR_Y = 110, BAR_H = 66;

            void Relayout()
            {
                int bw = Math.Max(1, (Width - BAR_X * 2 - 10) / 5);
                for (int i = 0; i < _btns.Length; i++)
                {
                    _btns[i].Location = new Point(BAR_X + 5 + i * bw, BAR_Y + 5);
                    _btns[i].Size = new Size(bw, BAR_H - 10);
                }
                // Content card: y = pill bottom + 12, height = remaining space minus padding
                int cardY = BAR_Y + BAR_H + 12;
                int cardH = Math.Max(340, Height - cardY - 20);
                pnlValueContent.SetBounds(48, cardY, Width - 96, cardH);
                Invalidate();
            }

            for (int i = 0; i < _btns.Length; i++)
            {
                int idx = i;
                var btn = _btns[i];
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;
                btn.Cursor = Cursors.Hand;
                btn.Text = "";

                btn.Paint += (s, pe) =>
                {
                    var b = (Button)s;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    bool on = _active == idx;
                    if (on)
                    {
                        var rr = b.ClientRectangle; rr.Inflate(-3, -3);
                        using var gp = RR(rr, 10);
                        g.FillPath(new SolidBrush(Color.FromArgb(38, 212, 160, 23)), gp);
                        g.DrawPath(new Pen(Color.FromArgb(145, 212, 160, 23), 1.4f), gp);
                    }
                    Color col = on ? _gold : Color.FromArgb(110, 248, 244, 239);
                    TextRenderer.DrawText(g, _btnIcons[idx],
                        new Font("Segoe UI Emoji", 14),
                        new Rectangle(0, 3, b.Width, 24), col, TextFormatFlags.HorizontalCenter);
                    TextRenderer.DrawText(g, _btnLabels[idx],
                        new Font("Segoe UI", 7.5f, on ? FontStyle.Bold : FontStyle.Regular),
                        new Rectangle(0, 28, b.Width, 18), col, TextFormatFlags.HorizontalCenter);
                    if (on)
                    {
                        int lw = 22, lx = (b.Width - lw) / 2;
                        g.DrawLine(new Pen(_gold, 2f), lx, b.Height - 5, lx + lw, b.Height - 5);
                    }
                };

                btn.Click += (s, e) =>
                {
                    _active = idx;
                    foreach (var b in _btns) b.Invalidate();
                    Invalidate();
                    pnlValueContent.Invalidate();
                };
            }

            Resize += (s, e) => Relayout();
            Relayout();
        }

        // ── Background paint: gold divider + pill bar ─────────────────
        private void BgPaint(object sender, PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Gold divider after eyebrow
            int ly = lblSectionTag.Top + lblSectionTag.Height / 2;
            g.DrawLine(new Pen(Color.FromArgb(50, 180, 160, 23), 1),
                lblSectionTag.Right + 10, ly, Width - 48, ly);
            // Dark pill bar (smaller, 66px)
            var bar = new Rectangle(48, 110, Width - 96, 66);
            using var gp = RR(bar, 14);
            g.FillPath(new SolidBrush(Color.FromArgb(12, 32, 18)), gp);
            g.DrawPath(new Pen(Color.FromArgb(55, 212, 160, 23), 1.4f), gp);
        }

        // ── Content card: fills remaining height, full width ──────────
        private void StyleContent()
        {
            pnlValueContent.BackColor = Color.Transparent;
            pnlValueContent.Paint += ContentPaint;
        }

        private void ContentPaint(object sender, PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int pw = pnlValueContent.Width, ph = pnlValueContent.Height, idx = _active;

            // White rounded card
            var r = new Rectangle(0, 0, pw - 2, ph - 2);
            using var gp = RR(r, 18);
            g.FillPath(Brushes.White, gp);
            g.DrawPath(new Pen(Color.FromArgb(220, 215, 210), 0.8f), gp);

            // Gold gradient top stripe
            using var lgb = new LinearGradientBrush(new Point(24, 0), new Point(pw - 24, 0), Color.Transparent, Color.Transparent);
            lgb.InterpolationColors = new ColorBlend
            {
                Colors = new[] { Color.Transparent, _gold, Color.Transparent },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            g.FillRectangle(lgb, new Rectangle(24, 0, pw - 48, 3));

            // Icon
            TextRenderer.DrawText(g, _btnIcons[idx],
                new Font("Segoe UI Emoji", 32),
                new Rectangle(32, 18, 64, 60), Color.Black, TextFormatFlags.Left);

            // Title
            TextRenderer.DrawText(g, _titles[idx],
                new Font("Georgia", 17, FontStyle.Bold),
                new Rectangle(32, 86, pw - 64, 36), Color.FromArgb(26, 26, 26), TextFormatFlags.Left);

            // Body
            TextRenderer.DrawText(g, _bodies[idx],
                new Font("Segoe UI", 10f),
                new Rectangle(32, 130, pw - 64, 68), Color.FromArgb(90, 90, 90),
                TextFormatFlags.Left | TextFormatFlags.WordBreak);

            // Divider
            g.DrawLine(new Pen(Color.FromArgb(218, 214, 208), 1), 32, 204, pw - 32, 204);

            // 4 bullet points — 2 columns (uses full width)
            string[] pts = _pts[idx];
            int colW = (pw - 64) / 2;
            int bulletStartY = 216;
            int rowH = Math.Max(52, (ph - bulletStartY - 16) / 2);
            for (int j = 0; j < 4; j++)
            {
                int col = j % 2, row = j / 2;
                int px = 32 + col * (colW + 16), py = bulletStartY + row * rowH;
                // Diamond bullet
                g.FillPolygon(new SolidBrush(_gold), new[]
                {
                    new Point(px + 6, py + 8), new Point(px + 13, py + 15),
                    new Point(px + 6, py + 22), new Point(px - 1, py + 15)
                });
                TextRenderer.DrawText(g, pts[j],
                    new Font("Segoe UI", 9.5f),
                    new Rectangle(px + 22, py + 4, colW - 28, rowH - 10),
                    Color.FromArgb(70, 70, 70),
                    TextFormatFlags.Left | TextFormatFlags.WordBreak);
            }
        }

        private static GraphicsPath RR(Rectangle b, int r)
        {
            int d = r * 2;
            var p = new GraphicsPath();
            if (r == 0) { p.AddRectangle(b); return p; }
            p.AddArc(b.X, b.Y, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Y, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.X, b.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}