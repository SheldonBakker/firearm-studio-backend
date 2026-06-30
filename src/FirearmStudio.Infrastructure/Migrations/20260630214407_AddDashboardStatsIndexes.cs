using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardStatsIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_status",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_company_id_storage_status",
                table: "storage_records",
                columns: new[] { "company_id", "storage_status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_status",
                table: "invoices",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_company_id_status",
                table: "firearm_licences",
                columns: new[] { "company_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_storage_records_company_id_storage_status",
                table: "storage_records");

            migrationBuilder.DropIndex(
                name: "ix_invoices_company_id_status",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_firearm_licences_company_id_status",
                table: "firearm_licences");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_status",
                table: "invoices",
                column: "status");
        }
    }
}
