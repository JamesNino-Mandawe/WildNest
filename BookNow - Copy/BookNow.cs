using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Project
{
    public partial class BookNow : UserControl
    {
        // ── Sub-pages ─────────────────────────────────────────────
        private Project.Booking.CabinStay _cabinStay;
        private Project.Booking.DayVisit _dayVisit;
        private Project.Booking.ExperienceVisit _experienceVisit;
        private Project.Booking.FullStayExperience _fullStayExperience;
        private Button _activeTab;

        // ── Palette ───────────────────────────────────────────────
        private static readonly Color Gold = Color.FromArgb(212, 160, 23);
        private static readonly Color Dark = Color.FromArgb(7, 26, 14);
        private static readonly Color Dark2 = Color.FromArgb(13, 36, 22);
        private static readonly Color Cream = Color.FromArgb(248, 244, 239);
        private static readonly Color Cream2 = Color.FromArgb(240, 235, 227);
        private static readonly Color TextMuted = Color.FromArgb(107, 101, 96);
        private static readonly Color TextDim = Color.FromArgb(155, 148, 144);
        private static readonly Color BorderClr = Color.FromArgb(220, 216, 210);
        private static readonly Color Success = Color.FromArgb(39, 174, 96);

        // Summary colours (on dark bg)
        private static readonly Color SumBodyBg = Color.FromArgb(245, 241, 236);
        private static readonly Color SumFootBg = Color.FromArgb(234, 230, 224);
        private static readonly Color SumLabel = Color.FromArgb(120, 115, 110);

        // ── Summary state ─────────────────────────────────────────
        private bool _summaryHasContent = false;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════════
        public BookNow()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = Dark;

            SetupHero();
            SetupStatusBar();
            SetupContent();

            ApplyLayout();
            Resize += (s, e) => ApplyLayout();
        }

        // ══════════════════════════════════════════════════════════
        //  TOP-LEVEL LAYOUT
        // ══════════════════════════════════════════════════════════
        private void ApplyLayout()
        {
            int w = ClientSize.Width > 0 ? ClientSize.Width : 1280;
            int h = ClientSize.Height > 0 ? ClientSize.Height : 800;

            pnlHero.Bounds = new Rectangle(0, 0, w, 180);
            pnlStatusBar.Bounds = new Rectangle(0, 180, w, 52);
            pnlContent.Bounds = new Rectangle(0, 232, w, h - 232);

            foreach (Control c in pnlHero.Controls)
                if (c is Label l) { l.Width = w; }

            pnlHero.Invalidate();
            pnlStatusBar.Invalidate();
            LayoutStatusButtons();
            ApplyContentLayout();
        }

        private void LayoutStatusButtons()
        {
            var tabs = new Button[] { btnCabinStay, btnDayVisit, btnFullStayExperience, btnExperienceVisit };
            int bw = pnlStatusBar.Width / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
                tabs[i].Bounds = new Rectangle(i * bw, 0, bw, pnlStatusBar.Height);
        }

        // ══════════════════════════════════════════════════════════
        //  HERO
        // ══════════════════════════════════════════════════════════
        private void SetupHero()
        {
            pnlHero.BackColor = Dark;
            pnlHero.Paint += Hero_Paint;

            lblLocation.Font = new Font("Bahnschrift", 8.5f, FontStyle.Bold);
            lblLocation.ForeColor = Gold;
            lblLocation.BackColor = Color.Transparent;
            lblLocation.Text = "✦   CARMEN, CEBU  —  PHILIPPINES  ✦";
            lblLocation.AutoSize = false;
            lblLocation.Size = new Size(pnlHero.Width, 22);
            lblLocation.Location = new Point(0, 38);
            lblLocation.TextAlign = ContentAlignment.MiddleCenter;

            lblHeroTitle.Font = new Font("Georgia", 26f, FontStyle.Bold);
            lblHeroTitle.ForeColor = Color.White;
            lblHeroTitle.BackColor = Color.Transparent;
            lblHeroTitle.Text = "Reserve Your WildNest Stay";
            lblHeroTitle.AutoSize = false;
            lblHeroTitle.Size = new Size(pnlHero.Width, 50);
            lblHeroTitle.Location = new Point(0, 66);
            lblHeroTitle.TextAlign = ContentAlignment.MiddleCenter;

            lblHeroSub.Font = new Font("Segoe UI", 9f);
            lblHeroSub.ForeColor = Color.FromArgb(128, 248, 244, 239);
            lblHeroSub.BackColor = Color.Transparent;
            lblHeroSub.Text = "Day visits  ·  Cabin stays  ·  Signature experiences  ·  All in one place";
            lblHeroSub.AutoSize = false;
            lblHeroSub.Size = new Size(pnlHero.Width, 22);
            lblHeroSub.Location = new Point(0, 126);
            lblHeroSub.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void Hero_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var pnl = sender as Panel;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var brush = new LinearGradientBrush(
                new Point(0, 0), new Point(0, pnl.Height),
                Color.FromArgb(255, 7, 26, 14), Color.FromArgb(255, 13, 40, 24));
            g.FillRectangle(brush, pnl.ClientRectangle);

            int cx = pnl.Width / 2;
            using var path = new GraphicsPath();
            path.AddEllipse(cx - 450, pnl.Height - 80, 900, 160);
            using var pgb = new PathGradientBrush(path);
            pgb.CenterColor = Color.FromArgb(22, 212, 160, 23);
            pgb.SurroundColors = new[] { Color.Transparent };
            g.FillPath(pgb, path);

            int y = 162;
            DrawFadeLine(g, cx - 200, cx - 12, y, false);
            DrawFadeLine(g, cx + 12, cx + 200, y, true);
            g.FillPolygon(new SolidBrush(Gold), new[]
            {
                new Point(cx,     y - 5), new Point(cx + 5, y),
                new Point(cx,     y + 5), new Point(cx - 5, y)
            });
        }

        private void DrawFadeLine(Graphics g, int x1, int x2, int y, bool fromRight)
        {
            using var brush = new LinearGradientBrush(
                new Point(x1, y), new Point(x2, y),
                fromRight ? Color.FromArgb(180, 212, 160, 23) : Color.Transparent,
                fromRight ? Color.Transparent : Color.FromArgb(180, 212, 160, 23));
            using var pen = new Pen(brush, 1.5f);
            g.DrawLine(pen, x1, y, x2, y);
        }

        // ══════════════════════════════════════════════════════════
        //  STATUS BAR / TABS
        // ══════════════════════════════════════════════════════════
        private void SetupStatusBar()
        {
            pnlStatusBar.BackColor = Dark2;
            pnlStatusBar.Paint += StatusBar_Paint;

            StyleNavButton(btnCabinStay, "🏕", "Cabin Stay", Gold, active: true);
            StyleNavButton(btnDayVisit, "🌿", "Day Visit", Color.FromArgb(80, 200, 120), active: false);
            StyleNavButton(btnFullStayExperience, "⭐", "Full Stay + Exp.", Color.FromArgb(255, 215, 0), active: false);
            StyleNavButton(btnExperienceVisit, "🦁", "Experience Only", Color.FromArgb(230, 160, 60), active: false);

            _activeTab = btnCabinStay;

            btnCabinStay.Click += (s, e) => SwitchTab(btnCabinStay, _cabinStay);
            btnDayVisit.Click += (s, e) => SwitchTab(btnDayVisit, _dayVisit);
            btnFullStayExperience.Click += (s, e) => SwitchTab(btnFullStayExperience, _fullStayExperience);
            btnExperienceVisit.Click += (s, e) => SwitchTab(btnExperienceVisit, _experienceVisit);
        }

        private void StatusBar_Paint(object sender, PaintEventArgs e)
        {
            int w = pnlStatusBar.Width, h = pnlStatusBar.Height;
            using var bot = new Pen(Color.FromArgb(60, 212, 160, 23), 1f);
            e.Graphics.DrawLine(bot, 0, h - 1, w, h - 1);
        }

        private void StyleNavButton(Button btn, string icon, string label, Color iconColor, bool active)
        {
            btn.Text = "";
            btn.Tag = new object[] { label, icon, iconColor, active ? "active" : "inactive" };
            btn.Font = new Font("Segoe UI", 9.5f);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(28, 212, 160, 23);
            btn.Cursor = Cursors.Hand;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = active ? Gold : Color.FromArgb(140, 248, 244, 239);
            btn.UseVisualStyleBackColor = false;
            btn.Paint += NavBtn_Paint;
        }

        private void SetTabActive(Button btn, bool active)
        {
            if (btn.Tag is object[] p) p[3] = active ? "active" : "inactive";
            btn.ForeColor = active ? Gold : Color.FromArgb(140, 248, 244, 239);
            btn.Invalidate();
        }

        private void NavBtn_Paint(object sender, PaintEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is not object[] parts) return;

            string label = parts[0] as string ?? "";
            string icon = parts[1] as string ?? "";
            Color icClr = parts[2] is Color c ? c : Color.White;
            bool active = parts[3]?.ToString() == "active";

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (active)
            {
                using var bg = new LinearGradientBrush(new Point(0, 0), new Point(0, btn.Height),
                    Color.FromArgb(28, 212, 160, 23), Color.Transparent);
                g.FillRectangle(bg, 0, 0, btn.Width, btn.Height);
            }

            using var iconFont = new Font("Segoe UI Emoji", 11f);
            using var textFont = new Font("Segoe UI", 9.5f, active ? FontStyle.Bold : FontStyle.Regular);

            var iSize = g.MeasureString(icon, iconFont);
            var tSize = g.MeasureString("  " + label, textFont);
            float totW = iSize.Width + tSize.Width;
            float sx = (btn.Width - totW) / 2f;
            float iy = (btn.Height - iSize.Height) / 2f;
            float ty = (btn.Height - tSize.Height) / 2f;

            g.DrawString(icon, iconFont, new SolidBrush(icClr), sx, iy);
            Color textColor = active ? Gold : Color.FromArgb(180, 248, 244, 239);
            g.DrawString("  " + label, textFont, new SolidBrush(textColor), sx + iSize.Width, ty);

            if (active)
            {
                using var lp = new Pen(Gold, 2.5f);
                g.DrawLine(lp, 16, btn.Height - 1, btn.Width - 16, btn.Height - 1);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONTENT AREA
        // ══════════════════════════════════════════════════════════
        private void SetupContent()
        {
            pnlContent.BackColor = Color.FromArgb(240, 237, 232);

            _cabinStay = new Project.Booking.CabinStay();
            _dayVisit = new Project.Booking.DayVisit();
            _experienceVisit = new Project.Booking.ExperienceVisit();
            _fullStayExperience = new Project.Booking.FullStayExperience();

            foreach (UserControl uc in new UserControl[]
                { _cabinStay, _dayVisit, _experienceVisit, _fullStayExperience })
            {
                uc.Dock = DockStyle.Fill;
                uc.Visible = false;
                pnlLeft.Controls.Add(uc);
            }

            _cabinStay.OnSummaryChanged += UpdateSummary;
            _dayVisit.OnSummaryChanged += UpdateSummary;
            _experienceVisit.OnSummaryChanged += UpdateSummary;
            _fullStayExperience.OnSummaryChanged += UpdateSummary;

            pnlLeft.Paint += PnlLeft_Paint;
            pnlLeft.Resize += (s, e) =>
            {
                if (pnlLeft.Width > 0 && pnlLeft.Height > 0)
                {
                    using var clipPath = RoundedRect(new Rectangle(0, 0, pnlLeft.Width, pnlLeft.Height), 14);
                    pnlLeft.Region = new Region(clipPath);
                }
            };

            BuildSummaryCard();
            // Start with CabinStay visible, summary blank
            SwitchTab(btnCabinStay, _cabinStay);
        }

        private void PnlLeft_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, pnlLeft.Width - 1, pnlLeft.Height - 1), 14);
            using var b = new SolidBrush(Cream);
            g.FillPath(b, path);
            using var pen = new Pen(BorderClr, 1f);
            g.DrawPath(pen, path);
        }

        private void SwitchTab(Button clicked, UserControl target)
        {
            SetTabActive(_activeTab, false);
            SetTabActive(clicked, true);
            _activeTab = clicked;

            foreach (Control c in pnlLeft.Controls) c.Visible = false;
            if (target != null) target.Visible = true;

            // When switching tabs, keep summary blank until user makes a selection
            ShowSummaryEmpty();
        }

        // ══════════════════════════════════════════════════════════
        //  SUMMARY CARD
        // ══════════════════════════════════════════════════════════
        private Panel _pnlSumHeader;
        private Panel _pnlSumBody;
        private Panel _pnlSumFooter;

        private void BuildSummaryCard()
        {
            pnlSummary.BackColor = Dark;
            pnlSummary.Paint += PnlSummary_Paint;
            pnlSummary.Resize += (s, e) =>
            {
                if (pnlSummary.Width > 0 && pnlSummary.Height > 0)
                {
                    using var clipPath = RoundedRect(new Rectangle(0, 0, pnlSummary.Width, pnlSummary.Height), 14);
                    pnlSummary.Region = new Region(clipPath);
                }
                SummaryCard_Resize(s, e);
            };

            // ── Header ──────────────────────────────────────────────
            _pnlSumHeader = new Panel
            {
                Location = new Point(0, 0),
                Height = 80,
                BackColor = Dark,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _pnlSumHeader.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var fill = new SolidBrush(Dark);
                g.FillRectangle(fill, 0, 0, _pnlSumHeader.Width, _pnlSumHeader.Height);

                // Gold accent line
                using var goldBrush = new LinearGradientBrush(
                    new Point(0, _pnlSumHeader.Height - 1),
                    new Point(_pnlSumHeader.Width, _pnlSumHeader.Height - 1),
                    Color.FromArgb(0, 212, 160, 23), Color.FromArgb(60, 212, 160, 23));
                using var pen = new Pen(goldBrush, 1f);
                g.DrawLine(pen, 20, _pnlSumHeader.Height - 1, _pnlSumHeader.Width - 20, _pnlSumHeader.Height - 1);
            };

            // Icon + Title row
            var iconLbl = new Label
            {
                Text = "📋",
                Font = new Font("Segoe UI Emoji", 14f),
                AutoSize = true,
                Location = new Point(20, 16),
                BackColor = Color.Transparent
            };
            var titleLbl = new Label
            {
                Text = "Booking Summary",
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                ForeColor = Cream,
                AutoSize = true,
                Location = new Point(56, 14),
                BackColor = Color.Transparent
            };
            var subLbl = new Label
            {
                Text = "Live updates as you select",
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = Success,
                AutoSize = true,
                Location = new Point(56, 42),
                BackColor = Color.Transparent
            };
            _pnlSumHeader.Controls.Add(iconLbl);
            _pnlSumHeader.Controls.Add(titleLbl);
            _pnlSumHeader.Controls.Add(subLbl);
            pnlSummary.Controls.Add(_pnlSumHeader);

            // ── Body ────────────────────────────────────────────────
            _pnlSumBody = new Panel
            {
                BackColor = SumBodyBg,
                AutoScroll = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };
            pnlSummary.Controls.Add(_pnlSumBody);

            // ── Footer (trust badges) ────────────────────────────────
            _pnlSumFooter = new Panel
            {
                BackColor = SumFootBg,
                Height = 116,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _pnlSumFooter.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(BorderClr, 1f);
                g.DrawLine(pen, 16, 0, _pnlSumFooter.Width - 16, 0);
            };

            var badges = new (string icon, string text)[]
            {
                ("✅", "Free cancellation · 48h prior"),
                ("🔒", "Secure payment · SSL encrypted"),
                ("🌿", "₱200 supports PH conservation"),
                ("⚡", "Instant confirmation via email"),
            };

            int by = 14;
            foreach (var (icon, text) in badges)
            {
                _pnlSumFooter.Controls.Add(new Label
                {
                    Text = icon,
                    Font = new Font("Segoe UI Emoji", 10f),
                    AutoSize = true,
                    Location = new Point(16, by + 1),
                    BackColor = Color.Transparent
                });
                _pnlSumFooter.Controls.Add(new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 8.3f),
                    ForeColor = TextMuted,
                    AutoSize = true,
                    Location = new Point(44, by + 3),
                    BackColor = Color.Transparent
                });
                by += 24;
            }
            pnlSummary.Controls.Add(_pnlSumFooter);

            SummaryCard_Resize(null, null);
            ShowSummaryEmpty();  // Start blank
        }

        private void PnlSummary_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new Rectangle(0, 0, pnlSummary.Width - 1, pnlSummary.Height - 1), 14);
            using var b = new SolidBrush(Dark);
            g.FillPath(b, path);
            using var pen = new Pen(Color.FromArgb(60, 212, 160, 23), 1.5f);
            g.DrawPath(pen, path);
        }

        private void SummaryCard_Resize(object sender, EventArgs e)
        {
            int w = pnlSummary.ClientSize.Width;
            int h = pnlSummary.ClientSize.Height;

            _pnlSumHeader?.SetBounds(0, 0, w, 80);
            _pnlSumFooter?.SetBounds(0, h - (_pnlSumFooter?.Height ?? 132), w, _pnlSumFooter?.Height ?? 132);
            if (_pnlSumBody != null && _pnlSumHeader != null && _pnlSumFooter != null)
                _pnlSumBody.Bounds = new Rectangle(
                    0, _pnlSumHeader.Height, w,
                    Math.Max(10, h - _pnlSumHeader.Height - _pnlSumFooter.Height));
        }

        // ── Empty state — BLANK on load ─────────────────────────────
        private void ShowSummaryEmpty()
        {
            _summaryHasContent = false;
            _pnlSumBody.Controls.Clear();

            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var iconLbl = new Label
            {
                Text = "🌿",
                Font = new Font("Segoe UI Emoji", 32f),
                AutoSize = true,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var line1 = new Label
            {
                Text = "Your booking summary",
                Font = new Font("Georgia", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 140, 130),
                AutoSize = false,
                Size = new Size(240, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            var line2 = new Label
            {
                Text = "will appear here once you",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(160, 150, 140),
                AutoSize = false,
                Size = new Size(240, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            var line3 = new Label
            {
                Text = "make your first selection.",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(160, 150, 140),
                AutoSize = false,
                Size = new Size(240, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Divider dots
            var dots = new Label
            {
                Text = "· · ·",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(120, 110, 100),
                AutoSize = false,
                Size = new Size(240, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            pnl.Controls.Add(iconLbl);
            pnl.Controls.Add(line1);
            pnl.Controls.Add(line2);
            pnl.Controls.Add(line3);
            pnl.Controls.Add(dots);

            pnl.Resize += (s, ev) =>
            {
                int cx = pnl.Width / 2;
                int cy = pnl.Height / 2;
                iconLbl.Location = new Point(cx - iconLbl.PreferredWidth / 2, cy - 80);
                dots.Location = new Point(cx - 120, cy - 24);
                line1.Location = new Point(cx - 120, cy + 6);
                line2.Location = new Point(cx - 120, cy + 26);
                line3.Location = new Point(cx - 120, cy + 46);
            };

            _pnlSumBody.Controls.Add(pnl);
        }

        // ── Populated summary ────────────────────────────────────────
        private void UpdateSummary(BookingSummary s)
        {
            if (_pnlSumBody == null) return;
            if (string.IsNullOrEmpty(s.PrimaryTitle)) { ShowSummaryEmpty(); return; }

            _summaryHasContent = true;
            _pnlSumBody.Controls.Clear();

            int iw = Math.Max(_pnlSumBody.ClientSize.Width - 24, 180);
            int y = 18;
            var inner = new Panel
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 18, 20, 18)
            };

            // ── Booking type label ──────────────────────────────────
            y += SumSectionLabel(inner,
                string.IsNullOrEmpty(s.BookingType) ? "BOOKING" : s.BookingType.ToUpperInvariant(), y, iw);

            // ── Primary card ────────────────────────────────────────
            int cardH = 74 + (s.PrimaryAmount > 0 ? 32 : 0);
            var cabinBox = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(iw, cardH),
                BackColor = Color.Transparent
            };
            cabinBox.Paint += (ss, ee) =>
            {
                ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, cabinBox.Width - 1, cabinBox.Height - 1), 9);
                ee.Graphics.FillPath(new SolidBrush(Color.FromArgb(22, 180, 130, 20)), path);
                ee.Graphics.DrawPath(new Pen(Color.FromArgb(80, 180, 130, 20), 1.5f), path);
            };
            var titleLbl = new Label
            {
                Text = s.PrimaryTitle,
                Font = new Font("Georgia", 12f, FontStyle.Bold),
                ForeColor = Dark,
                AutoSize = false,
                Size = new Size(iw - 24, 22),
                Location = new Point(12, 10),
                BackColor = Color.Transparent
            };
            cabinBox.Controls.Add(titleLbl);
            var subLbl = new Label
            {
                Text = !string.IsNullOrEmpty(s.PrimarySubtitle)
                                ? s.PrimarySubtitle
                                : s.Nights > 0
                                    ? $"{s.Nights} night{(s.Nights > 1 ? "s" : "")} stay"
                                    : "Make your selections",
                Font = new Font("Segoe UI", 8.3f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(iw - 24, 18),
                Location = new Point(12, 34),
                BackColor = Color.Transparent
            };
            cabinBox.Controls.Add(subLbl);
            if (s.PrimaryAmount > 0)
            {
                var priceMeta = new Label
                {
                    Text = !string.IsNullOrEmpty(s.PrimaryAmountLabel) ? s.PrimaryAmountLabel : $"₱{s.PrimaryAmount:N0}",
                    Font = new Font("Segoe UI", 7.8f),
                    ForeColor = TextMuted,
                    AutoSize = false,
                    Location = new Point(12, 58),
                    Size = new Size(iw - 24, 20),
                    BackColor = Color.Transparent
                };
                cabinBox.Controls.Add(priceMeta);
                var priceVal = new Label
                {
                    Text = $"₱{s.PrimaryAmount:N0}",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = Gold,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                cabinBox.Controls.Add(priceVal);
                void AlignPrimaryCard()
                {
                    int valueX = Math.Max(12, cabinBox.Width - priceVal.PreferredWidth - 12);
                    priceVal.Location = new Point(valueX, 56);
                    priceMeta.Size = new Size(Math.Max(100, valueX - 20), 20);
                    titleLbl.Size = new Size(cabinBox.Width - 24, 22);
                    subLbl.Size = new Size(cabinBox.Width - 24, 18);
                }

                cabinBox.Resize += (ss, ee) => AlignPrimaryCard();
                AlignPrimaryCard();
            }
            inner.Controls.Add(cabinBox);
            y += cardH + 14;

            // ── Summary lines ───────────────────────────────────────
            foreach (var line in s.Lines)
                y += SumRow(inner, line.Label, line.Value, y, iw);

            // ── Add-ons ─────────────────────────────────────────────
            if (s.Addons?.Count > 0)
            {
                y += 10;
                y += SumSectionLabel(inner, "ADD-ONS", y, iw);
                foreach (var a in s.Addons)
                    y += SumRow(inner, a.Name, $"₱{a.Price:N0}", y, iw);
            }

            // ── Divider ─────────────────────────────────────────────
            inner.Controls.Add(new Panel
            {
                Location = new Point(0, y + 8),
                Size = new Size(iw, 1),
                BackColor = BorderClr
            });
            y += 24;

            // ── Estimated Total ─────────────────────────────────────
            var lTotLbl = new Label
            {
                Text = "Estimated Total",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Dark,
                AutoSize = true,
                Location = new Point(0, y + 14),
                BackColor = Color.Transparent
            };
            var lTotVal = new Label
            {
                Text = $"₱{s.GrandTotal:N0}",
                Font = new Font("Georgia", 18f, FontStyle.Bold),
                ForeColor = Gold,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            inner.Controls.Add(lTotLbl);
            inner.Controls.Add(lTotVal);
            void AlignTot() => lTotVal.Location = new Point(Math.Max(0, iw - lTotVal.PreferredWidth), y + 4);
            inner.Resize += (ss, ee) => AlignTot();
            AlignTot();
            y += Math.Max(lTotLbl.PreferredHeight, lTotVal.PreferredHeight) + 18;

            // Note
            inner.Controls.Add(new Label
            {
                Text = "Final price confirmed on booking page",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(140, 130, 120),
                AutoSize = false,
                Size = new Size(iw, 18),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            y += 28;

            inner.Size = new Size(_pnlSumBody.ClientSize.Width, y + 20);
            _pnlSumBody.Controls.Add(inner);
            _pnlSumBody.Resize += (ss, ee) =>
            {
                if (inner.Parent != null)
                    inner.Width = _pnlSumBody.ClientSize.Width;
            };
        }

        // ── Summary helpers ──────────────────────────────────────────
        private static int SumSectionLabel(Panel p, string text, int y, int iw)
        {
            p.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7.2f, FontStyle.Bold),
                ForeColor = SumLabel,
                AutoSize = true,
                Location = new Point(0, y),
                BackColor = Color.Transparent
            });
            return 22;
        }

        private static int SumRow(Panel p, string label, string value, int y, int iw)
        {
            var lVal = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                ForeColor = Dark,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            int valueX = Math.Max(140, iw - lVal.PreferredWidth);
            var lLbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(Math.Max(120, valueX - 14), 18),
                Location = new Point(0, y + 2),
                BackColor = Color.Transparent
            };
            lVal.Location = new Point(valueX, y + 2);
            p.Controls.Add(lLbl);
            p.Controls.Add(lVal);
            return 26;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        internal static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure(); return path;
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════
        public void OpenDayVisit() => SwitchTab(btnDayVisit, _dayVisit);
        public void OpenCabinStay() => SwitchTab(btnCabinStay, _cabinStay);
        public void OpenExperienceVisit() => SwitchTab(btnExperienceVisit, _experienceVisit);
        public void OpenFullStayExperience() => SwitchTab(btnFullStayExperience, _fullStayExperience);

        private void lblHeroSub_Click(object sender, EventArgs e) { }
    }

    // ══════════════════════════════════════════════════════════
    //  DATA MODELS
    // ══════════════════════════════════════════════════════════
    public class BookingSummary
    {
        public string BookingType { get; set; } = "";
        public string CabinName { get; set; } = "";
        public string PrimarySubtitle { get; set; } = "";
        public string PrimaryAmountLabel { get; set; } = "";
        public int PrimaryAmount { get; set; }
        public int CabinPricePerNight { get; set; }
        public int Nights { get; set; }
        public string PrimaryTitle => !string.IsNullOrWhiteSpace(CabinName) ? CabinName : BookingType;
        public System.Collections.Generic.List<AddonItem> Addons { get; set; } = new();
        public System.Collections.Generic.List<SummaryLine> Lines { get; set; } = new();

        private int _grandTotal;
        public int GrandTotal
        {
            get => _grandTotal > 0 ? _grandTotal : CabinTotal + AddonsTotal + 400;
            set => _grandTotal = value;
        }
        public int CabinTotal => CabinPricePerNight * Nights;
        public int AddonsTotal => Addons?.Sum(a => a.Price) ?? 0;
    }

    public class AddonItem
    {
        public string Name { get; set; } = "";
        public int Price { get; set; }
    }

    public class SummaryLine
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
