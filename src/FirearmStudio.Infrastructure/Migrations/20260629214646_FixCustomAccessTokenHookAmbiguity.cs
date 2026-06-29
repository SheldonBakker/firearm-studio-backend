using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomAccessTokenHookAmbiguity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The PL/pgSQL local variable `auth_user_id` collided with the
            // `app_users.auth_user_id` column, making the WHERE clause ambiguous
            // (SQLSTATE 42702). Rename the locals with a `v_` prefix so they can
            // never shadow a column name.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
                    RETURNS jsonb
                    LANGUAGE plpgsql
                    STABLE
                    SET search_path TO ''
                AS $function$
                DECLARE
                    claims jsonb;
                    v_company_id uuid;
                    v_application_role integer;
                    v_auth_user_id uuid := (event->>'user_id')::uuid;
                BEGIN
                    claims := event->'claims';

                    SELECT app_user.company_id, app_user.role
                      INTO v_company_id, v_application_role
                      FROM public.app_users AS app_user
                     WHERE app_user.auth_user_id = v_auth_user_id
                       AND app_user.is_active
                     LIMIT 1;

                    IF v_company_id IS NOT NULL THEN
                        claims := jsonb_set(claims, '{company_id}', to_jsonb(v_company_id::text), true);
                        claims := jsonb_set(
                            claims,
                            '{app_metadata}',
                            coalesce(claims->'app_metadata', '{}'::jsonb)
                                || jsonb_build_object(
                                    'roles',
                                    jsonb_build_array(
                                        CASE v_application_role
                                            WHEN 0 THEN 'admin'
                                            WHEN 1 THEN 'manager'
                                            WHEN 2 THEN 'staff'
                                            WHEN 3 THEN 'viewer'
                                        END)),
                            true);
                    END IF;

                    RETURN jsonb_set(event, '{claims}', claims, true);
                END;
                $function$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the previous (ambiguous) function body from EnforceDataConventions.
            migrationBuilder.Sql(@"
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
                $function$;");
        }
    }
}
