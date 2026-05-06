-- =============================================================
--  wildnest_manager_migration.sql
--  OPTIONAL — only run this if you want to rename existing
--  tbl_users rows from Role = 'Administrator' to 'Manager'.
--
--  If you skip this script, the system still works correctly:
--  CanonicalRole() in StaffLogin.cs already maps
--  'Administrator' → 'Manager' at login time.
--
--  Run once against wildnest_db. Safe to run multiple times
--  (the WHERE clause prevents double-updates).
-- =============================================================

USE wildnest_db;

-- 1. Rename existing Administrator accounts to Manager
UPDATE tbl_users
SET    Role = 'Manager'
WHERE  Role IN ('Administrator', 'Admin', 'administrator', 'admin');

-- 2. Verify
SELECT UserID, FullName, Username, Role, IsActive
FROM   tbl_users
ORDER  BY Role, FullName;

-- =============================================================
--  NO other schema changes are required for the Manager portal.
--  All Sales Reports queries use tbl_payments + tbl_reservations
--  which already exist.  The new account-creation feature
--  writes to tbl_users using its existing columns:
--    FullName, Username, PasswordHash, Role, ContactNo,
--    IsActive, CreatedAt
--  No new tables or columns needed.
-- =============================================================
