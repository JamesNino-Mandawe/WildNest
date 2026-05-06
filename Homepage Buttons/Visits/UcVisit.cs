using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using Project.Homepage_Buttons.Visits;

namespace Project
{
    public partial class UcVisit : UserControl
    {
        private UcGettingHere _ucGettingHere;
        private UcWhatToExpect _ucWhatToExpect;
        private UcFaq _ucFaq;
        private int _activeTab = 0;

        static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        static readonly Color C_DARK2 = Color.FromArgb(12, 36, 22);
        static readonly Color C_SBAR = Color.FromArgb(17, 43, 28);
        static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        static readonly Color C_GREEN = Color.FromArgb(29, 158, 117);
        static readonly Color C_RED = Color.FromArgb(226, 75, 74);
        static readonly Color C_CARD_BRD = Color.FromArgb(224, 221, 216);
        static readonly Color C_TEXT_DARK = Color.FromArgb(26, 26, 26);

        // Hours-tab footer (scroll stop anchor)
        private Panel _hoursFooter;

        public UcVisit()
        {
            InitializeComponent();
            this.AutoScroll = false;
            this.AutoScrollMinSize = Size.Empty;
            pnlContentArea.Dock = DockStyle.Top;
            this.DoubleBuffered = true;
            this.BackColor = C_BG;
            StyleHero();
            StyleStatusBar();
            StyleTabBar();
            StyleHoursContent();
            TabClicked(0);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HERO  (550 px)
        // ════════════════════════════════════════════════════════════════════
        private void StyleHero()
        {
            pnlHero.BackColor = C_DARK;
            pnlHero.Paint += PaintHero;

            lblLocation.Font = new Font("Segoe UI", 9f);
            lblLocation.ForeColor = C_GOLD;
            lblLocation.BackColor = Color.Transparent;
            lblLocation.Text = "Carmen, Cebu  —  Philippines";

            lblHeroTitle.BackColor = Color.Transparent;
            lblHeroTitle.Text = "";
            lblHeroTitle.Paint += PaintHeroTitle;

            lblHeroSub.Font = new Font("Segoe UI", 10.5f);
            lblHeroSub.ForeColor = Color.FromArgb(130, 248, 244, 239);
            lblHeroSub.BackColor = Color.Transparent;
            lblHeroSub.AutoSize = false;
            lblHeroSub.Size = new Size(760, 30);
            lblHeroSub.TextAlign = ContentAlignment.MiddleCenter;
            lblHeroSub.Text = "Everything you need before arriving at WildNest.  Select a topic to get started.";

            pnlHero.Resize += (s, e) => CentreHeroElements();
            CentreHeroElements();
        }

        private void PaintHero(object s, PaintEventArgs e)
        {
            HeroSurfacePainter.Paint(e.Graphics, pnlHero.ClientRectangle, HeroSurfaceVariant.Visit);
        }

        private void PaintHeroTitle(object s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var f = new Font("Georgia", 46f, FontStyle.Bold);
            string p1 = "Plan Your ", p2 = "Visit";
            var sz1 = g.MeasureString(p1, f);
            float tot = sz1.Width + g.MeasureString(p2, f).Width - 20f;
            float sx = (lblHeroTitle.Width - tot) / 2f;
            float sy = (lblHeroTitle.Height - sz1.Height) / 2f;
            g.DrawString(p1, f, new SolidBrush(C_CREAM), sx, sy);
            g.DrawString(p2, f, new SolidBrush(C_GOLD), sx + sz1.Width - 20f, sy);
        }

        private void CentreHeroElements()
        {
            int cx = pnlHero.Width / 2;
            lblLocation.Location = new Point(cx - lblLocation.PreferredWidth / 2, 165);
            lblHeroTitle.Location = new Point(cx - lblHeroTitle.Width / 2, 218);
            lblHeroSub.Location = new Point(cx - lblHeroSub.PreferredWidth / 2, 308);
            visit_tabs.Location = new Point(cx - visit_tabs.Width / 2, 380);
        }

        // ════════════════════════════════════════════════════════════════════
        //  STATUS BAR  (60 px)
        // ════════════════════════════════════════════════════════════════════
        private void StyleStatusBar()
        {
            pnlStatusBar.BackColor = C_SBAR;
            lblCabinStatus.Visible = false;
            lblSafariTimer.Visible = false;
            lblAnimalCount.Visible = false;
            tableLayoutPanel1.Visible = false;

            pnlStatusBar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 212, 160, 23)),
                    0, 0, pnlStatusBar.Width, 1);
                g.FillRectangle(new SolidBrush(Color.FromArgb(25, 212, 160, 23)),
                    0, pnlStatusBar.Height - 1, pnlStatusBar.Width, 1);

                var items = new[] {
                    (Color.FromArgb(29, 158, 117), "Open Today",           "8:00 AM – 6:00 PM", false),
                    (Color.FromArgb(212, 160, 23),  "Night Safari tonight", "7:30 PM",            true),
                    (Color.FromArgb(29, 158, 117),  "Adult from",          "₱450",               false),
                    (Color.FromArgb(91, 196, 245),  "~45 min",             "from Cebu City",     false),
                };

                int colW = pnlStatusBar.Width / 4;
                using var lblF = new Font("Segoe UI", 8f);
                using var valF = new Font("Segoe UI", 8f, FontStyle.Bold);

                for (int i = 0; i < items.Length; i++)
                {
                    var (dotC, label, val, isGold) = items[i];
                    int colCx = i * colW + colW / 2;
                    float blockW = Math.Max(g.MeasureString(label, lblF).Width,
                                           g.MeasureString(val, valF).Width) + 14f;
                    float bx = colCx - blockW / 2f;

                    g.FillEllipse(new SolidBrush(dotC), bx, 16, 7, 7);
                    g.DrawString(label, lblF,
                        new SolidBrush(Color.FromArgb(160, 248, 244, 239)), bx + 14, 10);
                    g.DrawString(val, valF,
                        new SolidBrush(isGold ? C_GOLD : C_CREAM), bx + 14, 30);

                    if (i < items.Length - 1)
                        g.DrawLine(new Pen(Color.FromArgb(28, 255, 255, 255), 1f),
                            (i + 1) * colW, 12, (i + 1) * colW, 46);
                }
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB BAR
        // ════════════════════════════════════════════════════════════════════
        private void StyleTabBar()
        {
            visit_tabs.BackColor = Color.Transparent;
            visit_tabs.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(
                    new Rectangle(0, 0, visit_tabs.Width - 1, visit_tabs.Height - 1), 20);
                g.FillPath(new SolidBrush(Color.FromArgb(236, 10, 29, 16)), path);
                g.DrawPath(new Pen(Color.FromArgb(66, 212, 160, 23), 1.25f), path);
            };

            string[] icons = { "🕗", "📍", "🎒", "❓" };
            string[] labels = { "Hours", "Getting Here", "What to Expect", "FAQ" };
            Label[] tabs = { lblHours, lblGettingHere, lblWhatToExpect, lblFAQ };

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                string icon = icons[i];
                string lbl = labels[i];
                var lb = tabs[i];

                lb.BackColor = Color.Transparent;
                lb.ForeColor = Color.Transparent;
                lb.Text = "";
                lb.Cursor = Cursors.Hand;
                lb.AutoSize = false;

                lb.Paint += (s, e) =>
                {
                    var g = e.Graphics;
                    bool active = idx == _activeTab;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    if (active)
                    {
                        using var ap = RoundedRect(
                            new Rectangle(10, 12, lb.Width - 20, lb.Height - 24), 16);
                        g.FillPath(new SolidBrush(Color.FromArgb(26, 212, 160, 23)), ap);
                        g.DrawPath(new Pen(Color.FromArgb(86, 212, 160, 23), 1f), ap);
                    }

                    if (idx < 3)
                        g.DrawLine(new Pen(Color.FromArgb(18, 255, 255, 255), 1f),
                            lb.Width - 1, 10, lb.Width - 1, lb.Height - 10);

                    using var iFont = new Font("Segoe UI Emoji", 16f);
                    var iSz = g.MeasureString(icon, iFont);
                    Brush iBr = active ? Brushes.White
                                       : new SolidBrush(Color.FromArgb(90, 248, 244, 239));
                    g.DrawString(icon, iFont, iBr, (lb.Width - iSz.Width) / 2f, 12f);

                    using var lFont = new Font("Segoe UI", 7.5f,
                        active ? FontStyle.Bold : FontStyle.Regular);
                    var lColor = active ? C_GOLD : Color.FromArgb(110, 248, 244, 239);
                    var lSz = g.MeasureString(lbl, lFont);
                    g.DrawString(lbl, lFont, new SolidBrush(lColor),
                        (lb.Width - lSz.Width) / 2f, 46f);

                    if (active)
                        g.DrawLine(new Pen(C_GOLD, 2.5f),
                            lb.Width / 2 - 16, lb.Height - 7,
                            lb.Width / 2 + 16, lb.Height - 7);
                };

                lb.Click += (s, e) =>
                {
                    _activeTab = idx;
                    foreach (var t in tabs) t.Invalidate();
                    TabClicked(idx);
                };
            }

            _ucGettingHere = new UcGettingHere { Dock = DockStyle.Fill, Visible = false };
            _ucWhatToExpect = new UcWhatToExpect { Dock = DockStyle.Fill, Visible = false };
            _ucFaq = new UcFaq { Dock = DockStyle.Fill, Visible = false };

            _ucGettingHere.SizeChanged += (s, e) => SyncActiveSubPageHeight(_ucGettingHere);
            _ucWhatToExpect.SizeChanged += (s, e) => SyncActiveSubPageHeight(_ucWhatToExpect);
            _ucFaq.SizeChanged += (s, e) => SyncActiveSubPageHeight(_ucFaq);

            pnlContentArea.Controls.Add(_ucGettingHere);
            pnlContentArea.Controls.Add(_ucWhatToExpect);
            pnlContentArea.Controls.Add(_ucFaq);
        }

        // ════════════════════════════════════════════════════════════════════
        //  HOURS CONTENT
        // ════════════════════════════════════════════════════════════════════
        private void StyleHoursContent()
        {
            pnlContentArea.BackColor = C_BG;

            // ── eyebrow ──
            lblOpeningHours.Font = new Font("Segoe UI", 8f);
            lblOpeningHours.ForeColor = C_GOLD;
            lblOpeningHours.BackColor = Color.Transparent;
            lblOpeningHours.Text = "OPENING HOURS";

            pnlContentArea.Paint += (s, e) =>
            {
                if (!pnlHoursContent.Visible) return;
                e.Graphics.DrawLine(new Pen(Color.FromArgb(45, 212, 160, 23), 1f),
                    lblOpeningHours.Right + 10, lblOpeningHours.Top + 9,
                    pnlContentArea.Width - 68, lblOpeningHours.Top + 9);
            };

            lblWhenToVisit.Font = new Font("Georgia", 28f, FontStyle.Bold);
            lblWhenToVisit.ForeColor = C_TEXT_DARK;
            lblWhenToVisit.BackColor = Color.Transparent;
            lblWhenToVisit.Text = "When to Visit";

            lblLastEntry.Font = new Font("Segoe UI", 9.5f);
            lblLastEntry.ForeColor = Color.FromArgb(136, 136, 136);
            lblLastEntry.BackColor = Color.Transparent;
            lblLastEntry.Text = "Last entry is 1 hour before closing — special sessions require advance booking";

            lblTodayOpen.BackColor = Color.Transparent;
            lblTodayOpen.Text = "";
            lblTodayOpen.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(
                    new Rectangle(0, 0, lblTodayOpen.Width - 1, lblTodayOpen.Height - 1), 15);
                g.FillPath(new SolidBrush(Color.FromArgb(225, 245, 238)), path);
                g.DrawPath(new Pen(Color.FromArgb(155, 225, 203), 1f), path);
                g.FillEllipse(new SolidBrush(C_GREEN), 12, 11, 8, 8);
                g.DrawString("Today: Open — 8:00 AM to 6:00 PM",
                    new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    new SolidBrush(Color.FromArgb(15, 110, 86)), 29, 7);
            };

            // ── Cards resize to fill full width ──
            pnlHoursContent.Resize += (s, e) =>
            {
                int halfW = (pnlHoursContent.Width - 22) / 2;
                pnlRegularHours.Width = halfW;
                pnlSpecialHours.Width = halfW;
                pnlSpecialHours.Left = halfW + 22;
                pnlRegularHours.Height = pnlHoursContent.Height;
                pnlSpecialHours.Height = pnlHoursContent.Height;
            };

            pnlContentArea.Resize += (s, e) =>
            {
                int newW = pnlContentArea.Width - 120;
                if (newW < 400) newW = 400;
                pnlHoursContent.Width = newW;
                pnlReserve.Width = newW;
                pnlHoursContent.Left = 60;
                pnlReserve.Left = 60;
                // Update reserve position to be flush under hours content
                pnlReserve.Top = pnlHoursContent.Bottom + 22;
                ApplyHoursScrollStop();
            };

            PaintHoursCard(pnlRegularHours,
                "📅", "Regular Operating Hours",
                "All 8 wildlife zones open unless noted",
                new (string, string, bool)[] {
                    ("Monday",    "8:00 AM — 6:00 PM", false),
                    ("Tuesday",   "8:00 AM — 6:00 PM", false),
                    ("Wednesday", "8:00 AM — 6:00 PM", false),
                    ("Thursday",  "8:00 AM — 6:00 PM", false),
                    ("Friday",    "8:00 AM — 8:00 PM", false),
                    ("Saturday",  "7:00 AM — 9:00 PM", false),
                    ("Sunday",    "Closed",             true),
                },
                "⚠  Last regular entry 1 hour before closing. Night Safari guests use a separate gate.");

            PaintHoursCard(pnlSpecialHours,
                "🌙", "Special Access Hours",
                "Advance booking required — no walk-ins",
                new (string, string, bool)[] {
                    ("Night Safari",         "7:30 PM — 9:30 PM", false),
                    ("Sunrise Walk",         "5:30 AM — 7:30 AM", false),
                    ("Keeper Experience",    "6:00 AM — 9:00 AM", false),
                    ("Grand Safari Circuit", "9:00 AM — 1:00 PM", false),
                    ("Animal Feeding",       "3:00 PM — 3:45 PM", false),
                    ("Photo Session",        "By appointment",    false),
                    ("Public Holidays",      "Check website",     true),
                },
                "✦  All special sessions must be booked at least 24 hours in advance.");

            // ── CTA reserve strip — info only, NO BUTTONS ──
            StyleReserveStrip();

            // ── Footer — THE scroll stop anchor ──
            _hoursFooter = new Panel
            {
                Height = 64,
                BackColor = Color.Transparent,
                Left = 0,
            };
            _hoursFooter.Paint += PaintHoursFooter;
            pnlContentArea.Controls.Add(_hoursFooter);

            ApplyHoursScrollStop();
        }

        // ── Called after any layout change in the Hours tab ──────────────
        private void ApplyHoursScrollStop()
        {
            if (_hoursFooter == null) return;

            // pnlReserve must be positioned first
            pnlReserve.Top = pnlHoursContent.Bottom + 22;

            // Footer sits immediately after reserve strip
            _hoursFooter.Top = pnlReserve.Bottom + 18;
            _hoursFooter.Width = Math.Max(pnlContentArea.Width, 400);

            // AutoScrollMinSize = exactly footer bottom → scroll STOPS at green panel
            if (_activeTab == 0)
                pnlContentArea.Height = _hoursFooter.Bottom;
            UpdateOuterHeight();
        }

        private void PaintHoursFooter(object s, PaintEventArgs e)
        {
            var fp = (Panel)s;
            var g = e.Graphics;
            using var lg = new LinearGradientBrush(new Point(0, 0), new Point(0, fp.Height),
                C_DARK, C_DARK2);
            g.FillRectangle(lg, fp.ClientRectangle);
            g.FillRectangle(new SolidBrush(Color.FromArgb(50, 212, 160, 23)), 0, 0, fp.Width, 1);
            using var bF = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString("WILDNEST", bF,
                new SolidBrush(Color.FromArgb(210, 248, 244, 239)), 60, (fp.Height - 14) / 2f - 1);
            string copy = "© 2026 WildNest Resort & Wildlife Experience. Carmen, Cebu, Philippines.";
            using var cF = new Font("Segoe UI", 8f);
            using var cB = new SolidBrush(Color.FromArgb(65, 248, 244, 239));
            var cSz = g.MeasureString(copy, cF);
            g.DrawString(copy, cF, cB, fp.Width - cSz.Width - 60, (fp.Height - cSz.Height) / 2f);
        }

        private void PaintHoursCard(Panel card, string icon, string title, string sub,
            (string day, string time, bool closed)[] rows, string note)
        {
            card.BackColor = Color.Transparent;
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using var shadow = RoundedRect(new Rectangle(2, 3, card.Width - 5, card.Height - 5), 16);
                g.FillPath(new SolidBrush(Color.FromArgb(16, 0, 0, 0)), shadow);

                using var path = RoundedRect(new Rectangle(0, 0, card.Width - 3, card.Height - 3), 16);
                g.FillPath(Brushes.White, path);
                g.DrawPath(new Pen(C_CARD_BRD, 0.5f), path);

                var hp = new GraphicsPath();
                hp.AddArc(0, 0, 32, 32, 180, 90);
                hp.AddArc(card.Width - 35, 0, 32, 32, 270, 90);
                hp.AddLine(card.Width - 3, 32, card.Width - 3, 68);
                hp.AddLine(0, 68, 0, 32);
                hp.CloseFigure();
                g.FillPath(new SolidBrush(C_DARK), hp);

                g.DrawString(icon, new Font("Segoe UI Emoji", 16f), Brushes.White, 16, 16);
                g.DrawString(title, new Font("Georgia", 12.5f, FontStyle.Bold),
                    new SolidBrush(C_CREAM), 54, 15);
                g.DrawString(sub, new Font("Segoe UI", 7.5f),
                    new SolidBrush(Color.FromArgb(100, 248, 244, 239)), 54, 38);

                int rowY = 76;
                using var dayF = new Font("Segoe UI", 9f);
                using var timeF = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var dotG = new SolidBrush(C_GREEN);
                using var dotR = new SolidBrush(C_RED);
                using var dayN = new SolidBrush(Color.FromArgb(68, 68, 68));
                using var dayCl = new SolidBrush(C_RED);
                using var timeN = new SolidBrush(C_TEXT_DARK);
                using var rowLn = new Pen(Color.FromArgb(245, 243, 240), 0.5f);

                foreach (var (day, time, closed) in rows)
                {
                    g.FillEllipse(closed ? dotR : dotG, 18, rowY + 7, 7, 7);
                    g.DrawString(day, dayF, closed ? dayCl : dayN, 34, rowY - 1);
                    var tSz = g.MeasureString(time, timeF);
                    g.DrawString(time, timeF, closed ? dayCl : timeN,
                        card.Width - (int)tSz.Width - 20, rowY - 1);
                    rowY += 26;
                    g.DrawLine(rowLn, 18, rowY, card.Width - 18, rowY);
                }

                int noteY = rowY + 10;
                if (noteY + 52 <= card.Height - 6)
                {
                    using var np = RoundedRect(new Rectangle(14, noteY, card.Width - 28, 52), 8);
                    g.FillPath(new SolidBrush(Color.FromArgb(248, 247, 244)), np);
                    g.DrawLine(new Pen(C_GOLD, 3f), 14, noteY + 5, 14, noteY + 47);
                    g.DrawString(note, new Font("Segoe UI", 7.5f),
                        new SolidBrush(Color.FromArgb(102, 102, 102)),
                        new RectangleF(22, noteY + 7, card.Width - 40, 44));
                }
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  CTA / RESERVE STRIP  (info only — NO buttons per requirements)
        // ════════════════════════════════════════════════════════════════════
        private void StyleReserveStrip()
        {
            // Height 130 px — enough for eyebrow + 20pt title + 2-line subtitle
            pnlReserve.Height = 130;
            pnlReserve.BackColor = Color.Transparent;

            pnlReserve.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using var path = RoundedRect(
                    new Rectangle(0, 0, pnlReserve.Width - 1, pnlReserve.Height - 1), 16);
                using var lg = new LinearGradientBrush(
                    new Point(0, 0), new Point(pnlReserve.Width, 0), C_DARK, C_DARK2);
                g.FillPath(lg, path);
                g.DrawPath(new Pen(Color.FromArgb(50, 212, 160, 23), 1f), path);

                // Radial glow
                using var gp = new GraphicsPath();
                gp.AddEllipse(pnlReserve.Width / 2 - 200, -40, 400, 200);
                using var pgb = new PathGradientBrush(gp);
                pgb.CenterColor = Color.FromArgb(14, 212, 160, 23);
                pgb.SurroundColors = new[] { Color.Transparent };
                g.FillPath(pgb, gp);

                // Eyebrow
                g.DrawString("READY TO BOOK?",
                    new Font("Segoe UI", 7.5f), new SolidBrush(C_GOLD), 36, 16);
                // Title
                g.DrawString("Reserve Your Spot Today",
                    new Font("Georgia", 20f, FontStyle.Bold), new SolidBrush(C_CREAM), 36, 34);
                // Subtitle — two lines, ample space
                g.DrawString(
                    "Night Safari and Grand Safari Circuit sell out days in advance.  Secure your slot before it's gone.",
                    new Font("Segoe UI", 9f),
                    new SolidBrush(Color.FromArgb(120, 248, 244, 239)),
                    new RectangleF(36, 82, pnlReserve.Width - 72, 40));
            };
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ════════════════════════════════════════════════════════════════════
        private void TabClicked(int idx)
        {
            bool hours = (idx == 0);

            pnlHoursContent.Visible = hours;
            pnlReserve.Visible = hours;
            lblOpeningHours.Visible = hours;
            lblWhenToVisit.Visible = hours;
            lblLastEntry.Visible = hours;
            lblTodayOpen.Visible = hours;
            if (_hoursFooter != null) _hoursFooter.Visible = hours;

            _ucGettingHere.Visible = (idx == 1);
            _ucWhatToExpect.Visible = (idx == 2);
            _ucFaq.Visible = (idx == 3);

            if (hours)
            {
                // Recalculate and apply exact scroll stop for Hours tab
                ApplyHoursScrollStop();
            }
            else
            {
                UserControl uc = idx == 1 ? (UserControl)_ucGettingHere
                               : idx == 2 ? _ucWhatToExpect
                               : (UserControl)_ucFaq;
                uc.BringToFront();
                pnlContentArea.Height = uc.Height;
                UpdateOuterHeight();
            }

            pnlContentArea.Invalidate();
        }

        private void UpdateOuterHeight()
        {
            this.Height = pnlContentArea.Bottom;
            this.MinimumSize = new Size(0, this.Height);
        }

        private void SyncActiveSubPageHeight(UserControl uc)
        {
            if (!uc.Visible || _activeTab == 0)
                return;

            pnlContentArea.Height = uc.Height;
            UpdateOuterHeight();
        }

        // ════════════════════════════════════════════════════════════════════
        //  HELPER
        // ════════════════════════════════════════════════════════════════════
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
