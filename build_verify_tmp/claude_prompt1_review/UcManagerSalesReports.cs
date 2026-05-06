// ============================================================
//  UcManagerSalesReports.cs   (namespace Project.UcManager)
//  NEW FILE — no equivalent existed in the Administrator portal
//
//  Provides:
//  • KPI row: Today / This Week / This Month / This Year revenue
//  • Revenue Snapshot metric table
//  • Period tab strip: Daily (30 d), Weekly (13 wk), Monthly (12 mo), Yearly
//  • Breakdown by Booking Type
//  • Breakdown by Payment Method
//
//  No schema changes required — all data comes from
//  tbl_payments and tbl_reservations which already exist.
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Project.UcManager
{
    public class UcManagerSalesReports : UserControl
    {
        public UcManagerSalesReports()
        {
            Dock      = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load     += (s, e) => Render();
        }

        private void Render()
        {
            Controls.Clear();

            bool dbOk = StaffPortalDb.CanConnect(out string dbMsg);

            // ── Revenue KPI scalars ────────────────────────────────────

            decimal todayRev  = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            decimal weekRev   = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEARWEEK(PaidAt,1)=YEARWEEK(CURDATE(),1) AND Status IN ('Paid','Completed','Settled');");
            decimal monthRev  = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND MONTH(PaidAt)=MONTH(CURDATE()) AND Status IN ('Paid','Completed','Settled');");
            decimal yearRev   = StaffPortalDb.Sum(
                "SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND Status IN ('Paid','Completed','Settled');");

            int todayTxn  = StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            int weekTxn   = StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_payments WHERE YEARWEEK(PaidAt,1)=YEARWEEK(CURDATE(),1) AND Status IN ('Paid','Completed','Settled');");
            int monthTxn  = StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND MONTH(PaidAt)=MONTH(CURDATE()) AND Status IN ('Paid','Completed','Settled');");
            int yearTxn   = StaffPortalDb.Count(
                "SELECT COUNT(*) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND Status IN ('Paid','Completed','Settled');");

            // ── Period detail tables ───────────────────────────────────

            var dailyTable = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(p.PaidAt,'%Y-%m-%d')   AS `Date`,
       COUNT(*)                            AS `Transactions`,
       COALESCE(SUM(p.Amount),0)           AS `Revenue (PHP)`,
       COALESCE(AVG(p.Amount),0)           AS `Avg Transaction`
FROM tbl_payments p
WHERE p.Status IN ('Paid','Completed','Settled')
  AND p.PaidAt >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)
GROUP BY DATE_FORMAT(p.PaidAt,'%Y-%m-%d')
ORDER BY `Date` DESC;");

            var weeklyTable = StaffPortalDb.GetTable(@"
SELECT CONCAT(YEAR(PaidAt),'-W',LPAD(WEEK(PaidAt,1),2,'0'))  AS `Week`,
       MIN(DATE_FORMAT(PaidAt,'%Y-%m-%d'))                    AS `Week Start`,
       COUNT(*)                                               AS `Transactions`,
       COALESCE(SUM(Amount),0)                                AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0)                                AS `Avg Transaction`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
  AND PaidAt >= DATE_SUB(CURDATE(), INTERVAL 13 WEEK)
GROUP BY CONCAT(YEAR(PaidAt),'-W',LPAD(WEEK(PaidAt,1),2,'0'))
ORDER BY `Week` DESC;");

            var monthlyTable = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(PaidAt,'%Y-%m')  AS `Month`,
       COUNT(*)                     AS `Transactions`,
       COALESCE(SUM(Amount),0)      AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0)      AS `Avg Transaction`,
       MAX(Amount)                  AS `Largest Payment`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
  AND PaidAt >= DATE_SUB(CURDATE(), INTERVAL 12 MONTH)
GROUP BY DATE_FORMAT(PaidAt,'%Y-%m')
ORDER BY `Month` DESC;");

            var yearlyTable = StaffPortalDb.GetTable(@"
SELECT YEAR(PaidAt)             AS `Year`,
       COUNT(*)                  AS `Transactions`,
       COALESCE(SUM(Amount),0)   AS `Revenue (PHP)`,
       COALESCE(AVG(Amount),0)   AS `Avg Transaction`,
       MAX(Amount)               AS `Largest Payment`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
GROUP BY YEAR(PaidAt)
ORDER BY `Year` DESC;");

            // ── Cross-cutting breakdowns ───────────────────────────────

            var byBookingType = StaffPortalDb.GetTable(@"
SELECT r.BookingType                           AS `Booking Type`,
       COUNT(*)                                AS `Reservations`,
       COALESCE(SUM(r.TotalAmount),0)          AS `Booking Value (PHP)`,
       COALESCE(SUM(p.Amount),0)               AS `Collected (PHP)`
FROM tbl_reservations r
LEFT JOIN tbl_payments p
       ON p.ReservationID = r.ReservationID
      AND p.Status IN ('Paid','Completed','Settled')
GROUP BY r.BookingType
ORDER BY `Collected (PHP)` DESC;");

            var byPaymentMethod = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(PaymentMethod),''),'Unspecified') AS `Method`,
       COUNT(*)                                               AS `Transactions`,
       COALESCE(SUM(Amount),0)                               AS `Revenue (PHP)`
FROM tbl_payments
WHERE Status IN ('Paid','Completed','Settled')
GROUP BY COALESCE(NULLIF(TRIM(PaymentMethod),''),'Unspecified')
ORDER BY `Revenue (PHP)` DESC;");

            // ── Build page ─────────────────────────────────────────────

            var sections = new List<Control>
            {
                StaffPortalUi.AlertBanner(dbMsg, !dbOk),

                // KPI row
                StaffPortalUi.StatsRow(
                    (StaffPortalUi.Peso(todayRev),  $"Today  ({todayTxn} txn)",        WildNestUI.Green),
                    (StaffPortalUi.Peso(weekRev),   $"This Week  ({weekTxn} txn)",      WildNestUI.Amber),
                    (StaffPortalUi.Peso(monthRev),  $"This Month  ({monthTxn} txn)",    WildNestUI.Blue),
                    (StaffPortalUi.Peso(yearRev),   $"This Year  ({yearTxn} txn)",      WildNestUI.Green)),

                // Snapshot metric table
                StaffPortalUi.MetricTableCard(
                    "Revenue Snapshot",
                    ("Today's Revenue",           StaffPortalUi.Peso(todayRev)),
                    ("Today's Transactions",      todayTxn.ToString()),
                    ("This Week's Revenue",       StaffPortalUi.Peso(weekRev)),
                    ("This Week's Transactions",  weekTxn.ToString()),
                    ("This Month's Revenue",      StaffPortalUi.Peso(monthRev)),
                    ("This Month's Transactions", monthTxn.ToString()),
                    ("This Year's Revenue",       StaffPortalUi.Peso(yearRev)),
                    ("This Year's Transactions",  yearTxn.ToString()),
                    ("Report Generated",          DateTime.Now.ToString("MMMM d, yyyy  h:mm tt"))),

                // Period tabs panel
                BuildPeriodTabPanel(dailyTable, weeklyTable, monthlyTable, yearlyTable),

                // Cross-cutting breakdowns
                StaffPortalUi.GridCard("Revenue by Booking Type",    byBookingType,   "No booking type data available."),
                StaffPortalUi.GridCard("Revenue by Payment Method",  byPaymentMethod, "No payment method data available.")
            };

            var page = StaffPortalUi.BuildPage(
                "Sales Reports",
                $"Resort revenue analysis  ·  Generated {DateTime.Now:MMMM d, yyyy  h:mm tt}",
                sections);

            Controls.Add(page);
        }

        // ── Period tab strip ──────────────────────────────────────────────
        // Renders four tabs above a shared grid panel.
        // Clicking a tab swaps the grid content — no extra scroll, no nesting.

        private Panel BuildPeriodTabPanel(
            System.Data.DataTable daily,
            System.Data.DataTable weekly,
            System.Data.DataTable monthly,
            System.Data.DataTable yearly)
        {
            // Outer card with header
            var card = WildNestUI.CardWithHeader(1100, 480, "Period Sales Detail", 42);

            // Tab button row sits just below the card header line
            var tabBar = new FlowLayoutPanel
            {
                Location      = new Point(14, 52),
                Size          = new Size(card.Width - 28, 42),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            card.Controls.Add(tabBar);

            // Grid host below the tabs
            var gridHost = new Panel
            {
                Location  = new Point(14, 104),
                Size      = new Size(card.Width - 28, card.Height - 118),
                BackColor = Color.White
            };
            card.Controls.Add(gridHost);

            // Keep tab bar and grid host sized when card resizes
            card.Resize += (s, e) =>
            {
                tabBar.Width   = card.Width - 28;
                gridHost.Width = card.Width - 28;
                gridHost.Size  = new Size(card.Width - 28, card.Height - 118);
            };

            Button? activeTab = null;

            // Helper: activate a tab + load its table
            void Activate(System.Data.DataTable table, Button btn)
            {
                // Deactivate old tab
                if (activeTab != null)
                {
                    activeTab.BackColor = Color.FromArgb(230, 227, 222);
                    activeTab.ForeColor = WildNestUI.Muted;
                }
                activeTab           = btn;
                btn.BackColor       = WildNestUI.Forest;
                btn.ForeColor       = WildNestUI.Gold;

                // Swap grid
                gridHost.Controls.Clear();
                var grid = StaffPortalUi.CreateGrid(table);
                grid.Dock = DockStyle.Fill;
                gridHost.Controls.Add(grid);
            }

            // Build the four tab buttons
            var periods = new (string Label, System.Data.DataTable Table)[]
            {
                ("📅  Daily (30 d)",   daily),
                ("📅  Weekly (13 wk)", weekly),
                ("📅  Monthly (12 mo)",monthly),
                ("📅  Yearly",         yearly),
            };

            foreach (var (label, table) in periods)
            {
                var captured = table;
                var btn = new Button
                {
                    Text      = label,
                    Size      = new Size(190, 36),
                    Margin    = new Padding(0, 0, 6, 0),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(230, 227, 222),
                    ForeColor = WildNestUI.Muted,
                    Font      = WildNestUI.FontLabel(8.5f),
                    Cursor    = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                tabBar.Controls.Add(btn);
                btn.Click += (s, e) => Activate(captured, btn);

                // Auto-select Monthly on load
                if (label.StartsWith("📅  Monthly"))
                    btn.PerformClick();
            }

            return card;
        }
    }
}
