// ============================================================
//  UcStaffChat.cs  —  WildNest Premium 1-to-1 Staff Messaging
//  Replaces the old Administrator-only hub model.
//  Any role can now message any other role directly.
//  Guest chat (UcReceptionChat / tbl_Chat) is untouched.
// ============================================================
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project
{
    public class UcStaffChat : UserControl
    {
        // ── Palette ──────────────────────────────────────────────
        static readonly Color Forest       = Color.FromArgb(7,   26,  14);
        static readonly Color ForestMid    = Color.FromArgb(13,  40,  24);
        static readonly Color Gold         = Color.FromArgb(212, 160, 23);
        static readonly Color Cream        = Color.FromArgb(248, 244, 239);
        static readonly Color Sand         = Color.FromArgb(240, 237, 232);
        static readonly Color White        = Color.White;
        static readonly Color Border       = Color.FromArgb(221, 221, 221);
        static readonly Color TextDark     = Color.FromArgb(26,  26,  26);
        static readonly Color Muted        = Color.FromArgb(110, 105, 100);
        static readonly Color RecvBubble   = Color.FromArgb(232, 245, 238);
        static readonly Color BcastBubble  = Color.FromArgb(255, 247, 224);
        static readonly Color SidebarBg    = Color.FromArgb(246, 244, 240);
        static readonly Color ActiveCard   = Color.FromArgb(224, 240, 230);
        static readonly Color BadgeRed     = Color.FromArgb(210, 50,  50);

        const string Conn =
            "server=localhost;user=root;database=wildnest_db;" +
            "password=Natsudragneel_525;Allow User Variables=True;";

        // ── All active staff roles ────────────────────────────────
        // "Administrator" is preserved only as a legacy alias in helper logic.
        static readonly string[] AllRoles =
            { "Manager", "Reception", "TourGuide", "ZooKeeper" };

        // ── Instance state ────────────────────────────────────────
        readonly string _myRole;
        readonly string _myName;
        readonly string _myUsername;
        readonly Dictionary<string, PeerRow> _peerIndex = new(StringComparer.OrdinalIgnoreCase);

        Panel           _pnlLeft       = null!;
        Panel           _pnlShell      = null!;
        Panel           _pnlHeader     = null!;
        Panel           _pnlInput      = null!;
        Panel           _pnlEmpty      = null!;
        Panel           _pnlToast      = null!;
        FlowLayoutPanel _flowConvos    = null!;
        FlowLayoutPanel _flowMessages  = null!;
        FlowLayoutPanel _flowQuick     = null!;
        Label           _lblChatTitle  = null!;
        Label           _lblChatSub    = null!;
        Label           _lblUnread     = null!;
        Label           _lblToast      = null!;
        TextBox         _txtInput      = null!;
        Button          _btnSend       = null!;
        Button?         _btnBroadcast;
        System.Windows.Forms.Timer _timer      = null!;
        System.Windows.Forms.Timer _toastTimer = null!;

        string _selectedPeerUsername = "";
        string _selectedPeerName = "";
        string _selectedPeerRole = "";
        int    _lastMessageId;
        int    _lastUnreadTotal = -1;
        bool   _built;
        bool   _loadingConvos;
        bool   _loadingMessages;

        // ── Constructor ───────────────────────────────────────────
        public UcStaffChat(string myRole, string myName, string myUsername)
        {
            _myRole = NormalizeRole(string.IsNullOrWhiteSpace(myRole) ? "Staff" : myRole.Trim());
            _myName = string.IsNullOrWhiteSpace(myName) ? _myRole : myName.Trim();
            _myUsername = string.IsNullOrWhiteSpace(myUsername) ? NormalizeHandle(_myName) : myUsername.Trim();

            Dock = DockStyle.Fill;
            BackColor = Sand;
            DoubleBuffered = true;
            HandleCreated += (_, _) => BuildUi();
        }

        // ─────────────────────────────────────────────────────────
        //  UI BUILD
        // ─────────────────────────────────────────────────────────
        void BuildUi()
        {
            if (_built) return;
            _built = true;
            EnsureTable();
            SuspendLayout();
            Controls.Clear();

            var header  = BuildPageHeader();
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Sand };

            _pnlLeft  = BuildConversationPanel();
            _pnlShell = BuildChatShell();
            content.Controls.Add(_pnlShell);
            content.Controls.Add(_pnlLeft);

            _pnlToast = BuildToast();

            Controls.Add(content);
            Controls.Add(header);
            Controls.Add(_pnlToast);
            _pnlToast.BringToFront();

            Resize += (_, _) => PositionToast();
            ResumeLayout();
            PositionToast();

            // Initial data load
            LoadConversations();

            // Polling timer
            _timer = new System.Windows.Forms.Timer { Interval = 2500 };
            _timer.Tick += (_, _) =>
            {
                LoadConversations();
                if (!string.IsNullOrEmpty(_selectedPeerUsername))
                    LoadMessages(incrementalOnly: true);
            };
            _timer.Start();

            _toastTimer = new System.Windows.Forms.Timer { Interval = 3800 };
            _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); _pnlToast.Visible = false; };
        }

        // ── Page header ───────────────────────────────────────────
        Panel BuildPageHeader()
        {
            var hdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Forest };
            hdr.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var bg  = new LinearGradientBrush(hdr.ClientRectangle, Forest, ForestMid, LinearGradientMode.Vertical);
                using var pen = new Pen(Color.FromArgb(70, Gold));
                e.Graphics.FillRectangle(bg, hdr.ClientRectangle);
                e.Graphics.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            };

            hdr.Controls.Add(new Label
            {
                Text      = "Staff Chat",
                Location  = new Point(26, 10),
                Size      = new Size(260, 28),
                Font      = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = Cream,
                BackColor = Color.Transparent
            });

            hdr.Controls.Add(new Label
            {
                Text      = $"{DisplayRole(_myRole)}  ·  {_myName}  —  1-to-1 internal messaging",
                Location  = new Point(28, 40),
                Size      = new Size(760, 18),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(190, Cream),
                BackColor = Color.Transparent
            });

            _lblUnread = new Label
            {
                Visible   = false,
                AutoSize  = false,
                Size      = new Size(40, 22),
                Location  = new Point(172, 12),
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = BadgeRed
            };
            hdr.Controls.Add(_lblUnread);
            return hdr;
        }

        // ── Left sidebar: conversation list ───────────────────────
        Panel BuildConversationPanel()
        {
            var panel = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 310,
                BackColor = SidebarBg
            };
            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, panel.Width - 1, 0, panel.Width - 1, panel.Height);
            };

            // Section label
            panel.Controls.Add(new Label
            {
                Text      = "CONVERSATIONS",
                Location  = new Point(20, 18),
                Size      = new Size(220, 20),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Color.Transparent
            });

            // Sub-label with peer count
            panel.Controls.Add(new Label
            {
                Text      = "live staff directory",
                Location  = new Point(20, 36),
                Size      = new Size(200, 16),
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(160, Muted),
                BackColor = Color.Transparent
            });

            panel.Controls.Add(new Label
            {
                Text      = "Direct person-to-person coordination with unread priority and manager broadcast visibility.",
                Location  = new Point(20, 52),
                Size      = new Size(260, 32),
                Font      = new Font("Segoe UI", 7.4f),
                ForeColor = Color.FromArgb(154, Muted),
                BackColor = Color.Transparent
            });

            _flowConvos = new FlowLayoutPanel
            {
                Location      = new Point(0, 92),
                Size          = new Size(panel.Width, panel.Height - 92),
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = Color.Transparent,
                Padding       = Padding.Empty
            };
            panel.Controls.Add(_flowConvos);
            return panel;
        }

        // ── Chat shell: header + messages + input ─────────────────
        Panel BuildChatShell()
        {
            var shell = new Panel { Dock = DockStyle.Fill, BackColor = Sand };

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = Sand,
                Margin      = Padding.Empty,
                Padding     = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f));

            // Chat sub-header
            _pnlHeader = new Panel { Dock = DockStyle.Fill, BackColor = White };
            _pnlHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            _lblChatTitle = new Label
            {
                Text      = "Select a conversation",
                Location  = new Point(28, 14),
                Size      = new Size(700, 26),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = TextDark,
                BackColor = Color.Transparent
            };
            _pnlHeader.Controls.Add(_lblChatTitle);

            _lblChatSub = new Label
            {
                Text      = "Choose a staff contact from the list on the left.",
                Location  = new Point(28, 42),
                Size      = new Size(700, 20),
                Font      = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(0, 120, 55),
                BackColor = Color.Transparent
            };
            _pnlHeader.Controls.Add(_lblChatSub);

            // Messages flow
            _flowMessages = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                AutoScroll    = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                Padding       = new Padding(26, 34, 26, 34),
                BackColor     = Sand
            };

            // Empty state
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = Sand };
            var emptyIcon = new Label
            {
                Text      = "CHAT READY",
                Size      = new Size(140, 28),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(26, Gold)
            };
            emptyIcon.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedPath(new Rectangle(0, 0, emptyIcon.Width - 1, emptyIcon.Height - 1), 14);
                using var fill = new SolidBrush(emptyIcon.BackColor);
                using var pen  = new Pen(Color.FromArgb(92, Gold));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
                TextRenderer.DrawText(e.Graphics, emptyIcon.Text, emptyIcon.Font, emptyIcon.ClientRectangle, emptyIcon.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            var emptyTitle = new Label
            {
                Text      = "Select a conversation to begin",
                Size      = new Size(430, 34),
                Font      = new Font("Georgia", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(78, 68, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var emptyBody = new Label
            {
                Text      = "Choose any staff contact from the left to start a private coordination thread. Managers can also issue all-staff broadcasts from this workspace.",
                Size      = new Size(520, 46),
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.TopCenter,
                BackColor = Color.Transparent
            };

            _pnlEmpty.Controls.Add(emptyIcon);
            _pnlEmpty.Controls.Add(emptyTitle);
            _pnlEmpty.Controls.Add(emptyBody);
            _pnlEmpty.Resize += (_, _) =>
            {
                int centerX = _pnlEmpty.Width / 2;
                int top = Math.Max(80, (_pnlEmpty.Height - 150) / 2);
                emptyIcon.Location  = new Point(centerX - (emptyIcon.Width / 2), top);
                emptyTitle.Location = new Point(centerX - (emptyTitle.Width / 2), top + 40);
                emptyBody.Location  = new Point(centerX - (emptyBody.Width / 2), top + 80);
            };

            _pnlInput = BuildInputPanel();

            var messageHost = new Panel { Dock = DockStyle.Fill, BackColor = Sand };
            messageHost.Controls.Add(_flowMessages);
            messageHost.Controls.Add(_pnlEmpty);
            _pnlEmpty.BringToFront();
            _pnlInput.Dock = DockStyle.Fill;

            layout.Controls.Add(_pnlHeader,   0, 0);
            layout.Controls.Add(messageHost,  0, 1);
            layout.Controls.Add(_pnlInput,    0, 2);
            shell.Controls.Add(layout);
            return shell;
        }

        // ── Input bar ─────────────────────────────────────────────
        Panel BuildInputPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Height = 108, BackColor = White };
            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
            };

            // Quick replies
            _flowQuick = new FlowLayoutPanel
            {
                Location      = new Point(24, 8),
                Size          = new Size(680, 30),
                Anchor        = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            foreach (var q in new[] { "Received", "On my way", "Please wait", "Need assistance" })
                _flowQuick.Controls.Add(QuickReplyBtn(q));
            panel.Controls.Add(_flowQuick);

            panel.Controls.Add(new Label
            {
                Text      = IsCentralRole(_myRole)
                    ? "Private thread by default. Use broadcast only for operational alerts that every staff role must see."
                    : "Private operational thread. Messages stay within the selected staff conversation.",
                AutoSize  = false,
                Size      = new Size(620, 18),
                Location  = new Point(24, 38),
                Font      = new Font("Segoe UI", 7.8f),
                ForeColor = Muted,
                BackColor = Color.Transparent
            });

            _btnSend = new Button
            {
                Text      = "Send Message",
                Anchor    = AnchorStyles.Right | AnchorStyles.Bottom,
                Size      = new Size(124, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Forest,
                ForeColor = Gold,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (_, _) => SendMessage();
            panel.Controls.Add(_btnSend);

            // Broadcast only for Manager/Administrator
            if (IsCentralRole(_myRole))
            {
                _btnBroadcast = new Button
                {
                    Text      = "Broadcast to All",
                    Anchor    = AnchorStyles.Right | AnchorStyles.Top,
                    Size      = new Size(138, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Gold,
                    ForeColor = Forest,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Cursor    = Cursors.Hand
                };
                _btnBroadcast.FlatAppearance.BorderSize = 0;
                _btnBroadcast.Click += (_, _) => SendBroadcast();
                panel.Controls.Add(_btnBroadcast);
            }

            _txtInput = new TextBox
            {
                Anchor      = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Height      = 38,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 10f),
                ForeColor   = TextDark,
                BackColor   = White
            };
            _txtInput.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift) { e.SuppressKeyPress = true; SendMessage(); }
            };
            panel.Controls.Add(_txtInput);

            panel.Resize += (_, _) => PositionInputControls();
            PositionInputControls();
            return panel;
        }

        Button QuickReplyBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Size      = new Size(112, 26),
                Margin    = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(248, 246, 242),
                ForeColor = TextDark,
                Font      = new Font("Segoe UI", 8f),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.BorderSize  = 1;
            b.Click += (_, _) => { if (_txtInput != null) { _txtInput.Text = text; SendMessage(); } };
            return b;
        }

        void PositionInputControls()
        {
            if (_pnlInput == null || _txtInput == null || _btnSend == null) return;
            int rp = 18, lp = 24;
            int y  = _pnlInput.Height - 52;

            _btnSend.Location = new Point(_pnlInput.Width - _btnSend.Width - rp, y);

            int broadW = 0;
            if (_btnBroadcast != null)
            {
                _btnBroadcast.Location = new Point(_pnlInput.Width - _btnBroadcast.Width - rp, 8);
                broadW = _btnBroadcast.Width + 12;
            }
            _flowQuick.Width  = Math.Max(240, _pnlInput.Width - lp - rp - broadW);
            _txtInput.Location = new Point(lp, y);
            _txtInput.Width    = Math.Max(220, _btnSend.Left - lp - 12);
        }

        // ── Toast notification ────────────────────────────────────
        Panel BuildToast()
        {
            var toast = new Panel { Size = new Size(360, 54), BackColor = Forest, Visible = false };
            toast.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedPath(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 14);
                using var bg   = new SolidBrush(Forest);
                using var pen  = new Pen(Color.FromArgb(120, Gold));
                e.Graphics.FillPath(bg,  path);
                e.Graphics.DrawPath(pen, path);
            };
            toast.Controls.Add(new Label
            {
                Text      = "New staff message",
                Location  = new Point(18, 8),
                Size      = new Size(315, 18),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Gold,
                BackColor = Color.Transparent
            });
            _lblToast = new Label
            {
                Location  = new Point(18, 27),
                Size      = new Size(320, 20),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Cream,
                BackColor = Color.Transparent
            };
            toast.Controls.Add(_lblToast);
            return toast;
        }

        void PositionToast()
        {
            if (_pnlToast == null) return;
            _pnlToast.Location = new Point(Math.Max(20, Width - _pnlToast.Width - 24), 78);
            _pnlToast.BringToFront();
        }

        void ShowToast(string text)
        {
            if (_pnlToast == null || _lblToast == null) return;
            _lblToast.Text = text.Length > 55 ? text[..55] + "…" : text;
            _pnlToast.Visible = true;
            _pnlToast.BringToFront();
            _toastTimer?.Stop();
            _toastTimer?.Start();
        }

        // ─────────────────────────────────────────────────────────
        //  DATA — Conversations
        // ─────────────────────────────────────────────────────────
        void LoadConversations()
        {
            if (_loadingConvos) return;
            _loadingConvos = true;

            Task.Run(() =>
            {
                var items       = new List<ConvoRow>();
                int unreadTotal = 0;

                try
                {
                    var peers = StaffPortalDb.GetActiveStaffDirectory(_myUsername);
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();

                    foreach (DataRow peer in peers.Rows)
                    {
                        string peerUsername = Convert.ToString(peer["Username"]) ?? string.Empty;
                        string peerName = Convert.ToString(peer["FullName"]) ?? peerUsername;
                        string peerRole = NormalizeRole(Convert.ToString(peer["Role"]) ?? "Staff");
                        if (string.IsNullOrWhiteSpace(peerUsername))
                            continue;

                        // Last message in this thread
                        string preview  = "No messages yet";
                        DateTime lastAt = DateTime.MinValue;
                        int unread      = 0;

                        using (var cmd = new MySqlCommand(@"
                            SELECT Message, SentAt
                            FROM   tbl_StaffMessages
                            WHERE  (SenderUsername = @meUser   AND ReceiverUsername = @peerUser)
                               OR  (SenderUsername = @peerUser AND ReceiverUsername = @meUser)
                               OR  (ReceiverRole = 'All' AND (SenderUsername = @peerUser OR (COALESCE(SenderUsername,'') = '' AND SenderRole = @peerRole)))
                            ORDER  BY SentAt DESC, MessageID DESC
                            LIMIT  1;", conn))
                        {
                            cmd.Parameters.AddWithValue("@meUser", _myUsername);
                            cmd.Parameters.AddWithValue("@peerUser", peerUsername);
                            cmd.Parameters.AddWithValue("@peerRole", peerRole);
                            using var r = cmd.ExecuteReader();
                            if (r.Read())
                            {
                                preview = r["Message"]?.ToString() ?? "";
                                lastAt  = Convert.ToDateTime(r["SentAt"]);
                            }
                        }

                        // Unread = messages sent TO me from this peer that I haven't read
                        using (var cmd = new MySqlCommand(@"
                            SELECT COUNT(*)
                            FROM   tbl_StaffMessages
                            WHERE  (
                                       (SenderUsername = @peerUser AND ReceiverUsername = @meUser)
                                    OR (ReceiverRole = 'All' AND (SenderUsername = @peerUser OR (COALESCE(SenderUsername,'') = '' AND SenderRole = @peerRole)))
                                   )
                              AND  IsRead = 0;", conn))
                        {
                            cmd.Parameters.AddWithValue("@meUser", _myUsername);
                            cmd.Parameters.AddWithValue("@peerUser", peerUsername);
                            cmd.Parameters.AddWithValue("@peerRole", peerRole);
                            unread = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        unreadTotal += unread;
                        items.Add(new ConvoRow(peerUsername, peerName, peerRole, preview, lastAt, unread));
                    }
                }
                catch (Exception ex) { ProjectDiagnostics.LogError("UcStaffChat", ex, "LoadConversations"); }

                // Sort: unread first, then by most recent
                items.Sort((a, b) =>
                {
                    if (b.Unread != a.Unread) return b.Unread.CompareTo(a.Unread);
                    return b.LastTime.CompareTo(a.LastTime);
                });

                BeginInvokeSafe(() =>
                {
                    RebuildConversations(items);

                    int visibleUnreadTotal = 0;
                    foreach (var item in items)
                        visibleUnreadTotal += item.Username.Equals(_selectedPeerUsername, StringComparison.OrdinalIgnoreCase) ? 0 : item.Unread;

                    if (_lblUnread != null)
                    {
                        _lblUnread.Visible = visibleUnreadTotal > 0;
                        _lblUnread.Text    = visibleUnreadTotal > 99 ? "99+" : visibleUnreadTotal.ToString();
                    }

                    if (_lastUnreadTotal >= 0 && unreadTotal > _lastUnreadTotal)
                        ShowToast("A staff member sent you a new message.");
                    _lastUnreadTotal = unreadTotal;
                    _loadingConvos   = false;
                });
            });
        }

        void RebuildConversations(List<ConvoRow> items)
        {
            if (_flowConvos == null) return;
            _flowConvos.SuspendLayout();
            _flowConvos.Controls.Clear();
            foreach (var item in items)
                _flowConvos.Controls.Add(BuildConversationCard(item));
            _flowConvos.ResumeLayout();
        }

        Control BuildConversationCard(ConvoRow row)
        {
            bool selected = row.Username.Equals(_selectedPeerUsername, StringComparison.OrdinalIgnoreCase);
            int visualUnread = selected ? 0 : row.Unread;
            bool hasUnread = visualUnread > 0;

            var card = new Panel
            {
                Size      = new Size(310, 82),
                Margin    = Padding.Empty,
                BackColor = selected ? ActiveCard : Color.Transparent,
                Cursor    = Cursors.Hand
            };
            card.Paint += (_, e) =>
            {
                // Bottom separator
                using var pen = new Pen(Color.FromArgb(226, 226, 226));
                e.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);

                // Left accent bar when selected
                if (selected)
                {
                    using var accent = new SolidBrush(Gold);
                    e.Graphics.FillRectangle(accent, 0, 12, 4, card.Height - 24);
                }
            };

            // Avatar circle
            var avatar = new Label
            {
                Text      = Initials(row.Role),
                Location  = new Point(16, 19),
                Size      = new Size(42, 42),
                BackColor = Forest,
                ForeColor = Gold,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            card.Controls.Add(avatar);

            // Role name
            card.Controls.Add(new Label
            {
                Text      = row.Name,
                Location  = new Point(68, 14),
                Size      = new Size(190, 20),
                Font      = new Font("Segoe UI", 9.5f, hasUnread ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = TextDark,
                BackColor = Color.Transparent
            });

            // Last-active timestamp
            card.Controls.Add(new Label
            {
                Text      = row.LastTime == DateTime.MinValue ? "" : FormatTime(row.LastTime),
                Location  = new Point(214, 15),
                Size      = new Size(82, 18),
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            });

            card.Controls.Add(new Label
            {
                Text      = $"{DisplayRole(row.Role)}  ·  @{row.Username}",
                Location  = new Point(68, 31),
                Size      = new Size(186, 16),
                Font      = new Font("Segoe UI", 7.4f),
                ForeColor = Muted,
                BackColor = Color.Transparent
            });

            // Message preview
            card.Controls.Add(new Label
            {
                Text      = TrimPreview(row.Preview),
                Location  = new Point(68, 49),
                Size      = new Size(195, 20),
                Font      = new Font("Segoe UI", 8f, hasUnread ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = hasUnread ? TextDark : Muted,
                BackColor = Color.Transparent
            });

            // Unread badge
            if (hasUnread)
            {
                card.Controls.Add(new Label
                {
                    Text      = visualUnread > 99 ? "99+" : visualUnread.ToString(),
                    Location  = new Point(262, 36),
                    Size      = new Size(34, 22),
                    Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = White,
                    BackColor = BadgeRed,
                    TextAlign = ContentAlignment.MiddleCenter
                });
            }

            void Select()
            {
                _selectedPeerUsername = row.Username;
                _selectedPeerName = row.Name;
                _selectedPeerRole = row.Role;
                _lastMessageId  = 0;
                ShowChatArea();
                LoadConversations();
                LoadMessages(incrementalOnly: false);
            }

            card.Click += (_, _) => Select();
            foreach (Control c in card.Controls) c.Click += (_, _) => Select();
            return card;
        }

        // ─────────────────────────────────────────────────────────
        //  DATA — Messages
        // ─────────────────────────────────────────────────────────
        void ShowChatArea()
        {
            if (string.IsNullOrEmpty(_selectedPeerUsername)) return;
            _pnlEmpty.Visible    = false;
            _flowMessages.Visible = true;
            _pnlInput.Visible    = true;

            _lblChatTitle.Text = _selectedPeerName;
            _lblChatSub.Text   = $"{DisplayRole(_selectedPeerRole)}  ·  @{_selectedPeerUsername}  —  private person-to-person staff thread";
        }

        void LoadMessages(bool incrementalOnly)
        {
            if (string.IsNullOrEmpty(_selectedPeerUsername) || _loadingMessages) return;
            _loadingMessages = true;

            string meUser = _myUsername;
            string peerUser = _selectedPeerUsername;
            string peerRole = _selectedPeerRole;
            int    from = incrementalOnly ? _lastMessageId : 0;

            Task.Run(() =>
            {
                var rows = new List<MessageRow>();
                try
                {
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();

                    // Load the thread: messages between me and peer, plus any broadcast
                    using (var cmd = new MySqlCommand(@"
                        SELECT MessageID, SenderRole, SenderName, SenderUsername, ReceiverRole, ReceiverUsername, Message, SentAt
                        FROM   tbl_StaffMessages
                        WHERE  (
                                   (SenderUsername = @meUser   AND ReceiverUsername = @peerUser)
                                OR (SenderUsername = @peerUser AND ReceiverUsername = @meUser)
                                OR (ReceiverRole = 'All' AND (SenderUsername = @peerUser OR (COALESCE(SenderUsername,'') = '' AND SenderRole = @peerRole)))
                               )
                          AND  MessageID > @from
                        ORDER  BY SentAt ASC, MessageID ASC;", conn))
                    {
                        cmd.Parameters.AddWithValue("@meUser", meUser);
                        cmd.Parameters.AddWithValue("@peerUser", peerUser);
                        cmd.Parameters.AddWithValue("@peerRole", peerRole);
                        cmd.Parameters.AddWithValue("@from", from);
                        using var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            rows.Add(new MessageRow(
                                Convert.ToInt32(r["MessageID"]),
                                r["SenderRole"]?.ToString()   ?? "",
                                r["SenderName"]?.ToString()   ?? "",
                                r["SenderUsername"]?.ToString() ?? "",
                                r["ReceiverRole"]?.ToString() ?? "",
                                r["ReceiverUsername"]?.ToString() ?? "",
                                r["Message"]?.ToString()      ?? "",
                                Convert.ToDateTime(r["SentAt"])));
                        }
                    }

                    // Mark messages from peer as read
                    MarkRead(conn, meUser, peerUser, peerRole);
                }
                catch (Exception ex) { ProjectDiagnostics.LogError("UcStaffChat", ex, "LoadMessages"); }

                BeginInvokeSafe(() =>
                {
                    if (!incrementalOnly) { _flowMessages.Controls.Clear(); _lastMessageId = 0; }

                    if (rows.Count > 0)
                    {
                        bool notify = incrementalOnly && rows.Exists(r => !r.SenderUsername.Equals(meUser, StringComparison.OrdinalIgnoreCase));
                        foreach (var row in rows)
                        {
                            _flowMessages.Controls.Add(BuildBubble(row));
                            _lastMessageId = Math.Max(_lastMessageId, row.Id);
                        }
                        ScrollToBottom();
                        if (notify) ShowToast($"New message from {rows[^1].DisplaySender}");
                    }
                    LoadConversations();
                    _loadingMessages = false;
                });
            });
        }

        void MarkRead(MySqlConnection conn, string meUser, string peerUser, string peerRole)
        {
            try
            {
                using var cmd = new MySqlCommand(@"
                    UPDATE tbl_StaffMessages
                    SET    IsRead = 1
                    WHERE  (
                               (SenderUsername = @peerUser AND ReceiverUsername = @meUser)
                            OR (ReceiverRole = 'All' AND (SenderUsername = @peerUser OR (COALESCE(SenderUsername,'') = '' AND SenderRole = @peerRole)))
                           )
                      AND  IsRead = 0;", conn);
                cmd.Parameters.AddWithValue("@meUser", meUser);
                cmd.Parameters.AddWithValue("@peerUser", peerUser);
                cmd.Parameters.AddWithValue("@peerRole", peerRole);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { ProjectDiagnostics.LogError("UcStaffChat", ex, "MarkRead"); }
        }

        // ─────────────────────────────────────────────────────────
        //  SEND
        // ─────────────────────────────────────────────────────────
        void SendMessage()
        {
            if (_txtInput == null) return;
            string msg = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            if (string.IsNullOrEmpty(_selectedPeerUsername))
            {
                MessageBox.Show("Please select a staff contact first.", "WildNest",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DoSend(msg, _selectedPeerUsername, _selectedPeerRole);
        }

        void SendBroadcast()
        {
            if (_txtInput == null) return;
            string msg = _txtInput.Text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            var confirm = MessageBox.Show(
                $"Send this message to ALL staff?\n\n\"{msg}\"",
                "WildNest — Broadcast",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            DoSend(msg, string.Empty, "All");
        }

        void DoSend(string msg, string receiverUsername, string receiverRole)
        {
            try
            {
                using var conn = new MySqlConnection(Conn);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    INSERT INTO tbl_StaffMessages
                        (SenderRole, SenderName, SenderUsername, ReceiverRole, ReceiverUsername, Message, SentAt, IsRead)
                    VALUES
                        (@senderRole, @senderName, @senderUsername, @receiverRole, @receiverUsername, @message, NOW(), 0);", conn);
                cmd.Parameters.AddWithValue("@senderRole",   _myRole);
                cmd.Parameters.AddWithValue("@senderName",   _myName);
                cmd.Parameters.AddWithValue("@senderUsername", _myUsername);
                cmd.Parameters.AddWithValue("@receiverRole", receiverRole);
                cmd.Parameters.AddWithValue("@receiverUsername", string.IsNullOrWhiteSpace(receiverUsername) ? DBNull.Value : receiverUsername);
                cmd.Parameters.AddWithValue("@message",      msg);
                cmd.ExecuteNonQuery();

                _txtInput.Clear();
                if (!string.IsNullOrEmpty(_selectedPeerUsername))
                    LoadMessages(incrementalOnly: true);
                LoadConversations();
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("UcStaffChat", ex, "DoSend");
                MessageBox.Show("Unable to send message. Please try again.", "WildNest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ─────────────────────────────────────────────────────────
        //  BUBBLE RENDERING
        // ─────────────────────────────────────────────────────────
        Control BuildBubble(MessageRow row)
        {
            bool mine      = row.SenderUsername.Equals(_myUsername, StringComparison.OrdinalIgnoreCase);
            bool broadcast = row.ReceiverRole.Equals("All", StringComparison.OrdinalIgnoreCase);

            int available  = Math.Max(520, _flowMessages.ClientSize.Width - 70);
            int maxBubbleW = Math.Min(560, Math.Max(260, (int)(available * 0.62)));

            using var mf = new Font("Segoe UI", 10f);
            var measured = TextRenderer.MeasureText(row.Message, mf, new Size(maxBubbleW - 40, 0),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

            int bW = Math.Min(maxBubbleW, Math.Max(190, measured.Width + 52));
            int bH = Math.Max(58, measured.Height + 36);

            var line = new Panel
            {
                Width     = Math.Max(available, _flowMessages.ClientSize.Width - 56),
                Height    = bH + 32,
                Margin    = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };

            var bubble = new Panel { Size = new Size(bW, bH), BackColor = Color.Transparent };
            bubble.Location = mine
                ? new Point(Math.Max(12, line.Width - bW - 14), 0)
                : new Point(14, 0);

            bubble.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color bg   = broadcast ? BcastBubble : (mine ? Forest : RecvBubble);
                Color edge = broadcast ? Gold         : (mine ? Forest : Color.FromArgb(204, 224, 214));
                using var path  = RoundedPath(new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1), 14);
                using var brush = new SolidBrush(bg);
                using var pen   = new Pen(edge);
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            };

            // Broadcast icon
            if (broadcast)
            {
                bubble.Controls.Add(new Label
                {
                    Text      = "📢",
                    Location  = new Point(10, 8),
                    Size      = new Size(24, 22),
                    Font      = new Font("Segoe UI", 9f),
                    BackColor = Color.Transparent
                });
            }

            int textX = broadcast ? 34 : 18;
            bubble.Controls.Add(new Label
            {
                Text      = row.Message,
                Location  = new Point(textX, 12),
                Size      = new Size(Math.Max(80, bW - textX - 18), Math.Max(26, bH - 24)),
                AutoSize  = false,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = broadcast ? Color.FromArgb(90, 55, 6) : (mine ? Cream : TextDark),
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = true
            });
            line.Controls.Add(bubble);

            // Sender/time label
            string senderLabel = mine ? $"You ({_myName})" : row.DisplaySender;
            if (broadcast) senderLabel += " · broadcast";
            line.Controls.Add(new Label
            {
                Text      = $"{senderLabel}  ·  {row.SentAt:h:mm tt}".ToLowerInvariant(),
                Location  = mine
                    ? new Point(Math.Max(12, bubble.Left + bW - 220), bubble.Bottom + 5)
                    : new Point(bubble.Left + 4, bubble.Bottom + 5),
                Size      = new Size(220, 18),
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Muted,
                TextAlign = mine ? ContentAlignment.MiddleRight : ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            });

            return line;
        }

        // ─────────────────────────────────────────────────────────
        //  DB SETUP
        // ─────────────────────────────────────────────────────────
        void EnsureTable()
        {
            try
            {
                using var conn = new MySqlConnection(Conn);
                conn.Open();
                using var cmd = new MySqlCommand(@"
                    CREATE TABLE IF NOT EXISTS tbl_StaffMessages (
                        MessageID    INT          AUTO_INCREMENT PRIMARY KEY,
                        SenderRole   VARCHAR(50)  NOT NULL,
                        SenderName   VARCHAR(100) NOT NULL,
                        ReceiverRole VARCHAR(50)  NOT NULL,
                        Message      TEXT         NOT NULL,
                        SentAt       DATETIME     DEFAULT NOW(),
                        IsRead       BOOLEAN      DEFAULT FALSE
                    );", conn);
                cmd.ExecuteNonQuery();
                StaffPortalDb.EnsureStaffMessageRoutingColumns();
            }
            catch (Exception ex) { ProjectDiagnostics.LogError("UcStaffChat", ex, "EnsureTable"); }
        }

        // ─────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────
        void ScrollToBottom()
        {
            if (_flowMessages == null || _flowMessages.Controls.Count == 0) return;
            _flowMessages.ScrollControlIntoView(_flowMessages.Controls[^1]);
        }

        void BeginInvokeSafe(Action action)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(action); } catch { }
        }

        static string NormalizeRole(string role) => role switch
        {
            "Administrator" => "Manager",
            _               => role
        };

        static string NormalizeHandle(string value)
        {
            value = value?.Trim().ToLowerInvariant() ?? "staff";
            return string.IsNullOrWhiteSpace(value) ? "staff" : value.Replace(" ", "");
        }

        static bool IsCentralRole(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Administrator", StringComparison.OrdinalIgnoreCase);

        static string DisplayRole(string role) => role switch
        {
            "Administrator" => "Manager",
            "Manager"       => "Manager",
            "TourGuide"     => "Tour Guide",
            "ZooKeeper"     => "Zoo Keeper",
            _               => role
        };

        static string Initials(string role)
        {
            string d = DisplayRole(role);
            var parts = d.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return (parts[0].Length >= 2 ? parts[0][..2] : parts[0]).ToUpperInvariant();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        static string FormatTime(DateTime dt)
        {
            if (dt == DateTime.MinValue) return "";
            if (dt.Date == DateTime.Today) return dt.ToString("h:mm tt");
            if (dt.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return dt.ToString("MMM d");
        }

        static string TrimPreview(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "No messages yet";
            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length > 36 ? text[..36] + "…" : text;
        }

        static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(rect.X,           rect.Y,            d, d, 180, 90);
            p.AddArc(rect.Right - d,   rect.Y,            d, d, 270, 90);
            p.AddArc(rect.Right - d,   rect.Bottom - d,   d, d,   0, 90);
            p.AddArc(rect.X,           rect.Bottom - d,   d, d,  90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();      _timer?.Dispose();
                _toastTimer?.Stop(); _toastTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ─────────────────────────────────────────────────────────
        //  DATA RECORDS
        // ─────────────────────────────────────────────────────────
        record PeerRow(string Username, string Name, string Role);
        record ConvoRow(string Username, string Name, string Role, string Preview, DateTime LastTime, int Unread);
        record MessageRow(int Id, string SenderRole, string SenderName, string SenderUsername,
                          string ReceiverRole, string ReceiverUsername, string Message, DateTime SentAt)
        {
            public string DisplaySender => string.IsNullOrWhiteSpace(SenderName) ? DisplayRole(SenderRole) : $"{SenderName}";
        }
    }
}

