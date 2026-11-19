using Microsoft.EntityFrameworkCore;
using EventEaseApp.Models;

namespace EventEaseApp.Data
{
    public class EventEaseDbContext : DbContext
    {
        public EventEaseDbContext(DbContextOptions<EventEaseDbContext> options)
            : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // Part 3: lookup table of predefined event categories
        public DbSet<EventType> EventTypes { get; set; }

        // Read-only, maps to the BookingDetailsView SQL view (see the
        // CreateBookingDetailsView migration and the .ToView() call below)
        public DbSet<BookingDetail> BookingDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Event -> Venue (optional, since an event can exist before a venue is assigned)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // Part 3: Event -> EventType (optional lookup). Restrict so a category
            // that is still in use can't be deleted out from under its events.
            modelBuilder.Entity<Event>()
                .HasOne(e => e.EventType)
                .WithMany(t => t.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Category names must be unique so the dropdown has no duplicates
            modelBuilder.Entity<EventType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Booking -> Venue
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking -> Event
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking -> Customer
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent double bookings: same Venue + Event combination can't repeat
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.VenueId, b.EventId })
                .IsUnique();

            // Ensure an event's end time is after its start time
            modelBuilder.Entity<Event>()
                .ToTable(t => t.HasCheckConstraint("CK_Event_DateRange", "[EndDateTime] > [StartDateTime]"));

            // BookingDetail is keyless and maps to a SQL view (not a table) -
            // EF Core will never try to create/migrate a table for it, and it's
            // only ever used for read queries (see BookingController.Index).
            modelBuilder.Entity<BookingDetail>()
                .HasNoKey()
                .ToView("BookingDetailsView");

            // Part 3: predefined event categories for the EventType lookup
            modelBuilder.Entity<EventType>().HasData(
                new EventType { EventTypeId = 1, Name = "Conference", Description = "Professional conferences, summits and conventions" },
                new EventType { EventTypeId = 2, Name = "Wedding", Description = "Wedding ceremonies, receptions and expos" },
                new EventType { EventTypeId = 3, Name = "Corporate Function", Description = "Company meetings, product launches and team events" },
                new EventType { EventTypeId = 4, Name = "Gala / Fundraiser", Description = "Formal dinners, galas, auctions and charity events" },
                new EventType { EventTypeId = 5, Name = "Concert / Live Music", Description = "Live performances, concerts and music festivals" },
                new EventType { EventTypeId = 6, Name = "Exhibition / Expo", Description = "Trade shows, exhibitions and expos" },
                new EventType { EventTypeId = 7, Name = "Workshop / Seminar", Description = "Training sessions, workshops and networking breakfasts" },
                new EventType { EventTypeId = 8, Name = "Graduation / Awards", Description = "Graduations, award ceremonies and prize-givings" },
                new EventType { EventTypeId = 9, Name = "Religious Gathering", Description = "Church services, worship nights and faith events" },
                new EventType { EventTypeId = 10, Name = "Social / Party", Description = "Birthdays, celebrations and private parties" }
            );

            // Seed data so the app has content immediately after migration
            modelBuilder.Entity<Venue>().HasData(
                new Venue { VenueId = 1, VenueName = "Cape Town Convention Centre", Location = "Cape Town, Western Cape", Capacity = 5000, ImageUrl = "https://via.placeholder.com/400x250?text=CTICC", IsAvailable = true },
                new Venue { VenueId = 2, VenueName = "The Venue on Fifth", Location = "Sandton, Gauteng", Capacity = 300, ImageUrl = "https://via.placeholder.com/400x250?text=Venue+on+Fifth", IsAvailable = true }
            );

            modelBuilder.Entity<Event>().HasData(
                new Event { EventId = 1, EventName = "Tech Innovators Summit", StartDateTime = new DateTime(2026, 9, 15, 9, 0, 0), EndDateTime = new DateTime(2026, 9, 15, 17, 0, 0), Description = "Annual technology and innovation conference", VenueId = 1, EventTypeId = 1 },
                new Event { EventId = 2, EventName = "Corporate Gala Dinner", StartDateTime = new DateTime(2026, 10, 2, 18, 30, 0), EndDateTime = new DateTime(2026, 10, 2, 23, 0, 0), Description = "Black-tie fundraising dinner", VenueId = 2, EventTypeId = 4 }
            );
        }
    }
}
