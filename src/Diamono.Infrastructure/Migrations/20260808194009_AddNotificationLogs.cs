using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Template = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_BookingId",
                table: "notification_logs",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_Channel",
                table: "notification_logs",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_CreatedAt",
                table: "notification_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_Status",
                table: "notification_logs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_Template",
                table: "notification_logs",
                column: "Template");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_logs");
        }
    }
}
