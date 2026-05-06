using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Abouts
{
    public partial class UcImpacts : UserControl
    {
        private readonly Color _gold = Color.FromArgb(212, 160, 23);
        private readonly Color _dark = Color.FromArgb(18, 44, 24);

        private static readonly string[] _icons = { "🦅", "🌳", "🎓", "💚" };
        private static readonly string[] _nums = { "312", "170ha", "360,000+", "₱2.4B" };
        private static readonly string[] _labels = {
            "Animals rescued &\nrehabilitated since founding",
            "Of restored and protected\nsanctuary land",
            "Guests educated across\n30 years of operation",
            "Reinvested in conservation\nand local community"
        };
        private static readonly string[] _partners = {
            "DENR Philippines", "WWF Philippines", "UP Visayas",
            "IUCN", "Cebu City Gov't", "Haribon Foundation"
        };

        // Layout constants (all relative to full UC width)
        private const int MX = 48;  // margin left/right

        public UcImpacts()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 237, 232);
            DoubleBuffered = true;
            StyleHeader();
            BuildStatCards();

            BuildCta();
        }

        // ── Header ─────────────────────────────────────────────────────
        private void StyleHeader()
        {
            lblSectionTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblSectionTag.ForeColor = Color.FromArgb(180, 140, 10);
            lblSectionTag.BackColor = Color.Transparent;
            lblSectionTag.Text = "CONSERVATION IMPACT";
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new Point(MX, 22);

            lblTitle.Font = new Font("Georgia", 26, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(26, 26, 26);
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Text = "30 Years in Numbers";
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(MX - 2, 46);

            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(140, 140, 140);
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Text = "Every visit, every booking, every stay directly contributes to these numbers";
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(MX, 84);

            Paint += (s, pe) =>
            {
                int ly = lblSectionTag.Top + lblSectionTag.Height / 2;
                pe.Graphics.DrawLine(new Pen(Color.FromArgb(50, 180, 160, 23), 1),
                    lblSectionTag.Right + 10, ly, Width - MX, ly);
            };
        }

        // ── 4 stat cards: full width, 1 row, y=108 ───────────────────
        private void BuildStatCards()
        {
            tlpStats.BackColor = Color.Transparent;
            tlpStats.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            tlpStats.Location = new Point(MX, 108);
            tlpStats.Size = new Size(Math.Max(100, Width - MX * 2), 190);
            tlpStats.ColumnCount = 4;
            tlpStats.RowCount = 1;
            tlpStats.ColumnStyles.Clear();
            tlpStats.RowStyles.Clear();
            for (int c = 0; c < 4; c++)
                tlpStats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tlpStats.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlpStats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Resize += (s, e) => { tlpStats.Width = Math.Max(100, Width - MX * 2); };

            for (int i = 0; i < 4; i++)
            {
                int ci = i;
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Margin = new Padding(6, 4, 6, 4)
                };
                card.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    var r = new Rectangle(0, 0, p.Width - 2, p.Height - 2);
                    using var gp = RR(r, 14);
                    g.FillPath(new SolidBrush(_dark), gp);
                    g.DrawPath(new Pen(Color.FromArgb(50, 212, 160, 23), 1.2f), gp);

                    // Top glow
                    using var gl = new GraphicsPath();
                    gl.AddEllipse(p.Width / 2 - 60, -18, 120, 80);
                    using var pgb = new PathGradientBrush(gl);
                    pgb.CenterColor = Color.FromArgb(24, 212, 160, 23);
                    pgb.SurroundColors = new[] { Color.Transparent };
                    g.FillPath(pgb, gl);

                    TextRenderer.DrawText(g, _icons[ci], new Font("Segoe UI Emoji", 28),
                        new Rectangle(0, 10, p.Width, 46), Color.White, TextFormatFlags.HorizontalCenter);
                    TextRenderer.DrawText(g, _nums[ci], new Font("Georgia", 22, FontStyle.Bold),
                        new Rectangle(0, 60, p.Width, 36), _gold, TextFormatFlags.HorizontalCenter);
                    TextRenderer.DrawText(g, _labels[ci], new Font("Segoe UI", 7.5f),
                        new Rectangle(8, 100, p.Width - 16, 66),
                        Color.FromArgb(140, 200, 180, 140),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                };
                tlpStats.Controls.Add(card, ci, 0);
            }
        }

        // ── Partner logos row: y=302, h=74 ───────────────────────────


        // ── CTA: y=392, h=164. Full width. Stat badge fills right side ─
        private void BuildCta()
        {
            pnlCta.BackColor = Color.Transparent;
            pnlCta.Location = new Point(MX, 308);
            pnlCta.Size = new Size(Math.Max(100, Width - MX * 2), 182);
            pnlCta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Resize += (s, e) => { pnlCta.Width = Math.Max(100, Width - MX * 2); };

            pnlCta.Paint += (s, pe) =>
            {
                var p = (Panel)s;
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                var r = p.ClientRectangle; r.Inflate(-1, -1);
                using var gp = RR(r, 18);
                using var lgb = new LinearGradientBrush(r,
                    Color.FromArgb(7, 26, 14), Color.FromArgb(14, 44, 22), 135f);
                g.FillPath(lgb, gp);
                g.DrawPath(new Pen(Color.FromArgb(50, 212, 160, 23), 1.2f), gp);

                // Left gold accent bar
                g.FillRectangle(new SolidBrush(_gold),
                    new Rectangle(r.X + 1, r.Y + 18, 4, r.Height - 36));

                // Eyebrow
                TextRenderer.DrawText(g, "JOIN THE MISSION",
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    new Rectangle(26, 20, p.Width - 220, 16), _gold, TextFormatFlags.Left);

                // Headline — large, uses most of width leaving badge space
                TextRenderer.DrawText(g, "Every Visit Funds Conservation",
                    new Font("Georgia", 20, FontStyle.Bold),
                    new Rectangle(26, 44, p.Width - 220, 34), Color.FromArgb(248, 244, 239),
                    TextFormatFlags.Left);

                // Body text
                TextRenderer.DrawText(g,
                    "100% of your ticket revenue goes directly to animal care, ranger salaries, and habitat restoration. Your visit is a direct act of conservation — every booking funds this sanctuary's 30-year mission.",
                    new Font("Segoe UI", 9f),
                    new Rectangle(26, 82, p.Width - 220, 70),
                    Color.FromArgb(128, 200, 180, 128),
                    TextFormatFlags.Left | TextFormatFlags.WordBreak);

                // ── RIGHT SIDE decorative badge ── fills the previously empty space
                int bw = 180, bh = 124;
                int bx = p.Width - bw - 14, by = (p.Height - bh) / 2;
                var badge = new Rectangle(bx, by, bw, bh);
                using var bgp = RR(badge, 16);
                g.FillPath(new SolidBrush(Color.FromArgb(26, 212, 160, 23)), bgp);
                g.DrawPath(new Pen(Color.FromArgb(55, 212, 160, 23), 1f), bgp);
                // Badge icon
                TextRenderer.DrawText(g, "🌱",
                    new Font("Segoe UI Emoji", 22),
                    new Rectangle(bx, by + 8, bw, 36), Color.White, TextFormatFlags.HorizontalCenter);
                // Badge number
                TextRenderer.DrawText(g, "₱2.4B",
                    new Font("Georgia", 20, FontStyle.Bold),
                    new Rectangle(bx, by + 48, bw, 30), _gold, TextFormatFlags.HorizontalCenter);
                // Badge label
                TextRenderer.DrawText(g, "reinvested in conservation\n& local community",
                    new Font("Segoe UI", 7.5f),
                    new Rectangle(bx + 8, by + 82, bw - 16, 36),
                    Color.FromArgb(120, 200, 180, 120),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
            };
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

        private void InitializeComponent()
        {
            lblSubtitle = new System.Windows.Forms.Label();
            lblTitle = new System.Windows.Forms.Label();
            lblSectionTag = new System.Windows.Forms.Label();
            tlpStats = new System.Windows.Forms.TableLayoutPanel();

            pnlCta = new System.Windows.Forms.Panel();
            SuspendLayout();
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(pnlCta);

            Controls.Add(tlpStats);
            Controls.Add(lblSubtitle);
            Controls.Add(lblTitle);
            Controls.Add(lblSectionTag);
            Name = "UcImpacts";
            Size = new System.Drawing.Size(1262, 690);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}