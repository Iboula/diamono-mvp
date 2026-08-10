using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTwilioNotificationDeliveryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "notification_logs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessageSafe",
                table: "notification_logs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "notification_logs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderMessageId",
                table: "notification_logs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "notification_logs");

            migrationBuilder.DropColumn(
                name: "ErrorMessageSafe",
                table: "notification_logs");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "notification_logs");

            migrationBuilder.DropColumn(
                name: "ProviderMessageId",
                table: "notification_logs");
        }
    }
}
