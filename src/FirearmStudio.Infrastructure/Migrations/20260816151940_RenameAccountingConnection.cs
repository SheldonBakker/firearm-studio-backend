using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountingConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "sage_connections",
                newName: "accounting_connections");

            migrationBuilder.RenameIndex(
                name: "ix_sage_connections_company_id",
                table: "accounting_connections",
                newName: "ix_accounting_connections_company_id");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT pk_sage_connections TO pk_accounting_connections;");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT fk_sage_connections_companies_company_id TO fk_accounting_connections_companies_company_id;");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT ck_sage_connections_sage_company_id TO ck_accounting_connections_sage_company_id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT pk_accounting_connections TO pk_sage_connections;");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT fk_accounting_connections_companies_company_id TO fk_sage_connections_companies_company_id;");

            migrationBuilder.Sql(
                "ALTER TABLE accounting_connections RENAME CONSTRAINT ck_accounting_connections_sage_company_id TO ck_sage_connections_sage_company_id;");

            migrationBuilder.RenameIndex(
                name: "ix_accounting_connections_company_id",
                table: "accounting_connections",
                newName: "ix_sage_connections_company_id");

            migrationBuilder.RenameTable(
                name: "accounting_connections",
                newName: "sage_connections");
        }
    }
}
