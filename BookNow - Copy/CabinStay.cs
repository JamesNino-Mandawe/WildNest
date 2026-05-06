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
    public partial class CabinStay : UserControl
    {
        // ── Summary callback ─────────────────────────────────────
        public event Action<Project.BookingSummary>? OnSummaryChanged;

        // ── Step labels ──────────────────────────────────────────
        private readonly string[] _stepLabels = { "Cabin", "Guest Info", "Payment", "Confirm" };

        // ── UI controls ──────────────────────────────────────────
        private Panel _stepBar = null!;
        private DateTimePicker _dtCheckIn = null!;
        private DateTimePicker _dtCheckOut = null!;
        private Label _lblNights = null!;
        private Label _lblAdults = null!;
        private Label _lblChildren = null!;
        private const string _connStr = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";
        // Guest Info fields
        private TextBox _txtFirst = null!, _txtLast = null!,
                         _txtEmail = null!, _txtPhone = null!,
                         _txtRequests = null!;
        private ComboBox _cboNationality = null!, _cboIdType = null!,
                         _cboArrival = null!, _cboTransport = null!;

        // Payment fields
        private readonly List<Panel> _payCards = new();
        private Panel _pnlCardFields = null!;
        private TextBox _txtCardNum = null!, _txtCardName = null!,
                        _txtExpiry = null!, _txtCvv = null!;

        // Confirm panels / labels
        private Panel _pnlReviewCard = null!;   // white review card
        private Panel _pnlTotalStrip = null!;   // dark total bar
        private Panel _pnlEmailNotice = null!;   // blue email row
        private Button _btnConfirmNow = null!;
        private Panel _pnlConfirmed = null!;   // success view
        private Label _lblBid = null!;
        private System.Drawing.Bitmap _qrBitmap = null!;

        // Review value labels (updated in RefreshReview)
        private Label _rvCabin = null!, _rvCheckIn = null!, _rvCheckOut = null!, _rvGuests = null!, _rvTotal = null!;

        // ── State ────────────────────────────────────────────────
        private int _currentStep = 1;
        private bool _uiBuilt = false;
        private int _adults = 2;
        private int _children = 0;
        private string _selectedCabin = "";
        private int _cabinRate = 0;
        private readonly Dictionary<string, int> _addons = new();
        private string _paymentMethod = "Credit / Debit Card";

        // ── Layout constants ─────────────────────────────────────
        private const int GAP = 14;

        // ══════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════════
        public CabinStay()
        {
            InitializeComponent();
            DoubleBuffered = true;
            BackColor = BookingFlowTheme.Page;

            HandleCreated += (s, e) => BeginInvoke(new Action(BuildUi));
            VisibleChanged += (s, e) => { if (Visible && !_uiBuilt) BeginInvoke(new Action(BuildUi)); };
            Resize += (s, e) => { if (!_uiBuilt) BeginInvoke(new Action(BuildUi)); };
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD  (fires once, deferred so Width is valid)
        // ══════════════════════════════════════════════════════════
        private void BuildUi()
        {
            if (_uiBuilt || Width < 100) return;
            _uiBuilt = true;

            pnlContent.BackColor = BookingFlowTheme.Page;

            // ── Step bar ─────────────────────────────────────────
            _stepBar = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = BookingFlowTheme.Dark };
            _stepBar.Paint += (s, e) =>
                BookingFlowTheme.PaintStepBar(e.Graphics, _stepBar.ClientRectangle, _stepLabels, _currentStep);
            pnlContent.Controls.Add(_stepBar);
            pnlContent.Controls.SetChildIndex(_stepBar, 0);

            // ── Size step panels below step bar ──────────────────
            Action resizePanels = () =>
            {
                int w = pnlContent.Width;
                int h = Math.Max(pnlContent.Height - 80, 100);
                foreach (var p in new[] { pnlCabin, pnlGuestInfo, pnlPayment, pnlConfirm })
                    p.Size = new Size(w, h);
            };
            foreach (var p in new[] { pnlCabin, pnlGuestInfo, pnlPayment, pnlConfirm })
            {
                p.Location = new Point(0, 80);
                p.Size = new Size(pnlContent.Width, Math.Max(pnlContent.Height - 80, 100));
                p.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                p.BackColor = BookingFlowTheme.Page;
            }
            pnlContent.Resize += (s, e) => resizePanels();

            BuildCabinStep();
            BuildGuestStep();
            BuildPaymentStep();
            BuildConfirmStep();
            ShowStep(1);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 1 — CABIN
        // ══════════════════════════════════════════════════════════
        private void BuildCabinStep()
        {
            var (stack, W) = ScrollStack(pnlCabin);

            // ── Dates ──────────────────────────────────────────
            var sDate = Sec("📅", "Select Your Dates",
                "Minimum 1 night · Free cancellation up to 48 hours before check-in", W);
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

            // Nights badge
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
            sDate.Height = 66 + dBody.Height + 20;
            stack.Controls.Add(sDate);

            // ── Guests ─────────────────────────────────────────
            var sGuests = Sec("👥", "Guests", "Number of adults and children staying", W);
            var gBody = Body(sGuests);
            int cW = (W - GAP) / 2;
            gBody.Controls.Add(CounterCard("Adults", "Age 13 and above", 0, cW));
            gBody.Controls.Add(CounterCard("Children", "Age 4–12 · ₱500 resort fee", cW + GAP, cW));
            gBody.Height = 80;
            sGuests.Height = 66 + gBody.Height + 20;
            stack.Controls.Add(sGuests);

            // ── Cabin cards ─────────────────────────────────────
            var sCabin = Sec("🏕️", "Choose Your Cabin", "All 10 cabins · Select one to continue", W);
            var cBody = Body(sCabin);

            var cabins = new (string N, string Meta, int Price, string Status)[]
            {
                ("River Shell",       "2 guests · Teardrop pod · River overlook",    14500, "Available"),
                ("Forest Nest",       "2 guests · Deep forest canopy",               18200, "Available"),
                ("Cliff Edge",        "2 guests · Panoramic · Glass-bottom balcony", 22800, "Available"),
                ("Glass Manor",       "4 guests · Premium villa · 360° views",       35000, "Available"),
                ("Safari Tent Alpha", "2 guests · Savanna view",                      5200, "Available"),
                ("Lakeside Lodge",    "3 guests · Waterfront",                        6500, "Available"),
                ("Treetop Treehouse", "2 guests · Forest view",                       4500, "Available"),
                ("Eco Bungalow",      "2 guests · Solar-powered",                     2500, "Last room"),
                ("Forest Cabin A",    "4 guests · Nature trail access",               3500, "Available"),
                ("Sanctuary Villa",   "6 guests · Private pool",                     11000, "Booked"),
            };

            var cabinCardList = new List<Panel>();
            int ccW = (W - GAP) / 2;
            int ccH = 80;

            for (int i = 0; i < cabins.Length; i++)
            {
                var (name, meta, price, status) = cabins[i];
                bool booked = status == "Booked";
                Color stClr = status == "Available" ? BookingFlowTheme.Success
                            : status == "Last room" ? BookingFlowTheme.Warning
                            : BookingFlowTheme.Danger;

                var card = new Panel
                {
                    Location = new Point((i % 2) * (ccW + GAP), (i / 2) * (ccH + 10)),
                    Size = new Size(ccW, ccH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = booked ? Cursors.Default : Cursors.Hand,
                    Tag = false
                };
                card.Paint += (s, e) =>
                {
                    bool sel = (bool)card.Tag;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    e.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    e.Graphics.DrawPath(pen, path);
                };

                card.Controls.Add(new Label { Text = $"● {status}", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = stClr, AutoSize = true, Location = new Point(14, 10), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = name, Font = new Font("Georgia", 10.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(14, 26), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = meta, Font = new Font("Segoe UI", 7.9f), ForeColor = BookingFlowTheme.TextDim, Size = new Size(ccW - 120, 16), Location = new Point(14, 48), BackColor = Color.Transparent });
                card.Controls.Add(new Label { Text = booked ? "Fully Booked" : $"₱{price:N0}/night", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = booked ? BookingFlowTheme.TextDim : BookingFlowTheme.Gold, AutoSize = true, Location = new Point(ccW - 108, 30), BackColor = Color.Transparent });

                if (!booked)
                {
                    int capturedIdx = i;
                    EventHandler choose = (ss, ee) =>
                    {
                        _selectedCabin = name;
                        _cabinRate = price;
                        foreach (var cc in cabinCardList) { cc.Tag = false; cc.Invalidate(); }
                        card.Tag = true; card.Invalidate();
                        FireSummary();
                    };
                    card.Click += choose;
                    foreach (Control c in card.Controls) c.Click += choose;
                }

                cabinCardList.Add(card);
                cBody.Controls.Add(card);
            }

            cBody.Height = ((cabins.Length + 1) / 2) * (ccH + 10);
            sCabin.Height = 66 + cBody.Height + 20;
            stack.Controls.Add(sCabin);

            // ── Add-ons ─────────────────────────────────────────
            var sAddon = Sec("✨", "Cabin Add-ons", "Optional extras to enhance your stay", W);
            var aBody = Body(sAddon);

            var addons = new (string Name, string Desc, int Price)[]
            {
                ("Gourmet Picnic",   "Artisan basket for two with local wine",   4500),
                ("Stargazing Kit",   "Telescope rental and sky map",              1200),
                ("Photography Pro",  "Dedicated photographer · 2-hour session",  5500),
                ("Welcome Basket",   "Local snacks and flowers",                   350),
                ("Breakfast for 2",  "In-cabin breakfast service",                 600),
                ("Romantic Setup",   "Rose petals, candles and wine",             1200),
                ("Early Check-in",   "From 10:00 AM",                            1500),
                ("Late Check-out",   "Until 4:00 PM",                            1500),
                ("Airport Transfer", "Cebu City ↔ WildNest",                     2800),
                ("Travel Insurance", "Covers trip interruption",                   450),
            };

            int aCols = 3, aGap = 12;
            int aW = (W - aGap * (aCols - 1)) / aCols, aH = 86;

            for (int i = 0; i < addons.Length; i++)
            {
                var (aname, adesc, aprice) = addons[i];
                int col = i % aCols, row = i / aCols;
                var ac = new Panel
                {
                    Location = new Point(col * (aW + aGap), row * (aH + aGap)),
                    Size = new Size(aW, aH),
                    BackColor = BookingFlowTheme.Cream,
                    Cursor = Cursors.Hand,
                    Tag = false
                };
                ac.Paint += (s, e) =>
                {
                    bool sel = (bool)ac.Tag;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, ac.Width - 1, ac.Height - 1), 10);
                    using var b = new SolidBrush(sel ? Color.FromArgb(12, 212, 160, 23) : BookingFlowTheme.Cream);
                    e.Graphics.FillPath(b, path);
                    using var pen = sel ? new Pen(BookingFlowTheme.Gold, 1.8f) : new Pen(BookingFlowTheme.Border, 1f);
                    e.Graphics.DrawPath(pen, path);
                };
                ac.Controls.Add(new Label { Text = aname, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(12, 12), BackColor = Color.Transparent });
                ac.Controls.Add(new Label { Text = adesc, Font = new Font("Segoe UI", 7.7f), ForeColor = BookingFlowTheme.TextDim, Size = new Size(aW - 24, 18), Location = new Point(12, 32), BackColor = Color.Transparent });
                ac.Controls.Add(new Label { Text = $"+₱{aprice:N0}", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(12, 58), BackColor = Color.Transparent });

                EventHandler toggle = (ss, ee) =>
                {
                    bool cur = (bool)ac.Tag;
                    ac.Tag = !cur;
                    if (!cur) _addons[aname] = aprice; else _addons.Remove(aname);
                    ac.Invalidate(); FireSummary();
                };
                ac.Click += toggle;
                foreach (Control c in ac.Controls) c.Click += toggle;
                aBody.Controls.Add(ac);
            }

            aBody.Height = ((addons.Length + aCols - 1) / aCols) * (aH + aGap);
            sAddon.Height = 66 + aBody.Height + 20;
            stack.Controls.Add(sAddon);

            // ── Nav ────────────────────────────────────────────
            var nav = NavBar(W);
            nav.Controls.Add(MetaLbl("Step 1 of 4 — dates, guests and cabin", 0));
            var btnCont = BookingFlowTheme.CreatePrimaryButton("Continue — Guest Info →");
            btnCont.Location = new Point(W - btnCont.Width, 8);
            btnCont.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(btnCont);
            stack.Controls.Add(nav);

            SyncNights();
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 2 — GUEST INFO  (matches screenshot 1)
        // ══════════════════════════════════════════════════════════
        private void BuildGuestStep()
        {
            var (stack, W) = ScrollStack(pnlGuestInfo);

            int fw = (W - GAP) / 2;
            int fh = 38;
            int rH = 70;   // label(20) + field(38) + gap(12)

            var sec = Sec("👤", "Primary Guest Information", "The lead guest must be 18 or older", W);
            var body = Body(sec);

            // Row 0
            _txtFirst = TB("Juan"); AddField(body, "FIRST NAME", _txtFirst, 0, 0, fw, fh);
            _txtLast = TB("Dela Cruz"); AddField(body, "LAST NAME", _txtLast, fw + GAP, 0, fw, fh);
            // Row 1
            _txtEmail = TB("juan@email.com"); AddField(body, "EMAIL ADDRESS", _txtEmail, 0, rH, fw, fh);
            _txtPhone = TB("09XX XXX XXXX"); AddField(body, "MOBILE NUMBER", _txtPhone, fw + GAP, rH, fw, fh);
            // Row 2
            _cboNationality = CB(new[] { "Filipino", "Foreign national" });
            _cboIdType = CB(new[] { "Philippine Passport", "Driver's License", "PhilSys / National ID", "UMID", "Foreign Passport" });
            AddField(body, "NATIONALITY", _cboNationality, 0, rH * 2, fw, fh);
            AddField(body, "VALID ID TYPE", _cboIdType, fw + GAP, rH * 2, fw, fh);

            // Arrival details divider
            int divY = rH * 3 + 2;
            body.Controls.Add(new Label { Text = "─── ARRIVAL DETAILS ───", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(0, divY), BackColor = Color.Transparent });

            // Row 3: Arrival | Transport
            int row3Y = divY + 24;
            _cboArrival = CB(new[] { "Before 12:00 PM", "12:00 PM – 2:00 PM", "2:00 PM – 4:00 PM", "4:00 PM – 6:00 PM", "After 6:00 PM" });
            _cboTransport = CB(new[] { "Private vehicle", "Hired van / bus", "Taxi / TNVS", "Public transport" });
            AddField(body, "ESTIMATED ARRIVAL TIME", _cboArrival, 0, row3Y, fw, fh);
            AddField(body, "MODE OF TRANSPORT", _cboTransport, fw + GAP, row3Y, fw, fh);

            // Special requests
            int reqY = row3Y + rH;
            int bW = W - 48;
            body.Controls.Add(FL("SPECIAL REQUESTS (OPTIONAL)", 0, reqY));
            _txtRequests = new TextBox
            {
                Location = new Point(0, reqY + 20),
                Size = new Size(bW, 76),
                Multiline = true,
                BackColor = BookingFlowTheme.Cream,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Any special requests or notes for the resort..."
            };
            body.Controls.Add(_txtRequests);

            // Send confirmation checkbox row
            int chkRowY = reqY + 20 + 76 + 12;
            var chkRow = new Panel { Location = new Point(0, chkRowY), Size = new Size(bW, 48), BackColor = Color.Transparent };
            chkRow.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, chkRow.Width - 1, chkRow.Height - 1), 8);
                using var b = new SolidBrush(Color.FromArgb(9, 65, 130, 210)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(38, 65, 130, 210), 1f); e.Graphics.DrawPath(pen, path);
            };
            chkRow.Controls.Add(new Label { Text = "📧", Font = new Font("Segoe UI Emoji", 13), AutoSize = true, Location = new Point(14, 13), BackColor = Color.Transparent });
            chkRow.Controls.Add(new Label { Text = "Send booking confirmation to email", Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = BookingFlowTheme.Dark, AutoSize = true, Location = new Point(44, 8), BackColor = Color.Transparent });
            chkRow.Controls.Add(new Label { Text = "Your Booking ID and QR Code will be sent to the email above", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(44, 28), BackColor = Color.Transparent });
            var chk = new CheckBox { Checked = true, Location = new Point(bW - 30, 14), Size = new Size(20, 20), BackColor = Color.Transparent };
            chkRow.Controls.Add(chk);
            body.Controls.Add(chkRow);

            body.Height = chkRowY + 48 + 8;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            // Nav
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
        //  STEP 3 — PAYMENT  (matches screenshot 2)
        // ══════════════════════════════════════════════════════════
        private void BuildPaymentStep()
        {
            var (stack, W) = ScrollStack(pnlPayment);

            var sec = Sec("💳", "Choose Payment Method", "All transactions are secured and encrypted", W);
            var body = Body(sec);

            var methods = new (string Icon, string Name, string Sub)[]
            {
                ("💳", "Credit / Debit Card", "Visa, Mastercard, JCB"),
                ("📱", "GCash / Maya",         "E-wallet transfer"),
                ("🏦", "Bank Transfer",        "BDO, BPI, Metrobank"),
                ("🏨", "Pay at Resort",        "Cash or card at front desk"),
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

            // Card fields
            int cfY = 2 * (pmH + 10) + 14;
            int bW = W - 48;
            _pnlCardFields = new Panel
            {
                Location = new Point(0, cfY),
                Size = new Size(bW, 220),
                BackColor = Color.FromArgb(8, 212, 160, 23),
                Visible = true
            };
            _pnlCardFields.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlCardFields.Width - 1, _pnlCardFields.Height - 1), 10);
                using var b = new SolidBrush(Color.FromArgb(8, 212, 160, 23)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(55, 212, 160, 23), 1.5f); e.Graphics.DrawPath(pen, path);
            };
            _pnlCardFields.Controls.Add(new Label { Text = "CARD DETAILS", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(18, 14), BackColor = Color.Transparent });

            int cf = bW - 36, hf = (cf - GAP) / 2, qf = (hf - GAP) / 2;

            // Card number (full width)
            _pnlCardFields.Controls.Add(FL("CARD NUMBER", 18, 34));
            _txtCardNum = TB("1234  5678  9012  3456"); _txtCardNum.SetBounds(18, 54, cf, 38);
            _pnlCardFields.Controls.Add(_txtCardNum);

            // Cardholder | Expiry | CVV
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
                ("✅", "Free cancellation",  "Cancel up to 48 hours before check-in with full refund"),
                ("🌿", "Conservation fee",   "₱200 per booking goes directly to Philippine wildlife conservation"),
                ("⚠️", "Health alert policy","If an animal is flagged, an alternative experience is offered at no charge"),
            })
            {
                var pol = PolicyRow(badge, title, desc, polY, bW);
                body.Controls.Add(pol);
                polY += pol.Height + 10;
            }

            // Terms
            int termsY = polY + 6;
            body.Controls.Add(new CheckBox { Location = new Point(0, termsY), AutoSize = false, Size = new Size(bW, 44), Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextMuted, Text = "I agree to the WildNest Terms & Conditions and Cancellation Policy. I acknowledge that wildlife encounters are subject to animal health and weather conditions beyond WildNest's control.", BackColor = Color.Transparent });

            body.Height = termsY + 50;
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            // Nav
            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(2);
            nav.Controls.Add(back);
            nav.Controls.Add(MetaLbl("Step 3 of 4 — payment method", W / 2 - 100));
            var cont = BookingFlowTheme.CreatePrimaryButton("Review Booking →");
            cont.Location = new Point(W - cont.Width, 8); cont.Click += (s, e) => ShowStep(4);
            nav.Controls.Add(cont);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  STEP 4 — CONFIRM  (matches screenshot 3 & 4)
        // ══════════════════════════════════════════════════════════
        private void BuildConfirmStep()
        {
            var (stack, W) = ScrollStack(pnlConfirm);

            // ── Review section ──────────────────────────────────
            var sec = Sec("📋", "Review Your Booking", "Please confirm all details before submitting", W);
            var body = Body(sec);

            // White card with cabin review rows
            int bW = W - 48;
            _pnlReviewCard = new Panel { Location = new Point(0, 0), Size = new Size(bW, 186), BackColor = Color.White };
            _pnlReviewCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlReviewCard.Width - 1, _pnlReviewCard.Height - 1), 10);
                using var b = new SolidBrush(Color.White); e.Graphics.FillPath(b, path);
                using var pen = new Pen(BookingFlowTheme.Border, 1f); e.Graphics.DrawPath(pen, path);
            };

            // Cream2 header
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
            hdr.Controls.Add(new Label { Text = "🏕  CABIN", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(16, 10), BackColor = Color.Transparent });
            _pnlReviewCard.Controls.Add(hdr);

            // Review rows — value labels stored so RefreshReview can update them
            _rvCabin = AddReviewRow(_pnlReviewCard, "Cabin", "—", 40, bW);
            _rvCheckIn = AddReviewRow(_pnlReviewCard, "Check-in", "—", 70, bW);
            _rvCheckOut = AddReviewRow(_pnlReviewCard, "Check-out", "—", 100, bW);
            _rvGuests = AddReviewRow(_pnlReviewCard, "Guests", "2 Adults", 130, bW);
            body.Controls.Add(_pnlReviewCard);

            // Dark total strip
            _pnlTotalStrip = new Panel { Location = new Point(0, 196), Size = new Size(bW, 56), BackColor = BookingFlowTheme.Dark };
            _pnlTotalStrip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlTotalStrip.Width - 1, _pnlTotalStrip.Height - 1), 10);
                using var b = new SolidBrush(BookingFlowTheme.Dark); e.Graphics.FillPath(b, path);
            };
            _pnlTotalStrip.Controls.Add(new Label { Text = "Total amount due", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(100, 248, 244, 239), AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent });
            _rvTotal = new Label { Text = "₱400", Font = new Font("Georgia", 16f, FontStyle.Bold), ForeColor = BookingFlowTheme.Gold, AutoSize = true, Location = new Point(bW - 130, 14), BackColor = Color.Transparent };
            _pnlTotalStrip.Controls.Add(_rvTotal);
            body.Controls.Add(_pnlTotalStrip);

            // Blue email notice
            _pnlEmailNotice = new Panel { Location = new Point(0, 262), Size = new Size(bW, 48), BackColor = Color.Transparent };
            _pnlEmailNotice.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, _pnlEmailNotice.Width - 1, _pnlEmailNotice.Height - 1), 8);
                using var b = new SolidBrush(Color.FromArgb(9, 65, 130, 210)); e.Graphics.FillPath(b, path);
                using var pen = new Pen(Color.FromArgb(38, 65, 130, 210), 1f); e.Graphics.DrawPath(pen, path);
            };
            _pnlEmailNotice.Controls.Add(new Label { Text = "📧", Font = new Font("Segoe UI Emoji", 11), AutoSize = true, Location = new Point(12, 14), BackColor = Color.Transparent });
            _pnlEmailNotice.Controls.Add(new Label { Text = "A confirmation email containing your Booking ID and QR Code will be automatically sent to your registered email after confirming.", Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextMuted, Location = new Point(38, 8), Size = new Size(bW - 52, 34), BackColor = Color.Transparent });
            body.Controls.Add(_pnlEmailNotice);

            // Confirm & Book button (dark bg, gold text, full width)
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
            sec.Height = 66 + body.Height + 20;
            stack.Controls.Add(sec);

            // ── Confirmed panel (hidden until confirm clicked) ───
            _pnlConfirmed = BuildConfirmedPanel(W);
            _pnlConfirmed.Visible = false;
            stack.Controls.Add(_pnlConfirmed);

            // Nav (back only)
            var nav = NavBar(W);
            var back = BookingFlowTheme.CreateSecondaryButton("← Back");
            back.Location = new Point(0, 8); back.Click += (s, e) => ShowStep(3);
            nav.Controls.Add(back);
            stack.Controls.Add(nav);
        }

        // ══════════════════════════════════════════════════════════
        //  CONFIRMED PANEL — matches screenshot 4
        // ══════════════════════════════════════════════════════════
        private Panel BuildConfirmedPanel(int W)
        {
            var pnl = new Panel { Size = new Size(W, 560), BackColor = Color.Transparent };

            // Green circle
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
            pnl.Controls.Add(new Label { Text = "Your WildNest stay has been successfully reserved. We can't wait to welcome you.", Font = new Font("Segoe UI", 9f), ForeColor = BookingFlowTheme.TextMuted, Size = new Size(W, 34), Location = new Point(0, 104), BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleCenter });

            // Booking ID dark box
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

            // QR box
            var qrBox = new Panel { Location = new Point(W / 2 - 200, 212), Size = new Size(400, 238), BackColor = Color.White };
            qrBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = BookingFlowTheme.RoundedRect(new Rectangle(0, 0, qrBox.Width - 1, qrBox.Height - 1), 10);
                using var b = new SolidBrush(Color.White); e.Graphics.FillPath(b, path);
                using var pen = new Pen(BookingFlowTheme.Border, 1f); e.Graphics.DrawPath(pen, path);
            };
            qrBox.Controls.Add(new Label { Text = "📱 YOUR QR CODE — Scan at accommodation terminal or guest portal", Font = new Font("Segoe UI", 7.8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(14, 12), BackColor = Color.Transparent });

            // QR placeholder
            var _qrPicBox = new PictureBox
            {
                Location = new Point(100, 36),
                Size = new Size(200, 144),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            qrBox.Controls.Add(_qrPicBox);
            this.Tag = _qrPicBox; // store ref so ConfirmBooking can set the image

            qrBox.Controls.Add(new Label { Text = "OR USE BOOKING ID  ·  Manual entry at portal:", Font = new Font("Segoe UI", 7.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(14, 190), BackColor = Color.Transparent });

            var btnSave = new Button { Text = "💾 Save QR as PNG", Location = new Point(14, 210), Size = new Size(174, 22), Font = new Font("Segoe UI", 7.8f, FontStyle.Bold), FlatStyle = FlatStyle.Flat, BackColor = BookingFlowTheme.Dark, ForeColor = BookingFlowTheme.Gold, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (_qrBitmap == null) { System.Windows.Forms.MessageBox.Show("QR not generated yet."); return; }
                using var dlg = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = $"WildNest_QR_{_lblBid.Text}.png" };
                if (dlg.ShowDialog() == DialogResult.OK) _qrBitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
            };
            var btnPrint = new Button { Text = "🖨️ Print Details", Location = new Point(196, 210), Size = new Size(190, 22), Font = new Font("Segoe UI", 7.8f), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = BookingFlowTheme.TextMuted, Cursor = Cursors.Hand };
            btnPrint.FlatAppearance.BorderColor = BookingFlowTheme.Border;
            qrBox.Controls.AddRange(new Control[] { btnSave, btnPrint });
            pnl.Controls.Add(qrBox);

            // Email sent notice
            int esY = 212 + 238 + 14;
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
            emailSent.Controls.Add(new Label { Text = "Booking ID and QR Code sent to your email", Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(46, 28), BackColor = Color.Transparent });
            pnl.Controls.Add(emailSent);

            // Action buttons
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
            pnlCabin.Visible = step == 1;
            pnlGuestInfo.Visible = step == 2;
            pnlPayment.Visible = step == 3;
            pnlConfirm.Visible = step >= 4;

            if (step == 4)
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

        private void ConfirmBooking()
        {
            // Validate basics
            if (string.IsNullOrWhiteSpace(_selectedCabin))
            { MessageBox.Show("Please select a cabin first."); return; }
            if (string.IsNullOrWhiteSpace(_txtFirst?.Text) || string.IsNullOrWhiteSpace(_txtEmail?.Text))
            { MessageBox.Show("Please fill in guest information first."); ShowStep(2); return; }

            try
            {
                using var conn = new MySqlConnection(_connStr);
                conn.Open();

                // 1. Insert guest
                var guestCmd = new MySqlCommand(@"
    INSERT INTO tbl_Guests 
        (FirstName, LastName, Email, Phone, Nationality, ValidIDType, SpecialRequests)
    VALUES 
        (@fn, @ln, @em, @ph, @nat, @id, @req);", conn);

                guestCmd.Parameters.AddWithValue("@fn", _txtFirst.Text.Trim());
                guestCmd.Parameters.AddWithValue("@ln", _txtLast?.Text.Trim() ?? "");
                guestCmd.Parameters.AddWithValue("@em", _txtEmail.Text.Trim());
                guestCmd.Parameters.AddWithValue("@ph", _txtPhone?.Text.Trim() ?? "");
                guestCmd.Parameters.AddWithValue("@nat", _cboNationality?.Text ?? "");
                guestCmd.Parameters.AddWithValue("@id", _cboIdType?.Text ?? "");
                guestCmd.Parameters.AddWithValue("@req", _txtRequests?.Text.Trim() ?? "");
                guestCmd.ExecuteNonQuery();

                // Get GuestID separately
                var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", conn);
                int guestId = Convert.ToInt32(idCmd.ExecuteScalar());

                // 2. Get CabinID from tbl_Cabins
                var cabinCmd = new MySqlCommand(
                    "SELECT CabinID FROM tbl_Cabins WHERE CabinName = @name LIMIT 1;", conn);
                cabinCmd.Parameters.AddWithValue("@name", _selectedCabin);
                int cabinId = Convert.ToInt32(cabinCmd.ExecuteScalar());

                // 3. Generate Booking ID
                string bid = BookingIdGenerator.NewId();

                // 4. Insert reservation
                int nights = Nights();
                decimal total = CalcTotal();

                var resCmd = new MySqlCommand(@"
            INSERT INTO tbl_Reservations
                (ReservationID, GuestID, CabinID, BookingType,
                 CheckInDate, CheckOutDate, NumAdults, NumChildren, TotalAmount,
                 ArrivalTime, ModeOfTransport)
            VALUES
                (@rid, @gid, @cid, 'CabinStay',
                 @cin, @cout, @adults, @children, @total,
                 @arrTime, @transport);", conn);

                resCmd.Parameters.AddWithValue("@rid", bid);
                resCmd.Parameters.AddWithValue("@gid", guestId);
                resCmd.Parameters.AddWithValue("@cid", cabinId);
                resCmd.Parameters.AddWithValue("@cin", _dtCheckIn.Value.Date);
                resCmd.Parameters.AddWithValue("@cout", _dtCheckOut.Value.Date);
                resCmd.Parameters.AddWithValue("@adults", _adults);
                resCmd.Parameters.AddWithValue("@children", _children);
                resCmd.Parameters.AddWithValue("@total", total);
                resCmd.ExecuteNonQuery();

                // 5. Insert add-ons (experiences)
                foreach (var addon in _addons)
                {
                    var expCmd = new MySqlCommand(
                        "SELECT ExperienceID FROM tbl_Experiences WHERE ExperienceName = @n LIMIT 1;", conn);
                    expCmd.Parameters.AddWithValue("@n", addon.Key);
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

                // 6. Insert payment
                var payCmd = new MySqlCommand(@"
            INSERT INTO tbl_Payments (ReservationID, Amount, PaymentMethod, Status, PaidAt)
            VALUES (@rid, @amt, @meth, 'Confirmed', NOW());", conn);
                payCmd.Parameters.AddWithValue("@rid", bid);
                payCmd.Parameters.AddWithValue("@amt", total);
                payCmd.Parameters.AddWithValue("@meth", _paymentMethod);
                payCmd.ExecuteNonQuery();

                // Generate QR (single source of truth for screen + email + Tab 2 scan)
                _qrBitmap = EmailService.GenerateQrBitmap(bid);
                if (this.Tag is PictureBox _pb) _pb.Image = _qrBitmap;

                // Send real-time email confirmation
                _ = System.Threading.Tasks.Task.Run(() =>
                    EmailService.SendConfirmation(
                        _txtEmail.Text.Trim(),
                        $"{_txtFirst.Text.Trim()} {_txtLast?.Text.Trim()}",
                        bid, "Cabin Stay", $"Check-in: {_dtCheckIn.Value:MMM dd, yyyy} → Check-out: {_dtCheckOut.Value:MMM dd, yyyy}", CalcTotal(), _paymentMethod, (System.Drawing.Bitmap)_qrBitmap.Clone()));

                // Show success UI
                if (_lblBid != null) _lblBid.Text = bid;
                if (_pnlReviewCard != null) _pnlReviewCard.Visible = false;
                if (_pnlTotalStrip != null) _pnlTotalStrip.Visible = false;
                if (_pnlEmailNotice != null) _pnlEmailNotice.Visible = false;
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
        //  REVIEW  &  SUMMARY
        // ══════════════════════════════════════════════════════════
        private void RefreshReview()
        {
            if (_rvCabin != null) _rvCabin.Text = string.IsNullOrEmpty(_selectedCabin) ? "—" : _selectedCabin;
            if (_rvCheckIn != null) _rvCheckIn.Text = _dtCheckIn != null ? _dtCheckIn.Value.ToString("MMM dd, yyyy") : "—";
            if (_rvCheckOut != null) _rvCheckOut.Text = _dtCheckOut != null ? _dtCheckOut.Value.ToString("MMM dd, yyyy") : "—";
            if (_rvGuests != null) _rvGuests.Text = $"{_adults} Adult{(_adults != 1 ? "s" : "")}" + (_children > 0 ? $", {_children} Child{(_children != 1 ? "ren" : "")}" : "");
            if (_rvTotal != null) _rvTotal.Text = $"₱{CalcTotal():N0}";
        }

        private void FireSummary()
        {
            // If no cabin chosen yet → blank summary (pnlSummary stays empty)
            if (string.IsNullOrEmpty(_selectedCabin))
            {
                OnSummaryChanged?.Invoke(new Project.BookingSummary());
                return;
            }

            int nights = Nights();
            OnSummaryChanged?.Invoke(new Project.BookingSummary
            {
                BookingType = "Cabin Stay",
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

        /// <summary>Creates a scrollable FlowLayoutPanel inside parent. Returns (stack, usable width).</summary>
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

            var stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(0, 20, 0, 40),
                BackColor = Color.Transparent
            };
            wrap.Controls.Add(stack);
            parent.Controls.Add(host);
            return (stack, usableW);
        }

        /// <summary>White card section. Body accessible via Body().</summary>
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

        private Label MetaLbl(string text, int x) =>
            new Label { Text = text, Font = new Font("Segoe UI", 8.5f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(x, 20), BackColor = Color.Transparent };

        private Label FL(string text, int x, int y) =>
            new Label { Text = text, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.TextMuted, AutoSize = true, Location = new Point(x, y), BackColor = Color.Transparent };

        private void AddField(Panel parent, string label, Control ctrl, int x, int y, int w, int h)
        {
            parent.Controls.Add(FL(label, x, y));
            ctrl.Location = new Point(x, y + 20);
            ctrl.Size = new Size(w, h);
            parent.Controls.Add(ctrl);
        }

        private Panel CounterCard(string title, string subtitle, int x, int w)
        {
            var card = new Panel { Location = new Point(x, 0), Size = new Size(w, 80), BackColor = BookingFlowTheme.Cream };
            card.Paint += (s, e) => BookingFlowTheme.PaintRoundedCard(card, e);

            card.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, AutoSize = true, Location = new Point(14, 14), BackColor = Color.Transparent });
            card.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 8f), ForeColor = BookingFlowTheme.TextDim, AutoSize = true, Location = new Point(14, 36), BackColor = Color.Transparent });

            int btnRight = w - 18;
            var lbl = new Label { Text = (title == "Adults" ? _adults : _children).ToString(), Font = new Font("Georgia", 15f, FontStyle.Bold), ForeColor = BookingFlowTheme.Text, Size = new Size(34, 30), Location = new Point(btnRight - 50, 24), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            var btnM = SmallBtn("−", btnRight - 88, 24);
            var btnP = SmallBtn("+", btnRight - 14, 24);

            if (title == "Adults")
            {
                _lblAdults = lbl;
                btnM.Click += (s, e) => { _adults = Math.Max(0, _adults - 1); lbl.Text = _adults.ToString(); FireSummary(); };
                btnP.Click += (s, e) => { _adults++; lbl.Text = _adults.ToString(); FireSummary(); };
            }
            else
            {
                _lblChildren = lbl;
                btnM.Click += (s, e) => { _children = Math.Max(0, _children - 1); lbl.Text = _children.ToString(); FireSummary(); };
                btnP.Click += (s, e) => { _children++; lbl.Text = _children.ToString(); FireSummary(); };
            }

            card.Controls.AddRange(new Control[] { btnM, lbl, btnP });
            return card;
        }

        private Button SmallBtn(string text, int x, int y)
        {
            var btn = new Button { Text = text, Location = new Point(x, y), Size = new Size(32, 32) };
            BookingFlowTheme.StyleSmallButton(btn);
            return btn;
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
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            BookingFlowTheme.StyleComboBox(c);
            return c;
        }

        /// <summary>
        /// Adds a label+value row to the review card panel.
        /// Returns the value label so RefreshReview can update it.
        /// </summary>
        private Label AddReviewRow(Panel parent, string label, string value, int y, int W)
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
