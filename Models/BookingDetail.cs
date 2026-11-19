namespace EventEaseApp.Models
{
    /// <summary>
    /// Read-only projection of the BookingDetailsView SQL view, which joins
    /// Bookings with Venues, Events, and Customers into a single consolidated
    /// row for the booking specialists' "richer" display (Part 2, Section C).
    /// This is a keyless entity: it's for querying only, never inserted/updated
    /// directly - all writes still go through the Booking entity/table.
    /// </summary>
    public class BookingDetail
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // Part 3: exposed by the updated view so the search page can filter
        // on venue availability without an extra join from the app side.
        public bool VenueIsAvailable { get; set; }

        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        // Part 3: event classification (nullable - an event may be unclassified)
        public int? EventTypeId { get; set; }
        public string? EventTypeName { get; set; }

        public int? CustomerId { get; set; }
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
    }
}
