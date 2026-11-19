# Part 3 – Step-by-step: update, test, deploy, screenshot

Screenshots needed for the rubric are marked 📸.

## 1. Update the code in Visual Studio
1. Replace your project folder with the one from `EventEaseApp_Part3.zip` (or copy the changed files over).
2. Build (Ctrl+Shift+B). It should build with 0 errors.

## 2. Update the Azure SQL database (Section B)
1. Azure Portal → **ST10545758-EventEaseDb** → **Query editor (preview)** → sign in as `venueadmin`.
2. Open `SQL/Part3_AdvancedFiltering.sql`, paste the whole script, click **Run**.
   You should see *Query succeeded*. 📸 *(script + success message)*
3. Run each query in `SQL/Part3_ScreenshotQueries.sql` one at a time:
   - `SELECT * FROM EventTypes ...` 📸 **newly created table in Query Editor** (rubric item)
   - `SELECT VenueId, VenueName, ..., IsAvailable FROM Venues` 📸 **updated field used for filtering in Venue table** (rubric item)
   - The Events and BookingDetailsView queries (optional, nice extra evidence).

> Running locally against LocalDB instead? Use `Update-Database` in the Package Manager Console.
> The `appsettings.json` connection string currently points at Azure SQL, so the app already
> uses the Azure database when run locally.

## 3. Test locally (Section A) – F5
1. **Venues** → *Edit* one venue that has a booking → switch **Available for booking** OFF → Save.
   The card now shows a grey *Unavailable* badge. 📸
2. **Events** → *Edit* any event that still shows *Unclassified* → pick an **Event type** → Save. 📸
3. **Find a Booking** page:
   - Choose an **Event type** (e.g. *Conference*) → *Apply filters*. 📸 *(filter by EventType)*
   - Clear, then set **From / To** dates (e.g. 2026-11-01 to 2026-11-30) → *Apply*. 📸 *(filter by date range)*
   - Clear, set **Venue availability = Unavailable venues only** → *Apply* (shows the venue you switched off),
     then *Available venues only*. 📸 *(filter by venue availability)*
   - Combine all three to show they stack. 📸 (optional)
   - Set *From* after *To* → yellow warning appears (validation). 📸 (optional)
4. **Bookings** → *Add Booking* → pick the unavailable venue → error
   "…is currently marked as unavailable…" 📸 (optional, shows the business rule)

## 4. Deploy to Azure App Service
1. Right-click the project → **Publish** → choose your existing profile **ST10545758 - Web Deploy** → **Publish**.
2. Wait for "Publish succeeded". 📸
3. Open the site:
   `https://st10545758-btcjh6h6fwe4heh3.southafricanorth-01.azurewebsites.net`
   Go to **Find a Booking** and repeat one filter on the live site. 📸 *(live URL visible in the address bar)*

## 5. Submission checklist
- [ ] Web app URL in the report (rubric asks for the `STxxx.azurewebsites.net` form – paste your App Service default domain)
- [ ] Updated codebase zip (delete the `bin`, `obj` and `.vs` folders first to keep it small)
- [ ] Screenshot: EventTypes table in Query Editor
- [ ] Screenshot: Venues table with IsAvailable in Query Editor
- [ ] Screenshots: the three filters working
- [ ] Report: feature list, component discussion, reflection, theory, references
