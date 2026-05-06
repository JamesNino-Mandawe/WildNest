using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Abouts
{
    public partial class UcHistory : UserControl
    {
        private readonly Color _gold = Color.FromArgb(212, 160, 23);

        private static readonly string[] _years = { "1995", "2001", "2008", "2015", "2020", "2026" };
        private static readonly string[] _tags = { "FOUNDING", "EXPANSION", "THE RESORT", "170 HECTARES", "RESILIENCE", "TODAY" };
        private static readonly string[] _titles = {
            "A Single Philippine Eagle Changes Everything",
            "The Sanctuary Grows to 60 Hectares",
            "Glamping Meets Conservation",
            "Full Sanctuary Achieved",
            "Surviving the Pandemic, Protecting the Animals",
            "Southeast Asia's Leading Wildlife Sanctuary"
        };
        private static readonly string[] _descs = {
            "Dr. Elena Vasquez and conservation biologist Marcos Reyes rescue a critically injured Philippine Eagle on Carmen's jungle canopy. Unable to release it back to the wild, they establish a temporary sanctuary on 12 hectares of donated farmland. WildNest is born.",
            "Government conservation grants and private partnerships allow WildNest to expand to 60 hectares. The first dedicated wildlife hospital opens. Six new species — including a rescued Visayan Warty Pig and a pair of freshwater crocodiles — join the sanctuary.",
            "WildNest opens its first three glamping tents along the Golden Savanna perimeter. The controversial decision proves transformative: revenue triples the conservation budget within two years, and the waiting list for stays exceeds six months.",
            "WildNest reaches its full 170-hectare footprint across all eight wildlife zones. The Sanctuary Villa opens as the flagship accommodation. The Night Safari launches to international acclaim — featured in Condé Nast Traveller's Asia-Pacific Emerging Destinations list.",
            "With zero guests for 14 months, WildNest's endowment fund covers 100% of animal care costs. Not a single animal is relocated. The team uses the closure to complete the Conservation Hub and expand the veterinary wing.",
            "35+ resident species. 9 luxury accommodations. 5 daily wildlife experiences. 280 full-time local staff. 4.9★ across 2,400+ guest reviews. And a founding pair of rangers who still walk the morning rounds every day at 5:30 AM."
        };

        // 6 items × 96px = 576px. Header = 108px. Total ≈ 684px — fits in 690px UC.
        private const int ITEM_H = 128;

        public UcHistory()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 237, 232);
            DoubleBuffered = true;
            StyleHeader();
            BuildTimeline();
        }

        private void StyleHeader()
        {
            lblSectionTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblSectionTag.ForeColor = Color.FromArgb(180, 140, 10);
            lblSectionTag.BackColor = Color.Transparent;
            lblSectionTag.Text = "OUR HISTORY";
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new Point(48, 22);

            lblTitle.Font = new Font("Georgia", 26, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(26, 26, 26);
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Text = "Three Decades of Conservation";
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(46, 46);

            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(140, 140, 140);
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Text = "From a single rescued eagle to Southeast Asia's leading wildlife sanctuary";
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(48, 84);

            Paint += (s, pe) =>
            {
                int ly = lblSectionTag.Top + lblSectionTag.Height / 2;
                pe.Graphics.DrawLine(new Pen(Color.FromArgb(50, 180, 160, 23), 1),
                    lblSectionTag.Right + 10, ly, Width - 48, ly);
            };
        }

        private void BuildTimeline()
        {
            const int PANEL_X = 48, PANEL_Y = 108;

            // Timeline panel fills full width
            flpTimeline.BackColor = Color.Transparent;
            flpTimeline.Location = new Point(PANEL_X, PANEL_Y);
            flpTimeline.Size = new Size(Math.Max(100, Width - PANEL_X * 2), ITEM_H * 6 + 4);
            flpTimeline.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Resize += (s, e) =>
            {
                int nw = Math.Max(100, Width - PANEL_X * 2);
                flpTimeline.Width = nw;
                foreach (Control c in flpTimeline.Controls)
                    c.Width = nw;
                flpTimeline.Invalidate(true);
            };

            for (int i = 0; i < 6; i++)
            {
                int ci = i;
                var item = new Panel
                {
                    Size = new Size(flpTimeline.Width, ITEM_H),
                    Location = new Point(0, ci * ITEM_H),
                    BackColor = Color.Transparent
                };

                item.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    const int LINE_X = 36;
                    int midY = ITEM_H / 2;

                    // Vertical connectors
                    if (ci > 0)
                        g.DrawLine(new Pen(Color.FromArgb(55, 212, 160, 23), 2),
                            LINE_X, 0, LINE_X, midY - 10);
                    if (ci < 5)
                        g.DrawLine(new Pen(Color.FromArgb(55, 212, 160, 23), 2),
                            LINE_X, midY + 10, LINE_X, ITEM_H);

                    // Glow ring
                    g.DrawEllipse(new Pen(Color.FromArgb(48, 212, 160, 23), 1.2f),
                        LINE_X - 13, midY - 13, 26, 26);
                    // Gold dot
                    g.FillEllipse(new SolidBrush(Color.FromArgb(212, 160, 23)),
                        LINE_X - 8, midY - 8, 16, 16);
                    // White pip
                    g.FillEllipse(Brushes.White, LINE_X - 3, midY - 3, 6, 6);

                    // Text uses FULL remaining width
                    int tx = LINE_X + 28;
                    int tw = p.Width - tx - 24;

                    // Year — Tag  (y = midY - 42)
                    TextRenderer.DrawText(g, $"{_years[ci]}  —  {_tags[ci]}",
                        new Font("Segoe UI", 8, FontStyle.Bold),
                        new Rectangle(tx, midY - 56, tw, 18),
                        Color.FromArgb(212, 160, 23), TextFormatFlags.Left);

                    // Title  (y = midY - 22)
                    TextRenderer.DrawText(g, _titles[ci],
                        new Font("Georgia", 13, FontStyle.Bold),
                        new Rectangle(tx, midY - 36, tw, 28),
                        Color.FromArgb(26, 26, 26), TextFormatFlags.Left);

                    // Description (clamped to 44px — never bleeds into next item)
                    TextRenderer.DrawText(g, _descs[ci],
                        new Font("Segoe UI", 8.5f),
                        new Rectangle(tx, midY - 4, tw, 60),
                        Color.FromArgb(105, 105, 105),
                        TextFormatFlags.Left | TextFormatFlags.WordBreak);
                };

                flpTimeline.Controls.Add(item);
            }
        }
    }
}