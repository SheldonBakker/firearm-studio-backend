using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountingExternalCompanyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sage_company_name",
                table: "accounting_connections",
                newName: "external_company_name");

            migrationBuilder.RenameColumn(
                name: "sage_company_id",
                table: "accounting_connections",
                newName: "external_company_id");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT ck_accounting_connections_sage_company_id TO ck_accounting_connections_external_company_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT ck_accounting_connections_external_company_id TO ck_accounting_connections_sage_company_id;");

            migrationBuilder.RenameColumn(
                name: "external_company_name",
                table: "accounting_connections",
                newName: "sage_company_name");

            migrationBuilder.RenameColumn(
                name: "external_company_id",
                table: "accounting_connections",
                newName: "sage_company_id");
        }
    }
}
