using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerSalesReports : UserControl
    {
        private const string PendingPaymentStatuses = "'Pending','Unpaid','Pending Verification','Pay on Arrival','Confirmed'";

        public UcManagerSalesReports()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbOk = StaffPortalDb.CanConnect(out string dbMsg);

            decimal todayRev = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            decimal weekRev = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEARWEEK(PaidAt,1)=YEARWEEK(CURDATE(),1) AND Status IN ('Paid','Completed','Settled');");
            decimal monthRev = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND MONTH(PaidAt)=MONTH(CURDATE()) AND Status IN ('Paid','Completed','Settled');");
            decimal yearRev = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND Status IN ('Paid','Completed','Settled');");

            int todayTxn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            int weekTxn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE YEARWEEK(PaidAt,1)=YEARWEEK(CURDATE(),1) AND Status IN ('Paid','Completed','Settled');");
            int monthTxn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND MONTH(PaidAt)=MONTH(CURDATE()) AND Status IN ('Paid','Completed','Settled');");
            int yearTxn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND Status IN ('Paid','Completed','Settled');");
            decimal lifetimeBooked = StaffPortalDb.Sum("SELECT COALESCE(SUM(TotalAmount),0) FROM tbl_reservations WHERE COALESCE(Status,'') <> 'Cancelled';");
            decimal lifetimeCollected = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            decimal pendingPipeline = StaffPortalDb.Sum(@"
SELECT COALESCE(SUM(
    CASE
        WHEN pay.ReservationID IS NULL THEN COALESCE(r.TotalAmount,0)
        WHEN pay.PendingCount > 0 THEN COALESCE(pay.PendingAmount,0)
        ELSE 0
    END
),0)
FROM tbl_reservations r
LEFT JOIN
(
    SELECT ReservationID,
           COALESCE(SUM(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN Amount ELSE 0 END),0) AS PendingAmount,
           SUM(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN 1 ELSE 0 END) AS PendingCount
    FROM tbl_payments
    GROUP BY ReservationID
) pay ON pay.ReservationID = r.ReservationID
WHERE COALESCE(r.Status,'') <> 'Cancelled'
  AND (pay.ReservationID IS NULL OR pay.PendingCount > 0);");
            int pendingTxn = StaffPortalDb.Count(@"
SELECT COUNT(*)
FROM tbl_reservations r
LEFT JOIN
(
    SELECT ReservationID,
           SUM(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN 1 ELSE 0 END) AS PendingCount
    FROM tbl_payments
    GROUP BY ReservationID
) pay ON pay.ReservationID = r.ReservationID
WHERE COALESCE(r.Status,'') <> 'Cancelled'
  AND (pay.ReservationID IS NULL OR pay.PendingCount > 0);");
            decimal averageSettled = StaffPortalDb.Sum("SELECT COALESCE(AVG(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            decimal collectionRate = lifetimeBooked <= 0m ? 0m : Math.Round((lifetimeCollected / lifetimeBooked) * 100m, 1);

            var dailyTable = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(p.PaidAt,'%Y-%m-%d') AS `Date`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(p.Amount),0) AS `Revenue (PHP)`,
       COALESCE(AVG(p.Amount),0) AS `Avg Transaction`
FROM tbl_payments p
WHERE p.Status IN ('Paid','Completed','Settled')
  AND p.PaidAt >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
GROUP BY DATE_FORMAT(p.PaidAt,'%Y-%m-%d')
ORDER BY `Date` DESC;");

            var weeklyTable = StaffPortalDb.GetTable(@"
SELECT CONCAT(YEAR(PaidAt),'-W',LPAD(WEEK(PaidAt,1),2,'0')) AS `Week`,
       MIN(DATE_FORMAT(PaidAt,'%Y-%m-%d')) AS `Week Start`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(Amount),0) AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0) AS `Avg Transaction`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
  AND PaidAt >= DATE_SUB(CURDATE(), INTERVAL 13 WEEK)
GROUP BY CONCAT(YEAR(PaidAt),'-W',LPAD(WEEK(PaidAt,1),2,'0'))
ORDER BY `Week` DESC;");

            var monthlyTable = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(PaidAt,'%Y-%m') AS `Month`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(Amount),0) AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0) AS `Avg Transaction`,
       MAX(Amount) AS `Largest Payment`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
  AND PaidAt >= DATE_SUB(CURDATE(), INTERVAL 12 MONTH)
GROUP BY DATE_FORMAT(PaidAt,'%Y-%m')
ORDER BY `Month` DESC;");

            var yearlyTable = StaffPortalDb.GetTable(@"
SELECT YEAR(PaidAt) AS `Year`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(Amount),0) AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0) AS `Avg Transaction`,
       MAX(Amount) AS `Largest Payment`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
GROUP BY YEAR(PaidAt)
ORDER BY `Year` DESC;");

            var byBookingType = StaffPortalDb.GetTable(@"
SELECT booking_rollup.`Booking Type`,
       COUNT(*) AS `Reservations`,
       COALESCE(SUM(booking_rollup.`Booking Value (PHP)`),0) AS `Booking Value (PHP)`,
       COALESCE(SUM(booking_rollup.`Collected (PHP)`),0) AS `Collected (PHP)`
FROM
(
    SELECT r.ReservationID,
           COALESCE(r.BookingType,'Unspecified') AS `Booking Type`,
           COALESCE(r.TotalAmount,0) AS `Booking Value (PHP)`,
           COALESCE((
               SELECT SUM(p.Amount)
               FROM tbl_payments p
               WHERE p.ReservationID = r.ReservationID
                 AND p.Status IN ('Paid','Completed','Settled')
           ),0) AS `Collected (PHP)`
    FROM tbl_reservations r
) booking_rollup
GROUP BY booking_rollup.`Booking Type`
ORDER BY `Collected (PHP)` DESC;");

            var byPaymentMethod = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(PaymentMethod),''),'Unspecified') AS `Method`,
       COUNT(*) AS `Transactions`,
       COALESCE(SUM(Amount),0) AS `Revenue (PHP)`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
GROUP BY COALESCE(NULLIF(TRIM(PaymentMethod),''),'Unspecified')
ORDER BY `Revenue (PHP)` DESC;");

            var topGuests = StaffPortalDb.GetTable(@"
SELECT CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COUNT(DISTINCT r.ReservationID) AS `Reservations`,
       COALESCE(SUM(p.Amount),0) AS `Collected (PHP)`,
       COALESCE(MAX(p.Amount),0) AS `Largest Payment (PHP)`
FROM tbl_payments p
LEFT JOIN tbl_reservations r ON r.ReservationID = p.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
WHERE p.Status IN ('Paid','Completed','Settled')
GROUP BY g.GuestID, CONCAT(g.FirstName, ' ', g.LastName)
ORDER BY `Collected (PHP)` DESC, `Reservations` DESC
LIMIT 8;");

            var paymentFollowUp = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(r.BookingType,'Unspecified') AS `Booking Type`,
       COALESCE(pay.PaymentStatus,'No Payment Record') AS `Payment Status`,
       COALESCE(pay.TrackedAmount,0) AS `Tracked Amount (PHP)`,
       COALESCE(r.TotalAmount,0) AS `Reservation Value (PHP)`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN
(
    SELECT ReservationID,
           MAX(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN Status ELSE NULL END) AS PaymentStatus,
           COALESCE(SUM(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN Amount ELSE 0 END),0) AS TrackedAmount,
           SUM(CASE WHEN Status IN (" + PendingPaymentStatuses + @") THEN 1 ELSE 0 END) AS PendingCount
    FROM tbl_payments
    GROUP BY ReservationID
) pay ON pay.ReservationID = r.ReservationID
WHERE pay.ReservationID IS NULL
   OR pay.PendingCount > 0
ORDER BY r.CreatedAt DESC
LIMIT 12;");

            var recentHighValue = StaffPortalDb.GetTable(@"
SELECT p.PaymentID AS `Payment ID`,
       p.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       p.PaymentMethod AS `Method`,
       p.Amount AS `Amount (PHP)`,
       DATE_FORMAT(p.PaidAt,'%Y-%m-%d %H:%i') AS `Paid At`
FROM tbl_payments p
LEFT JOIN tbl_reservations r ON r.ReservationID = p.ReservationID
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
WHERE p.Status IN ('Paid','Completed','Settled')
ORDER BY p.Amount DESC, p.PaidAt DESC
LIMIT 10;");

            var exportMetrics = new (string Label, string Value)[]
            {
                ("Today's Revenue", StaffPortalUi.Peso(todayRev)),
                ("Today's Transactions", todayTxn.ToString()),
                ("This Week's Revenue", StaffPortalUi.Peso(weekRev)),
                ("This Week's Transactions", weekTxn.ToString()),
                ("This Month's Revenue", StaffPortalUi.Peso(monthRev)),
                ("This Month's Transactions", monthTxn.ToString()),
                ("This Year's Revenue", StaffPortalUi.Peso(yearRev)),
                ("This Year's Transactions", yearTxn.ToString()),
                ("Lifetime Booking Value", StaffPortalUi.Peso(lifetimeBooked)),
                ("Lifetime Collected", StaffPortalUi.Peso(lifetimeCollected)),
                ("Collection Rate", collectionRate.ToString("N1") + "%"),
                ("Average Settled Ticket", StaffPortalUi.Peso(averageSettled)),
                ("Pending Pipeline", StaffPortalUi.Peso(pendingPipeline)),
                ("Pending Transactions", pendingTxn.ToString()),
                ("Report Generated", DateTime.Now.ToString("MMMM d, yyyy h:mm tt"))
            };

            var exportSections = new List<(string Title, DataTable Table)>
            {
                ("Revenue by Booking Type", byBookingType),
                ("Revenue by Payment Method", byPaymentMethod),
                ("Top Paying Guests", topGuests),
                ("Payment Follow-up Queue", paymentFollowUp),
                ("Recent High-Value Collections", recentHighValue),
                ("Daily Revenue Detail", dailyTable),
                ("Weekly Revenue Detail", weeklyTable),
                ("Monthly Revenue Detail", monthlyTable),
                ("Yearly Revenue Detail", yearlyTable)
            };

            var sections = new List<Control>
            {
                StaffPortalUi.AlertBanner(
                    dbOk
                        ? "Executive sales intelligence is live. Revenue movement, collection health, and payment mix are updating from the production tables."
                        : dbMsg,
                    !dbOk),
                BuildRevenueHero(todayRev, weekRev, monthRev, collectionRate, pendingPipeline, pendingTxn),
                StaffPortalUi.MessageCard(
                    "Executive Revenue View",
                    "This manager sales console is designed for fast decision-making: period summaries, collection effectiveness, booking mix, payment behavior, high-value guests, and follow-up risk are all visible from one screen.",
                    alert: false),
                StaffPortalUi.StatsRow(
                    (StaffPortalUi.Peso(todayRev), $"Today ({todayTxn} txn)", WildNestUI.Green),
                    (StaffPortalUi.Peso(weekRev), $"This Week ({weekTxn} txn)", WildNestUI.Amber),
                    (StaffPortalUi.Peso(monthRev), $"This Month ({monthTxn} txn)", WildNestUI.Blue),
                    (StaffPortalUi.Peso(yearRev), $"This Year ({yearTxn} txn)", WildNestUI.Green)),
                StaffPortalUi.MetricTableCard(
                    "Revenue Snapshot",
                    exportMetrics),
                BuildReportExportCard(exportMetrics, exportSections),
                BuildPeriodTabPanel(dailyTable, weeklyTable, monthlyTable, yearlyTable),
                StaffPortalUi.GridCard("Revenue by Booking Type", byBookingType, "No booking type data available."),
                StaffPortalUi.GridCard("Revenue by Payment Method", byPaymentMethod, "No payment method data available."),
                StaffPortalUi.GridCard("Top Paying Guests", topGuests, "No guest payment data available."),
                StaffPortalUi.GridCard("Payment Follow-up Queue", paymentFollowUp, "No pending payment follow-up items."),
                StaffPortalUi.GridCard("Recent High-Value Collections", recentHighValue, "No settled payment records found.")
            };

            var page = StaffPortalUi.BuildPage(
                "Executive Sales Intelligence",
                $"Manager-grade revenue analysis across daily, weekly, monthly, and yearly performance as of {DateTime.Now:MMMM d, yyyy h:mm tt}.",
                sections);

            Controls.Add(page);
        }

        private Panel BuildRevenueHero(decimal todayRevenue, decimal weekRevenue, decimal monthRevenue, decimal collectionRate, decimal pendingPipeline, int pendingTransactions)
        {
            var hero = new Panel
            {
                Height = 246,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };

            hero.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                WildNestUI.PaintSoftShadow(e.Graphics, new Rectangle(6, 10, hero.Width - 18, hero.Height - 18), 16, 4);
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, hero.Width - 1, hero.Height - 1), 20);
                using var fill = new System.Drawing.Drawing2D.LinearGradientBrush(hero.ClientRectangle, WildNestUI.Forest, WildNestUI.ForestL, 0f);
                using var border = new Pen(Color.FromArgb(70, WildNestUI.Gold), 1f);
                using var accent = new SolidBrush(Color.FromArgb(24, WildNestUI.Gold));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                e.Graphics.FillEllipse(accent, -36, -18, 126, 126);
                e.Graphics.FillEllipse(accent, hero.Width - 118, hero.Height - 86, 152, 152);
            };

            var title = new Label
            {
                Text = "Revenue Control Tower",
                Font = WildNestUI.FontTitle(18f),
                ForeColor = WildNestUI.Cream,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(26, 22)
            };
            hero.Controls.Add(title);

            var subtitle = new Label
            {
                Text = "A quick executive read on collection momentum, month health, and booking-payment pressure before you move into detailed reports.",
                Font = WildNestUI.FontBody(9.8f),
                ForeColor = Color.FromArgb(222, WildNestUI.Cream),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(790, 46),
                Location = new Point(28, 56)
            };
            hero.Controls.Add(subtitle);

            var spotlight = BuildMiniMetric(
                "Live Collection Pace",
                $"{StaffPortalUi.Peso(todayRevenue)} today\n{StaffPortalUi.Peso(weekRevenue)} this week",
                WildNestUI.Gold);
            spotlight.Location = new Point(28, 148);
            hero.Controls.Add(spotlight);

            var balance = BuildMiniMetric(
                "Month Health",
                $"{StaffPortalUi.Peso(monthRevenue)} collected\n{collectionRate:N1}% collection rate",
                WildNestUI.Blue);
            balance.Location = new Point(330, 148);
            hero.Controls.Add(balance);

            var risk = BuildMiniMetric(
                "Follow-up Pressure",
                $"{StaffPortalUi.Peso(pendingPipeline)} pending\n{pendingTransactions} transactions requiring attention",
                WildNestUI.Red);
            risk.Location = new Point(632, 148);
            hero.Controls.Add(risk);

            return hero;
        }

        private static Panel BuildMiniMetric(string title, string body, Color accent)
        {
            var panel = new Panel
            {
                Size = new Size(272, 82),
                BackColor = Color.FromArgb(20, 255, 255, 255)
            };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 16);
                using var fill = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                using var border = new Pen(Color.FromArgb(56, accent), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            panel.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(),
                Font = WildNestUI.FontLabel(8.1f),
                ForeColor = accent,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 10)
            });

            panel.Controls.Add(new Label
            {
                Text = body,
                Font = WildNestUI.FontBody(9.2f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(238, 50),
                Location = new Point(16, 26)
            });

            return panel;
        }

        private Panel BuildPeriodTabPanel(
            System.Data.DataTable daily,
            System.Data.DataTable weekly,
            System.Data.DataTable monthly,
            System.Data.DataTable yearly)
        {
            var card = WildNestUI.CardWithHeader(1100, 480, "Period Sales Detail", 42);

            var tabBar = new FlowLayoutPanel
            {
                Location = new Point(14, 52),
                Size = new Size(card.Width - 28, 42),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };
            card.Controls.Add(tabBar);

            var gridHost = new Panel
            {
                Location = new Point(14, 104),
                Size = new Size(card.Width - 28, card.Height - 118),
                BackColor = Color.White
            };
            card.Controls.Add(gridHost);

            card.Resize += (_, _) =>
            {
                tabBar.Width = card.Width - 28;
                gridHost.Size = new Size(card.Width - 28, card.Height - 118);
            };

            Button? activeTab = null;
            void Activate(System.Data.DataTable table, Button btn)
            {
                if (activeTab != null)
                {
                    activeTab.BackColor = Color.FromArgb(230, 227, 222);
                    activeTab.ForeColor = WildNestUI.Muted;
                }

                activeTab = btn;
                btn.BackColor = WildNestUI.Forest;
                btn.ForeColor = WildNestUI.Gold;

                gridHost.Controls.Clear();
                var grid = StaffPortalUi.CreateGrid(table);
                grid.Dock = DockStyle.Fill;
                gridHost.Controls.Add(grid);
            }

            var periods = new (string Label, System.Data.DataTable Table)[]
            {
                ("Daily (30 d)", daily),
                ("Weekly (13 wk)", weekly),
                ("Monthly (12 mo)", monthly),
                ("Yearly", yearly)
            };

            foreach (var (label, table) in periods)
            {
                var captured = table;
                var btn = new Button
                {
                    Text = label,
                    Size = new Size(190, 36),
                    Margin = new Padding(0, 0, 6, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(230, 227, 222),
                    ForeColor = WildNestUI.Muted,
                    Font = WildNestUI.FontLabel(8.5f),
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                tabBar.Controls.Add(btn);
                btn.Click += (_, _) => Activate(captured, btn);

                if (label.StartsWith("Monthly"))
                    btn.PerformClick();
            }

            return card;
        }

        private Panel BuildReportExportCard(
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            var card = WildNestUI.CardWithHeader(1100, 172, "Formal Report Export", 42);

            var lead = new Label
            {
                Text = "Generate a manager-ready revenue report for checking, printing, and submission. Export to PDF for presentation or Word-compatible format for editing.",
                Font = WildNestUI.FontBody(10f),
                ForeColor = WildNestUI.TextDark,
                Location = new Point(18, 62),
                Size = new Size(720, 42),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lead);

            var note = new Label
            {
                Text = "The export uses live sales intelligence from bookings, payments, and follow-up tables at the moment you click it.",
                Font = WildNestUI.FontBody(8.9f),
                ForeColor = WildNestUI.Muted,
                Location = new Point(18, 108),
                Size = new Size(740, 20),
                BackColor = Color.Transparent
            };
            card.Controls.Add(note);

            var pdfBtn = WildNestUI.BtnPrimary("Export PDF Report", 168, 38);
            pdfBtn.Location = new Point(card.Width - 370, 78);
            pdfBtn.Click += async (_, _) =>
            {
                await StaffPortalUi.ExportExecutiveReportPdfAsync(
                    "WildNest Executive Sales Intelligence",
                    $"Manager-grade revenue analysis generated on {DateTime.Now:MMMM d, yyyy h:mm tt}.",
                    metrics,
                    sections);
            };
            card.Controls.Add(pdfBtn);

            var wordBtn = new Button
            {
                Text = "Export Word Report",
                Size = new Size(168, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(238, 232, 222),
                ForeColor = WildNestUI.TextDark,
                Font = WildNestUI.FontLabel(8.5f),
                Cursor = Cursors.Hand
            };
            wordBtn.FlatAppearance.BorderColor = Color.FromArgb(214, 204, 189);
            wordBtn.FlatAppearance.BorderSize = 1;
            wordBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 223, 211);
            wordBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(221, 214, 201);
            wordBtn.Location = new Point(card.Width - 186, 78);
            wordBtn.Click += (_, _) =>
            {
                StaffPortalUi.ExportExecutiveReportRtf(
                    "WildNest Executive Sales Intelligence",
                    $"Manager-grade revenue analysis generated on {DateTime.Now:MMMM d, yyyy h:mm tt}.",
                    metrics,
                    sections);
            };
            card.Controls.Add(wordBtn);

            card.Resize += (_, _) =>
            {
                lead.Size = new Size(Math.Max(460, card.Width - 392), 42);
                note.Size = new Size(Math.Max(460, card.Width - 392), 20);
                pdfBtn.Location = new Point(card.Width - 370, 78);
                wordBtn.Location = new Point(card.Width - 186, 78);
            };

            return card;
        }
    }
}
