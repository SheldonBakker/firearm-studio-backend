using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDataConventions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_storage_records_active",
                table: "storage_records");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "storage_records",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "payments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "firearms",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "model",
                table: "firearms",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "internal_reference",
                table: "firearms",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "firearm_type",
                table: "firearms",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "calibre",
                table: "firearms",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "document_url",
                table: "firearm_licences",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "vat_number",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registration_number",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "province",
                table: "customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_code",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "customers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "customers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line2",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "province",
                table: "companies",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_code",
                table: "companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "companies",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line2",
                table: "companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_active",
                table: "storage_records",
                column: "firearm_id",
                unique: true,
                filter: "storage_status = 'active'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_records_date_range",
                table: "storage_records",
                sql: "stored_until is null or stored_until >= stored_from");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_records_monthly_rate",
                table: "storage_records",
                sql: "monthly_rate > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_storage_records_status_dates",
                table: "storage_records",
                sql: "(storage_status = 'active' and stored_until is null) or (storage_status <> 'active' and stored_until is not null)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_amount",
                table: "payments",
                sql: "amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoices_amounts",
                table: "invoices",
                sql: "subtotal >= 0 and vat_amount >= 0 and total = subtotal + vat_amount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_invoice_lines_amounts",
                table: "invoice_lines",
                sql: "quantity > 0 and unit_price >= 0 and line_total >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_firearm_licences_date_range",
                table: "firearm_licences",
                sql: "issued_on is null or issued_on <= expires_on");

            migrationBuilder.AddCheckConstraint(
                name: "ck_app_users_role",
                table: "app_users",
                sql: "role between 0 and 3");

            migrationBuilder.Sql(
                """
                ALTER TABLE public.companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.app_users ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.customers ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.firearms ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.firearm_licences ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.storage_records ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.invoices ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.invoice_lines ENABLE ROW LEVEL SECURITY;
                ALTER TABLE public.payments ENABLE ROW LEVEL SECURITY;

                REVOKE ALL PRIVILEGES ON TABLE
                    public.companies,
                    public.app_users,
                    public.audit_logs,
                    public.customers,
                    public.firearms,
                    public.firearm_licences,
                    public.storage_records,
                    public.invoices,
                    public.invoice_lines,
                    public.payments
                FROM anon, authenticated;

                CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
                    RETURNS jsonb
                    LANGUAGE plpgsql
                    STABLE
                    SET search_path TO ''
                AS $function$
                DECLARE
                    claims jsonb;
                    company_id uuid;
                    application_role integer;
                    auth_user_id uuid := (event->>'user_id')::uuid;
                BEGIN
                    claims := event->'claims';

                    SELECT app_user.company_id, app_user.role
                      INTO company_id, application_role
                      FROM public.app_users AS app_user
                     WHERE app_user.auth_user_id = auth_user_id
                       AND app_user.is_active
                     LIMIT 1;

                    IF company_id IS NOT NULL THEN
                        claims := jsonb_set(claims, '{company_id}', to_jsonb(company_id::text), true);
                        claims := jsonb_set(
                            claims,
                            '{app_metadata}',
                            coalesce(claims->'app_metadata', '{}'::jsonb)
                                || jsonb_build_object(
                                    'roles',
                                    jsonb_build_array(
                                        CASE application_role
                                            WHEN 0 THEN 'admin'
                                            WHEN 1 THEN 'manager'
                                            WHEN 2 THEN 'staff'
                                            WHEN 3 THEN 'viewer'
                                        END)),
                            true);
                    END IF;

                    RETURN jsonb_set(event, '{claims}', claims, true);
                END;
                $function$;

                GRANT USAGE ON SCHEMA public TO supabase_auth_admin;
                GRANT EXECUTE ON FUNCTION public.custom_access_token_hook(jsonb) TO supabase_auth_admin;
                REVOKE EXECUTE ON FUNCTION public.custom_access_token_hook(jsonb) FROM anon, authenticated, public;
                GRANT SELECT ON TABLE public.app_users TO supabase_auth_admin;

                DROP POLICY IF EXISTS "Allow auth admin to read app_users" ON public.app_users;
                DROP POLICY IF EXISTS app_users_auth_admin_select ON public.app_users;
                CREATE POLICY app_users_auth_admin_select
                    ON public.app_users
                    FOR SELECT
                    TO supabase_auth_admin
                    USING (true);

                CREATE SCHEMA IF NOT EXISTS app_private;
                REVOKE ALL ON SCHEMA app_private FROM public, anon, authenticated;

                CREATE OR REPLACE FUNCTION app_private.link_app_user()
                    RETURNS trigger
                    LANGUAGE plpgsql
                    SECURITY DEFINER
                    SET search_path TO ''
                AS $function$
                BEGIN
                    IF NEW.email IS NULL OR EXISTS (
                        SELECT 1
                          FROM public.app_users AS linked_user
                         WHERE linked_user.auth_user_id = NEW.id
                    ) THEN
                        RETURN NEW;
                    END IF;

                    UPDATE public.app_users
                       SET auth_user_id = NEW.id,
                           linked_at = timezone('utc', now()),
                           updated_at = timezone('utc', now())
                     WHERE id = (
                        SELECT pending_user.id
                          FROM public.app_users AS pending_user
                         WHERE pending_user.auth_user_id IS NULL
                           AND lower(pending_user.email) = lower(NEW.email)
                         ORDER BY pending_user.invited_at NULLS LAST, pending_user.created_at, pending_user.id
                         LIMIT 1
                     );

                    RETURN NEW;
                END;
                $function$;

                REVOKE ALL ON FUNCTION app_private.link_app_user() FROM public, anon, authenticated;
                DROP TRIGGER IF EXISTS link_app_user_after_auth_user_change ON auth.users;
                CREATE TRIGGER link_app_user_after_auth_user_change
                    AFTER INSERT OR UPDATE OF email ON auth.users
                    FOR EACH ROW
                    EXECUTE FUNCTION app_private.link_app_user();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS link_app_user_after_auth_user_change ON auth.users;
                DROP FUNCTION IF EXISTS app_private.link_app_user();
                DROP SCHEMA IF EXISTS app_private;
                DROP POLICY IF EXISTS app_users_auth_admin_select ON public.app_users;

                ALTER TABLE public.companies DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.app_users DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.audit_logs DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.customers DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.firearms DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.firearm_licences DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.storage_records DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.invoices DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.invoice_lines DISABLE ROW LEVEL SECURITY;
                ALTER TABLE public.payments DISABLE ROW LEVEL SECURITY;

                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
                    public.companies,
                    public.app_users,
                    public.audit_logs,
                    public.customers,
                    public.firearms,
                    public.firearm_licences,
                    public.storage_records,
                    public.invoices,
                    public.invoice_lines,
                    public.payments
                TO anon, authenticated;
                """);

            migrationBuilder.DropIndex(
                name: "ix_storage_records_active",
                table: "storage_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_records_date_range",
                table: "storage_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_records_monthly_rate",
                table: "storage_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_storage_records_status_dates",
                table: "storage_records");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_amount",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoices_amounts",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_invoice_lines_amounts",
                table: "invoice_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_firearm_licences_date_range",
                table: "firearm_licences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_app_users_role",
                table: "app_users");

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "storage_records",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "payments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "firearms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "model",
                table: "firearms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "internal_reference",
                table: "firearms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "firearm_type",
                table: "firearms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "calibre",
                table: "firearms",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "document_url",
                table: "firearm_licences",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "vat_number",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registration_number",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "province",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_code",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line2",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "customers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "province",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "postal_code",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line2",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "address_line1",
                table: "companies",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_storage_records_active",
                table: "storage_records",
                column: "firearm_id",
                filter: "storage_status = 'active'");
        }
    }
}
