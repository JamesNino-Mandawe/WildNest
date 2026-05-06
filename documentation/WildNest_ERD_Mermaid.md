# WildNest Simplified ERD

Use this Mermaid source to generate or redraw a clean ERD for printing.

```mermaid
erDiagram
    tbl_Guests ||--o{ tbl_Reservations : places
    tbl_Cabins ||--o{ tbl_Reservations : assigned_to
    tbl_Reservations ||--o{ tbl_Payments : has
    tbl_Reservations ||--o{ tbl_BookingExperiences : includes
    tbl_Experiences ||--o{ tbl_BookingExperiences : selected_in
    tbl_Reservations ||--o{ tbl_Chat : opens
    tbl_users ||--o{ tbl_staffmessages : sends
    tbl_users ||--o{ tbl_staffmessages : receives
    tbl_Reservations ||--o{ tbl_tourschedules : creates
    tbl_users ||--o{ tbl_tourschedules : assigned_to
    tbl_tourschedules ||--o{ tbl_tourcompletions : completed_as
    tbl_animals ||--o{ tbl_healthrecords : has
    tbl_animals ||--o{ tbl_feedings : scheduled_for

    tbl_Guests {
        int GuestID PK
        string FirstName
        string LastName
        string Email
        string Phone
        string Nationality
    }

    tbl_Cabins {
        int CabinID PK
        string CabinName
        string CabinType
        decimal PricePerNight
        string Status
    }

    tbl_Reservations {
        string ReservationID PK
        int GuestID FK
        int CabinID FK
        string BookingType
        date CheckInDate
        date CheckOutDate
        date VisitDate
        string ArrivalTime
        int NumAdults
        int NumChildren
        decimal TotalAmount
        string Status
    }

    tbl_Payments {
        int PaymentID PK
        string ReservationID FK
        decimal Amount
        string PaymentMethod
        string Status
        datetime PaidAt
    }

    tbl_Experiences {
        int ExperienceID PK
        string ExperienceName
        decimal Price
        string Status
    }

    tbl_BookingExperiences {
        int BookingExperienceID PK
        string ReservationID FK
        int ExperienceID FK
    }

    tbl_Chat {
        int ChatID PK
        string ReservationID FK
        string GuestName
        string SenderRole
        string Message
        datetime SentAt
        bool IsRead
    }

    tbl_users {
        int UserID PK
        string Username
        string PasswordHash
        string Role
        bool IsActive
    }

    tbl_staffmessages {
        int StaffMessageID PK
        int SenderUserID FK
        int ReceiverUserID FK
        string Message
        datetime SentAt
        bool IsRead
    }

    tbl_tourschedules {
        int TourScheduleID PK
        string ReservationID FK
        int AssignedGuideUserID FK
        string Status
        datetime ScheduledAt
    }

    tbl_tourcompletions {
        int TourCompletionID PK
        int TourScheduleID FK
        datetime CompletedAt
        string Notes
    }

    tbl_animals {
        int AnimalID PK
        string AnimalName
        string Species
        string HealthStatus
        bool IsEncounterEligible
    }

    tbl_healthrecords {
        int HealthRecordID PK
        int AnimalID FK
        string RecordType
        string Notes
        bool IsCleared
        datetime RecordedAt
    }

    tbl_feedings {
        int FeedingID PK
        int AnimalID FK
        datetime FeedingTime
        string Status
    }
```

Recommended print approach:
- `A3`, `Landscape`
- this is the **best hard-copy size** if your professor wants it readable
- if you only have normal bond paper, use `Legal` landscape for ERD and `A3` for class diagram

