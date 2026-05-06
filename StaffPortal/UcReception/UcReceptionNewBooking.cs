using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcReception
{
    public partial class UcReceptionNewBooking : UserControl
    {
        public UcReceptionNewBooking()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            Load += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            int todayBookings = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE DATE(CreatedAt) = CURDATE();");
            int guestProfiles = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests;");
            int openCabins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int experiences = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;");

            var recentBookings = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       r.BookingType AS `Booking Type`,
       r.Status,
       r.TotalAmount AS `Total Amount`,
       DATE_FORMAT(r.CreatedAt, '%Y-%m-%d %H:%i') AS `Created`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
ORDER BY r.CreatedAt DESC
LIMIT 10;");

            var launchCard = StaffPortalUi.ActionCard("Front Desk Booking Actions", panel =>
            {
                var intro = new Label
                {
                    Text = "Reception launches the same live booking flow used by guests, so walk-ins and assisted reservations still save to the real reservation, guest, payment, and experience records.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    Location = new Point(18, 60),
                    Size = new Size(panel.Width - 36, 42)
                };
                var helper = new Label
                {
                    Text = "Use this for walk-in guests or guided front-desk bookings. The booking workspace opens in a cleaner reception shell, then returns you safely to Reception when closed.",
                    Font = WildNestUI.FontBody(9f),
                    ForeColor = WildNestUI.Muted,
                    BackColor = Color.Transparent,
                    Location = new Point(18, 94),
                    Size = new Size(panel.Width - 36, 36)
                };

                var btnOpen = WildNestUI.BtnPrimary("Open Walk-In Booking Workspace", 238, 36);
                btnOpen.Location = new Point(18, 132);
                btnOpen.Click += (s, e) => OpenBookingWorkspace();

                panel.Controls.Add(intro);
                panel.Controls.Add(helper);
                panel.Controls.Add(btnOpen);
            }, 190);

            var page = StaffPortalUi.BuildPage(
                "New Booking",
                "Reception handoff into the existing Book Now logic used across cabins, day visit, experiences, and full stay + experience.",
                new List<Control>
                {
                    StaffPortalUi.MessageCard(
                        "What To Input Here",
                        "There is no separate manual reservation form here. Reception launches the same live Book Now experience used by guests, then completes the booking for walk-ins or assisted visitors in one premium flow."),
                    StaffPortalUi.StatsRow(
                        (todayBookings.ToString(), "Bookings Created Today", WildNestUI.Green),
                        (guestProfiles.ToString(), "Guest Profiles", WildNestUI.Blue),
                        (openCabins.ToString(), "Available Cabins", WildNestUI.Amber),
                        (experiences.ToString(), "Experience Packages", WildNestUI.Green)),
                    launchCard,
                    StaffPortalUi.GridCard("Recently Created Bookings", recentBookings)
                });

            Controls.Add(page);
        }

        private void OpenBookingWorkspace()
        {
            var owner = FindForm();
            Control anchor = owner != null ? owner : this;
            var screen = Screen.FromControl(anchor);

            using var host = new Form
            {
                Text = "WildNest Reception Booking",
                StartPosition = FormStartPosition.Manual,
                Bounds = screen.WorkingArea,
                WindowState = FormWindowState.Maximized,
                BackColor = WildNestUI.Sand,
                MinimizeBox = false,
                Padding = new Padding(0),
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                TopMost = true,
                KeyPreview = true
            };
            host.Shown += (s, e) =>
            {
                host.Location = screen.WorkingArea.Location;
                host.Size = screen.WorkingArea.Size;
                host.WindowState = FormWindowState.Maximized;
                host.BringToFront();
                host.Activate();
            };

            host.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    host.Close();
            };

            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WildNestUI.Sand,
                Padding = new Padding(12)
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = WildNestUI.Forest
            };

            var title = new Label
            {
                AutoSize = true,
                Text = "Walk-In Booking Workspace",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = WildNestUI.FontTitle(17f),
                ForeColor = WildNestUI.Gold,
                Location = new Point(22, 10),
                BackColor = Color.Transparent
            };

            var subtitle = new Label
            {
                AutoSize = true,
                Text = "Use the same guest booking flow for walk-ins, then return cleanly to Reception when finished.",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = WildNestUI.FontBody(8.75f),
                ForeColor = Color.FromArgb(210, 248, 244, 239),
                Location = new Point(23, 38),
                BackColor = Color.Transparent
            };

            var btnClose = new Button
            {
                Text = "Close Booking Window",
                Size = new Size(178, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(18, 41, 30),
                ForeColor = WildNestUI.Cream,
                Cursor = Cursors.Hand,
                Font = WildNestUI.FontBold(8.75f)
            };
            btnClose.FlatAppearance.BorderSize = 1;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(96, 212, 160, 23);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 70, 51);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 48, 35);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(header.Width - btnClose.Width - 22, 18);
            btnClose.Click += (s, e) => host.Close();
            header.Resize += (s, e) =>
            {
                btnClose.Location = new Point(header.ClientSize.Width - btnClose.Width - 22, 14);
            };

            var topAccent = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 2,
                BackColor = Color.FromArgb(180, 212, 160, 23)
            };

            var contentFrame = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 12, 0, 0),
                BackColor = WildNestUI.Sand
            };

            var contentCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };
            contentCard.Paint += (s, e) =>
            {
                var panel = (Panel)s!;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                WildNestUI.PaintSoftShadow(e.Graphics, new Rectangle(4, 6, panel.Width - 10, panel.Height - 12), 12, 3);
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 14);
                using var fill = new SolidBrush(Color.White);
                using var border = new Pen(Color.FromArgb(224, 220, 214), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            header.Controls.Add(btnClose);
            header.Controls.Add(subtitle);
            header.Controls.Add(title);
            header.Controls.Add(topAccent);

            var bookingHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                BackColor = Color.White
            };

            var booking = new global::Project.BookNow { Dock = DockStyle.Fill };
            bookingHost.Controls.Add(booking);
            contentCard.Controls.Add(bookingHost);
            contentFrame.Controls.Add(contentCard);

            host.FormClosed += (s, e) =>
            {
                bookingHost.Controls.Clear();
                booking.Dispose();
            };

            shell.Controls.Add(contentFrame);
            shell.Controls.Add(header);
            host.Controls.Add(shell);
            host.ShowDialog(owner);
        }
    }
}
