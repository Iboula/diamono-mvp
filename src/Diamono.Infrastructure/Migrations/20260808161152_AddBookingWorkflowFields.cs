using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diamono.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS resources (
                    "Id" uuid NOT NULL,
                    "Name" character varying(160) NOT NULL,
                    "OpensAt" time without time zone NOT NULL,
                    "ClosesAt" time without time zone NOT NULL,
                    "IsBookable" boolean NOT NULL,
                    CONSTRAINT "PK_resources" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS bookings (
                    "Id" uuid NOT NULL,
                    "Reference" character varying(32) NOT NULL,
                    "ResourceId" uuid NOT NULL,
                    "StartsAt" timestamp with time zone NOT NULL,
                    "EndsAt" timestamp with time zone NOT NULL,
                    "CustomerName" character varying(160) NOT NULL,
                    "Phone" character varying(40) NOT NULL,
                    "CustomerCategory" integer NOT NULL,
                    "ActivityType" character varying(100) NOT NULL,
                    "RentalAmount" numeric NOT NULL,
                    "LightingAmount" numeric NOT NULL,
                    "DepositAmount" numeric NOT NULL,
                    "Status" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "ApprovedAt" timestamp with time zone NULL,
                    "RejectedAt" timestamp with time zone NULL,
                    "PaidAt" timestamp with time zone NULL,
                    "CancelledAt" timestamp with time zone NULL,
                    "RejectionReason" character varying(500) NULL,
                    "CancellationReason" character varying(500) NULL,
                    CONSTRAINT "PK_bookings" PRIMARY KEY ("Id")
                );

                CREATE TABLE IF NOT EXISTS booking_blocks (
                    "Id" uuid NOT NULL,
                    "ResourceId" uuid NOT NULL,
                    "StartsAt" timestamp with time zone NOT NULL,
                    "EndsAt" timestamp with time zone NOT NULL,
                    "Reason" character varying(250) NOT NULL,
                    CONSTRAINT "PK_booking_blocks" PRIMARY KEY ("Id")
                );

                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "ApprovedAt" timestamp with time zone NULL;
                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "RejectedAt" timestamp with time zone NULL;
                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "PaidAt" timestamp with time zone NULL;
                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "CancelledAt" timestamp with time zone NULL;
                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "RejectionReason" character varying(500) NULL;
                ALTER TABLE bookings ADD COLUMN IF NOT EXISTS "CancellationReason" character varying(500) NULL;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_bookings_Reference" ON bookings ("Reference");
                CREATE INDEX IF NOT EXISTS "IX_bookings_ResourceId_StartsAt_EndsAt" ON bookings ("ResourceId", "StartsAt", "EndsAt");
                CREATE INDEX IF NOT EXISTS "IX_booking_blocks_ResourceId_StartsAt_EndsAt" ON booking_blocks ("ResourceId", "StartsAt", "EndsAt");

                INSERT INTO resources ("Id", "Name", "OpensAt", "ClosesAt", "IsBookable")
                VALUES ('7f099973-c811-4afe-beca-c9a1b8fcd001', 'Terrain principal - Stade Diamono', TIME '08:00', TIME '23:00', TRUE)
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS booking_blocks;
                DROP TABLE IF EXISTS bookings;
                DROP TABLE IF EXISTS resources;
                """);
        }
    }
}
