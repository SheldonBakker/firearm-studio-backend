using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyBillingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auto_billing_enabled",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "due_days",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_billing_enabled",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "due_days",
                table: "companies");
        }
    }
}
