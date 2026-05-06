using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Abouts
{
    public partial class UcTeam : UserControl
    {
        private readonly Color _gold = Color.FromArgb(212, 160, 23);

        private static readonly string[] _avatars = { "👩‍🔬", "👨‍🌾", "👩‍⚕️", "🧑‍🏫", "👩‍💻", "👨‍🍳", "👩‍🎨", "👨‍🔧" };
        private static readonly string[] _names = { "Dr. Elena Vasquez", "Marcos Reyes", "Dr. Rina Castillo", "Jerome Bautista", "Carla Mendoza", "Chef Paolo Cruz", "Mia Santos", "Renzo Bohol" };
        private static readonly string[] _roles = { "CO-FOUNDER & DIRECTOR", "CO-FOUNDER & HEAD RANGER", "CHIEF VETERINARIAN", "EDUCATION LEAD", "CONSERVATION RESEARCH LEAD", "HEAD OF CULINARY", "GUEST EXPERIENCE DIRECTOR", "HEAD OF HABITATS" };
        private static readonly string[] _specs = {
            "Wildlife veterinarian. 30+ years in field conservation. Pioneer of the Philippine Eagle rehabilitation protocol.",
            "Conservation biologist specialising in Visayan species. Leads all ranger certification and training programs.",
            "Specialist in large mammal and raptor medicine. Oversees all daily health assessments and the wildlife hospital.",
            "Heads all school outreach, ranger training, and community engagement. Author of WildNest's conservation curriculum.",
            "Wildlife ecologist and data scientist. Manages all species monitoring, habitat health assessment, and research partnerships.",
            "Farm-to-table chef sourcing 95% of ingredients from Carmen community farms. Leads the Savanna Grill and Canopy Café.",
            "Designed all WildNest guest experiences from scratch. Former luxury hospitality director. Responsible for our 4.9★ rating.",
            "Civil and environmental engineer who designed all 8 wildlife zone enclosures with international wildlife architects."
        };
        private static readonly string[] _years = { "Since 1995", "Since 1995", "Since 2005", "Since 2010", "Since 2014", "Since 2016", "Since 2018", "Since 2008" };
        private static readonly Color[] _accents = {
            Color.FromArgb(27, 130, 80),  Color.FromArgb(27, 130, 80),
            Color.FromArgb(135, 75, 165), Color.FromArgb(40, 130, 185),
            Color.FromArgb(188, 108, 18), Color.FromArgb(185, 50, 50),
            Color.FromArgb(145, 65, 150), Color.FromArgb(65, 112, 52)
        };

        public UcTeam()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(240, 237, 232);
            DoubleBuffered = true;
            StyleHeader();
            BuildGrid();
        }

        private void StyleHeader()
        {
            lblSectionTag.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblSectionTag.ForeColor = Color.FromArgb(180, 140, 10);
            lblSectionTag.BackColor = Color.Transparent;
            lblSectionTag.Text = "OUR PEOPLE";
            lblSectionTag.AutoSize = true;
            lblSectionTag.Location = new Point(48, 22);

            lblTitle.Font = new Font("Georgia", 26, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(26, 26, 26);
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Text = "The Rangers Behind the Sanctuary";
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(46, 46);

            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(140, 140, 140);
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Text = "Certified wildlife professionals — most of them from Carmen itself";
            lblSubtitle.AutoSize = true;
            lblSubtitle.Location = new Point(48, 84);

            Paint += (s, pe) =>
            {
                int ly = lblSectionTag.Top + lblSectionTag.Height / 2;
                pe.Graphics.DrawLine(new Pen(Color.FromArgb(50, 180, 160, 23), 1),
                    lblSectionTag.Right + 10, ly, Width - 48, ly);
            };
        }

        private void BuildGrid()
        {
            // Grid: y=108, fills full width, height = remaining (690 - 108 - margin)
            const int GRID_Y = 108;
            int gridH = 570;  // leaves room for header above

            tlpTeam.BackColor = Color.Transparent;
            tlpTeam.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            tlpTeam.Location = new Point(48, GRID_Y);
            tlpTeam.Size = new Size(Math.Max(100, Width - 96), gridH);
            tlpTeam.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Resize += (s, e) =>
            {
                tlpTeam.Width = Math.Max(100, Width - 96);
                tlpTeam.Invalidate(true);
            };

            for (int i = 0; i < 8; i++)
            {
                int ci = i;
                Color ac = _accents[ci];
                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Margin = new Padding(8)
                };

                card.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    var r = new Rectangle(0, 0, p.Width - 2, p.Height - 2);
                    using var gp = RR(r, 14);
                    g.FillPath(Brushes.White, gp);
                    g.DrawPath(new Pen(Color.FromArgb(215, 212, 206), 0.8f), gp);
                    g.DrawLine(new Pen(ac, 3f), 16, 1, r.Width - 16, 1);

                    // Avatar circle (centred, 64px)
                    int cD = 64, cX = (p.Width - cD) / 2, cY = 12;
                    var circ = new Rectangle(cX, cY, cD, cD);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(22, ac.R, ac.G, ac.B)), circ);
                    g.DrawEllipse(new Pen(Color.FromArgb(55, ac.R, ac.G, ac.B), 1.4f), circ);
                    TextRenderer.DrawText(g, _avatars[ci],
                        new Font("Segoe UI Emoji", 24), circ, Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // Name  y=84
                    TextRenderer.DrawText(g, _names[ci],
                        new Font("Georgia", 10.5f, FontStyle.Bold),
                        new Rectangle(6, 84, p.Width - 12, 26), Color.FromArgb(26, 26, 26),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);

                    // Role  y=114 — two-line cap, enough height for long titles
                    TextRenderer.DrawText(g, _roles[ci],
                        new Font("Segoe UI", 6f, FontStyle.Bold),
                        new Rectangle(4, 114, p.Width - 8, 28), ac,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);

                    // Spec text — height clamped so it cannot reach badge
                    int badgeTop = p.Height - 34;
                    int specTop = 148;
                    int specH = Math.Max(0, badgeTop - specTop - 8);
                    TextRenderer.DrawText(g, _specs[ci],
                        new Font("Segoe UI", 7.5f),
                        new Rectangle(10, specTop, p.Width - 20, specH),
                        Color.FromArgb(110, 110, 110),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);

                    // Since badge (pinned to bottom)
                    var badge = new Rectangle((p.Width - 92) / 2, p.Height - 32, 92, 24);
                    using var bgp = RR(badge, 11);
                    g.FillPath(new SolidBrush(Color.FromArgb(16, ac.R, ac.G, ac.B)), bgp);
                    g.DrawPath(new Pen(Color.FromArgb(46, ac.R, ac.G, ac.B), 0.8f), bgp);
                    TextRenderer.DrawText(g, _years[ci],
                        new Font("Segoe UI", 7.5f), badge, ac,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };

                tlpTeam.Controls.Add(card, ci % 4, ci / 4);
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