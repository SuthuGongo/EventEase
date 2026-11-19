using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEaseApp.Data;
using EventEaseApp.Models;

namespace EventEaseApp.Controllers
{
    public class BookingController : Controller
    {
        private readonly EventEaseDbContext _context;

        public BookingController(EventEaseDbContext context)
        {
            _context = context;
        }

        // GET: Booking (supports search by venue, event, or customer name)
        public async Task<IActionResult> Index(string? searchString)
        {
            var bookings = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                bookings = bookings.Where(b =>
                    (b.Venue != null && b.Venue.VenueName.Contains(searchString)) ||
                    (b.Event != null && b.Event.EventName.Contains(searchString)) ||
                    (b.Customer != null && (b.Customer.FirstName.Contains(searchString) || b.Customer.LastName.Contains(searchString))));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await bookings.ToListAsync());
        }

        // GET: Booking/Search
        // Part 2, Section C: consolidated view + text search (Booking ID,
        // reference code or Event Name) reading from BookingDetailsView.
        // Part 3, Section A: advanced filters layered on top of the text search:
        //   - eventTypeId  : only bookings whose event has this EventType
        //   - startDate/endDate : only bookings whose event overlaps the range
        //   - availability : "available" / "unavailable" venues only
        // Every filter is optional and they combine with AND, so specialists can
        // narrow results step by step. All filtering happens in SQL (IQueryable),
        // not in memory, so only matching rows leave the database.
        public async Task<IActionResult> Search(BookingSearchViewModel filters)
        {
            var bookings = _context.BookingDetails.AsQueryable();

            // 1. Free-text search (unchanged from Part 2)
            if (!string.IsNullOrWhiteSpace(filters.SearchString))
            {
                var term = filters.SearchString.Trim();
                bookings = bookings.Where(b =>
                    b.EventName.Contains(term) ||
                    b.BookingId.ToString().Contains(term) ||
                    b.BookingReference.Contains(term));
            }

            // 2. Event type filter
            if (filters.EventTypeId.HasValue)
            {
                bookings = bookings.Where(b => b.EventTypeId == filters.EventTypeId.Value);
            }

            // 3. Date range filter. The inputs are whole dates, so "To" is treated
            //    as inclusive (up to 23:59:59 on that day). An event is included if
            //    any part of it falls inside the range (overlap test), so a
            //    multi-day event that starts before "From" still shows up.
            if (filters.StartDate.HasValue && filters.EndDate.HasValue &&
                filters.StartDate.Value.Date > filters.EndDate.Value.Date)
            {
                ViewData["FilterError"] = "The 'From' date must be on or before the 'To' date. The date filter was not applied.";
            }
            else
            {
                if (filters.StartDate.HasValue)
                {
                    var from = filters.StartDate.Value.Date;
                    bookings = bookings.Where(b => b.EndDateTime >= from);
                }

                if (filters.EndDate.HasValue)
                {
                    var toExclusive = filters.EndDate.Value.Date.AddDays(1);
                    bookings = bookings.Where(b => b.StartDateTime < toExclusive);
                }
            }

            // 4. Venue availability filter
            if (string.Equals(filters.Availability, "available", StringComparison.OrdinalIgnoreCase))
            {
                bookings = bookings.Where(b => b.VenueIsAvailable);
            }
            else if (string.Equals(filters.Availability, "unavailable", StringComparison.OrdinalIgnoreCase))
            {
                bookings = bookings.Where(b => !b.VenueIsAvailable);
            }

            filters.Results = await bookings
                .OrderBy(b => b.StartDateTime)
                .ThenBy(b => b.BookingId)
                .ToListAsync();

            filters.TotalBookings = await _context.Bookings.CountAsync();
            filters.EventTypes = new SelectList(
                await _context.EventTypes.OrderBy(t => t.Name).ToListAsync(),
                "EventTypeId", "Name", filters.EventTypeId);

            return View(filters);
        }

        // GET: Booking/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        private void PopulateDropdowns(Booking? booking = null)
        {
            // Part 3: flag unavailable venues in the dropdown so specialists can
            // see why a booking would be rejected before they submit it.
            var venueOptions = _context.Venues
                .OrderBy(v => v.VenueName)
                .Select(v => new
                {
                    v.VenueId,
                    Label = v.IsAvailable ? v.VenueName : v.VenueName + " (unavailable)"
                })
                .ToList();
            ViewData["VenueId"] = new SelectList(venueOptions, "VenueId", "Label", booking?.VenueId);
            ViewData["EventId"] = new SelectList(_context.Events, "EventId", "EventName", booking?.EventId);
            ViewData["CustomerId"] = new SelectList(_context.Customers, "CustomerId", "FirstName", booking?.CustomerId);

            // Maps EventId -> VenueId so the Create/Edit views can auto-select
            // the correct Venue as soon as an Event is chosen, avoiding the
            // "venue doesn't match event" validation error entirely.
            var eventVenueMap = _context.Events
                .Where(e => e.VenueId != null)
                .ToDictionary(e => e.EventId, e => e.VenueId);
            ViewData["EventVenueMap"] = JsonSerializer.Serialize(eventVenueMap);
        }

        // Shared validation for Create and Edit, keeping business rules in one place.
        private async Task ValidateBookingAsync(Booking booking)
        {
            // 1. Booking date can't be materially in the past
            if (booking.BookingDate < DateTime.Now.AddMinutes(-5))
            {
                ModelState.AddModelError(nameof(booking.BookingDate), "Booking date cannot be in the past.");
            }

            // Part 3: a venue marked as unavailable can't take new bookings.
            // On Edit we only block if the venue is being *changed* to an
            // unavailable one, so existing bookings can still be edited.
            var selectedVenue = await _context.Venues.FindAsync(booking.VenueId);
            if (selectedVenue != null && !selectedVenue.IsAvailable)
            {
                var existingVenueId = booking.BookingId == 0
                    ? (int?)null
                    : await _context.Bookings.Where(b => b.BookingId == booking.BookingId)
                                             .Select(b => (int?)b.VenueId)
                                             .FirstOrDefaultAsync();

                if (existingVenueId != booking.VenueId)
                {
                    ModelState.AddModelError(nameof(booking.VenueId),
                        $"{selectedVenue.VenueName} is currently marked as unavailable and cannot take new bookings.");
                }
            }

            var selectedEvent = await _context.Events.FindAsync(booking.EventId);

            if (selectedEvent != null)
            {
                // 2. The event must already have a venue assigned before it can be booked
                if (selectedEvent.VenueId == null)
                {
                    ModelState.AddModelError(nameof(booking.EventId),
                        "This event does not yet have a venue assigned and cannot be booked.");
                }
                // 3. The booking's venue must match the event's actual venue
                else if (selectedEvent.VenueId != booking.VenueId)
                {
                    ModelState.AddModelError(nameof(booking.VenueId),
                        "The selected venue does not match the venue assigned to this event.");
                }

                // 4. Prevent double-booking: block any overlapping event at the same venue
                bool overlap = await _context.Bookings
                    .Include(b => b.Event)
                    .Where(b => b.VenueId == booking.VenueId && b.BookingId != booking.BookingId)
                    .AnyAsync(b => b.Event != null &&
                        b.Event.StartDateTime < selectedEvent.EndDateTime &&
                        selectedEvent.StartDateTime < b.Event.EndDateTime);

                if (overlap)
                {
                    ModelState.AddModelError(string.Empty,
                        "This venue is already booked for an overlapping time slot.");
                }
            }
        }

        // GET: Booking/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VenueId,EventId,CustomerId,BookingDate")] Booking booking)
        {
            await ValidateBookingAsync(booking);

            if (ModelState.IsValid)
            {
                booking.BookingReference = await GenerateUniqueReferenceAsync();
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Booking created successfully. Reference: {booking.BookingReference}";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(booking);
            return View(booking);
        }

        // GET: Booking/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            PopulateDropdowns(booking);
            return View(booking);
        }

        // POST: Booking/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,VenueId,EventId,CustomerId,BookingDate")] Booking booking)
        {
            if (id != booking.BookingId) return NotFound();

            await ValidateBookingAsync(booking);

            if (ModelState.IsValid)
            {
                // Load the tracked entity so BookingReference (not part of the
                // edit form) is preserved rather than overwritten with an
                // empty string by a blind _context.Update(booking) call.
                var bookingToUpdate = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == id);
                if (bookingToUpdate == null) return NotFound();

                bookingToUpdate.VenueId = booking.VenueId;
                bookingToUpdate.EventId = booking.EventId;
                bookingToUpdate.CustomerId = booking.CustomerId;
                bookingToUpdate.BookingDate = booking.BookingDate;

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Booking updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(booking);
            return View(booking);
        }

        // GET: Booking/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Booking/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Booking deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id) => _context.Bookings.Any(b => b.BookingId == id);

        // Characters chosen to avoid visually ambiguous ones (0/O, 1/I/L),
        // since these codes are meant to be read out loud or typed by hand.
        private static readonly char[] ReferenceChars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789".ToCharArray();

        private async Task<string> GenerateUniqueReferenceAsync()
        {
            var random = new Random();
            string candidate;

            do
            {
                candidate = new string(Enumerable.Range(0, 6)
                    .Select(_ => ReferenceChars[random.Next(ReferenceChars.Length)])
                    .ToArray());
            }
            while (await _context.Bookings.AnyAsync(b => b.BookingReference == candidate));

            return candidate;
        }
    }
}
