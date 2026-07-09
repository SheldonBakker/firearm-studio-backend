using System;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707214205_AddSageConnection")]
    public partial class AddSageConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sage_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_key_ciphertext = table.Column<string>(type: "text", nullable: false),
                    username_ciphertext = table.Column<string>(type: "text", nullable: false),
                    password_ciphertext = table.Column<string>(type: "text", nullable: false),
                    sage_company_id = table.Column<int>(type: "integer", nullable: false),
                    sage_company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_registered_by_auth_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sage_connections", x => x.id);
                    table.CheckConstraint("ck_sage_connections_sage_company_id", "sage_company_id > 0");
                    table.ForeignKey(
                        name: "fk_sage_connections_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sage_connections_company_id",
                table: "sage_connections",
                column: "company_id",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE sage_connections ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sage_connections");
        }
    }
}
