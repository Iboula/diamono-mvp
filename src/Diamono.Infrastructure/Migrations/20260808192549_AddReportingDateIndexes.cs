using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_bookings_StartsAt_EndsAt",
                table: "bookings",
                columns: new[] { "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_blocks_StartsAt_EndsAt",
                table: "booking_blocks",
                columns: new[] { "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_StartsAt_EndsAt",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_booking_blocks_StartsAt_EndsAt",
                table: "booking_blocks");
        }
    }
}
