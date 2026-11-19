/* =====================================================================
   EventEase - POE Part 3, Section B: queries for the rubric screenshots
   Run each one separately in the Azure Query Editor and screenshot the
   Results grid.
   ===================================================================== */

-- Screenshot 1: the newly created EventTypes table
SELECT * FROM EventTypes ORDER BY EventTypeId;

-- Screenshot 2: the updated Venues table showing the new IsAvailable field
SELECT VenueId, VenueName, Location, Capacity, IsAvailable FROM Venues ORDER BY VenueId;

-- Screenshot 3 (optional): events with their new EventType classification
SELECT e.EventId, e.EventName, e.StartDateTime, et.Name AS EventType
FROM Events e
LEFT JOIN EventTypes et ON e.EventTypeId = et.EventTypeId
ORDER BY e.StartDateTime;

-- Screenshot 4 (optional): the updated consolidated view
SELECT BookingId, BookingReference, EventName, EventTypeName,
       VenueName, VenueIsAvailable, StartDateTime
FROM BookingDetailsView
ORDER BY StartDateTime;
