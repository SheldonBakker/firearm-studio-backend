using System;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingAttendees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
                .Annotation("Npgsql:Enum:firearm_origin", "own,range_rental")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:customer_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
                .OldAnnotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .OldAnnotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .OldAnnotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "booking_attendees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    id_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    licence_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    firearm_make_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    firearm_serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calibre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    firearm_origin = table.Column<FirearmOrigin>(type: "firearm_origin", nullable: false, defaultValue: FirearmOrigin.Own),
                    signed_indemnity = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_attendees", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_attendees_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_attendees_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_booking_attendees_booking_id",
                table: "booking_attendees",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_attendees_company_id",
                table: "booking_attendees",
                column: "company_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE public.booking_attendees ENABLE ROW LEVEL SECURITY;

                REVOKE ALL PRIVILEGES ON TABLE public.booking_attendees FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_attendees");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:customer_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
                .OldAnnotation("Npgsql:Enum:firearm_origin", "own,range_rental")
                .OldAnnotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .OldAnnotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .OldAnnotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");
        }
    }
}
