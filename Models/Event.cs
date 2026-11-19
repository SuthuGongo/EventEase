using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEaseApp.Models
{
    public class Event : IValidatableObject
    {
        [Key]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(100)]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date/time is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Start date/time")]
        public DateTime StartDateTime { get; set; }

        [Required(ErrorMessage = "End date/time is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "End date/time")]
        public DateTime EndDateTime { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        // Nullable: an event can be loaded before a venue is assigned
        [Display(Name = "Venue")]
        public int? VenueId { get; set; }

        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        // Part 3: classification via the EventType lookup table.
        // Nullable so events captured before Part 3 (or not yet classified)
        // remain valid without forcing a value onto existing rows.
        [Display(Name = "Event type")]
        public int? EventTypeId { get; set; }

        [ForeignKey("EventTypeId")]
        [Display(Name = "Event type")]
        public EventType? EventType { get; set; }

        public ICollection<Booking>? Bookings { get; set; }

        // App-level validation to mirror the CK_Event_Dates DB constraint,
        // so the user gets a clean inline error instead of a DB exception.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDateTime <= StartDateTime)
            {
                yield return new ValidationResult(
                    "End date/time must be after the start date/time.",
                    new[] { nameof(EndDateTime) });
            }
        }
    }
}
