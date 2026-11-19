using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventEaseApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateBookingDetailsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW BookingDetailsView;");
        }
    }
}
