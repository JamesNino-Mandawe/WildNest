using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Project.Accomodations;

namespace Project.Booking
{
    public partial class FullStayExperience : UserControl
    {
        public event Action<Project.BookingSummary>? OnSummaryChanged;

        private readonly string[] _stepLabels = { "Stay", "Experiences", "Guest Info", "Payment", "Confirm" };
        private Panel _stepBar = null!;

        // Step 1 — Stay
        private DateTimePicker _dtCheckIn = null!, _dtCheckOut = null!;
        private Label _lblNights = null!, _lblAdults = null!, _lblChildren = null!;
        private int _adults = 2, _children = 0;
        private string _selectedCabin = "";
        private int _cabinRate = 0;
        private readonly List<Panel> _cabinCards = new();

        // Step 2 — Experiences
        private readonly List<Panel> _expCards = new();
        private readonly List<Panel> _addonCards = new();
        private readonly Dictionary<string, int> _addons = new();

        // Step 3 — Guest Info
        private TextBox _txtFirst = null!, _txtLast = null!, _txtEmail = null!, _txtPhone = null!;
        private ComboBox _cboNationality = null!, _cboIdType = null!;
        private TextBox _txtRequests = null!;

        // Step 4 — Payment
        private readonly List<Panel> _payCards = new();
        private Panel _pnlCardFields = null!;
        private TextBox _txtCardNum = null!, _txtCardName = null!, _txtExpiry = null!, _txtCvv = null!;
        private string _paymentMethod = "Credit / Debit Card";

        // Step 5 — Confirm
        private Panel _pnlReviewCard = null!;
        private Panel _pnlTotalStrip = null!;
        private Panel _pnlEmailNotice = null!;
        private Button _btnConfirmNow = null!;
        private Panel _pnlConfirmed = null!;
        private Label _lblBid = null!;
        private System.Drawing.Bitmap _qrBitmap = null!;
        private Label _rvCabin = null!, _rvDates = null!, _rvExperiences = null!;

        private int _currentStep = 1;
        private bool _uiBuilt = false;
        private int _builtContentWidth = -1;
        private const int GAP = 20;

        public FullStayExperience()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = BookingFlowTheme.Page;
            VisibleChanged += (s, e) => { if (Visible) BeginInvoke(new Action(TryBuild)); };
            Resize += (s, e) => { if (Visible) BeginInvoke(new Action(TryBuild)); };
        }

        public void TryBuild() => BuildUi();

        private void BuildUi()
        {
            int layoutWidth = Math.Max(pnlContent.Width, Width);
            if (layoutWidth < 100) return;
            if (_uiBuilt && Math.Abs(layoutWidth - _builtContentWidth) < 24) return;

            int stepToShow = _uiBuilt ? _currentStep : 1;
            if (_uiBuilt)
                ResetGeneratedLayout(new[] { pnlStay, pnlExperiences, pnlGuestInfo, pnlPayment, pnlConfirm });

            _uiBuilt = true;
            _builtContentWidth = layoutWidth;

            pnlContent.BackColor = BookingFlowTheme.Page;

            _stepBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = BookingFlowTheme.Dark };
            _stepBar.Paint += (s, e) =>
                BookingFlowTheme.PaintStepBar(e.Graphics, _stepBar.ClientRectangle, _stepLabels, _currentStep);
            pnlContent.Controls.Add(_stepBar);
            pnlContent.Controls.SetChildIndex(_stepBar, 0);

            Action resizePanels = () =>
            {
                int w = pnlContent.Width;
                int h = Math.Max(pnlContent.Height - 80, 100);
                foreach (var p in new[] { pnlStay, pnlExperiences, pnlGuestInfo, pnlPayment, pnlConfirm })
                    p.Size = new Size(w, h);
            };
            foreach (var p in new[] { pnlStay, pnlExperiences, pnlGuestInfo, pnlPayment, pnlConfirm })
            {
                p.Location = new Point(0, 80);
                p.Size = new Size(pnlContent.Width, Math.Max(pnlContent.Height - 80, 100));
                p.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                p.BackColor = BookingFlowTheme.Page;
            }
            resizePanels();
            pnlContent.Resize += (s, e) => resizePanels();

            BuildStayStep();
            BuildExperiencesStep();
            BuildGuestStep();
            BuildPaymentStep();
            BuildConfirmStep();
            ShowStep(Math.Max(1, Math.Min(5, stepToShow)));
        }

        private void ResetGeneratedLayout(Panel[] panels)
        {
            if (_stepBar != null)
            {
                pnlContent.Controls.Remove(_stepBar);
                _stepBar.Dispose();
                _stepBar = null;
            }

            _cabinCards.Clear();
            _expCards.Clear();
            _addonCards.Clear();
            _payCards.Clear();
            foreach (var panel in panels)
            {
                panel.Controls.Clear();
                panel.Visible = false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 1 — STAY
        // ══════════════════════════════════════════════════════════
        private void BuildStayStep()
        {
            var (stack, W) = ScrollStack(pnlStay);

            // ── Dates ──────────────────────────────────────────
            var sDate = Sec("📅", "Select Your Dates", "Minimum 1 night · Free cancellation 48 hours prior", W);
            var dBody = Body(sDate);

            int badgeW = 120;
            int pickerW = (W - GAP * 2 - badgeW) / 2;

            dBody.Controls.Add(FL("CHECK-IN", 0, 0));
            dBody.Controls.Add(FL("CHECK-OUT", pickerW + GAP + badgeW + GAP, 0));

            _dtCheckIn = BookingFlowTheme.CreateDatePicker();
            _dtCheckIn.SetBounds(0, 20, pickerW, 38);
            _dtCheckIn.Value = DateTime.Today.AddDays(2);
            _dtCheckIn.ValueChanged += (s, e) => SyncNights();
            dBody.Controls.Add(_dtCheckIn);

            _dtCheckOut = BookingFlowTheme.CreateDatePicker();
            _dtCheckOut.SetBounds(pickerW + GAP + badgeW + GAP, 20, pickerW, 38);
            _dtCheckOut.Value = DateTime.Today.AddDays(5);
            _dtCheckOut.ValueChanged += (s, e) => SyncNights();
            dBody.Controls.Add(_dtCheckOut);

            var badge = new Panel { Location = new Point(pickerW + GAP, 0), Size = new Size(badgeW, 70), BackColor = Color.Transparent };
            badge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, badge.Width - 1, badge.Height - 1), 9);
                using var b = new SolidBrush(Color.FromArgb(22, 212, 160, 23)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(60, 212, 160, 23), 1f); e.Graphics.DrawPath(pen, path);
            };
            _lblNights = new Label { Text = "3", Font = new Font("Georgia", 22f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, Size = new Size(badgeW, 36), Location = new Point(0, 8), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            badge.Controls.Add(_lblNights);
            badge.Controls.Add(new Label { Text = "NIGHTS", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextDim, Size = new Size(badgeW, 16), Location = new Point(0, 48), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
            dBody.Controls.Add(badge);

            dBody.Height = 66;
            sDate.Height = 72 + dBody.Height + 32;
            stack.Controls.Add(sDate);

            // ── Guests ──────────────────────────────────────────
            var sGuests = Sec("👥", "Guests", "Number of adults and children staying", W);
            var gBody = Body(sGuests);
            int cW = (W - GAP) / 2;
            gBody.Controls.Add(CounterCard("Adults", "Age 13 and above", 0, cW, ref _adults, ref _lblAdults, null));
            gBody.Controls.Add(CounterCard("Children", "Age 4–12 · ₱500 fee each", cW + GAP, cW, ref _children, ref _lblChildren, null));
            gBody.Height = 80;
            sGuests.Height = 72 + gBody.Height + 32;
            stack.Controls.Add(sGuests);

            // ── Choose Cabin ──────────────────────────────────────
            var sCabin = Sec("🏕️", "Choose Your Cabin", "All 10 cabins available", W);
            var cBody = Body(sCabin);

            var cabins = new (string N, string Meta, int Price, string Status, string Icon)[]
            {
                ("River Shell",       "2 guests · Teardrop pod · River overlook",    14500, "Available", "🌊"),
                ("Forest Nest",       "2 guests · Deep forest canopy",               18200, "Available", "🌳"),
                ("Cliff Edge",        "2 guests · Panoramic · Glass-bottom balcony", 22800, "Available", "🏔️"),
                ("Glass Manor",       "4 guests · Premium villa · 360° views",       35000, "Available", "✨"),
                ("Safari Tent Alpha", "2 guests · Savanna view",                      5200, "Available", "⛺"),
                ("Lakeside Lodge",    "3 guests · Waterfront",                        6500, "Available", "🏡"),
                ("Treetop Treehouse", "2 guests · Forest view",                       4500, "Available", "🌿"),
                ("Eco Bungalow",      "2 guests · Solar-powered",                     2500, "Available",  "🌱"),
                ("Forest Cabin A",    "4 guests · Nature trail access",               3500, "Available", "🏕️"),
                ("Sanctuary Villa",   "6 guests · Private pool",                     11000, "Available", "🏰"),
            };

            int ccW = (W - GAP) / 2, ccH = 96;

            for (int i = 0; i < cabins.Length; i++)
            {
                var (name, meta, price, status, icon) = cabins[i];
                bool booked = false;
                Color stClr = BookingFlowTheme.Success;

                var card = new Panel
                {
                    Location = new Point((i % 2) * (ccW + GAP), (i / 2) * (ccH + 14)),
                    Size = new Size(ccW, ccH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = booked ? Cursors.Default : Cursors.Hand,
                    Tag = false
                };
                card.Paint += (ss, ee) =>
                {
                    bool sel = (bool)card.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };

                card.Controls.Add(new Label { Text = "● Available", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = stClr, AutoSize = true, Location = new Point(14, 10), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = name, Font = new Font("Georgia", 10.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(14, 26), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = meta, Font = new Font("Segoe UI", 7.9f), ForeColor = BookingFlowTheme.TextDim, Size = new Size(ccW - 120, 16), Location = new Point(14, 48), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = $"₱{price:N0}/night", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(ccW - 108, 30), BackColor = Color.Transparent });

                if (!booked)
                {
                    EventHandler choose = (ss, ee) =>
                    {
                        _selectedCabin = name; _cabinRate = price;
                        foreach (var cc in _cabinCards) { cc.Tag = false; cc.Invalidate(); }
                        card.Tag = true; card.Invalidate();
                        FireSummary();
                    };
                    card.Click += choose;
                    foreach (Control c in card.Controls) c.Click += choose;
                }
                _cabinCards.Add(card);
                cBody.Controls.Add(card);
            }

            cBody.Height = ((cabins.Length + 1) / 2) * (ccH + 14);
            sCabin.Height = 72 + cBody.Height + 32;
            stack.Controls.Add(sCabin);

            var nav = NavBar(W);
            nav.Controls.Add(MetaLbl("Step 1 of 5 — stay details", 0));
            var btnCont = BookingFlowTheme.CreatePrimaryButton("Continue — Experiences →");
            btnCont.Location = new Point(W - btnCont.Width, 8);
            btnCont.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(btnCont);
            stack.Controls.Add(nav);

            SyncNights();
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 2 — EXPERIENCES
        // ══════════════════════════════════════════════════════════
        private void BuildExperiencesStep()
        {
            var (stack, W) = ScrollStack(pnlExperiences);

            var sExp = Sec("🌿", "Wildlife Experiences", "Add guided encounters to your stay · All 5 experiences · Price is per person", W);
            var eBody = Body(sExp);

            var exps = new (string Icon, string Name, string Detail, int Price)[]
            {
                ("🍖","Animal Feeding Tour","45 min · Morning rounds with a zookeeper · Savanna Zone · Max 10",500),
                ("🌙","Night Safari Walk","2 hrs · Guided nocturnal trail · All 8 zones · Max 15",800),
                ("📸","Photo Encounter","30 min · Up-close wildlife photography · Aviary Dome · Max 12",3500),
                ("🧑‍🌾","Keeper Experience","3 hrs · Shadow a zookeeper · Behind the scenes · Max 6",1200),
                ("🌅","Sunrise Safari Walk","90 min · Forest Zone · Max 8 · 5 slots available",600),
            };

            int ecH = 68;
            for (int i = 0; i < exps.Length; i++)
            {
                var (icon, name, detail, price) = exps[i];
                var ec = new Panel { Location = new Point(0, i * (ecH + 10)), Size = new Size(W - 48, ecH), BackColor = BookingFlowTheme.Cream, Cursor = Cursors.Hand, Tag = false };
                ec.Paint += (ss, ee) =>
                {
                    bool sel = (bool)ec.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, ec.Width - 1, ec.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };

                var iconBox = new Panel { Location = new Point(12, 10), Size = new Size(48, 48), BackColor = Color.Transparent };
                iconBox.Paint += (ss, ee) =>
                {
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, 47, 47), 10);
                    using var b = new SolidBrush(Color.FromArgb(15, 7, 26, 14)); ee.Graphics.FillPath(b, path);
                };
                iconBox.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 18f), AutoSize = true, Location = new Point(6, 8), BackColor = Color.Transparent });
                ec.Controls.Add(iconBox);

                ec.Controls.Add(new Label { Text = name, Font = new Font("Georgia", 11f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(74, 12), BackColor = Color.Transparent });
                ec.Controls.Add(new Label { Text = detail, Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(ec.Width - 200, 16), Location = new Point(74, 34), BackColor = Color.Transparent });
                var pricePanel = new Panel { Location = new Point(ec.Width - 100, 14), Size = new Size(88, 40), BackColor = Color.Transparent };
                pricePanel.Controls.Add(new Label { Text = $"₱{price:N0}", Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(0, 0), BackColor = Color.Transparent });
                pricePanel.Controls.Add(new Label { Text = "/person", Font = new Font("Segoe UI", 7.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(0, 20), BackColor = Color.Transparent });
                ec.Controls.Add(pricePanel);

                var chkCircle = new Panel { Location = new Point(ec.Width - 36, (ecH - 22) / 2), Size = new Size(22, 22), BackColor = Color.Transparent };
                chkCircle.Paint += (ss, ee) =>
                {
                    bool sel = (bool)ec.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (sel) { ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Gold), 1, 1, 19, 19); var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; ee.Graphics.DrawString("✓", new Font("Segoe UI", 9f, FontStyle.Bold), new SolidBrush(BookingFlowTheme.Dark), new RectangleF(1, 1, 19, 19), sf); }
                    else { ee.Graphics.DrawEllipse(new Pen(Color.FromArgb(40, 0, 0, 0), 1.5f), 1, 1, 19, 19); }
                };
                ec.Controls.Add(chkCircle);

                EventHandler toggle = (ss, ee) => { ec.Tag = !(bool)ec.Tag; ec.Invalidate(); foreach (Control c in ec.Controls) c.Invalidate(); iconBox.Invalidate(); };
                ec.Click += toggle;
                foreach (Control c in ec.Controls) c.Click += toggle;
                iconBox.Click += toggle;
                foreach (Control c in iconBox.Controls) c.Click += toggle;

                _expCards.Add(ec);
                eBody.Controls.Add(ec);
            }

            // Section divider "Add-on Packages"
            int divY = exps.Length * (ecH + 10) + 8;
            var divLbl = new Label { Text = "────  ADD-ON PACKAGES  ────", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(0, divY), BackColor = Color.Transparent };
            eBody.Controls.Add(divLbl);

            var addons = new (string Icon, string Name, string Desc, int Price)[]
            {
                ("🧺","Gourmet Picnic","Artisan basket for two with local wine",4500),
                ("🔭","Stargazing Kit","Telescope rental and sky map",1200),
                ("📷","Photography Pro","Dedicated photographer · 2-hour session",5500),
            };

            int aGap = 16, aCols = 3;
            int aW = (W - 48 - aGap * (aCols - 1)) / aCols, aH = 86;
            int addonY = divY + 28;

            for (int i = 0; i < addons.Length; i++)
            {
                var (aicon, aname, adesc, aprice) = addons[i];
                var ac = new Panel { Location = new Point(i * (aW + aGap), addonY), Size = new Size(aW, aH), BackColor = BookingFlowTheme.Cream, Cursor = Cursors.Hand, Tag = false };
                ac.Paint += (ss, ee) =>
                {
                    bool sel = (bool)ac.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, ac.Width - 1, ac.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };
                ac.Controls.Add(new Label { Text = aicon, Font = new Font("Segoe UI Emoji", 14), AutoSize = true, Location = new Point(12, 10), BackColor = Color.Transparent });
                ac.Controls.Add(new Label { Text = aname, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(12, 32), BackColor = Color.Transparent });
                ac.Controls.Add(new Label { Text = adesc, Font = new Font("Segoe UI", 7.5f), ForeColor = BookingFlowTheme.TextDim, Size = new Size(aW - 24, 16), Location = new Point(12, 50), BackColor = Color.Transparent });
                ac.Controls.Add(new Label { Text = $"+₱{aprice:N0}", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(12, 66), BackColor = Color.Transparent });

                EventHandler toggleA = (ss, ee) =>
                {
                    bool cur = (bool)ac.Tag; ac.Tag = !cur;
                    if (!cur) _addons[aname] = aprice; else _addons.Remove(aname);
                    ac.Invalidate(); FireSummary();
                };
                ac.Click += toggleA;
                foreach (Control c in ac.Controls) c.Click += toggleA;
                _addonCards.Add(ac);
                eBody.Controls.Add(ac);
            }

            eBody.Height = addonY + aH + 8;
            sExp.Height = 72 + eBody.Height + 32;
            stack.Controls.Add(sExp);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(1);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 2 of 5 — experiences", W / 2 - 90));
            var cont = BookingFlowTheme.CreatePrimaryButton("Continue — Guest Info →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(3);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 3 — GUEST INFO
        // ══════════════════════════════════════════════════════════
        private void BuildGuestStep()
        {
            var (stack, W) = ScrollStack(pnlGuestInfo);

            int fw = (W - GAP) / 2, fh = 38, rH = 82;
            var sec = Sec("👤", "Primary Guest Information", "Lead guest must be 18 or older", W);
            var body = Body(sec);

            _txtFirst = TB("Juan"); AddField(body, "FIRST NAME", _txtFirst, 0, 0, fw, fh);
            _txtLast = TB("Dela Cruz"); AddField(body, "LAST NAME", _txtLast, fw + GAP, 0, fw, fh);
            _txtEmail = TB("juan@email.com"); AddField(body, "EMAIL ADDRESS", _txtEmail, 0, rH, fw, fh);
            _txtEmail.TextChanged += (s, e) => ResetEmailVerificationState();
            _txtPhone = TB("09XX XXX XXXX"); AddField(body, "MOBILE NUMBER", _txtPhone, fw + GAP, rH, fw, fh);

            _cboNationality = CB(new[] { "Filipino", "Foreign national" });
            _cboIdType = CB(new[] { "Philippine Passport", "Driver's License", "PhilSys / National ID", "UMID" });
            AddField(body, "NATIONALITY", _cboNationality, 0, rH * 2, fw, fh);
            AddField(body, "VALID ID TYPE", _cboIdType, fw + GAP, rH * 2, fw, fh);

            int reqY = rH * 3 + 14;
            body.Controls.Add(FL("SPECIAL REQUESTS (OPTIONAL)", 0, reqY));
            _txtRequests = new TextBox { Location = new Point(0, reqY + 20), Size = new Size(W - 48, 76), Multiline = true, BackColor = BookingFlowTheme.Cream, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f), PlaceholderText = "Any special requests or notes for the resort..." };
            body.Controls.Add(_txtRequests);

            int verifyY = reqY + 20 + 76 + 18;
            int verifyH = AddEmailVerificationBlock(body, verifyY, W - 48);

            body.Height = verifyY + verifyH + 8;
            sec.Height = 72 + body.Height + 32;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 3 of 5 — guest info", W / 2 - 80));
            var cont = BookingFlowTheme.CreatePrimaryButton("Continue — Payment →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => { if (EnsureGuestVerificationReady()) ShowStep(4); };
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 4 — PAYMENT
        // ══════════════════════════════════════════════════════════
        private void BuildPaymentStep()
        {
            var (stack, W) = ScrollStack(pnlPayment);

            var sec = Sec("💳", "Payment Method", "All transactions are secured and encrypted", W);
            var body = Body(sec);

            var methods = new (string Icon, string Name, string Sub)[]
            {
                ("💳","Credit / Debit Card","Visa, Mastercard, JCB"),
                ("📱","GCash / Maya","E-wallet transfer"),
                ("🏦","Bank Transfer","BDO, BPI, Metrobank"),
                ("🏨","Pay at Resort","Cash or card at front desk"),
            };

            int pmW = (W - GAP) / 2, pmH = 60;
            for (int i = 0; i < methods.Length; i++)
            {
                var (icon, name, sub) = methods[i];
                int col = i % 2, row = i / 2;
                bool isFirst = i == 0;

                var card = new Panel { Location = new Point(col * (pmW + GAP), row * (pmH + 10)), Size = new Size(pmW, pmH), BackColor = BookingFlowTheme.Cream, Cursor = Cursors.Hand, Tag = isFirst };

                var radio = new Panel { Location = new Point(pmW - 34, 20), Size = new Size(20, 20), BackColor = Color.Transparent };
                radio.Paint += (ss, ee) =>
                {
                    bool sel = (bool)card.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    ee.Graphics.DrawEllipse(sel ? new Pen(BookingFlowTheme.Gold, 1.5f) : new Pen(BookingFlowTheme.Border, 1.5f), 1, 1, 17, 17);
                    if (sel) { ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Gold), 1, 1, 17, 17); ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Dark), 5, 5, 9, 9); }
                };

                card.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 15f), AutoSize = true, Location = new Point(14, 16), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(50, 12), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(50, 32), BackColor = Color.Transparent });
                card.Controls.Add(radio);

                card.Paint += (ss, ee) =>
                {
                    bool sel = (bool)card.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };

                int capturedIdx = i;
                EventHandler choose = (ss, ee) =>
                {
                    foreach (var pc in _payCards) { pc.Tag = false; pc.Invalidate(); foreach (Control c in pc.Controls) c.Invalidate(); }
                    card.Tag = true; _paymentMethod = name;
                    card.Invalidate(); foreach (Control c in card.Controls) c.Invalidate();
                    _pnlCardFields.Visible = (capturedIdx == 0);
                };
                card.Click += choose;
                foreach (Control c in card.Controls) c.Click += choose;
                _payCards.Add(card);
                body.Controls.Add(card);
            }

            // Card fields
            int cfY = 2 * (pmH + 10) + 14;
            int bW = W - 48;
            _pnlCardFields = new Panel { Location = new Point(0, cfY), Size = new Size(bW, 164), BackColor = Color.Transparent, Visible = true };
            _pnlCardFields.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlCardFields.Width - 1, _pnlCardFields.Height - 1), 10);
                using var b = new SolidBrush(Color.FromArgb(8, 212, 160, 23)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(55, 212, 160, 23), 1.5f); e.Graphics.DrawPath(pen, path);
            };
            _pnlCardFields.Controls.Add(new Label { Text = "CARD DETAILS", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent });

            int cf = bW - 36, hf = (cf - GAP) / 2, qf = (hf - GAP) / 2;
            _pnlCardFields.Controls.Add(FL("CARD NUMBER", 18, 34));
            _txtCardNum = TB("1234  5678  9012  3456"); _txtCardNum.SetBounds(18, 54, cf, 38);
            _pnlCardFields.Controls.Add(_txtCardNum);
            _pnlCardFields.Controls.Add(FL("CARDHOLDER NAME", 18, 102));
            _txtCardName = TB("Juan Dela Cruz"); _txtCardName.SetBounds(18, 122, hf, 38);
            _pnlCardFields.Controls.Add(_txtCardName);
            _pnlCardFields.Controls.Add(FL("EXPIRY", 18 + hf + GAP, 102));
            _txtExpiry = TB("MM / YY"); _txtExpiry.SetBounds(18 + hf + GAP, 122, qf, 38);
            _pnlCardFields.Controls.Add(_txtExpiry);
            _pnlCardFields.Controls.Add(FL("CVV", 18 + hf + GAP + qf + GAP, 102));
            _txtCvv = TB("•••"); _txtCvv.SetBounds(18 + hf + GAP + qf + GAP, 122, qf, 38);
            _pnlCardFields.Controls.Add(_txtCvv);
            body.Controls.Add(_pnlCardFields);

            // Policies
            int polY = cfY + _pnlCardFields.Height + 14;
            foreach (var (badge, title, desc) in new (string, string, string)[]
            {
                ("✅","Free cancellation","Cancel up to 48 hours before check-in with full refund"),
                ("🌿","Conservation fee","₱200 per booking funds Philippine wildlife conservation"),
            })
            {
                var pol = PolicyRow(badge, title, desc, polY, bW);
                body.Controls.Add(pol);
                polY += pol.Height + 10;
            }

            int termsY = polY + 6;
            body.Controls.Add(new CheckBox { Location = new Point(0, termsY), AutoSize = false, Size = new Size(bW, 44), Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextMuted, Text = "I agree to the WildNest Terms & Conditions and Cancellation Policy.", BackColor = Color.Transparent });

            body.Height = termsY + 50;
            sec.Height = 72 + body.Height + 32;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(3);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 4 of 5 — payment", W / 2 - 70));
            var cont = BookingFlowTheme.CreatePrimaryButton("Review Booking →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(5);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 5 — CONFIRM
        // ══════════════════════════════════════════════════════════
        private void BuildConfirmStep()
        {
            var (stack, W) = ScrollStack(pnlConfirm);

            var sec = Sec("📋", "Review Your Booking", "Confirm all details before submitting", W);
            var body = Body(sec);

            // Review card
            int bW = W - 48;
            _pnlReviewCard = new Panel { Location = new Point(0, 0), Size = new Size(bW, 186), BackColor = Color.White };
            _pnlReviewCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlReviewCard.Width - 1, _pnlReviewCard.Height - 1), 10);
                using var b = new SolidBrush(Color.White); e.Graphics.FillPath(b, path);
                using var pen = new Pen(BookingFlowTheme.Border, 1f); e.Graphics.DrawPath(pen, path);
            };

            var hdr = new Panel { Location = new Point(0, 0), Size = new Size(bW, 36), BackColor = BookingFlowTheme.Cream2 };
            hdr.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var gp = new GraphicsPath();
                int d = 20;
                gp.AddArc(0, 0, d, d, 180, 90); gp.AddArc(hdr.Width - d, 0, d, d, 270, 90);
                gp.AddLine(hdr.Width, hdr.Height, 0, hdr.Height); gp.CloseFigure();
                using var b = new SolidBrush(BookingFlowTheme.Cream2); e.Graphics.FillPath(b, gp);
            };
            hdr.Controls.Add(new Label { Text = "🏕️  CABIN + EXPERIENCES", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(16, 10), BackColor = Color.Transparent });
            _pnlReviewCard.Controls.Add(hdr);

            _rvCabin = AddReviewRowReturn(_pnlReviewCard, "Cabin", "—", 40, bW);
            _rvDates = AddReviewRowReturn(_pnlReviewCard, "Dates", "—", 70, bW);
            _rvExperiences = AddReviewRowReturn(_pnlReviewCard, "Experiences", "Not added yet", 100, bW);
            AddReviewRow(_pnlReviewCard, "Guests", "2 Adults", 130, bW);
            body.Controls.Add(_pnlReviewCard);

            // Total strip
            _pnlTotalStrip = new Panel { Location = new Point(0, 196), Size = new Size(bW, 56), BackColor = BookingFlowTheme.Dark };
            _pnlTotalStrip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlTotalStrip.Width - 1, _pnlTotalStrip.Height - 1), 10);
                using var b = new SolidBrush(BookingFlowTheme.Dark); e.Graphics.FillPath(b, path);
            };
            _pnlTotalStrip.Controls.Add(new Label { Text = "Total amount due", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 248, 244, 239), AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent });
            var rvTotal = new Label { Text = "₱0", Font = new Font("Georgia", 16f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(bW - 130, 14), BackColor = Color.Transparent };
            _pnlTotalStrip.Controls.Add(rvTotal);
            body.Controls.Add(_pnlTotalStrip);

            // Email notice
            _pnlEmailNotice = new Panel { Location = new Point(0, 262), Size = new Size(bW, 48), BackColor = Color.Transparent };
            _pnlEmailNotice.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlEmailNotice.Width - 1, _pnlEmailNotice.Height - 1), 8);
                using var b = new SolidBrush(Color.FromArgb(9, 39, 174, 96)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(38, 39, 174, 96), 1f); e.Graphics.DrawPath(pen, path);
            };
            _pnlEmailNotice.Controls.Add(new Label { Text = "📧", Font = new Font("Segoe UI Emoji", 11), AutoSize = true, Location = new Point(12, 14), BackColor = Color.Transparent });
            _pnlEmailNotice.Controls.Add(new Label { Text = "Confirmation email with Booking ID and QR Code will be sent automatically.", Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextMuted, Location = new Point(38, 8), Size = new Size(bW - 52, 34), BackColor = Color.Transparent });
            body.Controls.Add(_pnlEmailNotice);

            _btnConfirmNow = new Button
            {
                Text = "✓  Confirm & Book Now",
                Location = new Point(0, 320),
                Size = new Size(bW, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = BookingFlowTheme.Dark,
                ForeColor = BookingFlowTheme.Gold,
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConfirmNow.FlatAppearance.BorderSize = 0;
            _btnConfirmNow.Click += (s, e) => ConfirmBooking();
            body.Controls.Add(_btnConfirmNow);

            body.Height = 378;
            sec.Height = 72 + body.Height + 32;
            stack.Controls.Add(sec);

            _pnlConfirmed = BuildConfirmedPanel(W);
            _pnlConfirmed.Visible = false;
            stack.Controls.Add(_pnlConfirmed);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(4);
            nav.Controls.Add(back);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  CONFIRMED PANEL
        // ══════════════════════════════════════════════════════════
        private Panel BuildConfirmedPanel(int W)
        {
            var pnl = new Panel { Size = new Size(W, 560), BackColor = Color.Transparent };

            var circle = new Panel { Location = new Point(W / 2 - 36, -4), Size = new Size(72, 72), BackColor = Color.Transparent };
            circle.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(BookingFlowTheme.Success), 0, 0, 71, 71);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("✓", new Font("Segoe UI", 22f, FontStyle.Bold), Brushes.White, new RectangleF(0, 0, 72, 72), sf);
            };
            pnl.Controls.Add(circle);
            pnl.Controls.Add(new Label { Text = "Booking Confirmed!", Font = new Font("Georgia", 20f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W / 2 - 140, 70), BackColor = Color.Transparent });
            pnl.Controls.Add(new Label { Text = "Your full stay + experience package has been reserved. See you at WildNest!", Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(W, 34), Location = new Point(0, 104), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });

            var bidBox = new Panel { Location = new Point(W / 2 - 200, 146), Size = new Size(400, 54), BackColor = BookingFlowTheme.Dark };
            bidBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, bidBox.Width - 1, bidBox.Height - 1), 10);
                using var b = new SolidBrush(BookingFlowTheme.Dark); e.Graphics.FillPath(b, path);
            };
            bidBox.Controls.Add(new Label { Text = "YOUR BOOKING ID", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 248, 244, 239), AutoSize = true, Location = new Point(18, 7), BackColor = Color.Transparent });
            _lblBid = new Label { Text = "WN-2026-0000", Font = new Font("Georgia", 14f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(18, 26), BackColor = Color.Transparent };
            bidBox.Controls.Add(_lblBid);
            var btnCopy = new Button { Text = "📋 Copy ID", Location = new Point(280, 14), Size = new Size(104, 28), Font = new Font("Segoe UI", 8f), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = BookingFlowTheme.Gold, Cursor = Cursors.Hand };
            btnCopy.FlatAppearance.BorderColor = Color.FromArgb(60, 212, 160, 23);
            btnCopy.Click += (s, e) => { try { Clipboard.SetText(_lblBid.Text); } catch { } };
            bidBox.Controls.Add(btnCopy);
            pnl.Controls.Add(bidBox);

            var qrBox = new Panel { Location = new Point(W / 2 - 200, 212), Size = new Size(400, 200), BackColor = Color.White };
            qrBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, qrBox.Width - 1, qrBox.Height - 1), 10);
                using var b = new SolidBrush(Color.White); e.Graphics.FillPath(b, path);
                using var pen = new Pen(BookingFlowTheme.Border, 1f); e.Graphics.DrawPath(pen, path);
            };
            qrBox.Controls.Add(new Label { Text = "📱 YOUR QR CODE", Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(14, 12), BackColor = Color.Transparent });
            var qrPictureBox = new PictureBox
            {
                Location = new Point(100, 36),
                Size = new Size(200, 120),
                BackColor = BookingFlowTheme.Cream2,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            qrPictureBox.Paint += (s, e) =>
            {
                if (qrPictureBox.Image != null) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, qrPictureBox.Width - 1, qrPictureBox.Height - 1), 8);
                using var b = new SolidBrush(BookingFlowTheme.Cream2); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(50, 0, 0, 0), 1.5f) { DashStyle = DashStyle.Dash }; e.Graphics.DrawPath(pen, path);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString("QR Code", new Font("Segoe UI", 9f), new SolidBrush(BookingFlowTheme.TextDim), new RectangleF(0, 0, 200, 120), sf);
            };
            qrBox.Controls.Add(qrPictureBox);
            this.Tag = qrPictureBox;
            var btnSave = new Button { Text = "💾 Save QR as PNG", Location = new Point(60, 168), Size = new Size(140, 24), Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, BackColor = BookingFlowTheme.Dark, ForeColor = BookingFlowTheme.Gold, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (_qrBitmap == null) { MessageBox.Show("QR not generated yet.", "WildNest QR", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                using var dlg = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"WildNest_QR_{_lblBid.Text}.png" };
                if (dlg.ShowDialog() == DialogResult.OK) _qrBitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
            };
            var btnPrint = new Button { Text = "🖨️ Print Details", Location = new Point(208, 168), Size = new Size(130, 24), Font = new Font("Segoe UI", 7.8f), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = BookingFlowTheme.TextMuted, Cursor = Cursors.Hand };
            btnPrint.FlatAppearance.BorderColor = BookingFlowTheme.Border;
            qrBox.Controls.AddRange(new Control[] { btnSave, btnPrint });
            pnl.Controls.Add(qrBox);

            int esY = 212 + 210 + 14;
            var emailSent = new Panel { Location = new Point(W / 2 - 200, esY), Size = new Size(400, 50), BackColor = Color.Transparent };
            emailSent.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, emailSent.Width - 1, emailSent.Height - 1), 8);
                using var b = new SolidBrush(Color.FromArgb(9, 39, 174, 96)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(50, 39, 174, 96), 1f); e.Graphics.DrawPath(pen, path);
            };
            emailSent.Controls.Add(new Label { Text = "📧", Font = new Font("Segoe UI Emoji", 14), AutoSize = true, Location = new Point(14, 14), BackColor = Color.Transparent });
            emailSent.Controls.Add(new Label { Text = "Email Confirmation Sent", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(46, 8), BackColor = Color.Transparent });
            emailSent.Controls.Add(new Label { Text = "Check your inbox", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(46, 28), BackColor = Color.Transparent });
            pnl.Controls.Add(emailSent);

            int btnY = esY + 64;
            var btnPortal = new Button { Text = "🏠 Go to Guest Portal", Location = new Point(W / 2 - 200, btnY), Size = new Size(188, 42), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, BackColor = BookingFlowTheme.Dark, ForeColor = BookingFlowTheme.Gold, Cursor = Cursors.Hand };
            btnPortal.FlatAppearance.BorderSize = 0;
            var btnNew = new Button { Text = "+ New Booking", Location = new Point(W / 2 + 4, btnY), Size = new Size(188, 42), Font = new Font("Segoe UI", 9.5f), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = BookingFlowTheme.Text, Cursor = Cursors.Hand };
            btnNew.FlatAppearance.BorderColor = BookingFlowTheme.Border;
            btnNew.Click += (s, e) => ShowStep(1);
            pnl.Controls.AddRange(new Control[] { btnPortal, btnNew });
            pnl.Height = btnY + 56;
            return pnl;
        }

        // ══════════════════════════════════════════════════════════
        //  NAVIGATION
        // ══════════════════════════════════════════════════════════
        private void ShowStep(int step)
        {
            _currentStep = step;
            pnlStay.Visible = step == 1;
            pnlExperiences.Visible = step == 2;
            pnlGuestInfo.Visible = step == 3;
            pnlPayment.Visible = step == 4;
            pnlConfirm.Visible = step >= 5;

            if (step == 5)
            {
                RefreshReview();
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = true;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = true;
                if (_pnlEmailNotice != null) _pnlEmailNotice.Visible = true;
                if (_btnConfirmNow != null) _btnConfirmNow.Visible = true;
                if (_pnlConfirmed != null) _pnlConfirmed.Visible = false;
            }
            _stepBar?.Invalidate();
        }
        private const string _connStr = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";
        private void ConfirmBooking()
        {
            if (string.IsNullOrWhiteSpace(_selectedCabin))
            { MessageBox.Show("Please select a cabin first."); ShowStep(1); return; }
            if (string.IsNullOrWhiteSpace(_txtFirst?.Text) || string.IsNullOrWhiteSpace(_txtEmail?.Text))
            { MessageBox.Show("Please fill in guest information first."); ShowStep(3); return; }
            if (!EnsureGuestVerificationReady())
            { ShowStep(3); return; }

            MySqlTransaction? tx = null;
            try
            {
                using var conn = new MySqlConnection(_connStr);
                conn.Open();
                tx = conn.BeginTransaction();

                int guestId = BookingPersistence.GetOrCreateGuest(
                    conn, tx,
                    _txtFirst.Text,
                    _txtLast?.Text ?? "",
                    _txtEmail.Text,
                    _txtPhone?.Text ?? "",
                    _cboNationality?.Text ?? "",
                    _cboIdType?.Text ?? "",
                    _txtRequests?.Text ?? "");

                int cabinId = BookingPersistence.ResolveCabinId(conn, tx, _selectedCabin);
                string bid = BookingIdGenerator.NewId();

                int nights = Math.Max(1, (_dtCheckOut.Value.Date - _dtCheckIn.Value.Date).Days);
                decimal total = (_cabinRate * nights) + _addons.Values.Sum() + (_children * 500) + 400;

                var rCmd = new MySqlCommand(@"
            INSERT INTO tbl_Reservations
                (ReservationID, GuestID, CabinID, BookingType,
                 CheckInDate, CheckOutDate, NumAdults, NumChildren, TotalAmount,
                 ArrivalTime, ModeOfTransport)
            VALUES (@rid, @gid, @cid, 'FullStay',
                    @cin, @cout, @adults, @children, @total,
                    @arrTime, @transport);", conn, tx);
                rCmd.Parameters.AddWithValue("@rid", bid);
                rCmd.Parameters.AddWithValue("@gid", guestId);
                rCmd.Parameters.AddWithValue("@cid", cabinId);
                rCmd.Parameters.AddWithValue("@cin", _dtCheckIn.Value.Date);
                rCmd.Parameters.AddWithValue("@cout", _dtCheckOut.Value.Date);
                rCmd.Parameters.AddWithValue("@adults", _adults);
                rCmd.Parameters.AddWithValue("@children", _children);
                rCmd.Parameters.AddWithValue("@total", total);
                rCmd.Parameters.AddWithValue("@arrTime", "");
                rCmd.Parameters.AddWithValue("@transport", "");
                rCmd.ExecuteNonQuery();

                foreach (var ec in _expCards.Where(c => c.Tag is bool isSelected && isSelected))
                {
                    // Get name from the card's label control
                    string expName = "";
                    foreach (Control c in ec.Controls)
                        if (c is Label lbl && lbl.Font.Bold && lbl.Text.Length > 3)
                        { expName = lbl.Text; break; }

                    if (!string.IsNullOrEmpty(expName))
                    {
                        int? experienceId = BookingPersistence.ResolveExperienceId(conn, tx, expName);
                        if (experienceId.HasValue)
                            BookingPersistence.InsertExperienceLink(conn, tx, bid, experienceId.Value);
                    }
                }

                BookingPersistence.InsertPayment(conn, tx, bid, total, _paymentMethod);
                tx.Commit();

                // Generate QR (single source of truth for screen + email + Tab 2 scan)
                _qrBitmap = EmailService.GenerateQrBitmap(bid);
                if (this.Tag is PictureBox _pb) _pb.Image = _qrBitmap;

                // Send real-time email confirmation
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    var result = EmailService.SendConfirmation(
                        _txtEmail.Text.Trim(),
                        $"{_txtFirst.Text.Trim()} {_txtLast?.Text.Trim()}",
                        bid, "Full Stay + Experience", $"Check-in: {_dtCheckIn.Value:MMM dd, yyyy} → Check-out: {_dtCheckOut.Value:MMM dd, yyyy}", (_cabinRate * Math.Max(1, (_dtCheckOut.Value.Date - _dtCheckIn.Value.Date).Days)) + _addons.Values.Sum() + (_children * 500m) + 400m, _paymentMethod, (System.Drawing.Bitmap)_qrBitmap.Clone());
                    if (!result.Success)
                        ProjectDiagnostics.LogWarning("FullStayExperience", $"Booking {bid} was saved but confirmation email failed: {result.Message}");
                });

                // Show success UI
                if (_lblBid != null) _lblBid.Text = bid;
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = false;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = false;
                if (_pnlEmailNotice != null) _pnlEmailNotice.Visible = false;
                if (_btnConfirmNow != null) _btnConfirmNow.Visible = false;
                if (_pnlConfirmed != null) _pnlConfirmed.Visible = true;
                _currentStep = 6;
                _stepBar?.Invalidate();
                OnSummaryChanged?.Invoke(new Project.BookingSummary());
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }
                MessageBox.Show("Booking failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshReview()
        {
            if (_rvCabin != null) _rvCabin.Text = string.IsNullOrEmpty(_selectedCabin) ? "—" : _selectedCabin;
            if (_rvDates != null && _dtCheckIn != null && _dtCheckOut != null)
                _rvDates.Text = $"{_dtCheckIn.Value:MMM dd} – {_dtCheckOut.Value:MMM dd}";
            int selectedExps = _expCards.Count(ec => ec.Tag is bool isSelected && isSelected);
            if (_rvExperiences != null) _rvExperiences.Text = selectedExps > 0 ? $"{selectedExps} experience(s)" : "None added";
        }

        private void FireSummary()
        {
            if (string.IsNullOrEmpty(_selectedCabin)) { OnSummaryChanged?.Invoke(new Project.BookingSummary()); return; }
            int nights = Nights();
            OnSummaryChanged?.Invoke(new Project.BookingSummary
            {
                BookingType = "Full Stay + Experience",
                CabinName = _selectedCabin,
                PrimarySubtitle = $"{nights} night(s) · {_dtCheckIn?.Value:MMM dd} – {_dtCheckOut?.Value:MMM dd}",
                PrimaryAmount = _cabinRate * nights,
                PrimaryAmountLabel = $"₱{_cabinRate:N0} × {nights} night(s)",
                Lines = new List<Project.SummaryLine>
                {
                    new() { Label = "Adults",           Value = _adults.ToString() },
                    new() { Label = "Children",         Value = _children.ToString() },
                    new() { Label = "Resort fee",       Value = "₱200" },
                    new() { Label = "Conservation fee", Value = "₱200" },
                },
                Addons = _addons.Select(x => new Project.AddonItem { Name = x.Key, Price = x.Value }).ToList(),
                GrandTotal = CalcTotal(),
                CabinPricePerNight = _cabinRate,
                Nights = nights
            });
        }

        private int Nights() => _dtCheckIn != null && _dtCheckOut != null ? Math.Max(1, (_dtCheckOut.Value.Date - _dtCheckIn.Value.Date).Days) : 1;
        private int CalcTotal() => _cabinRate * Nights() + _children * 500 + _addons.Values.Sum() + 400;

        private void SyncNights()
        {
            if (_dtCheckOut != null && _dtCheckIn != null && _dtCheckOut.Value <= _dtCheckIn.Value)
                _dtCheckOut.Value = _dtCheckIn.Value.AddDays(1);
            if (_lblNights != null) _lblNights.Text = Nights().ToString();
            FireSummary();
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        private (FlowLayoutPanel stack, int W) ScrollStack(Panel parent)
        {
            int scrollW = SystemInformation.VerticalScrollBarWidth;
            int usableW = Math.Max(parent.ClientSize.Width - 34 - scrollW, 560);
            var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BookingFlowTheme.Page };
            var wrap = new Panel { Width = usableW, AutoSize = false, BackColor = Color.Transparent };
            host.Controls.Add(wrap);
            host.Resize += (s, e) =>
            {
                int newW = Math.Max(host.ClientSize.Width - 34 - scrollW, 560);
                wrap.Width = newW;
                wrap.Left = Math.Max(16, (host.ClientSize.Width - newW) / 2);
            };
            wrap.Left = Math.Max(16, (parent.ClientSize.Width - usableW) / 2);
            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(0, 24, 0, 60),
                BackColor = Color.Transparent
            };
            wrap.Controls.Add(stack);
            host.AutoScrollMinSize = new Size(0, 2000);
            stack.SizeChanged += (s, e) =>
            {
                wrap.Height = stack.Bottom + 24;
                host.AutoScrollMinSize = new Size(0, stack.Bottom + 80);
            };
            parent.Controls.Add(host);
            return (stack, usableW);
        }

        private Panel Sec(string icon, string title, string sub, int W)
        {
            var sec = new Panel { Width = W, Height = 200, BackColor = Color.Transparent };
            sec.Paint += (s, e) => BookingFlowTheme.PaintSectionShell(e.Graphics, sec.ClientRectangle);
            var hdr = new Panel { Location = new Point(0, 0), Size = new Size(W, 72), BackColor = Color.Transparent };
            hdr.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 17f), AutoSize = true, Location = new Point(24, 20), BackColor = Color.Transparent });
            hdr.Controls.Add(new Label { Text = title, Font = new Font("Georgia", 13f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = false, Size = new Size(W - 110, 24), Location = new Point(68, 14), BackColor = Color.Transparent });
            hdr.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = false, Location = new Point(68, 42), Size = new Size(W - 116, 20), BackColor = Color.Transparent });
            sec.Controls.Add(hdr);
            var body = new Panel { Name = "body", Location = new Point(24, 82), Size = new Size(W - 48, 80), BackColor = Color.Transparent };
            sec.Controls.Add(body);
            sec.Tag = body;
            return sec;
        }

        private static Panel Body(Panel sec) =>
            sec.Tag as Panel ?? throw new InvalidOperationException("Section body panel is missing.");
        private Panel NavBar(int W)
        {
            var nav = new Panel { Width = W, Height = 58, BackColor = Color.Transparent };
            nav.Paint += (s, e) => BookingFlowTheme.PaintNavBar(e.Graphics, nav.ClientRectangle);
            return nav;
        }

        private Label MetaLbl(string text, int x) => new Label { Text = text, Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(x, 20), BackColor = Color.Transparent };
        private Label FL(string text, int x, int y) => new Label { Text = text, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(x, y), BackColor = Color.Transparent };

        private void AddField(Panel parent, string label, Control ctrl, int x, int y, int w, int h)
        {
            parent.Controls.Add(FL(label, x, y));
            ctrl.Location = new Point(x, y + 20);
            ctrl.Size = new Size(w, h);
            parent.Controls.Add(ctrl);
        }

        private TextBox TB(string placeholder)
        {
            var textBox = new TextBox { PlaceholderText = placeholder };
            BookingFlowTheme.StyleTextInput(textBox);
            return textBox;
        }
        private ComboBox CB(string[] items)
        {
            var c = new ComboBox();
            c.Items.AddRange(items); if (items.Length > 0) c.SelectedIndex = 0;
            BookingFlowTheme.StyleComboBox(c);
            c.DropDownHeight = 200;
            c.IntegralHeight = false;
            return c;
        }

        private Panel CounterCard(string title, string subtitle, int x, int w, ref int count, ref Label lbl, Action? onChange)
        {
            int localCount = count;
            var card = new Panel { Location = new Point(x, 0), Size = new Size(w, 80), BackColor = BookingFlowTheme.Cream };
            card.Paint += (s, e) => BookingFlowTheme.PaintRoundedCard(card, e);
            card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(14, 14), BackColor = Color.Transparent });
            card.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(14, 36), BackColor = Color.Transparent });
            int btnRight = w - 18;
            var lblCount = new Label { Text = localCount.ToString(), Font = new Font("Georgia", 15f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, Size = new Size(34, 30), Location = new Point(btnRight - 50, 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            lbl = lblCount;
            var btnM = SmallBtn("−", btnRight - 88, 24);
            var btnP = SmallBtn("+", btnRight - 14, 24);
            btnM.Click += (s, e) =>
            {
                localCount = Math.Max(0, localCount - 1);
                lblCount.Text = localCount.ToString();
                SyncGuestCounter(title, localCount);
                onChange?.Invoke();
                FireSummary();
            };
            btnP.Click += (s, e) =>
            {
                localCount++;
                lblCount.Text = localCount.ToString();
                SyncGuestCounter(title, localCount);
                onChange?.Invoke();
                FireSummary();
            };
            card.Controls.AddRange(new Control[] { btnM, lblCount, btnP });
            return card;
        }

        private void SyncGuestCounter(string title, int value)
        {
            if (title.StartsWith("Adults", StringComparison.OrdinalIgnoreCase))
                _adults = value;
            else
                _children = value;
        }

        private Button SmallBtn(string text, int x, int y)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(32, 32) };
            BookingFlowTheme.StyleSmallButton(btn);
            return btn;
        }

        private void AddReviewRow(Panel parent, string label, string value, int y, int W)
        {
            parent.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W - 230, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Panel { Location = new Point(18, y + 24), Size = new Size(W - 36, 1), BackColor = Color.FromArgb(14, 0, 0, 0) });
        }

        private Label AddReviewRowReturn(Panel parent, string label, string value, int y, int W)
        {
            parent.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, y + 4), BackColor = Color.Transparent });
            var lVal = new Label { Text = value, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W - 230, y + 4), BackColor = Color.Transparent };
            parent.Controls.Add(lVal);
            parent.Controls.Add(new Panel { Location = new Point(18, y + 24), Size = new Size(W - 36, 1), BackColor = Color.FromArgb(14, 0, 0, 0) });
            return lVal;
        }

        private Panel PolicyRow(string badge, string title, string desc, int y, int W)
        {
            var pnl = new Panel { Location = new Point(0, y), Size = new Size(W, 56), BackColor = BookingFlowTheme.Cream2 };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8);
                using var b = new SolidBrush(BookingFlowTheme.Cream2); e.Graphics.FillPath(b, path);
            };
            pnl.Controls.Add(new Label { Text = badge, Font = new Font("Segoe UI Emoji", 14), AutoSize = true, Location = new Point(14, 16), BackColor = Color.Transparent });
            pnl.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(50, 10), BackColor = Color.Transparent });
            pnl.Controls.Add(new Label { Text = desc, Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, Location = new Point(50, 30), Size = new Size(W - 68, 18), BackColor = Color.Transparent });
            return pnl;
        }
    }
}
