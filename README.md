# Firearm Studio Backend

A multi-tenant SaaS API for South African firearm-storage businesses, built with **.NET 10** and
**Supabase** (Postgres + Auth). Each company that signs up becomes its own isolated tenant: the
signing-up user becomes that company's `admin` and can assign `manager` / `staff` / `viewer` users.
One company can never see or modify another company's data.

> Compliance note: this is a technical scaffold, not legal advice. Confirm all firearm storage,
> dealer, safe-custody, invoicing, VAT and POPIA obligations with a qualified professional before
> production use.

## Features

- **Supabase authentication** — validates Supabase-issued ES256 JWTs via JWKS (the API never issues tokens).
- **Multi-tenancy** — strict per-company isolation enforced by an EF Core global query filter plus a `SaveChanges` interceptor that stamps and guards `CompanyId`.
- **Role-based access** — `admin` / `manager` / `staff` / `viewer`, delivered in the JWT by a Supabase custom access-token hook and enforced with `[Authorize(Roles = ...)]`.
- **Domain** — companies, app users, customers, firearms, licences (with auto-calculated renewal date), storage records, invoices, invoice lines, payments, audit logs.
- **Billing** — monthly invoice generation from active storage, payments, send/cancel lifecycle.

## Tech stack

| Concern | Choice |
| --- | --- |
| Runtime | .NET 10 |
| Web | ASP.NET Core (controllers, API versioning, Swagger) |
| Data | EF Core 10 + Npgsql (Postgres), snake_case naming, native PG enums |
| Auth | Supabase (GoTrue) JWT + JWKS, custom access-token hook |
| Validation | FluentValidation |
| Results | ErrorOr |
| Config | `appsettings.json` + `.env` (DotNetEnv) + user-secrets / env vars |
| Logging | Serilog |

## Architecture

Clean Architecture with dependencies pointing inward (`WebApi → Infrastructure → Application → Domain`):

```
src/
  FirearmStudio.Domain/          Entities, enums, value objects, role constants (no dependencies)
  FirearmStudio.Application/     Abstractions, options, DTOs/contracts, validators
  FirearmStudio.Infrastructure/  EF Core DbContext, tenancy, repositories, services
  FirearmStudio.WebApi/          Controllers, auth wiring, middleware, Program.cs
```

Tenant isolation lives in the infrastructure, not in each query: every entity implementing
`ITenantEntity` is automatically scoped to the caller's company by a global query filter, and the
`TenantAndAuditInterceptor` sets `CompanyId` + timestamps on insert and blocks cross-tenant moves.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Supabase project (this repo targets ref `yqayiyhixfjyhkykbbsa`)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## Configuration

Configuration is read from `appsettings.json`, then `.env` (loaded via DotNetEnv), then real
environment variables / user-secrets (later sources win). Copy the template and fill it in:

```bash
cp .env.example .env
```

`.env` keys (note the `Section__Key` double-underscore convention):

| Key | Description |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Supabase Postgres connection (Npgsql key-value format). |
| `SupabaseJwtSettings__Authority` | Supabase auth URL, e.g. `https://<ref>.supabase.co/auth/v1`. Issuer is derived from it; audience defaults to `authenticated`. |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production`. |

### Connection string

Use the **Session pooler** host (IPv4) from the Supabase dashboard → **Connect** → **Session pooler**,
in Npgsql **key-value** format (not the `postgresql://` URI):

```
ConnectionStrings__DefaultConnection=Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

The direct host `db.<ref>.supabase.co` is IPv6-only and will not connect from IPv4-only networks —
prefer the pooler. `.env` is git-ignored; never commit real credentials.

## Database setup

The schema is managed by EF Core migrations.

```bash
dotnet ef database update -p src/FirearmStudio.Infrastructure -s src/FirearmStudio.WebApi
```

Then apply the Supabase **custom access-token hook** and linkage triggers (run in the Supabase SQL
editor or via a migration). These inject `company_id` + `roles` into every issued JWT and link
invited users to their Supabase account:

```sql
create or replace function public.custom_access_token_hook(event jsonb)
returns jsonb language plpgsql stable set search_path = '' as $$
declare claims jsonb; v_company uuid; v_role public.app_role; v_auth_id uuid := (event->>'user_id')::uuid;
begin
  claims := event->'claims';
  select au.company_id, au.role into v_company, v_role
    from public.app_users au where au.auth_user_id = v_auth_id and au.is_active limit 1;
  if v_company is not null then
    claims := jsonb_set(claims, '{company_id}', to_jsonb(v_company::text), true);
    claims := jsonb_set(claims, '{app_metadata}',
      coalesce(claims->'app_metadata','{}'::jsonb) ||
      jsonb_build_object('roles', jsonb_build_array(v_role::text)), true);
  end if;
  return jsonb_set(event, '{claims}', claims, true);
end; $$;

grant execute on function public.custom_access_token_hook(jsonb) to supabase_auth_admin;
revoke execute on function public.custom_access_token_hook(jsonb) from authenticated, anon, public;
grant select on public.app_users to supabase_auth_admin;

-- app_users has RLS enabled. The hook runs as supabase_auth_admin (a non-BYPASSRLS role), so it
-- needs an explicit read policy or it will see zero rows and inject no claims.
create policy "Allow auth admin to read app_users" on public.app_users
  as permissive for select to supabase_auth_admin using (true);
```

Finally, **enable the hook** in the Supabase dashboard → **Authentication → Hooks → Customize
Access Token (JWT) Claims** → select `public.custom_access_token_hook`. The hook is inert until
enabled — without it, tokens carry no `company_id` or roles and tenant isolation will not engage.

## Building & running

```bash
# Restore dependencies and build the whole solution
dotnet build

# Run the API
dotnet run --project src/FirearmStudio.WebApi
```

Swagger UI (Development): `http://localhost:5146/swagger`. Paste a Supabase access token via the
**Authorize** button to call protected endpoints.

## API overview

All routes are versioned under `api/v1`. Reads require any authenticated role; writes are gated per
the table below.

| Area | Routes | Minimum role |
| --- | --- | --- |
| Onboarding | `POST /onboarding/company` | any authenticated user (no company yet) |
| Users | `GET /users`, `POST /users/invite`, `PATCH /users/{id}/role`, `PATCH /users/{id}/deactivate` | admin |
| Customers | `GET/POST/PATCH /customers`, `GET /customers/{id}/firearms`, `GET /customers/{id}/invoices` | read: viewer+ / write: manager+ |
| Firearms | `GET/POST/PATCH /firearms`, `GET /firearms/storage/active`, `GET /firearms/{id}/licences` | read: viewer+ / write: manager+ |
| Licences | `GET /licences/due-renewal`, `GET /licences/expired`, `POST /firearms/{id}/licences`, `PATCH /licences/{id}` | read: viewer+ / write: staff+ |
| Storage | `POST /firearms/{id}/storage`, `PATCH /storage-records/{id}/release`, `GET /storage/active`, `GET /storage/customer/{id}` | read: viewer+ / write: staff+ |
| Invoices | `POST /invoices/generate-monthly`, `GET /invoices`, `GET /invoices/{id}`, `POST /invoices/{id}/send`, `POST /invoices/{id}/payments`, `PATCH /invoices/{id}/cancel` | read: viewer+ / write: manager+ |
| Audit logs | `GET /audit-logs` | manager+ |
| Me | `GET /me`, `GET /me/admin-check` | any authenticated |

### Onboarding flow

1. A user signs up via Supabase Auth.
2. With their access token, they call `POST /api/v1/onboarding/company` — this creates the company and makes them its `admin`.
3. They **refresh their Supabase session**; the access-token hook now injects their `company_id` and `admin` role.
4. Subsequent requests are fully tenant-scoped and role-aware.

Admins invite staff via `POST /api/v1/users/invite`; invitees are linked to their Supabase account
automatically on signup (or at invite time if they already have one).

## Project layout

```
FirearmStudio.slnx              Solution
Directory.Build.props           Shared build settings (net10.0, nullable, implicit usings)
Directory.Packages.props        Central NuGet package versions
.env.example                    Configuration template (copy to .env)
src/                            Source projects (see Architecture)
```

## Verifying tenant isolation

Onboard two separate companies, create data in each, then confirm that requests authenticated as
company A never return or mutate company B's rows (cross-company `GET`/`PATCH` by id return `404`,
and the same firearm serial number may exist independently in both companies).
