using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRangeBookingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_invoices_company_id_customer_id_invoice_month",
                table: "invoices");

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    max_shooters = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packages", x => x.id);
                    table.CheckConstraint("ck_packages_duration", "duration_minutes between 15 and 480");
                    table.CheckConstraint("ck_packages_max_shooters", "max_shooters between 1 and 20");
                    table.CheckConstraint("ck_packages_price", "price >= 0");
                    table.ForeignKey(
                        name: "fk_packages_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shooting_ranges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    lane_count = table.Column<int>(type: "integer", nullable: false),
                    slot_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shooting_ranges", x => x.id);
                    table.CheckConstraint("ck_shooting_ranges_lane_count", "lane_count between 1 and 100");
                    table.CheckConstraint("ck_shooting_ranges_slot_interval", "slot_interval_minutes between 5 and 240");
                    table.ForeignKey(
                        name: "fk_shooting_ranges_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "package_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_items", x => x.id);
                    table.CheckConstraint("ck_package_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_package_items_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_package_items_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shooting_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    booking_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    package_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    package_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    shooter_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_price", "package_price >= 0");
                    table.CheckConstraint("ck_bookings_shooters", "shooter_count between 1 and 20");
                    table.CheckConstraint("ck_bookings_status", "status between 0 and 4");
                    table.CheckConstraint("ck_bookings_times", "end_time > start_time");
                    table.ForeignKey(
                        name: "fk_bookings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_bookings_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_shooting_ranges_shooting_range_id",
                        column: x => x.shooting_range_id,
                        principalTable: "shooting_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "range_operating_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shooting_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<int>(type: "integer", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_range_operating_hours", x => x.id);
                    table.CheckConstraint("ck_range_operating_hours_day", "day between 0 and 6");
                    table.CheckConstraint("ck_range_operating_hours_window", "close_time > open_time");
                    table.ForeignKey(
                        name: "fk_range_operating_hours_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_range_operating_hours_shooting_ranges_shooting_range_id",
                        column: x => x.shooting_range_id,
                        principalTable: "shooting_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_customer_id_invoice_month",
                table: "invoices",
                columns: new[] { "company_id", "customer_id", "invoice_month" },
                unique: true,
                filter: "kind = 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoices_kind",
                table: "invoices",
                sql: "kind between 0 and 1");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id",
                table: "bookings",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id_booking_number",
                table: "bookings",
                columns: new[] { "company_id", "booking_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id_shooting_range_id_booking_date",
                table: "bookings",
                columns: new[] { "company_id", "shooting_range_id", "booking_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id_status",
                table: "bookings",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_customer_id",
                table: "bookings",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_invoice_id",
                table: "bookings",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_package_id",
                table: "bookings",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_shooting_range_id",
                table: "bookings",
                column: "shooting_range_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_items_company_id",
                table: "package_items",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_items_package_id",
                table: "package_items",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_packages_company_id",
                table: "packages",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_range_operating_hours_company_id",
                table: "range_operating_hours",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_range_operating_hours_shooting_range_id_day",
                table: "range_operating_hours",
                columns: new[] { "shooting_range_id", "day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_ranges_company_id",
                table: "shooting_ranges",
                column: "company_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE public.shooting_ranges ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.range_operating_hours ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.packages ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.package_items ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.bookings ENABLE ROW LEVEL SECURITY;

                REVOKE ALL PRIVILEGES ON TABLE
                    public.shooting_ranges,
                    public.range_operating_hours,
                    public.packages,
                    public.package_items,
                    public.bookings
                FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "package_items");

            migrationBuilder.DropTable(
                name: "range_operating_hours");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "shooting_ranges");

            migrationBuilder.DropIndex(
                name: "ix_invoices_company_id_customer_id_invoice_month",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoices_kind",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "invoices");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_customer_id_invoice_month",
                table: "invoices",
                columns: new[] { "company_id", "customer_id", "invoice_month" },
                unique: true);
        }
    }
}
