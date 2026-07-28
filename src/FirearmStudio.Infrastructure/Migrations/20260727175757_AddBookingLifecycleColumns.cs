using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingLifecycleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "calendar_token",
                table: "bookings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder_sent_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            // Existing rows get a placeholder empty calendar_token above; backfill each with a
            // unique, URL-safe token before the column is indexed as unique.
            migrationBuilder.Sql(
                """
                UPDATE public.bookings
                SET calendar_token = replace(gen_random_uuid()::text, '-', '')
                WHERE calendar_token = '';
                """);

            // The empty-string default only existed to satisfy the NOT NULL constraint during
            // backfill above; every row now has a real token, so no further inserts should ever
            // rely on this default.
            migrationBuilder.Sql(
                """
                ALTER TABLE public.bookings ALTER COLUMN calendar_token DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_calendar_token",
                table: "bookings",
                column: "calendar_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bookings_calendar_token",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "calendar_token",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "checked_in_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "reminder_sent_at",
                table: "bookings");
        }
    }
}
