using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Project.Homepage_Buttons.Abouts;

namespace Project
{
    public partial class UcAbout : UserControl
    {
        private int _activeTab = 0;
        private readonly Panel _heroTitleArt = new Panel();
        private readonly Color _gold = Color.FromArgb(212, 160, 23);
        private readonly Color _darkGreen = Color.FromArgb(7, 26, 14);
        private readonly Color _lightBg = Color.FromArgb(240, 237, 232);

        // Designer: pnlHero = 550px tall, pnlStatusBar = 60px tall (matches UcVisit exactly)
        // Tab bar sits at bottom of hero (96px, identical to UcVisit visit_tabs)
        private const int TAB_H = 96;

        public UcAbout()
        {
            InitializeComponent();
            this.AutoScroll = false;
            this.AutoScrollMinSize = Size.Empty;
            pnlContentArea.Dock = DockStyle.Top;
            pnlContentArea.AutoScroll = false;
          
            BackColor = _lightBg;
            DoubleBuffered = true;

            BuildHero();
            BuildStatusBar();
            BuildTabBar();
            BuildContentArea();

            pnlHero.HandleCreated += (s, e) => CentreHeroContent();
            pnlHero.Resize += (s, e) => CentreHeroContent();

            LoadTab(0);
        }

        // ═══════════════════════════════════════════════════════════════
        //  HERO  (320px — dark green, radial glow, centred content)
        // ═══════════════════════════════════════════════════════════════
        private void BuildHero()
        {
            pnlHero.BackColor = _darkGreen;
            pnlHero.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                HeroSurfacePainter.Paint(g, pnlHero.ClientRectangle, HeroSurfaceVariant.About);

                // Flanking lines around eyebrow label
                if (lblLocation.Width > 0)
                {
                    int ey = lblLocation.Top + lblLocation.Height / 2;
                    int ex1 = lblLocation.Left - 14, ex2 = lblLocation.Right + 14;
                    g.DrawLine(new Pen(Color.FromArgb(120, 212, 160, 23), 1), ex1 - 48, ey, ex1, ey);
                    g.DrawLine(new Pen(Color.FromArgb(120, 212, 160, 23), 1), ex2, ey, ex2 + 48, ey);
                }
            };

            lblLocation.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblLocation.ForeColor = _gold;
            lblLocation.BackColor = Color.Transparent;
            lblLocation.Text = "OUR STORY";
            lblLocation.AutoSize = true;

            _heroTitleArt.BackColor = Color.Transparent;
            _heroTitleArt.Size = new Size(960, 168);
            _heroTitleArt.Paint += PaintHeroTitle;
            pnlHero.Controls.Add(_heroTitleArt);

            lblHeroTitle.Visible = false;

            lblHeroSub.Font = new Font("Georgia", 11, FontStyle.Italic);
            lblHeroSub.ForeColor = Color.FromArgb(95, 248, 244, 239);
            lblHeroSub.BackColor = Color.Transparent;
            lblHeroSub.AutoSize = false;
            lblHeroSub.Size = new Size(820, 72);
            lblHeroSub.TextAlign = ContentAlignment.MiddleCenter;
            lblHeroSub.Text = "We didn't build a zoo. We restored a sanctuary — and invited the world to witness what wildlife looks like when it is truly free.";

            pnlFoundedStrip.BackColor = Color.Transparent;
            pnlFoundedStrip.Size = new Size(480, 32);
            pnlFoundedStrip.Paint += PaintFoundedStrip;
        }

        private void CentreHeroContent()
        {
            int cx = pnlHero.Width / 2;

            // Content zone = hero height (550) minus tab bar (96) = 454 px.
            // Vertical stack heights:
            //   eyebrow (lblLocation)  = 22 px
            //   gap                    =  8 px
            //   title  (lblHeroTitle)  = 110 px
            //   gap                    = 16 px
            //   subtitle (lblHeroSub)  =  64 px
            //   gap                    = 14 px
            //   founded strip          =  32 px
            //   ──────────────────────────────
            //   total                  = 266 px
            int contentH = pnlHero.Height - TAB_H;   // 454 with default sizes
            const int totalH = 22 + 8 + 168 + 10 + 72 + 10 + 32; // 322 px
            int y = (contentH - totalH) / 2;
            if (y < 12) y = 12;

            // Eyebrow
            lblLocation.Location = new Point(cx - lblLocation.PreferredWidth / 2, y);
            y += 22 + 8;

            // Hero title
            _heroTitleArt.Location = new Point(cx - _heroTitleArt.Width / 2, y);
            y += 168 + 10;

            // Hero subtitle
            lblHeroSub.Location = new Point(cx - lblHeroSub.Width / 2, y);
            y += 72 + 10;

            // Founded strip
            pnlFoundedStrip.Location = new Point(cx - pnlFoundedStrip.Width / 2, y);

            // Match Visit tab sizing more closely instead of spanning edge to edge.
            int tabsWidth = Math.Min(pnlHero.Width - 320, 920);
            tlpTabs.Size = new Size(tabsWidth, TAB_H);
            tlpTabs.Location = new Point((pnlHero.Width - tabsWidth) / 2, pnlHero.Height - TAB_H - 14);

            pnlHero.Invalidate();
            pnlFoundedStrip.Invalidate();
        }

        private void PaintHeroTitle(object sender, PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using var f = new Font("Georgia", 40f, FontStyle.Bold);
            string line1 = "Born from a";
            string line2Gold = "Belief";
            string line2Cream = "in the Wild";

            SizeF l1 = g.MeasureString(line1, f);
            SizeF l2Gold = g.MeasureString(line2Gold, f);
            SizeF l2Cream = g.MeasureString(line2Cream, f);

            float y1 = 18f;
            float y2 = 82f;
            float x1 = (_heroTitleArt.Width - l1.Width) / 2f;
            float line2Width = l2Gold.Width + l2Cream.Width - 10f;
            float x2 = (_heroTitleArt.Width - line2Width) / 2f;

            using var cream = new SolidBrush(Color.FromArgb(248, 244, 239));
            using var gold = new SolidBrush(_gold);
            g.DrawString(line1, f, cream, x1, y1);
            g.DrawString(line2Gold, f, gold, x2, y2);
            g.DrawString(line2Cream, f, cream, x2 + l2Gold.Width - 10f, y2);
        }

        private void PaintFoundedStrip(object sender, PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = pnlFoundedStrip.ClientRectangle; r.Inflate(-1, -1);
            using var gp = RR(r, 15);
            g.FillPath(new SolidBrush(Color.FromArgb(28, 212, 160, 23)), gp);
            g.DrawPath(new Pen(Color.FromArgb(70, 212, 160, 23), 1.2f), gp);
            int x = 16;
            using var fn = new Font("Segoe UI", 8.5f);
            using var fb = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            void P(string t, bool bold, Color c)
            {
                var f = bold ? fb : fn;
                TextRenderer.DrawText(g, t, f, new Point(x, 6), c);
                x += TextRenderer.MeasureText(g, t, f).Width - 3;
            }
            P("Established ", false, Color.FromArgb(155, 248, 244, 239));
            P("1995", true, _gold);
            P("  ·  Carmen, Cebu  ·  ", false, Color.FromArgb(155, 248, 244, 239));
            P("30+", true, _gold);
            P(" years of conservation", false, Color.FromArgb(155, 248, 244, 239));
        }

        // ═══════════════════════════════════════════════════════════════
        //  STATUS BAR  (60px, matches Visit page exactly)
        // ═══════════════════════════════════════════════════════════════
        private void BuildStatusBar()
        {
            pnlStatusBar.BackColor = Color.FromArgb(17, 43, 28);
            pnlStatusBar.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                int w = pnlStatusBar.Width, h = pnlStatusBar.Height;
                g.DrawLine(new Pen(Color.FromArgb(55, 212, 160, 23), 1), 0, 0, w, 0);
                g.DrawLine(new Pen(Color.FromArgb(55, 212, 160, 23), 1), 0, h - 1, w, h - 1);

                string[] icons = { "🌿", "🐾", "👷", "⭐" };
                string[] bolds = { "170ha", "35+", "280+", "4.9★" };
                string[] plains = { " of restored sanctuary", " resident species", " local jobs created", " average guest rating" };

                int contentW = Math.Min(w - 120, 1000);
                int startX = Math.Max(48, (w - contentW) / 2);
                int colW = contentW / 4;
                using var fi = new Font("Segoe UI Emoji", 11);
                using var fb = new Font("Segoe UI", 9, FontStyle.Bold);
                using var fn = new Font("Segoe UI", 9);

                for (int i = 0; i < 4; i++)
                {
                    int iw = TextRenderer.MeasureText(g, icons[i], fi).Width
                           + TextRenderer.MeasureText(g, bolds[i], fb).Width
                           + TextRenderer.MeasureText(g, plains[i], fn).Width - 10;
                    int sx = startX + i * colW + (colW - iw) / 2;
                    int ty = (h - 18) / 2;
                    TextRenderer.DrawText(g, icons[i], fi, new Point(sx, ty - 1), Color.FromArgb(200, 248, 244, 239));
                    sx += TextRenderer.MeasureText(g, icons[i], fi).Width - 5;
                    TextRenderer.DrawText(g, bolds[i], fb, new Point(sx, ty), _gold);
                    sx += TextRenderer.MeasureText(g, bolds[i], fb).Width - 3;
                    TextRenderer.DrawText(g, plains[i], fn, new Point(sx, ty), Color.FromArgb(165, 248, 244, 239));
                    if (i < 3)
                        g.DrawLine(new Pen(Color.FromArgb(36, 212, 160, 23), 1),
                            startX + (i + 1) * colW, 10, startX + (i + 1) * colW, h - 10);
                }
            };
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB BAR  (full-width dark bar, 5 tabs, inside hero bottom)
        // ═══════════════════════════════════════════════════════════════
        private void BuildTabBar()
        {
            tlpTabs.BackColor = Color.Transparent;
            tlpTabs.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
            tlpTabs.Padding = new Padding(0);
            tlpTabs.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Full-width pill bar — 8 px inset each side, 10 px vertical inset
                int pillX = 8, pillY = 8;
                int pillW = tlpTabs.Width - 16;
                int pillH = TAB_H - 16;   // 80 px with 96px TAB_H
                var pill = new Rectangle(pillX, pillY, pillW, pillH);
                using var pgp = RR(pill, 20);
                g.FillPath(new SolidBrush(Color.FromArgb(236, 10, 29, 16)), pgp);
                g.DrawPath(new Pen(Color.FromArgb(64, 212, 160, 23), 1.25f), pgp);
            };

            string[] icons = { "📖", "🎯", "🧑\u200d🌾", "🕰️", "🌱" };
            string[] labs = { "Our Story", "Mission & Values", "Our Team", "History", "Impact" };
            Label[] lbls = { lblOurStory, lblMission, lblTeam, lblHistory, lblImpact };

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var lbl = lbls[i];
                lbl.    Dock = DockStyle.Fill;
                lbl.Text = "";
                lbl.BackColor = Color.Transparent;
                lbl.Cursor = Cursors.Hand;
                lbl.Tag = new object[] { icons[idx], labs[idx], idx };

                lbl.Paint += (s, pe) =>
                {
                    var l = (Label)s;
                    var tag = (object[])l.Tag;
                    int ti = (int)tag[2];
                    bool active = _activeTab == ti;
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    if (active)
                    {
                        var r2 = new Rectangle(10, 14, l.Width - 20, l.Height - 26);
                        using var gp2 = RR(r2, 14);
                        g.FillPath(new SolidBrush(Color.FromArgb(26, 212, 160, 23)), gp2);
                        g.DrawPath(new Pen(Color.FromArgb(88, 212, 160, 23), 1.1f), gp2);
                    }

                    Color mc = active ? _gold : Color.FromArgb(128, 248, 244, 239);

                    // Icon — vertically centred in upper ~55% of cell
                    TextRenderer.DrawText(g, (string)tag[0],
                        new Font("Segoe UI Emoji", 15),
                        new Rectangle(0, 14, l.Width, 30), mc, TextFormatFlags.HorizontalCenter);

                    // Label — sits 4 px below icon, never cut off
                    TextRenderer.DrawText(g, (string)tag[1],
                        new Font("Segoe UI", 7.5f, active ? FontStyle.Bold : FontStyle.Regular),
                        new Rectangle(2, 46, l.Width - 4, 22), mc,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix);

                    // Gold underline on active
                    if (active)
                    {
                        int lw = 24, lx2 = (l.Width - lw) / 2;
                        g.DrawLine(new Pen(_gold, 2f), lx2, l.Height - 8, lx2 + lw, l.Height - 8);
                    }

                    // Vertical separator between tabs
                    if (ti < 4)
                        g.DrawLine(new Pen(Color.FromArgb(20, 255, 255, 255), 1),
                            l.Width - 1, 14, l.Width - 1, l.Height - 14);
                };
                lbl.Click += (s, e) => LoadTab(idx);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  CONTENT AREA  —  Our Story (tab 0)
        //  KEY FIX: pnlContentArea is Dock=Fill so it uses full width
        // ═══════════════════════════════════════════════════════════════
        private void BuildContentArea()
        {
            // pnlContentArea already set to Dock=Fill in constructor

            // ── Header ─────────────────────────────────────────────────
            lblEyebrow.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblEyebrow.ForeColor = Color.FromArgb(180, 140, 10);
            lblEyebrow.BackColor = Color.Transparent;
            lblEyebrow.Text = "OUR STORY";
            lblEyebrow.AutoSize = true;
            lblEyebrow.Location = new Point(54, 32);

            lblHowBegan.Font = new Font("Georgia", 28, FontStyle.Bold);
            lblHowBegan.ForeColor = Color.FromArgb(26, 26, 26);
            lblHowBegan.BackColor = Color.Transparent;
            lblHowBegan.Text = "How WildNest Began";
            lblHowBegan.AutoSize = false;
            lblHowBegan.Size = new Size(700, 50);
            lblHowBegan.Location = new Point(54, 58);

            lblSubtitle.Font = new Font("Segoe UI", 10);
            lblSubtitle.ForeColor = Color.FromArgb(140, 140, 140);
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Text = "From a single rescued eagle to Southeast Asia's leading wildlife sanctuary";
            lblSubtitle.AutoSize = false;
            lblSubtitle.Size = new Size(800, 24);
            lblSubtitle.Location = new Point(54, 114);

            // Gold divider line after eyebrow
            pnlContentArea.Paint += (s, pe) =>
            {
                if (_activeTab != 0) return;
                int lx = lblEyebrow.Right + 10, ly = lblEyebrow.Top + lblEyebrow.Height / 2;
                pe.Graphics.DrawLine(new Pen(Color.FromArgb(50, 180, 160, 23), 1),
                    lx, ly, pnlContentArea.Width - 48, ly);
            };

            // ── Mission statement card ──────────────────────────────────
            pnlMissionStatement.BackColor = Color.Transparent;
            pnlMissionStatement.Location = new Point(48, 146);
            // Width set in resize handler
            pnlMissionStatement.Size = new Size(1166, 190);
            pnlMissionStatement.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlMissionStatement.Paint += PaintMission;

            // ── 3 pillar cards ─────────────────────────────────────────
            const int cY = 348, cH = 272, gap = 20, mx = 48;
            pnlConservation.Location = new Point(mx, cY);
            pnlConservation.Size = new Size(364, cH);
            pnlConservation.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pnlEducation.Location = new Point(mx + 384, cY);
            pnlEducation.Size = new Size(364, cH);
            pnlEducation.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            pnlCommunity.Location = new Point(mx + 768, cY);
            pnlCommunity.Size = new Size(364, cH);
            pnlCommunity.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            PaintPillar(pnlConservation, 0);
            PaintPillar(pnlEducation, 1);
            PaintPillar(pnlCommunity, 2);

            int footerY = cY + cH + 28;
            AddScrollFooter(pnlContentArea, footerY);
            pnlContentArea.Height = footerY + 60 + 20;
            this.AutoScrollMinSize = new Size(0, pnlHero.Height + pnlStatusBar.Height + pnlContentArea.Height);
            // Resize handler: distribute 3 cards evenly across full width
            pnlContentArea.Resize += (s, e) =>
            {
                int w = pnlContentArea.ClientSize.Width;
                if (w < 300) return;
                int avail = w - mx * 2 - gap * 2;
                int cw = avail / 3;
                pnlMissionStatement.SetBounds(mx, 146, w - mx * 2, 190);
                pnlConservation.SetBounds(mx, cY, cw, cH);
                pnlEducation.SetBounds(mx + cw + gap, cY, cw, cH);
                pnlCommunity.SetBounds(mx + (cw + gap) * 2, cY, cw, cH);
                foreach (Control c in pnlContentArea.Controls)
                    if (c.Name == "pnlFooter") { c.Width = w; break; }
            };
        }

        // ── Mission card paint ────────────────────────────────────────
        private void PaintMission(object sender, PaintEventArgs pe)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int pw = pnlMissionStatement.Width, ph = pnlMissionStatement.Height;
            var r = new Rectangle(0, 0, pw - 2, ph - 2);
            using var gp = RR(r, 18);
            g.FillPath(Brushes.White, gp);
            g.DrawPath(new Pen(Color.FromArgb(215, 212, 206), 0.8f), gp);

            // Gold gradient top bar
            using var lgb = new LinearGradientBrush(new Point(20, 0), new Point(pw - 20, 0), Color.Transparent, Color.Transparent);
            lgb.InterpolationColors = new ColorBlend
            {
                Colors = new[] { Color.Transparent, _gold, Color.Transparent },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            g.FillRectangle(lgb, new Rectangle(20, 0, pw - 40, 3));

            TextRenderer.DrawText(g, "OUR MISSION STATEMENT",
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new Rectangle(0, 16, pw, 16), _gold, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "To protect, rehabilitate, and celebrate native and endangered wildlife",
                new Font("Georgia", 13, FontStyle.Bold),
                new Rectangle(80, 42, pw - 160, 24), Color.FromArgb(26, 26, 26), TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "through world-class conservation — while creating transformative experiences",
                new Font("Georgia", 13),
                new Rectangle(80, 70, pw - 160, 24), Color.FromArgb(26, 26, 26), TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "that connect every guest to the natural world in a way they will never forget.",
                new Font("Georgia", 13),
                new Rectangle(80, 98, pw - 160, 24), Color.FromArgb(26, 26, 26), TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "— WildNest Founding Charter, 1995",
                new Font("Segoe UI", 8, FontStyle.Italic),
                new Rectangle(80, 144, pw - 160, 18), Color.FromArgb(160, 160, 160), TextFormatFlags.HorizontalCenter);
        }

        // ── Pillar card data + paint ──────────────────────────────────
        private static readonly string[] _pLbls = { "01 — CONSERVATION", "02 — EDUCATION", "03 — COMMUNITY" };
        private static readonly string[] _pIcons = { "🌿", "📚", "🤝" };
        private static readonly string[] _pTitles = { "Wildlife First, Always", "Knowledge That Changes Lives", "Rooted in Carmen" };
        private static readonly string[] _pBodies = {
            "Every habitat, schedule, and guest interaction is designed around the well-being of our animals. Our residents live on their own terms — never forced to perform.",
            "We believe that a person who truly understands an animal will fight to protect it. Every ranger-led experience builds that connection from child to scientist.",
            "WildNest was built by, and for, the people of Carmen. 80% of our team are local hires. Every booking funds conservation jobs and community scholarships."
        };
        private static readonly string[] _pStats = { "47+", "12,000+", "280+" };
        private static readonly string[] _pStatLbls = { "Species under active care", "Students reached annually", "Local jobs created" };
        private static readonly Color[] _pColors = {
            Color.FromArgb(29, 158, 85), Color.FromArgb(212, 160, 23), Color.FromArgb(59, 130, 246)
        };

        private void PaintPillar(Panel card, int ci)
        {
            card.BackColor = Color.Transparent;
            card.Paint += (s, pe) =>
            {
                var p = (Panel)s;
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var r = new Rectangle(0, 0, p.Width - 2, p.Height - 2);
                using var gp = RR(r, 16);
                g.FillPath(Brushes.White, gp);
                g.DrawPath(new Pen(Color.FromArgb(215, 212, 206), 0.8f), gp);
                g.DrawLine(new Pen(_pColors[ci], 3), 16, 0, r.Width - 16, 0);

                TextRenderer.DrawText(g, _pLbls[ci],
                    new Font("Segoe UI", 7f, FontStyle.Bold),
                    new Rectangle(18, 14, p.Width - 36, 16), _pColors[ci], TextFormatFlags.Left);
                TextRenderer.DrawText(g, _pIcons[ci],
                    new Font("Segoe UI Emoji", 26),
                    new Rectangle(16, 34, 54, 50), Color.Black, TextFormatFlags.Left);
                TextRenderer.DrawText(g, _pTitles[ci],
                    new Font("Georgia", 14, FontStyle.Bold),
                    new Rectangle(16, 90, p.Width - 32, 28), Color.FromArgb(26, 26, 26), TextFormatFlags.Left);
                TextRenderer.DrawText(g, _pBodies[ci],
                    new Font("Segoe UI", 8.5f),
                    new Rectangle(16, 124, p.Width - 32, 90), Color.FromArgb(95, 95, 95),
                    TextFormatFlags.Left | TextFormatFlags.WordBreak);
                g.DrawLine(new Pen(Color.FromArgb(215, 212, 206), 1), 16, 220, p.Width - 16, 220);
                TextRenderer.DrawText(g, _pStats[ci],
                    new Font("Georgia", 24, FontStyle.Bold),
                    new Rectangle(16, 226, 140, 32), _pColors[ci], TextFormatFlags.Left);
                TextRenderer.DrawText(g, _pStatLbls[ci],
                    new Font("Segoe UI", 7.5f),
                    new Rectangle(16, 258, p.Width - 32, 16), Color.FromArgb(130, 130, 130), TextFormatFlags.Left);
            };
        }

        // ── Footer (end-scroll dark green panel) ─────────────────────
        private void AddScrollFooter(Panel parent, int y)
        {
            var f = new Panel
            {
                BackColor = _darkGreen,
                Location = new Point(0, y),
                Size = new Size(parent.ClientSize.Width > 0 ? parent.ClientSize.Width : 1262, 60),
                Name = "pnlFooter",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            f.Paint += (s, pe) =>
            {
                var p = (Panel)s;
                pe.Graphics.DrawLine(new Pen(Color.FromArgb(70, 212, 160, 23), 1), 0, 0, p.Width, 0);
                TextRenderer.DrawText(pe.Graphics,
                    "🌿  WildNest Resort & Wildlife Experience  ·  Carmen, Cebu, Philippines  ·  © 2026",
                    new Font("Segoe UI", 8.5f),
                    new Rectangle(0, 0, p.Width, p.Height),
                    Color.FromArgb(90, 212, 160, 23),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            parent.Controls.Add(f);
        }

        // ═══════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ═══════════════════════════════════════════════════════════════
        private void LoadTab(int idx)
        {
            _activeTab = idx;
            foreach (var l in new[] { lblOurStory, lblMission, lblTeam, lblHistory, lblImpact })
                l.Invalidate();
            tlpTabs.Invalidate();

            // Remove previously loaded UCs + dynamic footer
            for (int i = pnlContentArea.Controls.Count - 1; i >= 0; i--)
            {
                var c = pnlContentArea.Controls[i];
                if (c is UserControl || c.Name == "pnlDynFooter")
                { pnlContentArea.Controls.RemoveAt(i); c.Dispose(); }
            }

            bool story = (idx == 0);
            lblEyebrow.Visible = story;
            lblHowBegan.Visible = story;
            lblSubtitle.Visible = story;
            pnlMissionStatement.Visible = story;
            pnlConservation.Visible = story;
            pnlEducation.Visible = story;
            pnlCommunity.Visible = story;
            foreach (Control c in pnlContentArea.Controls)
                if (c.Name == "pnlFooter") { c.Visible = story; break; }

          

            pnlContentArea.AutoScrollPosition = Point.Empty;

            if (!story)
            {
                UserControl uc = idx switch
                {
                    1 => new UcMission(),
                    2 => new UcTeam(),
                    3 => new UcHistory(),
                    4 => new UcImpacts(),
                    _ => null
                };
                if (uc == null) return;

                int ucH = GetTabContentHeight(idx, uc);
                int cw = pnlContentArea.ClientSize.Width;
                uc.Location = Point.Empty;
                uc.Size = new Size(cw, ucH);
                uc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                pnlContentArea.Controls.Add(uc);
                uc.BringToFront();

                // Keep UC width matching content area on resize
                EventHandler ucResize = null;
                ucResize = (s, e) =>
                {
                    if (uc.IsDisposed) { pnlContentArea.Resize -= ucResize; return; }
                    uc.Width = pnlContentArea.ClientSize.Width;
                    foreach (Control c2 in pnlContentArea.Controls)
                        if (c2.Name == "pnlDynFooter") { c2.Width = pnlContentArea.ClientSize.Width; break; }
                };
                pnlContentArea.Resize += ucResize;

                // Dynamic footer — scroll stops here
                var df = new Panel
                {
                    BackColor = _darkGreen,
                    Location = new Point(0, ucH),
                    Size = new Size(cw, 60),
                    Name = "pnlDynFooter"
                };
                df.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    pe.Graphics.DrawLine(new Pen(Color.FromArgb(70, 212, 160, 23), 1), 0, 0, p.Width, 0);
                    TextRenderer.DrawText(pe.Graphics,
                        "🌿  WildNest Resort & Wildlife Experience  ·  Carmen, Cebu, Philippines  ·  © 2026",
                        new Font("Segoe UI", 8.5f),
                        new Rectangle(0, 0, p.Width, p.Height),
                        Color.FromArgb(90, 212, 160, 23),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
                pnlContentArea.Controls.Add(df);
                pnlContentArea.Height = ucH + 60 + 20;
                this.AutoScrollMinSize = new Size(0, pnlHero.Height + pnlStatusBar.Height + pnlContentArea.Height);
            }

            pnlContentArea.Invalidate(true);
            UpdateOuterHeight();
        }

        private void UpdateOuterHeight()
        {
            this.Height = pnlContentArea.Bottom;
            this.MinimumSize = new Size(0, this.Height);
        }

        private int GetTabContentHeight(int idx, UserControl uc)
        {
            return idx switch
            {
                1 => Math.Max(760, uc.Height),
                2 => Math.Max(760, uc.Height),
                3 => Math.Max(920, uc.Height),
                4 => Math.Max(560, uc.Height),
                _ => Math.Max(690, uc.Height)
            };
        }

        // ── Rounded rect helper ───────────────────────────────────────
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

        private void lblSubtitle_Click(object sender, EventArgs e) { }
    }
}
