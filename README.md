# EventEase: Venue Booking System

A venue booking system for an events company, deployed to the cloud on **Azure App Service**, **Azure SQL Database** and **Azure Blob Storage**. Staff manage venues and events, take bookings, and find bookings quickly with combined filters.

**Highlights**
- **No double bookings:** a unique database index plus an overlap check before every booking is saved
- **Business rules enforced in code:** unavailable venues can't take bookings, past dates are rejected, and venues or events with bookings can't be deleted
- **Advanced search:** filter by event type, date range and venue availability. Filters run as SQL in the database and live in the URL, so a search can be bookmarked or shared
- **Cloud image storage:** venue and event images upload to Azure Blob Storage (JPG/PNG/GIF/WEBP, max 5 MB)

**Built with:** C# · ASP.NET Core 8 MVC · Entity Framework Core · Azure SQL · Azure Blob Storage · Azure App Service · Bootstrap 5

## Run it locally

1. Clone the repo and open `EventEaseApp.slnx` in Visual Studio 2022.
2. The default connection string uses SQL Server LocalDB. Run `Update-Database` in the Package Manager Console to create the database.
3. For image uploads, start the [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) storage emulator (the default `UseDevelopmentStorage=true` connects to it), or set a real Azure Storage connection string with user-secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:AzureStorageConnection" "<your connection string>"
   ```
4. Press F5.

Real connection strings are never committed. Use user-secrets locally and App Service configuration in Azure.

> Built as a three-part project for the Cloud Development module (CLDV7111) of my Advanced Diploma in ICT at Rosebank College.

---

## Development notes: advanced filtering (Part 3)

| Change | Where |
|---|---|
| New `EventType` lookup table with 10 predefined categories | `Models/EventType.cs`, seed in `Data/EventEaseDbContext.cs` |
| `Event.EventTypeId` (nullable FK → EventTypes) | `Models/Event.cs`, Event Create/Edit/Index/Details views |
| `Venue.IsAvailable` availability field (default: available) | `Models/Venue.cs`, Venue Create/Edit/Index/Details views |
| Booking search filters: **event type**, **date range**, **venue availability** (combine with the Part 2 text search) | `BookingController.Search`, `Models/BookingSearchViewModel.cs`, `Views/Booking/Search.cshtml` |
| `BookingDetailsView` now exposes `EventTypeId`, `EventTypeName`, `VenueIsAvailable` | Migration `20261106100000_AddEventTypeAndVenueAvailability` |
| New bookings are blocked for venues marked unavailable | `BookingController.ValidateBookingAsync` |
| Event list can be filtered by type; venue list by availability | `EventController.Index`, `VenueController.Index` |

### Updating the database

* **Azure SQL (Query Editor):** run `SQL/Part3_AdvancedFiltering.sql` (idempotent, single batch).
* **Local DB via EF Core:** `Update-Database` in the Package Manager Console applies the new migration.

### How the filters work

All filters are optional and combine with AND. They are applied to an `IQueryable` so the
filtering is translated to SQL and runs in the database:

* **Event type** – `EventTypeId == selected`.
* **Date range** – overlap test: an event is included if any part of it falls between
  *From* (00:00) and *To* (inclusive, up to 23:59:59). A *From* later than *To* shows a warning
  and the date filter is skipped.
* **Venue availability** – `VenueIsAvailable` true / false / all.

Filters are passed in the query string (`GET`), so a filtered result page can be bookmarked or shared.

## Business rules (all parts)

* No double bookings — unique index on (VenueId, EventId) plus an overlap check in the controller.
* Venues/events with bookings (or venues with events) cannot be deleted.
* Events can exist before a venue is assigned (`Event.VenueId` nullable), but can't be booked until one is.
* Booking venue must match the event's venue; booking date can't be in the past.
* Unavailable venues can't take new bookings (Part 3).
* Image uploads limited to JPG/PNG/GIF/WEBP up to 5 MB (Part 2).

## Code attribution

* Project scaffolding, Razor CRUD views, EF Core DbContext and migration structure follow the
  standard ASP.NET Core MVC and EF Core templates and tutorials:
  Microsoft (2024) *Get started with ASP.NET Core MVC*; Microsoft (2024) *Tutorial: Add sorting,
  filtering, and paging – ASP.NET MVC with EF Core* (the `IQueryable` filter-chaining pattern used
  in `BookingController.Search`, `EventController.Index` and `VenueController.Index`).
* Seed data with `HasData` and migration `InsertData`/`UpdateData`: Microsoft (2023) *Data Seeding – EF Core*.
* Keyless entity mapped to a view (`BookingDetail` → `BookingDetailsView`): Microsoft (2023) *Keyless Entity Types – EF Core*.
* Blob upload/delete service: Microsoft (2024) *Quickstart: Azure Blob Storage client library for .NET*.
* Bootstrap 5.3 and Bootstrap Icons 1.11 loaded from the jsDelivr CDN (MIT licence).


Full references are in the Part 3 report.
