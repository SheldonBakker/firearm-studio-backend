using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDepositPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .OldAnnotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .OldAnnotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .OldAnnotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .OldAnnotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .OldAnnotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.AddColumn<DepositMode>(
                name: "deposit_mode",
                table: "companies",
                type: "deposit_mode",
                nullable: false,
                defaultValue: DepositMode.None);

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_value",
                table: "companies",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "deposit_window_hours",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 48);

            migrationBuilder.AddCheckConstraint(
                name: "ck_companies_deposit_percentage",
                table: "companies",
                sql: "deposit_mode <> 'percentage' or deposit_value <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "ck_companies_deposit_value",
                table: "companies",
                sql: "deposit_value >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_companies_deposit_window_hours",
                table: "companies",
                sql: "deposit_window_hours between 1 and 336");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_companies_deposit_percentage",
                table: "companies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_companies_deposit_value",
                table: "companies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_companies_deposit_window_hours",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "deposit_mode",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "deposit_value",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "deposit_window_hours",
                table: "companies");

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
                .OldAnnotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
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
