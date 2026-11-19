using System.ComponentModel.DataAnnotations;

namespace EventEaseApp.Models
{
    public class Venue
    {
        [Key]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Venue name is required.")]
        [StringLength(100)]
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required.")]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000.")]
        public int Capacity { get; set; }

        [Display(Name = "Image URL")]
        [Url(ErrorMessage = "Please enter a valid URL (e.g. https://...).")]
        public string? ImageUrl { get; set; }

        // Part 3: availability flag used by the booking search filters.
        // When false the venue is temporarily closed (e.g. renovations) and
        // new bookings against it are blocked in BookingController.
        [Display(Name = "Available for booking")]
        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public ICollection<Event>? Events { get; set; }
        public ICollection<Booking>? Bookings { get; set; }
    }
}
