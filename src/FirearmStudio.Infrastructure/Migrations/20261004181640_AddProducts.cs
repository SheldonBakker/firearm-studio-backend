using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    cost_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    image_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sku_lower = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, computedColumnSql: "lower(sku)", stored: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.CheckConstraint("ck_products_cost_price", "cost_price is null or cost_price >= 0");
                    table.CheckConstraint("ck_products_price", "price >= 0");
                    table.CheckConstraint("ck_products_stock", "stock_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_products_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_products_company_id",
                table: "products",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_company_id_category",
                table: "products",
                columns: new[] { "company_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_products_company_id_is_active",
                table: "products",
                columns: new[] { "company_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_products_company_id_sku_lower",
                table: "products",
                columns: new[] { "company_id", "sku_lower" },
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                table: "products",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
