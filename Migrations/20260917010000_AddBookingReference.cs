using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingReference",
                table: "Bookings",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");

            // Backfill any bookings that already exist (created before this
            // migration) with a generated 6-character code, since NEWID()
            // evaluates separately per row this gives each one its own value.
            migrationBuilder.Sql(@"
UPDATE Bookings
SET BookingReference = UPPER(LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 6))
WHERE BookingReference = '' OR BookingReference IS NULL;
");

            // Recreate the view to include the new column
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW BookingDetailsView;");
            migrationBuilder.Sql(@"
CREATE VIEW BookingDetailsView AS
SELECT
    b.BookingId,
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

            migrationBuilder.DropColumn(
                name: "BookingReference",
                table: "Bookings");
        }
    }
}
