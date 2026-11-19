using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEaseApp.Data;
using EventEaseApp.Models;
using EventEaseApp.Services;

namespace EventEaseApp.Controllers
{
    public class EventController : Controller
    {
        private readonly EventEaseDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly string _containerName;

        public EventController(EventEaseDbContext context, IBlobStorageService blobStorageService, IConfiguration configuration)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _containerName = configuration["BlobStorage:EventContainer"] ?? "event-images";
        }

        // GET: Event  (search by name + Part 3 filter by EventType)
        public async Task<IActionResult> Index(string? searchString, int? eventTypeId)
        {
            var events = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e => e.EventName.Contains(searchString));
            }

            if (eventTypeId.HasValue)
            {
                events = events.Where(e => e.EventTypeId == eventTypeId.Value);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["EventTypeFilter"] = new SelectList(
                await _context.EventTypes.OrderBy(t => t.Name).ToListAsync(),
                "EventTypeId", "Name", eventTypeId);
            return View(await events.OrderBy(e => e.StartDateTime).ToListAsync());
        }

        // Populates the Venue and EventType dropdowns used by Create/Edit
        private void PopulateDropdowns(Event? ev = null)
        {
            ViewData["VenueId"] = new SelectList(_context.Venues.OrderBy(v => v.VenueName), "VenueId", "VenueName", ev?.VenueId);
            ViewData["EventTypeId"] = new SelectList(_context.EventTypes.OrderBy(t => t.Name), "EventTypeId", "Name", ev?.EventTypeId);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: Event/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventName,StartDateTime,EndDateTime,Description,VenueId,EventTypeId")] Event ev, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    try
                    {
                        ev.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile, _containerName);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        PopulateDropdowns(ev);
                        return View(ev);
                    }
                }

                _context.Add(ev);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Event created successfully.";
                return RedirectToAction(nameof(Index));
            }
            PopulateDropdowns(ev);
            return View(ev);
        }

        // GET: Event/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            PopulateDropdowns(ev);
            return View(ev);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("EventId,EventName,StartDateTime,EndDateTime,Description,VenueId,EventTypeId")] Event ev, IFormFile? imageFile)
        {
            if (id != ev.EventId) return NotFound();

            // Load the tracked entity so we keep the existing ImageUrl unless
            // the user actually uploaded a replacement image this time.
            var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (eventToUpdate == null) return NotFound();

            if (ModelState.IsValid)
            {
                eventToUpdate.EventName = ev.EventName;
                eventToUpdate.StartDateTime = ev.StartDateTime;
                eventToUpdate.EndDateTime = ev.EndDateTime;
                eventToUpdate.Description = ev.Description;
                eventToUpdate.VenueId = ev.VenueId;
                eventToUpdate.EventTypeId = ev.EventTypeId;

                if (imageFile != null)
                {
                    try
                    {
                        var newImageUrl = await _blobStorageService.UploadImageAsync(imageFile, _containerName);
                        await _blobStorageService.DeleteImageAsync(eventToUpdate.ImageUrl, _containerName);
                        eventToUpdate.ImageUrl = newImageUrl;
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        ev.ImageUrl = eventToUpdate.ImageUrl;
                        PopulateDropdowns(ev);
                        return View(ev);
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Event updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(ev.EventId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ev.ImageUrl = eventToUpdate.ImageUrl;
            PopulateDropdowns(ev);
            return View(ev);
        }

        // GET: Event/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // POST: Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            // Business rule: restrict deletion of events with existing bookings
            if (ev.Bookings != null && ev.Bookings.Any())
            {
                TempData["Error"] = "This event cannot be deleted because it has existing bookings.";
                return RedirectToAction(nameof(Index));
            }

            await _blobStorageService.DeleteImageAsync(ev.ImageUrl, _containerName);

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id) => _context.Events.Any(e => e.EventId == id);
    }
}
