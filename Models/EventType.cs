using System.ComponentModel.DataAnnotations;

namespace EventEaseApp.Models
{
    /// <summary>
    /// Lookup table of predefined event categories (Part 3, Section A).
    /// Each Event can optionally be classified with one EventType, which the
    /// booking specialists can then use to filter search results.
    /// The rows are seeded in EventEaseDbContext.OnModelCreating and in
    /// SQL/Part3_AdvancedFiltering.sql for the Azure SQL database.
    /// </summary>
    public class EventType
    {
        [Key]
        public int EventTypeId { get; set; }

        [Required(ErrorMessage = "Event type name is required.")]
        [StringLength(50)]
        [Display(Name = "Event type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation property: all events classified with this type
        public ICollection<Event>? Events { get; set; }
    }
}
