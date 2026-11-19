using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EventEaseApp.Models;

namespace EventEaseApp.Controllers
{
    // Home only exists to host the friendly error page that Program.cs's
    // UseExceptionHandler("/Home/Error") redirects to in production, so any
    // unhandled exception shows the user a clean message instead of a raw
    // stack trace or, worse, a 404 because this route didn't exist.
    public class HomeController : Controller
    {
        // Convenience redirect so hitting the site root sends people
        // somewhere useful instead of a blank page.
        public IActionResult Index() => RedirectToAction("Index", "Venue");

        // Shown for 404s and similar status codes via UseStatusCodePagesWithReExecute
        // in Program.cs, instead of the browser's/server's plain default error page.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound(int? statusCode = null)
        {
            Response.StatusCode = statusCode ?? StatusCodes.Status404NotFound;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(model);
        }
    }
}
