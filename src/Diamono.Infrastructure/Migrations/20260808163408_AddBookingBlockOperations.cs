using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingBlockOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "booking_blocks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "booking_blocks",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "booking_blocks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "booking_blocks",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "booking_blocks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "booking_blocks",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "booking_blocks");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "booking_blocks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "booking_blocks");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "booking_blocks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "booking_blocks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "booking_blocks");
        }
    }
}
