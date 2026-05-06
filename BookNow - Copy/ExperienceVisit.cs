using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Project.Accomodations;

namespace Project.Booking
{
    public partial class ExperienceVisit : UserControl
    {
        public event Action<Project.BookingSummary>? OnSummaryChanged;

        private readonly string[] _stepLabels = { "Experiences", "Schedule", "Guest Info", "Payment" };
        private Panel _stepBar = null!;

        // Step 1 controls
        private DateTimePicker _dtVisitDate = null!;
        private ComboBox _cboArrival = null!;
        private Label _lblAdults = null!, _lblChildren = null!;
        private int _adults = 2, _children = 0;
        private readonly List<Panel> _expCards = new();

        // Step 2 - time slots
        private readonly List<Panel> _slotCards = new();

        // Step 3 - guest info
        private TextBox _txtFirst = null!, _txtLast = null!, _txtEmail = null!, _txtPhone = null!;

        // Step 4 - payment + confirm combined
        private readonly List<Panel> _payCards = new();
        private Panel _pnlReviewCard = null!;
        private Panel _pnlTotalStrip = null!;
        private Button _btnConfirmNow = null!;
        private Panel _pnlConfirmed = null!;
        private Label _lblBid = null!;
        private System.Drawing.Bitmap _qrBitmap = null!;

        private int _currentStep = 1;
        private bool _uiBuilt = false;
        private string _paymentMethod = "Credit / Debit Card";
        private const int GAP = 14;

        public ExperienceVisit()
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
                foreach (var p in new[] { pnlExperiences, pnlSchedule, pnlGuestInfo, pnlPayment, pnlConfirm })
                    p.Size = new Size(w, h);
            };
            foreach (var p in new[] { pnlExperiences, pnlSchedule, pnlGuestInfo, pnlPayment, pnlConfirm })
            {
                p.Location = new Point(0, 80);
                p.Size = new Size(pnlContent.Width, Math.Max(pnlContent.Height - 80, 100));
                p.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                p.BackColor = BookingFlowTheme.Page;
            }
            pnlContent.Resize += (s, e) => resizePanels();

            BuildExperiencesStep();
            BuildScheduleStep();
            BuildGuestStep();
            BuildPaymentStep();
            ShowStep(1);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 1 — EXPERIENCES
        // ══════════════════════════════════════════════════════════
        private void BuildExperiencesStep()
        {
            var (stack, W) = ScrollStack(pnlExperiences);

            // ── Visit Date ──────────────────────────────────────
            var sDate = Sec("🗓️", "Visit Date", "Day of your experience visit — no overnight stay required", W);
            var dBody = Body(sDate);

            int fw = (W - GAP) / 2;
            dBody.Controls.Add(FL("VISIT DATE", 0, 0));
            _dtVisitDate = BookingFlowTheme.CreateDatePicker();
            _dtVisitDate.SetBounds(0, 20, fw, 38);
            dBody.Controls.Add(_dtVisitDate);

            dBody.Controls.Add(FL("APPROX. ARRIVAL", fw + GAP, 0));
            _cboArrival = CB(new[] { "8:00 AM", "9:00 AM", "10:00 AM", "1:00 PM", "3:00 PM" });
            _cboArrival.SetBounds(fw + GAP, 20, fw, 38);
            dBody.Controls.Add(_cboArrival);

            dBody.Height = 66;
            sDate.Height = 66 + dBody.Height + 20;
            stack.Controls.Add(sDate);

            // ── Guests ──────────────────────────────────────────
            var sGuests = Sec("👥", "Guests", "Experience pricing is per person", W);
            var gBody = Body(sGuests);
            int cW = (W - GAP) / 2;
            gBody.Controls.Add(CounterCard("Adults", "Age 13 and above", 0, cW, ref _adults, ref _lblAdults));
            gBody.Controls.Add(CounterCard("Children", "Age 4–12", cW + GAP, cW, ref _children, ref _lblChildren));
            gBody.Height = 80;
            sGuests.Height = 66 + gBody.Height + 20;
            stack.Controls.Add(sGuests);

            // ── Choose Experiences ───────────────────────────────
            var sExp = Sec("🌿", "Choose Your Experiences", "All 5 signature experiences · Select one or more", W);
            var eBody = Body(sExp);

            var experiences = new (string Icon, string Name, string Detail, int Price)[]
            {
                ("🍖","Animal Feeding Session","45 min · Savanna Zone · Max 10 · Ranger guided",500),
                ("🌙","Night Safari Tour","2 hrs · All 8 zones · Max 15 · After dark",800),
                ("🧑‍🌾","Keeper Experience","3 hrs · Behind the scenes · Max 6 guests",1200),
                ("📸","Photo Opportunity","30 min · Aviary Dome · Max 12 guests",3500),
                ("🌅","Sunrise Safari Walk","90 min · Forest Zone · Max 8 · 5 slots available",600),
            };

            int ecH = 68;
            for (int i = 0; i < experiences.Length; i++)
            {
                var (icon, name, detail, price) = experiences[i];
                var ec = new Panel
                {
                    Location = new Point(0, i * (ecH + 10)),
                    Size = new Size(W - 48, ecH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = false
                };
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

                // Icon box
                var iconBox = new Panel { Location = new Point(12, 10), Size = new Size(48, 48), BackColor = Color.FromArgb(15, 7, 26, 14) };
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

                // Check circle
                var chkCircle = new Panel { Location = new Point(ec.Width - 36, (ecH - 22) / 2), Size = new Size(22, 22), BackColor = Color.Transparent, Name = "chk" };
                chkCircle.Paint += (ss, ee) =>
                {
                    bool sel = (bool)ec.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (sel)
                    {
                        ee.Graphics.FillEllipse(new SolidBrush(BookingFlowTheme.Gold), 1, 1, 19, 19);
                        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        ee.Graphics.DrawString("✓", new Font("Segoe UI", 9f, FontStyle.Bold), new SolidBrush(BookingFlowTheme.Dark), new RectangleF(1, 1, 19, 19), sf);
                    }
                    else
                    {
                        ee.Graphics.DrawEllipse(new Pen(Color.FromArgb(40, 0, 0, 0), 1.5f), 1, 1, 19, 19);
                    }
                };
                ec.Controls.Add(chkCircle);

                EventHandler toggle = (ss, ee) =>
                {
                    ec.Tag = !(bool)ec.Tag;
                    ec.Invalidate();
                    foreach (Control c in ec.Controls) c.Invalidate();
                };
                ec.Click += toggle;
                foreach (Control c in ec.Controls) c.Click += toggle;
                iconBox.Click += toggle;
                foreach (Control c in iconBox.Controls) c.Click += toggle;

                _expCards.Add(ec);
                eBody.Controls.Add(ec);
            }

            eBody.Height = experiences.Length * (ecH + 10);
            sExp.Height = 66 + eBody.Height + 20;
            stack.Controls.Add(sExp);

            var nav = NavBar(W);
            nav.Controls.Add(MetaLbl("Step 1 of 4 — pick experiences", 0));
            var btnCont = BookingFlowTheme.CreatePrimaryButton("Continue — Schedule →");
            btnCont.Location = new Point(W - btnCont.Width, 8);
            btnCont.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(btnCont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 2 — SCHEDULE
        // ══════════════════════════════════════════════════════════
        private void BuildScheduleStep()
        {
            var (stack, W) = ScrollStack(pnlSchedule);

            var sec = Sec("⏰", "Pick a Time Slot", "Available slots from tour schedule", W);
            var body = Body(sec);

            var slots = new (string Time, string Label, string Avail)[]
            {
                ("8:00 AM","Morning session","7 slots left"),
                ("11:00 AM","Midday session","4 slots left"),
                ("3:00 PM","Afternoon session","2 slots left"),
            };

            int slW = (W - GAP * 2 - 48) / 3, slH = 70;

            for (int i = 0; i < slots.Length; i++)
            {
                var (time, label, avail) = slots[i];
                bool isFirst = i == 0;
                var sl = new Panel
                {
                    Location = new Point(i * (slW + GAP), 0),
                    Size = new Size(slW, slH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = isFirst
                };
                sl.Paint += (ss, ee) =>
                {
                    bool sel = (bool)sl.Tag;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, sl.Width - 1, sl.Height - 1), 8);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    ee.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    ee.Graphics.DrawPath(pen, path);
                };

                var sf2 = new StringFormat { Alignment = StringAlignment.Center };
                sl.Controls.Add(new Label { Text = time, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, Size = new Size(slW, 22), Location = new Point(0, 10), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
                sl.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(slW, 16), Location = new Point(0, 30), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
                sl.Controls.Add(new Label { Text = avail, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Success, Size = new Size(slW, 14), Location = new Point(0, 48), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

                EventHandler choose = (ss, ee) =>
                {
                    foreach (var sc in _slotCards) { sc.Tag = false; sc.Invalidate(); }
                    sl.Tag = true; sl.Invalidate();
                };
                sl.Click += choose;
                foreach (Control c in sl.Controls) c.Click += choose;

                _slotCards.Add(sl);
                body.Controls.Add(sl);
            }

            body.Height = slH + 10;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(1);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 2 of 4 — schedule", W / 2 - 80));
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

            int fw = (W - GAP) / 2, fh = 38, rH = 70;
            var sec = Sec("👤", "Primary Guest Information", "Lead guest must be 18 or older", W);
            var body = Body(sec);

            _txtFirst = TB("Juan"); AddField(body, "FIRST NAME", _txtFirst, 0, 0, fw, fh);
            _txtLast = TB("Dela Cruz"); AddField(body, "LAST NAME", _txtLast, fw + GAP, 0, fw, fh);
            _txtEmail = TB("juan@email.com"); AddField(body, "EMAIL ADDRESS", _txtEmail, 0, rH, fw, fh);
            _txtPhone = TB("09XX XXX XXXX"); AddField(body, "MOBILE NUMBER", _txtPhone, fw + GAP, rH, fw, fh);

            body.Height = rH * 2 + 14;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 3 of 4 — guest info", W / 2 - 80));
            var cont = BookingFlowTheme.CreatePrimaryButton("Continue — Payment →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(4);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 4 — PAYMENT + REVIEW & CONFIRM
        // ══════════════════════════════════════════════════════════
        private void BuildPaymentStep()
        {
            var (stack, W) = ScrollStack(pnlPayment);

            // Payment section
            var secPay = Sec("📋", "Review & Pay", "Complete your experience booking", W);
            var payBody = Body(secPay);

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
                };
                card.Click += choose;
                foreach (Control c in card.Controls) c.Click += choose;
                _payCards.Add(card);
                payBody.Controls.Add(card);
            }

            // Review block
            int rvY = 2 * (pmH + 10) + 16;
            _pnlReviewCard = new Panel { Location = new Point(0, rvY), Size = new Size(W - 48, 148), BackColor = Color.White };
            _pnlReviewCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlReviewCard.Width - 1, _pnlReviewCard.Height - 1), 10);
                using var b = new SolidBrush(Color.White); e.Graphics.FillPath(b, path);
                using var pen = new Pen(BookingFlowTheme.Border, 1f); e.Graphics.DrawPath(pen, path);
            };

            var rvHdr = new Panel { Location = new Point(0, 0), Size = new Size(W - 48, 36), BackColor = BookingFlowTheme.Cream2 };
            rvHdr.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var gp = new GraphicsPath();
                int d = 20; int rW = rvHdr.Width;
                gp.AddArc(0, 0, d, d, 180, 90); gp.AddArc(rW - d, 0, d, d, 270, 90);
                gp.AddLine(rW, rvHdr.Height, 0, rvHdr.Height); gp.CloseFigure();
                using var b = new SolidBrush(BookingFlowTheme.Cream2); e.Graphics.FillPath(b, gp);
            };
            rvHdr.Controls.Add(new Label { Text = "🌿  EXPERIENCE SUMMARY", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(16, 10), BackColor = Color.Transparent });
            _pnlReviewCard.Controls.Add(rvHdr);

            int rvW = W - 48;
            AddReviewRow(_pnlReviewCard, "Experience(s)", "—", 40, rvW);
            AddReviewRow(_pnlReviewCard, "Guests", "2 Adults", 70, rvW);
            AddReviewRow(_pnlReviewCard, "Day entry fee", "₱200", 100, rvW);
            AddReviewRow(_pnlReviewCard, "Conservation fee", "₱200", 130, rvW);
            payBody.Controls.Add(_pnlReviewCard);

            // Dark total strip
            _pnlTotalStrip = new Panel { Location = new Point(0, rvY + 158), Size = new Size(W - 48, 56), BackColor = BookingFlowTheme.Dark };
            _pnlTotalStrip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlTotalStrip.Width - 1, _pnlTotalStrip.Height - 1), 10);
                using var b = new SolidBrush(BookingFlowTheme.Dark); e.Graphics.FillPath(b, path);
            };
            _pnlTotalStrip.Controls.Add(new Label { Text = "Total amount due", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 248, 244, 239), AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent });
            _pnlTotalStrip.Controls.Add(new Label { Text = "₱0", Font = new Font("Georgia", 16f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(rvW - 130, 14), BackColor = Color.Transparent });
            payBody.Controls.Add(_pnlTotalStrip);

            // Terms
            int termsY = rvY + 158 + 66;
            payBody.Controls.Add(new CheckBox { Location = new Point(0, termsY), AutoSize = false, Size = new Size(W - 48, 36), Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextMuted, Text = "I agree to the WildNest Terms & Conditions and Cancellation Policy.", BackColor = Color.Transparent });

            // Confirm button
            _btnConfirmNow = new Button
            {
                Text = "✓  Confirm Experience Booking",
                Location = new Point(0, termsY + 46),
                Size = new Size(W - 48, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = BookingFlowTheme.Dark,
                ForeColor = BookingFlowTheme.Gold,
                Font = new Font("Georgia", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConfirmNow.FlatAppearance.BorderSize = 0;
            _btnConfirmNow.Click += (s, e) => ConfirmBooking();
            payBody.Controls.Add(_btnConfirmNow);

            payBody.Height = termsY + 104;
            secPay.Height = 66 + payBody.Height + 20;
            stack.Controls.Add(secPay);

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
            var pnl = new Panel { Size = new Size(W, 548), BackColor = Color.Transparent };

            var circle = new Panel { Location = new Point(W / 2 - 36, -4), Size = new Size(72, 72), BackColor = Color.Transparent };
            circle.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(BookingFlowTheme.Success), 0, 0, 71, 71);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("✓", new Font("Segoe UI", 22f, FontStyle.Bold), Brushes.White, new RectangleF(0, 0, 72, 72), sf);
            };
            pnl.Controls.Add(circle);
            pnl.Controls.Add(new Label { Text = "Experience Booked!", Font = new Font("Georgia", 20f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W / 2 - 120, 70), BackColor = Color.Transparent });
            pnl.Controls.Add(new Label { Text = "Your wildlife experience is reserved. See you at WildNest!", Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(W, 34), Location = new Point(0, 104), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });

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
            pnlExperiences.Visible = step == 1;
            pnlSchedule.Visible = step == 2;
            pnlGuestInfo.Visible = step == 3;
            pnlPayment.Visible = step == 4;
            pnlConfirm.Visible = false; // hidden - confirm is inside pnlPayment

            if (step == 4)
            {
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = true;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = true;
                if (_btnConfirmNow != null) _btnConfirmNow.Visible = true;
                if (_pnlConfirmed != null) _pnlConfirmed.Visible = false;
            }
            _stepBar?.Invalidate();
        }
        private const string _connStr = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";
        private void ConfirmBooking()
        {
            if (string.IsNullOrWhiteSpace(_txtFirst?.Text) || string.IsNullOrWhiteSpace(_txtEmail?.Text))
            { MessageBox.Show("Please fill in guest information first."); ShowStep(3); return; }

            try
            {
                using var conn = new MySqlConnection(_connStr);
                conn.Open();

                // 1. Insert guest
                var gCmd = new MySqlCommand(@"
            INSERT INTO tbl_Guests (FirstName, LastName, Email, Phone)
            VALUES (@fn, @ln, @em, @ph);", conn);
                gCmd.Parameters.AddWithValue("@fn", _txtFirst.Text.Trim());
                gCmd.Parameters.AddWithValue("@ln", _txtLast?.Text.Trim() ?? "");
                gCmd.Parameters.AddWithValue("@em", _txtEmail.Text.Trim());
                gCmd.Parameters.AddWithValue("@ph", _txtPhone?.Text.Trim() ?? "");
                gCmd.ExecuteNonQuery();

                int guestId = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID();", conn).ExecuteScalar());

                // 2. Generate Booking ID
                string bid = BookingIdGenerator.NewId();

                // 3. Count selected experiences and calc total
                int selectedCount = _expCards.Count(c => (bool)c.Tag);
                decimal total = selectedCount * 200 + (_adults + _children) * 200;

                // 4. Insert reservation
                var rCmd = new MySqlCommand(@"
            INSERT INTO tbl_Reservations
                (ReservationID, GuestID, BookingType, VisitDate, NumAdults, NumChildren, TotalAmount)
            VALUES (@rid, @gid, 'ExperienceVisit', @vd, @adults, @children, @total);", conn);
                rCmd.Parameters.AddWithValue("@rid", bid);
                rCmd.Parameters.AddWithValue("@gid", guestId);
                rCmd.Parameters.AddWithValue("@vd", _dtVisitDate.Value.Date);
                rCmd.Parameters.AddWithValue("@adults", _adults);
                rCmd.Parameters.AddWithValue("@children", _children);
                rCmd.Parameters.AddWithValue("@total", total);
                rCmd.ExecuteNonQuery();

                // 5. Insert selected experiences
                var experiences = new (string Name, int Price)[]
                {
            ("Animal Feeding Session", 500),
            ("Night Safari Tour",      800),
            ("Keeper Experience",      1200),
            ("Photo Opportunity",      3500),
            ("Sunrise Safari Walk",    600),
                };

                for (int i = 0; i < _expCards.Count && i < experiences.Length; i++)
                {
                    if ((bool)_expCards[i].Tag)
                    {
                        var expCmd = new MySqlCommand(
                            "SELECT ExperienceID FROM tbl_Experiences WHERE ExperienceName = @n LIMIT 1;", conn);
                        expCmd.Parameters.AddWithValue("@n", experiences[i].Name);
                        var expIdObj = expCmd.ExecuteScalar();
                        if (expIdObj != null)
                        {
                            var linkCmd = new MySqlCommand(@"
                        INSERT INTO tbl_BookingExperiences (ReservationID, ExperienceID, Quantity, TotalCost)
                        VALUES (@rid, @eid, 1, (SELECT PricePerPerson FROM tbl_Experiences WHERE ExperienceID = @eid));", conn);
                            linkCmd.Parameters.AddWithValue("@rid", bid);
                            linkCmd.Parameters.AddWithValue("@eid", Convert.ToInt32(expIdObj));
                            linkCmd.ExecuteNonQuery();
                        }
                    }
                }

                // 6. Insert payment
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
                        bid, "Experience Visit", $"Visit Date: {_dtVisitDate.Value:MMM dd, yyyy}", (decimal)(_expCards.Count(cx => (bool)cx.Tag) * 200 + (_adults + _children) * 200), _paymentMethod, (System.Drawing.Bitmap)_qrBitmap.Clone()));

                // Show success UI
                if (_lblBid != null) _lblBid.Text = bid;
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = false;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = false;
                if (_btnConfirmNow != null) _btnConfirmNow.Visible = false;
                if (_pnlConfirmed != null) _pnlConfirmed.Visible = true;
                _currentStep = 5;
                _stepBar?.Invalidate();
                OnSummaryChanged?.Invoke(new Project.BookingSummary());
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

        private void AddReviewRow(Panel parent, string label, string value, int y, int W)
        {
            parent.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(W - 210, y + 4), BackColor = Color.Transparent });
            parent.Controls.Add(new Panel { Location = new Point(18, y + 24), Size = new Size(W - 36, 1), BackColor = Color.FromArgb(14, 0, 0, 0) });
        }
    }
}
