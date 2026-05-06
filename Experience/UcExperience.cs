using System;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Project
{
    public partial class UcExperience : UserControl
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // ═══════════════════════════════════════════════
        //  EXPERIENCE DATA
        // ═══════════════════════════════════════════════
        private class ExpInfo
        {
            public string Name = "", Category = "", Zone = "", Desc = "", Icon = "", Price = "", Status = "", Duration = "", MaxCap = "", Slots = "", Cancel = "";
            public Color BgTop, BgBot;
            public string[] Includes = Array.Empty<string>(), Tags = Array.Empty<string>();
        }

        private readonly ExpInfo[] _exps = new ExpInfo[]
        {
            new ExpInfo {
                Name="Animal Feeding Session", Category="Wildlife Encounter", Zone="Savanna Zone",
                Icon="🍖", Price="₱500", Status="Available", Duration="45 min", MaxCap="10", Slots="7 slots", Cancel="Free cancellation",
                Desc="Hand-feed WildNest's gentle Savanna Zone herbivores under the supervision of a certified wildlife ranger. Learn each animal's dietary needs and approach grazing animals habituated for safe guest interaction.",
                BgTop=Color.FromArgb(72,50,8), BgBot=Color.FromArgb(38,25,5),
                Includes=new[]{"🥩 Feeding Tools","🎙️ Ranger Guide","📸 Photo Time","🦒 Animal ID Cards","🌿 Take-home Guide"},
                Tags=new[]{"Hands-on","Savanna","Ranger-led","Family"}
            },
            new ExpInfo {
                Name="Night Safari Tour", Category="Night Experience", Zone="Full Resort",
                Icon="🌙", Price="₱800", Status="Limited", Duration="2 hours", MaxCap="15", Slots="3 slots", Cancel="Free cancellation",
                Desc="A guided nocturnal adventure through WildNest's eight zones after dark. Witness animals in true nighttime behaviour — crepuscular predators, bats over the Aquatic Zone, and glowing eyes along the dark trail.",
                BgTop=Color.FromArgb(10,10,42), BgBot=Color.FromArgb(5,5,28),
                Includes=new[]{"🔦 Torchlight Kit","🎙️ Night Ranger","🌌 Night-vision Use","🔭 Star Map","☕ Hot Drink"},
                Tags=new[]{"Nocturnal","2 hours","After dark","Nature"}
            },
            new ExpInfo {
                Name="Keeper Experience", Category="Behind the Scenes", Zone="Keeper's Corner",
                Icon="🧑‍🌾", Price="₱1,200", Status="Available", Duration="3 hours", MaxCap="6", Slots="4 slots", Cancel="Free cancellation",
                Desc="Spend a morning in the shoes of a WildNest keeper. Prepare animal diets, assist in morning health checks, and contribute to daily enrichment activities. Maximum 6 guests for an intimate behind-the-scenes experience.",
                BgTop=Color.FromArgb(26,61,24), BgBot=Color.FromArgb(12,38,10),
                Includes=new[]{"👕 Keeper Uniform","🎙️ Keeper Mentor","🍽️ Diet Prep Session","💉 Vet Observation","📋 Certificate"},
                Tags=new[]{"Behind scenes","3 hours","Exclusive","Small group"}
            },
            new ExpInfo {
                Name="Wildlife Photo Opportunity", Category="Photography", Zone="Aviary Dome",
                Icon="📸", Price="₱3,500", Status="Available", Duration="30 min", MaxCap="12", Slots="10 slots", Cancel="Free cancellation",
                Desc="A professional-grade photography session with WildNest's ambassador animals and the full Aviary Dome as your studio. Ambassador handlers position trained birds and rescued animals for up-close portrait shots.",
                BgTop=Color.FromArgb(26,10,42), BgBot=Color.FromArgb(14,5,28),
                Includes=new[]{"📸 Photo Assistant","🦜 Ambassador Animals","📱 Digital Photo Pack","🎙️ Handler Commentary","🪶 Feather Souvenir"},
                Tags=new[]{"Photography","30 min","Ambassador animals","Aviary"}
            },
        };

        private readonly ExpInfo _signature = new ExpInfo
        {
            Name = "Grand Safari Circuit",
            Category = "Grand Tour",
            Zone = "Full Resort — All 8 Zones",
            Icon = "🗺️",
            Price = "₱1,800",
            Status = "Available",
            Duration = "4 hours",
            MaxCap = "12",
            Slots = "8 slots",
            Cancel = "Free cancellation",
            Desc = "The ultimate WildNest experience — a fully guided four-hour expedition through all eight wildlife zones aboard our custom open-top Electric Safari Tram. Expert naturalist narrates every zone, provides live animal health updates, and stops for hands-on encounters at three interaction points. Ends with a sunset viewing platform overlooking the full savanna.",
            BgTop = Color.FromArgb(26, 10, 61),
            BgBot = Color.FromArgb(10, 5, 38),
            Includes = new[] { "🚌 Electric Tram", "🎙️ Expert Guide", "📋 Conservation Cert", "📸 Photo Stops", "🦁 3 Encounters", "☕ Welcome Drink" },
            Tags = new[] { "All zones", "4 hours", "Tram", "Signature" }
        };

        private string _activeFilter = "All";
        private readonly Panel[] _expPanels = new Panel[4];

        private static readonly Color C_BG = Color.FromArgb(240, 237, 232);
        private static readonly Color C_DARK = Color.FromArgb(7, 26, 14);
        private static readonly Color C_GOLD = Color.FromArgb(212, 160, 23);
        private static readonly Color C_GREEN = Color.FromArgb(27, 67, 50);
        private static readonly Color C_CREAM = Color.FromArgb(248, 244, 239);

        // Extra luxury palette
        private static readonly Color C_GOLD_LIGHT = Color.FromArgb(245, 200, 80);
        private static readonly Color C_GOLD_DIM = Color.FromArgb(140, 100, 10);
        private static readonly Color C_CARD_BG = Color.FromArgb(12, 30, 18);
        private static readonly Color C_CARD_MID = Color.FromArgb(18, 44, 26);
        private static readonly Color C_CARD_BORDER = Color.FromArgb(60, 212, 160, 23);

        private string _breadcrumb = "Viewing: All Experiences  —  5 Experiences";

        // ═══════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════
        public UcExperience()
        {
            InitializeComponent();
            // In constructor
            this.AutoScroll = false;
            this.Dock = DockStyle.Fill;
            this.BackColor = C_BG;
            this.DoubleBuffered = true;
            BuildUI();
            SetupNewUI();
        }

        // ═══════════════════════════════════════════════
        //  BuildUI
        // ═══════════════════════════════════════════════
        private void BuildUI()
        {
            pnlHero.Paint += PaintHeroBackground;

            lblLocation.Font = new Font("Segoe UI", 9f);
            lblLocation.ForeColor = C_GOLD;
            lblLocation.BackColor = Color.Transparent;

            lblHeroTitle.Font = new Font("Georgia", 40f, FontStyle.Bold);
            lblHeroTitle.ForeColor = C_CREAM;
            lblHeroTitle.BackColor = Color.Transparent;
            lblHeroTitle.Text = "";
            lblHeroTitle.AutoSize = false;
            lblHeroTitle.Size = new Size(920, 88);
            lblHeroTitle.Paint -= PaintExperienceHeroTitle;
            lblHeroTitle.Paint += PaintExperienceHeroTitle;

            lblHeroSub.Font = new Font("Segoe UI", 11f);
            lblHeroSub.ForeColor = Color.FromArgb(160, 248, 244, 239);
            lblHeroSub.BackColor = Color.Transparent;

            pnlChecker.Size = new Size(780, 90);

            Action layoutHero = () =>
            {
                int cx = pnlHero.Width / 2;
                lblLocation.Location = new Point(cx - lblLocation.PreferredWidth / 2, 120);
                lblHeroTitle.Location = new Point(cx - lblHeroTitle.Width / 2, 156);
                lblHeroSub.Location = new Point(cx - lblHeroSub.PreferredWidth / 2, 270);
                pnlChecker.Location = new Point(cx - pnlChecker.Width / 2, 330);
            };
            pnlHero.HandleCreated += (s, e) => layoutHero();
            pnlHero.Resize += (s, e) => layoutHero();

            pnlChecker.BackColor = Color.Transparent;
            pnlChecker.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = pnlChecker.ClientRectangle;
                rect.Inflate(-1, -1);
                int r = 20;
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, r, r, 180, 90);
                path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
                path.CloseFigure();
                using (var fill = new SolidBrush(Color.FromArgb(25, 52, 32)))
                    pe.Graphics.FillPath(fill, path);
                using (var border = new Pen(C_GOLD, 1.5f))
                    pe.Graphics.DrawPath(border, path);
                int[] divX = { 205, 395, 550 };
                foreach (int x in divX)
                    pe.Graphics.DrawLine(new Pen(Color.FromArgb(60, 255, 255, 255), 1),
                        x, rect.Y + 15, x, rect.Bottom - 15);
            };

            lblCheckIn.Text = "DATE"; lblCheckIn.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblCheckIn.ForeColor = C_GOLD; lblCheckIn.BackColor = Color.Transparent;
            lblCheckIn.Location = new Point(30, 12);

            lblCheckOut.Text = "END DATE"; lblCheckOut.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblCheckOut.ForeColor = C_GOLD; lblCheckOut.BackColor = Color.Transparent;
            lblCheckOut.Location = new Point(218, 12);

            lblGuests.Text = "GUESTS"; lblGuests.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            lblGuests.ForeColor = C_GOLD; lblGuests.BackColor = Color.Transparent;
            lblGuests.Location = new Point(404, 12);

            dtpCheckIn.Location = new Point(30, 36);
            dtpCheckOut.Location = new Point(218, 36);
            cmbGuests.Location = new Point(404, 36);

            StyleInput(dtpCheckIn);
            StyleInput(dtpCheckOut);
            StyleInput(cmbGuests);

            btnCheckAvailability.Size = new Size(160, 44);
            btnCheckAvailability.Location = new Point(560, 23);
            btnCheckAvailability.FlatStyle = FlatStyle.Flat;
            btnCheckAvailability.BackColor = Color.Transparent;
            btnCheckAvailability.ForeColor = Color.Transparent;
            btnCheckAvailability.FlatAppearance.BorderSize = 0;
            btnCheckAvailability.Cursor = Cursors.Hand;
            btnCheckAvailability.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                pe.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                Rectangle rect = btnCheckAvailability.ClientRectangle;
                rect.Inflate(-1, -1);
                int rr = rect.Height;
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, rr, rr, 90, 180);
                path.AddArc(rect.Right - rr, rect.Y, rr, rr, 270, 180);
                path.CloseFigure();
                using (var fill = new SolidBrush(C_GOLD))
                    pe.Graphics.FillPath(fill, path);
                TextRenderer.DrawText(pe.Graphics, "Check Availability",
                    new Font("Segoe UI", 10f, FontStyle.Bold),
                    btnCheckAvailability.ClientRectangle,
                    Color.FromArgb(20, 45, 28),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        // ═══════════════════════════════════════════════
        //  SetupNewUI
        // ═══════════════════════════════════════════════
        private Panel _pnlGrid;
        private Panel _pnlFooter;

        private void SetupNewUI()
        {
            pnlSideIcons.Dock = DockStyle.Left;
            pnlSideIcons.Width = 118;
            pnlSideIcons.BackColor = C_DARK;

            pnlMain.Dock = DockStyle.Top; 
            pnlMain.Height = 860;
            pnlMain.BackColor = C_BG;
            pnlMain.AutoScroll = false;

            pnlSignature.Dock = DockStyle.Top;
            pnlSignature.Height = 260;
            pnlSignature.BackColor = C_BG;
            pnlSignature.Padding = new Padding(0);

            picSignature.Dock = DockStyle.Fill;
            picSignature.BackColor = Color.Transparent;
            picSignature.Cursor = Cursors.Hand;
            picSignature.Paint += PaintSignature;
            picSignature.Click += (s, e) => ShowDetail(_signature);

            _pnlGrid = new Panel { BackColor = C_BG, Location = new Point(0, 268), AutoSize = false };
            flpCabins.Visible = false;
            pnlMain.Controls.Add(_pnlGrid);

            _pnlFooter = new Panel
            {
                BackColor = C_DARK,
                Height = 52,
                Location = new Point(0, 600)
            };
            _pnlFooter.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                using (var pen = new Pen(Color.FromArgb(60, C_GOLD), 1f))
                    g.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);

                using (var br = new SolidBrush(Color.FromArgb(80, C_GOLD)))
                    g.FillEllipse(br, 28, _pnlFooter.Height / 2 - 3, 6, 6);

                using (var br = new SolidBrush(Color.FromArgb(80, C_GOLD)))
                    g.FillEllipse(br, _pnlFooter.Width - 34, _pnlFooter.Height / 2 - 3, 6, 6);

                TextRenderer.DrawText(g,
                    "✦  You've explored all our experiences  ✦",
                    new Font("Georgia", 9.5f, FontStyle.Italic),
                    _pnlFooter.ClientRectangle,
                    Color.FromArgb(120, C_GOLD),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlMain.Controls.Add(_pnlFooter);

            pnlMain.Resize += (s, e) => DoGridLayout(_activeFilter);

            pnlStatusBar.BackColor = Color.FromArgb(7, 26, 14);
            pnlStatusBar.Controls.Clear();
            pnlStatusBar.Paint += PaintStatusBar;

            Panel[] panels = { pnlExp1, pnlExp2, pnlExp3, pnlExp4 };
            for (int i = 0; i < 4; i++)
            {
                _expPanels[i] = panels[i];
                flpCabins.Controls.Remove(panels[i]);
                _pnlGrid.Controls.Add(panels[i]);
                SetupExpPanel(panels[i], _exps[i]);
            }

            SetupFilterButtons();
            UpdateBreadcrumb("All", 4);

            pnlMain.HandleCreated += (s, e) => DoGridLayout("All");
        }

        private void DoGridLayout(string filter)
        {
            if (pnlMain.Width <= 0) return;
            const int pad = 16, gap = 14, cardH = 280;
            int totalW = pnlMain.Width - pad * 2;
            int cardW = (totalW - gap) / 2;
            if (cardW < 180) cardW = 180;

            bool sigVisible = filter == "All" || filter == "Grand Tour";
            pnlSignature.Visible = sigVisible;
            _pnlGrid.Location = new Point(0, sigVisible ? pnlSignature.Height + 8 : 0);
            _pnlGrid.Width = pnlMain.Width;

            int visIdx = 0;
            for (int i = 0; i < 4; i++)
            {
                bool show = filter == "All" || _exps[i].Category == filter;
                _expPanels[i].Visible = show;
                if (show)
                {
                    int col = visIdx % 2, row = visIdx / 2;
                    _expPanels[i].Size = new Size(cardW, cardH);
                    _expPanels[i].Location = new Point(pad + col * (cardW + gap), pad + row * (cardH + gap));
                    visIdx++;
                }
            }
            int rows = (visIdx + 1) / 2;
            int gridH = pad * 2 + rows * (cardH + gap) - gap + 20;
            _pnlGrid.Height = gridH;

            int footerY = _pnlGrid.Location.Y + gridH + 6;
            _pnlFooter.Location = new Point(0, footerY);
            _pnlFooter.Width = pnlMain.Width;
            _pnlFooter.Height = 52;

            int contentH = _pnlFooter.Location.Y + _pnlFooter.Height + 18;
            pnlMain.Height = contentH;
            int bodyH = pnlMain.Height;
            pnlSideIcons.Height = bodyH;
            this.Height = pnlHero.Height + pnlStatusBar.Height + bodyH;
            this.MinimumSize = new Size(0, this.Height);
            this.AutoScrollMinSize = Size.Empty;
        }

        // ═══════════════════════════════════════════════
        //  STATUS BAR
        // ═══════════════════════════════════════════════
        private void PaintStatusBar(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var p = (Panel)sender;

            using (var pen = new Pen(Color.FromArgb(100, C_GOLD), 1.5f))
                g.DrawLine(pen, 0, 0, p.Width, 0);

            using (var br = new SolidBrush(C_GOLD))
                g.FillEllipse(br, 18, p.Height / 2 - 4, 7, 7);

            string[] parts = _breadcrumb.Split(new[] { "  —  " }, StringSplitOptions.None);
            int startX = 36;
            if (parts.Length == 2)
            {
                var f1 = new Font("Segoe UI", 10f);
                var f2 = new Font("Georgia", 11f, FontStyle.Italic);
                var sep = "  —  ";
                var sz1 = TextRenderer.MeasureText(g, parts[0] + sep, f1);
                TextRenderer.DrawText(g, parts[0] + sep, f1,
                    new Rectangle(startX, 0, sz1.Width + 10, p.Height),
                    Color.FromArgb(170, C_CREAM),
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                TextRenderer.DrawText(g, parts[1], f2,
                    new Rectangle(startX + sz1.Width, 0, 360, p.Height),
                    C_GOLD,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }

            string[] chips = { "⭐ 4.9 Rating", "✅ 5 Available" };
            int rx = p.Width - 16;
            foreach (var chip in chips)
            {
                var cf = new Font("Segoe UI", 8.5f);
                var csz = TextRenderer.MeasureText(g, chip, cf);
                rx -= csz.Width + 20;
                var cr = new Rectangle(rx, p.Height / 2 - 11, csz.Width + 16, 22);
                using (var path = RoundRect(cr, 11))
                using (var br = new SolidBrush(Color.FromArgb(38, C_GOLD)))
                    g.FillPath(br, path);
                TextRenderer.DrawText(g, chip, cf, cr, C_GOLD,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                rx -= 8;
            }

            using (var pen = new Pen(Color.FromArgb(35, C_GOLD), 1f))
                g.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private void UpdateBreadcrumb(string filter, int count)
        {
            string label = filter == "All" ? "All Experiences" : filter;
            int total = filter == "All" ? count + 1 : count;
            _breadcrumb = $"Viewing: {label}  —  {total} Experience{(total != 1 ? "s" : "")}";
            pnlStatusBar.Invalidate();
        }

        // ═══════════════════════════════════════════════
        //  FILTER BUTTONS
        // ═══════════════════════════════════════════════
        private void SetupFilterButtons()
        {
            var filters = new (Button btn, string emoji, string label, string filter)[]
            {
                (btnFilterAll,       "🌿", "All",       "All"),
                (btnFilterTreehouse, "🍖", "Wildlife",  "Wildlife Encounter"),
                (btnFilterNight,     "🌙", "Night",     "Night Experience"),
                (btnFilterKeeper,    "🧑‍🌾", "Keeper",   "Behind the Scenes"),
                (btnFilterGrand,     "📸", "Photo",     "Photography"),
            };

            int y = 16;
            foreach (var (btn, emoji, label, filter) in filters)
            {
                btn.Location = new Point(11, y);
                btn.Size = new Size(96, 84);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;
                btn.Cursor = Cursors.Hand;
                btn.Text = "";
                btn.Visible = true;

                string capFilter = filter;
                string capEmoji = emoji;
                string capLabel = label;
                Button capBtn = btn;

                capBtn.Paint += (s, pe) =>
                    PaintFilterBtn(pe, capBtn, capEmoji, capLabel, capFilter == _activeFilter);

                capBtn.Click += (s, e) =>
                {
                    _activeFilter = capFilter;
                    int cnt = capFilter == "All" ? 4 : _exps.Count(x => x.Category == capFilter);
                    UpdateBreadcrumb(capFilter, cnt);
                    DoGridLayout(capFilter);
                    foreach (var (b, _, _, _) in filters) b.Invalidate();
                };
                capBtn.MouseEnter += (s, e) => capBtn.Invalidate();
                capBtn.MouseLeave += (s, e) => capBtn.Invalidate();

                y += 90;
            }
        }

        private void PaintFilterBtn(PaintEventArgs e, Button btn, string emoji, string label, bool active)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var r = btn.ClientRectangle;
            bool hovered = r.Contains(btn.PointToClient(Cursor.Position));

            var shellRect = new Rectangle(r.X + 5, r.Y + 4, r.Width - 10, r.Height - 8);
            using (var shellPath = RoundRect(shellRect, 16))
            using (var shellBrush = new SolidBrush(active
                       ? Color.FromArgb(42, C_GOLD)
                       : hovered ? Color.FromArgb(26, 255, 255, 255)
                       : Color.FromArgb(12, 255, 255, 255)))
            {
                g.FillPath(shellBrush, shellPath);
            }

            if (active)
            {
                using (var path = RoundRect(shellRect, 16))
                using (var br = new LinearGradientBrush(shellRect, Color.FromArgb(64, C_GOLD), Color.FromArgb(18, C_GOLD), 90f))
                    g.FillPath(br, path);
                using (var path = RoundRect(shellRect, 16))
                using (var pen = new Pen(Color.FromArgb(170, C_GOLD), 1.5f))
                    g.DrawPath(pen, path);
                var activeBar = new Rectangle(shellRect.X - 1, shellRect.Y + 14, 4, shellRect.Height - 28);
                using (var path = RoundRect(activeBar, 2))
                using (var br = new SolidBrush(C_GOLD))
                    g.FillPath(br, path);
            }
            else if (hovered)
            {
                using (var path = RoundRect(shellRect, 16))
                using (var pen = new Pen(Color.FromArgb(38, C_CREAM), 1f))
                    g.DrawPath(pen, path);
            }

            var orbRect = new Rectangle(shellRect.X + 12, shellRect.Y + 7, shellRect.Width - 24, 32);
            using (var orbPath = RoundRect(orbRect, 14))
            using (var orbBrush = new LinearGradientBrush(
                        orbRect,
                        active ? Color.FromArgb(54, C_GOLD) : Color.FromArgb(24, 255, 255, 255),
                        active ? Color.FromArgb(18, C_GOLD) : Color.FromArgb(10, 255, 255, 255),
                       90f))
            {
                g.FillPath(orbBrush, orbPath);
            }
            if (hovered || active)
            {
                using (var orbPath = RoundRect(orbRect, 14))
                using (var pen = new Pen(active ? Color.FromArgb(140, C_GOLD_LIGHT) : Color.FromArgb(55, C_CREAM), 1f))
                    g.DrawPath(pen, orbPath);
            }

            using (var emojiBrush = new SolidBrush(active ? C_GOLD : Color.FromArgb(150, C_CREAM)))
            using (var emojiFont = new Font("Segoe UI Emoji", 15.2f))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(emoji, emojiFont, emojiBrush,
                    new RectangleF(r.X + 4, r.Y + 8, r.Width - 8, 30), sf);

            TextRenderer.DrawText(g, label,
                new Font("Segoe UI Semibold", active ? 8.0f : 7.7f, FontStyle.Regular),
                new Rectangle(r.X + 9, r.Y + 44, r.Width - 18, 30),
                active ? C_GOLD : Color.FromArgb(170, C_CREAM),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        // ═══════════════════════════════════════════════
        //  EXP CARD  — LUXURY DARK REDESIGN
        // ═══════════════════════════════════════════════
        private void SetupExpPanel(Panel pnl, ExpInfo exp)
        {
            pnl.Controls.Clear();
            pnl.Size = new Size(320, 280);
            pnl.Margin = new Padding(0);
            pnl.BackColor = Color.Transparent;
            pnl.Cursor = Cursors.Hand;

            var pic = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            pic.Paint += (s, e) => PaintExpCard(e, pnl, exp);
            pic.Click += (s, e) => ShowDetail(exp);
            pic.MouseEnter += (s, e) => pnl.Invalidate(true);
            pic.MouseLeave += (s, e) => pnl.Invalidate(true);
            pnl.Controls.Add(pic);
        }

        private void PaintExpCard(PaintEventArgs e, Panel pnl, ExpInfo exp)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool hovered = pnl.RectangleToScreen(pnl.ClientRectangle).Contains(Cursor.Position);

            // ── shadow
            for (int sh = 6; sh >= 1; sh--)
            {
                int alpha = hovered ? (sh * 22) : (sh * 12);
                var sr = new Rectangle(4 + sh, 4 + sh, pnl.Width - 8, pnl.Height - 8);
                using (var path = RoundRect(sr, 16))
                using (var br = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0)))
                    g.FillPath(br, path);
            }

            var r = new Rectangle(3, 3, pnl.Width - 7, pnl.Height - 7);

            // ── card body — dark luxury
            using (var path = RoundRect(r, 16))
            {
                using (var br = new LinearGradientBrush(r, C_CARD_BG, C_CARD_MID, 135f))
                    g.FillPath(br, path);
            }

            // ── gold border (brighter on hover)
            int borderAlpha = hovered ? 180 : 80;
            using (var path = RoundRect(r, 16))
            using (var pen = new Pen(Color.FromArgb(borderAlpha, C_GOLD), hovered ? 1.8f : 1.2f))
                g.DrawPath(pen, path);

            // ── image / hero section
            int imgH = 130;
            var imgRect = new Rectangle(r.X, r.Y, r.Width, imgH);

            using (var imgPath = RoundRectTop(imgRect, 16))
            using (var br = new LinearGradientBrush(imgRect, exp.BgTop, exp.BgBot, 145f))
                g.FillPath(br, imgPath);

            // subtle gold shimmer line across top of image
            using (var shimmer = new LinearGradientBrush(
                new Rectangle(imgRect.X, imgRect.Y, imgRect.Width, 2),
                Color.Transparent, Color.FromArgb(100, C_GOLD_LIGHT), 0f))
            using (var pen = new Pen(shimmer, 1.5f))
                g.DrawLine(pen, imgRect.X, imgRect.Y + 1, imgRect.Right, imgRect.Y + 1);

            // ── icon centered in image
            TextRenderer.DrawText(g, exp.Icon, new Font("Segoe UI Emoji", 44f),
                new Rectangle(imgRect.X, imgRect.Y, imgRect.Width, imgH),
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // gradient fade at bottom of image into card
            var fadeRect = new Rectangle(imgRect.X, imgRect.Bottom - 40, imgRect.Width, 40);
            using (var fade = new LinearGradientBrush(fadeRect, Color.Transparent, C_CARD_BG, 90f))
                g.FillRectangle(fade, fadeRect);

            // ── STATUS badge
            Color bdgBg = exp.Status == "Available" ? Color.FromArgb(200, 14, 80, 52)
                        : exp.Status == "Limited" ? Color.FromArgb(200, 120, 60, 0)
                                                     : Color.FromArgb(200, 120, 20, 20);
            Color bdgFg = exp.Status == "Available" ? Color.FromArgb(168, 240, 216)
                        : exp.Status == "Limited" ? Color.FromArgb(255, 210, 110)
                                                     : Color.FromArgb(255, 160, 160);
            var bdgRect = new Rectangle(imgRect.X + 10, imgRect.Y + 10, 82, 22);
            using (var path = RoundRect(bdgRect, 11))
            using (var br = new SolidBrush(bdgBg)) g.FillPath(br, path);
            using (var path = RoundRect(bdgRect, 11))
            using (var pen = new Pen(Color.FromArgb(60, bdgFg), 1f)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, "● " + exp.Status, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                bdgRect, bdgFg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // ── SLOTS badge top-right
            Color slotCol = exp.Status == "Limited" ? Color.FromArgb(255, 220, 130) : Color.FromArgb(200, C_GOLD);
            var slotsRect = new Rectangle(r.Right - 72, imgRect.Y + 10, 64, 22);
            using (var path = RoundRect(slotsRect, 11))
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0))) g.FillPath(br, path);
            using (var path = RoundRect(slotsRect, 11))
            using (var pen = new Pen(Color.FromArgb(50, C_GOLD), 1f)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, exp.Slots, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                slotsRect, slotCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // ── ZONE label inside image bottom
            TextRenderer.DrawText(g, exp.Zone.ToUpper(), new Font("Segoe UI", 6.5f, FontStyle.Bold),
                new Rectangle(imgRect.X + 10, imgRect.Bottom - 20, imgRect.Width - 20, 18),
                Color.FromArgb(160, C_GOLD), TextFormatFlags.Left);

            // ── BODY section
            int by = r.Y + imgH + 10;
            int bx = r.X + 14;
            int bw = r.Width - 28;

            // category · duration
            TextRenderer.DrawText(g, $"{exp.Category.ToUpper()}  ·  {exp.Duration}",
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new Rectangle(bx, by, bw, 16),
                C_GOLD,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            by += 19;

            // Name
            TextRenderer.DrawText(g, exp.Name, new Font("Georgia", 13f, FontStyle.Bold),
                new Rectangle(bx, by, bw, 24),
                C_CREAM,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            by += 26;

            // Max guests · zone
            string infoLine = $"Max {exp.MaxCap} guests  ·  {exp.Zone}";
            TextRenderer.DrawText(g, infoLine, new Font("Segoe UI", 7.5f),
                new Rectangle(bx, by, bw, 16),
                Color.FromArgb(130, C_CREAM),
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            by += 18;

            // thin gold divider
            using (var pen = new Pen(Color.FromArgb(50, C_GOLD), 1f))
                g.DrawLine(pen, bx, by, r.Right - 14, by);
            by += 9;

            var bRect = new Rectangle(r.Right - 112, by, 100, 30);
            int priceW = Math.Max(92, bRect.X - bx - 8);

            // Price
            TextRenderer.DrawText(g, exp.Price, new Font("Georgia", 14f, FontStyle.Bold),
                new Rectangle(bx, by, priceW, 24),
                C_GOLD_LIGHT,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, "/person", new Font("Segoe UI", 7f),
                new Rectangle(bx, by + 18, priceW, 14),
                Color.FromArgb(90, C_CREAM),
                TextFormatFlags.Left);

            // Book Now button
            using (var path = RoundRect(bRect, 15))
            using (var br = new LinearGradientBrush(bRect, C_GOLD, Color.FromArgb(190, 140, 10), 90f))
                g.FillPath(br, path);
            // subtle inner glow on button
            var bInner = new Rectangle(bRect.X + 2, bRect.Y + 1, bRect.Width - 4, bRect.Height / 2);
            using (var path = RoundRectTop(bInner, 13))
            using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 200)))
                g.FillPath(br, path);
            TextRenderer.DrawText(g, "Book Now", new Font("Segoe UI", 8.5f, FontStyle.Bold),
                bRect, Color.FromArgb(15, 35, 12),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void PaintExperienceHeroTitle(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using var font = new Font("Georgia", 46f, FontStyle.Bold);
            using var creamBrush = new SolidBrush(C_CREAM);
            using var goldBrush = new SolidBrush(C_GOLD);

            string left = "Wildlife";
            string right = "Experiences";

            SizeF leftSize = g.MeasureString(left, font, 1000, StringFormat.GenericTypographic);
            SizeF rightSize = g.MeasureString(right, font, 1000, StringFormat.GenericTypographic);
            float total = leftSize.Width + rightSize.Width;
            float x = (lblHeroTitle.Width - total) / 2f;
            float y = (lblHeroTitle.Height - leftSize.Height) / 2f - 2f;

            g.DrawString(left, font, creamBrush, x, y, StringFormat.GenericTypographic);
            g.DrawString(right, font, goldBrush, x + leftSize.Width + 16f, y, StringFormat.GenericTypographic);
        }

        // ═══════════════════════════════════════════════
        //  SIGNATURE PAINT — LUXURY REDESIGN
        // ═══════════════════════════════════════════════
        private void PaintSignature(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var pic = (PictureBox)sender;

            // outer card bounds with safe margin
            var r = new Rectangle(6, 6, pic.Width - 12, pic.Height - 12);

            // shadow
            for (int sh = 5; sh >= 1; sh--)
            {
                var sr = new Rectangle(r.X + sh, r.Y + sh, r.Width, r.Height);
                using (var path = RoundRect(sr, 18))
                using (var br = new SolidBrush(Color.FromArgb(sh * 14, 0, 0, 0)))
                    g.FillPath(br, path);
            }

            // card background
            using (var path = RoundRect(r, 18))
            using (var br = new LinearGradientBrush(r, Color.FromArgb(22, 8, 58), Color.FromArgb(8, 4, 32), 140f))
                g.FillPath(br, path);

            // gold border
            using (var path = RoundRect(r, 18))
            using (var pen = new Pen(Color.FromArgb(110, C_GOLD), 1.4f))
                g.DrawPath(pen, path);

            // ── LEFT image panel (40% width)
            int leftW = (int)(r.Width * 0.40f);
            var leftRect = new Rectangle(r.X, r.Y, leftW, r.Height);

            using (var lPath = RoundRectLeft(leftRect, 18))
            using (var br = new LinearGradientBrush(leftRect,
                Color.FromArgb(38, 16, 90), Color.FromArgb(18, 8, 54), 130f))
                g.FillPath(br, lPath);

            // vertical gold divider line
            using (var pen = new Pen(Color.FromArgb(70, C_GOLD), 1f))
                g.DrawLine(pen, leftRect.Right, r.Y + 20, leftRect.Right, r.Bottom - 20);

            // glow orb behind icon
            var gc = new PointF(leftRect.X + leftW / 2f, leftRect.Y + leftRect.Height / 2f);
            using (var glowBr = new PathGradientBrush(new[] {
                new PointF(gc.X - 90, gc.Y), new PointF(gc.X, gc.Y - 90),
                new PointF(gc.X + 90, gc.Y), new PointF(gc.X, gc.Y + 90) }))
            {
                glowBr.CenterColor = Color.FromArgb(55, C_GOLD);
                glowBr.SurroundColors = new[] { Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent };
                g.FillEllipse(glowBr, gc.X - 90, gc.Y - 90, 180, 180);
            }

            // icon
            TextRenderer.DrawText(g, _signature.Icon, new Font("Segoe UI Emoji", 52f),
                leftRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Available badge — top-left of left panel
            var av = new Rectangle(leftRect.X + 12, leftRect.Y + 12, 88, 22);
            using (var path = RoundRect(av, 11))
            using (var br = new SolidBrush(Color.FromArgb(200, 14, 80, 52))) g.FillPath(br, path);
            using (var path = RoundRect(av, 11))
            using (var pen = new Pen(Color.FromArgb(60, Color.FromArgb(168, 240, 216)), 1f)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, "● Available", new Font("Segoe UI", 7.5f, FontStyle.Bold),
                av, Color.FromArgb(168, 240, 216), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Star badge — top-right of left panel
            var st = new Rectangle(leftRect.Right - 58, leftRect.Y + 12, 50, 22);
            using (var path = RoundRect(st, 11))
            using (var br = new SolidBrush(Color.FromArgb(60, C_GOLD))) g.FillPath(br, path);
            using (var path = RoundRect(st, 11))
            using (var pen = new Pen(Color.FromArgb(120, C_GOLD), 1f)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, "★ 5.0", new Font("Segoe UI", 7.5f, FontStyle.Bold),
                st, C_GOLD, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // ── RIGHT info panel
            // reserve safe padding so nothing clips
            int rx = r.X + leftW + 22;
            int infoW = r.Right - rx - 18; // keeps text from touching border
            int iy = r.Y + 18;

            // Category · Zone — single line, ellipsis-safe
            string catLine = $"{_signature.Category.ToUpper()}  ·  {_signature.Zone.ToUpper()}";
            TextRenderer.DrawText(g, catLine,
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new Rectangle(rx, iy, infoW, 18),
                C_GOLD,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            iy += 22;

            // Name
            TextRenderer.DrawText(g, _signature.Name,
                new Font("Georgia", 16f, FontStyle.Bold),
                new Rectangle(rx, iy, infoW, 32),
                C_CREAM,
                TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            iy += 36;

            // gold accent line
            using (var pen = new Pen(C_GOLD, 1.5f)) g.DrawLine(pen, rx, iy, rx + 44, iy);
            iy += 10;

            // Description — limited height, wrapped
            TextRenderer.DrawText(g, _signature.Desc,
                new Font("Segoe UI", 8f),
                new Rectangle(rx, iy, infoW, 52),
                Color.FromArgb(145, C_CREAM),
                TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
            iy += 58;

            // Includes chips — wrap-aware, never overflow right
            int px = rx;
            int chipRowMax = rx + infoW;
            foreach (var inc in _signature.Includes)
            {
                var af = new Font("Segoe UI", 7.5f);
                var asz = TextRenderer.MeasureText(g, inc, af);
                var pr = new Rectangle(px, iy, asz.Width + 14, 22);
                if (px + pr.Width > chipRowMax) break;
                using (var path = RoundRect(pr, 11))
                {
                    using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255))) g.FillPath(br, path);
                    using (var pen = new Pen(Color.FromArgb(45, C_GOLD), 1f)) g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(g, inc, af, pr, Color.FromArgb(170, C_CREAM),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                px += pr.Width + 5;
            }
            iy += 30;

            // Price
            TextRenderer.DrawText(g, _signature.Price,
                new Font("Georgia", 22f, FontStyle.Bold),
                new Rectangle(rx, iy - 2, 190, 38),
                C_GOLD_LIGHT,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "/person",
                new Font("Segoe UI", 7.5f),
                new Rectangle(rx, iy + 28, 70, 14),
                Color.FromArgb(90, C_CREAM),
                TextFormatFlags.Left);

            // Buttons — anchored to right edge of infoW safely
            int btnRight = r.Right - 18;
            var bookR = new Rectangle(btnRight - 112, iy + 1, 108, 30);
            using (var path = RoundRect(bookR, 15))
            using (var br = new LinearGradientBrush(bookR, C_GOLD, Color.FromArgb(190, 140, 10), 90f))
                g.FillPath(br, path);
            // inner highlight
            var bookInner = new Rectangle(bookR.X + 2, bookR.Y + 1, bookR.Width - 4, bookR.Height / 2);
            using (var path = RoundRectTop(bookInner, 13))
            using (var br = new SolidBrush(Color.FromArgb(40, 255, 255, 200)))
                g.FillPath(br, path);
            TextRenderer.DrawText(g, "Book Now  →",
                new Font("Segoe UI", 8.5f, FontStyle.Bold),
                bookR, Color.FromArgb(15, 35, 12),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            var detR = new Rectangle(bookR.X - 90, iy + 1, 84, 30);
            using (var path = RoundRect(detR, 15))
            using (var br = new SolidBrush(Color.FromArgb(30, 255, 255, 255))) g.FillPath(br, path);
            using (var path = RoundRect(detR, 15))
            using (var pen = new Pen(Color.FromArgb(70, C_GOLD), 1f)) g.DrawPath(pen, path);
            TextRenderer.DrawText(g, "Details",
                new Font("Segoe UI", 8.5f),
                detR, Color.FromArgb(175, C_CREAM),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ═══════════════════════════════════════════════
        //  DETAIL MODAL — LUXURY DARK REDESIGN
        // ═══════════════════════════════════════════════
        private void ShowDetail(ExpInfo exp)
        {
            Form? parentForm = this.FindForm();
            Control overlayParent = parentForm as Control ?? this;

            int mw = Math.Min(780, overlayParent.Width - 60);
            int mh = Math.Min(650, overlayParent.Height - 50);

            // ── dimmed overlay
            

            // ── modal panel
            var modal = new Panel
            {
                Size = new Size(mw, mh),
                BackColor = Color.FromArgb(10, 28, 16),
                Location = new Point((overlayParent.Width - mw) / 2, (overlayParent.Height - mh) / 2)
            };
            using (var path = RoundRect(new Rectangle(0, 0, mw, mh), 20))
                modal.Region = new Region(path);
            overlayParent.Controls.Add(modal);
            modal.BringToFront();

            // ── outer gold border via Paint
            modal.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pe.Graphics.Clear(Color.FromArgb(10, 28, 16));
                using (var fill = new LinearGradientBrush(new Rectangle(0, 0, mw, mh), Color.FromArgb(12, 34, 20), Color.FromArgb(8, 22, 13), 90f))
                    pe.Graphics.FillRectangle(fill, new Rectangle(0, 0, mw, mh));
                var mr = new Rectangle(0, 0, mw - 1, mh - 1);
                // double-line gold border
                using (var path = RoundRect(mr, 20))
                using (var pen = new Pen(Color.FromArgb(180, C_GOLD), 2f))
                    pe.Graphics.DrawPath(pen, path);
                var mr2 = new Rectangle(3, 3, mw - 7, mh - 7);
                using (var path = RoundRect(mr2, 18))
                using (var pen = new Pen(Color.FromArgb(40, C_GOLD), 1f))
                    pe.Graphics.DrawPath(pen, path);
                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                    pe.Graphics.DrawLine(pen, 28, 68, mw - 28, 68);
            };

            void onResize(object s, EventArgs e2)
            {
               
                modal.Location = new Point((overlayParent.Width - mw) / 2, (overlayParent.Height - mh) / 2);
            }
            overlayParent.Resize += onResize;
            modal.Disposed += (s, e) =>
            {
                overlayParent.Resize -= onResize;
                
            };
           

            // ── HERO SECTION
            int heroH = 174;
            var hero = new Panel { Location = Point.Empty, Size = new Size(mw, heroH), BackColor = Color.Transparent };
            hero.Paint += (s, pe) =>
            {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var hr = hero.ClientRectangle;

                // gradient background
                using (var path = RoundRectTop(new Rectangle(0, 0, hr.Width, hr.Height), 20))
                using (var br = new LinearGradientBrush(hr, exp.BgTop, exp.BgBot, 130f))
                    g.FillPath(br, path);

                using (var shimmer = new LinearGradientBrush(
                    new Rectangle(0, 0, hr.Width, 3),
                    Color.Transparent, Color.FromArgb(140, C_GOLD_LIGHT), 0f))
                using (var pen = new Pen(shimmer, 3f))
                    g.DrawLine(pen, 28, 2, hr.Width - 28, 2);

                var crestRect = new Rectangle((hr.Width - 110) / 2, 28, 110, 104);
                using (var crestPath = RoundRect(crestRect, 28))
                using (var crestFill = new SolidBrush(Color.FromArgb(40, 9, 24, 14)))
                using (var crestPen = new Pen(Color.FromArgb(75, C_GOLD), 1f))
                {
                    g.FillPath(crestFill, crestPath);
                    g.DrawPath(crestPen, crestPath);
                }

                var iconRect = new Rectangle(crestRect.X, crestRect.Y + 2, crestRect.Width, crestRect.Height - 2);
                TextRenderer.DrawText(g, exp.Icon, new Font("Segoe UI Emoji", 52f), iconRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // fade to card bg at bottom
                var fadeR = new Rectangle(0, hr.Height - 64, hr.Width, 64);
                using (var fade = new LinearGradientBrush(fadeR, Color.Transparent, Color.FromArgb(10, 28, 16), 90f))
                    g.FillRectangle(fade, fadeR);

                // status badge
                Color bg = exp.Status == "Available" ? Color.FromArgb(200, 14, 80, 52)
                         : exp.Status == "Limited" ? Color.FromArgb(200, 120, 60, 0)
                                                      : Color.FromArgb(200, 120, 20, 20);
                Color fg = exp.Status == "Available" ? Color.FromArgb(168, 240, 216)
                         : exp.Status == "Limited" ? Color.FromArgb(255, 210, 110)
                                                      : Color.FromArgb(255, 160, 160);
                var bdg = new Rectangle(18, 16, 96, 28);
                using (var p2 = RoundRect(bdg, 12))
                using (var br2 = new SolidBrush(bg)) g.FillPath(br2, p2);
                using (var p2 = RoundRect(bdg, 12))
                using (var pen = new Pen(Color.FromArgb(60, fg), 1f)) g.DrawPath(pen, p2);
                TextRenderer.DrawText(g, "● " + exp.Status, new Font("Segoe UI", 8f, FontStyle.Bold),
                    bdg, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            modal.Controls.Add(hero);

            // ── CLOSE BUTTON
            var btnX = new Button
            {
                Text = "✕",
                Size = new Size(38, 38),
                Location = new Point(mw - 54, 16),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(115, 7, 18, 12),
                ForeColor = Color.FromArgb(220, C_GOLD_LIGHT),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnX.FlatAppearance.BorderSize = 0;
            btnX.Resize += (s, e) =>
            {
                using var path = RoundRect(new Rectangle(0, 0, btnX.Width, btnX.Height), 12);
                btnX.Region = new Region(path);
            };
            btnX.Click += (s, e) => modal.Dispose();
            modal.Controls.Add(btnX);
            btnX.BringToFront();

            // ── BOTTOM STRIP
            var strip = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                BackColor = Color.FromArgb(8, 22, 13)
            };
            strip.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var sr = strip.ClientRectangle;
                using (var fill = new LinearGradientBrush(sr, Color.FromArgb(8, 22, 13), Color.FromArgb(6, 18, 11), 90f))
                    pe.Graphics.FillRectangle(fill, sr);
                using (var pen = new Pen(Color.FromArgb(92, C_GOLD), 1f))
                    pe.Graphics.DrawLine(pen, 24, 0, strip.Width - 24, 0);
                using (var pen = new Pen(Color.FromArgb(26, 255, 255, 255), 1f))
                    pe.Graphics.DrawLine(pen, 24, 1, strip.Width - 24, 1);

                TextRenderer.DrawText(pe.Graphics, exp.Price,
                    new Font("Georgia", 18f, FontStyle.Bold),
                    new Rectangle(28, 10, 180, 36),
                    C_GOLD_LIGHT,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                TextRenderer.DrawText(pe.Graphics, "/person",
                    new Font("Segoe UI", 8.5f),
                    new Rectangle(28, 48, 72, 18),
                    Color.FromArgb(160, C_CREAM),
                    TextFormatFlags.Left);

                var noteRect = new Rectangle(96, 46, 172, 24);
                using (var notePath = RoundRect(noteRect, 10))
                using (var noteFill = new SolidBrush(Color.FromArgb(18, 52, 32)))
                using (var notePen = new Pen(Color.FromArgb(60, C_GOLD), 1f))
                {
                    pe.Graphics.FillPath(noteFill, notePath);
                    pe.Graphics.DrawPath(notePen, notePath);
                }
                TextRenderer.DrawText(pe.Graphics, exp.Cancel,
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    noteRect,
                    Color.FromArgb(210, 236, 224),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            modal.Controls.Add(strip);

            // Book Now button in strip
            var btnBook = new Button
            {
                Text = "Book Now  →",
                Size = new Size(146, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_GOLD,
                ForeColor = Color.FromArgb(12, 35, 14),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBook.FlatAppearance.BorderSize = 0;
            btnBook.Click += (s, e) =>
            {
                modal.Dispose();
                Form mainForm = this.FindForm();
                if (mainForm == null) return;
                Panel pnlMain = mainForm.Controls.Find("pnlMain", true).FirstOrDefault() as Panel;
                if (pnlMain == null) return;
                pnlMain.Controls.Clear();
                BookNow bookNow = new BookNow { Dock = DockStyle.Fill };
                pnlMain.Controls.Add(bookNow);
                bookNow.BringToFront();
                bookNow.OpenExperienceVisit();
            };
            strip.Controls.Add(btnBook);
            btnBook.Location = new Point(mw - 170, 20);
            strip.Resize += (s, e) => btnBook.Location = new Point(strip.Width - 170, 20);

            // ── SCROLLABLE BODY
            var body = new Panel
            {
                Location = new Point(0, hero.Bottom),
                Size = new Size(mw, mh - hero.Height - strip.Height),
                AutoScroll = true,
                Padding = new Padding(30, 22, 30, 22),
                BackColor = Color.FromArgb(10, 28, 16)
            };
            modal.Controls.Add(body);

            int by = 0;
            int bodyW = mw - 60; // 30px padding each side
            int contentW = Math.Min(bodyW, 680);
            int contentX = Math.Max(0, (bodyW - contentW) / 2);

            // Category + Zone
            body.Controls.Add(new Label
            {
                Text = exp.Category.ToUpper() + "  ·  " + exp.Zone.ToUpper(),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = C_GOLD,
                BackColor = Color.Transparent,
                Location = new Point(contentX, by),
                Size = new Size(contentW, 18),
                AutoSize = false,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleCenter
            });
            by += 24;

            // Name
            var nameLabel = new Label
            {
                Text = exp.Name,
                Font = new Font("Georgia", 20f, FontStyle.Bold),
                ForeColor = C_CREAM,
                BackColor = Color.Transparent,
                Location = new Point(contentX, by),
                Size = new Size(contentW, 0),
                AutoSize = false
            };
            using (var g3 = body.CreateGraphics())
            {
                SizeF titleSize = g3.MeasureString(exp.Name, nameLabel.Font, contentW);
                nameLabel.Height = Math.Max(38, (int)Math.Ceiling(titleSize.Height) + 6);
            }
            nameLabel.TextAlign = ContentAlignment.TopCenter;
            body.Controls.Add(nameLabel);
            by += nameLabel.Height + 8;

            // Gold divider
            body.Controls.Add(new Panel
            {
                Height = 2,
                Width = 50,
                BackColor = C_GOLD,
                Location = new Point(contentX + (contentW - 50) / 2, by)
            });
            by += 14;

            // Description
            var descLabel = new Label
            {
                Text = exp.Desc,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(165, C_CREAM),
                BackColor = Color.Transparent,
                Location = new Point(contentX, by),
                Size = new Size(contentW, 0),
                AutoSize = false
            };
            // measure height needed
            using (var g2 = body.CreateGraphics())
            {
                SizeF sz = g2.MeasureString(exp.Desc, descLabel.Font, contentW);
                descLabel.Height = (int)sz.Height + 8;
            }
            descLabel.TextAlign = ContentAlignment.TopCenter;
            body.Controls.Add(descLabel);
            by += descLabel.Height + 8;

            // ── Info chips (2 columns)
            int cellW = (contentW - 12) / 2;
            string[] iLabels = { "Duration", "Max Capacity", "Zone", "Slots Today" };
            string[] iVals = { exp.Duration, $"Up to {exp.MaxCap} guests", exp.Zone, exp.Slots };
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2, row = i / 2;
                var chip = new Panel
                {
                    Location = new Point(contentX + col * (cellW + 12), by + row * 52),
                    Size = new Size(cellW, 46),
                    BackColor = Color.FromArgb(18, 44, 26)
                };
                // gold outline on chip
                chip.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = RoundRect(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 10))
                    {
                        using (var fill = new SolidBrush(Color.FromArgb(17, 48, 28)))
                            pe.Graphics.FillPath(fill, path);
                        using (var pen = new Pen(Color.FromArgb(65, C_GOLD), 1f))
                            pe.Graphics.DrawPath(pen, path);
                    }
                };
                chip.Controls.Add(new Label
                {
                    Text = iLabels[i],
                    Font = new Font("Segoe UI", 7.5f),
                    ForeColor = Color.FromArgb(120, C_GOLD),
                    AutoSize = false,
                    Size = new Size(cellW - 16, 16),
                    BackColor = Color.Transparent,
                    Location = new Point(10, 5),
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                chip.Controls.Add(new Label
                {
                    Text = iVals[i],
                    Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                    ForeColor = C_CREAM,
                    AutoSize = false,
                    Size = new Size(cellW - 16, 20),
                    BackColor = Color.Transparent,
                    Location = new Point(10, 22),
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleCenter
                });
                body.Controls.Add(chip);
            }
            by += 110;

            // ── What's Included header
            body.Controls.Add(new Label
            {
                Text = "WHAT'S INCLUDED",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, C_GOLD),
                AutoSize = false,
                Size = new Size(contentW, 18),
                BackColor = Color.Transparent,
                Location = new Point(contentX, by),
                TextAlign = ContentAlignment.MiddleCenter
            });
            by += 24;

            // Includes chips (3 columns)
            int aChipW = (contentW - 8) / 3;
            int aChipH = 76;
            for (int ai = 0; ai < exp.Includes.Length; ai++)
            {
                string[] pts = exp.Includes[ai].Split(new[] { ' ' }, 2);
                string icon = pts[0];
                string iname = pts.Length > 1 ? pts[1] : "";
                int acol = ai % 3, arow = ai / 3;
                var chip = new Panel
                {
                    Location = new Point(contentX + acol * (aChipW + 4), by + arow * (aChipH + 6)),
                    Size = new Size(aChipW, aChipH),
                    BackColor = Color.FromArgb(16, 40, 22)
                };
                chip.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var path = RoundRect(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 10))
                    {
                        using (var br = new LinearGradientBrush(chip.ClientRectangle, Color.FromArgb(15, 36, 21), Color.FromArgb(12, 30, 18), 90f))
                            pe.Graphics.FillPath(br, path);
                        using (var pen = new Pen(Color.FromArgb(55, C_GOLD), 1f))
                            pe.Graphics.DrawPath(pen, path);
                    }
                };
                chip.Controls.Add(new Label
                {
                    Text = icon,
                    Font = new Font("Segoe UI Emoji", 17f),
                    AutoSize = false,
                    Size = new Size(aChipW, 30),
                    BackColor = Color.Transparent,
                    Location = new Point(0, 8),
                    TextAlign = ContentAlignment.MiddleCenter
                });
                chip.Controls.Add(new Label
                {
                    Text = iname,
                    Font = new Font("Segoe UI", 7.25f),
                    ForeColor = Color.FromArgb(208, C_CREAM),
                    AutoSize = false,
                    Size = new Size(aChipW - 10, 30),
                    BackColor = Color.Transparent,
                    Location = new Point(5, 40),
                    TextAlign = ContentAlignment.TopCenter,
                    AutoEllipsis = true
                });
                body.Controls.Add(chip);
            }
        }

        // ═══════════════════════════════════════════════
        //  StyleInput
        // ═══════════════════════════════════════════════
        private void StyleInput(Control ctrl)
        {
            ctrl.Font = new Font("Segoe UI", 10);

            if (ctrl is DateTimePicker dtp)
            {
                dtp.Format = DateTimePickerFormat.Custom; dtp.CustomFormat = "dd/MM/yyyy";
                dtp.Size = new Size(165, 34); dtp.Visible = false;
                Panel fi = new Panel { Size = dtp.Size, BackColor = Color.FromArgb(30, 55, 35), Cursor = Cursors.Hand };
                fi.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = fi.ClientRectangle; r.Inflate(-1, -1); int rad = 8;
                    GraphicsPath p = new GraphicsPath();
                    p.AddArc(r.X, r.Y, rad, rad, 180, 90); p.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
                    p.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90); p.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
                    p.CloseFigure();
                    using (var f = new SolidBrush(Color.FromArgb(30, 55, 35))) pe.Graphics.FillPath(f, p);
                    using (var b = new Pen(Color.FromArgb(70, 220, 215, 200), 1.2f)) pe.Graphics.DrawPath(b, p);
                    TextRenderer.DrawText(pe.Graphics, dtp.Value.ToString("dd/MM/yyyy"), new Font("Segoe UI", 10),
                        new Rectangle(r.X + 10, r.Y, r.Width - 40, r.Height), Color.FromArgb(220, 215, 205),
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    Rectangle ir = new Rectangle(r.Right - 30, r.Y + 1, 29, r.Height - 2);
                    using (var ib = new SolidBrush(Color.FromArgb(18, 40, 22))) pe.Graphics.FillRectangle(ib, ir);
                    int ix2 = ir.Left + (ir.Width / 2) - 7, iy2 = ir.Top + (ir.Height / 2) - 7;
                    Rectangle cb = new Rectangle(ix2, iy2, 14, 14);
                    using (var cp = new Pen(Color.FromArgb(180, 200, 180), 1.2f))
                    {
                        pe.Graphics.DrawRectangle(cp, cb);
                        pe.Graphics.DrawLine(cp, cb.X, cb.Y + 4, cb.Right, cb.Y + 4);
                        pe.Graphics.DrawLine(cp, cb.X + 4, cb.Y - 2, cb.X + 4, cb.Y + 2);
                        pe.Graphics.DrawLine(cp, cb.Right - 4, cb.Y - 2, cb.Right - 4, cb.Y + 2);
                        for (int row = 0; row < 2; row++) for (int col = 0; col < 3; col++)
                                pe.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(160, 180, 160)), cb.X + 2 + (col * 4), cb.Y + 7 + (row * 4), 2, 2);
                    }
                };
                fi.Click += (s, ce) => { dtp.Location = fi.Location; dtp.Visible = true; dtp.Focus(); SendMessage(dtp.Handle, 0x408, IntPtr.Zero, IntPtr.Zero); };
                dtp.Leave += (s, le) => { dtp.Visible = false; fi.Invalidate(); };
                dtp.ValueChanged += (s, ve) => { fi.Invalidate(); };
                dtp.Parent?.Controls.Add(fi); fi.Location = dtp.Location; fi.BringToFront(); dtp.Tag = fi;
            }

            if (ctrl is ComboBox cmb)
            {
                cmb.Visible = false; cmb.BackColor = Color.FromArgb(30, 55, 35);
                cmb.ForeColor = Color.FromArgb(220, 215, 205); cmb.FlatStyle = FlatStyle.Flat;
                cmb.DrawMode = DrawMode.OwnerDrawFixed;
                cmb.DrawItem += (s, e) => {
                    if (e.Index < 0) return;
                    bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                    e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(50, 80, 50) : Color.FromArgb(30, 55, 35)), e.Bounds);
                    TextRenderer.DrawText(e.Graphics, Convert.ToString(cmb.Items[e.Index]) ?? string.Empty, new Font("Segoe UI", 10), e.Bounds, Color.FromArgb(220, 215, 205), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                };
                Panel fc = new Panel { Size = new Size(130, 34), BackColor = Color.FromArgb(30, 55, 35), Cursor = Cursors.Hand };
                fc.Paint += (s, pe) => {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle r = fc.ClientRectangle; r.Inflate(-1, -1); int rad = 8;
                    GraphicsPath p = new GraphicsPath();
                    p.AddArc(r.X, r.Y, rad, rad, 180, 90); p.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
                    p.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90); p.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
                    p.CloseFigure();
                    using (var f = new SolidBrush(Color.FromArgb(30, 55, 35))) pe.Graphics.FillPath(f, p);
                    using (var b = new Pen(Color.FromArgb(70, 220, 215, 200), 1.2f)) pe.Graphics.DrawPath(b, p);
                    string sv = cmb.SelectedIndex >= 0 ? (Convert.ToString(cmb.Items[cmb.SelectedIndex]) ?? string.Empty) : string.Empty;
                    TextRenderer.DrawText(pe.Graphics, sv, new Font("Segoe UI", 10), new Rectangle(r.X + 10, r.Y, r.Width - 35, r.Height), Color.FromArgb(220, 215, 205), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                    Rectangle ar = new Rectangle(r.Right - 28, r.Y + 1, 27, r.Height - 2);
                    using (var ab = new SolidBrush(Color.FromArgb(18, 40, 22))) pe.Graphics.FillRectangle(ab, ar);
                    int ax = ar.Left + (ar.Width / 2), ay = ar.Top + (ar.Height / 2) - 2;
                    using (var ab = new SolidBrush(Color.FromArgb(180, 200, 180))) pe.Graphics.FillPolygon(ab, new[] { new Point(ax - 5, ay), new Point(ax + 5, ay), new Point(ax, ay + 6) });
                };
                fc.Click += (s, ce) => { cmb.Location = fc.Location; cmb.Size = fc.Size; cmb.Visible = true; cmb.Focus(); cmb.DroppedDown = true; };
                cmb.Leave += (s, le) => { cmb.Visible = false; fc.Invalidate(); };
                cmb.SelectedIndexChanged += (s, ve) => { cmb.Visible = false; fc.Invalidate(); };
                cmb.Parent?.Controls.Add(fc); fc.Location = cmb.Location; fc.BringToFront(); cmb.Tag = fc;
            }
        }

        // ═══════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════
        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
        private static GraphicsPath RoundRectTop(Rectangle r, int radius)
        {
            int d = radius * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddLine(r.Right, r.Bottom, r.X, r.Bottom); p.CloseFigure(); return p;
        }
        private static GraphicsPath RoundRectLeft(Rectangle r, int radius)
        {
            int d = radius * 2; var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddLine(r.Right, r.Y, r.Right, r.Bottom);
            p.AddLine(r.Right, r.Bottom, r.X + d / 2, r.Bottom); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        private void PaintHeroBackground(object sender, PaintEventArgs pe)
        {
            Panel pnl = (Panel)sender;
            HeroSurfacePainter.Paint(pe.Graphics, pnl.ClientRectangle, HeroSurfaceVariant.Experiences);
        }
    }
}
