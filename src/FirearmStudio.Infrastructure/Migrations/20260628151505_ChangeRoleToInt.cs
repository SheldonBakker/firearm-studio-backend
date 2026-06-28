using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirearmStudio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRoleToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Convert the column from the app_role enum to int.
            //    Map preserves the AppRole enum ordinals: admin=0, manager=1, staff=2, viewer=3.
            migrationBuilder.Sql(
                """
                ALTER TABLE app_users
                    ALTER COLUMN role TYPE integer
                    USING (
                        CASE role::text
                            WHEN 'admin'   THEN 0
                            WHEN 'manager' THEN 1
                            WHEN 'staff'   THEN 2
                            WHEN 'viewer'  THEN 3
                        END
                    );
                """);

            // 2. Recreate the access token hook so it no longer depends on the app_role
            //    type and maps the int role back to its string name for the JWT claim.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
                    RETURNS jsonb
                    LANGUAGE plpgsql
                    STABLE
                    SET search_path TO ''
                AS $function$
                declare
                    claims    jsonb;
                    v_company uuid;
                    v_role    int;
                    v_auth_id uuid := (event->>'user_id')::uuid;
                begin
                    claims := event->'claims';

                    select au.company_id, au.role
                      into v_company, v_role
                      from public.app_users au
                     where au.auth_user_id = v_auth_id
                       and au.is_active
                     limit 1;

                    if v_company is not null then
                        claims := jsonb_set(claims, '{company_id}', to_jsonb(v_company::text), true);
                        claims := jsonb_set(
                            claims,
                            '{app_metadata}',
                            coalesce(claims->'app_metadata', '{}'::jsonb)
                                || jsonb_build_object('roles', jsonb_build_array(
                                    case v_role
                                        when 0 then 'admin'
                                        when 1 then 'manager'
                                        when 2 then 'staff'
                                        when 3 then 'viewer'
                                    end)),
                            true);
                    end if;

                    return jsonb_set(event, '{claims}', claims, true);
                end;
                $function$;
                """);

            // 3. Drop the now-unused enum type.
            migrationBuilder.Sql("DROP TYPE app_role;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Recreate the enum type.
            migrationBuilder.Sql(
                "CREATE TYPE app_role AS ENUM ('admin', 'manager', 'staff', 'viewer');");

            // 2. Convert the column back to the enum.
            migrationBuilder.Sql(
                """
                ALTER TABLE app_users
                    ALTER COLUMN role TYPE app_role
                    USING (
                        CASE role
                            WHEN 0 THEN 'admin'
                            WHEN 1 THEN 'manager'
                            WHEN 2 THEN 'staff'
                            WHEN 3 THEN 'viewer'
                        END::app_role
                    );
                """);

            // 3. Restore the original hook that reads the app_role enum directly.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION public.custom_access_token_hook(event jsonb)
                    RETURNS jsonb
                    LANGUAGE plpgsql
                    STABLE
                    SET search_path TO ''
                AS $function$
                declare
                    claims    jsonb;
                    v_company uuid;
                    v_role    public.app_role;
                    v_auth_id uuid := (event->>'user_id')::uuid;
                begin
                    claims := event->'claims';

                    select au.company_id, au.role
                      into v_company, v_role
                      from public.app_users au
                     where au.auth_user_id = v_auth_id
                       and au.is_active
                     limit 1;

                    if v_company is not null then
                        claims := jsonb_set(claims, '{company_id}', to_jsonb(v_company::text), true);
                        claims := jsonb_set(
                            claims,
                            '{app_metadata}',
                            coalesce(claims->'app_metadata', '{}'::jsonb)
                                || jsonb_build_object('roles', jsonb_build_array(v_role::text)),
                            true);
                    end if;

                    return jsonb_set(event, '{claims}', claims, true);
                end;
                $function$;
                """);
        }
    }
}
