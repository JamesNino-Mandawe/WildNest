# WildNest Current Codebase Report

Date: April 19, 2026  
Project: WildNest Zoo Resort & Wildlife Experience  
Tech Stack: C# WinForms, .NET 8 Windows, MySQL, WebView2, MailKit, MimeKit, QRCoder, ZXing.Net

## 1. Purpose of This Report

This document is a current-state technical handoff report for the WildNest project. It is intended to help future assistants, collaborators, or maintainers understand what the project is doing now, what was recently changed, which parts are already connected, and which parts still need improvement.

This report especially focuses on:

- The four `Book Now` booking flows
- Their database writes into MySQL
- QR code and booking ID generation
- Email confirmation behavior
- Guest portal authentication and data display
- Guest-to-reception chat
- Internal staff chat status
- Staff dashboard structure and redesign context
- Current risks and recommended next steps

## 2. Project Summary

WildNest is a Windows Forms desktop management and booking platform for a wildlife resort in Carmen, Cebu, Philippines. The project includes:

- Public-facing homepage and booking entry points
- Four booking modules:
  - Cabin Stay
  - Day Visit
  - Experience Visit
  - Full Stay + Experience
- A guest portal accessed through booking ID + email or QR code
- Staff portals for:
  - Administrator
  - Reception
  - TourGuide
  - ZooKeeper
- Two chat systems:
  - Guest <-> Reception
  - Staff <-> Administrator

The project is already functionally connected end-to-end in many areas. The architecture is understandable, but some schema, UI, and reliability issues still need cleanup.

## 3. Main Architectural Flow

At a high level, the current project works like this:

1. User enters from the homepage.
2. User chooses a booking type through `BookNow`.
3. The booking module collects guest info, dates, selections, and payment choice.
4. The booking module writes to MySQL tables.
5. A booking ID is generated.
6. A QR code is generated from that booking ID.
7. A confirmation email is sent with booking details and QR.
8. The guest later logs in through `MyAccomodation`:
   - Tab 1: Booking ID + email
   - Tab 2: QR code decode
9. `GuestPortalWebView` queries MySQL and displays the guest’s real booking data.
10. Guest and Reception can communicate through `tbl_Chat`.

## 4. Important Main Files

### 4.1 Booking and Public Flow

- `BookNow/BookNow.cs`
- `BookNow/BookNow.Designer.cs`
- `BookNow/CabinStay.cs`
- `BookNow/DayVisit.cs`
- `BookNow/ExperienceVisit.cs`
- `BookNow/FullStayExperience.cs`
- `BookNow/BookingIdGenerator.cs`
- `Accomodations/EmailService.cs`
- `HomePage.cs`

### 4.2 Guest Portal

- `Accomodations/MyAccomodation.cs`
- `Accomodations/GuestPortalWebView.cs`
- `wildnest_portal.html`

### 4.3 Staff Portal

- `StaffPortal/StaffDashboard.cs`
- `StaffPortal/StaffLogin.cs`
- `StaffPortal/UcAdministrator/UcAdminDashboard.cs`
- `StaffPortal/UcReception/UcReceptionDashboard.cs`
- `StaffPortal/UcTourGuide/UcTourGuideDashboard.cs`
- `StaffPortal/UcZooKeeper/UcZookeeperDashboard.cs`
- `StaffPortal/UcReception/UcReceptionChat.cs`
- `StaffPortal/UcStaffChat.cs`

## 5. Database Context

The active backend is MySQL `wildnest_db`.

The user-provided current tables include:

- `tbl_bookingexperiences`
- `tbl_cabins`
- `tbl_chat`
- `tbl_experiences`
- `tbl_reservations`
- `tbl_users`

Other tables referenced directly in code include:

- `tbl_guests`
- `tbl_payments`
- `tbl_staffmessages`

### 5.1 User-Provided Schema Snapshot

The current schema snapshot provided by the user includes:

#### `tbl_bookingexperiences`

- `BookingExpID` int AI PK
- `ReservationID` varchar(20)
- `ExperienceID` int
- `Quantity` int
- `TotalCost` decimal(...)

#### `tbl_cabins`

- `CabinID` int AI PK
- `CabinName` varchar(100)
- `PricePerNight` decimal(10,2)
- `MaxGuests` int
- `Status` varchar(50)

#### `tbl_chat`

- `ChatID` int AI PK
- `ReservationID` varchar(20)
- `GuestName` varchar(200)
- `SenderRole` varchar(20)
- `Message` text
- `SentAt` datetime

#### `tbl_experiences`

- `ExperienceID` int AI PK
- `ExperienceName` varchar(150)
- `PricePerPerson` decimal(10,2)
- `DurationMinutes` int

#### `tbl_reservations`

- `ReservationID` varchar(20) PK
- `GuestID` int
- `CabinID` int
- `BookingType` varchar(50)
- `CheckInDate` date
- `CheckOutDate` date
- `VisitDate` date
- `NumAdults` int
- `NumChildren` int
- `TotalAmount` decimal(10,2)
- `Status` varchar(50)
- `CreatedAt` datetime
- `ArrivalTime` varchar(50)
- `ModeOfTransport` varchar(...)

#### `tbl_users`

- `UserID` int AI PK
- `FullName` varchar(255)
- `Username` varchar(255)
- `PasswordHash` varchar(255)
- `Role` varchar(50)
- `ContactNo` varchar(50)
- `IsActive` tinyint(1)

### 5.2 Important Schema Notes

1. `tbl_chat` in code uses an `IsRead` column.
   - Both `GuestPortalWebView.cs` and `UcReceptionChat.cs` read and update `IsRead`.
   - If the actual table does not have `IsRead`, the guest chat will not fully work as coded.

2. `tbl_StaffMessages` is now required for internal staff chat.
   - This is separate from `tbl_chat`.
   - It is the correct design direction because internal staff messaging should not be mixed with guest chat.

3. `ReservationID` length is currently a risk.
   - The user-provided schema says `varchar(20)`.
   - The new booking ID generator now produces IDs longer than 20 characters.

## 6. Booking ID Generation

### 6.1 Current Logic

Current shared generator file:

- `BookNow/BookingIdGenerator.cs`

Current booking ID format:

- `WN-yyyyMMddHHmmssfff-rand`

Example shape:

- `WN-20260419153045123-4821`

### 6.2 Why This Was Changed

Previously, booking IDs used a weaker pattern similar to:

- `WN-2026-1234`

That was more collision-prone if multiple users booked close together.

The current generator is much safer because it combines:

- timestamp to milliseconds
- cryptographically strong random suffix

### 6.3 Current Risk

The current generator is safer, but the user-provided database schema still says:

- `tbl_reservations.ReservationID = varchar(20)`
- `tbl_bookingexperiences.ReservationID = varchar(20)`

The new booking IDs are longer than 20 characters, so this must be aligned. Otherwise, inserts may fail or data may be truncated.

This is one of the highest-priority technical issues in the project.

## 7. The Four Booking Modules

All four booking modules are separate WinForms `UserControl` flows and all feed into the same reservation ecosystem.

### 7.1 Cabin Stay

Main file:

- `BookNow/CabinStay.cs`

Behavior:

1. User selects:
   - check-in
   - check-out
   - adults
   - children
   - cabin
   - optional experiences/add-ons
2. User enters guest information.
3. User selects payment method.
4. On confirmation:
   - inserts guest into `tbl_Guests`
   - looks up `CabinID` from `tbl_Cabins`
   - inserts reservation into `tbl_Reservations`
   - inserts linked experience rows into `tbl_BookingExperiences` if selected
   - inserts payment into `tbl_Payments`
   - generates QR from `ReservationID`
   - sends confirmation email
   - shows the final confirmation screen

Important code points:

- `ConfirmBooking()` performs the write sequence
- `OnSummaryChanged` updates the right-side booking summary UI
- QR display is now real and tied to `_qrBitmap`

### 7.2 Day Visit

Main file:

- `BookNow/DayVisit.cs`

Behavior:

1. User selects:
   - visit date
   - guest counts
   - day-visit related options
2. User enters guest info.
3. User selects payment method.
4. On confirmation:
   - inserts guest into `tbl_Guests`
   - inserts reservation into `tbl_Reservations` using `VisitDate`
   - inserts payment into `tbl_Payments`
   - generates QR
   - sends confirmation email
   - shows final confirmation with real QR

Important note:

- Day Visit currently does not write booking experiences in the same way as the experience-heavy flows unless that flow was explicitly extended.

### 7.3 Experience Visit

Main file:

- `BookNow/ExperienceVisit.cs`

Behavior:

1. User selects one or more experiences.
2. User selects date / schedule-related values.
3. User enters guest info.
4. On confirmation:
   - inserts guest into `tbl_Guests`
   - inserts reservation into `tbl_Reservations`
   - looks up `ExperienceID` values in `tbl_Experiences`
   - inserts rows into `tbl_BookingExperiences`
   - inserts payment into `tbl_Payments`
   - generates QR
   - sends email
   - shows final confirmation with real QR

### 7.4 Full Stay + Experience

Main file:

- `BookNow/FullStayExperience.cs`

Behavior:

1. User selects stay details:
   - cabin
   - check-in
   - check-out
   - guest counts
2. User selects one or more experiences.
3. User enters guest information.
4. User selects payment method.
5. On confirmation:
   - inserts guest into `tbl_Guests`
   - looks up `CabinID`
   - inserts reservation into `tbl_Reservations`
   - inserts experience links into `tbl_BookingExperiences`
   - inserts payment into `tbl_Payments`
   - generates QR
   - sends email
   - shows final confirmation with real QR

## 8. Shared Book Now UI Logic

The shell is controlled by:

- `BookNow/BookNow.cs`
- `BookNow/BookNow.Designer.cs`

Current structure:

- hero section
- booking-type tab bar
- left booking form panel
- right booking summary panel

Important recent UI changes:

- summary column narrowed
- left booking form widened
- internal margins reduced in all four booking flows
- summary footer height reduced

Reason:

These changes were made because the right side was visually stealing too much space and some booking controls were being clipped or hard to click.

## 9. Booking Summary Logic

The booking shell listens to each booking control’s:

- `OnSummaryChanged`

The right-side summary panel updates live as the user makes selections. This is a strong part of the current user experience because it gives the user immediate visual confirmation of their current booking context.

## 10. QR Code and Email Confirmation

Main file:

- `Accomodations/EmailService.cs`

### 10.1 Current Behavior

The system now uses:

- `EmailService.GenerateQrBitmap(bookingId)`
- `EmailService.SendConfirmation(...)`

This means the same `ReservationID` becomes the source for:

- final on-screen QR
- saved QR image
- attached and embedded email QR
- QR login into the guest portal

### 10.2 Current Status

This is now much more consistent than before. The older placeholder/fake QR behavior in Day Visit, Experience Visit, and Full Stay + Experience has been replaced with real QR display tied to `_qrBitmap`.

### 10.3 Current Risk

Email credentials are hardcoded in source code:

- Gmail sender address
- Gmail app password

This works for development, but is not safe for production or public code sharing.

## 11. Guest Portal Authentication

Main file:

- `Accomodations/MyAccomodation.cs`

The accommodation/guest-portal entry screen has two tabs.

### 11.1 Tab 1 — Booking ID + Email

User enters:

- `ReservationID`
- email used during booking

The system checks:

- `tbl_Reservations`
- joined with `tbl_Guests`

Matching rule:

- reservation ID must match
- email must match

### 11.2 Tab 2 — QR Code

The QR login path:

1. User uploads or drags a QR image
2. ZXing decodes the image
3. The booking ID is extracted
4. The system looks up the reservation by `ReservationID`
5. If found, it opens the same guest portal

Important note:

- Tab 2 currently validates by booking ID alone
- Tab 1 validates by booking ID + email

Both open the same guest portal, but Tab 2 is slightly less strict.

## 12. Guest Portal Data Display

Main file:

- `Accomodations/GuestPortalWebView.cs`

### 12.1 Portal Technology

The guest portal is HTML-based and hosted inside WinForms using WebView2.

This is one of the more visually modern parts of the project.

### 12.2 Data Flow

After successful lookup:

1. The code identifies the guest from email / reservation context.
2. It queries `tbl_Reservations`.
3. It joins:
   - `tbl_Guests`
   - `tbl_Cabins`
   - `tbl_Payments`
4. For each reservation, it also queries:
   - `tbl_BookingExperiences`
   - `tbl_Experiences`
5. It packages the results and injects them into the portal view.

### 12.3 What the Portal Displays

The portal can display:

- reservation ID
- booking type
- check-in / check-out or visit date
- guests
- amount
- cabin name
- payment method
- arrival time
- mode of transport
- add-on experiences
- status / date grouping

This means the guest portal is genuinely reading SQL data, not just static placeholders.

## 13. Guest Chat: Guest <-> Reception

Main files:

- `Accomodations/GuestPortalWebView.cs`
- `StaffPortal/UcReception/UcReceptionChat.cs`

Database table:

- `tbl_Chat`

### 13.1 Guest Side

The guest portal:

- initializes chat using reservation ID and guest name
- inserts guest messages into `tbl_Chat`
- polls for new messages every 3 seconds
- appends new messages into the portal UI

### 13.2 Reception Side

Reception chat:

- shows a conversation list on the left
- groups conversations by reservation ID and guest name
- shows unread counts
- loads full message history
- marks guest messages as read
- polls every 3 seconds
- sends reception replies into `tbl_Chat`
- uses message bubbles in the UI

### 13.3 Important Schema Warning

The code expects `tbl_Chat` to have:

- `IsRead`

But the user-provided schema snapshot did not include `IsRead`. This should be verified in MySQL.

## 14. Internal Staff Chat: Staff <-> Administrator

Main file:

- `StaffPortal/UcStaffChat.cs`

Database table:

- `tbl_StaffMessages`

### 14.1 Purpose

This is a separate internal communication system for:

- Administrator
- Reception
- TourGuide
- ZooKeeper

It is separate from guest chat, which is correct.

### 14.2 Current Behavior

Non-admin roles:

- see only their conversation with Administrator

Administrator:

- sees a conversation list for staff roles
- can reply to a specific role
- can broadcast to all staff

### 14.3 Current Status

The project builds with this feature added.

However:

- Reception internal staff chat is not yet fully wired in the same way as the other roles
- polling cleanup/disposal should be improved
- some silent catches should be replaced with clearer feedback

## 15. Staff Portal Structure

Main files:

- `StaffPortal/StaffDashboard.cs`
- `StaffPortal/StaffLogin.cs`

Current role loader:

- Administrator -> `UcAdminDashboard`
- Reception -> `UcReceptionDashboard`
- TourGuide -> `UcTourGuideDashboard`
- ZooKeeper -> `UcZookeeperDashboard`

This structure is understandable and still connected correctly.

## 16. Homepage and Public UI Status

Main file:

- `HomePage.cs`

Recent public-facing work includes:

- parent-scroll cleanup for Cabin / Experience / Visit / About
- hero-title adjustments
- gold-word spacing improvements
- featured stay and featured experience price visibility fixes
- status-strip height reduction
- booking page layout width adjustments

Some UI issues still need targeted polishing, but the public side is far more structured now than before.

## 17. Major Recent Improvements

The following important project improvements are already reflected in the current code:

1. Unused `EmailLookout` control removed.
2. Namespace/reference errors after folder movement fixed.
3. `GuestPortalWebView` and reception chat structure confirmed.
4. Real QR code generation unified across booking flows.
5. Booking ID generation upgraded to reduce collisions.
6. The same QR now powers:
   - final confirmation display
   - email attachment
   - guest portal QR login
7. Internal staff chat added.
8. Several homepage and booking-layout issues were corrected.

## 18. Current Risks and Technical Debt

These are the most important current issues:

### 18.1 Reservation ID Schema Mismatch

Most serious current risk.

- code now generates long booking IDs
- user-provided schema still says `varchar(20)`

This should be fixed first.

### 18.2 Hardcoded Secrets

`EmailService.cs` contains SMTP credentials directly in source code.

### 18.3 Silent Error Handling

Several areas use empty `catch { }`, especially chat-related code. This hides problems.

### 18.4 Polling Cleanup

Guest chat and internal staff chat rely on timers. Reception guest chat disposes properly; internal staff chat should be reviewed and made equally safe.

### 18.5 Reception Internal Chat Integration

Reception still primarily exposes guest chat. Internal staff chat should be added without breaking guest chat.

### 18.6 Build Warnings

The project builds, but many nullable/reference warnings remain. This does not block execution, but it is a maintainability issue.

## 19. Recommended Next Steps

Recommended order of improvement:

1. Fix `ReservationID` length in MySQL
2. Verify `tbl_Chat` includes `IsRead`
3. Move email credentials out of source code
4. Finish Reception staff-chat integration
5. Improve `UcStaffChat` timer disposal and polling safety
6. Continue staff dashboard redesign using the HTML prototype reference
7. Clean highest-risk warnings

## 20. What Claude Should Understand Before Modifying the Staff Portals

Any future assistant redesigning the staff portals should treat the following as already established:

- The project already has working booking-to-database logic
- The guest portal already reads live booking data from MySQL
- Guest authentication has two entry paths
- QR and booking ID are already central concepts
- Guest <-> Reception chat already exists and should not be broken
- Internal staff chat now exists separately and should remain separate from guest chat
- Staff dashboards must preserve role-specific behavior and loading through `StaffDashboard`
- The current most important non-UI risk is the `ReservationID` length mismatch

## 21. File Reference Map for Future Work

### Booking

- `BookNow/BookNow.cs`
- `BookNow/CabinStay.cs`
- `BookNow/DayVisit.cs`
- `BookNow/ExperienceVisit.cs`
- `BookNow/FullStayExperience.cs`
- `BookNow/BookingIdGenerator.cs`

### Guest Portal

- `Accomodations/MyAccomodation.cs`
- `Accomodations/GuestPortalWebView.cs`
- `Accomodations/EmailService.cs`

### Chat

- `StaffPortal/UcReception/UcReceptionChat.cs`
- `StaffPortal/UcStaffChat.cs`

### Staff Dashboards

- `StaffPortal/UcAdministrator/UcAdminDashboard.cs`
- `StaffPortal/UcReception/UcReceptionDashboard.cs`
- `StaffPortal/UcTourGuide/UcTourGuideDashboard.cs`
- `StaffPortal/UcZooKeeper/UcZookeeperDashboard.cs`
- `StaffPortal/StaffDashboard.cs`
- `StaffPortal/StaffLogin.cs`

## 22. Final Assessment

The project is no longer just a loose collection of forms. It is now a partially integrated resort platform with:

- real booking persistence
- real reservation retrieval
- guest access through booking credentials
- QR-based convenience access
- email confirmation
- guest messaging
- role-based staff dashboards

Its biggest remaining gaps are not basic functionality anymore. They are now:

- schema alignment
- UI polish
- reliability cleanup
- role dashboard modernization

That is a strong sign of progress. The codebase is understandable, connected, and already usable as a solid foundation for the next staff portal redesign phase.
