using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project.Homepage_Buttons.Visits
{
    public partial class UcFaq : UserControl
    {
        // ── Colours ───────────────────────────────────────────────────
        static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        static readonly Color C_DARK2 = Color.FromArgb(10, 30, 18);
        static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);
        static readonly Color C_BRD = Color.FromArgb(224, 221, 216);
        static readonly Color C_RED = Color.FromArgb(226, 75, 74);

        // ── Layout ───────────────────────────────────────────────────
        const int PAD_X = 60;
        const int HEADER_H = 128;
        const int CLOSED_H = 58;
        const int OPEN_H = 182;
        const int ITEM_GAP = 10;
        const int FOOTER_H = 64;

        // ── State ────────────────────────────────────────────────────
        private Panel _faqColumn;
        private Panel _footer;
        private bool _built = false;

        private class FaqItem
        {
            public string Question, Answer;
            public bool Open;
            public Panel Pnl;
        }
        private readonly List<FaqItem> _items = new();

        private static readonly (string Q, string A)[] DATA = {
            ("Can I bring my pet to WildNest?",
             "Pets are not permitted inside the sanctuary at any time. This policy protects our resident animals from stress, disease, and territorial behaviour. Pet-friendly areas are available outside the main gate."),
            ("Is WildNest suitable for very young children?",
             "Yes — children aged 3 and above are welcome with a guardian. Children under 3 enter free. The tram route is fully stroller-friendly and our rangers are trained in child-safe wildlife interaction."),
            ("Can I book experiences on the day of my visit?",
             "Walk-in bookings depend on availability. The Grand Safari Circuit, Keeper Experience, and Night Safari typically sell out 24–48 hours ahead. We strongly recommend booking online before your visit."),
            ("What is the cancellation policy?",
             "All admission tickets are refundable up to 48 hours before your visit. Experiences offer free cancellation up to 24 hours prior. No-shows and same-day cancellations are non-refundable. Rescheduling is free."),
            ("Is there WiFi inside the sanctuary?",
             "WiFi is available at the entrance, both dining venues, and the Conservation Hub. Wildlife zones intentionally have limited connectivity to reduce phone disturbance to animals — we encourage guests to be fully present."),
            ("Are drones or professional cameras allowed?",
             "Drones are strictly prohibited — they cause significant distress to wildlife. Professional camera equipment requires prior written approval from our media team at media@wildnest.ph."),
            ("Do you offer group or school discounts?",
             "Groups of 10+ receive a 15% discount on all admission tickets. Schools and educational institutions qualify for 25% with advance arrangement. Contact groups@wildnest.ph."),
        };

        // ── Constructor ──────────────────────────────────────────────
        public UcFaq()
        {
            InitializeComponent();
            this.BackColor = C_BG;
            this.AutoScroll = false;
            this.AutoScrollMinSize = Size.Empty;
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            StyleHeader();
            BuildSidePanels();
            BuildFooter();

            // HandleCreated fires after the control is fully shown with real dimensions
            this.HandleCreated += (s, e) => EnsureBuilt();
            this.Resize += (s, e) => { EnsureBuilt(); DoLayout(); };
        }

        // ── Ensure FAQ column is built exactly once with real width ───
        private void EnsureBuilt()
        {
            if (_built || this.Width < 100) return;
            _built = true;
            BuildFaqColumn();
            DoLayout();
        }

        // ── Header labels ─────────────────────────────────────────────
        private void StyleHeader()
        {
            lblFrequentlyAsked.ForeColor = C_GOLD;
            lblFrequentlyAsked.Font = new Font("Segoe UI", 8f);
            lblFrequentlyAsked.BackColor = Color.Transparent;
            lblFrequentlyAsked.Text = "FREQUENTLY ASKED";
            lblFrequentlyAsked.Location = new Point(PAD_X, 26);

            lblCommonQuestions.ForeColor = Color.FromArgb(26, 26, 26);
            lblCommonQuestions.Font = new Font("Georgia", 26f, FontStyle.Bold);
            lblCommonQuestions.BackColor = Color.Transparent;
            lblCommonQuestions.AutoSize = false;
            lblCommonQuestions.Size = new Size(600, 46);
            lblCommonQuestions.Location = new Point(PAD_X, 48);
            lblCommonQuestions.Text = "Common Questions";

            lblEverythingGuests.ForeColor = Color.FromArgb(136, 136, 136);
            lblEverythingGuests.Font = new Font("Segoe UI", 9.5f);
            lblEverythingGuests.BackColor = Color.Transparent;
            lblEverythingGuests.AutoSize = false;
            lblEverythingGuests.Size = new Size(660, 22);
            lblEverythingGuests.Location = new Point(PAD_X, 98);
            lblEverythingGuests.Text = "Everything guests ask before their first visit to WildNest";

            // Gold eyebrow rule (extends to the right)
            this.Paint += (s, e) =>
            {
                if (this.Width < 200) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawLine(
                    new Pen(Color.FromArgb(50, 212, 160, 23), 1f),
                    lblFrequentlyAsked.Right + 12, lblFrequentlyAsked.Top + 10,
                    this.Width - PAD_X, lblFrequentlyAsked.Top + 10);
            };
        }

        // ── Side panels ──────────────────────────────────────────────
        private void BuildSidePanels()
        {
            pnlWereHereToHelp.BackColor = Color.Transparent;
            pnlWereHereToHelp.Height = 256;
            pnlWereHereToHelp.Paint += (s, e) => DrawContactCard(e.Graphics, pnlWereHereToHelp);

            pnlQuickHours.BackColor = Color.Transparent;
            pnlQuickHours.Height = 230;
            pnlQuickHours.Paint += (s, e) => DrawQuickHours(e.Graphics, pnlQuickHours);
        }

        // ── Footer ───────────────────────────────────────────────────
        private void BuildFooter()
        {
            _footer = new Panel { Height = FOOTER_H, BackColor = Color.Transparent, Left = 0 };
            _footer.Paint += PaintFooter;
            this.Controls.Add(_footer);
        }

        // ── Build accordion column (called once after real Width known) ─
        private void BuildFaqColumn()
        {
            int sideW = CalcSideW();
            int faqW = CalcFaqW(sideW);

            _faqColumn = new Panel
            {
                BackColor = Color.Transparent,
                Left = PAD_X,
                Top = HEADER_H,
                Width = faqW,   // width set BEFORE items are created
                Height = 10,
            };
            this.Controls.Add(_faqColumn);
            _faqColumn.BringToFront(); // ensure it's above any stale controls

            int yy = 0;
            foreach (var (q, a) in DATA)
            {
                var item = new FaqItem { Question = q, Answer = a, Open = false };
                var pnl = MakeFaqPanel(item, faqW);
                pnl.Top = yy;
                pnl.Left = 0;
                item.Pnl = pnl;
                _items.Add(item);
                _faqColumn.Controls.Add(pnl);
                yy += pnl.Height + ITEM_GAP;
            }
            _faqColumn.Height = yy;
        }

        // ── Layout: called on every resize ───────────────────────────
        private void DoLayout()
        {
            if (_faqColumn == null || this.Width < 100) return;

            int sideW = CalcSideW();
            int faqW = CalcFaqW(sideW);
            int sideX = PAD_X + faqW + 28;

            // ── FAQ column ──
            _faqColumn.Left = PAD_X;
            _faqColumn.Top = HEADER_H;
            _faqColumn.Width = faqW;

            int yy = 0;
            foreach (var item in _items)
            {
                item.Pnl.Top = yy;
                item.Pnl.Left = 0;
                item.Pnl.Width = faqW;
                yy += item.Pnl.Height + ITEM_GAP;
            }
            _faqColumn.Height = Math.Max(yy, 10);

            // ── Side panels ──
            pnlWereHereToHelp.Left = sideX;
            pnlWereHereToHelp.Top = HEADER_H;
            pnlWereHereToHelp.Width = sideW;

            pnlQuickHours.Left = sideX;
            pnlQuickHours.Top = pnlWereHereToHelp.Bottom + 14;
            pnlQuickHours.Width = sideW;

            // ── Footer (scroll stop anchor) ──
            int contentEnd = Math.Max(_faqColumn.Bottom, pnlQuickHours.Bottom) + 30;
            _footer.Top = contentEnd;
            _footer.Left = 0;
            _footer.Width = Math.Max(this.Width, 200);

            // Scroll stops exactly at footer bottom — no dead space after green panel
            this.Height = _footer.Bottom;
            this.MinimumSize = new Size(0, this.Height);

            // Repaint header rule
            this.Invalidate(new Rectangle(0, 0, this.Width, HEADER_H));
        }

        private int CalcSideW() =>
            Math.Min(330, Math.Max(260, (int)((this.Width - PAD_X * 2) * 0.28f)));

        private int CalcFaqW(int sideW)
        {
            int w = this.Width - PAD_X * 2 - sideW - 28;
            return w < 340 ? 340 : w;
        }

        // ── Single FAQ panel ─────────────────────────────────────────
        private Panel MakeFaqPanel(FaqItem item, int w)
        {
            var p = new Panel
            {
                Width = w,
                Height = CLOSED_H,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            p.Paint += (s, e) => PaintFaqPanel(e.Graphics, p, item);
            p.Click += (s, e) =>
            {
                item.Open = !item.Open;
                p.Height = item.Open ? OPEN_H : CLOSED_H;
                p.Invalidate();
                DoLayout();
            };
            return p;
        }

        private void PaintFaqPanel(Graphics g, Panel p, FaqItem item)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = p.Width - 2;
            int h = p.Height - 2;
            if (w < 10 || h < 10) return;

            // Subtle shadow
            using var sh = RoundedRect(new Rectangle(2, 3, w - 2, h - 2), 12);
            g.FillPath(new SolidBrush(Color.FromArgb(10, 0, 0, 0)), sh);

            // White card body
            using var path = RoundedRect(new Rectangle(0, 0, w, h), 12);
            g.FillPath(Brushes.White, path);

            if (item.Open)
            {
                // Gold left accent bar
                using var acc = RoundedRect(new Rectangle(0, 0, 5, h), 2);
                g.FillPath(new SolidBrush(C_GOLD), acc);
                g.DrawPath(new Pen(Color.FromArgb(55, 212, 160, 23), 1f), path);
            }
            else
            {
                g.DrawPath(new Pen(C_BRD, 0.5f), path);
            }

            // ── Question text — drawn with explicit dark colour ──
            using var qF = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var qBr = new SolidBrush(item.Open ? C_DARK : Color.FromArgb(26, 26, 26));
            var qRect = new RectangleF(22, 0, w - 56, CLOSED_H);
            var qFmt = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };
            g.DrawString(item.Question, qF, qBr, qRect, qFmt);

            // ── Arrow ──
            int ax = w - 28, ay = CLOSED_H / 2;
            if (item.Open)
            {
                using var cir = RoundedRect(new Rectangle(ax - 11, ay - 11, 22, 22), 11);
                g.FillPath(new SolidBrush(Color.FromArgb(28, 212, 160, 23)), cir);
            }
            string arr = item.Open ? "▲" : "▼";
            using var aF = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var aGo = new SolidBrush(C_GOLD);
            var aS = g.MeasureString(arr, aF);
            g.DrawString(arr, aF, aGo, ax - aS.Width / 2f, ay - aS.Height / 2f);

            // ── Answer (only when open) ──
            if (item.Open)
            {
                g.DrawLine(new Pen(Color.FromArgb(232, 230, 226), 0.8f),
                    22, CLOSED_H + 1, w - 18, CLOSED_H + 1);
                using var anF = new Font("Segoe UI", 9f);
                using var anBr = new SolidBrush(Color.FromArgb(72, 72, 72));
                g.DrawString(item.Answer, anF, anBr,
                    new RectangleF(22, CLOSED_H + 14, w - 36, OPEN_H - CLOSED_H - 22));
            }
        }

        // ── Contact card ─────────────────────────────────────────────
        private void DrawContactCard(Graphics g, Panel card)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int w = card.Width - 1, h = card.Height - 1;
            if (w < 10 || h < 10) return;

            using var path = RoundedRect(new Rectangle(0, 0, w, h), 16);
            using var lg = new LinearGradientBrush(new Point(0, 0), new Point(0, h), C_DARK, C_DARK2);
            g.FillPath(lg, path);
            g.DrawPath(new Pen(Color.FromArgb(45, 212, 160, 23), 1f), path);

            using var gp = new GraphicsPath();
            gp.AddEllipse(w / 2 - 90, -40, 180, 160);
            using var pgb = new PathGradientBrush(gp);
            pgb.CenterColor = Color.FromArgb(22, 212, 160, 23);
            pgb.SurroundColors = new[] { Color.Transparent };
            g.FillPath(pgb, gp);

            int y = 26;
            g.DrawString("STILL HAVE QUESTIONS?",
                new Font("Segoe UI", 7f), new SolidBrush(C_GOLD), 24, y);
            y += 22;
            g.DrawString("We're Here to Help",
                new Font("Georgia", 14f, FontStyle.Bold), new SolidBrush(C_CREAM), 24, y);
            y += 30;
            g.DrawString("Our guest services team responds within\n2 hours on weekdays, 4 hours on weekends.",
                new Font("Segoe UI", 8f),
                new SolidBrush(Color.FromArgb(105, 248, 244, 239)), 24, y);
            y += 48;
            g.DrawLine(new Pen(Color.FromArgb(25, 212, 160, 23), 0.8f), 24, y, w - 24, y);
            y += 14;

            var rows = new[] {
                ("✉️", "hello@wildnest.ph",  true),
                ("📞", "+63 (32) 555-WILD",   false),
                ("🕐", "Mon–Sat, 8AM–6PM",    false),
            };
            using var rowF = new Font("Segoe UI", 8.5f);
            using var eF = new Font("Segoe UI Emoji", 10f);
            using var dimB = new SolidBrush(Color.FromArgb(145, 248, 244, 239));
            using var goldB = new SolidBrush(C_GOLD);
            foreach (var (ico, txt, ig) in rows)
            {
                g.DrawString(ico, eF, dimB, 24, y);
                g.DrawString(txt, rowF, ig ? goldB : dimB, 48, y + 1);
                y += 28;
            }
        }

        // ── Quick hours card ─────────────────────────────────────────
        private void DrawQuickHours(Graphics g, Panel card)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            int w = card.Width - 2, h = card.Height - 2;
            if (w < 10 || h < 10) return;

            using var sh = RoundedRect(new Rectangle(2, 3, w - 2, h - 2), 13);
            g.FillPath(new SolidBrush(Color.FromArgb(12, 0, 0, 0)), sh);
            using var path = RoundedRect(new Rectangle(0, 0, w, h), 13);
            g.FillPath(Brushes.White, path);
            g.DrawPath(new Pen(C_BRD, 0.5f), path);

            int y = 18;
            g.DrawString("🕗", new Font("Segoe UI Emoji", 10f),
                new SolidBrush(Color.FromArgb(26, 26, 26)), 18, y);
            g.DrawString("Quick Hours Reference",
                new Font("Segoe UI", 9f, FontStyle.Bold),
                new SolidBrush(Color.FromArgb(26, 26, 26)), 44, y + 1);
            y += 34;
            g.DrawLine(new Pen(Color.FromArgb(235, 232, 228), 1f), 18, y, w - 18, y);
            y += 8;

            var rows = new[] {
                ("Mon – Thu",    "8AM – 6PM",     false),
                ("Friday",       "8AM – 8PM",     false),
                ("Saturday",     "7AM – 9PM",     false),
                ("Sunday",       "Closed",         true),
                ("Night Safari", "7:30 – 9:30PM", false),
            };
            using var dF = new Font("Segoe UI", 8.5f);
            using var tF = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var nB = new SolidBrush(Color.FromArgb(85, 85, 85));
            using var dkB = new SolidBrush(Color.FromArgb(26, 26, 26));
            using var rB = new SolidBrush(C_RED);
            using var lP = new Pen(Color.FromArgb(242, 240, 237), 0.5f);
            foreach (var (day, time, isClosed) in rows)
            {
                g.DrawString(day, dF, nB, 18, y);
                var tSz = g.MeasureString(time, tF);
                g.DrawString(time, tF, isClosed ? rB : dkB, w - (int)tSz.Width - 16, y);
                y += 28;
                g.DrawLine(lP, 18, y - 2, w - 18, y - 2);
            }
        }

        // ── Footer ───────────────────────────────────────────────────
        private void PaintFooter(object s, PaintEventArgs e)
        {
            var fp = (Panel)s;
            var g = e.Graphics;
            using var lg = new LinearGradientBrush(new Point(0, 0), new Point(0, fp.Height),
                C_DARK, C_DARK2);
            g.FillRectangle(lg, fp.ClientRectangle);
            g.FillRectangle(new SolidBrush(Color.FromArgb(50, 212, 160, 23)), 0, 0, fp.Width, 1);
            using var bF = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString("WILDNEST", bF,
                new SolidBrush(Color.FromArgb(210, 248, 244, 239)),
                PAD_X, (fp.Height - 14) / 2f - 1);
            string copy = "© 2026 WildNest Resort & Wildlife Experience. Carmen, Cebu, Philippines.";
            using var cF = new Font("Segoe UI", 8f);
            using var cB = new SolidBrush(Color.FromArgb(65, 248, 244, 239));
            var cSz = g.MeasureString(copy, cF);
            g.DrawString(copy, cF, cB,
                fp.Width - cSz.Width - PAD_X, (fp.Height - cSz.Height) / 2f);
        }

        // ── Helper ───────────────────────────────────────────────────
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
