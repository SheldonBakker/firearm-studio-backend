using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingNumberSequenceAndCustomerEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "booking_number_seq");

            // Functional index backing the public-booking customer lookup, which filters on
            // company_id (tenant filter) and lower(email). EF cannot model lower() indexes.
            migrationBuilder.Sql(
                "CREATE INDEX ix_customers_company_id_lower_email ON customers (company_id, lower(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "booking_number_seq");

            migrationBuilder.Sql("DROP INDEX ix_customers_company_id_lower_email;");
        }
    }
}
