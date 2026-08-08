using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPublicAccessToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicAccessToken",
                table: "bookings",
                type: "character varying(96)",
                maxLength: 96,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE bookings
                SET "PublicAccessToken" =
                    lower(md5(random()::text || clock_timestamp()::text || "Id"::text)) ||
                    lower(md5("Id"::text || random()::text || clock_timestamp()::text))
                WHERE "PublicAccessToken" IS NULL OR "PublicAccessToken" = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PublicAccessToken",
                table: "bookings",
                type: "character varying(96)",
                maxLength: 96,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(96)",
                oldMaxLength: 96,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_PublicAccessToken",
                table: "bookings",
                column: "PublicAccessToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_bookings_PublicAccessToken",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PublicAccessToken",
                table: "bookings");
        }
    }
}
