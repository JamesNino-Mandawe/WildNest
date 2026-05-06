# WildNest Simplified Class Diagram

Use this Mermaid source to generate or redraw a clean class diagram for printing.

```mermaid
classDiagram
direction LR

class HomePage
class BookNow
class BookNowWebBridge
class BookingPersistence
class BookingIdGenerator
class GuestPortalWebView
class GuestBookingPassGenerator
class EmailService
class StaffLogin
class StaffDashboard
class StaffPortalSupport
class UcReceptionCheckIn
class UcReceptionChat
class UcStaffChat
class UcManagerDashboardContent
class UcTourGuideGroups
class UcZookeeperHealth
class UcZookeeperFlag
class UcZookeeperClear

BookNow --> BookNowWebBridge : uses
BookNow --> BookingPersistence : saves bookings
BookNow --> BookingIdGenerator : generates ID
BookNow --> EmailService : sends confirmation
BookNow --> GuestBookingPassGenerator : creates QR/pass

GuestPortalWebView --> StaffPortalSupport : shared helpers
GuestPortalWebView --> EmailService : uses booking info

StaffLogin --> StaffDashboard : opens
StaffDashboard --> StaffPortalSupport : shared UI/db logic

UcReceptionCheckIn --> StaffPortalSupport : database + dialogs
UcReceptionChat --> StaffPortalSupport : guest chat logic
UcStaffChat --> StaffPortalSupport : internal chat logic

UcManagerDashboardContent --> StaffPortalSupport : analytics data
UcTourGuideGroups --> StaffPortalSupport : tour assignment data
UcZookeeperHealth --> StaffPortalSupport : animal records
UcZookeeperFlag --> StaffPortalSupport : alert workflow
UcZookeeperClear --> StaffPortalSupport : clear alerts

HomePage --> BookNow : opens booking
HomePage --> GuestPortalWebView : opens guest portal
HomePage --> StaffLogin : opens staff portal
```

Recommended print approach:
- `A3`, `Landscape`
- keep this as the **simplified class diagram** for defense
- do not print the giant auto-generated class map unless your professor explicitly wants every field and method

