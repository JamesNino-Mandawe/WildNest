# WildNest — Manager Portal Refactor
## What changed and how to integrate

---

## Files delivered

| File | Action | Replaces |
|---|---|---|
| `StaffLogin.cs` | Replace | `StaffLogin.cs` |
| `StaffDashboard.cs` | Replace | `StaffDashboard.cs` |
| `UcManager/UcManagerDashboard.Designer.cs` | Replace | `UcAdminDashboard.Designer.cs` |
| `UcManager/UcManagerDashboard.cs` | Replace | `UcAdminDashboard.cs` |
| `UcManager/UcManagerDashboardContent.cs` | Replace | `UcAdminDashboardContent.cs` |
| `UcManager/UcManagerUsers.cs` | Replace | `UcAdminUsers.cs` |
| `UcManager/UcManagerBills.cs` | Replace | `UcAdminBills.cs` |
| `UcManager/UcManagerSalesReports.cs` | **New file** | (none) |
| `wildnest_manager_migration.sql` | **Optional** | (none) |

---

## Step-by-step integration

### 1. Rename the folder (optional but clean)
```
Project/UcAdministrator/  →  Project/UcManager/
```
In Visual Studio: right-click the folder → Rename.

### 2. Drop in the new files
Copy each file from this delivery into its target location.

### 3. Update namespace references in files you did NOT rename
The following files still live under `Project.UcAdministrator` and are
**not renamed** — the Manager portal simply calls them as-is:
- `UcAdminCabins`
- `UcAdminReservations`
- `UcAdminGuests`
- `UcAdminAnimals`
- `UcAdminEncounters`
- `UcAdminSmartSearch`

If you did rename the folder, add a global namespace alias or update
the `using` statements in `UcManagerDashboard.cs` where those classes
are instantiated.

### 4. Remove the old Administrator files
Delete (or archive):
- `UcAdminDashboard.cs` / `UcAdminDashboard.Designer.cs`
- `UcAdminDashboardContent.cs`
- `UcAdminUsers.cs`
- `UcAdminBills.cs`

### 5. Run the SQL (optional)
Run `wildnest_manager_migration.sql` against `wildnest_db` if you want
existing `Role = 'Administrator'` rows in `tbl_users` to show as
"Manager" in the grid. **The portal works without this** because
`CanonicalRole()` already maps both strings at login.

### 6. Build and test
- Log in with `manager` / `manager123` (demo) → Manager portal opens
- Log in with `maria` / `maria123` → Reception portal — unchanged
- Create a new Reception account from Staff Accounts → verify row in DB

---

## What changed per file

### StaffLogin.cs
- Role dropdown: `"Administrator"` → `"Manager"`
- `CanonicalRole()`: `"administrator"` / `"admin"` → `"Manager"`  
  (backward-compatible: old DB rows still log in)
- Demo account: `("admin","admin123","Administrator",…)` → `("manager","manager123","Manager",…)`
- `LaunchDashboard()` helper extracted (cosmetic cleanup)
- All paint helpers: **unchanged**

### StaffDashboard.cs
- `using Project.UcAdministrator` → `using Project.UcManager`
- `case "Administrator"` → `case "Manager"` in the role switch
- `btnRoleAdmin.Tag = "Manager"` (was `"Administrator"`)
- `btnRoleAdmin.Text = "Manager"`
- Top-bar context string uses `RoleLabel()` helper
- Everything else: **unchanged**

### UcManagerDashboard.Designer.cs
- Class and panel names renamed (`pnlAdminSidebar` → `pnlManagerSidebar`, etc.)
- `btnNavSalesReports` added as a new designer-declared button
- No layout logic changed

### UcManagerDashboard.cs
- `lblSideRole` displays `"Resort Manager"` instead of `"System Administrator"`
- `btnNavUsers` loads `UcManagerUsers` (has account creation)
- `btnNavBills` loads `UcManagerBills` (same data, new namespace)
- `btnNavSalesReports` **added** → loads `UcManagerSalesReports`
- Staff Chat wires `"Manager"` role string
- Sidebar text: `"👥  Staff Accounts"` (was `"👥  User Management"`)

### UcManagerDashboardContent.cs
- Page title: `"Manager Dashboard"`
- **New**: overdue-stay alert banner at top
- **New**: `todayRevenue` KPI replaces the old "Active Staff Users" card in StatsRow
- All SQL, trend charts, GridCards: **identical to original**

### UcManagerUsers.cs (was UcAdminUsers)
- All original read-only stats/grids preserved
- **New**: three action buttons above the page content
  - **Create Staff Account** — dialog with Full Name, Username, Password, Contact, Role (Reception/ZooKeeper/TourGuide only). Hashes password with SHA-256 matching `VerifyPassword()`. Checks username uniqueness before insert.
  - **Toggle Active/Inactive** — enter username, flips `IsActive`. Blocked for Manager/Administrator rows.
  - **Reset Password** — enter username + new password, updates `PasswordHash`. Blocked for Manager rows.

### UcManagerBills.cs (was UcAdminBills)
- Namespace and class renamed only. All SQL and UI: **identical**.

### UcManagerSalesReports.cs (NEW)
- **Daily** table: last 30 days, grouped by date
- **Weekly** table: last 13 weeks, grouped by ISO week
- **Monthly** table: last 12 months, grouped by month
- **Yearly** table: all-time, grouped by year
- Each table shows: Transactions, Revenue, Avg Transaction, Largest Payment
- Period **tab strip** inside a card — click to switch. Monthly is selected by default.
- **Revenue by Booking Type** grid
- **Revenue by Payment Method** grid
- KPI StatsRow: Today / This Week / This Month / This Year revenue + transaction counts
- Revenue Snapshot MetricTableCard with all eight values

---

## DB changes required

**None.** All new features read from existing tables:
- `tbl_payments` — all Sales Reports queries
- `tbl_reservations` — booking-type cross-tab
- `tbl_users` — account management (existing columns only)

The optional SQL renames `Role = 'Administrator'` rows to `'Manager'`
but is **not required** for the portal to function.
