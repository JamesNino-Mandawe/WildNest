using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MySql.Data.MySqlClient;
using Project.Accomodations;

namespace Project
{
    internal static class StaffPortalDb
    {
        internal const string ConnString = "server=localhost;user=root;database=wildnest_db;password=Natsudragneel_525;Allow User Variables=True;";

        internal static DataTable GetTable(string sql, params MySqlParameter[] parameters)
        {
            var table = new DataTable();
            try
            {
                using var conn = new MySqlConnection(ConnString);
                using var cmd = new MySqlCommand(sql, conn);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                using var adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(table);
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "GetTable failed");
            }

            return table;
        }

        internal static int Count(string sql, params MySqlParameter[] parameters)
        {
            object? value = Scalar(sql, parameters);
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        internal static decimal Sum(string sql, params MySqlParameter[] parameters)
        {
            object? value = Scalar(sql, parameters);
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        internal static object? Scalar(string sql, params MySqlParameter[] parameters)
        {
            try
            {
                using var conn = new MySqlConnection(ConnString);
                using var cmd = new MySqlCommand(sql, conn);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                return cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "Scalar failed");
                return null;
            }
        }

        internal static void Execute(string sql, params MySqlParameter[] parameters)
        {
            try
            {
                using var conn = new MySqlConnection(ConnString);
                using var cmd = new MySqlCommand(sql, conn);
                if (parameters?.Length > 0)
                    cmd.Parameters.AddRange(parameters);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "Execute failed");
                throw;
            }
        }

        internal static void ExecuteTransaction(Action<MySqlConnection, MySqlTransaction> work)
        {
            using var conn = new MySqlConnection(ConnString);
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                work(conn, tx);
                tx.Commit();
            }
            catch
            {
                try
                {
                    tx.Rollback();
                }
                catch
                {
                    // Best effort rollback only.
                }

                throw;
            }
        }

        internal static object? Scalar(MySqlConnection conn, MySqlTransaction tx, string sql, params MySqlParameter[] parameters)
        {
            using var cmd = new MySqlCommand(sql, conn, tx);
            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteScalar();
        }

        internal static int Execute(MySqlConnection conn, MySqlTransaction tx, string sql, params MySqlParameter[] parameters)
        {
            using var cmd = new MySqlCommand(sql, conn, tx);
            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        internal static bool TableExists(string tableName)
        {
            const string sql = @"
SELECT COUNT(*)
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name = @tableName;";
            return Count(sql, new MySqlParameter("@tableName", tableName)) > 0;
        }

        internal static bool CanConnect(out string message)
        {
            try
            {
                using var conn = new MySqlConnection(ConnString);
                conn.Open();
                using var cmd = new MySqlCommand("SELECT DATABASE();", conn);
                string db = Convert.ToString(cmd.ExecuteScalar()) ?? "wildnest_db";
                message = $"Connected successfully to {db}.";
                return true;
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "Connection check failed");
                message = ex.Message;
                return false;
            }
        }

        internal static void EnsureStaffMessageRoutingColumns()
        {
            EnsureColumn("tbl_staffmessages", "SenderUsername", "ALTER TABLE tbl_staffmessages ADD COLUMN SenderUsername VARCHAR(255) NULL AFTER SenderName;");
            EnsureColumn("tbl_staffmessages", "ReceiverUsername", "ALTER TABLE tbl_staffmessages ADD COLUMN ReceiverUsername VARCHAR(255) NULL AFTER ReceiverRole;");
        }

        internal static void EnsureGuestChatAssignmentColumns()
        {
            EnsureColumn("tbl_chat", "AssignedReceptionUsername", "ALTER TABLE tbl_chat ADD COLUMN AssignedReceptionUsername VARCHAR(255) NULL AFTER GuestName;");
            EnsureColumn("tbl_chat", "AssignedReceptionName", "ALTER TABLE tbl_chat ADD COLUMN AssignedReceptionName VARCHAR(255) NULL AFTER AssignedReceptionUsername;");
        }

        internal static void EnsureHealthAlertColumns()
        {
            EnsureColumn("tbl_healthrecords", "IsAlert", "ALTER TABLE tbl_healthrecords ADD COLUMN IsAlert TINYINT(1) NOT NULL DEFAULT 0 AFTER IsCleared;");

            if (!TableExists("tbl_healthrecords"))
                return;

            try
            {
                Execute(@"
UPDATE tbl_healthrecords
SET IsAlert = 1
WHERE COALESCE(IsAlert, 0) = 0
  AND IsCleared = 0
  AND (Diagnosis IS NULL OR TRIM(Diagnosis) = '')
  AND (Treatment IS NULL OR TRIM(Treatment) = '');");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "EnsureHealthAlertColumns backfill failed");
            }
        }

        internal static DataTable GetActiveStaffDirectory(string currentUsername)
        {
            return GetTable(@"
SELECT Username,
       FullName,
       Role
FROM tbl_users
WHERE COALESCE(IsActive, 1) = 1
  AND COALESCE(NULLIF(TRIM(Username), ''), '') <> ''
  AND Role IN ('Manager', 'Administrator', 'Reception', 'TourGuide', 'ZooKeeper')
  AND LOWER(Username) <> LOWER(@username)
ORDER BY
    CASE
        WHEN Role IN ('Manager', 'Administrator') THEN 0
        WHEN Role = 'Reception' THEN 1
        WHEN Role = 'TourGuide' THEN 2
        WHEN Role = 'ZooKeeper' THEN 3
        ELSE 4
    END,
    FullName,
    Username;",
                new MySqlParameter("@username", currentUsername ?? string.Empty));
        }

        internal static bool TryGetStaffUser(string username, out int userId, out string role, out string fullName)
        {
            userId = 0;
            role = string.Empty;
            fullName = string.Empty;

            var table = GetTable(@"
SELECT UserID, FullName, Role
FROM tbl_users
WHERE LOWER(Username) = LOWER(@username)
LIMIT 1;",
                new MySqlParameter("@username", username ?? string.Empty));

            if (table.Rows.Count == 0)
                return false;

            var row = table.Rows[0];
            userId = Convert.ToInt32(row["UserID"]);
            fullName = Convert.ToString(row["FullName"]) ?? username;
            role = Convert.ToString(row["Role"]) ?? string.Empty;
            return true;
        }

        internal static bool TryDeleteOrArchiveStaff(string username, out string message)
        {
            message = "The staff account could not be processed.";
            if (string.IsNullOrWhiteSpace(username))
            {
                message = "Enter a username first.";
                return false;
            }

            if (GetStaffRole(username) is string role &&
                (string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)))
            {
                message = "Manager accounts cannot be deleted from this control.";
                return false;
            }

            if (!TryGetStaffUser(username, out int userId, out _, out string fullName))
            {
                message = $"No staff account was found for '{username}'.";
                return false;
            }

            int linkedOperationalRows =
                Count("SELECT COUNT(*) FROM tbl_feedings WHERE AssignedUserID = @id;", new MySqlParameter("@id", userId)) +
                Count("SELECT COUNT(*) FROM tbl_healthrecords WHERE RecordedByUserID = @id;", new MySqlParameter("@id", userId)) +
                Count("SELECT COUNT(*) FROM tbl_tourschedules WHERE GuideUserID = @id;", new MySqlParameter("@id", userId)) +
                Count("SELECT COUNT(*) FROM tbl_tourcompletions WHERE CompletedByUserID = @id;", new MySqlParameter("@id", userId));

            if (linkedOperationalRows > 0)
            {
                Execute("UPDATE tbl_users SET IsActive = 0 WHERE UserID = @id;", new MySqlParameter("@id", userId));
                message = $"{fullName} has linked operational history, so the account was archived by setting it inactive instead of being deleted.";
                return true;
            }

            Execute("DELETE FROM tbl_users WHERE UserID = @id;", new MySqlParameter("@id", userId));
            message = $"{fullName} was removed from the staff directory.";
            return true;
        }

        internal static string? GetStaffRole(string username)
        {
            object? value = Scalar("SELECT Role FROM tbl_users WHERE LOWER(Username) = LOWER(@username) LIMIT 1;",
                new MySqlParameter("@username", username ?? string.Empty));
            return value == null || value == DBNull.Value ? null : Convert.ToString(value);
        }

        internal static int RefreshReservationLifecycle()
        {
            const string sql = @"
UPDATE tbl_reservations
SET Status = 'Overdue'
WHERE Status = 'Checked-In'
  AND CheckOutDate IS NOT NULL
  AND CheckOutDate < CURDATE();";

            try
            {
                using var conn = new MySqlConnection(ConnString);
                using var cmd = new MySqlCommand(sql, conn);
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, "RefreshReservationLifecycle failed");
                return 0;
            }
        }

        internal static void ResetBookingData()
        {
            ExecuteTransaction((conn, tx) =>
            {
                // Clear child records first, then guests and reservations, while restoring
                // the accommodation inventory to a fresh "Available" state.
                if (TableExists("tbl_tourcompletions"))
                    Execute(conn, tx, "DELETE FROM tbl_tourcompletions;");
                if (TableExists("tbl_tourschedules"))
                    Execute(conn, tx, "DELETE FROM tbl_tourschedules;");
                if (TableExists("tbl_bookingexperiences"))
                    Execute(conn, tx, "DELETE FROM tbl_bookingexperiences;");
                if (TableExists("tbl_chat"))
                    Execute(conn, tx, "DELETE FROM tbl_chat;");
                if (TableExists("tbl_payments"))
                    Execute(conn, tx, "DELETE FROM tbl_payments;");
                if (TableExists("tbl_reservations"))
                    Execute(conn, tx, "DELETE FROM tbl_reservations;");
                if (TableExists("tbl_guests"))
                    Execute(conn, tx, "DELETE FROM tbl_guests;");
                if (TableExists("tbl_cabins"))
                    Execute(conn, tx, "UPDATE tbl_cabins SET Status = 'Available';");
            });

            ResetIdentity("tbl_tourcompletions");
            ResetIdentity("tbl_tourschedules");
            ResetIdentity("tbl_bookingexperiences");
            ResetIdentity("tbl_chat");
            ResetIdentity("tbl_payments");
            ResetIdentity("tbl_guests");
            GuestBookingPassGenerator.ClearGeneratedPasses();
        }

        private static void ResetIdentity(string tableName)
        {
            if (!TableExists(tableName))
                return;

            try
            {
                Execute($"ALTER TABLE {tableName} AUTO_INCREMENT = 1;");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, $"ResetIdentity failed for {tableName}");
            }
        }

        private static void EnsureColumn(string tableName, string columnName, string alterSql)
        {
            if (!TableExists(tableName))
                return;

            bool exists = Count(@"
SELECT COUNT(*)
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = @tableName
  AND column_name = @columnName;",
                new MySqlParameter("@tableName", tableName),
                new MySqlParameter("@columnName", columnName)) > 0;

            if (exists)
                return;

            try
            {
                Execute(alterSql);
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("StaffPortalDb", ex, $"EnsureColumn failed for {tableName}.{columnName}");
            }
        }

        internal static int EnsureWildlifeSeed()
        {
            if (!TableExists("tbl_animals"))
                return 0;

            var animals = new (string Name, string Species, string ScientificName, string Sex, string BirthDate, string Zone, string Enclosure, string Diet, string Health, int Eligible, string Notes)[]
            {
                ("Malakas", "African Lion", "Panthera leo", "Male", "2018-06-12", "Golden Savanna", "Savanna Den A", "Raw meat", "Healthy", 1, "Lead male lion and featured animal of the day."),
                ("Amara", "African Lion", "Panthera leo", "Female", "2019-08-04", "Golden Savanna", "Savanna Den A", "Raw meat", "Healthy", 1, "Calm lioness, monitored during guest viewing."),
                ("Bantay", "Bengal Tiger", "Panthera tigris tigris", "Male", "2017-09-05", "Predator Ridge", "Tiger Rock Habitat", "Raw meat", "Under Observation", 0, "Restricted from encounters until appetite normalizes."),
                ("Sari", "Bengal Tiger", "Panthera tigris tigris", "Female", "2018-02-17", "Predator Ridge", "Tiger Rock Habitat", "Raw meat", "Healthy", 0, "Behind-barrier viewing only."),
                ("Luna", "Clouded Leopard", "Neofelis nebulosa", "Female", "2020-01-18", "Jungle Canopy", "Canopy Shade Den", "Raw meat", "Healthy", 1, "Most active during afternoon keeper rounds."),
                ("Kidlat", "Clouded Leopard", "Neofelis nebulosa", "Male", "2020-10-03", "Jungle Canopy", "Canopy Shade Den", "Raw meat", "Healthy", 0, "Shy temperament; limited keeper access."),
                ("Sinta", "Philippine Eagle", "Pithecophaga jefferyi", "Female", "2019-03-20", "Aviary Dome", "Eagle Flight Habitat", "Fish and poultry", "Healthy", 0, "Protected native species; no direct guest contact."),
                ("Haribon", "Philippine Eagle", "Pithecophaga jefferyi", "Male", "2016-07-11", "Aviary Dome", "Eagle Flight Habitat", "Fish and poultry", "Healthy", 0, "Flagship conservation ambassador."),
                ("Tala", "Reticulated Python", "Malayopython reticulatus", "Female", "2016-11-02", "Reptile House", "Python Habitat A", "Rodents", "Healthy", 0, "Handled only by trained keepers."),
                ("Datu", "Reticulated Python", "Malayopython reticulatus", "Male", "2015-05-24", "Reptile House", "Python Habitat B", "Rodents", "Healthy", 0, "Weekly feeding schedule."),
                ("Maya", "Palawan Peacock-Pheasant", "Polyplectron napoleonis", "Female", "2021-04-09", "Aviary Dome", "Forest Bird Habitat", "Seeds and insects", "Healthy", 1, "Good for educational keeper talks."),
                ("Lakan", "Palawan Peacock-Pheasant", "Polyplectron napoleonis", "Male", "2020-12-13", "Aviary Dome", "Forest Bird Habitat", "Seeds and insects", "Healthy", 1, "Displays plumage during morning activity."),
                ("Bughaw", "Blue-naped Parrot", "Tanygnathus lucionensis", "Male", "2021-06-26", "Aviary Dome", "Parrot Grove", "Fruit and seeds", "Healthy", 1, "Responsive to enrichment puzzles."),
                ("Perlas", "Blue-naped Parrot", "Tanygnathus lucionensis", "Female", "2022-02-14", "Aviary Dome", "Parrot Grove", "Fruit and seeds", "Healthy", 1, "Guest education friendly."),
                ("Gubat", "Philippine Deer", "Rusa marianna", "Male", "2018-01-29", "Forest Edge", "Deer Meadow", "Grass and browse", "Healthy", 1, "Calm around feeding platform."),
                ("Ligaya", "Philippine Deer", "Rusa marianna", "Female", "2019-09-09", "Forest Edge", "Deer Meadow", "Grass and browse", "Healthy", 1, "Often visible during morning rounds."),
                ("Alon", "Asian Small-clawed Otter", "Aonyx cinereus", "Male", "2020-05-19", "Aquatic Zone", "Otter Stream", "Fish and crustaceans", "Healthy", 1, "High activity during enrichment sessions."),
                ("Dalisay", "Asian Small-clawed Otter", "Aonyx cinereus", "Female", "2020-06-02", "Aquatic Zone", "Otter Stream", "Fish and crustaceans", "Healthy", 1, "Pair-bonded with Alon."),
                ("Pag-asa", "Saltwater Crocodile", "Crocodylus porosus", "Male", "2014-10-10", "Aquatic Zone", "Crocodile Marsh", "Fish and poultry", "Healthy", 0, "Secure viewing only."),
                ("Bituin", "Saltwater Crocodile", "Crocodylus porosus", "Female", "2015-12-08", "Aquatic Zone", "Crocodile Marsh", "Fish and poultry", "Healthy", 0, "Secure viewing only."),
                ("Kulas", "Binturong", "Arctictis binturong", "Male", "2019-04-21", "Jungle Canopy", "Binturong Climb", "Fruit and protein", "Healthy", 1, "Strong scent marking; monitor climbing ropes."),
                ("Narra", "Binturong", "Arctictis binturong", "Female", "2020-08-30", "Jungle Canopy", "Binturong Climb", "Fruit and protein", "Healthy", 1, "Excellent for keeper talks."),
                ("Ginto", "Visayan Warty Pig", "Sus cebifrons", "Male", "2018-03-18", "Conservation Hub", "Warty Pig Range", "Vegetables and grain", "Healthy", 1, "Conservation breeding group."),
                ("Hiyas", "Visayan Warty Pig", "Sus cebifrons", "Female", "2019-01-07", "Conservation Hub", "Warty Pig Range", "Vegetables and grain", "Healthy", 1, "Monitor rooting enrichment."),
                ("Pula", "Red Panda", "Ailurus fulgens", "Male", "2021-11-15", "Highland Grove", "Bamboo Walk", "Bamboo and fruit", "Healthy", 0, "Temperature-sensitive habitat."),
                ("Mithi", "Red Panda", "Ailurus fulgens", "Female", "2022-01-12", "Highland Grove", "Bamboo Walk", "Bamboo and fruit", "Healthy", 0, "Temperature-sensitive habitat."),
                ("Dakila", "Asian Elephant", "Elephas maximus", "Male", "2008-05-01", "Golden Savanna", "Elephant Plains", "Hay, fruit, vegetables", "Healthy", 1, "Large mammal encounter from safe barrier."),
                ("Hiraya", "Asian Elephant", "Elephas maximus", "Female", "2010-09-22", "Golden Savanna", "Elephant Plains", "Hay, fruit, vegetables", "Healthy", 1, "Participates in conservation talks."),
                ("Raya", "Giraffe", "Giraffa camelopardalis", "Female", "2017-04-05", "Golden Savanna", "Giraffe Terrace", "Leaves and pellets", "Healthy", 1, "Platform feeding ambassador."),
                ("Tangkad", "Giraffe", "Giraffa camelopardalis", "Male", "2016-06-16", "Golden Savanna", "Giraffe Terrace", "Leaves and pellets", "Healthy", 1, "Tallest animal in the sanctuary."),
                ("Kape", "Philippine Civet", "Paradoxurus philippinensis", "Male", "2021-02-03", "Night Safari", "Nocturnal House A", "Fruit and insects", "Healthy", 0, "Nocturnal viewing only."),
                ("Dilaw", "Philippine Civet", "Paradoxurus philippinensis", "Female", "2021-07-27", "Night Safari", "Nocturnal House A", "Fruit and insects", "Healthy", 0, "Nocturnal viewing only."),
                ("Aninag", "Tarsier", "Carlito syrichta", "Male", "2022-03-01", "Night Safari", "Tarsier Grove", "Insects", "Healthy", 0, "Low-light habitat; quiet zone."),
                ("Liwanag", "Tarsier", "Carlito syrichta", "Female", "2022-05-06", "Night Safari", "Tarsier Grove", "Insects", "Healthy", 0, "Low-light habitat; quiet zone."),
                ("Tagpi", "Zebra", "Equus quagga", "Male", "2018-12-19", "Golden Savanna", "Savanna Plains", "Grass and hay", "Healthy", 1, "Visible in safari circuit.")
            };

            int inserted = 0;
            ExecuteTransaction((conn, tx) =>
            {
                const string sql = @"
INSERT INTO tbl_animals
    (AnimalName, Species, ScientificName, Sex, BirthDate, ZoneName, EnclosureName, DietType, HealthStatus, IsEncounterEligible, Notes)
SELECT @name, @species, @scientificName, @sex, @birthDate, @zone, @enclosure, @diet, @health, @eligible, @notes
WHERE NOT EXISTS
(
    SELECT 1
    FROM tbl_animals
    WHERE AnimalName = @name
      AND Species = @species
);";

                foreach (var animal in animals)
                {
                    inserted += Execute(conn, tx, sql,
                        new MySqlParameter("@name", animal.Name),
                        new MySqlParameter("@species", animal.Species),
                        new MySqlParameter("@scientificName", animal.ScientificName),
                        new MySqlParameter("@sex", animal.Sex),
                        new MySqlParameter("@birthDate", animal.BirthDate),
                        new MySqlParameter("@zone", animal.Zone),
                        new MySqlParameter("@enclosure", animal.Enclosure),
                        new MySqlParameter("@diet", animal.Diet),
                        new MySqlParameter("@health", animal.Health),
                        new MySqlParameter("@eligible", animal.Eligible),
                        new MySqlParameter("@notes", animal.Notes));
                }
            });

            return inserted;
        }

        internal static int EnsureTourGuideSchedules()
        {
            if (!TableExists("tbl_tourschedules") || !TableExists("tbl_bookingexperiences") ||
                !TableExists("tbl_reservations") || !TableExists("tbl_experiences"))
            {
                return 0;
            }

            var pending = GetTable(@"
SELECT be.ReservationID,
       be.ExperienceID,
       COALESCE(r.VisitDate, r.CheckInDate) AS TourDate,
       COALESCE(e.DurationMinutes, 60) AS DurationMinutes
FROM tbl_bookingexperiences be
JOIN tbl_reservations r ON r.ReservationID = be.ReservationID
JOIN tbl_experiences e ON e.ExperienceID = be.ExperienceID
LEFT JOIN tbl_tourschedules ts
  ON ts.ReservationID = be.ReservationID
 AND ts.ExperienceID = be.ExperienceID
WHERE ts.TourScheduleID IS NULL
  AND COALESCE(r.Status, '') <> 'Cancelled'
  AND COALESCE(r.VisitDate, r.CheckInDate) IS NOT NULL
ORDER BY COALESCE(r.VisitDate, r.CheckInDate), be.ReservationID, be.ExperienceID;");

            if (pending.Rows.Count == 0)
                return 0;

            int inserted = 0;

            ExecuteTransaction((conn, tx) =>
            {
                const string insertSql = @"
INSERT INTO tbl_tourschedules
    (ReservationID, ExperienceID, GuideUserID, TourDate, StartTime, EndTime, Status, Notes, CreatedAt)
VALUES
    (@reservationId, @experienceId, @guideUserId, @tourDate, @startTime, @endTime, @status, @notes, NOW());";

                foreach (DataRow row in pending.Rows)
                {
                    if (row["TourDate"] == DBNull.Value)
                        continue;

                    int durationMinutes = row["DurationMinutes"] == DBNull.Value
                        ? 60
                        : Math.Max(30, Convert.ToInt32(row["DurationMinutes"]));

                    DateTime tourDate = Convert.ToDateTime(row["TourDate"]).Date;
                    int slot = inserted % 5;
                    TimeSpan startTime = new TimeSpan(9 + (slot * 2), slot % 2 == 0 ? 0 : 30, 0);
                    TimeSpan endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));
                    string status = "Open for Claim";
                    string notes = $"Auto-generated from booking {Convert.ToString(row["ReservationID"])} and waiting for guide claim.";

                    inserted += Execute(conn, tx, insertSql,
                        new MySqlParameter("@reservationId", Convert.ToString(row["ReservationID"]) ?? string.Empty),
                        new MySqlParameter("@experienceId", Convert.ToInt32(row["ExperienceID"])),
                        new MySqlParameter("@guideUserId", DBNull.Value),
                        new MySqlParameter("@tourDate", tourDate),
                        new MySqlParameter("@startTime", startTime),
                        new MySqlParameter("@endTime", endTime),
                        new MySqlParameter("@status", status),
                        new MySqlParameter("@notes", notes));
                }
            });

            return inserted;
        }
    }

    internal static class StaffPortalUi
    {
        internal enum MessageTone
        {
            Info,
            Success,
            Warning,
            Error
        }

        internal static UserControl BuildPage(string title, string subtitle, IEnumerable<Control> sections)
        {
            const int maxContentWidth = 1180;
            const int minContentWidth = 1040;
            const int sidePadding = 56;

            var host = new UserControl
            {
                Dock = DockStyle.Fill,
                BackColor = WildNestUI.Sand,
                AutoScroll = true
            };

            var scroll = WildNestUI.ScrollWrapper();
            scroll.Padding = new Padding(0, 28, 0, 42);

            int width = maxContentWidth;
            var flow = WildNestUI.FlowColumn(width);
            var header = WildNestUI.PageHeader(title, subtitle, width);
            flow.Controls.Add(header);

            foreach (var section in sections)
            {
                section.Width = width;
                flow.Controls.Add(section);
            }

            scroll.Controls.Add(flow);
            host.Controls.Add(scroll);

            void LayoutCenteredContent()
            {
                int available = Math.Max(scroll.ClientSize.Width - sidePadding * 2, minContentWidth);
                int newWidth = Math.Min(maxContentWidth, Math.Max(minContentWidth, available));
                int x = Math.Max(32, (scroll.ClientSize.Width - newWidth) / 2);

                flow.Width = newWidth;
                flow.Location = new Point(x, 0);

                foreach (Control c in flow.Controls)
                {
                    c.Width = newWidth;
                    c.PerformLayout();
                    c.Invalidate();
                }

                flow.PerformLayout();
            }

            host.Resize += (s, e) => LayoutCenteredContent();
            scroll.Resize += (s, e) => LayoutCenteredContent();
            host.HandleCreated += (s, e) => LayoutCenteredContent();

            return host;
        }

        internal static Form BuildEliteDialog(string title, string subtitle, Size size)
        {
            var dialog = new Form
            {
                Text = title,
                Size = size,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.None,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = Color.FromArgb(18, 18, 18),
                Font = WildNestUI.FontBody(9.5f),
                KeyPreview = true,
                Padding = new Padding(18)
            };

            dialog.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    dialog.Close();
            };

            var shell = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            dialog.Controls.Add(shell);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                WildNestUI.PaintSoftShadow(e.Graphics, new Rectangle(8, 8, Math.Max(0, card.Width - 20), Math.Max(0, card.Height - 20)), 16, 4);
                using var path = WildNestUI.RoundRect(new Rectangle(1, 1, Math.Max(0, card.Width - 3), Math.Max(0, card.Height - 3)), 18);
                using var fill = new SolidBrush(Color.White);
                using var border = new Pen(Color.FromArgb(219, 212, 201), 1f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            };
            shell.Controls.Add(card);

            void ApplyRoundedRegion()
            {
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, Math.Max(1, card.Width - 1), Math.Max(1, card.Height - 1)), 18);
                card.Region?.Dispose();
                card.Region = new Region(path);
            }

            card.Resize += (_, _) => ApplyRoundedRegion();
            dialog.Shown += (_, _) => ApplyRoundedRegion();

            var chrome = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.White
            };
            chrome.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(236, 231, 223), 1f);
                e.Graphics.DrawLine(pen, 0, chrome.Height - 1, chrome.Width, chrome.Height - 1);
            };
            card.Controls.Add(chrome);

            var chromeTitle = new Label
            {
                Text = "WILDNEST MANAGER",
                AutoSize = true,
                Location = new Point(18, 11),
                Font = WildNestUI.FontLabel(8.5f),
                ForeColor = WildNestUI.Muted,
                BackColor = Color.Transparent
            };
            chrome.Controls.Add(chromeTitle);

            var closeBtn = new Button
            {
                Text = "×",
                Size = new Size(34, 34),
                Location = new Point(Math.Max(0, size.Width - 88), 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = WildNestUI.TextDark,
                Font = new Font("Segoe UI", 14f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 239, 231);
            closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(234, 227, 217);
            closeBtn.Click += (_, _) => dialog.Close();
            chrome.Controls.Add(closeBtn);
            chrome.Resize += (_, _) => closeBtn.Location = new Point(chrome.Width - closeBtn.Width - 8, 2);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 118,
                BackColor = WildNestUI.Forest
            };
            header.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    header.ClientRectangle,
                    Color.FromArgb(8, 30, 16),
                    Color.FromArgb(19, 54, 33),
                    0f);
                e.Graphics.FillRectangle(brush, header.ClientRectangle);
                using var accent = new SolidBrush(Color.FromArgb(36, WildNestUI.Gold));
                e.Graphics.FillEllipse(accent, -42, -28, 132, 132);
                e.Graphics.FillEllipse(accent, header.Width - 96, header.Height - 56, 140, 140);
                using var pen = new Pen(Color.FromArgb(58, WildNestUI.Gold), 1f);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            card.Controls.Add(header);

            var body = new Panel
            {
                Name = "EliteDialogBody",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(249, 247, 243),
                Padding = new Padding(26, 22, 26, 24)
            };
            card.Controls.Add(body);

            header.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(24, 24),
                Font = WildNestUI.FontTitle(17f),
                ForeColor = WildNestUI.Cream,
                BackColor = Color.Transparent
            });

            header.Controls.Add(new Label
            {
                Text = subtitle,
                AutoSize = false,
                Location = new Point(26, 60),
                Size = new Size(Math.Max(160, size.Width - 116), 40),
                Font = WildNestUI.FontBody(10f),
                ForeColor = Color.FromArgb(214, WildNestUI.Cream),
                BackColor = Color.Transparent
            });

            return dialog;
        }

        internal static DialogResult ShowEliteMessage(
            IWin32Window? owner,
            string title,
            string message,
            MessageTone tone = MessageTone.Info,
            string buttonText = "OK")
        {
            Color accent = tone switch
            {
                MessageTone.Success => WildNestUI.Green,
                MessageTone.Warning => WildNestUI.Amber,
                MessageTone.Error => WildNestUI.Red,
                _ => WildNestUI.Blue
            };

            string badge = tone switch
            {
                MessageTone.Success => "Success",
                MessageTone.Warning => "Attention",
                MessageTone.Error => "Action Needed",
                _ => "WildNest"
            };

            const int bodyWidth = 468;
            Size measured = TextRenderer.MeasureText(
                message ?? string.Empty,
                WildNestUI.FontBody(10f),
                new Size(bodyWidth, 0),
                TextFormatFlags.WordBreak | TextFormatFlags.Left);
            int messageHeight = Math.Max(96, measured.Height + 12);
            int dialogHeight = Math.Max(306, 176 + messageHeight + 72);

            using var dialog = BuildEliteDialog(title, "WildNest notification center", new Size(538, dialogHeight));
            var shell = dialog.Controls[0].Controls[0];
            var body = shell.Controls["EliteDialogBody"] as Panel ?? shell;
            body.Controls.Clear();
            body.Padding = new Padding(24, 20, 24, 22);

            var badgePanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(128, 30),
                BackColor = Color.FromArgb(18, accent)
            };
            badgePanel.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var path = WildNestUI.RoundRect(new Rectangle(0, 0, badgePanel.Width - 1, badgePanel.Height - 1), 14);
                using var pen = new Pen(Color.FromArgb(84, accent), 1f);
                using var fill = new SolidBrush(Color.FromArgb(18, accent));
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            };
            body.Controls.Add(badgePanel);

            badgePanel.Controls.Add(new Label
            {
                Text = badge,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = WildNestUI.FontLabel(8.5f),
                ForeColor = accent,
                BackColor = Color.Transparent
            });

            var messageLabel = new Label
            {
                Text = message,
                Location = new Point(0, 50),
                Size = new Size(bodyWidth, messageHeight),
                Font = WildNestUI.FontBody(10f),
                ForeColor = WildNestUI.TextDark,
                BackColor = Color.Transparent
            };
            body.Controls.Add(messageLabel);

            var divider = new Panel
            {
                Height = 1,
                Width = bodyWidth,
                Location = new Point(0, messageLabel.Bottom + 12),
                BackColor = Color.FromArgb(233, 229, 221)
            };
            body.Controls.Add(divider);

            var btn = WildNestUI.BtnPrimary(buttonText, 124, 36);
            btn.BackColor = accent;
            btn.ForeColor = Color.White;
            btn.Location = new Point(bodyWidth - btn.Width, divider.Bottom + 18);
            btn.DialogResult = DialogResult.OK;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(accent);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.DarkDark(accent);
            body.Controls.Add(btn);

            dialog.AcceptButton = btn;
            return owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        }

        internal static Panel StatsRow(params (string Value, string Label, Color Color)[] stats)
        {
            int width = 1100;
            var row = new Panel
            {
                Height = 96,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 18)
            };

            void Layout()
            {
                row.Controls.Clear();
                if (stats.Length == 0)
                    return;

                int cardGap = 12;
                int cardWidth = Math.Max((row.Width - ((stats.Length - 1) * cardGap)) / stats.Length, 150);
                for (int i = 0; i < stats.Length; i++)
                {
                    var stat = stats[i];
                    var card = WildNestUI.StatCard(stat.Value, stat.Label, stat.Color, cardWidth);
                    card.Location = new Point(i * (cardWidth + cardGap), 4);
                    row.Controls.Add(card);
                }
            }

            row.Width = width;
            row.Resize += (s, e) => Layout();
            Layout();
            return row;
        }

        internal static Panel GridCard(string title, DataTable table, string emptyMessage = "No records found for this view.")
        {
            int width = 1100;
            int height = table.Rows.Count == 0 ? 180 : Math.Min(460, 120 + (table.Rows.Count * 28));
            var card = WildNestUI.CardWithHeader(width, height, title, 42);

            if (table.Rows.Count == 0)
            {
                card.Controls.Add(new Label
                {
                    Text = emptyMessage,
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.Muted,
                    AutoSize = true,
                    Location = new Point(18, 74),
                    BackColor = Color.Transparent
                });
                return card;
            }

            var grid = CreateGrid(table);
            grid.Location = new Point(14, 54);
            grid.Size = new Size(card.Width - 28, card.Height - 68);
            card.Controls.Add(grid);
            card.Resize += (s, e) => grid.Size = new Size(card.Width - 28, card.Height - 68);

            var exportBtn = new Button
            {
                Text = "Export CSV",
                Font = WildNestUI.FontLabel(8f),
                ForeColor = WildNestUI.TextDark,
                BackColor = WildNestUI.Amber,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(104, 28),
                Cursor = Cursors.Hand
            };
            exportBtn.FlatAppearance.BorderSize = 0;
            exportBtn.Location = new Point(card.Width - exportBtn.Width - 16, 9);
            exportBtn.Click += (s, e) => ExportTable(table, title);
            card.Controls.Add(exportBtn);
            exportBtn.BringToFront();
            card.Resize += (s, e) => exportBtn.Location = new Point(card.Width - exportBtn.Width - 16, 9);

            return card;
        }

        internal static Panel MessageCard(string title, string message, bool alert = false)
        {
            var card = WildNestUI.CardWithHeader(1100, 150, title, 42);
            var label = new Label
            {
                Text = message,
                Font = WildNestUI.FontBody(10f),
                ForeColor = alert ? WildNestUI.Red : WildNestUI.TextDark,
                Location = new Point(18, 62),
                Size = new Size(card.Width - 36, 70),
                BackColor = Color.Transparent
            };
            card.Controls.Add(label);
            card.Resize += (s, e) => label.Size = new Size(card.Width - 36, 70);
            return card;
        }

        internal static Panel AlertBanner(string message, bool alert = false)
        {
            return MessageCard(alert ? "Connection Alert" : "System Status", message, alert);
        }

        internal static Panel MetricTableCard(string title, params (string Label, string Value)[] metrics)
        {
            int height = Math.Max(110, 58 + (metrics.Length * 28));
            var card = WildNestUI.CardWithHeader(1100, height, title, 42);

            for (int i = 0; i < metrics.Length; i++)
            {
                int y = 58 + (i * 26);
                var metric = metrics[i];

                card.Controls.Add(new Label
                {
                    Text = metric.Label,
                    Font = WildNestUI.FontBody(9.5f),
                    ForeColor = WildNestUI.Muted,
                    AutoSize = true,
                    Location = new Point(18, y),
                    BackColor = Color.Transparent
                });

                var value = new Label
                {
                    Text = metric.Value,
                    Font = new Font(WildNestUI.FontBody(9.5f), FontStyle.Bold),
                    ForeColor = WildNestUI.TextDark,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                card.Controls.Add(value);
                value.Location = new Point(card.Width - value.PreferredWidth - 18, y);
                card.Resize += (s, e) => value.Location = new Point(card.Width - value.PreferredWidth - 18, y);
            }

            return card;
        }

        internal static Panel ActionCard(string title, Action<Panel> builder, int height = 180)
        {
            var card = WildNestUI.CardWithHeader(1100, height, title, 42);
            builder(card);
            return card;
        }

        internal static Panel TrendCard(string title, IEnumerable<(string Label, decimal Value, string Display, Color Color)> points, string emptyMessage = "No trend data available yet.")
        {
            var data = points?.ToList() ?? new List<(string Label, decimal Value, string Display, Color Color)>();
            int height = data.Count == 0 ? 180 : Math.Max(190, 76 + (data.Count * 46));
            var card = WildNestUI.CardWithHeader(1100, height, title, 42);

            if (data.Count == 0)
            {
                card.Controls.Add(new Label
                {
                    Text = emptyMessage,
                    Font = WildNestUI.FontBody(10f),
                    ForeColor = WildNestUI.Muted,
                    AutoSize = true,
                    Location = new Point(18, 74),
                    BackColor = Color.Transparent
                });
                return card;
            }

            decimal maxValue = Math.Max(1m, data.Max(p => p.Value));

            for (int i = 0; i < data.Count; i++)
            {
                var point = data[i];
                int y = 58 + (i * 44);

                var row = new Panel
                {
                    Location = new Point(18, y),
                    Size = new Size(card.Width - 36, 38),
                    BackColor = Color.Transparent
                };
                card.Controls.Add(row);

                var label = new Label
                {
                    Text = point.Label,
                    Font = WildNestUI.FontBody(9.2f),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.Transparent,
                    AutoEllipsis = true,
                    Location = new Point(0, 8),
                    Size = new Size(170, 22)
                };
                row.Controls.Add(label);

                var value = new Label
                {
                    Text = point.Display,
                    Font = new Font(WildNestUI.FontBody(8.8f), FontStyle.Bold),
                    ForeColor = WildNestUI.TextDark,
                    BackColor = Color.FromArgb(248, 246, 242),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(132, 26)
                };
                row.Controls.Add(value);

                var barBack = new Panel
                {
                    BackColor = Color.FromArgb(232, 235, 228),
                    Height = 12
                };
                row.Controls.Add(barBack);

                var barFill = new Panel
                {
                    BackColor = point.Color,
                    Location = new Point(0, 0),
                    Height = 12
                };
                barBack.Controls.Add(barFill);

                void layoutRow()
                {
                    row.Width = Math.Max(500, card.Width - 36);
                    value.Location = new Point(row.Width - value.Width, 6);

                    int barX = label.Right + 18;
                    int barW = Math.Max(160, value.Left - barX - 18);
                    barBack.Location = new Point(barX, 13);
                    barBack.Size = new Size(barW, 12);

                    int fillWidth = Math.Max(point.Value <= 0 ? 0 : 8,
                        (int)Math.Round((double)(point.Value / maxValue) * barW));
                    barFill.Size = new Size(Math.Min(fillWidth, barW), 12);
                }

                layoutRow();
                card.Resize += (s, e) => layoutRow();
            }

            return card;
        }

        internal static DataGridView CreateGrid(DataTable table)
        {
            var grid = new DataGridView
            {
                DataSource = table,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = WildNestUI.Muted;
            grid.ColumnHeadersDefaultCellStyle.Font = WildNestUI.FontLabel(8f);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 248, 248);
            grid.ColumnHeadersHeight = 36;

            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = WildNestUI.TextDark;
            grid.DefaultCellStyle.Font = WildNestUI.FontBody(9f);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(236, 242, 236);
            grid.DefaultCellStyle.SelectionForeColor = WildNestUI.TextDark;
            grid.GridColor = Color.FromArgb(238, 238, 238);
            grid.RowTemplate.Height = 30;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase) ||
                    column.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                    column.Name.Contains("Revenue", StringComparison.OrdinalIgnoreCase))
                {
                    column.DefaultCellStyle.Format = "N2";
                }
            }

            return grid;
        }

        internal static string Peso(decimal amount) => $"PHP {amount:N2}";

        internal static string SafeString(object? value, string fallback = "N/A")
        {
            if (value == null || value == DBNull.Value)
                return fallback;
            var text = Convert.ToString(value)?.Trim();
            return string.IsNullOrEmpty(text) ? fallback : text;
        }

        internal static async Task ExportExecutiveReportPdfAsync(
            string title,
            string subtitle,
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export executive report to PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                FileName = $"{SanitizeFileName(title)}_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                await ExportExecutiveReportPdfInternalAsync(dialog.FileName, title, subtitle, metrics, sections);
                ShowEliteMessage(null, "Executive Report Export",
                    "PDF report generated successfully.",
                    MessageTone.Success);
                ProjectDiagnostics.LogInfo("ExecutiveReportExport", $"Exported PDF report to {dialog.FileName}.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("ExecutiveReportExport", ex, "Failed exporting executive PDF report");
                ShowEliteMessage(null, "Executive Report Export",
                    "PDF export failed: " + ex.Message,
                    MessageTone.Error);
            }
        }

        internal static void ExportExecutiveReportRtf(
            string title,
            string subtitle,
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export executive report to Word-compatible format",
                Filter = "Rich Text Format (*.rtf)|*.rtf",
                FileName = $"{SanitizeFileName(title)}_{DateTime.Now:yyyyMMdd_HHmm}.rtf"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                File.WriteAllText(dialog.FileName, BuildExecutiveReportRtf(title, subtitle, metrics, sections), Encoding.UTF8);
                ShowEliteMessage(null, "Executive Report Export",
                    "Word-compatible report generated successfully.",
                    MessageTone.Success);
                ProjectDiagnostics.LogInfo("ExecutiveReportExport", $"Exported RTF report to {dialog.FileName}.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("ExecutiveReportExport", ex, "Failed exporting executive RTF report");
                ShowEliteMessage(null, "Executive Report Export",
                    "Word export failed: " + ex.Message,
                    MessageTone.Error);
            }
        }

        private static void ExportTable(DataTable table, string title)
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Export analytics data",
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"{SanitizeFileName(title)}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                File.WriteAllText(dialog.FileName, ToCsv(table), Encoding.UTF8);
                ShowEliteMessage(null, "Analytics Export",
                    "Analytics export completed successfully.",
                    MessageTone.Success);
                ProjectDiagnostics.LogInfo("AnalyticsExport", $"Exported {title} to {dialog.FileName}.");
            }
            catch (Exception ex)
            {
                ProjectDiagnostics.LogError("AnalyticsExport", ex, $"Failed exporting {title}");
                ShowEliteMessage(null, "Analytics Export",
                    "Export failed: " + ex.Message,
                    MessageTone.Error);
            }
        }

        private static string ToCsv(DataTable table)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => CsvEscape(c.ColumnName))));

            foreach (DataRow row in table.Rows)
            {
                sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>()
                    .Select(c => CsvEscape(Convert.ToString(row[c]) ?? string.Empty))));
            }

            return sb.ToString();
        }

        private static string CsvEscape(string value)
        {
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static async Task ExportExecutiveReportPdfInternalAsync(
            string fileName,
            string title,
            string subtitle,
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            string html = BuildExecutiveReportHtml(title, subtitle, metrics, sections);
            var completion = new TaskCompletionSource<bool>();

            using var host = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                Size = new Size(1280, 1600),
                FormBorderStyle = FormBorderStyle.None,
                Opacity = 0
            };

            using var web = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            host.Controls.Add(web);

            host.Shown += async (_, _) =>
            {
                try
                {
                    await web.EnsureCoreWebView2Async();
                    web.CoreWebView2.NavigationCompleted += async (s, e) =>
                    {
                        if (!e.IsSuccess)
                        {
                            completion.TrySetException(new InvalidOperationException("The report view could not finish loading for PDF export."));
                            host.Close();
                            return;
                        }

                        try
                        {
                            await Task.Delay(250);
                            CoreWebView2PrintSettings settings = web.CoreWebView2.Environment.CreatePrintSettings();
                            settings.ShouldPrintBackgrounds = true;
                            settings.ShouldPrintHeaderAndFooter = false;
                            settings.Orientation = CoreWebView2PrintOrientation.Portrait;
                            bool printed = await web.CoreWebView2.PrintToPdfAsync(fileName, settings);
                            if (!printed)
                                completion.TrySetException(new InvalidOperationException("The PDF engine did not confirm the report export."));
                            else
                                completion.TrySetResult(true);
                        }
                        catch (Exception ex)
                        {
                            completion.TrySetException(ex);
                        }
                        finally
                        {
                            host.Close();
                        }
                    };

                    web.NavigateToString(html);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                    host.Close();
                }
            };

            host.Show();
            await completion.Task;
        }

        private static string BuildExecutiveReportHtml(
            string title,
            string subtitle,
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            var sb = new StringBuilder();
            sb.Append("""
<!doctype html>
<html>
<head>
<meta charset="utf-8">
<style>
body{font-family:'Segoe UI',Arial,sans-serif;background:#f5f1e9;color:#1c1c1c;margin:0;padding:36px;}
.shell{max-width:1060px;margin:0 auto;background:#fffdf8;border:1px solid #ddd4c5;border-radius:20px;overflow:hidden;}
.hero{background:linear-gradient(135deg,#071a0e,#17452b);color:#f8f4ef;padding:28px 34px 26px;position:relative;}
.hero:after{content:'';position:absolute;right:-42px;bottom:-42px;width:160px;height:160px;border-radius:50%;background:rgba(212,160,23,.13);}
.eyebrow{display:inline-block;padding:8px 14px;border-radius:999px;background:rgba(212,160,23,.14);color:#d4a017;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;}
h1{margin:18px 0 8px;font-family:Georgia,serif;font-size:30px;line-height:1.15;}
.subtitle{max-width:760px;font-size:14px;line-height:1.55;color:#e2dbd0;}
.section{padding:24px 34px;}
.metrics{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px 22px;}
.metric{padding:14px 16px;border:1px solid #eadfcd;border-radius:14px;background:#fbf8f1;}
.metric .label{font-size:11px;text-transform:uppercase;letter-spacing:.08em;color:#6f665d;font-weight:700;margin-bottom:6px;}
.metric .value{font-size:18px;font-weight:700;color:#102315;}
.report-title{font-family:Georgia,serif;font-size:22px;margin:0 0 14px;color:#102315;}
.table-card{margin-top:18px;border:1px solid #e4dac8;border-radius:16px;overflow:hidden;}
.table-head{padding:14px 18px;background:#faf5ec;font-size:15px;font-weight:700;color:#102315;}
table{width:100%;border-collapse:collapse;font-size:12px;}
th,td{padding:10px 12px;border-bottom:1px solid #efe7d8;text-align:left;vertical-align:top;}
th{background:#fffdf8;color:#6f665d;text-transform:uppercase;font-size:10px;letter-spacing:.08em;}
tr:nth-child(even) td{background:#fcfaf5;}
.empty{padding:16px 18px;color:#7b736b;font-style:italic;}
</style>
</head>
<body>
<div class="shell">
<div class="hero">
<div class="eyebrow">WildNest Manager Report</div>
""");
            sb.Append("<h1>").Append(Html(title)).Append("</h1>");
            sb.Append("<div class=\"subtitle\">").Append(Html(subtitle)).Append("</div></div>");
            sb.Append("<div class=\"section\"><h2 class=\"report-title\">Executive Snapshot</h2><div class=\"metrics\">");

            foreach (var (label, value) in metrics)
            {
                sb.Append("<div class=\"metric\"><div class=\"label\">")
                  .Append(Html(label))
                  .Append("</div><div class=\"value\">")
                  .Append(Html(value))
                  .Append("</div></div>");
            }

            sb.Append("</div></div>");

            foreach (var (sectionTitle, table) in sections)
            {
                sb.Append("<div class=\"section\"><div class=\"table-card\"><div class=\"table-head\">")
                  .Append(Html(sectionTitle))
                  .Append("</div>");

                if (table.Rows.Count == 0)
                {
                    sb.Append("<div class=\"empty\">No records available for this section.</div></div></div>");
                    continue;
                }

                sb.Append("<table><thead><tr>");
                foreach (DataColumn column in table.Columns)
                    sb.Append("<th>").Append(Html(column.ColumnName)).Append("</th>");
                sb.Append("</tr></thead><tbody>");

                foreach (DataRow row in table.Rows)
                {
                    sb.Append("<tr>");
                    foreach (DataColumn column in table.Columns)
                        sb.Append("<td>").Append(Html(Convert.ToString(row[column]) ?? string.Empty)).Append("</td>");
                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table></div></div>");
            }

            sb.Append("</div></body></html>");
            return sb.ToString();
        }

        private static string BuildExecutiveReportRtf(
            string title,
            string subtitle,
            IEnumerable<(string Label, string Value)> metrics,
            IEnumerable<(string Title, DataTable Table)> sections)
        {
            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0");
            sb.Append(@"{\fonttbl{\f0 Segoe UI;}{\f1 Georgia;}}");
            sb.Append(@"\margl1440\margr1440\margt1080\margb1080");
            sb.Append(@"\f1\fs36\b ").Append(Rtf(title)).Append(@"\b0\par");
            sb.Append(@"\f0\fs20 ").Append(Rtf(subtitle)).Append(@"\par\par");
            sb.Append(@"\f1\fs28\b Executive Snapshot\b0\par");

            foreach (var (label, value) in metrics)
            {
                sb.Append(@"\f0\fs20\b ").Append(Rtf(label)).Append(@":\b0 ").Append(Rtf(value)).Append(@"\par");
            }

            foreach (var (sectionTitle, table) in sections)
            {
                sb.Append(@"\par\f1\fs28\b ").Append(Rtf(sectionTitle)).Append(@"\b0\par");
                if (table.Rows.Count == 0)
                {
                    sb.Append(@"\f0\fs20 No records available for this section.\par");
                    continue;
                }

                sb.Append(@"\f0\fs18\b ");
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (i > 0) sb.Append(@" \tab ");
                    sb.Append(Rtf(table.Columns[i].ColumnName));
                }
                sb.Append(@"\b0\par");

                foreach (DataRow row in table.Rows)
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        if (i > 0) sb.Append(@" \tab ");
                        sb.Append(Rtf(Convert.ToString(row[i]) ?? string.Empty));
                    }
                    sb.Append(@"\par");
                }
            }

            sb.Append("}");
            return sb.ToString();
        }

        private static string Html(string value) => WebUtility.HtmlEncode(value);

        private static string Rtf(string value)
        {
            return value
                .Replace(@"\", @"\\")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                .Replace("\r\n", @"\par ")
                .Replace("\n", @"\par ");
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }
    }
}
