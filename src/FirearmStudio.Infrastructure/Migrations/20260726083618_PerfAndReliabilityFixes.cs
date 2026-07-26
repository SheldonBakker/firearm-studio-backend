using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PerfAndReliabilityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_firearms_serial_number_trgm",
                table: "firearms",
                column: "serial_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_licence_number",
                table: "firearm_licences",
                column: "licence_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_name",
                table: "customers",
                column: "company_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_full_name",
                table: "customers",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                table: "customers",
                column: "phone")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id_booking_date",
                table: "bookings",
                columns: new[] { "company_id", "booking_date" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_company_id_created_at",
                table: "audit_logs",
                columns: new[] { "company_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_app_users_full_name",
                table: "app_users",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_invoice_number",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_firearms_serial_number_trgm",
                table: "firearms");

            migrationBuilder.DropIndex(
                name: "ix_firearm_licences_licence_number",
                table: "firearm_licences");

            migrationBuilder.DropIndex(
                name: "ix_customers_company_name",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_email",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_full_name",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_phone",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_bookings_company_id_booking_date",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_company_id_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_app_users_full_name",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "locked_until",
                table: "outbox_messages");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .OldAnnotation("Npgsql:Enum:customer_type", "company,individual")
                .OldAnnotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .OldAnnotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");
        }
    }
}
