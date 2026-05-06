using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Project
{
    public partial class HomePage : Form
    {
        private sealed class TransitionOverlay : Control
        {
            public float Progress { get; set; }

            public TransitionOverlay()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.SupportsTransparentBackColor |
                         ControlStyles.UserPaint |
                         ControlStyles.ResizeRedraw, true);
                BackColor = Color.Transparent;
                Enabled = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (Width <= 0 || Height <= 0) return;

                // Simple, fast fade: dark overlay that fades out
                // Progress 0→0.5 = fade in, 0.5→1 = fade out
                float alpha = Progress < 0.5f
                    ? Progress * 2f           // 0 → 1
                    : (1f - Progress) * 2f;   // 1 → 0

                int a = (int)(alpha * 140);   // max alpha = 140 (subtle, not black)
                if (a > 0)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(a, 7, 22, 14)))
                        e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // ── COLOR PALETTE ──
        private static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        private static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        private static readonly Color C_HERO = Color.FromArgb(10, 36, 18);
        private static readonly Color C_HERO2 = Color.FromArgb(14, 46, 24);
        private static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        private static readonly Color C_GOLD_LIGHT = Color.FromArgb(232, 181, 40);
        private static readonly Color C_TEXT = Color.FromArgb(248, 244, 239);
        private static readonly Color C_MUTED = Color.FromArgb(160, 180, 160);
        private static readonly Color C_GREEN = Color.FromArgb(27, 67, 50);
        private static readonly Color C_HERO_GLASS = Color.FromArgb(24, 255, 255, 255);

        private Panel pnlHeroLocationTag;
        private Panel pnlHeroTitleArt;
        private readonly System.Windows.Forms.Timer _navTransitionTimer = new System.Windows.Forms.Timer();
        private TransitionOverlay _navTransitionOverlay;
        private Control? _transitionIncoming;
        private Control? _transitionOutgoing;
        private Action? _transitionFinalize;
        private Control? _activeScrollableContent;
        private EventHandler? _activeScrollableContentSizeChanged;
        private float _transitionProgress;
        private bool _isTransitioning;
        private const int TransitionDurationMs = 180;

        public HomePage()
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
            this.FormClosed += (s, e) => Application.Exit();
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = C_BG;
            this.Text = "WildNest — Zoo Resort & Wildlife Experience";
            this.MinimumSize = new Size(1280, 800);

            // ── pnlMain scrolling ──
            pnlMain.AutoScroll = true;
            pnlMain.AutoScrollMinSize = new Size(0, 5000);
            pnlMain.VerticalScroll.Visible = true;
            pnlMain.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(pnlMain, true);

            _navTransitionTimer.Interval = 8;
            _navTransitionTimer.Tick += NavTransitionTimer_Tick;

            _navTransitionOverlay = new TransitionOverlay
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlMain.Controls.Add(_navTransitionOverlay);
            _navTransitionOverlay.BringToFront();
            pnlMain.Resize += (s, e) => SyncScrollableContentBounds();

            // ── NAV ──
            pnlNav.BackColor = C_DARK;
            pnlNav.Height = 80;
            pnlNav.Dock = DockStyle.Top;
            pnlNav.Paint += (s, e) =>
            {
                if (pnlNav.Width <= 1 || pnlNav.Height <= 1) return;
                var g = e.Graphics;
                // Subtle top-to-bottom dark gradient for depth
                using (var lgb = new LinearGradientBrush(
                    new Rectangle(0, 0, pnlNav.Width, pnlNav.Height),
                    Color.FromArgb(18, 38, 22), Color.FromArgb(7, 26, 14), 90f))
                    g.FillRectangle(lgb, 0, 0, pnlNav.Width, pnlNav.Height);

                // Gold bottom border with soft glow blur underneath
                using (var glowBrush = new LinearGradientBrush(
                    new Rectangle(0, pnlNav.Height - 10, pnlNav.Width, 10),
                    Color.Transparent, Color.FromArgb(28, 212, 160, 23), 90f))
                    g.FillRectangle(glowBrush, 0, pnlNav.Height - 10, pnlNav.Width, 10);

                using (var pen = new Pen(Color.FromArgb(200, 212, 160, 23), 1.5f))
                    g.DrawLine(pen, 0, pnlNav.Height - 1, pnlNav.Width, pnlNav.Height - 1);
            };

            // ── LOGO ──
            picLogo.Size = new Size(130, 130);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.BackColor = Color.Transparent;
            picLogo.Image = LoadSafeLogoImage();

            // ── BRAND ──
            lblBrand.ForeColor = C_TEXT;
            lblBrand.Font = new Font("Georgia", 22, FontStyle.Bold);
            lblBrand.BackColor = Color.Transparent;
            lblBrand.Text = "WILDNEST";
            lblBrand.AutoSize = true;

            // ── NAV BUTTONS ──
            Button[] navBtns = { btnHome, btnCabins, btnExperiences, btnAnimals, btnMap, btnVisit, btnAbout };
            foreach (Button b in navBtns)
            {
                string originalText = b.Text;
                b.Text = "";
                b.Tag = originalText;
                b.Size = new Size(112, 50);
                b.TabStop = false;
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.BackColor = Color.Transparent;
                b.Cursor = Cursors.Hand;
                b.Font = new Font("Segoe UI Semibold", 10.5f);

                b.Paint += (s, e) =>
                {
                    if (s is not Button btn) return;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    bool isActive = (btn.Tag?.ToString() == GetActivePageTag());

                    // Active: draw a subtle pill highlight behind text
                    if (isActive)
                    {
                        Rectangle pill = new Rectangle(8, btn.Height / 2 - 14, btn.Width - 16, 28);
                        using (var path = GetRoundedPath(pill, 14))
                        using (var sb = new SolidBrush(Color.FromArgb(22, 212, 160, 23)))
                            e.Graphics.FillPath(sb, path);
                    }

                    Color txtCol = isActive ? C_GOLD : Color.FromArgb(195, 248, 244, 239);
                    TextRenderer.DrawText(e.Graphics, btn.Tag.ToString(), btn.Font,
                        btn.ClientRectangle, txtCol,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    if (isActive)
                    {
                        // Thicker, centered underline dot
                        int cx = btn.Width / 2;
                        using (var pen = new Pen(C_GOLD, 2.5f))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            e.Graphics.DrawLine(pen, cx - 12, btn.Height - 5, cx + 12, btn.Height - 5);
                        }
                    }
                };
            }

            // ── HERO ──
            pnlHero.BackColor = C_HERO;
            pnlHero.Height = 520;
            pnlHero.Dock = DockStyle.None;
            pnlHero.Location = new Point(0, 0);
            pnlHero.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            pnlHero.Paint += (s, e) =>
            {
                if (pnlHero.Width <= 1 || pnlHero.Height <= 1) return;

                HeroSurfacePainter.Paint(e.Graphics, pnlHero.ClientRectangle, HeroSurfaceVariant.Home);

                using var fadeRect = new LinearGradientBrush(
                    new Rectangle(0, pnlHero.Height - 92, pnlHero.Width, 92),
                    Color.Transparent, Color.FromArgb(52, 0, 0, 0), 90f);
                e.Graphics.FillRectangle(fadeRect, 0, pnlHero.Height - 92, pnlHero.Width, 92);
            };

            pnlHeroLocationTag = new Panel
            {
                BackColor = Color.Transparent,
                Size = new Size(380, 30)
            };
            pnlHeroLocationTag.Paint += (s, e) =>
            {
                if (pnlHeroLocationTag.Width <= 1 || pnlHeroLocationTag.Height <= 1) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // Subtle pill background behind location tag
                Rectangle pill = new Rectangle(0, 3, 300, 22);
                using (var path = GetRoundedPath(pill, 11))
                using (var sb = new SolidBrush(Color.FromArgb(18, 212, 160, 23)))
                    g.FillPath(sb, path);

                // Short gold line accent
                using (var pen = new Pen(C_GOLD, 1.5f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, 12, 14, 34, 14);
                }

                TextRenderer.DrawText(g, "CARMEN, CEBU  —  PHILIPPINES",
                    new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    new Rectangle(44, 0, pnlHeroLocationTag.Width - 44, pnlHeroLocationTag.Height),
                    Color.FromArgb(230, 212, 160, 23),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            };

            pnlHeroTitleArt = new Panel
            {
                BackColor = Color.Transparent,
                Size = new Size(980, 166)
            };
            pnlHeroTitleArt.Paint += (s, e) =>
            {
                if (pnlHeroTitleArt.Width <= 1 || pnlHeroTitleArt.Height <= 1) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                using var titleFont = new Font("Georgia", 48f, FontStyle.Bold);
                using var titleBrush = new SolidBrush(C_TEXT);
                using var accentBrush = new LinearGradientBrush(
                    new RectangleF(0, 72, 430, 60),
                    C_GOLD_LIGHT, C_GOLD, 0f);

                g.DrawString("Where the Wild", titleFont, titleBrush, new PointF(0, 0));
                g.DrawString("Meets", titleFont, titleBrush, new PointF(0, 78));

                SizeF meetsSize = g.MeasureString("Meets", titleFont, 1000, StringFormat.GenericTypographic);
                g.DrawString("Comfort", titleFont, accentBrush, new PointF(meetsSize.Width + 34f, 78));
            };

            lblLocation.Visible = false;
            lblHeroTitle.Visible = false;
            pnlHero.Controls.Add(pnlHeroLocationTag);
            pnlHero.Controls.Add(pnlHeroTitleArt);
            if (!pnlHero.Controls.Contains(lblHeroSub))
                pnlHero.Controls.Add(lblHeroSub);

            lblHeroSub.ForeColor = Color.FromArgb(188, 248, 244, 239);
            lblHeroSub.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);
            lblHeroSub.BackColor = Color.Transparent;
            lblHeroSub.Text = "A 170-hectare living sanctuary of wildlife, glamping and unforgettable\nencounters — designed for those who refuse to choose between\nadventure and luxury.";
            lblHeroSub.AutoSize = false;
            lblHeroSub.Size = new Size(680, 86);
            lblHeroSub.Visible = true;

            // ── PILL BUTTONS ──
            StylePillButton(btnBookNow, C_GOLD, Color.FromArgb(26, 46, 10), "Book Now");
            StylePillButton(btnStaffPortal, Color.Transparent, Color.FromArgb(180, 248, 244, 239), "Staff Portal", true);
            StylePillButton(btnMyAccommodations, Color.Transparent, Color.FromArgb(180, 248, 244, 239), "Accommodation", true);

            // ── WILDNEST PANEL ──
            SetupWildNestPanel();

            // ── STATUS BAR ──
            pnlStatusBar.BackColor = C_DARK;
            pnlStatusBar.Dock = DockStyle.None;
            pnlStatusBar.Location = new Point(0, 538);
            pnlStatusBar.Size = new Size(pnlContent.Width, 46);
            pnlStatusBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            SetupStatusBar();

            // ── ANIMAL OF THE DAY ──
            pnlAnimalDay.BackColor = Color.Transparent;
            pnlAnimalDay.Location = new Point(0, 674);
            pnlAnimalDay.Height = 180;
            pnlAnimalDay.Width = pnlMain.Width;
            pnlAnimalDay.BringToFront();
            SetupAnimalDayButtons();

            // ── CABIN SECTION ──
            pnlCabinSection.Location = new Point(0, pnlAnimalDay.Bottom + 30);
            pnlCabinSection.Width = pnlMain.Width;
            pnlCabinSection.Height = 600;
            pnlCabinSection.BackColor = Color.Transparent;
            pnlCabinSection.BringToFront();
            GenerateCabinCards();

            // ── LOAD EVENT ──
            this.Load += (s, e) =>
            {
                RefreshLayout();
                // Ensure Home button active state renders
                foreach (Button b in navBtns) b.Invalidate();
            };

            // Initial experience/highlights/footer (before Load)
            pnlExperience.Location = new Point(0, pnlCabinSection.Bottom + 30);
            pnlExperience.Size = new Size(pnlMain.Width, 600);
            PopulateExperienceSection();

            pnlHighlightsGrid.Location = new Point(0, pnlExperience.Bottom + 30);
            pnlHighlightsGrid.Size = new Size(pnlMain.Width, 180);
            SetupHighlightsGrid();

            pnlMain.AutoScrollMinSize = new Size(0, pnlHighlightsGrid.Bottom + 200);
        }

        // ── ACTIVE PAGE TRACKING ──
        private string _activePage = "Home";
        private string GetActivePageTag() => _activePage;

        private void RefreshLayout()
        {
            int barW = pnlNav.Width;
            int barH = pnlNav.Height;
            int midY = barH / 2;

            Button[] navBtns = { btnHome, btnCabins, btnExperiences, btnAnimals, btnMap, btnVisit, btnAbout };

            picLogo.Location = new Point(30, midY - picLogo.Height / 2);
            lblBrand.Location = new Point(picLogo.Right + 10, midY - lblBrand.Height / 2 + 2);

            int totalWidth = navBtns.Length * 130;
            int startX = barW / 2 - totalWidth / 2;
            for (int i = 0; i < navBtns.Length; i++)
                navBtns[i].Location = new Point(startX + i * 130, midY - navBtns[i].Height / 2);

            btnStaffPortal.Location = new Point(barW - 160, midY - btnStaffPortal.Height / 2);
            btnBookNow.Location = new Point(barW - 310, midY - btnBookNow.Height / 2);
            btnMyAccommodations.Location = new Point(barW - 460, midY - btnMyAccommodations.Height / 2);

            // Hero labels
            int contentW = Math.Min(pnlHero.Width - 260, 1540);
            int heroLeft = Math.Max(118, (pnlHero.Width - contentW) / 2);

            pnlHeroLocationTag.Location = new Point(heroLeft, 48);
            pnlHeroTitleArt.Location = new Point(heroLeft, 76);
            lblHeroSub.Location = new Point(heroLeft, 238);

            LayoutWildNestPanel();

            // Responsive widths
            pnlHero.Width = pnlContent.Width;
            pnlStatusBar.Location = new Point(0, pnlHero.Bottom);
            pnlStatusBar.Width = pnlContent.Width;
            pnlAnimalDay.Location = new Point(0, pnlStatusBar.Bottom + 10);
            pnlAnimalDay.Height = 180;
            pnlAnimalDay.Width = pnlContent.Width;
            pnlCabinSection.Width = pnlContent.Width;

            SetupAnimalDayButtons();
            GenerateCabinCards();

            pnlExperience.Location = new Point(0, pnlCabinSection.Bottom + 30);
            pnlExperience.Size = new Size(pnlContent.Width, 600);
            pnlExperience.Padding = new Padding(0);
            pnlExperience.AutoScroll = false;
            PopulateExperienceSection();

            pnlHighlightsGrid.Location = new Point(0, pnlExperience.Bottom + 30);
            pnlHighlightsGrid.Size = new Size(pnlContent.Width, 180);
            SetupHighlightsGrid();

            pnlFooterSection.Location = new Point(0, pnlHighlightsGrid.Bottom + 20);
            pnlFooterSection.Size = new Size(pnlContent.Width, 350);
            SetupFooterSection();

            pnlMain.AutoScrollMinSize = new Size(0, pnlFooterSection.Bottom + 20);
            pnlContent.Size = new Size(pnlContent.Width, pnlFooterSection.Bottom + 20);
        }

        // ══════════════════════════════════════════════════════
        // HOME BUTTON — THE KEY FIX
        // Restores pnlContent into pnlMain and scrolls to top
        // ══════════════════════════════════════════════════════
        private void btnHome_Click(object sender, EventArgs e)
        {
            if (_isTransitioning || _activePage == "Home")
                return;

            _activePage = "Home";
            InvalidateAllNavButtons();

            pnlMain.AutoScroll = true;
            pnlMain.AutoScrollMinSize = new Size(0, 5000);

            pnlContent.Dock = DockStyle.None;
            pnlContent.Location = new Point(0, 0);
            pnlContent.Width = pnlMain.ClientSize.Width;
            pnlContent.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlContent.Visible = true;

            BeginPanelTransition(
                pnlContent,
                autoScroll: true,
                finalizer: () =>
                {
                    pnlMain.AutoScrollPosition = new Point(0, 0);
                    RefreshLayout();
                });
        }

        private void InvalidateAllNavButtons()
        {
            Button[] navBtns = { btnHome, btnCabins, btnExperiences, btnAnimals, btnMap, btnVisit, btnAbout };
            foreach (var b in navBtns) b.Invalidate();
        }

        // ══════════════════════════════════════════════════════
        // WILDNEST PANEL (hero quick-action cards + rating badge)
        // ══════════════════════════════════════════════════════
        private Panel? pnlRate;

        private void SetupWildNestPanel()
        {
            pnlWildNest.BackColor = Color.Transparent;
            pnlWildNest.Controls.Clear();
            if (pnlRate != null && pnlHero.Controls.Contains(pnlRate))
            {
                pnlHero.Controls.Remove(pnlRate);
                pnlRate.Dispose();
                pnlRate = null;
            }

            string[] icons = { "🏕️", "🦁", "🗺️" };
            string[] labels = { "Browse Cabins", "Book Experience", "Explore the Map" };
            string[] subs = { "10 stays available tonight", "5 wildlife encounters today", "8 wildlife zones live" };
            bool[] isGold = { false, false, true };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Panel card = new Panel
                {
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    Tag = i
                };

                card.Paint += (s, pe) =>
                {
                    if (s is not Panel c) return;
                    if (c.Width <= 4 || c.Height <= 4) return;
                    bool gold = isGold[idx];
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    Rectangle rect = c.ClientRectangle;
                    rect.Inflate(-2, -2);
                    if (rect.Width <= 1 || rect.Height <= 1) return;
                    GraphicsPath path = GetRoundedPath(rect, 14);

                    if (gold)
                    {
                        // Richer gold gradient
                        using (var lgb = new LinearGradientBrush(rect,
                            Color.FromArgb(238, 190, 42), Color.FromArgb(207, 158, 16), 145f))
                            g.FillPath(lgb, path);

                        // Sheen highlight at top
                        Rectangle sheen = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2);
                        using (var sheenBrush = new LinearGradientBrush(sheen,
                            Color.FromArgb(40, 255, 255, 255), Color.Transparent, 90f))
                            g.FillPath(sheenBrush, path);
                    }
                    else
                    {
                        // Glass: dark green translucent fill
                        using (var sb = new SolidBrush(Color.FromArgb(34, 255, 255, 255)))
                            g.FillPath(sb, path);
                        // Inner gradient for depth
                        using (var lgb = new LinearGradientBrush(rect,
                            Color.FromArgb(18, 255, 255, 255), Color.FromArgb(6, 255, 255, 255), 90f))
                            g.FillPath(lgb, path);
                        using (var pen = new Pen(Color.FromArgb(65, 212, 160, 23), 1f))
                            g.DrawPath(pen, path);
                    }

                    TextRenderer.DrawText(g, icons[idx],
                        new Font("Segoe UI Emoji", 20f),
                        new Rectangle(rect.X + 18, rect.Y + 18, 54, 40),
                        gold ? Color.FromArgb(7, 26, 14) : C_TEXT,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                    Color labelFg = gold ? Color.FromArgb(7, 26, 14) : C_TEXT;
                    TextRenderer.DrawText(g, labels[idx],
                        new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                        new Rectangle(rect.X + 18, rect.Y + 72, rect.Width - 36, 24),
                        labelFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    Color subFg = gold ? Color.FromArgb(145, 7, 26, 14) : Color.FromArgb(145, 248, 244, 239);
                    TextRenderer.DrawText(g, subs[idx],
                        new Font("Segoe UI", 8.5f),
                        new Rectangle(rect.X + 18, rect.Y + 96, rect.Width - 36, 20),
                        subFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                };

                card.Click += (s, e) =>
                {
                    switch (idx)
                    {
                        case 0:
                            btnCabins_Click(btnCabins, EventArgs.Empty);
                            break;
                        case 1:
                            btnExperiences_Click(btnExperiences, EventArgs.Empty);
                            break;
                        case 2:
                            btnMap_Click(btnMap, EventArgs.Empty);
                            break;
                    }
                };

                pnlWildNest.Controls.Add(card);
            }

            if (!pnlHero.Controls.Contains(pnlWildNest))
            {
                pnlHero.Controls.Add(pnlWildNest);
            }
            pnlWildNest.BringToFront();

            // ── RATE BADGE (right side) ──
            pnlRate = new Panel { BackColor = Color.Transparent };

            pnlRate.Paint += (s, pe) =>
            {
                if (s is not Panel c) return;
                if (c.Width <= 4 || c.Height <= 4) return;
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle rect = c.ClientRectangle;
                rect.Inflate(-2, -2);
                if (rect.Width <= 1 || rect.Height <= 1) return;
                GraphicsPath path = GetRoundedPath(rect, 18);

                // Refined glass fill
                using (var lgb = new LinearGradientBrush(rect,
                    Color.FromArgb(22, 255, 255, 255), Color.FromArgb(10, 255, 255, 255), 120f))
                    g.FillPath(lgb, path);

                // Border
                using (var pen = new Pen(Color.FromArgb(72, 212, 160, 23), 1.1f))
                    g.DrawPath(pen, path);

                // Star rating at top
                TextRenderer.DrawText(g, "4.9",
                    new Font("Georgia", 31f, FontStyle.Bold),
                    new Rectangle(rect.X, rect.Y + 36, rect.Width, 58),
                    C_GOLD,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, "★★★★★",
                    new Font("Segoe UI", 13.5f),
                    new Rectangle(rect.X, rect.Y + 92, rect.Width, 30),
                    C_GOLD,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(g, "GUEST RATING",
                    new Font("Segoe UI", 7.8f, FontStyle.Regular),
                    new Rectangle(rect.X, rect.Y + 124, rect.Width, 20),
                    Color.FromArgb(155, 248, 244, 239),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                int divY = rect.Y + 164;
                using (var pen = new Pen(Color.FromArgb(60, 212, 160, 23), 1))
                    g.DrawLine(pen, rect.X + 34, divY, rect.Right - 34, divY);

                string[] statLines = { "170 hectares", "35 animal species", "8 wildlife zones" };
                for (int i = 0; i < statLines.Length; i++)
                {
                    TextRenderer.DrawText(g, statLines[i],
                        new Font("Segoe UI", 9f, FontStyle.Regular),
                            new Rectangle(rect.X, rect.Y + 178 + i * 28, rect.Width, 22),
                        Color.FromArgb(220, 248, 244, 239),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            pnlHero.Controls.Add(pnlRate);
            pnlRate.BringToFront();
        }

        private void LayoutWildNestPanel()
        {
            int heroW = pnlHero.Width;
            int contentW = Math.Min(heroW - 260, 1540);
            int heroLeft = Math.Max(118, (heroW - contentW) / 2);
            int rateW = 248;
            int gap = 60;
            int leftLaneW = Math.Min(1120, Math.Max(760, heroW - heroLeft - rateW - gap - 140));
            int rateX = heroLeft + leftLaneW + gap;

            if (rateX + rateW > heroW - 110)
            {
                rateX = Math.Max(heroLeft + 700, heroW - rateW - 110);
                leftLaneW = Math.Max(700, rateX - heroLeft - gap);
            }

            int actionW = leftLaneW;
            int cardH = 128;
            int panelY = 348;
            int actionsX = heroLeft;

            pnlWildNest.Location = new Point(actionsX, panelY);
            pnlWildNest.Size = new Size(actionW, cardH);

            int cardGap = 16;
            int cardW = (actionW - (cardGap * 2)) / 3;

            for (int i = 0; i < pnlWildNest.Controls.Count; i++)
            {
                pnlWildNest.Controls[i].Location = new Point(i * (cardW + cardGap), 0);
                pnlWildNest.Controls[i].Size = new Size(cardW, cardH);
            }

            int rateH = 286;
            int rateY = 160;
            if (pnlRate != null)
            {
                pnlRate.Location = new Point(rateX, rateY);
                pnlRate.Size = new Size(rateW, rateH);
            }
        }

        // ══════════════════════════════════════════════════════
        // STYLE PILL BUTTON
        // ══════════════════════════════════════════════════════
        private void StylePillButton(Button btn, Color bg, Color fg, string text, bool border = false)
        {
            btn.Text = "";
            btn.Size = new Size(148, 44);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                Rectangle rect = btn.ClientRectangle;
                rect.Inflate(-2, -2);

                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = rect.Height;
                    path.AddArc(rect.X, rect.Y, r, r, 90, 180);
                    path.AddArc(rect.Width - r + rect.X, rect.Y, r, r, 270, 180);
                    path.CloseFigure();

                    if (bg != Color.Transparent)
                    {
                        // Richer gold gradient for Book Now
                        using (var lgb = new LinearGradientBrush(rect,
                            Color.FromArgb(232, 185, 45),
                            Color.FromArgb(195, 145, 15), 135f))
                            e.Graphics.FillPath(lgb, path);

                        // Subtle sheen highlight at top
                        Rectangle sheen = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height / 2 - 2);
                        using (var sheenBrush = new LinearGradientBrush(sheen,
                            Color.FromArgb(55, 255, 255, 255), Color.Transparent, 90f))
                            e.Graphics.FillPath(sheenBrush, path);
                    }

                    if (border)
                    {
                        using (var p = new Pen(Color.FromArgb(130, 255, 255, 255), 1.2f))
                            e.Graphics.DrawPath(p, path);
                    }

                    Font font = bg != Color.Transparent
                        ? new Font("Segoe UI", 10, FontStyle.Bold)
                        : new Font("Segoe UI", 10);

                    TextRenderer.DrawText(e.Graphics, text,
                        font, rect, fg,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
        }

        // ══════════════════════════════════════════════════════
        // STATUS BAR
        // ══════════════════════════════════════════════════════
        private void SetupStatusBar()
        {
            pnlStatusBar.Controls.Clear();

            // Top and bottom gold accent lines
            Panel goldTop = new Panel { Height = 2, Dock = DockStyle.Top, BackColor = Color.FromArgb(200, 212, 160, 23) };
            Panel goldBottom = new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = Color.FromArgb(60, 212, 160, 23) };
            pnlStatusBar.Controls.Add(goldTop);
            pnlStatusBar.Controls.Add(goldBottom);

            tableLayoutPanel1 = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(24, 2, 24, 2) };
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            tableLayoutPanel1.Controls.Add(CreateStatusLabel("10 cabins available"), 0, 0);
            tableLayoutPanel1.Controls.Add(CreateStatusLabel("Night Safari starts in 4h 22m"), 1, 0);
            tableLayoutPanel1.Controls.Add(CreateStatusLabel("35 animals across 8 zones"), 2, 0);

            pnlStatusBar.Controls.Add(tableLayoutPanel1);
        }

        private Label CreateStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(220, 212, 160, 23),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Padding = new Padding(8, 0, 8, 0)
            };
        }

        // ══════════════════════════════════════════════════════
        // ANIMAL OF THE DAY (pnlAnimalDay Paint)
        // ══════════════════════════════════════════════════════
        private void pnlAnimalDay_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int marginX = 60;
            int marginY = 12;
            Rectangle cardRect = new Rectangle(marginX, marginY,
                pnlAnimalDay.Width - marginX * 2, pnlAnimalDay.Height - marginY * 2);

            using (GraphicsPath path = GetRoundedPath(cardRect, 20))
            {
                // Multi-layer shadow for depth
                for (int sh = 6; sh >= 1; sh--)
                {
                    Rectangle shadow = cardRect;
                    shadow.Inflate(0, sh / 2);
                    shadow.Offset(0, sh);
                    using (var sb = new SolidBrush(Color.FromArgb(8 + sh * 3, 0, 0, 0)))
                        g.FillPath(sb, GetRoundedPath(shadow, 20));
                }

                // Card fill — warm white with subtle gradient
                using (var lgb = new LinearGradientBrush(cardRect,
                    Color.FromArgb(255, 255, 255, 253),
                    Color.FromArgb(255, 248, 246, 242), 90f))
                    g.FillPath(lgb, path);

                g.SetClip(path);

                // Gold left accent bar with gradient
                using (var accentBrush = new LinearGradientBrush(
                    new Rectangle(cardRect.X, cardRect.Y, 8, cardRect.Height),
                    C_GOLD_LIGHT, C_GOLD, 90f))
                    g.FillRectangle(accentBrush, cardRect.X, cardRect.Y, 7, cardRect.Height);

                // Subtle warm tint on top-right area
                using (var tintPath = new GraphicsPath())
                {
                    tintPath.AddEllipse(cardRect.Right - 200, cardRect.Y - 40, 280, 180);
                    using (var pgb = new PathGradientBrush(tintPath))
                    {
                        pgb.CenterColor = Color.FromArgb(10, 212, 160, 23);
                        pgb.SurroundColors = new[] { Color.Transparent };
                        g.FillPath(pgb, tintPath);
                    }
                }

                g.ResetClip();

                // Crisp border
                using (var pen = new Pen(Color.FromArgb(45, 212, 160, 23), 1.2f))
                    g.DrawPath(pen, path);
            }

            int contentX = cardRect.X + 28;
            int contentY = cardRect.Y + (cardRect.Height / 2) - 50;

            // Avatar circle with ring
            int avatarSize = 98;
            g.FillEllipse(new SolidBrush(Color.FromArgb(228, 245, 238)), contentX, contentY, avatarSize, avatarSize);
            using (var ringPen = new Pen(Color.FromArgb(80, 212, 160, 23), 2f))
                g.DrawEllipse(ringPen, contentX - 1, contentY - 1, avatarSize + 2, avatarSize + 2);
            TextRenderer.DrawText(g, "🦁", new Font("Segoe UI Emoji", 40f),
                new Rectangle(contentX, contentY, avatarSize, avatarSize), Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int textX = contentX + avatarSize + 22;
            int textY = cardRect.Y + 18;

            // Label pill
            Rectangle pillRect = new Rectangle(textX, textY, 148, 20);
            using (var pillPath = GetRoundedPath(pillRect, 10))
            using (var sb = new SolidBrush(Color.FromArgb(28, 212, 160, 23)))
                g.FillPath(sb, pillPath);

            TextRenderer.DrawText(g, "✦  ANIMAL OF THE DAY",
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new Rectangle(textX + 6, textY, 160, 20), Color.FromArgb(148, 89, 14),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(g, "African Lion — \"Malakas\"",
                new Font("Georgia", 20f, FontStyle.Bold),
                new Point(textX, textY + 24), Color.FromArgb(20, 20, 20));

            TextRenderer.DrawText(g, "🟢  Golden Savanna Zone  ·  Feeding at 3:00 PM today",
                new Font("Segoe UI", 9.5f),
                new Point(textX, textY + 56), Color.FromArgb(12, 108, 78));

            string desc = "Two cubs born last month are now visible to guests during morning rounds. Malakas has been exceptionally\nactive this week — rangers report the pride is frequently spotted near the northern viewing platform at sunrise.";
            TextRenderer.DrawText(g, desc,
                new Font("Segoe UI", 9.5f),
                new Rectangle(textX, textY + 80, cardRect.Width - (textX - cardRect.X) - 30, 68),
                Color.FromArgb(75, 75, 75),
                TextFormatFlags.WordBreak);
        }

        private void SetupAnimalDayButtons()
        {
            var toRemove = new List<Control>();
            foreach (Control c in pnlAnimalDay.Controls)
                if (c is Button) toRemove.Add(c);
            foreach (var c in toRemove) pnlAnimalDay.Controls.Remove(c);
        }

        // ══════════════════════════════════════════════════════
        // CABIN CARDS
        // ══════════════════════════════════════════════════════
        private void GenerateCabinCards()
        {
            pnlCabinSection.Controls.Clear();
            pnlCabinSection.BackColor = C_BG;
            pnlCabinSection.Location = new Point(0, pnlAnimalDay.Bottom + 30);
            pnlCabinSection.Width = pnlMain.Width > 0 ? pnlMain.Width : 1262;

            int sectionW = pnlCabinSection.Width;
            int marginX = GetSectionMargin(sectionW);

            pnlCabinSection.Controls.Add(CreateTextLabel("FEATURED STAY", new Font("Segoe UI", 8f, FontStyle.Bold), Color.FromArgb(148, 90, 14), new Point(marginX, 12), new Size(sectionW - marginX * 2, 20)));
            pnlCabinSection.Controls.Add(CreateTextLabel("Our Signature Accommodation", new Font("Georgia", 24, FontStyle.Bold), C_DARK, new Point(marginX, 34), new Size(sectionW - marginX * 2, 40)));
            pnlCabinSection.Controls.Add(CreateTextLabel("WildNest's most exclusive retreat — the crown jewel of the resort", new Font("Segoe UI", 10), Color.FromArgb(115, 115, 115), new Point(marginX, 78), new Size(sectionW - marginX * 2, 22)));

            int cardW = sectionW - marginX * 2;
            int cardH = 440;
            int cardY = 106;

            Panel sigCard = new Panel
            {
                Size = new Size(cardW, cardH),
                Location = new Point(marginX, cardY),
                BackColor = Color.FromArgb(13, 32, 20)
            };
            sigCard.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, cardW, cardH, 22, 22));

            // LEFT
            int leftW = (int)(cardW * 0.45);
            Panel leftPanel = new Panel
            {
                Size = new Size(leftW, cardH),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(18, 44, 28)
            };
            leftPanel.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, leftW, cardH, 22, 22));

            leftPanel.Paint += (s, pe) =>
            {
                if (leftPanel.Width <= 1 || leftPanel.Height <= 1) return;
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Richer bg gradient
                using (var lgb = new LinearGradientBrush(leftPanel.ClientRectangle,
                    Color.FromArgb(15, 38, 22), Color.FromArgb(24, 58, 36), 135f))
                    g.FillRectangle(lgb, leftPanel.ClientRectangle);

                // Center radial glow behind icon
                using (var glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(new Rectangle(leftW / 2 - 100, cardH / 2 - 130, 200, 200));
                    using (var pgb = new PathGradientBrush(glowPath))
                    {
                        pgb.CenterColor = Color.FromArgb(35, 212, 160, 23);
                        pgb.SurroundColors = new[] { Color.Transparent };
                        g.FillPath(pgb, glowPath);
                    }
                }

                // Bottom fade overlay for badge area
                using (var fadeBrush = new LinearGradientBrush(
                    new Rectangle(0, cardH - 100, leftW, 100),
                    Color.Transparent, Color.FromArgb(60, 0, 0, 0), 90f))
                    g.FillRectangle(fadeBrush, 0, cardH - 100, leftW, 100);
            };

            Label lblIcon = new Label
            {
                Text = "🏯",
                Font = new Font("Segoe UI Emoji", 54),
                Size = new Size(130, 130),
                Location = new Point(leftW / 2 - 65, cardH / 2 - 90),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            leftPanel.Controls.Add(lblIcon);

            AddBadge(leftPanel, "● Available", Color.FromArgb(30, 200, 120), Color.FromArgb(20, 60, 35), leftW / 2 - 76, cardH - 65);
            AddBadge(leftPanel, "★ 5.0", C_GOLD, Color.FromArgb(50, 40, 10), leftW / 2 + 14, cardH - 65);
            sigCard.Controls.Add(leftPanel);

            // RIGHT
            int rightX = leftW + 2;
            int rightW = cardW - rightX;
            Panel rightPanel = new Panel
            {
                Size = new Size(rightW, cardH),
                Location = new Point(rightX, 0),
                BackColor = Color.FromArgb(13, 32, 20)
            };

            int rPad = 32;
            rightPanel.Controls.Add(CreateTextLabel("PREMIUM VILLA  ·  AQUATIC ZONE", new Font("Segoe UI", 8.5f, FontStyle.Bold), C_GOLD, new Point(rPad, 28), new Size(rightW - rPad * 2, 18)));
            rightPanel.Controls.Add(CreateTextLabel("The Sanctuary Villa", new Font("Georgia", 23, FontStyle.Bold), C_TEXT, new Point(rPad, 50), new Size(rightW - rPad * 2, 38)));

            Panel goldAccent = new Panel { Size = new Size(44, 3), Location = new Point(rPad, 94), BackColor = C_GOLD };
            rightPanel.Controls.Add(goldAccent);

            Label lblDesc = CreateWrappedLabel(
                "WildNest's crown jewel — an exclusive three-bedroom villa with a full infinity pool and sweeping panoramic views of the entire savanna. Private walled garden, full kitchen, and a dedicated villa host for the duration of your stay.",
                new Font("Segoe UI", 9.5f),
                Color.FromArgb(190, 215, 195),
                new Point(rPad, 108),
                new Size(rightW - rPad * 2, 86));
            rightPanel.Controls.Add(lblDesc);

            string[] chips = { "🏊 Infinity Pool", "🛏 3 Bedrooms", "🧑‍🍳 Villa Host", "👥 6 Guests", "🌿 Private Garden", "🚗 Buggy Transfer" };
            int chipX = rPad, chipY = 200;
            foreach (string chip in chips)
            {
                Label lc = new Label
                {
                    Text = chip,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(200, 220, 200),
                    BackColor = Color.FromArgb(28, 60, 38),
                    AutoSize = true,
                    Padding = new Padding(8, 4, 8, 4)
                };
                lc.Location = new Point(chipX, chipY);
                rightPanel.Controls.Add(lc);
                lc.Size = new Size(TextRenderer.MeasureText(chip, lc.Font).Width + 22, 27);
                lc.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, lc.Width, 27, 13, 13));
                chipX += lc.Width + 9;
                if (chipX > rightW - rPad - 20) { chipX = rPad; chipY += 35; }
            }

            rightPanel.Controls.Add(CreateTextLabel("₱11,000", new Font("Georgia", 34, FontStyle.Bold), C_GOLD, new Point(rPad, cardH - 158), new Size(360, 52)));
            rightPanel.Controls.Add(CreateTextLabel("/night  ·  Free cancellation", new Font("Segoe UI", 9.5f), Color.FromArgb(130, 150, 130), new Point(rPad, cardH - 106), new Size(rightW - rPad * 2, 28)));

            sigCard.Controls.Add(rightPanel);
            pnlCabinSection.Controls.Add(sigCard);

            pnlCabinSection.Height = cardY + cardH + 28;
        }

        // ══════════════════════════════════════════════════════
        // EXPERIENCE SECTION
        // ══════════════════════════════════════════════════════
        private void PopulateExperienceSection()
        {
            pnlExperience.Controls.Clear();
            pnlExperience.BackColor = C_BG;
            pnlExperience.Region = null;

            int sectionW = pnlExperience.Width;
            int marginX = GetSectionMargin(sectionW);

            pnlExperience.Controls.Add(CreateTextLabel("FEATURED EXPERIENCE", new Font("Segoe UI", 8f, FontStyle.Bold), Color.FromArgb(148, 90, 14), new Point(marginX, 12), new Size(sectionW - marginX * 2, 20)));
            pnlExperience.Controls.Add(CreateTextLabel("Our Signature Wildlife Experience", new Font("Georgia", 24, FontStyle.Bold), C_DARK, new Point(marginX, 34), new Size(sectionW - marginX * 2, 40)));
            pnlExperience.Controls.Add(CreateTextLabel("The ultimate WildNest adventure — not to be missed", new Font("Segoe UI", 10), Color.FromArgb(115, 115, 115), new Point(marginX, 78), new Size(sectionW - marginX * 2, 22)));

            int cardW = sectionW - marginX * 2;
            int cardH = 440;

            Panel sigCard = new Panel
            {
                Size = new Size(cardW, cardH),
                Location = new Point(marginX, 106),
                BackColor = Color.FromArgb(20, 15, 44)
            };
            sigCard.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, cardW, cardH, 22, 22));

            int leftW = (int)(cardW * 0.45);
            Panel leftPanel = new Panel
            {
                Size = new Size(leftW, cardH),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(28, 22, 58)
            };
            leftPanel.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, leftW, cardH, 22, 22));

            leftPanel.Paint += (s, pe) =>
            {
                if (leftPanel.Width <= 1 || leftPanel.Height <= 1) return;
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Richer deep purple gradient
                using (var lgb = new LinearGradientBrush(leftPanel.ClientRectangle,
                    Color.FromArgb(22, 16, 50), Color.FromArgb(36, 28, 72), 135f))
                    g.FillRectangle(lgb, leftPanel.ClientRectangle);

                // Purple-gold center glow behind icon
                using (var glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(new Rectangle(leftW / 2 - 100, cardH / 2 - 130, 200, 200));
                    using (var pgb = new PathGradientBrush(glowPath))
                    {
                        pgb.CenterColor = Color.FromArgb(40, 180, 130, 255);
                        pgb.SurroundColors = new[] { Color.Transparent };
                        g.FillPath(pgb, glowPath);
                    }
                }

                // Bottom fade for badge area
                using (var fadeBrush = new LinearGradientBrush(
                    new Rectangle(0, cardH - 100, leftW, 100),
                    Color.Transparent, Color.FromArgb(70, 0, 0, 0), 90f))
                    g.FillRectangle(fadeBrush, 0, cardH - 100, leftW, 100);
            };

            leftPanel.Controls.Add(new Label
            {
                Text = "🗺️",
                Font = new Font("Segoe UI Emoji", 54),
                Size = new Size(130, 130),
                Location = new Point(leftW / 2 - 65, cardH / 2 - 90),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            AddBadge(leftPanel, "● Available", Color.FromArgb(30, 200, 120), Color.FromArgb(20, 50, 35), leftW / 2 - 76, cardH - 65);
            AddBadge(leftPanel, "★ 5.0", C_GOLD, Color.FromArgb(50, 40, 10), leftW / 2 + 14, cardH - 65);
            sigCard.Controls.Add(leftPanel);

            int rightX = leftW + 2;
            int rightW = cardW - rightX;
            Panel rightPanel = new Panel { Size = new Size(rightW, cardH), Location = new Point(rightX, 0), BackColor = Color.FromArgb(20, 15, 44) };

            int rPad = 32;
            rightPanel.Controls.Add(CreateTextLabel("GRAND TOUR  ·  FULL RESORT — ALL 8 ZONES", new Font("Segoe UI", 8.5f, FontStyle.Bold), C_GOLD, new Point(rPad, 28), new Size(rightW - rPad * 2, 18)));
            rightPanel.Controls.Add(CreateTextLabel("Grand Safari Circuit", new Font("Georgia", 23, FontStyle.Bold), C_TEXT, new Point(rPad, 50), new Size(rightW - rPad * 2, 38)));

            Panel goldAccent = new Panel { Size = new Size(44, 3), Location = new Point(rPad, 94), BackColor = C_GOLD };
            rightPanel.Controls.Add(goldAccent);

            Label lblDesc = CreateWrappedLabel(
                "The ultimate WildNest experience — a fully guided four-hour expedition through all eight wildlife zones aboard our custom open-top Electric Safari Tram. Expert naturalist narrates every zone, provides live animal health updates, and stops for hands-on encounters at three interaction points.",
                new Font("Segoe UI", 9.5f),
                Color.FromArgb(180, 180, 210),
                new Point(rPad, 108),
                new Size(rightW - rPad * 2, 86));
            rightPanel.Controls.Add(lblDesc);

            string[] chips = { "🚌 Electric Tram", "🎙️ Expert Guide", "📋 Conservation Cert", "📸 Photo Stops", "🦁 3 Encounters", "☕ Welcome Drink" };
            int chipX = rPad, chipY = 200;
            foreach (string chip in chips)
            {
                Label lc = new Label
                {
                    Text = chip,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(200, 200, 230),
                    BackColor = Color.FromArgb(35, 30, 70),
                    AutoSize = true,
                    Padding = new Padding(8, 4, 8, 4)
                };
                lc.Location = new Point(chipX, chipY);
                rightPanel.Controls.Add(lc);
                lc.Size = new Size(TextRenderer.MeasureText(chip, lc.Font).Width + 22, 27);
                lc.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, lc.Width, 27, 13, 13));
                chipX += lc.Width + 9;
                if (chipX > rightW - rPad - 20) { chipX = rPad; chipY += 35; }
            }

            rightPanel.Controls.Add(CreateTextLabel("₱1,800", new Font("Georgia", 34, FontStyle.Bold), C_GOLD, new Point(rPad, cardH - 158), new Size(360, 52)));
            rightPanel.Controls.Add(CreateTextLabel("/person  ·  4 hours  ·  Max 12 guests", new Font("Segoe UI", 9.5f), Color.FromArgb(130, 130, 160), new Point(rPad, cardH - 106), new Size(rightW - rPad * 2, 28)));

            sigCard.Controls.Add(rightPanel);
            pnlExperience.Controls.Add(sigCard);

            pnlExperience.Height = 106 + cardH + 28;
        }

        // ══════════════════════════════════════════════════════
        // HIGHLIGHTS GRID (4 stat cards)
        // ══════════════════════════════════════════════════════
        private void SetupHighlightsGrid()
        {
            pnlHighlightsGrid.Controls.Clear();
            pnlHighlightsGrid.BackColor = C_BG;

            string[] icons = { "🌿", "🐾", "🏕️", "🎟️" };
            string[] stats = { "170", "35", "10", "5" };
            string[] labels = { "Hectares of sanctuary", "Animal species", "Cabin stays", "Wildlife experiences" };

            int sectionW = pnlHighlightsGrid.Width;
            int marginX = GetSectionMargin(sectionW);
            int gap = 16;
            int totalW = sectionW - marginX * 2;
            int cardW = (totalW - gap * 3) / 4;
            int cardH = 176;

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                Panel card = new Panel
                {
                    Size = new Size(cardW, cardH),
                    Location = new Point(marginX + i * (cardW + gap), 10),
                    BackColor = Color.White
                };
                card.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, cardW, cardH, 18, 18));

                card.Paint += (s, pe) =>
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    Rectangle rect = card.ClientRectangle;
                    rect.Inflate(-1, -1);

                    // Soft shadow
                    Rectangle shadow = rect;
                    shadow.Inflate(1, 1);
                    shadow.Offset(0, 3);
                    using (var sb = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
                        g.FillPath(sb, GetRoundedPath(shadow, 18));

                    using (var path = GetRoundedPath(rect, 18))
                    {
                        // Warm white gradient fill
                        using (var lgb = new LinearGradientBrush(rect,
                            Color.FromArgb(255, 255, 255, 253),
                            Color.FromArgb(255, 250, 248, 244), 90f))
                            g.FillPath(lgb, path);

                        using (var pen = new Pen(Color.FromArgb(28, 212, 160, 23), 1.2f))
                            g.DrawPath(pen, path);
                    }

                    // Top gold gradient stripe
                    using (var stripeBrush = new LinearGradientBrush(
                        new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 5),
                        C_GOLD_LIGHT, C_GOLD, 0f))
                        g.FillRectangle(stripeBrush, rect.X + 1, rect.Y + 1, rect.Width - 2, 5);

                    // Icon with subtle bg circle
                    int iconY = rect.Y + 18;
                    g.FillEllipse(new SolidBrush(Color.FromArgb(22, 212, 160, 23)),
                        rect.X + rect.Width / 2 - 19, iconY - 1, 38, 36);
                    TextRenderer.DrawText(g, icons[idx],
                        new Font("Segoe UI Emoji", 17),
                        new Rectangle(rect.X, iconY, rect.Width, 32),
                        Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                    // Big stat number
                    TextRenderer.DrawText(g, stats[idx],
                        new Font("Georgia", 25, FontStyle.Bold),
                        new Rectangle(rect.X, rect.Y + 50, rect.Width, 64),
                        C_DARK,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

                    // Thin divider
                    int cx = rect.X + rect.Width / 2;
                    int divY = rect.Y + 112;
                    using (var pen = new Pen(Color.FromArgb(40, 212, 160, 23), 1))
                        g.DrawLine(pen, cx - 22, divY, cx + 22, divY);

                    // Label below divider
                    TextRenderer.DrawText(g, labels[idx],
                        new Font("Segoe UI", 8.7f),
                        new Rectangle(rect.X + 12, divY + 10, rect.Width - 24, 42),
                        Color.FromArgb(120, 120, 120),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                };

                pnlHighlightsGrid.Controls.Add(card);
            }

            pnlHighlightsGrid.Height = cardH + 26;
        }

        // ══════════════════════════════════════════════════════
        // FOOTER SECTION
        // ══════════════════════════════════════════════════════
        private void SetupFooterSection()
        {
            pnlFooterSection.Controls.Clear();
            pnlFooterSection.BackColor = C_BG;

            int sectionW = pnlFooterSection.Width;
            int marginX = GetSectionMargin(sectionW);

            // Conservation mission card
            int cardW = sectionW - marginX * 2;
            Panel mission = new Panel { Size = new Size(cardW, 106), Location = new Point(marginX, 14), BackColor = Color.Transparent };
            mission.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, cardW, 106, 16, 16));

            mission.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = mission.ClientRectangle;
                r.Inflate(-1, -1);
                using (var path = GetRoundedPath(r, 16))
                {
                    // Warm white gradient
                    using (var lgb = new LinearGradientBrush(r,
                        Color.FromArgb(255, 255, 255, 254),
                        Color.FromArgb(255, 248, 245, 240), 90f))
                        g.FillPath(lgb, path);

                    // Soft shadow at bottom
                    using (var shadowBrush = new LinearGradientBrush(
                        new Rectangle(r.X, r.Bottom - 20, r.Width, 20),
                        Color.Transparent, Color.FromArgb(8, 0, 0, 0), 90f))
                        g.FillPath(shadowBrush, path);

                    using (var pen = new Pen(Color.FromArgb(30, 212, 160, 23), 1.2f))
                        g.DrawPath(pen, path);

                    // Left accent stripe
                    using (var accentBrush = new LinearGradientBrush(
                        new Rectangle(r.X, r.Y, 5, r.Height), C_GOLD_LIGHT, C_GOLD, 90f))
                        g.FillRectangle(accentBrush, r.X, r.Y + 1, 5, r.Height - 2);
                }
            };

            Label missionIcon = new Label { Text = "🌿", Font = new Font("Segoe UI Emoji", 20), Size = new Size(56, 56), Location = new Point(26, 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(200, 235, 222) };
            missionIcon.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, 62, 62, 62, 62));
            mission.Controls.Add(missionIcon);
            mission.Controls.Add(CreateTextLabel("WildNest Conservation Mission", new Font("Segoe UI Semibold", 12f, FontStyle.Bold), C_DARK, new Point(96, 20), new Size(cardW - 124, 24)));

            Label missionText = CreateWrappedLabel(
                "Every booking directly funds the daily care of our 35 resident species and supports the local Carmen community through our conservation-first operating model. WildNest is a certified wildlife sanctuary — not a zoo.",
                new Font("Segoe UI", 9f),
                Color.FromArgb(105, 105, 105),
                new Point(96, 44),
                new Size(cardW - 124, 42));
            mission.Controls.Add(missionText);
            pnlFooterSection.Controls.Add(mission);

            // Footer bar
            Panel footer = new Panel { Size = new Size(sectionW, 102), Location = new Point(0, 132), BackColor = Color.Transparent };

            footer.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = footer.ClientRectangle;

                // Deep dark gradient
                using (var lgb = new LinearGradientBrush(r,
                    Color.FromArgb(9, 30, 16), Color.FromArgb(5, 18, 10), 90f))
                    g.FillRectangle(lgb, r);

                // Gold top glow
                using (var glowBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, r.Width, 10),
                    Color.FromArgb(38, 212, 160, 23), Color.Transparent, 90f))
                    g.FillRectangle(glowBrush, 0, 0, r.Width, 10);

                // Crisp gold top line
                using (var pen = new Pen(Color.FromArgb(210, 212, 160, 23), 1.5f))
                    g.DrawLine(pen, 0, 0, r.Width, 0);
            };

            footer.Controls.Add(CreateTextLabel("WILDNEST", new Font("Georgia", 17, FontStyle.Bold), Color.FromArgb(248, 244, 239), new Point(marginX, 18), new Size(300, 28)));
            footer.Controls.Add(CreateTextLabel("Zoo Resort  Wildlife Experience", new Font("Segoe UI", 9f), Color.FromArgb(160, 210, 170), new Point(marginX, 46), new Size(340, 20)));
            footer.Controls.Add(CreateTextLabel("© 2026 WildNest Resort. Carmen, Cebu, Philippines.", new Font("Segoe UI", 8.1f), Color.FromArgb(100, 150, 110), new Point(marginX, 70), new Size(380, 20)));

            // "BACK TO TOP" with a small up-arrow feel
            Label lblBackToTop = CreateTextLabel("↑  BACK TO TOP", new Font("Segoe UI", 9, FontStyle.Bold), C_GOLD, new Point(sectionW - marginX - 170, 44), new Size(170, 22));
            lblBackToTop.TextAlign = ContentAlignment.MiddleRight;
            lblBackToTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblBackToTop.Cursor = Cursors.Hand;
            lblBackToTop.Click += (s, ev) => { pnlMain.AutoScrollPosition = new Point(0, 0); };
            footer.Controls.Add(lblBackToTop);

            pnlFooterSection.Controls.Add(footer);
            pnlFooterSection.Height = 236;
        }

        // ══════════════════════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════════════════════
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Label MakeLabel(string text, Font font, Color fg, Point location)
        {
            return new Label { Text = text, Font = font, ForeColor = fg, Location = location, AutoSize = true, BackColor = Color.Transparent };
        }

        private static Image? LoadSafeLogoImage()
        {
            return AppAssetLoader.LoadImage("Logo", "Resources", "Logo.png");
        }

        private Label CreateTextLabel(string text, Font font, Color fg, Point location, Size size)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = fg,
                Location = location,
                Size = size,
                AutoSize = false,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
        }

        private Label CreateWrappedLabel(string text, Font font, Color fg, Point location, Size size)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = fg,
                Location = location,
                Size = size,
                AutoSize = false,
                BackColor = Color.Transparent
            };
        }

        private int GetSectionMargin(int width)
        {
            return Math.Max(28, Math.Min(60, width / 18));
        }

        private void AddBadge(Panel parent, string text, Color fg, Color bg, int x, int y)
        {
            Label lbl = new Label { Text = text, Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = fg, BackColor = bg, Location = new Point(x, y), AutoSize = true, Padding = new Padding(8, 4, 8, 4) };
            lbl.Size = new Size(TextRenderer.MeasureText(text, lbl.Font).Width + 18, 26);
            lbl.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, lbl.Width, 26, 13, 13));
            parent.Controls.Add(lbl);
        }

        private Button MakeGoldBtn(string text, Point location, int width)
        {
            Button btn = new Button { Size = new Size(width, 34), Location = location, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = C_GOLD, ForeColor = C_DARK, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Text = text };
            btn.FlatAppearance.BorderSize = 0;
            btn.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, width, 34, 17, 17));
            return btn;
        }

        private Button MakeDarkOutlineBtn(string text, Point location, int width)
        {
            Button btn = new Button { Size = new Size(width, 34), Location = location, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.Transparent, ForeColor = Color.FromArgb(180, 200, 180), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Text = text };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 200, 200, 200);
            btn.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, width, 34, 17, 17));
            return btn;
        }

        // ══════════════════════════════════════════════════════
        // NAV BUTTON CLICK HANDLERS
        // ══════════════════════════════════════════════════════
        private void NavigateTo(Control uc, string pageTag, bool autoScroll = true)
        {
            if (_isTransitioning || _activePage == pageTag)
                return;

            _activePage = pageTag;
            InvalidateAllNavButtons();

            uc.Dock = DockStyle.None;
            uc.Width = pnlMain.ClientSize.Width;
            uc.Height = Math.Max(pnlMain.ClientSize.Height, uc.Height);
            uc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uc.Location = new Point(0, 0);

            BeginPanelTransition(uc, autoScroll);
        }

        private void btnCabins_Click(object sender, EventArgs e) => NavigateTo(new UcCabin(), "Cabins", autoScroll: true);
        private void btnExperiences_Click(object sender, EventArgs e) => NavigateTo(new UcExperience(), "Experiences", autoScroll: true);
        private void btnAnimals_Click(object sender, EventArgs e) => NavigateTo(new UcAnimals(), "Animals");
        private void btnVisit_Click(object sender, EventArgs e) => NavigateTo(new UcVisit(), "Visit", autoScroll: true);
        private void btnAbout_Click(object sender, EventArgs e) => NavigateTo(new UcAbout(), "About", autoScroll: true);
        private void btnMap_Click(object sender, EventArgs e)
        {
            if (_isTransitioning || _activePage == "Map")
                return;

            _activePage = "Map";
            InvalidateAllNavButtons();

            UcMap uc = new UcMap
            {
                Dock = DockStyle.None,
                Width = pnlMain.ClientSize.Width,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 0)
            };

            BeginPanelTransition(uc, autoScroll: true);
        }

        private void btnStaffPortal_Click(object sender, EventArgs e)
        {
            this.Hide();
            StaffLogin loginForm = new StaffLogin();
            loginForm.Owner = this;
            loginForm.WindowState = FormWindowState.Maximized;
            loginForm.FormClosed += (s, args) => this.Show();
            loginForm.Show();
        }

        private void btnBookNow_Click(object sender, EventArgs e)
        {
            if (_isTransitioning || _activePage == "BookNow")
                return;

            _activePage = "BookNow";
            BookNow uc = new BookNow();
            InvalidateAllNavButtons();
            uc.Dock = DockStyle.None;
            uc.Width = pnlMain.ClientSize.Width;
            uc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uc.Location = new Point(0, 0);
            BeginPanelTransition(uc, autoScroll: true);
        }

        private void BeginPanelTransition(Control incoming, bool autoScroll, Action? finalizer = null)
        {
            _navTransitionTimer.Stop();

            _transitionOutgoing = pnlMain.Controls
                .Cast<Control>()
                .FirstOrDefault(c => c != _navTransitionOverlay && c.Visible);

            _transitionIncoming = incoming;
            _transitionFinalize = finalizer;
            _transitionProgress = 0f;
            _isTransitioning = true;

            pnlMain.SuspendLayout();
            pnlMain.AutoScroll = false;
            pnlMain.AutoScrollMinSize = Size.Empty;

            if (_transitionIncoming.Parent != pnlMain)
                pnlMain.Controls.Add(_transitionIncoming);

            // Position incoming off-screen (far left — will snap to 0 at midpoint)
            _transitionIncoming.Visible = true;
            _transitionIncoming.Left = -pnlMain.ClientSize.Width; // hidden off-screen until swap
            _transitionIncoming.Top = 0;
            _transitionIncoming.Width = pnlMain.ClientSize.Width;
            _transitionIncoming.Height = Math.Max(_transitionIncoming.Height, pnlMain.ClientSize.Height);
            _transitionIncoming.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Show the overlay immediately — it fades IN over old content first
            _navTransitionOverlay.Progress = 0f;
            _navTransitionOverlay.Visible = true;
            _navTransitionOverlay.BringToFront();

            pnlMain.ResumeLayout();

            _transitionIncoming.Tag = autoScroll;
            _navTransitionTimer.Start();
        }

        private void NavTransitionTimer_Tick(object sender, EventArgs e)
        {
            _transitionProgress += _navTransitionTimer.Interval / (float)TransitionDurationMs;
            float t = Math.Min(1f, _transitionProgress);

            // Phase 1 (0→0.5): fade overlay IN over old content, then swap
            // Phase 2 (0.5→1): fade overlay OUT revealing new content
            _navTransitionOverlay.Progress = t;
            _navTransitionOverlay.Invalidate();

            // At midpoint: swap the panels (content switch happens under the dark overlay)
            if (t >= 0.5f && _transitionIncoming != null && _transitionIncoming.Left != 0)
            {
                pnlMain.SuspendLayout();

                // Remove outgoing panel
                foreach (Control control in pnlMain.Controls.Cast<Control>().ToList())
                {
                    if (control == _navTransitionOverlay) continue;
                    if (control != _transitionIncoming)
                        pnlMain.Controls.Remove(control);
                }

                // Snap incoming to final position immediately
                _transitionIncoming.Left = 0;
                _transitionIncoming.Top = 0;
                _transitionIncoming.Width = pnlMain.ClientSize.Width;
                _transitionIncoming.Height = Math.Max(_transitionIncoming.Height, pnlMain.ClientSize.Height);
                _navTransitionOverlay.BringToFront();

                pnlMain.ResumeLayout();
            }

            if (t < 1f) return;

            // Done — clean up overlay
            _navTransitionTimer.Stop();

            bool autoScroll = _transitionIncoming?.Tag is bool shouldScroll && shouldScroll;

            pnlMain.SuspendLayout();

            // Final cleanup: remove anything that isn't overlay or incoming
            foreach (Control control in pnlMain.Controls.Cast<Control>().ToList())
            {
                if (control == _navTransitionOverlay) continue;
                if (control != _transitionIncoming)
                    pnlMain.Controls.Remove(control);
            }

            if (_transitionIncoming != null)
            {
                _transitionIncoming.Left = 0;
                _transitionIncoming.Top = 0;
                _transitionIncoming.Width = pnlMain.ClientSize.Width;
                _transitionIncoming.Height = Math.Max(_transitionIncoming.Height, pnlMain.ClientSize.Height);
                _transitionIncoming.Tag = null;
            }

            pnlMain.AutoScroll = autoScroll;
            AttachScrollableContent(_transitionIncoming, autoScroll);
            _navTransitionOverlay.Visible = false;
            pnlMain.ResumeLayout();

            _transitionFinalize?.Invoke();
            _transitionIncoming = null;
            _transitionOutgoing = null;
            _transitionFinalize = null;
            _isTransitioning = false;
        }

        private void AttachScrollableContent(Control? content, bool autoScroll)
        {
            if (_activeScrollableContent != null && _activeScrollableContentSizeChanged != null)
                _activeScrollableContent.SizeChanged -= _activeScrollableContentSizeChanged;

            _activeScrollableContent = null;
            _activeScrollableContentSizeChanged = null;

            if (!autoScroll || content == null)
            {
                pnlMain.AutoScrollPosition = new Point(0, 0);
                pnlMain.AutoScrollMinSize = Size.Empty;
                return;
            }

            _activeScrollableContent = content;
            _activeScrollableContentSizeChanged = (s, e) => SyncScrollableContentBounds();
            _activeScrollableContent.SizeChanged += _activeScrollableContentSizeChanged;
            pnlMain.AutoScrollPosition = new Point(0, 0);
            SyncScrollableContentBounds();
        }

        private void SyncScrollableContentBounds()
        {
            if (_activeScrollableContent == null)
                return;

            _activeScrollableContent.Left = 0;
            _activeScrollableContent.Top = 0;
            _activeScrollableContent.Width = pnlMain.ClientSize.Width;
            pnlMain.AutoScrollMinSize = new Size(0, Math.Max(_activeScrollableContent.Bottom + 8, _activeScrollableContent.Height + 8));
        }

        private static float EaseOutCubic(float t)
        {
            float inv = 1f - t;
            return 1f - inv * inv * inv;
        }

        private void btnMyAccommodations_Click(object sender, EventArgs e)
        {
            this.Hide();
            MyAccomodation accommodationForm = new MyAccomodation();
            accommodationForm.Owner = this;
            accommodationForm.WindowState = FormWindowState.Maximized;
            accommodationForm.FormClosed += (s, args) => this.Show();
            accommodationForm.Show();
        }

      
    }
}
