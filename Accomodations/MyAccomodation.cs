using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Project.Accomodations;
using Project.UcReception;

namespace Project
{
    public partial class MyAccomodation : Form
    {
        // ══════════════════════════════════════════════════════
        //  PALETTE  (matches WildNest forest/gold brand)
        // ══════════════════════════════════════════════════════
        static readonly Color C_BG = Color.FromArgb(6, 20, 11);
        static readonly Color C_BG2 = Color.FromArgb(11, 33, 19);
        static readonly Color C_Forest = Color.FromArgb(7, 26, 14);
        static readonly Color C_ForestMid = Color.FromArgb(13, 40, 24);
        static readonly Color C_ForestLight = Color.FromArgb(27, 67, 50);
        static readonly Color C_Accent = Color.FromArgb(15, 110, 86);
        static readonly Color C_Gold = Color.FromArgb(212, 160, 23);
        static readonly Color C_GoldLight = Color.FromArgb(240, 201, 80);
        static readonly Color C_Cream = Color.FromArgb(248, 244, 239);
        static readonly Color C_Sand = Color.FromArgb(228, 224, 216);
        static readonly Color C_White = Color.White;
        static readonly Color C_TextDark = Color.FromArgb(22, 22, 22);
        static readonly Color C_Muted = Color.FromArgb(130, 130, 130);
        static readonly Color C_Border = Color.FromArgb(210, 210, 210);
        static readonly Color C_GoldBorder = Color.FromArgb(55, 212, 160, 23);

        // DB connection
        const string CONN = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";

        // ── State ──
        bool _isRefTab = true;

        // ── Top-level panels ──
        Panel? _card;
        Panel? _header;      // green hero area
        Panel? _tabStrip;    // sits BELOW the green header
        Panel? _tabIndicator;
        Button? _btnTab1;
        Button? _btnTab2;
        Panel? _body;        // white content area

        // ── Tab 1 controls ──
        Panel? _tab1;
        TextBox? _tbRef;
        TextBox? _tbEmail;
        Button? _btnFind;

        // ── Tab 2 controls ──
        Panel? _tab2;
        Label? _lblQrStatus;
        QrCameraScanner? _guestQrCamera;
        Label? _lblCameraStatus;
        Label? _lblCameraLead;
        string? _decodedBookingId = null;

        // ── Footer ──
        Label? _lblBack;

        public MyAccomodation()
        {
            InitializeComponent();
            BuildUI();
            FormClosed += (_, _) => _guestQrCamera?.StopCamera();
        }

        // ══════════════════════════════════════════════════════
        //  BUILD UI
        // ══════════════════════════════════════════════════════
        void BuildUI()
        {
            this.BackColor = C_BG;
            this.ClientSize = new Size(520, 900);
            this.Paint += OnFormPaint;

            BuildCard();
            BuildHeader();
            BuildTabStrip();   // tabs anchored directly below header
            BuildBody();
            BuildTab1();
            BuildTab2();
            BuildFooter();

            ShowTab(1);
        }

        // ── FORM BACKGROUND ──
        void OnFormPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(this.Width, this.Height),
                C_BG, C_BG2))
                g.FillRectangle(br, this.ClientRectangle);

            DrawGlow(g, this.Width / 2, this.Height, this.Width * 0.65f,
                Color.FromArgb(20, 212, 160, 23));

            DrawBracket(g, 18, 18, true, true);
            DrawBracket(g, Width - 18, 18, false, true);
            DrawBracket(g, 18, Height - 18, true, false);
            DrawBracket(g, Width - 18, Height - 18, false, false);
        }

        // ── CARD ──
        void BuildCard()
        {
            _card = new Panel
            {
                Size = new Size(460, 760),
                BackColor = C_White
            };
            _card.Location = new Point(
                (this.Width - _card.Width) / 2,
                (this.Height - _card.Height) / 2 - 20);

            SetRound(_card, 18);
            _card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(C_GoldBorder, 1.8f))
                    g.DrawPath(p, RoundPath(_card.ClientRectangle, 18));
            };

            this.Controls.Add(_card);
            this.Resize += (s, e) =>
            {
                _card.Location = new Point(
                    (this.Width - _card.Width) / 2,
                    (this.Height - _card.Height) / 2 - 20);
            };
        }

        // ── HEADER (green hero panel) ──
        //  Uses absolute positioning so we can stack header → tabStrip → body
        void BuildHeader()
        {
            _header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(_card?.Width ?? Width, 195),
                BackColor = C_Forest
            };
            _header.Paint += OnHeaderPaint;
            _card?.Controls.Add(_header);

            // Logo badge
            var logoBadge = new Panel
            {
                Size = new Size(76, 76),
                BackColor = Color.FromArgb(35, 212, 160, 23)
            };
            logoBadge.Location = new Point((_header.Width - logoBadge.Width) / 2, 16);
            SetRound(logoBadge, 20);
            _header.Controls.Add(logoBadge);

            var logo = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try { logo.Image = AppAssetLoader.LoadImage("Logo", "Resources", "Logo.png"); } catch { }
            logoBadge.Controls.Add(logo);

            var title = MakeLabel("Access Your Reservations",
                new Font("Georgia", 15.5f, FontStyle.Bold), C_Cream,
                new Rectangle(0, 100, _header.Width, 30),
                ContentAlignment.MiddleCenter);
            title.BackColor = Color.Transparent;
            _header.Controls.Add(title);

            var sub = MakeLabel(
                "No account needed — enter your booking reference\nor scan your confirmation QR code.",
                new Font("Bahnschrift SemiLight", 9f), Color.FromArgb(120, C_Cream),
                new Rectangle(0, 134, _header.Width, 40),
                ContentAlignment.TopCenter);
            sub.BackColor = Color.Transparent;
            _header.Controls.Add(sub);

            _header.Resize += (s, e) =>
            {
                logoBadge.Location = new Point((_header.Width - 76) / 2, 16);
                title.Width = _header.Width;
                sub.Width = _header.Width;
                _header.Invalidate();
            };
        }

        void OnHeaderPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            if (s is not Panel p) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var br = new LinearGradientBrush(
                new Point(0, 0), new Point(0, p.Height),
                C_Forest, C_ForestMid))
                g.FillRectangle(br, p.ClientRectangle);

            DrawGlow(g, p.Width / 2, p.Height, p.Width * 0.6f,
                Color.FromArgb(22, 212, 160, 23));

            // Gold ornament line at bottom of header
            int cx = p.Width / 2, y = p.Height - 2;
            DrawFadeLine(g, 40, y, cx - 12, y, C_Gold, false);
            DrawFadeLine(g, cx + 12, y, p.Width - 40, y, C_Gold, true);
            using (var br2 = new SolidBrush(C_Gold))
                g.FillEllipse(br2, cx - 4, y - 4, 8, 8);
        }

        // ── TAB STRIP — sits directly below the green header ──
        void BuildTabStrip()
        {
            if (_header == null || _card == null) return;

            // TAB STRIP is placed immediately below _header
            int tabY = _header.Bottom;    // e.g. 195

            _tabStrip = new Panel
            {
                Location = new Point(0, tabY),
                Size = new Size(_card.Width, 50),
                BackColor = C_Sand
            };

            // Subtle top separator line
            _tabStrip.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(40, C_Gold), 1f))
                    e.Graphics.DrawLine(pen, 0, 0, _tabStrip.Width, 0);
                // Bottom border
                using (var pen = new Pen(Color.FromArgb(25, 0, 0, 0), 1f))
                    e.Graphics.DrawLine(pen, 0, _tabStrip.Height - 1,
                                             _tabStrip.Width, _tabStrip.Height - 1);
            };

            _card.Controls.Add(_tabStrip);

            int btnW = (_card.Width - 8) / 2;

            _btnTab1 = MakeTabBtn("Booking Reference", true);
            _btnTab1.Size = new Size(btnW, 38);
            _btnTab1.Location = new Point(4, 6);

            _btnTab2 = MakeTabBtn("Scan QR Code", false);
            _btnTab2.Size = new Size(btnW, 38);
            _btnTab2.Location = new Point(4 + btnW, 6);

            _tabStrip.Controls.Add(_btnTab1);
            _tabStrip.Controls.Add(_btnTab2);

            // Gold active indicator bar
            _tabIndicator = new Panel
            {
                Size = new Size(btnW, 3),
                Location = new Point(4, 47),
                BackColor = C_Gold
            };
            SetRound(_tabIndicator, 2);
            _tabStrip.Controls.Add(_tabIndicator);

            _btnTab1.Click += (s, e) => ShowTab(1);
            _btnTab2.Click += (s, e) => ShowTab(2);
        }

        Button MakeTabBtn(string text, bool active)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Bahnschrift SemiLight", 9.5f,
                                     active ? FontStyle.Bold : FontStyle.Regular),
                BackColor = active ? C_White : Color.Transparent,
                ForeColor = active ? C_TextDark : C_Muted
            };
            btn.FlatAppearance.BorderSize = 0;
            if (active) SetRound(btn, 7);
            return btn;
        }

        void ShowTab(int tab)
        {
            if (_card == null || _btnTab1 == null || _btnTab2 == null || _tabIndicator == null || _tab1 == null || _tab2 == null)
                return;

            _isRefTab = (tab == 1);
            int btnW = (_card.Width - 8) / 2;

            _btnTab1.Font = new Font("Bahnschrift SemiLight", 9.5f, tab == 1 ? FontStyle.Bold : FontStyle.Regular);
            _btnTab1.BackColor = tab == 1 ? C_White : Color.Transparent;
            _btnTab1.ForeColor = tab == 1 ? C_TextDark : C_Muted;
            if (tab == 1) SetRound(_btnTab1, 7); else _btnTab1.Region = null;

            _btnTab2.Font = new Font("Bahnschrift SemiLight", 9.5f, tab == 2 ? FontStyle.Bold : FontStyle.Regular);
            _btnTab2.BackColor = tab == 2 ? C_White : Color.Transparent;
            _btnTab2.ForeColor = tab == 2 ? C_TextDark : C_Muted;
            if (tab == 2) SetRound(_btnTab2, 7); else _btnTab2.Region = null;

            // Slide gold indicator under the active tab
            _tabIndicator.Size = new Size(btnW, 3);
            _tabIndicator.Location = new Point(tab == 1 ? 4 : 4 + btnW, 47);

            _tab1.Visible = (tab == 1);
            _tab2.Visible = (tab == 2);

            if (tab == 2)
            {
                _guestQrCamera?.StartCamera();
                if (_lblCameraStatus != null && string.IsNullOrWhiteSpace(_lblCameraStatus.Text))
                    _lblCameraStatus.Text = "Live guest scanner ready. Point your confirmation QR inside the frame.";
            }
            else
            {
                _guestQrCamera?.StopCamera();
            }
        }

        // ── BODY (white content area below tab strip) ──
        void BuildBody()
        {
            if (_tabStrip == null || _card == null) return;

            int bodyY = _tabStrip.Bottom;  // e.g. 245

            _body = new Panel
            {
                Location = new Point(0, bodyY),
                Size = new Size(_card.Width, _card.Height - bodyY),
                BackColor = C_White,
                Padding = new Padding(28, 18, 28, 18)
            };
            _card.Controls.Add(_body);
        }

        // ══════════════════════════════════════════════════════
        //  TAB 1 — BOOKING REFERENCE + EMAIL
        // ══════════════════════════════════════════════════════
        void BuildTab1()
        {
            if (_body == null) return;

            _tab1 = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_White,
                Padding = new Padding(0, 4, 0, 0)
            };
            _body.Controls.Add(_tab1);

            int lx = 0;
            int w = _body.ClientSize.Width > 0 ? _body.ClientSize.Width - 56 : 360;
            int y = 12;

            // ── Section heading ──────────────────────────────────────
            var sectionLbl = MakeLabel("Enter your booking details below",
                new Font("Bahnschrift SemiLight", 9.5f),
                Color.FromArgb(155, C_TextDark),
                new Rectangle(lx, y, w, 20),
                ContentAlignment.MiddleLeft);
            sectionLbl.BackColor = Color.Transparent;
            sectionLbl.Name = "t1_section";
            _tab1.Controls.Add(sectionLbl);
            y += 30;

            // ── Booking Reference ────────────────────────────────────
            var refRow = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 22),
                BackColor = Color.Transparent,
                Name = "t1_refrow"
            };
            var refIcon = MakeLabel("🔖", new Font("Segoe UI Emoji", 8.5f),
                C_Gold, new Rectangle(0, 0, 22, 22), ContentAlignment.MiddleLeft);
            refIcon.BackColor = Color.Transparent;
            refRow.Controls.Add(refIcon);
            var refLbl = MakeLabel("BOOKING REFERENCE",
                new Font("Bahnschrift", 8f, FontStyle.Bold),
                C_Muted, new Rectangle(24, 0, w - 24, 22), ContentAlignment.MiddleLeft);
            refLbl.BackColor = Color.Transparent;
            refRow.Controls.Add(refLbl);
            _tab1.Controls.Add(refRow);
            y += 26;

            _tbRef = AddRoundedTextBox(_tab1, "e.g.  WN-2026-4821", lx, y, w);
            _tbRef.CharacterCasing = CharacterCasing.Upper;
            _tbRef.Font = new Font("Bahnschrift SemiLight", 12f);
            (_tbRef.Parent as Panel)!.Name = "t1_refbox";
            y += 54;

            AddHintLabel(_tab1, "  Found in your confirmation email", lx, y);
            _tab1.Controls[_tab1.Controls.Count - 1].Name = "t1_refhint";
            y += 28;

            // ── Divider ─────────────────────────────────────────────
            var divider = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 1),
                BackColor = Color.FromArgb(28, 0, 0, 0),
                Name = "t1_div"
            };
            _tab1.Controls.Add(divider);
            y += 18;

            // ── Email Address ────────────────────────────────────────
            var emailRow = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 22),
                BackColor = Color.Transparent,
                Name = "t1_emailrow"
            };
            var emailIcon = MakeLabel("✉️", new Font("Segoe UI Emoji", 8.5f),
                C_Gold, new Rectangle(0, 0, 22, 22), ContentAlignment.MiddleLeft);
            emailIcon.BackColor = Color.Transparent;
            emailRow.Controls.Add(emailIcon);
            var emailLbl = MakeLabel("EMAIL ADDRESS",
                new Font("Bahnschrift", 8f, FontStyle.Bold),
                C_Muted, new Rectangle(24, 0, w - 24, 22), ContentAlignment.MiddleLeft);
            emailLbl.BackColor = Color.Transparent;
            emailRow.Controls.Add(emailLbl);
            _tab1.Controls.Add(emailRow);
            y += 26;

            _tbEmail = AddRoundedTextBox(_tab1, "your@email.com", lx, y, w);
            _tbEmail.Font = new Font("Bahnschrift SemiLight", 12f);
            (_tbEmail.Parent as Panel)!.Name = "t1_emailbox";
            y += 54;

            AddHintLabel(_tab1, "  Must match the email used during booking", lx, y);
            _tab1.Controls[_tab1.Controls.Count - 1].Name = "t1_emailhint";
            y += 28;

            // ── Find My Booking button ───────────────────────────────
            _btnFind = new Button
            {
                Text = "🔍   Find My Booking",
                Size = new Size(w, 50),
                Location = new Point(lx, y),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Bahnschrift SemiLight", 11.5f, FontStyle.Bold),
                BackColor = C_Forest,
                ForeColor = C_Gold,
                Cursor = Cursors.Hand,
                Name = "t1_btnfind"
            };
            _btnFind.FlatAppearance.BorderSize = 0;
            SetRound(_btnFind, 11);
            _btnFind.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(80, C_Gold), 1.5f))
                    e.Graphics.DrawPath(p, RoundPath(new Rectangle(0, 0, _btnFind.Width - 1, _btnFind.Height - 1), 11));
            };
            _btnFind.MouseEnter += (s, e) => { _btnFind.BackColor = C_ForestLight; _btnFind.ForeColor = C_GoldLight; SetRound(_btnFind, 11); _btnFind.Invalidate(); };
            _btnFind.MouseLeave += (s, e) => { _btnFind.BackColor = C_Forest; _btnFind.ForeColor = C_Gold; SetRound(_btnFind, 11); _btnFind.Invalidate(); };
            _btnFind.Click += OnFindClicked;
            _tab1.Controls.Add(_btnFind);
            y += 62;

            // ── Security strip ───────────────────────────────────────
            var secStrip = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 32),
                BackColor = Color.FromArgb(7, 0, 100, 0),
                Name = "t1_sec"
            };
            SetRound(secStrip, 7);
            secStrip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(25, 39, 174, 96), 1f))
                    e.Graphics.DrawPath(p, RoundPath(new Rectangle(0, 0, secStrip.Width - 1, secStrip.Height - 1), 7));
            };
            var secLbl = MakeLabel("🔒  Your data is never stored. Lookup only.",
                new Font("Bahnschrift SemiLight", 8.5f),
                Color.FromArgb(60, 140, 90),
                new Rectangle(0, 0, w, 32),
                ContentAlignment.MiddleCenter);
            secLbl.BackColor = Color.Transparent;
            secStrip.Controls.Add(secLbl);
            _tab1.Controls.Add(secStrip);
            y += 42;

            // ── Demo hint card ───────────────────────────────────────
            var demo = new Panel
            {
                Size = new Size(w, 72),
                Location = new Point(lx, y),
                BackColor = Color.FromArgb(255, 251, 230),
                Name = "t1_demo"
            };
            SetRound(demo, 9);
            demo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(100, 212, 160, 23), 1f))
                    e.Graphics.DrawPath(p, RoundPath(new Rectangle(0, 0, demo.Width - 1, demo.Height - 1), 9));
            };
            var demoIcon = MakeLabel("💡", new Font("Segoe UI Emoji", 9f),
                Color.FromArgb(160, 100, 8), new Rectangle(12, 4, 24, 20), ContentAlignment.MiddleLeft);
            demoIcon.BackColor = Color.Transparent;
            demo.Controls.Add(demoIcon);
            var demoTitle = MakeLabel("DEMO CREDENTIALS",
                new Font("Bahnschrift", 7.5f, FontStyle.Bold),
                Color.FromArgb(160, 100, 8), new Rectangle(36, 6, w - 48, 22), ContentAlignment.MiddleLeft);
            demoTitle.BackColor = Color.Transparent;
            demo.Controls.Add(demoTitle);
            var demoVal = MakeLabel("Use your exact booking reference and the same email used during booking",
                new Font("Bahnschrift SemiLight", 8f),
                Color.FromArgb(120, 80, 6), new Rectangle(12, 30, w - 24, 36), ContentAlignment.MiddleLeft);
            demoVal.BackColor = Color.Transparent;
            demo.Controls.Add(demoVal);
            _tab1.Controls.Add(demo);

            _body.Resize += (s, e) => ReflowTab1();
        }

        void ReflowTab1()
        {
            if (_body == null || _tab1 == null) return;
            int w = _body.ClientSize.Width - 56;
            if (w < 100) return;
            foreach (Control c in _tab1.Controls)
                c.Width = w;
        }

        // ══════════════════════════════════════════════════════
        //  TAB 2 — QR SCANNER
        // ══════════════════════════════════════════════════════
        void BuildTab2()
        {
            if (_body == null) return;

            _tab2 = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_White,
                Visible = false
            };
            _body.Controls.Add(_tab2);

            int lx = 0, w = _body.ClientSize.Width > 0 ? _body.ClientSize.Width - 56 : 360;
            int y = 8;

            var introCard = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 110),
                BackColor = C_Forest
            };
            SetRound(introCard, 16);
            introCard.Paint += (s, e) =>
            {
                if (s is not Panel p || p.Width < 2 || p.Height < 2) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new LinearGradientBrush(new Point(0, 0), new Point(0, p.Height), C_Forest, C_ForestMid);
                g.FillPath(br, RoundPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 16));
                using var gold = new Pen(Color.FromArgb(52, C_Gold), 1.2f);
                g.DrawPath(gold, RoundPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 16));
            };
            _tab2.Controls.Add(introCard);

            var introBadge = new Label
            {
                Text = "CAMERA-ONLY ACCESS",
                AutoSize = false,
                Font = new Font("Bahnschrift", 8.2f, FontStyle.Bold),
                ForeColor = C_Gold,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(18, 16, w - 36, 18)
            };
            introCard.Controls.Add(introBadge);

            var introTitle = new Label
            {
                Text = "Scan Your Confirmation QR",
                AutoSize = false,
                Font = new Font("Georgia", 15f, FontStyle.Bold),
                ForeColor = C_Cream,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(18, 38, w - 36, 28)
            };
            introCard.Controls.Add(introTitle);

            var introSub = new Label
            {
                Text = "Point your booking QR inside the camera frame and WildNest will recognize your reservation automatically.",
                AutoSize = false,
                Font = new Font("Bahnschrift SemiLight", 9.1f),
                ForeColor = Color.FromArgb(205, C_Cream),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft,
                Bounds = new Rectangle(18, 68, w - 36, 28)
            };
            introCard.Controls.Add(introSub);
            y += 126;

            var scannerShell = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 340),
                BackColor = Color.FromArgb(250, 248, 244)
            };
            SetRound(scannerShell, 18);
            scannerShell.Paint += (s, e) =>
            {
                if (s is not Panel p || p.Width < 2 || p.Height < 2) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var border = new Pen(Color.FromArgb(65, C_Gold), 1.2f);
                g.DrawPath(border, RoundPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 18));
            };
            _tab2.Controls.Add(scannerShell);

            var shellTitle = MakeLabel(
                "Live Camera Recognition",
                new Font("Georgia", 13f, FontStyle.Bold),
                C_TextDark,
                new Rectangle(22, 18, w - 44, 26),
                ContentAlignment.MiddleLeft);
            shellTitle.BackColor = Color.Transparent;
            scannerShell.Controls.Add(shellTitle);

            _lblCameraLead = MakeLabel(
                "The system will first recognize the reservation, then confirm the guest before opening the portal.",
                new Font("Bahnschrift SemiLight", 8.9f),
                C_Muted,
                new Rectangle(22, 46, w - 44, 34),
                ContentAlignment.TopLeft);
            _lblCameraLead.BackColor = Color.Transparent;
            scannerShell.Controls.Add(_lblCameraLead);

            _guestQrCamera = new QrCameraScanner
            {
                Size = new Size(w - 44, 188),
                Location = new Point(22, 92)
            };
            _guestQrCamera.QrCodeDetected += OnGuestCameraQrDetected;
            scannerShell.Controls.Add(_guestQrCamera);

            _lblCameraStatus = MakeLabel(
                "Scanner warming up. Hold your booking QR steady inside the guide frame.",
                new Font("Bahnschrift SemiLight", 8.4f),
                C_Muted,
                new Rectangle(22, 286, w - 44, 18),
                ContentAlignment.MiddleLeft);
            _lblCameraStatus.BackColor = Color.Transparent;
            scannerShell.Controls.Add(_lblCameraStatus);

            _lblQrStatus = MakeLabel(
                "Awaiting reservation recognition.",
                new Font("Bahnschrift SemiLight", 9f),
                Color.FromArgb(140, 98, 78),
                new Rectangle(22, 306, w - 44, 18),
                ContentAlignment.MiddleLeft);
            _lblQrStatus.BackColor = Color.Transparent;
            scannerShell.Controls.Add(_lblQrStatus);
            y += 352;

            var hintPanel = new Panel
            {
                Location = new Point(lx, y),
                Size = new Size(w, 64),
                BackColor = Color.FromArgb(246, 242, 234)
            };
            SetRound(hintPanel, 14);
            hintPanel.Paint += (s, e) =>
            {
                if (s is not Panel p || p.Width < 2 || p.Height < 2) return;
                using var border = new Pen(Color.FromArgb(38, C_Gold), 1f);
                e.Graphics.DrawPath(border, RoundPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 14));
            };
            _tab2.Controls.Add(hintPanel);

            var safeTitle = MakeLabel(
                "FRONT-DESK SAFE",
                new Font("Bahnschrift", 8f, FontStyle.Bold),
                Color.FromArgb(140, 92, 14),
                new Rectangle(18, 10, w - 36, 16),
                ContentAlignment.MiddleLeft);
            safeTitle.BackColor = Color.Transparent;
            hintPanel.Controls.Add(safeTitle);

            var safeBody = MakeLabel(
                "Only valid WildNest reservation QR codes are accepted. Once recognized, the guest portal opens using the booking linked in the database.",
                new Font("Bahnschrift SemiLight", 8.5f),
                C_Muted,
                new Rectangle(18, 28, w - 36, 26),
                ContentAlignment.TopLeft);
            safeBody.BackColor = Color.Transparent;
            hintPanel.Controls.Add(safeBody);
        }

        void OnGuestCameraQrDetected(string rawPayload)
        {
            if (_guestQrCamera == null)
                return;

            string bookingId = ExtractBookingId(rawPayload);
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                SetQrStatus("❌  Live scan detected a code but no valid booking reference was found.", Color.FromArgb(180, 40, 40));
                return;
            }

            _guestQrCamera.ShowRecognition($"Recognizing reservation {bookingId}...");
            ProcessQrPayload(rawPayload, null, fromLiveCamera: true);
        }

        void ProcessQrPayload(string rawPayload, Image? previewImage, bool fromLiveCamera)
        {
            string bookingId = ExtractBookingId(rawPayload);
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                SetQrStatus("❌  QR decoded but no Booking ID was found.", Color.FromArgb(180, 40, 40));
                _decodedBookingId = null;
                return;
            }

            _decodedBookingId = bookingId;
            previewImage?.Dispose();

            if (fromLiveCamera)
            {
                SetQrStatus($"🔎  Recognizing reservation {_decodedBookingId}...", Color.FromArgb(160, 100, 8));
                Application.DoEvents();
                LookupAndOpenPortal(_decodedBookingId, null, viaQR: true);
            }
            else
            {
                SetQrStatus($"✅  QR recognized. Ready to verify reservation {_decodedBookingId}.", Color.FromArgb(20, 130, 70));
            }
        }

        string ExtractBookingId(string rawPayload)
        {
            string raw = (rawPayload ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string bookingId = raw.ToUpperInvariant();
            if (raw.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(raw);
                    var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    bookingId = (qs["i"] ?? qs["id"] ?? qs["r"] ?? qs["ref"] ?? qs["reservationId"] ?? "").ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(bookingId))
                    {
                        var match = Regex.Match(uri.AbsolutePath, @"WN-\d{4}\d{10,}-\d{4}|WN-\d{4}-\d{4,}|WN-\d{12,}-\d{4,}", RegexOptions.IgnoreCase);
                        if (match.Success)
                            bookingId = match.Value.ToUpperInvariant();
                    }
                }
                catch
                {
                    bookingId = raw.ToUpperInvariant();
                }
            }

            return bookingId;
        }

        void SetQrStatus(string msg, Color color)
        {
            if (_lblQrStatus == null) return;
            _lblQrStatus.Text = msg;
            _lblQrStatus.ForeColor = color;
        }

        // ══════════════════════════════════════════════════════
        //  FOOTER
        // ══════════════════════════════════════════════════════
        void BuildFooter()
        {
            if (_card == null) return;

            _lblBack = new Label
            {
                Text = "← Back to WildNest",
                AutoSize = false,
                Font = new Font("Bahnschrift SemiLight", 9.5f),
                ForeColor = Color.FromArgb(140, C_Cream),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _lblBack.MouseEnter += (s, e) => _lblBack.ForeColor = C_Gold;
            _lblBack.MouseLeave += (s, e) => _lblBack.ForeColor = Color.FromArgb(140, C_Cream);
            _lblBack.Click += (s, e) => this.Close();
            this.Controls.Add(_lblBack);

            Action pos = () =>
            {
                if (_lblBack == null) return;
                _lblBack.Size = new Size(this.Width, 28);
                _lblBack.Location = new Point(0, _card.Bottom + 14);
            };
            pos();
            this.Resize += (s, e) => pos();
            _lblBack.BringToFront();
        }

        // ══════════════════════════════════════════════════════
        //  DB LOOKUP
        // ══════════════════════════════════════════════════════
        void OnFindClicked(object? s, EventArgs e)
        {
            string refNum = _tbRef?.Text.Trim().ToUpper() ?? string.Empty;
            string email = _tbEmail?.Text.Trim().ToLower() ?? string.Empty;

            if (string.IsNullOrEmpty(refNum) || string.IsNullOrEmpty(email))
            {
                ShowError("Please fill in both fields.");
                return;
            }

            if (!EmailSecurity.TryNormalizeAndValidate(email, out var normalizedEmail, out var error))
            {
                ShowError(error);
                return;
            }

            LookupAndOpenPortal(refNum, normalizedEmail, viaQR: false);
        }

        void LookupAndOpenPortal(string bookingId, string? email, bool viaQR)
        {
            try
            {
                using (var conn = new MySqlConnection(CONN))
                {
                    conn.Open();

                    string sql = viaQR
                        ? @"SELECT r.ReservationID, g.FirstName, g.LastName, g.Email,
                                   r.CheckInDate, r.CheckOutDate, r.VisitDate,
                                   COALESCE(c.CabinName, '') AS CabinName
                            FROM tbl_Reservations r
                            JOIN tbl_Guests g         ON g.GuestID  = r.GuestID
                            LEFT JOIN tbl_Cabins c    ON c.CabinID  = r.CabinID
                            WHERE r.ReservationID = @bid
                            LIMIT 1"
                        : @"SELECT r.ReservationID, g.FirstName, g.LastName, g.Email,
                                   r.CheckInDate, r.CheckOutDate, r.VisitDate,
                                   COALESCE(c.CabinName, '') AS CabinName
                            FROM tbl_Reservations r
                            JOIN tbl_Guests g         ON g.GuestID  = r.GuestID
                            LEFT JOIN tbl_Cabins c    ON c.CabinID  = r.CabinID
                            WHERE r.ReservationID = @bid
                              AND LOWER(g.Email) = LOWER(@email)
                            LIMIT 1";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@bid", bookingId);
                        if (!viaQR)
                            cmd.Parameters.AddWithValue("@email", email);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                ShowError(viaQR
                                    ? "No booking found for this QR code."
                                    : "Booking not found. Please check your reference and email.");
                                return;
                            }

                            reader.Read();
                            string reservationId = Convert.ToString(reader["ReservationID"]) ?? "";
                            string guestName = $"{reader["FirstName"]} {reader["LastName"]}";
                            string guestEmail = Convert.ToString(reader["Email"]) ?? "";
                            string cabin = Convert.ToString(reader["CabinName"]) ?? "";
                            string checkIn = reader["CheckInDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["CheckInDate"]).ToString("MMM dd, yyyy")
                                : (reader["VisitDate"] != DBNull.Value ? Convert.ToDateTime(reader["VisitDate"]).ToString("MMM dd, yyyy") : "");
                            string checkOut = reader["CheckOutDate"] != DBNull.Value
                                ? Convert.ToDateTime(reader["CheckOutDate"]).ToString("MMM dd, yyyy") : "";
                            int nights = (reader["CheckInDate"] != DBNull.Value && reader["CheckOutDate"] != DBNull.Value)
                                ? (Convert.ToDateTime(reader["CheckOutDate"]) - Convert.ToDateTime(reader["CheckInDate"])).Days : 0;

                            if (viaQR)
                            {
                                SetQrStatus($"✅  Recognizing guest {guestName}. Opening portal...", Color.FromArgb(20, 130, 70));
                                _guestQrCamera?.ShowRecognition($"Recognizing guest {guestName}...");
                                Application.DoEvents();
                            }

                            // Replace with your GuestPortalDashboard form:
                            // var portal = new GuestPortalDashboard(reservationId, guestName, guestEmail, cabin, checkIn, checkOut);
                            // portal.Show(); this.Hide();

                            if (_card != null) _card.Visible = false;
                            if (_lblBack != null) _lblBack.Visible = false;
                            _guestQrCamera?.StopCamera();
                            this.BackColor = Color.FromArgb(235, 231, 225);

                            var portal = new GuestPortalWebView();
                            portal.OnSignOut += () =>
                            {
                                this.Controls.Remove(portal);
                                portal.Dispose();
                                if (_card != null) _card.Visible = true;
                                if (_lblBack != null) _lblBack.Visible = true;
                                this.BackColor = Color.FromArgb(6, 20, 11);
                                _tbRef?.Clear();
                                _tbEmail?.Clear();
                                if (!_isRefTab)
                                    _guestQrCamera?.StartCamera();
                            };
                            this.Controls.Add(portal);
                            portal.BringToFront();
                            portal.OpenPortalDirectly(guestEmail, bookingId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetQrStatus($"⚠️  Portal recognition failed: {ex.Message}", Color.FromArgb(180, 100, 0));
                ShowError($"Database error: {ex.Message}");
            }
        }

        void ShowError(string msg)
        {
            MessageBox.Show(msg, "WildNest — Access Denied",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ══════════════════════════════════════════════════════
        //  CONTROL FACTORY HELPERS
        // ══════════════════════════════════════════════════════
        Label MakeLabel(string text, Font font, Color fore, Rectangle bounds, ContentAlignment align)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = fore,
                AutoSize = false,
                Size = bounds.Size,
                Location = bounds.Location,
                TextAlign = align
            };
        }

        void AddHintLabel(Panel parent, string text, int x, int y)
        {
            var lbl = MakeLabel(text,
                new Font("Bahnschrift SemiLight", 8f),
                Color.FromArgb(150, C_Muted),
                new Rectangle(x, y, 380, 18),
                ContentAlignment.MiddleLeft);
            lbl.BackColor = Color.Transparent;
            parent.Controls.Add(lbl);
        }

        /// <summary>
        /// Rounded text-box wrapper with placeholder, focus glow, and border.
        /// </summary>
        TextBox AddRoundedTextBox(Panel parent, string placeholder, int x, int y, int w)
        {
            var wrapper = new Panel
            {
                Size = new Size(w, 46),
                Location = new Point(x, y),
                BackColor = C_White,
                Padding = new Padding(12, 0, 12, 0)
            };
            SetRound(wrapper, 10);
            wrapper.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var p = new Pen(Color.FromArgb(160, C_Border), 1.5f))
                    e.Graphics.DrawPath(p,
                        RoundPath(new Rectangle(0, 0, wrapper.Width - 1, wrapper.Height - 1), 10));
            };

            var tb = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Bahnschrift SemiLight", 11.5f),
                BackColor = C_White,
                ForeColor = C_TextDark,
                PlaceholderText = placeholder
            };

            tb.Enter += (s, e) =>
            {
                wrapper.BackColor = Color.FromArgb(248, 254, 251);
                tb.BackColor = Color.FromArgb(248, 254, 251);
                // Highlight border gold on focus
                wrapper.Tag = "focused";
                wrapper.Invalidate();
            };
            tb.Leave += (s, e) =>
            {
                wrapper.BackColor = C_White;
                tb.BackColor = C_White;
                wrapper.Tag = null;
                wrapper.Invalidate();
            };

            // Re-draw border with gold tint when focused
            wrapper.Paint += (s, e) =>
            {
                if (wrapper.Tag is string t && t == "focused")
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var p = new Pen(Color.FromArgb(180, C_Gold), 1.8f))
                        e.Graphics.DrawPath(p,
                            RoundPath(new Rectangle(0, 0, wrapper.Width - 1, wrapper.Height - 1), 10));
                }
            };

            wrapper.Controls.Add(tb);
            parent.Controls.Add(wrapper);
            return tb;
        }

        // ══════════════════════════════════════════════════════
        //  DRAWING HELPERS
        // ══════════════════════════════════════════════════════
        void SetRound(Control c, int r) =>
            c.Region = new Region(RoundPath(c.ClientRectangle, r));

        GraphicsPath RoundPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        void DrawGlow(Graphics g, float cx, float cy, float radius, Color color)
        {
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
                using (var br = new PathGradientBrush(path))
                {
                    br.CenterColor = color;
                    br.SurroundColors = new[] { Color.FromArgb(0, color) };
                    g.FillPath(br, path);
                }
            }
        }

        void DrawFadeLine(Graphics g, int x1, int y1, int x2, int y2, Color c, bool reverse)
        {
            using (var br = new LinearGradientBrush(
                new Point(x1, y1), new Point(x2, y2),
                reverse ? c : Color.FromArgb(0, c),
                reverse ? Color.FromArgb(0, c) : c))
            using (var pen = new Pen(br, 1.5f))
                g.DrawLine(pen, x1, y1, x2, y2);
        }

        void DrawBracket(Graphics g, int x, int y, bool left, bool top)
        {
            int sz = 22;
            using (var pen = new Pen(Color.FromArgb(45, C_Gold), 1.5f))
            {
                g.DrawLine(pen, left ? x : x - sz, top ? y : y,
                                left ? x + sz : x, top ? y : y);
                g.DrawLine(pen, x, top ? y : y,
                                x, top ? y + sz : y - sz);
            }
        }
    }
}
