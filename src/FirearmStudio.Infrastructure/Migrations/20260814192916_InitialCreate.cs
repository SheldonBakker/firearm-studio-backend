using System;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:customer_type", "company,individual")
                .Annotation("Npgsql:Enum:deposit_mode", "fixed_amount,none,percentage")
                .Annotation("Npgsql:Enum:firearm_origin", "own,range_rental")
                .Annotation("Npgsql:Enum:firearm_status", "in_storage,inactive,pending_transfer,released")
                .Annotation("Npgsql:Enum:invoice_status", "cancelled,draft,overdue,paid,sent")
                .Annotation("Npgsql:Enum:licence_reminder_tier", "days30,days60,days90,expired")
                .Annotation("Npgsql:Enum:licence_status", "expired,renewal_due,unknown,valid")
                .Annotation("Npgsql:Enum:payment_method", "card,cash,debit_order,eft,other")
                .Annotation("Npgsql:Enum:storage_status", "active,cancelled,released")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateSequence(
                name: "booking_number_seq");

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    province = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_holder = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bank_account_number = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    bank_branch_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bank_account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    bank_swift_code = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    due_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    auto_billing_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deposit_mode = table.Column<DepositMode>(type: "deposit_mode", nullable: false, defaultValue: DepositMode.None),
                    deposit_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    deposit_window_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 48),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.CheckConstraint("ck_companies_deposit_percentage", "deposit_mode <> 'percentage' or deposit_value <= 100");
                    table.CheckConstraint("ck_companies_deposit_value", "deposit_value >= 0");
                    table.CheckConstraint("ck_companies_deposit_window_hours", "deposit_window_hours between 1 and 336");
                    table.CheckConstraint("ck_companies_due_days", "due_days between 0 and 365");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    auth_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    role = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    invited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.id);
                    table.CheckConstraint("ck_app_users_role", "role between 0 and 3");
                    table.ForeignKey(
                        name: "fk_app_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_type = table.Column<CustomerType>(type: "customer_type", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    id_number = table.Column<string>(type: "text", nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registration_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vat_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address_line2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    province = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_customers_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_app_users_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_audit_logs_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "firearms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    make = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    calibre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    firearm_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<FirearmStatus>(type: "firearm_status", nullable: false),
                    internal_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_firearms", x => x.id);
                    table.ForeignKey(
                        name: "fk_firearms_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_firearms_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_month = table.Column<DateOnly>(type: "date", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<InvoiceStatus>(type: "invoice_status", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_on = table.Column<DateOnly>(type: "date", nullable: true),
                    deposit_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    deposit_due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deposit_paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.CheckConstraint("ck_invoices_amounts", "subtotal >= 0 and vat_amount >= 0 and total = subtotal + vat_amount");
                    table.CheckConstraint("ck_invoices_kind", "kind between 0 and 1");
                    table.ForeignKey(
                        name: "fk_invoices_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
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

            migrationBuilder.CreateTable(
                name: "firearm_licences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    firearm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    licence_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: false),
                    renewal_due_on = table.Column<DateOnly>(type: "date", nullable: false, computedColumnSql: "expires_on - 90", stored: true),
                    status = table.Column<LicenceStatus>(type: "licence_status", nullable: false),
                    document_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_firearm_licences", x => x.id);
                    table.CheckConstraint("ck_firearm_licences_date_range", "issued_on is null or issued_on <= expires_on");
                    table.ForeignKey(
                        name: "fk_firearm_licences_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_firearm_licences_firearms_firearm_id",
                        column: x => x.firearm_id,
                        principalTable: "firearms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "storage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    firearm_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stored_from = table.Column<DateOnly>(type: "date", nullable: false),
                    stored_until = table.Column<DateOnly>(type: "date", nullable: true),
                    monthly_rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    storage_status = table.Column<StorageStatus>(type: "storage_status", nullable: false),
                    storage_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    rack_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    safe_number = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_storage_records", x => x.id);
                    table.CheckConstraint("ck_storage_records_date_range", "stored_until is null or stored_until >= stored_from");
                    table.CheckConstraint("ck_storage_records_monthly_rate", "monthly_rate > 0");
                    table.CheckConstraint("ck_storage_records_status_dates", "(storage_status = 'active' and stored_until is null) or (storage_status <> 'active' and stored_until is not null)");
                    table.ForeignKey(
                        name: "fk_storage_records_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_storage_records_firearms_firearm_id",
                        column: x => x.firearm_id,
                        principalTable: "firearms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    calendar_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    firearm_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_lines", x => x.id);
                    table.CheckConstraint("ck_invoice_lines_amounts", "quantity > 0 and unit_price >= 0 and line_total >= 0");
                    table.ForeignKey(
                        name: "fk_invoice_lines_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoice_lines_firearms_firearm_id",
                        column: x => x.firearm_id,
                        principalTable: "firearms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    method = table.Column<PaymentMethod>(type: "payment_method", nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "licence_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    licence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<LicenceReminderTier>(type: "licence_reminder_tier", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_licence_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_licence_reminders_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_licence_reminders_firearm_licences_licence_id",
                        column: x => x.licence_id,
                        principalTable: "firearm_licences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_attendees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    id_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    licence_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    firearm_make_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    firearm_serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calibre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    firearm_origin = table.Column<FirearmOrigin>(type: "firearm_origin", nullable: false, defaultValue: FirearmOrigin.Own),
                    signed_indemnity = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_attendees", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_attendees_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_attendees_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_users_auth_user_id",
                table: "app_users",
                column: "auth_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_users_company_id",
                table: "app_users",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_users_company_id_email",
                table: "app_users",
                columns: new[] { "company_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_users_full_name",
                table: "app_users",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_app_user_id",
                table: "audit_logs",
                column: "app_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_company_id",
                table: "audit_logs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_company_id_created_at",
                table: "audit_logs",
                columns: new[] { "company_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_attendees_booking_id",
                table: "booking_attendees",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_attendees_company_id",
                table: "booking_attendees",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_calendar_token",
                table: "bookings",
                column: "calendar_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id",
                table: "bookings",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_company_id_booking_date",
                table: "bookings",
                columns: new[] { "company_id", "booking_date" });

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
                name: "ix_customers_company_id",
                table: "customers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_name",
                table: "customers",
                column: "company_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_email",
                table: "customers",
                column: "email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_full_name",
                table: "customers",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_phone",
                table: "customers",
                column: "phone")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_company_id",
                table: "firearm_licences",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_company_id_status",
                table: "firearm_licences",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_expires_on",
                table: "firearm_licences",
                column: "expires_on");

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_firearm_id",
                table: "firearm_licences",
                column: "firearm_id");

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_firearm_id_licence_number",
                table: "firearm_licences",
                columns: new[] { "firearm_id", "licence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_licence_number",
                table: "firearm_licences",
                column: "licence_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_firearm_licences_renewal_due_on",
                table: "firearm_licences",
                column: "renewal_due_on");

            migrationBuilder.CreateIndex(
                name: "ix_firearms_company_id",
                table: "firearms",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_firearms_company_id_serial_number",
                table: "firearms",
                columns: new[] { "company_id", "serial_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_firearms_customer_id",
                table: "firearms",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_firearms_serial_number",
                table: "firearms",
                column: "serial_number");

            migrationBuilder.CreateIndex(
                name: "ix_firearms_serial_number_trgm",
                table: "firearms",
                column: "serial_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_firearms_status",
                table: "firearms",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_company_id",
                table: "invoice_lines",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_firearm_id",
                table: "invoice_lines",
                column: "firearm_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_invoice_id",
                table: "invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id",
                table: "invoices",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_customer_id_invoice_month",
                table: "invoices",
                columns: new[] { "company_id", "customer_id", "invoice_month" },
                unique: true,
                filter: "kind = 0");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_invoice_number",
                table: "invoices",
                columns: new[] { "company_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_company_id_status",
                table: "invoices",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_customer_id_invoice_month",
                table: "invoices",
                columns: new[] { "customer_id", "invoice_month" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_number",
                table: "invoices",
                column: "invoice_number")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_licence_reminders_company_id",
                table: "licence_reminders",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_licence_reminders_licence_id_tier",
                table: "licence_reminders",
                columns: new[] { "licence_id", "tier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                table: "outbox_messages",
                column: "created_at",
                filter: "processed_at IS NULL");

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
                name: "ix_payments_company_id",
                table: "payments",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_invoice_id",
                table: "payments",
                column: "invoice_id");

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
                name: "ix_sage_connections_company_id",
                table: "sage_connections",
                column: "company_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shooting_ranges_company_id",
                table: "shooting_ranges",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_active",
                table: "storage_records",
                column: "firearm_id",
                unique: true,
                filter: "storage_status = 'active'");

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_company_id",
                table: "storage_records",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_company_id_storage_status",
                table: "storage_records",
                columns: new[] { "company_id", "storage_status" });

            // Hand-written, and must be preserved if this baseline is ever regenerated.
            // A functional index cannot be expressed in the EF model, so the scaffolder
            // will not emit it. It enforces one customer per email address per company,
            // case-insensitively. CustomerEmailUniquenessTests guards it.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ix_customers_company_id_lower_email
                ON customers (company_id, lower(email));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_customers_company_id_lower_email;");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "booking_attendees");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "licence_reminders");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "package_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "range_operating_hours");

            migrationBuilder.DropTable(
                name: "sage_connections");

            migrationBuilder.DropTable(
                name: "storage_records");

            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "firearm_licences");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "shooting_ranges");

            migrationBuilder.DropTable(
                name: "firearms");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropSequence(
                name: "booking_number_seq");
        }
    }
}
