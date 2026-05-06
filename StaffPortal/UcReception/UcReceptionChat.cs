using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcReception
{
    public class UcReceptionChat : UserControl
    {
        static readonly Color Forest = Color.FromArgb(7, 26, 14);
        static readonly Color ForestMid = Color.FromArgb(13, 40, 24);
        static readonly Color Gold = Color.FromArgb(212, 160, 23);
        static readonly Color Cream = Color.FromArgb(248, 244, 239);
        static readonly Color Sand = Color.FromArgb(240, 237, 232);
        static readonly Color White = Color.White;
        static readonly Color Border = Color.FromArgb(221, 221, 221);
        static readonly Color TextDark = Color.FromArgb(26, 26, 26);
        static readonly Color Muted = Color.FromArgb(110, 105, 100);
        static readonly Color GuestBubble = Color.FromArgb(232, 245, 238);
        static readonly Color SoftBlue = Color.FromArgb(228, 239, 252);

        const string Conn =
            "server=localhost;user=root;database=wildnest_db;" +
            "password=Natsudragneel_525;Allow User Variables=True;";

        readonly string _myUsername;
        readonly string _myName;

        Panel _pnlLeft = null!;
        Panel _pnlShell = null!;
        Panel _pnlHeader = null!;
        Panel _pnlInput = null!;
        Panel _pnlEmpty = null!;
        Panel _pnlToast = null!;
        FlowLayoutPanel _flowConvos = null!;
        FlowLayoutPanel _flowMessages = null!;
        Label _lblGuest = null!;
        Label _lblUnread = null!;
        Label _lblToast = null!;
        TextBox _txtReply = null!;
        Button _btnSend = null!;
        System.Windows.Forms.Timer _timer = null!;
        System.Windows.Forms.Timer _toastTimer = null!;

        string _selectedReservationId = "";
        string _selectedGuestName = "";
        int _lastChatId;
        int _lastUnreadTotal = -1;
        bool _built;
        bool _loadingConvos;
        bool _loadingMessages;

        public UcReceptionChat(string myUsername, string myName)
        {
            _myUsername = string.IsNullOrWhiteSpace(myUsername) ? "reception" : myUsername.Trim();
            _myName = string.IsNullOrWhiteSpace(myName) ? "Reception" : myName.Trim();
            Dock = DockStyle.Fill;
            BackColor = Sand;
            DoubleBuffered = true;
            HandleCreated += (_, _) => BuildUi();
        }

        void BuildUi()
        {
            if (_built) return;
            _built = true;
            StaffPortalDb.EnsureGuestChatAssignmentColumns();
            SuspendLayout();
            Controls.Clear();

            var pageHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = White
            };
            pageHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, pageHeader.Height - 1, pageHeader.Width, pageHeader.Height - 1);
            };
            pageHeader.Controls.Add(new Label
            {
                Text = "Guest Chat",
                Font = new Font("Georgia", 17f, FontStyle.Bold),
                ForeColor = Forest,
                AutoSize = true,
                Location = new Point(24, 15),
                BackColor = Color.Transparent
            });
            _lblUnread = new Label
            {
                Visible = false,
                AutoSize = false,
                Size = new Size(36, 22),
                Location = new Point(166, 18),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(210, 50, 50)
            };
            pageHeader.Controls.Add(_lblUnread);

            _pnlLeft = BuildConversationPanel();
            _pnlShell = BuildChatShell();
            _pnlToast = BuildToast();

            Controls.Add(_pnlShell);
            Controls.Add(_pnlLeft);
            Controls.Add(pageHeader);
            Controls.Add(_pnlToast);
            _pnlToast.BringToFront();
            Resize += (_, _) => PositionToast();
            ResumeLayout();
            PositionToast();

            LoadConversations();
            _timer = new System.Windows.Forms.Timer { Interval = 2500 };
            _timer.Tick += (_, _) =>
            {
                LoadConversations();
                if (!string.IsNullOrEmpty(_selectedReservationId))
                    LoadMessages(incrementalOnly: true);
            };
            _timer.Start();

            _toastTimer = new System.Windows.Forms.Timer { Interval = 3600 };
            _toastTimer.Tick += (_, _) =>
            {
                _toastTimer.Stop();
                _pnlToast.Visible = false;
            };
        }

        Panel BuildToast()
        {
            var toast = new Panel
            {
                Size = new Size(360, 54),
                BackColor = Forest,
                Visible = false
            };
            toast.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Rounded(new Rectangle(0, 0, toast.Width - 1, toast.Height - 1), 14);
                using var bg = new SolidBrush(Forest);
                using var pen = new Pen(Color.FromArgb(120, Gold));
                e.Graphics.FillPath(bg, path);
                e.Graphics.DrawPath(pen, path);
            };

            toast.Controls.Add(new Label
            {
                Text = "New guest message",
                Location = new Point(18, 9),
                Size = new Size(315, 18),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Gold,
                BackColor = Color.Transparent
            });

            _lblToast = new Label
            {
                Text = "",
                Location = new Point(18, 27),
                Size = new Size(320, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Cream,
                BackColor = Color.Transparent
            };
            toast.Controls.Add(_lblToast);
            return toast;
        }

        void PositionToast()
        {
            if (_pnlToast == null) return;
            _pnlToast.Location = new Point(
                Math.Max(20, Width - _pnlToast.Width - 24),
                70);
            _pnlToast.BringToFront();
        }

        void ShowToast(string message)
        {
            if (IsDisposed || _pnlToast == null || _lblToast == null) return;
            _lblToast.Text = Shorten(message, 52);
            PositionToast();
            _pnlToast.Visible = true;
            _pnlToast.BringToFront();
            _toastTimer?.Stop();
            _toastTimer?.Start();
        }

        Panel BuildConversationPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = White
            };
            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, panel.Width - 1, 0, panel.Width - 1, panel.Height);
            };

            var title = new Label
            {
                Text = "CONVERSATIONS",
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(18, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Muted,
                BackColor = Sand
            };

            _flowConvos = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = White,
                Padding = new Padding(0)
            };
            _flowConvos.HorizontalScroll.Enabled = false;
            _flowConvos.HorizontalScroll.Visible = false;

            panel.Controls.Add(_flowConvos);
            panel.Controls.Add(title);
            return panel;
        }

        Panel BuildChatShell()
        {
            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Sand,
                Padding = new Padding(0)
            };

            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = White,
                Visible = false
            };
            _pnlHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };
            _lblGuest = new Label
            {
                Text = "",
                AutoSize = false,
                Location = new Point(24, 13),
                Size = new Size(900, 26),
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = TextDark,
                BackColor = Color.Transparent
            };
            _pnlHeader.Controls.Add(_lblGuest);
            _pnlHeader.Controls.Add(new Label
            {
                Text = $"Assigned reception owner: {_myName}",
                AutoSize = false,
                Location = new Point(24, 40),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(34, 139, 34),
                BackColor = Color.Transparent
            });

            _pnlInput = BuildInputPanel();
            _pnlInput.Visible = false;

            _flowMessages = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Sand,
                Padding = new Padding(24, 38, 24, 26)
            };
            _flowMessages.HorizontalScroll.Enabled = false;
            _flowMessages.HorizontalScroll.Visible = false;
            _flowMessages.Resize += (_, _) =>
            {
                if (!string.IsNullOrEmpty(_selectedReservationId) && _flowMessages.Controls.Count > 0)
                    LoadMessages();
            };

            _pnlEmpty = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Sand
            };
            var empty = new Label
            {
                Text = "Select a guest conversation to view messages.",
                AutoSize = false,
                Size = new Size(420, 80),
                Font = new Font("Segoe UI", 11f),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _pnlEmpty.Controls.Add(empty);
            _pnlEmpty.Resize += (_, _) =>
            {
                empty.Location = new Point(
                    Math.Max(20, (_pnlEmpty.Width - empty.Width) / 2),
                    Math.Max(40, (_pnlEmpty.Height - empty.Height) / 2));
            };

            shell.Controls.Add(_flowMessages);
            shell.Controls.Add(_pnlInput);
            shell.Controls.Add(_pnlHeader);
            shell.Controls.Add(_pnlEmpty);
            _pnlEmpty.BringToFront();
            return shell;
        }

        Panel BuildInputPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = White
            };
            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
            };

            _btnSend = new Button
            {
                Text = "Send",
                Size = new Size(86, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Forest,
                ForeColor = Gold,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (_, _) => SendMessage();

            _txtReply = new TextBox
            {
                Multiline = false,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f),
                BackColor = White,
                ForeColor = TextDark,
                PlaceholderText = "Type your reply...",
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _txtReply.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendMessage();
                }
            };

            panel.Controls.Add(_txtReply);
            panel.Controls.Add(_btnSend);
            panel.Resize += (_, _) => LayoutInput();
            LayoutInput();
            return panel;
        }

        void LayoutInput()
        {
            if (_pnlInput == null || _txtReply == null || _btnSend == null) return;
            int pad = 18;
            _btnSend.Location = new Point(Math.Max(pad, _pnlInput.Width - pad - _btnSend.Width), 18);
            _txtReply.Location = new Point(pad, 18);
            _txtReply.Size = new Size(Math.Max(240, _btnSend.Left - pad - 12), 42);
        }

        void LoadConversations()
        {
            if (_loadingConvos || IsDisposed) return;
            _loadingConvos = true;
            Task.Run(() =>
            {
                var convos = new List<ConversationRow>();
                try
                {
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();
                    string sql = @"
                        SELECT c.ReservationID,
                               c.GuestName,
                               MAX(COALESCE(c.AssignedReceptionUsername,'')) AS AssignedReceptionUsername,
                               MAX(COALESCE(c.AssignedReceptionName,'')) AS AssignedReceptionName,
                               (SELECT Message FROM tbl_Chat x
                                WHERE x.ReservationID = c.ReservationID
                                ORDER BY x.SentAt DESC, x.ChatID DESC LIMIT 1) AS LastMsg,
                               MAX(c.SentAt) AS LastAt,
                               SUM(CASE WHEN c.SenderRole='guest' AND c.IsRead=0 THEN 1 ELSE 0 END) AS Unread
                        FROM tbl_Chat c
                        WHERE COALESCE(c.AssignedReceptionUsername,'') = ''
                           OR c.AssignedReceptionUsername = @meUser
                        GROUP BY c.ReservationID, c.GuestName
                        ORDER BY CASE WHEN MAX(COALESCE(c.AssignedReceptionUsername,'')) = @meUser THEN 0 ELSE 1 END,
                                 MAX(c.SentAt) DESC;";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@meUser", _myUsername);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        convos.Add(new ConversationRow(
                            reader["ReservationID"].ToString() ?? "",
                            reader["GuestName"].ToString() ?? "Guest",
                            reader["AssignedReceptionUsername"].ToString() ?? "",
                            reader["AssignedReceptionName"].ToString() ?? "",
                            reader["LastMsg"]?.ToString() ?? "",
                            Convert.ToDateTime(reader["LastAt"]),
                            Convert.ToInt32(reader["Unread"])));
                    }
                }
                catch (Exception ex)
                {
                    ProjectDiagnostics.LogError("UcReceptionChat", ex, "LoadConversations");
                }

                BeginInvokeSafe(() =>
                {
                    _loadingConvos = false;
                    RenderConversationList(convos);
                    if (string.IsNullOrEmpty(_selectedReservationId) && convos.Count > 0)
                        SelectConversation(convos[0].ReservationId, convos[0].GuestName);
                });
            });
        }

        void RenderConversationList(List<ConversationRow> convos)
        {
            _flowConvos.SuspendLayout();
            _flowConvos.Controls.Clear();

            int unread = 0;
            foreach (var c in convos)
            {
                unread += c.ReservationId == _selectedReservationId ? 0 : c.Unread;
            }
            _lblUnread.Text = unread > 9 ? "9+" : unread.ToString();
            _lblUnread.Visible = unread > 0;
            if (_lastUnreadTotal >= 0 && unread > _lastUnreadTotal)
            {
                var newestUnread = convos.Find(c => c.Unread > 0 && c.ReservationId != _selectedReservationId)
                    ?? convos.Find(c => c.Unread > 0);
                if (newestUnread != null)
                    ShowToast($"New message from {newestUnread.GuestName}");
            }
            _lastUnreadTotal = unread;

            if (convos.Count == 0)
            {
                _flowConvos.Controls.Add(new Label
                {
                    Text = "No guest messages yet.",
                    AutoSize = false,
                    Size = new Size(_pnlLeft.Width - 1, 90),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Muted,
                    BackColor = White
                });
            }

            foreach (var convo in convos)
                _flowConvos.Controls.Add(BuildConversationCard(convo));

            _flowConvos.ResumeLayout();
        }

        Panel BuildConversationCard(ConversationRow convo)
        {
            bool active = convo.ReservationId == _selectedReservationId;
            int visualUnread = active ? 0 : convo.Unread;
            bool assignedToMe = convo.AssignedReceptionUsername.Equals(_myUsername, StringComparison.OrdinalIgnoreCase);
            bool openQueue = string.IsNullOrWhiteSpace(convo.AssignedReceptionUsername);
            var card = new Panel
            {
                Size = new Size(_pnlLeft.Width - 1, 74),
                BackColor = active ? Color.FromArgb(230, 245, 237) : White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            card.Paint += (_, e) =>
            {
                if (convo.ReservationId == _selectedReservationId)
                {
                    using var accent = new SolidBrush(Gold);
                    e.Graphics.FillRectangle(accent, 0, 0, 4, card.Height);
                }
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, 0, card.Height - 1, card.Width, card.Height - 1);
            };

            var avatar = new Label
            {
                Text = Initials(convo.GuestName),
                Size = new Size(42, 42),
                Location = new Point(14, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Gold,
                BackColor = ForestMid
            };

            var name = new Label
            {
                Text = convo.GuestName,
                Location = new Point(66, 10),
                Size = new Size(162, 20),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TextDark,
                BackColor = Color.Transparent
            };
            var id = new Label
            {
                Text = convo.ReservationId,
                Location = new Point(66, 29),
                Size = new Size(168, 16),
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Muted,
                BackColor = Color.Transparent
            };
            var preview = new Label
            {
                Text = Shorten(convo.LastMessage, 28),
                Location = new Point(66, 48),
                Size = new Size(104, 16),
                Font = new Font("Segoe UI", 8f),
                ForeColor = visualUnread > 0 ? TextDark : Muted,
                BackColor = Color.Transparent
            };
            preview.AutoEllipsis = true;

            int assignmentWidth = assignedToMe ? 112 : (openQueue ? 132 : 96);
            var assignment = new Label
            {
                AutoSize = false,
                Size = new Size(assignmentWidth, 20),
                Location = new Point(card.Width - assignmentWidth - 14, 44),
                Font = new Font("Segoe UI", 6.7f, FontStyle.Bold),
                ForeColor = assignedToMe ? Color.FromArgb(13, 86, 44) : Color.FromArgb(32, 77, 125),
                BackColor = assignedToMe ? Color.FromArgb(219, 242, 227) : SoftBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = assignedToMe ? "ASSIGNED" : (openQueue ? "OPEN QUEUE" : "LOCKED")
            };
            assignment.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = Rounded(new Rectangle(0, 0, assignment.Width - 1, assignment.Height - 1), 10);
                using var fill = new SolidBrush(assignment.BackColor);
                using var pen = new Pen(Color.FromArgb(130, assignedToMe ? Color.FromArgb(62, 141, 89) : Color.FromArgb(77, 124, 181)));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
                TextRenderer.DrawText(e.Graphics, assignment.Text, assignment.Font, assignment.ClientRectangle, assignment.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };

            card.Controls.AddRange(new Control[] { avatar, name, id, preview, assignment });
            if (visualUnread > 0)
            {
                card.Controls.Add(new Label
                {
                    Text = visualUnread > 9 ? "9+" : visualUnread.ToString(),
                    Location = new Point(card.Width - 38, 10),
                    Size = new Size(20, 20),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = White,
                    BackColor = Color.FromArgb(210, 50, 50)
                });
            }

            void click(object? s, EventArgs e) => SelectConversation(convo.ReservationId, convo.GuestName);
            card.Click += click;
            foreach (Control child in card.Controls) child.Click += click;
            return card;
        }

        void SelectConversation(string reservationId, string guestName)
        {
            if (!TryClaimConversation(reservationId)) return;

            _selectedReservationId = reservationId;
            _selectedGuestName = guestName;
            _lastChatId = 0;

            _lblGuest.Text = $"{guestName}  |  {reservationId}";
            _pnlEmpty.Visible = false;
            _pnlHeader.Visible = true;
            _pnlInput.Visible = true;
            _flowMessages.Visible = true;
            _flowMessages.BringToFront();
            _pnlInput.BringToFront();
            _pnlHeader.BringToFront();
            _txtReply.Focus();

            MarkGuestMessagesRead();
            LoadMessages();
        }

        bool TryClaimConversation(string reservationId)
        {
            try
            {
                using var conn = new MySqlConnection(Conn);
                conn.Open();

                using (var check = new MySqlCommand(@"
                    SELECT AssignedReceptionUsername, AssignedReceptionName
                    FROM tbl_Chat
                    WHERE ReservationID = @rid
                      AND COALESCE(AssignedReceptionUsername,'') <> ''
                    ORDER BY ChatID DESC
                    LIMIT 1;", conn))
                {
                    check.Parameters.AddWithValue("@rid", reservationId);
                    using var reader = check.ExecuteReader();
                    if (reader.Read())
                    {
                        string assignedUser = reader["AssignedReceptionUsername"]?.ToString() ?? "";
                        string assignedName = reader["AssignedReceptionName"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(assignedUser) &&
                            !assignedUser.Equals(_myUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show(
                                $"This guest conversation is already assigned to {assignedName} (@{assignedUser}).",
                                "WildNest",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                            return false;
                        }
                    }
                }

                using var claim = new MySqlCommand(@"
                    UPDATE tbl_Chat
                    SET AssignedReceptionUsername = @meUser,
                        AssignedReceptionName = @meName
                    WHERE ReservationID = @rid
                      AND (COALESCE(AssignedReceptionUsername,'') = '' OR AssignedReceptionUsername = @meUser);", conn);
                claim.Parameters.AddWithValue("@rid", reservationId);
                claim.Parameters.AddWithValue("@meUser", _myUsername);
                claim.Parameters.AddWithValue("@meName", _myName);
                claim.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("UcReceptionChat", ex, "TryClaimConversation");
                MessageBox.Show("Unable to open this guest conversation right now.", "WildNest",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        void LoadMessages(bool incrementalOnly = false)
        {
            if (_loadingMessages || string.IsNullOrEmpty(_selectedReservationId) || IsDisposed) return;
            _loadingMessages = true;
            string rid = _selectedReservationId;
            int fromId = incrementalOnly ? _lastChatId : 0;

            Task.Run(() =>
            {
                var rows = new List<MessageRow>();
                try
                {
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();
                    string sql = "SELECT ChatID, SenderRole, Message, SentAt FROM tbl_Chat " +
                                 "WHERE ReservationID=@rid AND ChatID>@last ORDER BY SentAt ASC, ChatID ASC;";
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@last", fromId);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        rows.Add(new MessageRow(
                            Convert.ToInt32(reader["ChatID"]),
                            reader["SenderRole"].ToString() ?? "",
                            reader["Message"].ToString() ?? "",
                            Convert.ToDateTime(reader["SentAt"])));
                    }
                }
                catch (Exception ex)
                {
                    ProjectDiagnostics.LogError("UcReceptionChat", ex, "LoadMessages");
                }

                BeginInvokeSafe(() =>
                {
                    _loadingMessages = false;
                    if (rid != _selectedReservationId) return;

                    if (incrementalOnly)
                    {
                        foreach (var row in rows)
                        {
                            AddMessageBubble(row);
                            if (string.Equals(row.Role, "guest", StringComparison.OrdinalIgnoreCase))
                                ShowToast($"New message from {_selectedGuestName}");
                        }
                    }
                    else
                    {
                        RenderMessages(rows);
                    }

                    if (rows.Count > 0)
                    {
                        _lastChatId = Math.Max(_lastChatId, rows[^1].ChatId);
                        MarkGuestMessagesRead();
                    }
                });
            });
        }

        void RenderMessages(List<MessageRow> rows)
        {
            _flowMessages.SuspendLayout();
            _flowMessages.Controls.Clear();

            if (rows.Count == 0)
            {
                _flowMessages.Controls.Add(new Label
                {
                    Text = "No messages yet. When the guest sends a message, it will appear here.",
                    AutoSize = false,
                    Size = new Size(MessageRowWidth(), 100),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 10f),
                    ForeColor = Muted,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 80, 0, 0)
                });
            }
            else
            {
                foreach (var row in rows) AddMessageBubble(row);
            }

            _flowMessages.ResumeLayout();
            ScrollToBottom();
        }

        void AddMessageBubble(MessageRow row)
        {
            _flowMessages.Controls.Add(BuildBubble(row));
            ScrollToBottom();
        }

        Panel BuildBubble(MessageRow row)
        {
            bool mine = string.Equals(row.Role, "reception", StringComparison.OrdinalIgnoreCase);
            string message = row.Message ?? string.Empty;
            int rowW = MessageRowWidth();
            int maxBubbleW = Math.Min(620, Math.Max(260, (int)(rowW * 0.62)));

            using var measureFont = new Font("Segoe UI", 10f);
            Size measured = TextRenderer.MeasureText(message, measureFont,
                new Size(Math.Max(80, maxBubbleW - 28), int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);

            int bubbleW = Math.Min(maxBubbleW, Math.Max(180, measured.Width + 56));
            int bubbleH = Math.Max(54, measured.Height + 34);
            int bubbleX = mine ? rowW - bubbleW - 18 : 18;

            var wrap = new Panel
            {
                Size = new Size(rowW, bubbleH + 36),
                Margin = new Padding(0, 6, 0, 8),
                BackColor = Color.Transparent
            };

            var bubble = new Panel
            {
                Size = new Size(bubbleW, bubbleH),
                Location = new Point(Math.Max(18, bubbleX), 8),
                BackColor = Color.Transparent
            };
            bubble.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, bubble.Width - 1, bubble.Height - 1);
                using var path = Rounded(rect, 14);
                using var bg = new SolidBrush(mine ? Forest : GuestBubble);
                e.Graphics.FillPath(bg, path);
                if (!mine)
                {
                    using var pen = new Pen(Color.FromArgb(210, 226, 218));
                    e.Graphics.DrawPath(pen, path);
                }
            };

            bubble.Controls.Add(new Label
            {
                Text = message,
                AutoSize = false,
                Size = new Size(Math.Max(60, bubble.Width - 36), Math.Max(24, bubble.Height - 24)),
                Location = new Point(18, 12),
                Font = new Font("Segoe UI", 10f),
                ForeColor = mine ? Cream : TextDark,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = true
            });

            var time = new Label
            {
                Text = row.SentAt.ToString("h:mm tt"),
                AutoSize = true,
                Font = new Font("Segoe UI", 7f),
                ForeColor = Muted,
                BackColor = Color.Transparent,
                Location = new Point(bubble.Left, bubble.Bottom + 5)
            };

            wrap.Controls.Add(bubble);
            wrap.Controls.Add(time);
            return wrap;
        }

        int MessageRowWidth()
        {
            int w = _flowMessages.ClientSize.Width;
            if (w <= 0) w = _pnlShell.ClientSize.Width;
            w -= _flowMessages.Padding.Horizontal + SystemInformation.VerticalScrollBarWidth + 8;
            return Math.Max(360, w);
        }

        void SendMessage()
        {
            string msg = _txtReply.Text.Trim();
            if (string.IsNullOrEmpty(msg) || string.IsNullOrEmpty(_selectedReservationId)) return;
            _txtReply.Clear();

            string rid = _selectedReservationId;
            string guest = _selectedGuestName;
            Task.Run(() =>
            {
                try
                {
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();
                    using var cmd = new MySqlCommand(
                        "INSERT INTO tbl_Chat (ReservationID, GuestName, SenderRole, Message, SentAt, IsRead, AssignedReceptionUsername, AssignedReceptionName) " +
                        "VALUES (@rid, @guest, 'reception', @msg, NOW(), 1, @meUser, @meName);", conn);
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@guest", guest);
                    cmd.Parameters.AddWithValue("@msg", msg);
                    cmd.Parameters.AddWithValue("@meUser", _myUsername);
                    cmd.Parameters.AddWithValue("@meName", _myName);
                    cmd.ExecuteNonQuery();
                    int id = Convert.ToInt32(new MySqlCommand("SELECT LAST_INSERT_ID();", conn).ExecuteScalar());

                    BeginInvokeSafe(() =>
                    {
                        if (rid != _selectedReservationId) return;
                        _lastChatId = Math.Max(_lastChatId, id);
                        AddMessageBubble(new MessageRow(id, "reception", msg, DateTime.Now));
                    });
                }
                catch (Exception ex)
                {
                    BeginInvokeSafe(() => MessageBox.Show("Send failed: " + ex.Message,
                        "Guest Chat", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
            });
        }

        void MarkGuestMessagesRead()
        {
            string rid = _selectedReservationId;
            if (string.IsNullOrEmpty(rid)) return;
            Task.Run(() =>
            {
                try
                {
                    using var conn = new MySqlConnection(Conn);
                    conn.Open();
                    using var cmd = new MySqlCommand(
                        "UPDATE tbl_Chat SET IsRead=1, AssignedReceptionUsername=@meUser, AssignedReceptionName=@meName " +
                        "WHERE ReservationID=@rid AND SenderRole='guest' AND (COALESCE(AssignedReceptionUsername,'') = '' OR AssignedReceptionUsername=@meUser);", conn);
                    cmd.Parameters.AddWithValue("@rid", rid);
                    cmd.Parameters.AddWithValue("@meUser", _myUsername);
                    cmd.Parameters.AddWithValue("@meName", _myName);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    ProjectDiagnostics.LogError("UcReceptionChat", ex, "MarkGuestMessagesRead");
                }
            });
        }

        void ScrollToBottom()
        {
            if (_flowMessages.Controls.Count == 0) return;
            try { _flowMessages.HorizontalScroll.Value = 0; } catch { }
            _flowMessages.ScrollControlIntoView(_flowMessages.Controls[^1]);
            try { _flowMessages.HorizontalScroll.Value = 0; } catch { }
        }

        void BeginInvokeSafe(Action action)
        {
            if (IsDisposed) return;
            try
            {
                if (InvokeRequired) BeginInvoke(action);
                else action();
            }
            catch { }
        }

        static string Initials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return (parts[0][0].ToString() + parts[1][0]).ToUpper();
            return name.Length >= 2 ? name[..2].ToUpper() : "G";
        }

        static string Shorten(string value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return "No messages yet";
            return value.Length <= max ? value : value[..max] + "...";
        }

        static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _toastTimer?.Stop();
                _toastTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        record ConversationRow(string ReservationId, string GuestName, string AssignedReceptionUsername, string AssignedReceptionName, string LastMessage, DateTime LastAt, int Unread);
        record MessageRow(int ChatId, string Role, string Message, DateTime SentAt);
    }
}
