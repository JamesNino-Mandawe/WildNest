using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Project.UcManager
{
    internal sealed class UcManagerSmartSearch : Panel
    {
        private readonly TextBox _txtSearch;
        private readonly FlowLayoutPanel _results;
        private readonly Label _summary;

        public UcManagerSmartSearch()
        {
            Height = 238;
            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 18);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = WildNestUI.Forest
            };
            Controls.Add(header);

            header.Controls.Add(new Label
            {
                Text = "Manager Command Search",
                Font = WildNestUI.FontTitle(18f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 18)
            });

            header.Controls.Add(new Label
            {
                Text = "Search bookings, guests, cabins, wildlife, experiences, payments, tours, users, and chat activity from one executive control point.",
                Font = WildNestUI.FontBody(9.5f),
                ForeColor = Color.FromArgb(210, 238, 232, 220),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(25, 52)
            });

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search reservation, guest, cabin, animal, payment, role, status, experience, or chat...",
                BorderStyle = BorderStyle.FixedSingle,
                Font = WildNestUI.FontBody(11f),
                Location = new Point(24, 116),
                Height = 34
            };
            Controls.Add(_txtSearch);

            var btnSearch = WildNestUI.BtnPrimary("Search", 120, 34);
            btnSearch.Location = new Point(0, 116);
            btnSearch.Click += (s, e) => RunSearch();
            Controls.Add(btnSearch);

            var btnClear = new Button
            {
                Text = "Clear",
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.FromArgb(246, 242, 234),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(86, 34),
                Cursor = Cursors.Hand,
                Location = new Point(0, 116)
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(218, 209, 190);
            btnClear.Click += (s, e) =>
            {
                _txtSearch.Clear();
                RenderEmptyState();
            };
            Controls.Add(btnClear);

            _summary = new Label
            {
                Text = "Type at least 2 characters to begin searching.",
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(24, 160),
                Height = 42
            };
            Controls.Add(_summary);

            _results = new FlowLayoutPanel
            {
                Location = new Point(24, 190),
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent
            };
            Controls.Add(_results);

            _txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    RunSearch();
                }
            };

            Resize += (s, e) => LayoutSearch(btnSearch, btnClear);
            LayoutSearch(btnSearch, btnClear);
            RenderEmptyState();
        }

        private void LayoutSearch(Button btnSearch, Button btnClear)
        {
            int rightPad = 24;
            int gap = 10;
            btnClear.Location = new Point(Width - rightPad - btnClear.Width, 116);
            btnSearch.Location = new Point(btnClear.Left - gap - btnSearch.Width, 116);
            _txtSearch.Width = Math.Max(300, btnSearch.Left - 24 - gap);
            _summary.Width = Math.Max(300, Width - 48);
            _results.Size = new Size(Math.Max(300, Width - 48), Math.Max(100, Height - _results.Top - 18));

            foreach (Control result in _results.Controls)
                result.Width = Math.Max(280, _results.ClientSize.Width - 24);
        }

        private void RenderEmptyState()
        {
            _results.Controls.Clear();
            _summary.Text = "Try: WN-2026, guest email, cabin, animal, experience, health status, payment status, staff role, or tour schedule.";
            AddHint("Manager search covers reservations, guests, cabins, experiences, animals, health records, feeding records, tours, users, payments, guest chat, and staff chat.");
            AddHint("Use this as a command lookup for fast manager verification instead of scrolling through multiple modules.");
        }

        private void AddHint(string text)
        {
            _results.Controls.Add(new Label
            {
                Text = text,
                Font = WildNestUI.FontBody(9.4f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = Math.Max(280, _results.ClientSize.Width - 24),
                Height = 32,
                Margin = new Padding(0, 8, 0, 0)
            });
        }

        private void RunSearch()
        {
            string term = _txtSearch.Text.Trim();
            if (term.Length < 2)
            {
                RenderEmptyState();
                return;
            }

            var items = FetchResults(term);
            _results.Controls.Clear();
            _summary.Text = items.Count == 0
                ? $"No matching operational records found for \"{term}\"."
                : $"Found {items.Count} result(s) for \"{term}\".";

            if (items.Count == 0)
            {
                AddHint("Tip: search reservation ID, guest/email/phone, cabin, animal, zone, health status, experience, payment, tour date, staff role, or chat text.");
                return;
            }

            foreach (var group in items.GroupBy(i => i.Group))
            {
                _results.Controls.Add(GroupHeader(group.Key, group.Count()));
                foreach (var item in group)
                    _results.Controls.Add(ResultCard(item));
            }
        }

        private static List<SearchResult> FetchResults(string term)
        {
            string like = "%" + term + "%";
            var results = new List<SearchResult>();

            AddCoreResults(results, like);
            AddOptionalResults(results, like);

            return results.Take(80).ToList();
        }

        private static void AddCoreResults(List<SearchResult> results, string like)
        {
            AddRows(results, "Reservations", StaffPortalDb.GetTable(@"
SELECT r.ReservationID AS Id,
       CONCAT(COALESCE(g.FirstName,''), ' ', COALESCE(g.LastName,'')) AS Title,
       CONCAT(r.BookingType, ' | ', r.Status, ' | ', COALESCE(c.CabinName, 'No cabin'), ' | ',
              COALESCE(DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'), DATE_FORMAT(r.VisitDate, '%Y-%m-%d'), 'No date'),
              ' | PHP ', FORMAT(r.TotalAmount, 2)) AS Detail
FROM tbl_reservations r
LEFT JOIN tbl_guests g ON g.GuestID = r.GuestID
LEFT JOIN tbl_cabins c ON c.CabinID = r.CabinID
WHERE r.ReservationID LIKE @q
   OR r.BookingType LIKE @q
   OR r.Status LIKE @q
   OR COALESCE(r.ArrivalTime,'') LIKE @q
   OR COALESCE(r.ModeOfTransport,'') LIKE @q
   OR COALESCE(DATE_FORMAT(r.CheckInDate, '%Y-%m-%d'),'') LIKE @q
   OR COALESCE(DATE_FORMAT(r.CheckOutDate, '%Y-%m-%d'),'') LIKE @q
   OR COALESCE(DATE_FORMAT(r.VisitDate, '%Y-%m-%d'),'') LIKE @q
   OR CAST(r.TotalAmount AS CHAR) LIKE @q
   OR CAST(r.NumAdults AS CHAR) LIKE @q
   OR CAST(r.NumChildren AS CHAR) LIKE @q
   OR CONCAT(COALESCE(g.FirstName,''), ' ', COALESCE(g.LastName,'')) LIKE @q
   OR COALESCE(g.Email,'') LIKE @q
   OR COALESCE(g.Phone,'') LIKE @q
   OR COALESCE(c.CabinName,'') LIKE @q
ORDER BY r.CreatedAt DESC
LIMIT 10;", new MySqlParameter("@q", like)));

            AddRows(results, "Guests", StaffPortalDb.GetTable(@"
SELECT CAST(g.GuestID AS CHAR) AS Id,
       CONCAT(g.FirstName, ' ', g.LastName) AS Title,
       CONCAT(COALESCE(g.Email,'No email'), ' | ', COALESCE(g.Phone,'No phone'), ' | ', COALESCE(g.Nationality,'No nationality'), ' | ', COALESCE(g.ValidIDType,'No ID type')) AS Detail
FROM tbl_guests g
WHERE CONCAT(g.FirstName, ' ', g.LastName) LIKE @q
   OR COALESCE(g.Email,'') LIKE @q
   OR COALESCE(g.Phone,'') LIKE @q
   OR COALESCE(g.Nationality,'') LIKE @q
   OR COALESCE(g.ValidIDType,'') LIKE @q
   OR COALESCE(g.SpecialRequests,'') LIKE @q
ORDER BY g.CreatedAt DESC
LIMIT 10;", new MySqlParameter("@q", like)));

            AddRows(results, "Cabins", StaffPortalDb.GetTable(@"
SELECT CAST(CabinID AS CHAR) AS Id,
       CabinName AS Title,
       CONCAT('PHP ', FORMAT(PricePerNight, 2), ' / night | ', MaxGuests, ' guests | ', Status) AS Detail
FROM tbl_cabins
WHERE CabinName LIKE @q
   OR Status LIKE @q
   OR CAST(MaxGuests AS CHAR) LIKE @q
   OR CAST(PricePerNight AS CHAR) LIKE @q
ORDER BY CabinName
LIMIT 10;", new MySqlParameter("@q", like)));

            AddRows(results, "Staff Users", StaffPortalDb.GetTable(@"
SELECT CAST(UserID AS CHAR) AS Id,
       FullName AS Title,
       CONCAT(Username, ' | ', Role, ' | ', COALESCE(ContactNo,'No contact'), ' | ', CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END) AS Detail
FROM tbl_users
WHERE FullName LIKE @q
   OR Username LIKE @q
   OR Role LIKE @q
   OR ContactNo LIKE @q
   OR CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END LIKE @q
ORDER BY Role, FullName
LIMIT 10;", new MySqlParameter("@q", like)));

            AddRows(results, "Payments", StaffPortalDb.GetTable(@"
SELECT ReservationID AS Id,
       CONCAT('Payment ', PaymentID) AS Title,
       CONCAT('PHP ', FORMAT(Amount, 2), ' | ', PaymentMethod, ' | ', Status, ' | ', COALESCE(DATE_FORMAT(PaidAt, '%Y-%m-%d %h:%i %p'), 'No paid date')) AS Detail
FROM tbl_payments
WHERE ReservationID LIKE @q
   OR PaymentMethod LIKE @q
   OR Status LIKE @q
   OR CAST(Amount AS CHAR) LIKE @q
   OR COALESCE(DATE_FORMAT(PaidAt, '%Y-%m-%d'),'') LIKE @q
ORDER BY PaidAt DESC
LIMIT 10;", new MySqlParameter("@q", like)));
        }

        private static void AddOptionalResults(List<SearchResult> results, string like)
        {
            if (StaffPortalDb.TableExists("tbl_experiences"))
            {
                AddRows(results, "Experiences", StaffPortalDb.GetTable(@"
SELECT CAST(ExperienceID AS CHAR) AS Id,
       ExperienceName AS Title,
       CONCAT('PHP ', FORMAT(PricePerPerson, 2), ' / person | ', DurationMinutes, ' minutes') AS Detail
FROM tbl_experiences
WHERE ExperienceName LIKE @q
   OR CAST(PricePerPerson AS CHAR) LIKE @q
   OR CAST(DurationMinutes AS CHAR) LIKE @q
ORDER BY ExperienceName
LIMIT 10;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_animals"))
            {
                AddRows(results, "Animals", StaffPortalDb.GetTable(@"
SELECT CAST(AnimalID AS CHAR) AS Id,
       AnimalName AS Title,
       CONCAT(Species, ' | ', COALESCE(ZoneName,'No zone'), ' | ', COALESCE(EnclosureName,'No enclosure'), ' | ', HealthStatus) AS Detail
FROM tbl_animals
WHERE AnimalName LIKE @q
   OR Species LIKE @q
   OR COALESCE(ScientificName,'') LIKE @q
   OR COALESCE(Sex,'') LIKE @q
   OR COALESCE(ZoneName,'') LIKE @q
   OR COALESCE(EnclosureName,'') LIKE @q
   OR COALESCE(DietType,'') LIKE @q
   OR HealthStatus LIKE @q
   OR COALESCE(Notes,'') LIKE @q
ORDER BY AnimalName
LIMIT 10;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_healthrecords"))
            {
                AddRows(results, "Health Records", StaffPortalDb.GetTable(@"
SELECT CAST(h.HealthRecordID AS CHAR) AS Id,
       COALESCE(a.AnimalName, CONCAT('Animal #', h.AnimalID)) AS Title,
       CONCAT(h.Status, ' | ', DATE_FORMAT(h.RecordDate, '%Y-%m-%d %h:%i %p'), ' | ', LEFT(CONCAT(COALESCE(h.Diagnosis,''), ' ', COALESCE(h.Treatment,''), ' ', COALESCE(h.Notes,'')), 110)) AS Detail
FROM tbl_healthrecords h
LEFT JOIN tbl_animals a ON a.AnimalID = h.AnimalID
WHERE COALESCE(a.AnimalName,'') LIKE @q
   OR COALESCE(a.Species,'') LIKE @q
   OR h.Status LIKE @q
   OR COALESCE(h.Diagnosis,'') LIKE @q
   OR COALESCE(h.Treatment,'') LIKE @q
   OR COALESCE(h.Notes,'') LIKE @q
   OR COALESCE(DATE_FORMAT(h.RecordDate, '%Y-%m-%d'),'') LIKE @q
ORDER BY h.RecordDate DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_feedings"))
            {
                AddRows(results, "Feeding Records", StaffPortalDb.GetTable(@"
SELECT CAST(f.FeedingID AS CHAR) AS Id,
       COALESCE(a.AnimalName, CONCAT('Animal #', f.AnimalID)) AS Title,
       CONCAT(f.FeedingDate, ' ', f.FeedingTime, ' | ', f.FoodItem, ' | ', f.Quantity, ' | ', f.Status) AS Detail
FROM tbl_feedings f
LEFT JOIN tbl_animals a ON a.AnimalID = f.AnimalID
WHERE COALESCE(a.AnimalName,'') LIKE @q
   OR COALESCE(a.Species,'') LIKE @q
   OR f.FoodItem LIKE @q
   OR f.Quantity LIKE @q
   OR f.Status LIKE @q
   OR COALESCE(f.Notes,'') LIKE @q
   OR COALESCE(DATE_FORMAT(f.FeedingDate, '%Y-%m-%d'),'') LIKE @q
ORDER BY f.FeedingDate DESC, f.FeedingTime DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_bookingexperiences"))
            {
                AddRows(results, "Booked Experiences", StaffPortalDb.GetTable(@"
SELECT bx.ReservationID AS Id,
       COALESCE(e.ExperienceName, CONCAT('Experience #', bx.ExperienceID)) AS Title,
       CONCAT('Quantity ', bx.Quantity, ' | PHP ', FORMAT(bx.TotalCost, 2)) AS Detail
FROM tbl_bookingexperiences bx
LEFT JOIN tbl_experiences e ON e.ExperienceID = bx.ExperienceID
WHERE bx.ReservationID LIKE @q
   OR COALESCE(e.ExperienceName,'') LIKE @q
   OR CAST(bx.Quantity AS CHAR) LIKE @q
   OR CAST(bx.TotalCost AS CHAR) LIKE @q
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_tourschedules"))
            {
                AddRows(results, "Tour Schedules", StaffPortalDb.GetTable(@"
SELECT CAST(ts.TourScheduleID AS CHAR) AS Id,
       COALESCE(e.ExperienceName, CONCAT('Experience #', ts.ExperienceID)) AS Title,
       CONCAT(ts.ReservationID, ' | ', ts.TourDate, ' ', ts.StartTime, ' - ', ts.EndTime, ' | ', ts.Status) AS Detail
FROM tbl_tourschedules ts
LEFT JOIN tbl_experiences e ON e.ExperienceID = ts.ExperienceID
LEFT JOIN tbl_users u ON u.UserID = ts.GuideUserID
WHERE ts.ReservationID LIKE @q
   OR COALESCE(e.ExperienceName,'') LIKE @q
   OR COALESCE(u.FullName,'') LIKE @q
   OR COALESCE(u.Username,'') LIKE @q
   OR ts.Status LIKE @q
   OR COALESCE(ts.Notes,'') LIKE @q
   OR COALESCE(DATE_FORMAT(ts.TourDate, '%Y-%m-%d'),'') LIKE @q
ORDER BY ts.TourDate DESC, ts.StartTime DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_tourcompletions"))
            {
                AddRows(results, "Tour Completions", StaffPortalDb.GetTable(@"
SELECT CAST(tc.TourCompletionID AS CHAR) AS Id,
       CONCAT('Schedule #', tc.TourScheduleID) AS Title,
       CONCAT(tc.CompletionStatus, ' | ', DATE_FORMAT(tc.CompletedAt, '%Y-%m-%d %h:%i %p'), ' | ', COALESCE(tc.Remarks,'')) AS Detail
FROM tbl_tourcompletions tc
LEFT JOIN tbl_users u ON u.UserID = tc.CompletedByUserID
WHERE CAST(tc.TourScheduleID AS CHAR) LIKE @q
   OR tc.CompletionStatus LIKE @q
   OR COALESCE(tc.Remarks,'') LIKE @q
   OR COALESCE(u.FullName,'') LIKE @q
   OR COALESCE(DATE_FORMAT(tc.CompletedAt, '%Y-%m-%d'),'') LIKE @q
ORDER BY tc.CompletedAt DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_chat"))
            {
                AddRows(results, "Guest Chat", StaffPortalDb.GetTable(@"
SELECT ReservationID AS Id,
       GuestName AS Title,
       CONCAT(SenderRole, ' | ', DATE_FORMAT(SentAt, '%Y-%m-%d %h:%i %p'), ' | ', LEFT(Message, 90)) AS Detail
FROM tbl_chat
WHERE ReservationID LIKE @q
   OR GuestName LIKE @q
   OR SenderRole LIKE @q
   OR Message LIKE @q
ORDER BY SentAt DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }

            if (StaffPortalDb.TableExists("tbl_staffmessages"))
            {
                AddRows(results, "Staff Chat", StaffPortalDb.GetTable(@"
SELECT CAST(MessageID AS CHAR) AS Id,
       CONCAT(SenderRole, ' -> ', ReceiverRole) AS Title,
       CONCAT(SenderName, ' | ', DATE_FORMAT(SentAt, '%Y-%m-%d %h:%i %p'), ' | ', LEFT(Message, 90)) AS Detail
FROM tbl_staffmessages
WHERE SenderRole LIKE @q
   OR SenderName LIKE @q
   OR ReceiverRole LIKE @q
   OR Message LIKE @q
   OR COALESCE(DATE_FORMAT(SentAt, '%Y-%m-%d'),'') LIKE @q
ORDER BY SentAt DESC
LIMIT 8;", new MySqlParameter("@q", like)));
            }
        }

        private static void AddRows(List<SearchResult> results, string group, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                results.Add(new SearchResult(
                    group,
                    StaffPortalUi.SafeString(row["Id"], "-"),
                    StaffPortalUi.SafeString(row["Title"], "Untitled"),
                    StaffPortalUi.SafeString(row["Detail"], "")));
            }
        }

        private static Control GroupHeader(string group, int count)
        {
            return new Label
            {
                Text = $"{group.ToUpperInvariant()}  -  {count}",
                Font = WildNestUI.FontLabel(8.5f),
                ForeColor = WildNestUI.Amber,
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 900,
                Height = 24,
                Margin = new Padding(0, 10, 0, 0)
            };
        }

        private Control ResultCard(SearchResult item)
        {
            var card = new Panel
            {
                Width = Math.Max(280, _results.ClientSize.Width - 24),
                Height = 70,
                BackColor = Color.FromArgb(250, 248, 242),
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };

            card.Controls.Add(new Label
            {
                Text = item.Id,
                Font = WildNestUI.FontLabel(8f),
                ForeColor = WildNestUI.Cream,
                BackColor = WildNestUI.Forest,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(14, 18),
                Size = new Size(112, 28)
            });

            card.Controls.Add(new Label
            {
                Text = item.Title,
                Font = WildNestUI.FontBold(10.5f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Location = new Point(142, 12),
                Size = new Size(Math.Max(220, card.Width - 166), 24)
            });

            card.Controls.Add(new Label
            {
                Text = item.Detail,
                Font = WildNestUI.FontBody(9f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                Location = new Point(142, 38),
                Size = new Size(Math.Max(220, card.Width - 166), 22)
            });

            card.Resize += (s, e) =>
            {
                foreach (Control child in card.Controls.Cast<Control>().Where(c => c.Left == 142))
                    child.Width = Math.Max(220, card.Width - 166);
            };

            card.Paint += (s, e) =>
            {
                using var border = new Pen(Color.FromArgb(226, 218, 202));
                e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);
            };

            return card;
        }

        private readonly record struct SearchResult(string Group, string Id, string Title, string Detail);
    }
}
