using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Project.UcManager
{
    public class UcManagerDashboardContent : UserControl
    {
        public UcManagerDashboardContent()
        {
            Dock = DockStyle.Fill;
            BackColor = WildNestUI.Sand;
            Load += (_, _) => Render();
        }

        private void Render()
        {
            Controls.Clear();
            StaffPortalDb.RefreshReservationLifecycle();

            bool dbOk = StaffPortalDb.CanConnect(out string dbMessage);

            int cabinsAvailable = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins WHERE Status = 'Available';");
            int activeReservations = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status IN ('Confirmed','Checked-In','Overdue');");
            decimal collected = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE Status IN ('Paid','Completed','Settled');");
            int activeUsers = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_users WHERE IsActive = 1;");

            int totalReservations = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations;");
            int todayBookings = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE DATE(CreatedAt) = CURDATE();");
            int checkedIn = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Checked-In';");
            int overdue = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_reservations WHERE Status = 'Overdue';");
            int pendingPayments = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_payments WHERE Status NOT IN ('Paid','Completed','Settled');");
            int guestChats = StaffPortalDb.TableExists("tbl_chat") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_chat;") : 0;
            int staffChats = StaffPortalDb.TableExists("tbl_staffmessages") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_staffmessages;") : 0;

            int animals = StaffPortalDb.TableExists("tbl_animals") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_animals;") : 0;
            int healthAlerts = StaffPortalDb.TableExists("tbl_healthrecords") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_healthrecords WHERE IsCleared = 0;") : 0;
            int feedingsToday = StaffPortalDb.TableExists("tbl_feedings") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_feedings WHERE FeedingDate = CURDATE();") : 0;
            int experiences = StaffPortalDb.TableExists("tbl_experiences") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_experiences;") : 0;
            int bookedExperienceLines = StaffPortalDb.TableExists("tbl_bookingexperiences") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_bookingexperiences;") : 0;
            int tourSchedules = StaffPortalDb.TableExists("tbl_tourschedules") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_tourschedules;") : 0;
            int tourCompletions = StaffPortalDb.TableExists("tbl_tourcompletions") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_tourcompletions;") : 0;
            int totalGuests = StaffPortalDb.TableExists("tbl_guests") ? StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_guests;") : 0;
            int totalCabins = StaffPortalDb.Count("SELECT COUNT(*) FROM tbl_cabins;");
            decimal occupancyRate = totalCabins == 0 ? 0m : Math.Round((activeReservations / (decimal)totalCabins) * 100m, 1);

            decimal todayRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE DATE(PaidAt)=CURDATE() AND Status IN ('Paid','Completed','Settled');");
            decimal monthRevenue = StaffPortalDb.Sum("SELECT COALESCE(SUM(Amount),0) FROM tbl_payments WHERE YEAR(PaidAt)=YEAR(CURDATE()) AND MONTH(PaidAt)=MONTH(CURDATE()) AND Status IN ('Paid','Completed','Settled');");

            var reservations = StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS `Reservation Ref`,
       CONCAT(g.FirstName, ' ', g.LastName) AS `Guest`,
       COALESCE(c.CabinName, 'No Cabin / Visit Booking') AS `Cabin`,
       COALESCE(DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'), DATE_FORMAT(r.VisitDate, '%Y-%m-%d')) AS `Start`,
       DATE_FORMAT(r.CheckOutDate, '%Y-%m-%d') AS `End`,
       r.BookingType AS `Type`,
       r.Status,
       r.TotalAmount AS `Total Amount`
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
ORDER BY r.CreatedAt DESC
LIMIT 12;");

            var bookingTypes = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(BookingType),''),'Unspecified') AS `Booking Type`,
       COUNT(*) AS `Reservations`,
       COALESCE(SUM(TotalAmount),0) AS `Revenue`
FROM tbl_reservations
GROUP BY COALESCE(NULLIF(TRIM(BookingType),''),'Unspecified')
ORDER BY `Reservations` DESC, `Revenue` DESC;");

            var revenueTrend = StaffPortalDb.GetTable(@"
SELECT DATE_FORMAT(PaidAt, '%Y-%m') AS `Month`,
       COALESCE(SUM(Amount),0) AS `Revenue`
FROM tbl_payments
WHERE PaidAt IS NOT NULL
  AND Status IN ('Paid','Completed','Settled')
GROUP BY DATE_FORMAT(PaidAt, '%Y-%m')
ORDER BY `Month` DESC
LIMIT 6;");

            var tourStatus = StaffPortalDb.TableExists("tbl_tourschedules")
                ? StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Status),''),'Unspecified') AS `Tour Status`,
       COUNT(*) AS `Schedules`
FROM tbl_tourschedules
GROUP BY COALESCE(NULLIF(TRIM(Status),''),'Unspecified')
ORDER BY `Schedules` DESC;")
                : new DataTable();

            var wildlifeOps = (StaffPortalDb.TableExists("tbl_animals") && StaffPortalDb.TableExists("tbl_healthrecords"))
                ? StaffPortalDb.GetTable(@"
SELECT a.ZoneName AS `Zone`,
       COUNT(*) AS `Animals`,
       SUM(CASE WHEN a.IsEncounterEligible = 1 THEN 1 ELSE 0 END) AS `Eligible`,
       SUM(CASE WHEN a.HealthStatus <> 'Healthy' THEN 1 ELSE 0 END) AS `With Alerts`
FROM tbl_animals a
GROUP BY a.ZoneName
ORDER BY `Animals` DESC, `Eligible` DESC;")
                : new DataTable();

            var paymentWatch = StaffPortalDb.GetTable(@"
SELECT COALESCE(NULLIF(TRIM(Status),''),'Unspecified') AS `Payment Status`,
       COUNT(*) AS `Records`,
       COALESCE(SUM(Amount),0) AS `Amount`
FROM tbl_payments
GROUP BY COALESCE(NULLIF(TRIM(Status),''),'Unspecified')
ORDER BY `Amount` DESC, `Records` DESC;");

            var sections = new List<Control>
            {
                StaffPortalUi.AlertBanner(dbOk
                    ? $"Manager command center is connected. Live operational data is loading from wildnest_db."
                    : $"Manager command center could not verify the database connection: {dbMessage}", !dbOk),
                BuildAuthorityHero(activeReservations, todayRevenue, monthRevenue, activeUsers, pendingPayments, occupancyRate, todayBookings, checkedIn, overdue),
                StaffPortalUi.MessageCard(
                    "Manager Command Center",
                    "This dashboard now elevates the Manager role beyond a simple admin mirror. It combines bookings, payments, staff accounts, guest messaging, wildlife operations, and tour execution into one executive overview.",
                    alert: false),
                BuildBookingResetCard(),
                new UcManagerSmartSearch(),
                StaffPortalUi.StatsRow(
                    (cabinsAvailable.ToString(), "Cabins Available", WildNestUI.Green),
                    (activeReservations.ToString(), "Active Reservations", WildNestUI.Blue),
                    (StaffPortalUi.Peso(todayRevenue), "Collected Today", WildNestUI.Amber),
                    (activeUsers.ToString(), "Active Staff Users", WildNestUI.Green)),
                StaffPortalUi.MetricTableCard(
                    "Executive Snapshot",
                    ("Total Guests", totalGuests.ToString()),
                    ("Total Reservations", totalReservations.ToString()),
                    ("Bookings Created Today", todayBookings.ToString()),
                    ("Currently Checked-In", checkedIn.ToString()),
                    ("Overdue Stays Requiring Action", overdue.ToString()),
                    ("Pending Payments", pendingPayments.ToString()),
                    ("Monthly Revenue", StaffPortalUi.Peso(monthRevenue)),
                    ("Overall Collections", StaffPortalUi.Peso(collected)),
                    ("Occupancy Rate", occupancyRate.ToString("N1") + "%"),
                    ("Guest Chat Messages", guestChats.ToString()),
                    ("Internal Staff Messages", staffChats.ToString()),
                    ("DB Status", dbOk ? "Connected" : "Attention Needed")),
                StaffPortalUi.MetricTableCard(
                    "Wildlife and Experience Snapshot",
                    ("Animal Registry", animals.ToString()),
                    ("Open Health Alerts", healthAlerts.ToString()),
                    ("Feedings Scheduled Today", feedingsToday.ToString()),
                    ("Experience Packages", experiences.ToString()),
                    ("Booked Experience Lines", bookedExperienceLines.ToString()),
                    ("Tour Schedules", tourSchedules.ToString()),
                    ("Tour Completions", tourCompletions.ToString())),
                StaffPortalUi.TrendCard(
                    "Monthly Revenue Trend",
                    ToTrendPoints(revenueTrend, "Month", "Revenue", WildNestUI.Amber, valuePrefix: "PHP "),
                    "No payment trend data available yet."),
                StaffPortalUi.TrendCard(
                    "Booking Type Performance",
                    ToTrendPoints(bookingTypes, "Booking Type", "Reservations", WildNestUI.Green, valueSuffix: " bookings"),
                    "No booking analytics available yet."),
                StaffPortalUi.GridCard("Payment Status Watch", paymentWatch, "No payment monitoring data available yet."),
                StaffPortalUi.GridCard("Tour Operations Status", tourStatus, "No tour schedule data available yet."),
                StaffPortalUi.GridCard("Wildlife Operations by Zone", wildlifeOps, "No wildlife operations data available yet."),
                StaffPortalUi.GridCard("Recent Reservation Activity", reservations, "No reservation records found yet.")
            };

            var page = StaffPortalUi.BuildPage(
                "Manager Command Center",
                $"Live executive overview across booking, finance, staff, guest, wildlife, and tour operations as of {DateTime.Now:MMMM d, yyyy h:mm tt}.",
                sections);

            Controls.Add(page);
        }

        private Panel BuildAuthorityHero(int activeReservations, decimal todayRevenue, decimal monthRevenue, int activeUsers, int pendingPayments, decimal occupancyRate, int todayBookings, int checkedIn, int overdue)
        {
            var hero = new Panel
            {
                Height = 332,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };

            hero.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                WildNestUI.PaintSoftShadow(e.Graphics, new Rectangle(6, 10, hero.Width - 18, hero.Height - 18), 16, 4);
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, hero.Width - 1, hero.Height - 1), 22);
                using var fill = new System.Drawing.Drawing2D.LinearGradientBrush(hero.ClientRectangle, Color.FromArgb(8, 31, 17), Color.FromArgb(20, 65, 40), 0f);
                using var border = new Pen(Color.FromArgb(70, WildNestUI.Gold), 1f);
                using var accent = new SolidBrush(Color.FromArgb(26, WildNestUI.Gold));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
                e.Graphics.FillEllipse(accent, -28, -18, 120, 120);
                e.Graphics.FillEllipse(accent, hero.Width - 108, hero.Height - 96, 144, 144);
            };

            hero.Controls.Add(new Label
            {
                Text = "Manager Authority Deck",
                Font = WildNestUI.FontTitle(18f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 22)
            });

            hero.Controls.Add(new Label
            {
                Text = "A live operational readout for occupancy, collection momentum, staff readiness, and booking pressure so the manager can act from one premium command surface.",
                Font = WildNestUI.FontBody(9.8f),
                ForeColor = Color.FromArgb(224, WildNestUI.Cream),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(820, 40),
                Location = new Point(28, 56)
            });

            var glanceStrip = BuildAtAGlanceStrip(
                ("Today", $"{todayBookings}", "new bookings"),
                ("Arrivals", $"{checkedIn}", "already checked in"),
                ("Payments", $"{pendingPayments}", "items pending"),
                ("Alerts", $"{overdue}", "overdue stays"));
            hero.Controls.Add(glanceStrip);

            var first = BuildAuthorityMetric("Operations Pulse", $"{activeReservations} active arrivals\n{occupancyRate:N1}% occupancy pressure", WildNestUI.Green);
            hero.Controls.Add(first);

            var second = BuildAuthorityMetric("Revenue Window", $"{StaffPortalUi.Peso(todayRevenue)} today\n{StaffPortalUi.Peso(monthRevenue)} this month", WildNestUI.Gold);
            hero.Controls.Add(second);

            var third = BuildAuthorityMetric("Control Watch", $"{activeUsers} active staff users\n{pendingPayments} payment items need review", WildNestUI.Blue);
            hero.Controls.Add(third);

            void LayoutHero()
            {
                int sidePadding = 28;
                glanceStrip.Location = new Point(sidePadding, 118);
                glanceStrip.Size = new Size(Math.Max(760, hero.Width - (sidePadding * 2)), 72);

                int gap = 14;
                int metricY = 212;
                int metricWidth = Math.Max(230, (hero.Width - (sidePadding * 2) - (gap * 2)) / 3);
                int metricHeight = 94;

                first.Location = new Point(sidePadding, metricY);
                first.Size = new Size(metricWidth, metricHeight);

                second.Location = new Point(first.Right + gap, metricY);
                second.Size = new Size(metricWidth, metricHeight);

                third.Location = new Point(second.Right + gap, metricY);
                third.Size = new Size(metricWidth, metricHeight);
            }

            hero.Resize += (s, e) => LayoutHero();
            hero.HandleCreated += (s, e) => LayoutHero();
            LayoutHero();

            return hero;
        }

        private static Panel BuildAtAGlanceStrip(params (string Label, string Value, string Sub)[] items)
        {
            var strip = new Panel
            {
                BackColor = Color.FromArgb(20, 255, 255, 255)
            };
            strip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, strip.Width - 1, strip.Height - 1), 18);
                using var fill = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                using var border = new Pen(Color.FromArgb(62, WildNestUI.Gold), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };

            int count = Math.Max(1, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var cell = new Panel
                {
                    BackColor = Color.Transparent
                };
                strip.Controls.Add(cell);

                cell.Controls.Add(new Label
                {
                    Text = item.Label.ToUpperInvariant(),
                    Font = WildNestUI.FontLabel(8f),
                    ForeColor = WildNestUI.Gold,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Location = new Point(0, 8)
                });

                cell.Controls.Add(new Label
                {
                    Text = item.Value,
                    Font = WildNestUI.FontTitle(18f),
                    ForeColor = WildNestUI.Cream,
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Location = new Point(0, 22)
                });

                cell.Controls.Add(new Label
                {
                    Text = item.Sub,
                    Font = WildNestUI.FontBody(8.6f),
                    ForeColor = Color.FromArgb(214, WildNestUI.Cream),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Location = new Point(0, 49)
                });

                Panel? divider = null;
                if (i < items.Length - 1)
                {
                    divider = new Panel
                    {
                        Width = 1,
                        BackColor = Color.FromArgb(52, WildNestUI.Gold)
                    };
                    strip.Controls.Add(divider);
                }

                strip.Resize += (s, e) =>
                {
                    int cellWidth = (strip.Width - 48 - ((count - 1) * 18)) / count;
                    cell.Location = new Point(24 + ((cellWidth + 18) * i), 6);
                    cell.Size = new Size(Math.Max(120, cellWidth), strip.Height - 12);

                    if (divider != null)
                    {
                        int x = 24 + ((cellWidth + 18) * i) + cellWidth + 9;
                        divider.Location = new Point(x, 14);
                        divider.Height = strip.Height - 28;
                    }
                };
            }

            return strip;
        }

        private static Panel BuildAuthorityMetric(string title, string body, Color accent)
        {
            var panel = new Panel
            {
                Size = new Size(274, 94),
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
                Font = WildNestUI.FontLabel(8f),
                ForeColor = accent,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 10)
            });

            panel.Controls.Add(new Label
            {
                Text = body,
                Font = WildNestUI.FontBody(9.1f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(240, 58),
                Location = new Point(16, 26)
            });

            return panel;
        }

        private Panel BuildBookingResetCard()
        {
            return StaffPortalUi.ActionCard("Booking Reset Console", card =>
            {
                var badge = WildNestUI.Badge("BOOKING-ONLY RESET", BadgeStyle.Red);
                badge.Location = new Point(18, 62);

                var intro = new Label
                {
                    Text = "Use this only when you need to wipe all guest booking activity and restart the booking environment from a clean slate for a new demo, defense, or fresh run.",
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.TextDark,
                    Location = new Point(18, 98),
                    Size = new Size(card.Width - 326, 42),
                    BackColor = Color.Transparent
                };

                var note = new Label
                {
                    Text = "This clears reservations, payments, booked experiences, tour schedules/completions, guest booking chats, guest records, generated pass files, and resets all cabins to Available.",
                    Font = WildNestUI.FontBody(9.2f),
                    ForeColor = WildNestUI.Red,
                    Location = new Point(18, 148),
                    Size = new Size(card.Width - 326, 40),
                    BackColor = Color.Transparent
                };

                var safeNote = new Label
                {
                    Text = "Staff accounts, animal records, health records, feedings, and internal staff messages are preserved.",
                    Font = WildNestUI.FontBody(9.1f),
                    ForeColor = WildNestUI.Blue,
                    Location = new Point(18, 192),
                    Size = new Size(card.Width - 326, 24),
                    BackColor = Color.Transparent
                };

                var sidePanel = new Panel
                {
                    Size = new Size(286, 144),
                    BackColor = Color.FromArgb(250, 242, 241)
                };
                sidePanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using var path = WildNestUI.RoundRect(new Rectangle(0, 0, sidePanel.Width - 1, sidePanel.Height - 1), 16);
                    using var fill = new SolidBrush(Color.FromArgb(250, 242, 241));
                    using var border = new Pen(Color.FromArgb(237, 198, 198), 1f);
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                };
                card.Controls.Add(sidePanel);

                sidePanel.Controls.Add(new Label
                {
                    Text = "RESET IMPACT",
                    Font = WildNestUI.FontLabel(8.3f),
                    ForeColor = WildNestUI.Red,
                    AutoSize = true,
                    Location = new Point(16, 14),
                    BackColor = Color.Transparent
                });

                sidePanel.Controls.Add(new Label
                {
                    Text = "Old guest QR passes will stop working.\nNew bookings will generate fresh reservation references and fresh QR output.",
                    Font = WildNestUI.FontBody(9f),
                    ForeColor = WildNestUI.TextDark,
                    AutoSize = false,
                    Size = new Size(248, 52),
                    Location = new Point(16, 34),
                    BackColor = Color.Transparent
                });

                var btnReset = new Button
                {
                    Text = "Reset All Booking Data",
                    Size = new Size(220, 46),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = WildNestUI.Red,
                    ForeColor = Color.White,
                    Font = new Font(WildNestUI.FontBody(9.5f), FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnReset.FlatAppearance.BorderSize = 0;
                void ShapeButton()
                {
                    using var path = WildNestUI.RoundRect(new Rectangle(0, 0, Math.Max(1, btnReset.Width - 1), Math.Max(1, btnReset.Height - 1)), 12);
                    btnReset.Region = new Region(path);
                }
                ShapeButton();
                btnReset.Resize += (_, _) => ShapeButton();
                sidePanel.Controls.Add(btnReset);
                btnReset.Location = new Point(33, 86);

                void LayoutResetCard()
                {
                    sidePanel.Location = new Point(card.Width - sidePanel.Width - 18, 64);
                    intro.Size = new Size(Math.Max(320, sidePanel.Left - 36), 42);
                    note.Size = new Size(Math.Max(320, sidePanel.Left - 36), 40);
                    safeNote.Size = new Size(Math.Max(320, sidePanel.Left - 36), 24);
                }
                card.Resize += (_, _) => LayoutResetCard();
                btnReset.Click += (_, _) => RunBookingReset();

                card.Controls.Add(badge);
                card.Controls.Add(intro);
                card.Controls.Add(note);
                card.Controls.Add(safeNote);
                LayoutResetCard();
            }, 226);
        }

        private void RunBookingReset()
        {
            var first = MessageBox.Show(
                "This will permanently clear all guest bookings, payments, booking experiences, tour schedules, booking chats, guest records, and generated QR/pass files. Staff, animals, and manager accounts will stay intact.\n\nDo you want to continue?",
                "Reset All Booking Data",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (first != DialogResult.Yes)
                return;

            var second = MessageBox.Show(
                "Final confirmation: cabins will be reset to Available and old guest QR/pass records will stop working.\n\nProceed with full booking reset?",
                "Final Booking Reset Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Stop,
                MessageBoxDefaultButton.Button2);

            if (second != DialogResult.Yes)
                return;

            try
            {
                StaffPortalDb.ResetBookingData();
                MessageBox.Show(
                    "All booking-side data has been reset. New bookings will generate fresh reservation references and fresh QR passes.",
                    "Booking Reset Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Render();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Booking reset failed: " + ex.Message,
                    "Reset Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static IEnumerable<(string Label, decimal Value, string Display, Color Color)> ToTrendPoints(
            DataTable table,
            string labelColumn,
            string valueColumn,
            Color color,
            string valuePrefix = "",
            string valueSuffix = "")
        {
            return table.Rows
                .Cast<DataRow>()
                .Select(row =>
                {
                    decimal value = row[valueColumn] == DBNull.Value ? 0m : Convert.ToDecimal(row[valueColumn]);
                    string label = Convert.ToString(row[labelColumn]) ?? "N/A";
                    return (label, value, $"{valuePrefix}{value:N0}{valueSuffix}".Trim(), color);
                })
                .ToList();
        }
    }
}
