using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxTenantEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_company_id",
                table: "outbox_messages",
                column: "company_id");

            migrationBuilder.AddForeignKey(
                name: "fk_outbox_messages_companies_company_id",
                table: "outbox_messages",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_outbox_messages_companies_company_id",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_company_id",
                table: "outbox_messages");
        }
    }
}
