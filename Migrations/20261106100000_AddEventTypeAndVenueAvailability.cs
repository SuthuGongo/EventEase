using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseApp.Migrations
{
    /// <summary>
    /// Part 3, Section A - Advanced Filtering.
    ///  * Creates the EventTypes lookup table and seeds the predefined categories
    ///  * Adds the nullable Events.EventTypeId foreign key
    ///  * Adds Venues.IsAvailable (existing venues default to available)
    ///  * Recreates BookingDetailsView to expose EventTypeId/EventTypeName and
    ///    VenueIsAvailable so the search page can filter on them
    /// </summary>
    public partial class AddEventTypeAndVenueAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    EventTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.EventTypeId);
                });

            // defaultValue: true backfills every existing venue as available.
            // (Not configured as a model-level default on purpose - with a bool
            // CLR default of false, EF would then skip sending "false" on insert.)
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Venues",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "EventTypeId",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "EventTypeId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Professional conferences, summits and conventions", "Conference" },
                    { 2, "Wedding ceremonies, receptions and expos", "Wedding" },
                    { 3, "Company meetings, product launches and team events", "Corporate Function" },
                    { 4, "Formal dinners, galas, auctions and charity events", "Gala / Fundraiser" },
                    { 5, "Live performances, concerts and music festivals", "Concert / Live Music" },
                    { 6, "Trade shows, exhibitions and expos", "Exhibition / Expo" },
                    { 7, "Training sessions, workshops and networking breakfasts", "Workshop / Seminar" },
                    { 8, "Graduations, award ceremonies and prize-givings", "Graduation / Awards" },
                    { 9, "Church services, worship nights and faith events", "Religious Gathering" },
                    { 10, "Birthdays, celebrations and private parties", "Social / Party" }
                });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "EventId",
                keyValue: 1,
                column: "EventTypeId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "EventId",
                keyValue: 2,
                column: "EventTypeId",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 1,
                column: "IsAvailable",
                value: true);

            migrationBuilder.UpdateData(
                table: "Venues",
                keyColumn: "VenueId",
                keyValue: 2,
                column: "IsAvailable",
                value: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_Name",
                table: "EventTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventTypes_EventTypeId",
                table: "Events",
                column: "EventTypeId",
                principalTable: "EventTypes",
                principalColumn: "EventTypeId",
                onDelete: ReferentialAction.Restrict);

            // Recreate the consolidated view with the new filter columns
            migrationBuilder.Sql("DROP VIEW BookingDetailsView;");
            migrationBuilder.Sql(@"
CREATE VIEW BookingDetailsView AS
SELECT
    b.BookingId,
    b.BookingReference,
    b.BookingDate,
    v.VenueId,
    v.VenueName,
    v.Location,
    v.Capacity,
    v.IsAvailable AS VenueIsAvailable,
    e.EventId,
    e.EventName,
    e.StartDateTime,
    e.EndDateTime,
    e.EventTypeId,
    et.Name AS EventTypeName,
    c.CustomerId,
    c.FirstName AS CustomerFirstName,
    c.LastName  AS CustomerLastName
FROM Bookings b
INNER JOIN Venues v ON b.VenueId = v.VenueId
INNER JOIN Events e ON b.EventId = e.EventId
LEFT JOIN EventTypes et ON e.EventTypeId = et.EventTypeId
LEFT JOIN Customers c ON b.CustomerId = c.CustomerId;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW BookingDetailsView;");
            migrationBuilder.Sql(@"
CREATE VIEW BookingDetailsView AS
SELECT
    b.BookingId,
    b.BookingReference,
    b.BookingDate,
    v.VenueId,
    v.VenueName,
    v.Location,
    v.Capacity,
    e.EventId,
    e.EventName,
    e.StartDateTime,
    e.EndDateTime,
    c.CustomerId,
    c.FirstName AS CustomerFirstName,
    c.LastName  AS CustomerLastName
FROM Bookings b
INNER JOIN Venues v ON b.VenueId = v.VenueId
INNER JOIN Events e ON b.EventId = e.EventId
LEFT JOIN Customers c ON b.CustomerId = c.CustomerId;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventTypes_EventTypeId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropIndex(
                name: "IX_Events_EventTypeId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "EventTypeId",
                table: "Events");
        }
    }
}
