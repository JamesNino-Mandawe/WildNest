# WildNest Prompt 1-4 Handoff Report

Date: 2026-04-27  
Project: `Project`  
App: WildNest WinForms + MySQL

This report is for transferring to a new chat without losing context.

## Project Baseline

WildNest is a C# WinForms desktop application with MySQL connectivity for:

- public booking flow
- guest portal access
- QR and email confirmation
- role-based staff portals
- animal records and zoo operations
- analytics and reports
- guest chat and staff chat

Current operational staff roles:

- `Manager` (refactored from Administrator concept)
- `Reception`
- `ZooKeeper`
- `TourGuide`

## Prompt 1: Manager Portal Refactor

### Goal

Refactor the old `Administrator` operational role into a stronger `Manager` role that:

- supervises operations
- manages staff accounts
- owns reports and analytics
- better matches real resort workflow

### Decisions already made

- Do **not** blindly delete old Administrator compatibility in code/database.
- Old `Administrator` records should still map safely to `Manager`.
- Present `Manager` as the real active operational control role.

### What was integrated conceptually

- Staff login and staff portal now recognize `Manager`.
- `Manager` is intended to replace operational `Administrator` labeling.
- Existing good admin modules should be reused instead of destroyed:
  - cabins
  - reservations
  - guest profiles
  - animal registry
  - billing/reports
  - search
  - staff chat

### Important files

- `StaffLogin.cs`
- `StaffDashboard.cs`
- `StaffPortal\\UcManager\\UcManagerDashboard.cs`
- `StaffPortal\\UcManager\\UcManagerDashboardContent.cs`
- `StaffPortal\\UcManager\\UcManagerUsers.cs`
- `StaffPortal\\UcManager\\UcManagerBills.cs`
- `StaffPortal\\UcManager\\UcManagerSalesReports.cs`
- old reusable admin modules under `StaffPortal\\UcAdministrator`

### Real `tbl_users` schema

- `UserID`
- `FullName`
- `Username`
- `PasswordHash`
- `Role`
- `ContactNo`
- `IsActive`

### Prompt 1 current unresolved issue

The biggest unresolved UI issue is the Manager account dialogs:

- `Create Staff Account`
- `Toggle Account Status`
- `Reset Staff Password`

Problems seen:

- clipped labels
- cropped helper text
- bad modal spacing
- black outer frame
- nested modal behavior
- not yet truly elite/professional visually

### Prompt 1 current logic expectations

These dialogs must remain wired to:

- create account -> insert into `tbl_users`
- toggle active/inactive -> update `IsActive`
- reset password -> update `PasswordHash`

Created roles must still route correctly on login to:

- Reception portal
- ZooKeeper portal
- TourGuide portal

## Prompt 2: Camera / QR / Smart Verification

### Goal

Elevate QR from a common class feature into a premium smart resort pass system.

### Intended direction

QR should become more than “scan to open booking”.

Desired smart behavior:

- reception can scan QR for check-in
- guest can scan QR to open a premium QR landing / portal pass
- same QR can show booking validity state
- show better anti-fraud / verification logic

### Best high-level concept already agreed

Turn it into a **WildNest Smart QR Pass**:

- booking ID + QR
- premium scan result
- validity state
- downloadable proof
- guest-facing pass view
- reception verification use

### Strong suggested states

- `Valid Today`
- `Upcoming`
- `Checked-In`
- `Completed`
- `Cancelled`
- `Requires Reception Review`

### Files likely involved

- `BookNow` booking files
- QR generation files
- `EmailService.cs`
- portal launch/open files
- guest portal HTML/webview files
- reception check-in files

### Current state

- QR and email confirmation exist
- guest portal QR flow exists
- reception QR/camera premium flow is still a desired upgrade area
- no final premium camera module integration was completed in this chat

## Prompt 3: Guest Portal Premium vs Limited

### Goal

Create a stronger difference between:

- limited guest lookup access
- richer premium portal access

### Agreed direction

Guests without richer portal-level access should see:

- basic booking lookup
- minimal reservation proof
- QR / confirmation essentials

Guests with recognized/verified richer access should see:

- better reservation dashboard
- downloadable proof / materials
- more premium booking insight
- more polished personalized presentation

### Files identified for this area

- `Accomodations\\wildnest_portal.html`
- `Accomodations\\GuestPortalWebView.cs`
- `Accomodations\\GuestPortalPanel.cs`
- `Accomodations\\MyAccomodation.cs`

### Current state

- guest portal exists
- HTML + WebView2 are already used in parts of the project
- this prompt was not yet fully implemented in this chat
- future work should preserve:
  - booking reference lookup
  - QR access
  - DB validation
  - email-linked reservation flow

### Important note

Prompt 3 is the most likely feature to return **HTML + integration instructions**, not only WinForms code.

## Prompt 4: Staff Chat 1-to-1 for All Staff

### Goal

Upgrade internal staff chat from a limited hierarchy into stronger 1-to-1 internal messaging between:

- Manager
- Reception
- ZooKeeper
- TourGuide

while keeping:

- guest-to-reception chat separate
- DB integrity intact

### Current reality

Reception guest chat was heavily worked on and improved.

Internal staff chat still needs continued polishing for:

- visibility
- message clipping
- first-message overlap under headers
- consistent layout across all staff roles

### Files involved

- `StaffPortal\\UcStaffChat.cs`
- `StaffPortal\\UcReception\\UcReceptionChat.cs`
- dashboard files for Reception / ZooKeeper / TourGuide / Manager

### Desired end state

- all staff roles can directly message one another
- clear sender/receiver labels
- professional conversation layout
- unread notification support
- no clipped bubbles or hidden first messages

### Current state

- chat exists
- reception guest chat was made more functional
- staff chat needs further polish and consistency if future chat continues

## Database Context

Known core tables in use:

- `tbl_users`
- `tbl_guests`
- `tbl_reservations`
- `tbl_payments`
- `tbl_cabins`
- `tbl_experiences`
- `tbl_bookingexperiences`
- `tbl_chat`
- `tbl_staffmessages`
- `tbl_animals`
- `tbl_healthrecords`
- `tbl_feedings`
- `tbl_tourschedules`
- `tbl_tourcompletions`

### Important operational logic already clarified

#### Reservation / guest identity

- Same guest can reuse the same guest identity/profile
- Each new booking must create a **new ReservationID**
- New reservation -> new QR and new email confirmation
- Old reservation remains as historical data

#### CRUD explanation

- Create -> bookings, records, schedules, chats
- Read -> dashboards, search, reports, portals
- Update -> statuses, health, feeding, check-in/out, completion, passwords, active flags
- Delete -> usually soft logic like cancel/deactivate, not unsafe hard delete

## ZooKeeper Logic Already Established

### Health Record vs Flag Health Alert vs Clear Animal

- `Health Records`
  - detailed medical/care documentation
- `Flag Health Alert`
  - active warning/restriction
- `Clear Animal`
  - remove active restriction once recovered

### Feeding Schedule logic

Typical status usage:

- `Scheduled`
- `Completed`
- `Missed`
- `Delayed`

### Animal ID guidance

Animal ID should be visible beside animals in registry/list views so ZooKeeper knows what to encode in:

- health records
- flag alerts
- clear animal
- feeding schedule

### Important note

Same species does **not** share the same AnimalID.  
Each animal is a different individual record.

## TourGuide Logic Already Established

### Events / schedules

TourGuide “events” are based on `tbl_tourschedules`.

They only appear when there is a proper chain:

- reservation exists
- booked experience exists
- guide user exists
- tour schedule is assigned

### TourGuide sections

- `My Schedule`
- `My Tour Groups`
- `Mark Complete`
- `Tour History`

### Key ID

`TourScheduleID` is the operational ID the guide uses to complete a tour.

### Expected behavior

Once a guest books an experience and it is assigned/scheduled correctly, the TourGuide should see it.

## Search Feature Context

There is a global/general admin-style search concept already discussed.

Correct wording now:

- `Global Manager Search`
- `General Manager Search`

It searches **database records**, not arbitrary code.

Targets may include:

- reservations
- guests
- cabins
- animals
- health records
- feeding records
- experiences
- payments
- tours
- users
- guest chat
- staff chat

## Current Biggest UI Pain Point

The **Manager modals** are still the biggest visual issue:

- `Create Staff Account`
- `Toggle Account Status`
- `Reset Staff Password`

The user specifically wants:

- no clipped text
- no labels hitting container edges
- no bad scroll behavior
- no black outline
- no nested popup on popup
- premium, top-tier, manager-grade look
- proper positions
- all textboxes visible
- logo can be placed tastefully

### Strong recommendation for future chat

Do not keep doing tiny patch fixes.

Instead, future chat should:

- rebuild the 3 manager dialogs as stable, fixed-size premium modals
- remove nested alert modal behavior for dialog validation
- prefer inline validation/status labels inside the modal
- eliminate black frame in shared elite dialog shell

## Important File Paths

Manager portal:

- `StaffPortal\\UcManager\\UcManagerUsers.cs`
- `StaffPortal\\UcManager\\UcManagerDashboard.cs`
- `StaffPortal\\UcManager\\UcManagerDashboardContent.cs`
- `StaffPortal\\UcManager\\UcManagerSalesReports.cs`
- `StaffPortal\\UcManager\\UcManagerBills.cs`

Shared dialog support:

- `StaffPortal\\StaffPortalSupport.cs`

Guest portal:

- `Accomodations\\wildnest_portal.html`
- `Accomodations\\GuestPortalWebView.cs`
- `Accomodations\\GuestPortalPanel.cs`
- `Accomodations\\MyAccomodation.cs`

Chat:

- `StaffPortal\\UcStaffChat.cs`
- `StaffPortal\\UcReception\\UcReceptionChat.cs`

Public UI:

- `HomePage.cs`
- splash / html files in project root

## What Future Chat Should Know Immediately

1. Prompt 1 is partially integrated but Manager modal UI is still not acceptable.
2. Prompt 2 and Prompt 3 are still mostly strategic directions, not fully integrated.
3. Prompt 4 exists conceptually and partly in code, but full cross-staff premium chat still needs further polish.
4. The user is highly sensitive to:
   - clipping
   - overlap
   - labels being cut
   - default WinForms look
   - inconsistent premium quality
5. The project must remain:
   - DB-accurate
   - role-routed correctly
   - demo-ready
   - presentation-grade visually

## Short Continuity Message for a New Chat

Use this in a fresh chat:

> We are continuing work on WildNest, a WinForms + MySQL zoo resort system. Prompt 1 partially refactored Administrator into Manager, but the Manager account modals are still visually broken and need a full premium rebuild without clipping, black frames, or nested popups. Prompt 2 is for QR/camera smart verification, Prompt 3 is for premium vs limited guest portal access, and Prompt 4 is for 1-to-1 staff chat among Manager, Reception, ZooKeeper, and TourGuide. Database wiring must remain accurate and existing working logic must not be broken.

