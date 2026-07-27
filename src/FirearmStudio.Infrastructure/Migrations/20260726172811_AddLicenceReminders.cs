using System;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenceReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:customer_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .OldAnnotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "licence_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    licence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<LicenceReminderTier>(type: "licence_reminder_tier", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_licence_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_licence_reminders_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_licence_reminders_firearm_licences_licence_id",
                        column: x => x.licence_id,
                        principalTable: "firearm_licences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_licence_reminders_company_id",
                table: "licence_reminders",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_licence_reminders_licence_id_tier",
                table: "licence_reminders",
                columns: new[] { "licence_id", "tier" },
                unique: true);

            migrationBuilder.Sql(
                """
                ALTER TABLE public.licence_reminders ENABLE ROW LEVEL SECURITY;

                REVOKE ALL PRIVILEGES ON TABLE public.licence_reminders FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "licence_reminders");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:customer_type", "company,individual")
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
