using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace Project.Booking
{
    internal static class BookingPersistence
    {
        private static readonly Dictionary<string, string> CabinAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Canopy Tree House"] = "Treetop Treehouse",
            ["Treetop House"] = "Treetop Treehouse",
            ["Forest Cabin B"] = "Forest Cabin A",
            ["Savanna Tent"] = "Safari Tent Alpha",
            ["Safari Lodge Suite"] = "Lakeside Lodge",
            ["The Sanctuary Villa"] = "Sanctuary Villa"
        };

        private static readonly Dictionary<string, string> ExperienceAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Grand Safari Circuit"] = "Night Safari Walk",
            ["Wildlife Photo Opportunity"] = "Photo Encounter",
            ["Photography Pro"] = "Photo Encounter",
            ["Photography Pro Kit"] = "Photo Encounter",
            ["Animal Feeding"] = "Animal Feeding Tour",
            ["Night Safari"] = "Night Safari Walk"
        };

        internal static int GetOrCreateGuest(
            MySqlConnection conn,
            MySqlTransaction tx,
            string firstName,
            string lastName,
            string email,
            string phone = "",
            string nationality = "",
            string validIdType = "",
            string specialRequests = "")
        {
            using var findCmd = new MySqlCommand(@"
SELECT GuestID
FROM tbl_Guests
WHERE LOWER(Email) = LOWER(@email)
  AND FirstName = @firstName
  AND LastName = @lastName
ORDER BY GuestID DESC
LIMIT 1;", conn, tx);
            findCmd.Parameters.AddWithValue("@email", email.Trim());
            findCmd.Parameters.AddWithValue("@firstName", firstName.Trim());
            findCmd.Parameters.AddWithValue("@lastName", lastName.Trim());

            object? existing = findCmd.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
            {
                int guestId = Convert.ToInt32(existing);
                using var updateCmd = new MySqlCommand(@"
UPDATE tbl_Guests
SET Phone = CASE WHEN @phone <> '' THEN @phone ELSE Phone END,
    Nationality = CASE WHEN @nationality <> '' THEN @nationality ELSE Nationality END,
    ValidIDType = CASE WHEN @validIdType <> '' THEN @validIdType ELSE ValidIDType END,
    SpecialRequests = CASE WHEN @specialRequests <> '' THEN @specialRequests ELSE SpecialRequests END
WHERE GuestID = @guestId;", conn, tx);
                updateCmd.Parameters.AddWithValue("@phone", phone.Trim());
                updateCmd.Parameters.AddWithValue("@nationality", nationality.Trim());
                updateCmd.Parameters.AddWithValue("@validIdType", validIdType.Trim());
                updateCmd.Parameters.AddWithValue("@specialRequests", specialRequests.Trim());
                updateCmd.Parameters.AddWithValue("@guestId", guestId);
                updateCmd.ExecuteNonQuery();
                return guestId;
            }

            using var insertCmd = new MySqlCommand(@"
INSERT INTO tbl_Guests
    (FirstName, LastName, Email, Phone, Nationality, ValidIDType, SpecialRequests)
VALUES
    (@firstName, @lastName, @email, @phone, @nationality, @validIdType, @specialRequests);",
                conn, tx);
            insertCmd.Parameters.AddWithValue("@firstName", firstName.Trim());
            insertCmd.Parameters.AddWithValue("@lastName", lastName.Trim());
            insertCmd.Parameters.AddWithValue("@email", email.Trim());
            insertCmd.Parameters.AddWithValue("@phone", phone.Trim());
            insertCmd.Parameters.AddWithValue("@nationality", nationality.Trim());
            insertCmd.Parameters.AddWithValue("@validIdType", validIdType.Trim());
            insertCmd.Parameters.AddWithValue("@specialRequests", specialRequests.Trim());
            insertCmd.ExecuteNonQuery();

            using var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", conn, tx);
            return Convert.ToInt32(idCmd.ExecuteScalar());
        }

        internal static int ResolveCabinId(MySqlConnection conn, MySqlTransaction tx, string cabinName)
        {
            string rawName = (cabinName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawName))
                throw new InvalidOperationException("A cabin selection is required before booking.");

            string canonicalName = CabinAliases.TryGetValue(rawName, out string? alias)
                ? alias
                : rawName;

            int? cabinId = ResolveNamedRow(conn, tx, "tbl_Cabins", "CabinID", "CabinName", canonicalName, preferAvailableStatus: true);
            if (cabinId.HasValue)
                return cabinId.Value;

            cabinId = ResolveNamedRow(conn, tx, "tbl_Cabins", "CabinID", "CabinName", rawName, preferAvailableStatus: true);
            if (cabinId.HasValue)
                return cabinId.Value;

            string normalizedWanted = NormalizeLookupToken(canonicalName);
            using var fuzzyCmd = new MySqlCommand(@"
SELECT CabinID, CabinName
FROM tbl_Cabins
ORDER BY CASE WHEN Status = 'Available' THEN 0 ELSE 1 END, CabinID ASC;", conn, tx);
            using var reader = fuzzyCmd.ExecuteReader();
            while (reader.Read())
            {
                string dbName = Convert.ToString(reader["CabinName"]) ?? string.Empty;
                if (NormalizeLookupToken(dbName) == normalizedWanted)
                    return Convert.ToInt32(reader["CabinID"]);
            }

            throw new InvalidOperationException($"Cabin '{cabinName}' was not found in the database.");
        }

        internal static int? ResolveExperienceId(MySqlConnection conn, MySqlTransaction tx, string experienceName)
        {
            string rawName = (experienceName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rawName))
                return null;

            string canonicalName = ExperienceAliases.TryGetValue(rawName, out string? alias)
                ? alias
                : rawName;

            int? experienceId = ResolveNamedRow(conn, tx, "tbl_Experiences", "ExperienceID", "ExperienceName", canonicalName);
            if (experienceId.HasValue)
                return experienceId;

            experienceId = ResolveNamedRow(conn, tx, "tbl_Experiences", "ExperienceID", "ExperienceName", rawName);
            if (experienceId.HasValue)
                return experienceId;

            string normalizedWanted = NormalizeLookupToken(canonicalName);
            using var fuzzyCmd = new MySqlCommand(@"
SELECT ExperienceID, ExperienceName
FROM tbl_Experiences
ORDER BY ExperienceID ASC;", conn, tx);
            using var reader = fuzzyCmd.ExecuteReader();
            while (reader.Read())
            {
                string dbName = Convert.ToString(reader["ExperienceName"]) ?? string.Empty;
                string normalizedDb = NormalizeLookupToken(dbName);
                if (normalizedDb == normalizedWanted ||
                    normalizedDb.Contains(normalizedWanted, StringComparison.OrdinalIgnoreCase) ||
                    normalizedWanted.Contains(normalizedDb, StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(reader["ExperienceID"]);
            }

            return null;
        }

        internal static void InsertExperienceLink(
            MySqlConnection conn,
            MySqlTransaction tx,
            string reservationId,
            int experienceId,
            int quantity = 1)
        {
            using var linkCmd = new MySqlCommand(@"
INSERT INTO tbl_BookingExperiences (ReservationID, ExperienceID, Quantity, TotalCost)
VALUES (@reservationId, @experienceId, @quantity,
        (SELECT PricePerPerson FROM tbl_Experiences WHERE ExperienceID = @experienceId) * @quantity);",
                conn, tx);
            linkCmd.Parameters.AddWithValue("@reservationId", reservationId);
            linkCmd.Parameters.AddWithValue("@experienceId", experienceId);
            linkCmd.Parameters.AddWithValue("@quantity", quantity);
            linkCmd.ExecuteNonQuery();
        }

        internal static void InsertPayment(
            MySqlConnection conn,
            MySqlTransaction tx,
            string reservationId,
            decimal amount,
            string paymentMethod,
            string status = "Confirmed")
        {
            string normalizedMethod = NormalizePaymentMethod(paymentMethod);
            string effectiveStatus = string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                ? DeriveBookingPaymentStatus(normalizedMethod)
                : status;

            bool markPaidImmediately = ShouldMarkPaymentAsCollected(effectiveStatus);

            using var payCmd = new MySqlCommand(@"
INSERT INTO tbl_Payments (ReservationID, Amount, PaymentMethod, Status, PaidAt)
VALUES (@reservationId, @amount, @paymentMethod, @status,
        CASE WHEN @markPaid = 1 THEN NOW() ELSE NULL END);", conn, tx);
            payCmd.Parameters.AddWithValue("@reservationId", reservationId);
            payCmd.Parameters.AddWithValue("@amount", amount);
            payCmd.Parameters.AddWithValue("@paymentMethod", normalizedMethod);
            payCmd.Parameters.AddWithValue("@status", effectiveStatus);
            payCmd.Parameters.AddWithValue("@markPaid", markPaidImmediately ? 1 : 0);
            payCmd.ExecuteNonQuery();
        }

        private static string NormalizePaymentMethod(string paymentMethod)
        {
            string method = (paymentMethod ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(method) ? "Credit / Debit Card" : method;
        }

        private static string DeriveBookingPaymentStatus(string paymentMethod)
        {
            return paymentMethod switch
            {
                "Pay at Resort" => "Pay on Arrival",
                "GCash / Maya" => "Paid",
                "Bank Transfer" => "Paid",
                "Credit / Debit Card" => "Paid",
                _ => "Paid"
            };
        }

        private static bool ShouldMarkPaymentAsCollected(string status)
        {
            return status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Settled", StringComparison.OrdinalIgnoreCase);
        }

        private static int? ResolveNamedRow(
            MySqlConnection conn,
            MySqlTransaction tx,
            string tableName,
            string idColumn,
            string nameColumn,
            string wantedName,
            bool preferAvailableStatus = false)
        {
            string statusOrder = preferAvailableStatus ? "CASE WHEN Status = 'Available' THEN 0 ELSE 1 END," : string.Empty;
            using var cmd = new MySqlCommand($@"
SELECT {idColumn}
FROM {tableName}
WHERE LOWER(TRIM({nameColumn})) = LOWER(TRIM(@name))
ORDER BY {statusOrder} {idColumn} ASC
LIMIT 1;", conn, tx);
            cmd.Parameters.AddWithValue("@name", wantedName);
            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        private static string NormalizeLookupToken(string value)
        {
            return new string(value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }
    }
}
