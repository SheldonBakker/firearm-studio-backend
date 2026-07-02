using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDueDaysConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_companies_due_days",
                table: "companies",
                sql: "due_days between 0 and 365");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_companies_due_days",
                table: "companies");
        }
    }
}
