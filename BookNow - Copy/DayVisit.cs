using MySql.Data.MySqlClient;
using Project.Accomodations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Project.Booking
{
    public partial class DayVisit : UserControl
    {
        public event Action<Project.BookingSummary>? OnSummaryChanged;

        private readonly string[] _stepLabels = { "Visit Details", "Guest Info", "Payment", "Confirm" };
        private Panel _stepBar = null!;

        // Step 1 controls
        private DateTimePicker _dtVisitDate = null!;
        private ComboBox _cboEntryTime = null!;
        private Label _lblAdults = null!, _lblChildren = null!, _lblSeniors = null!;
        private int _adults = 2, _children = 0, _seniors = 0;
        private readonly List<Panel> _zoneCards = new();

        // Step 2 controls
        private TextBox _txtFirst = null!, _txtLast = null!, _txtEmail = null!, _txtPhone = null!;
        private ComboBox _cboNationality = null!, _cboIdType = null!;

        // Step 3 controls
        private readonly List<Panel> _payCards = new();
        private Panel _pnlCardFields = null!;
        private TextBox _txtCardNum = null!, _txtCardName = null!, _txtExpiry = null!, _txtCvv = null!;

        // Step 4 (Confirm) controls
        private Panel _pnlReviewCard = null!;
        private Panel _pnlTotalStrip = null!;
        private Panel _pnlEmailNotice = null!;
        private Button _btnConfirmNow = null!;
        private Panel _pnlConfirmed = null!;
        private Label _lblBid = null!;
        private System.Drawing.Bitmap _qrBitmap = null!;

        private int _currentStep = 1;
        private bool _uiBuilt = false;
        private string _paymentMethod = "Credit / Debit Card";
        private const int GAP = 14;

        public DayVisit()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = BookingFlowTheme.Page;
            HandleCreated += (s, e) => BeginInvoke(new Action(BuildUi));
            VisibleChanged += (s, e) => { if (Visible && !_uiBuilt) BeginInvoke(new Action(BuildUi)); };
            Resize += (s, e) => { if (!_uiBuilt) BeginInvoke(new Action(BuildUi)); };
        }

        private void BuildUi()
        {
            if (_uiBuilt || Width < 100) return;
            _uiBuilt = true;

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
                foreach (var p in new[] { pnlVisitDetails, pnlGuestInfo, pnlPayment, pnlConfirm })
                    p.Size = new Size(w, h);
            };
            foreach (var p in new[] { pnlVisitDetails, pnlGuestInfo, pnlPayment, pnlConfirm })
            {
                p.Location = new Point(0, 80);
                p.Size = new Size(pnlContent.Width, Math.Max(pnlContent.Height - 80, 100));
                p.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                p.BackColor = BookingFlowTheme.Page;
            }
            pnlContent.Resize += (s, e) => resizePanels();

            BuildVisitDetailsStep();
            BuildGuestStep();
            BuildPaymentStep();
            BuildConfirmStep();
            ShowStep(1);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 1 — VISIT DETAILS
        // ══════════════════════════════════════════════════════════
        private void BuildVisitDetailsStep()
        {
            var (stack, W) = ScrollStack(pnlVisitDetails);

            // ── Visit Date & Time ──────────────────────────────
            var sDate = Sec("📅", "Visit Date & Time", "Day passes are for a single day — no overnight stay", W);
            var dBody = Body(sDate);

            int fw = (W - GAP) / 2;
            dBody.Controls.Add(FL("VISIT DATE", 0, 0));
            _dtVisitDate = BookingFlowTheme.CreateDatePicker();
            _dtVisitDate.SetBounds(0, 20, fw, 38);
            dBody.Controls.Add(_dtVisitDate);

            dBody.Controls.Add(FL("ENTRY TIME", fw + GAP, 0));
            _cboEntryTime = CB(new[] { "8:00 AM (opens)", "9:00 AM", "10:00 AM", "1:00 PM" });
            _cboEntryTime.SetBounds(fw + GAP, 20, fw, 38);
            dBody.Controls.Add(_cboEntryTime);

            dBody.Height = 66;
            sDate.Height = 66 + dBody.Height + 20;
            stack.Controls.Add(sDate);

            // ── Number of Visitors ──────────────────────────────
            var sGuests = Sec("👥", "Number of Visitors", "Day pass pricing per person", W);
            var gBody = Body(sGuests);
            int cW = (W - GAP * 2) / 3;

            gBody.Controls.Add(CounterCard("Adults", "₱450/person · Age 13+", 0, cW, ref _adults, ref _lblAdults));
            gBody.Controls.Add(CounterCard("Children", "₱250/child · Age 4–12", cW + GAP, cW, ref _children, ref _lblChildren));
            gBody.Controls.Add(CounterCard("Seniors / PWD", "₱350/person · 20% disc.", (cW + GAP) * 2, cW, ref _seniors, ref _lblSeniors));

            gBody.Height = 80;
            sGuests.Height = 66 + gBody.Height + 20;
            stack.Controls.Add(sGuests);

            // ── Zones to Visit ──────────────────────────────────
            var sZones = Sec("🗺️", "Zones to Visit", "All 8 zones — choose all or select custom zones", W);
            var zBody = Body(sZones);

            // Zone toggle buttons
            var btnAll = ZoneToggleBtn("All Zones", 0, true);
            var btnCustom = ZoneToggleBtn("Custom Zones", 100, false);
            zBody.Controls.Add(btnAll);
            zBody.Controls.Add(btnCustom);

            btnAll.Click += (s, e) =>
            {
                btnAll.BackColor = BookingFlowTheme.Dark; btnAll.ForeColor = BookingFlowTheme.Gold;
                btnCustom.BackColor = BookingFlowTheme.Cream; btnCustom.ForeColor = BookingFlowTheme.TextMuted;
                foreach (var zc in _zoneCards) { zc.Tag = true; zc.Invalidate(); UpdateZoneCheck(zc, true); }
            };
            btnCustom.Click += (s, e) =>
            {
                btnCustom.BackColor = BookingFlowTheme.Dark; btnCustom.ForeColor = BookingFlowTheme.Gold;
                btnAll.BackColor = BookingFlowTheme.Cream; btnAll.ForeColor = BookingFlowTheme.TextMuted;
                foreach (var zc in _zoneCards) { zc.Tag = false; zc.Invalidate(); UpdateZoneCheck(zc, false); }
            };

            var zones = new (string Icon, string Name, string Desc)[]
            {
                ("🦁","Savanna Zone","Free-roaming herbivores"),
                ("🐆","Predator Den","Large carnivores"),
                ("🦜","Aviary Dome","Birds of all sizes"),
                ("🐍","Reptile House","Climate-controlled"),
                ("🐒","Primate Park","Forested primate area"),
                ("🦦","Aquatic Zone","Waterfront habitat"),
                ("🦉","Nocturnal Trail","Low-light animals"),
                ("🌱","Conservation Corner","Endangered species"),
            };

            int zGridY = 46;
            int zcW = (W - GAP) / 2, zcH = 52;
            for (int i = 0; i < zones.Length; i++)
            {
                var (icon, name, desc) = zones[i];
                int col = i % 2, row = i / 2;
                var zcard = new Panel
                {
                    Location = new Point(col * (zcW + GAP), zGridY + row * (zcH + 8)),
                    Size = new Size(zcW, zcH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = true // all selected by default
                };
                zcard.Paint += (ss, ee) =>
                {
                    bool sel = (bool)zcard.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, zcard.Width - 1, zcard.Height - 1), 9);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.5f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };

                zcard.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 14), AutoSize = true, Location = new Point(10, 13), BackColor = Color.Transparent });
                zcard.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(44, 8), BackColor = Color.Transparent });
                zcard.Controls.Add(new Label { Text = desc, Font = new Font("Segoe UI", 7.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(44, 27), BackColor = Color.Transparent });

                // Check indicator
                var chkLbl = new Label { Text = "✓", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, Size = new Size(18, 18), Location = new Point(zcW - 26, 16), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Name = "chk" };
                zcard.Controls.Add(chkLbl);

                EventHandler toggle = (ss, ee) =>
                {
                    bool cur = (bool)zcard.Tag;
                    zcard.Tag = !cur;
                    UpdateZoneCheck(zcard, !cur);
                    zcard.Invalidate();
                };
                zcard.Click += toggle;
                foreach (Control c in zcard.Controls) c.Click += toggle;

                _zoneCards.Add(zcard);
                zBody.Controls.Add(zcard);
            }

            int zBodyH = zGridY + ((zones.Length + 1) / 2) * (zcH + 8);
            zBody.Height = zBodyH;
            sZones.Height = 66 + zBody.Height + 20;
            stack.Controls.Add(sZones);

            // ── Add Experience Packages ──────────────────────────
            var sAddons = Sec("🌿", "Add Experience Packages", "Optional — day visitors can add encounter packages", W);
            var aBody = Body(sAddons);

            var addons = new (string Icon, string Name, string Desc, int Price)[]
            {
                ("🍖","Animal Feeding","45 min · Savanna Zone",500),
                ("📸","Photo Opportunity","30 min · Aviary Dome",3500),
                ("🧑‍🌾","Keeper Experience","3 hrs · Behind the scenes",1200),
                ("🌙","Night Safari","2 hrs · All 8 zones",800),
                ("🌅","Sunrise Safari Walk","90 min · Forest Zone",600),
                ("🎤","Conservation Talk","1 hr · Vet Center",250),
            };

            int aCols = 3, aGap = 12;
            int aW = (W - aGap * (aCols - 1)) / aCols, aH = 86;

            for (int i = 0; i < addons.Length; i++)
            {
                var (aicon, aname, adesc, aprice) = addons[i];
                int col = i % aCols, row = i / aCols;
                var ac = new Panel
                {
                    Location = new Point(col * (aW + aGap), row * (aH + aGap)),
                    Size = new Size(aW, aH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = false
                };
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
                ac.Controls.Add(new Label { Text = $"+₱{aprice:N0}/person", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(aW - 90, 60), BackColor = Color.Transparent });

                EventHandler toggle2 = (ss, ee) => { ac.Tag = !(bool)ac.Tag; ac.Invalidate(); };
                ac.Click += toggle2;
                foreach (Control c in ac.Controls) c.Click += toggle2;
                aBody.Controls.Add(ac);
            }

            aBody.Height = ((addons.Length + aCols - 1) / aCols) * (aH + aGap);
            sAddons.Height = 66 + aBody.Height + 20;
            stack.Controls.Add(sAddons);

            // Nav
            var nav = NavBar(W);
            nav.Controls.Add(MetaLbl("Step 1 of 4 — visit details & zones", 0));
            var btnCont = BookingFlowTheme.CreatePrimaryButton("Continue — Guest Info →");
            btnCont.Location = new Point(W - btnCont.Width, 8);
            btnCont.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(btnCont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 2 — GUEST INFO
        // ══════════════════════════════════════════════════════════
        private void BuildGuestStep()
        {
            var (stack, W) = ScrollStack(pnlGuestInfo);

            int fw = (W - GAP) / 2, fh = 38, rH = 70;

            var sec = Sec("👤", "Primary Guest Information", "Lead guest must be 18 or older", W);
            var body = Body(sec);

            _txtFirst = TB("Juan"); AddField(body, "FIRST NAME", _txtFirst, 0, 0, fw, fh);
            _txtLast = TB("Dela Cruz"); AddField(body, "LAST NAME", _txtLast, fw + GAP, 0, fw, fh);
            _txtEmail = TB("juan@email.com"); AddField(body, "EMAIL ADDRESS", _txtEmail, 0, rH, fw, fh);
            _txtPhone = TB("09XX XXX XXXX"); AddField(body, "MOBILE NUMBER", _txtPhone, fw + GAP, rH, fw, fh);

            _cboNationality = CB(new[] { "Filipino", "Foreign national" });
            _cboIdType = CB(new[] { "Philippine Passport", "Driver's License", "PhilSys / National ID", "UMID" });
            AddField(body, "NATIONALITY", _cboNationality, 0, rH * 2, fw, fh);
            AddField(body, "VALID ID TYPE", _cboIdType, fw + GAP, rH * 2, fw, fh);

            body.Height = rH * 3 - 12;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(1);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 2 of 4 — guest information", W / 2 - 110));
            var cont = BookingFlowTheme.CreatePrimaryButton("Continue — Payment →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(3);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 3 — PAYMENT
        // ══════════════════════════════════════════════════════════
        private void BuildPaymentStep()
        {
            var (stack, W) = ScrollStack(pnlPayment);

            var sec = Sec("💳", "Choose Payment Method", "All transactions are secured and encrypted", W);
            var body = Body(sec);

            var methods = new (string Icon, string Name, string Sub)[]
            {
                ("💳","Credit / Debit Card","Visa, Mastercard, JCB"),
                ("📱","GCash / Maya","E-wallet transfer"),
                ("🏦","Bank Transfer","BDO, BPI, Metrobank"),
                ("🏨","Pay at Resort","Cash or card on arrival"),
            };

            int pmW = (W - GAP) / 2, pmH = 60;

            for (int i = 0; i < methods.Length; i++)
            {
                var (icon, name, sub) = methods[i];
                int col = i % 2, row = i / 2;
                bool isFirst = i == 0;

                var card = new Panel
                {
                    Location = new Point(col * (pmW + GAP), row * (pmH + 10)),
                    Size = new Size(pmW, pmH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = isFirst
                };

                var radio = new Panel { Location = new Point(pmW - 34, 20), Size = new Size(20, 20), BackColor = Color.Transparent };
                radio.Paint += (ss, ee) =>
                {
                    bool sel = (bool)card.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    ee.Graphics.DrawEllipse(sel ? new Pen(BookingFlowTheme.Gold, 1.5f) : new Pen(BookingFlowTheme.Border, 1.5f), 1, 1, 17, 17);
                    if (sel)
                    {
                        ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Gold), 1, 1, 17, 17);
                        ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Dark), 5, 5, 9, 9);
                    }
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

            // Card fields panel
            int cfY = 2 * (pmH + 10) + 14;
            int bodyW = W - 48;
            _pnlCardFields = new Panel { Location = new Point(0, cfY), Size = new Size(bodyW, 164), BackColor = Color.Transparent, Visible = true };
            _pnlCardFields.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlCardFields.Width - 1, _pnlCardFields.Height - 1), 10);
                using var b = new SolidBrush(Color.FromArgb(8, 212, 160, 23)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(55, 212, 160, 23), 1.5f); e.Graphics.DrawPath(pen, path);
            };
            _pnlCardFields.Controls.Add(new Label { Text = "CARD DETAILS", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent });

            int cf = bodyW - 36, hf = (cf - GAP) / 2, qf = (hf - GAP) / 2;
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

            // Terms
            int termsY = cfY + _pnlCardFields.Height + 14;
            body.Controls.Add(new CheckBox { Location = new Point(0, termsY), AutoSize = false, Size = new Size(bodyW, 36), Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextMuted, Text = "I agree to the WildNest Terms & Conditions and Cancellation Policy.", BackColor = Color.Transparent });

            body.Height = termsY + 42;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 3 of 4 — payment", W / 2 - 80));
            var cont = BookingFlowTheme.CreatePrimaryButton("Review Booking →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(4);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 4 — CONFIRM
        // ══════════════════════════════════════════════════════════
        private void BuildConfirmStep()
        {
            var (stack, W) = ScrollStack(pnlConfirm);

            var sec = Sec("📋", "Review Your Day Visit", "Please confirm all details before submitting", W);
            var body = Body(sec);

            // Review card
            int bW = W - 48;
            _pnlReviewCard = new Panel { Location = new Point(0, 0), Size = new Size(bW, 148), BackColor = Color.White };
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
            hdr.Controls.Add(new Label { Text = "🗺️  DAY VISIT", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(16, 10), BackColor = Color.Transparent });
            _pnlReviewCard.Controls.Add(hdr);

            AddReviewRow(_pnlReviewCard, "Type", "Day Pass — All Zones", 40, bW);
            AddReviewRow(_pnlReviewCard, "Adults × 2", "₱900", 70, bW);
            AddReviewRow(_pnlReviewCard, "Conservation fee", "₱200", 100, bW);
            body.Controls.Add(_pnlReviewCard);

            // Dark total strip
            _pnlTotalStrip = new Panel { Location = new Point(0, 158), Size = new Size(bW, 56), BackColor = BookingFlowTheme.Dark };
            _pnlTotalStrip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlTotalStrip.Width - 1, _pnlTotalStrip.Height - 1), 10);
                using var b = new SolidBrush(BookingFlowTheme.Dark); e.Graphics.FillPath(b, path);
            };
            _pnlTotalStrip.Controls.Add(new Label { Text = "Total amount due", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 248, 244, 239), AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent });
            var rvTotal = new Label { Text = "₱1,100", Font = new Font("Georgia", 16f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(bW - 130, 14), BackColor = Color.Transparent };
            _pnlTotalStrip.Controls.Add(rvTotal);
            body.Controls.Add(_pnlTotalStrip);

            // Email notice (green tint)
            _pnlEmailNotice = new Panel { Location = new Point(0, 224), Size = new Size(bW, 48), BackColor = Color.Transparent };
            _pnlEmailNotice.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlEmailNotice.Width - 1, _pnlEmailNotice.Height - 1), 8);
                using var b = new SolidBrush(Color.FromArgb(9, 39, 174, 96)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(38, 39, 174, 96), 1f); e.Graphics.DrawPath(pen, path);
            };
            _pnlEmailNotice.Controls.Add(new Label { Text = "📧", Font = new Font("Segoe UI Emoji", 12), AutoSize = true, Location = new Point(12, 14), BackColor = Color.Transparent });
            _pnlEmailNotice.Controls.Add(new Label { Text = "A confirmation email will be sent with your Booking ID and QR Code.", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, Location = new Point(40, 14), Size = new Size(bW - 56, 24), BackColor = Color.Transparent });
            body.Controls.Add(_pnlEmailNotice);

            // Confirm button
            _btnConfirmNow = new Button
            {
                Text = "✓  Confirm Day Visit",
                Location = new Point(0, 282),
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

            body.Height = 340;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            _pnlConfirmed = BuildConfirmedPanel(W);
            _pnlConfirmed.Visible = false;
            stack.Controls.Add(_pnlConfirmed);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(3);
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
            pnl.Controls.Add(new Label { Text = "Day Visit Confirmed!", Font = new Font("Georgia", 20f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W / 2 - 130, 70), BackColor = Color.Transparent });
            pnl.Controls.Add(new Label { Text = "Your WildNest day visit is reserved. Show QR or Booking ID at the entrance.", Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(W, 34), Location = new Point(0, 104), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });

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
                if (_qrBitmap == null) { System.Windows.Forms.MessageBox.Show("QR not generated yet."); return; }
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
            pnlVisitDetails.Visible = step == 1;
            pnlGuestInfo.Visible = step == 2;
            pnlPayment.Visible = step == 3;
            pnlConfirm.Visible = step >= 4;

            if (step == 4)
            {
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
            if (string.IsNullOrWhiteSpace(_txtFirst?.Text) || string.IsNullOrWhiteSpace(_txtEmail?.Text))
            { MessageBox.Show("Please fill in guest information first."); ShowStep(2); return; }

            try
            {
                using var conn = new MySqlConnection(_connStr);
                conn.Open();

                // 1. Insert guest
                var gCmd = new MySqlCommand(@"
            INSERT INTO tbl_Guests (FirstName, LastName, Email, Phone, Nationality, ValidIDType)
            VALUES (@fn, @ln, @em, @ph, @nat, @id);", conn);
                gCmd.Parameters.AddWithValue("@fn", _txtFirst.Text.Trim());
                gCmd.Parameters.AddWithValue("@ln", _txtLast?.Text.Trim() ?? "");
                gCmd.Parameters.AddWithValue("@em", _txtEmail.Text.Trim());
                gCmd.Parameters.AddWithValue("@ph", _txtPhone?.Text.Trim() ?? "");
                gCmd.Parameters.AddWithValue("@nat", _cboNationality?.Text ?? "");
                gCmd.Parameters.AddWithValue("@id", _cboIdType?.Text ?? "");
                gCmd.ExecuteNonQuery();

                int guestId = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID();", conn).ExecuteScalar());

                // 2. Generate Booking ID
                string bid = BookingIdGenerator.NewId();

                // 3. Insert reservation
                decimal total = (_adults * 450) + (_children * 250) + (_seniors * 350) + 200;
                var rCmd = new MySqlCommand(@"
            INSERT INTO tbl_Reservations
                (ReservationID, GuestID, BookingType, VisitDate, NumAdults, NumChildren, TotalAmount)
            VALUES (@rid, @gid, 'DayVisit', @vd, @adults, @children, @total);", conn);
                rCmd.Parameters.AddWithValue("@rid", bid);
                rCmd.Parameters.AddWithValue("@gid", guestId);
                rCmd.Parameters.AddWithValue("@vd", _dtVisitDate.Value.Date);
                rCmd.Parameters.AddWithValue("@adults", _adults);
                rCmd.Parameters.AddWithValue("@children", _children);
                rCmd.Parameters.AddWithValue("@total", total);
                rCmd.ExecuteNonQuery();

                // 4. Insert payment
                var pCmd = new MySqlCommand(@"
            INSERT INTO tbl_Payments (ReservationID, Amount, PaymentMethod, Status, PaidAt)
            VALUES (@rid, @amt, @meth, 'Confirmed', NOW());", conn);
                pCmd.Parameters.AddWithValue("@rid", bid);
                pCmd.Parameters.AddWithValue("@amt", total);
                pCmd.Parameters.AddWithValue("@meth", _paymentMethod);
                pCmd.ExecuteNonQuery();

                // Generate QR (single source of truth for screen + email + Tab 2 scan)
                _qrBitmap = EmailService.GenerateQrBitmap(bid);
                if (this.Tag is PictureBox _pb) _pb.Image = _qrBitmap;

                // Send real-time email confirmation
                _ = System.Threading.Tasks.Task.Run(() =>
                    EmailService.SendConfirmation(
                        _txtEmail.Text.Trim(),
                        $"{_txtFirst.Text.Trim()} {_txtLast?.Text.Trim()}",
                        bid, "Day Visit", $"Visit Date: {_dtVisitDate.Value:MMM dd, yyyy}", (decimal)((_adults * 450) + (_children * 250) + (_seniors * 350) + 200), _paymentMethod, (System.Drawing.Bitmap)_qrBitmap.Clone()));

                // Show success UI
                if (_lblBid != null) _lblBid.Text = bid;
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = false;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = false;
                if (_pnlEmailNotice != null) _pnlEmailNotice.Visible = false;
                if (_btnConfirmNow != null) _btnConfirmNow.Visible = false;
                if (_pnlConfirmed != null) _pnlConfirmed.Visible = true;
                _currentStep = 5;
                _stepBar?.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Booking failed: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        private (FlowLayoutPanel stack, int W) ScrollStack(Panel parent)
        {
            int scrollW = SystemInformation.VerticalScrollBarWidth;
            int usableW = Math.Max(parent.ClientSize.Width - 18 - scrollW, 400);
            var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BookingFlowTheme.Page };
            var wrap = new Panel { Width = usableW, AutoSize = true, BackColor = Color.Transparent };
            host.Controls.Add(wrap);
            host.Resize += (s, e) =>
            {
                int newW = Math.Max(host.ClientSize.Width - 18 - scrollW, 400);
                wrap.Width = newW;
                wrap.Left = Math.Max(6, (host.ClientSize.Width - newW) / 2);
            };
            wrap.Left = Math.Max(6, (parent.ClientSize.Width - usableW) / 2);
            var stack = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 20, 0, 40), BackColor = Color.Transparent };
            wrap.Controls.Add(stack);
            parent.Controls.Add(host);
            return (stack, usableW);
        }

        private Panel Sec(string icon, string title, string sub, int W)
        {
            var sec = new Panel { Width = W, Height = 200, BackColor = Color.Transparent };
            sec.Paint += (s, e) => BookingFlowTheme.PaintSectionShell(e.Graphics, sec.ClientRectangle);
            var hdr = new Panel { Location = new Point(0, 0), Size = new Size(W, 64), BackColor = Color.Transparent };
            hdr.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 17f), AutoSize = true, Location = new Point(22, 17), BackColor = Color.Transparent });
            hdr.Controls.Add(new Label { Text = title, Font = new Font("Georgia", 13f, FontStyle.Bold), ForeColor = BookingFlowTheme.Cream, AutoSize = false, Size = new Size(W - 110, 22), Location = new Point(68, 12), BackColor = Color.Transparent });
            hdr.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(185, 248, 244, 239), AutoSize = false, Location = new Point(68, 36), Size = new Size(W - 116, 18), BackColor = Color.Transparent });
            sec.Controls.Add(hdr);
            var body = new Panel { Name = "body", Location = new Point(24, 72), Size = new Size(W - 48, 80), BackColor = Color.Transparent };
            sec.Controls.Add(body);
            sec.Tag = body;
            return sec;
        }

        private static Panel Body(Panel sec) => (Panel)sec.Tag;
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
            return c;
        }

        private Panel CounterCard(string title, string subtitle, int x, int w, ref int count, ref Label lbl)
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

            btnM.Click += (s, e) => { localCount = Math.Max(0, localCount - 1); lblCount.Text = localCount.ToString(); };
            btnP.Click += (s, e) => { localCount++; lblCount.Text = localCount.ToString(); };
            card.Controls.AddRange(new Control[] { btnM, lblCount, btnP });
            return card;
        }

        private Button SmallBtn(string text, int x, int y)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(32, 32) };
            BookingFlowTheme.StyleSmallButton(btn);
            return btn;
        }

        private Button ZoneToggleBtn(string text, int x, bool active)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(92, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? BookingFlowTheme.Dark : BookingFlowTheme.Cream,
                ForeColor = active ? BookingFlowTheme.Gold : BookingFlowTheme.TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(30, 0, 0, 0);
            return btn;
        }

        private static void UpdateZoneCheck(Panel zcard, bool selected)
        {
            foreach (Control c in zcard.Controls)
                if (c is Label lbl && lbl.Name == "chk")
                    lbl.ForeColor = selected ? BookingFlowTheme.Dark : Color.Transparent;
        }

        private void AddReviewRow(Panel parent, string label, string value, int y, int W)
        {
            parent.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W - 230, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Panel { Location = new Point(18, y + 24), Size = new Size(W - 36, 1), BackColor = Color.FromArgb(14, 0, 0, 0) });
        }
    }
}
