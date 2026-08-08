using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStadiumBookingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stadium_booking_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ClosesAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    MinimumDurationHours = table.Column<int>(type: "integer", nullable: false),
                    MaximumDurationHours = table.Column<int>(type: "integer", nullable: false),
                    StandardHourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LocalAscHourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LightingHourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LightingStartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    MaximumAdvanceBookingDays = table.Column<int>(type: "integer", nullable: false),
                    PaymentDeadlineHours = table.Column<int>(type: "integer", nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stadium_booking_settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "stadium_booking_settings",
                columns: new[]
                {
                    "Id",
                    "OpensAt",
                    "ClosesAt",
                    "MinimumDurationHours",
                    "MaximumDurationHours",
                    "StandardHourlyRate",
                    "LocalAscHourlyRate",
                    "LightingHourlyRate",
                    "LightingStartsAt",
                    "DepositAmount",
                    "MaximumAdvanceBookingDays",
                    "PaymentDeadlineHours",
                    "ApprovalRequired",
                    "UpdatedAt",
                    "UpdatedBy"
                },
                values: new object[]
                {
                    new Guid("2df08bf4-26d9-4f65-9e40-0b9b62c9a001"),
                    new TimeOnly(8, 0),
                    new TimeOnly(23, 0),
                    2,
                    6,
                    25_000m,
                    15_000m,
                    5_000m,
                    new TimeOnly(19, 0),
                    25_000m,
                    60,
                    24,
                    true,
                    new DateTimeOffset(new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stadium_booking_settings");
        }
    }
}
