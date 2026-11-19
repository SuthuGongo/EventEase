using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEaseApp.Data;
using EventEaseApp.Models;
using EventEaseApp.Services;

namespace EventEaseApp.Controllers
{
    public class VenueController : Controller
    {
        private readonly EventEaseDbContext _context;
        private readonly IBlobStorageService _blobStorageService;
        private readonly string _containerName;

        public VenueController(EventEaseDbContext context, IBlobStorageService blobStorageService, IConfiguration configuration)
        {
            _context = context;
            _blobStorageService = blobStorageService;
            _containerName = configuration["BlobStorage:VenueContainer"] ?? "venue-images";
        }

        // GET: Venue  (supports search by name or location, plus the Part 3
        // availability filter: "available" / "unavailable" / all)
        public async Task<IActionResult> Index(string? searchString, string? availability)
        {
            var venues = _context.Venues.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                venues = venues.Where(v =>
                    v.VenueName.Contains(searchString) ||
                    v.Location.Contains(searchString));
            }

            if (availability == "available")
            {
                venues = venues.Where(v => v.IsAvailable);
            }
            else if (availability == "unavailable")
            {
                venues = venues.Where(v => !v.IsAvailable);
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentAvailability"] = availability;
            return View(await venues.OrderBy(v => v.VenueName).ToListAsync());
        }

        // GET: Venue/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Events)
                .Include(v => v.Bookings)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // GET: Venue/Create  (new Venue() so the "Available" switch starts ON)
        public IActionResult Create() => View(new Venue());

        // POST: Venue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VenueName,Location,Capacity,IsAvailable")] Venue venue, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    try
                    {
                        venue.ImageUrl = await _blobStorageService.UploadImageAsync(imageFile, _containerName);
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        return View(venue);
                    }
                }

                _context.Add(venue);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Venue created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        // GET: Venue/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venue/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VenueId,VenueName,Location,Capacity,IsAvailable")] Venue venue, IFormFile? imageFile)
        {
            if (id != venue.VenueId) return NotFound();

            // Load the tracked entity so we keep the existing ImageUrl unless
            // the user actually uploaded a replacement image this time.
            var venueToUpdate = await _context.Venues.FirstOrDefaultAsync(v => v.VenueId == id);
            if (venueToUpdate == null) return NotFound();

            if (ModelState.IsValid)
            {
                venueToUpdate.VenueName = venue.VenueName;
                venueToUpdate.Location = venue.Location;
                venueToUpdate.Capacity = venue.Capacity;
                venueToUpdate.IsAvailable = venue.IsAvailable;

                if (imageFile != null)
                {
                    try
                    {
                        var newImageUrl = await _blobStorageService.UploadImageAsync(imageFile, _containerName);
                        // Clean up the old image now that the new one has uploaded successfully
                        await _blobStorageService.DeleteImageAsync(venueToUpdate.ImageUrl, _containerName);
                        venueToUpdate.ImageUrl = newImageUrl;
                    }
                    catch (ArgumentException ex)
                    {
                        ModelState.AddModelError(string.Empty, ex.Message);
                        venue.ImageUrl = venueToUpdate.ImageUrl;
                        return View(venue);
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Venue updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.VenueId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            venue.ImageUrl = venueToUpdate.ImageUrl;
            return View(venue);
        }

        // GET: Venue/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .Include(v => v.Events)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            return View(venue);
        }

        // POST: Venue/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Bookings)
                .Include(v => v.Events)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null) return NotFound();

            bool hasBookings = venue.Bookings != null && venue.Bookings.Any();
            bool hasEvents = venue.Events != null && venue.Events.Any();

            // Business rule: restrict deletion of venues with existing bookings
            // OR events assigned to them (prevents an unhandled DB FK exception).
            if (hasBookings || hasEvents)
            {
                TempData["Error"] = "This venue cannot be deleted because it has existing bookings or events assigned to it.";
                return RedirectToAction(nameof(Index));
            }

            await _blobStorageService.DeleteImageAsync(venue.ImageUrl, _containerName);

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Venue deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id) => _context.Venues.Any(v => v.VenueId == id);
    }
}
