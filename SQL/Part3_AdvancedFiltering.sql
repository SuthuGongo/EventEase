/* =====================================================================
   EventEase - POE Part 3, Section B: Azure SQL database update
   Run in: Azure Portal > ST10545758-EventEaseDb > Query editor (preview)

   What it does (safe to run more than once):
     1. Creates the EventTypes lookup table + unique index on Name
     2. Seeds the 10 predefined event categories
     3. Adds Events.EventTypeId (nullable FK -> EventTypes)
     4. Adds Venues.IsAvailable (bit, NOT NULL, existing venues = 1/available)
     5. Classifies the existing events by name
     6. Recreates BookingDetailsView with EventTypeId, EventTypeName and
        VenueIsAvailable so the app's search filters can use them
     7. Records the EF Core migration as applied (if the history table exists)

   Each DDL step is wrapped in EXEC() so the whole script runs as ONE batch
   (no GO separators needed) - columns added in one step can be used in the
   next without "invalid column name" compile errors.
   ===================================================================== */

-- 1. EventTypes lookup table ------------------------------------------------
IF OBJECT_ID(N'dbo.EventTypes', N'U') IS NULL
BEGIN
    EXEC(N'
    CREATE TABLE dbo.EventTypes (
        EventTypeId  INT IDENTITY(1,1) NOT NULL,
        Name         NVARCHAR(50)  NOT NULL,
        Description  NVARCHAR(200) NULL,
        CONSTRAINT PK_EventTypes PRIMARY KEY (EventTypeId)
    );
    CREATE UNIQUE INDEX IX_EventTypes_Name ON dbo.EventTypes (Name);');
END;

-- 2. Seed predefined categories (fixed IDs to match the EF Core seed data) --
EXEC(N'
SET IDENTITY_INSERT dbo.EventTypes ON;
MERGE dbo.EventTypes AS target
USING (VALUES
    (1,  N''Conference'',           N''Professional conferences, summits and conventions''),
    (2,  N''Wedding'',              N''Wedding ceremonies, receptions and expos''),
    (3,  N''Corporate Function'',   N''Company meetings, product launches and team events''),
    (4,  N''Gala / Fundraiser'',    N''Formal dinners, galas, auctions and charity events''),
    (5,  N''Concert / Live Music'', N''Live performances, concerts and music festivals''),
    (6,  N''Exhibition / Expo'',    N''Trade shows, exhibitions and expos''),
    (7,  N''Workshop / Seminar'',   N''Training sessions, workshops and networking breakfasts''),
    (8,  N''Graduation / Awards'',  N''Graduations, award ceremonies and prize-givings''),
    (9,  N''Religious Gathering'',  N''Church services, worship nights and faith events''),
    (10, N''Social / Party'',       N''Birthdays, celebrations and private parties'')
) AS source (EventTypeId, Name, Description)
ON target.EventTypeId = source.EventTypeId
WHEN NOT MATCHED THEN
    INSERT (EventTypeId, Name, Description)
    VALUES (source.EventTypeId, source.Name, source.Description);
SET IDENTITY_INSERT dbo.EventTypes OFF;');

-- 3. Events.EventTypeId + index + foreign key -------------------------------
IF COL_LENGTH(N'dbo.Events', N'EventTypeId') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Events ADD EventTypeId INT NULL;');
    EXEC(N'CREATE INDEX IX_Events_EventTypeId ON dbo.Events (EventTypeId);');
    EXEC(N'ALTER TABLE dbo.Events ADD CONSTRAINT FK_Events_EventTypes_EventTypeId
           FOREIGN KEY (EventTypeId) REFERENCES dbo.EventTypes (EventTypeId);');
END;

-- 4. Venues.IsAvailable ------------------------------------------------------
IF COL_LENGTH(N'dbo.Venues', N'IsAvailable') IS NULL
BEGIN
    -- WITH VALUES fills every existing venue with 1 (available)
    EXEC(N'ALTER TABLE dbo.Venues ADD IsAvailable BIT NOT NULL
           CONSTRAINT DF_Venues_IsAvailable DEFAULT (1) WITH VALUES;');
END;

-- 5. Classify existing events (only rows not yet classified) ----------------
EXEC(N'
UPDATE dbo.Events SET EventTypeId = CASE
        WHEN EventName LIKE N''%Summit%'' OR EventName LIKE N''%Conference%'' THEN 1
        WHEN EventName LIKE N''%Wedding%''                                     THEN 2
        WHEN EventName LIKE N''%Launch%'' OR EventName LIKE N''%Pitch%''         THEN 3
        WHEN EventName LIKE N''%Gala%''   OR EventName LIKE N''%Auction%''
          OR EventName LIKE N''%Fundrais%''                                    THEN 4
        WHEN EventName LIKE N''%Concert%'' OR EventName LIKE N''%Music%''       THEN 5
        WHEN EventName LIKE N''%Expo%''   OR EventName LIKE N''%Exhibition%''   THEN 6
        WHEN EventName LIKE N''%Workshop%'' OR EventName LIKE N''%Seminar%''
          OR EventName LIKE N''%Networking%'' OR EventName LIKE N''%Breakfast%'' THEN 7
        WHEN EventName LIKE N''%Graduation%'' OR EventName LIKE N''%Award%''    THEN 8
        WHEN EventName LIKE N''%Gospel%'' OR EventName LIKE N''%Worship%''
          OR EventName LIKE N''%Church%''                                       THEN 9
        WHEN EventName LIKE N''%Party%'' OR EventName LIKE N''%Birthday%''      THEN 10
        ELSE NULL END
WHERE EventTypeId IS NULL;');

-- 6. Recreate the consolidated view with the new filter columns ------------
EXEC(N'
CREATE OR ALTER VIEW dbo.BookingDetailsView AS
SELECT
    b.BookingId,
    b.BookingReference,
    b.BookingDate,
    v.VenueId,
    v.VenueName,
    v.Location,
    v.Capacity,
    v.IsAvailable AS VenueIsAvailable,
    e.EventId,
    e.EventName,
    e.StartDateTime,
    e.EndDateTime,
    e.EventTypeId,
    et.Name AS EventTypeName,
    c.CustomerId,
    c.FirstName AS CustomerFirstName,
    c.LastName  AS CustomerLastName
FROM dbo.Bookings b
INNER JOIN dbo.Venues v      ON b.VenueId = v.VenueId
INNER JOIN dbo.Events e      ON b.EventId = e.EventId
LEFT  JOIN dbo.EventTypes et ON e.EventTypeId = et.EventTypeId
LEFT  JOIN dbo.Customers c   ON b.CustomerId = c.CustomerId;');

-- 7. Keep EF Core's migration history in step (only if the table exists) ---
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
                   WHERE MigrationId = N'20261106100000_AddEventTypeAndVenueAvailability')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261106100000_AddEventTypeAndVenueAvailability', N'8.0.10');
END;

PRINT 'Part 3 database update complete.';
