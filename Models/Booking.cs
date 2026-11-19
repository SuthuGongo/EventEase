using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEaseApp.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // A short, human-friendly code (e.g. "X7R9ZA") shown to customers/staff
        // instead of the raw numeric ID - generated automatically on Create,
        // see BookingController.GenerateUniqueReferenceAsync().
        [StringLength(6)]
        [Display(Name = "Reference")]
        public string BookingReference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a venue.")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [Required(ErrorMessage = "Please select an event.")]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [Required(ErrorMessage = "Booking date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Booking date")]
        public DateTime BookingDate { get; set; } = DateTime.Now;
    }
}
