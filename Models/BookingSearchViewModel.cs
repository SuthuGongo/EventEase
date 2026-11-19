using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventEaseApp.Models
{
    /// <summary>
    /// Backs the "Find a Booking" page (Booking/Search). Holds the text search
    /// from Part 2 plus the Part 3 advanced filters (event type, date range and
    /// venue availability) so the form can redisplay what the user selected.
    /// </summary>
    public class BookingSearchViewModel
    {
        // ---- Filter inputs (bound from the query string) ----
        [Display(Name = "Search")]
        public string? SearchString { get; set; }

        [Display(Name = "Event type")]
        public int? EventTypeId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "From")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To")]
        public DateTime? EndDate { get; set; }

        /// <summary>"available", "unavailable" or null/empty for all venues.</summary>
        [Display(Name = "Venue availability")]
        public string? Availability { get; set; }

        // ---- Output (never bound from the request) ----
        [BindNever]
        public List<BookingDetail> Results { get; set; } = new();
        [BindNever]
        public SelectList? EventTypes { get; set; }
        [BindNever]
        public int TotalBookings { get; set; }

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchString) ||
            EventTypeId.HasValue ||
            StartDate.HasValue ||
            EndDate.HasValue ||
            !string.IsNullOrWhiteSpace(Availability);
    }
}
