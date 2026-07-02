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
- **API-key gate** — every `/api/*` request must carry a valid `X-Api-Key` header (checked before auth).
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
  FirearmStudio.Infrastructure/  EF Core DbContext, tenancy, auditing, technical adapters
  FirearmStudio.WebApi/          Controllers, auth wiring, middleware, Program.cs
```

Tenant isolation lives in the infrastructure, not in each query: every entity implementing
`ITenantEntity` is automatically scoped to the caller's company by a global query filter, and the
`TenantAndAuditInterceptor` sets `CompanyId` + timestamps on insert and blocks cross-tenant moves.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Supabase project (this repo targets ref `yqayiyhixfjyhkykbbsa`)
- Restore the pinned local `dotnet-ef` tool: `dotnet tool restore`

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
| `ApiKeySettings__Key` | Shared secret required on every `/api/*` request via the `X-Api-Key` header (e.g. `openssl rand -base64 32`). |
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

The migrations also install the Supabase custom access-token hook, its grants and RLS policy,
enable RLS on every application table, and revoke direct table access from the `anon` and
`authenticated` Data API roles. Application data is exposed only through this WebApi.

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
**Authorize** button, and send the `X-Api-Key` header, to call protected endpoints.

## Deployment (Docker / Portainer)

The repo is container-ready: a multi-stage `Dockerfile` (build on `sdk:10.0`, run on the distroless,
non-root `aspnet:10.0-noble-chiseled`) plus a `docker-compose.yml` for a Portainer stack.

- The container serves **plain HTTP on port 5146** — TLS is terminated by your reverse proxy.
- Configuration comes from **environment variables** (no `.env` is baked into the image). Set the
  config keys above as **stack environment variables in Portainer** (Stacks → your stack →
  Environment variables), using the `Section__Key` convention.
- Liveness endpoint: `GET /health` (anonymous, no API key). Point your reverse proxy's health check
  there — the chiseled runtime has no shell, so a container-level `HEALTHCHECK` is intentionally omitted.
- Database migrations are **not** run from the container; the Supabase schema is managed separately.

The image is **built by GitHub Actions** (`.github/workflows/docker-publish.yml`) on every push to
`main` and pushed to **GHCR** as `ghcr.io/sheldonbakker/firearm-studio-api:latest`. `docker-compose.yml`
references that image. In Portainer → **Stacks → Add stack**, add the env vars and deploy; update with
**Pull and redeploy**. The package starts **private** — either make it public or add a `ghcr.io`
Registry in Portainer (your username + a PAT with `read:packages`) so it can pull.

To build/run locally instead:

```bash
docker build -t firearm-studio-api .
docker run -p 5146:5146 \
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true" \
  -e SupabaseJwtSettings__Authority="https://yqayiyhixfjyhkykbbsa.supabase.co/auth/v1" \
  -e ApiKeySettings__Key="<your-api-key>" \
  firearm-studio-api
```

## API overview

All routes are versioned under `api/v1` and require the `X-Api-Key` header. Reads require any
authenticated role; writes are gated per the table below.

| Area | Routes | Minimum role |
| --- | --- | --- |
| Onboarding | `POST /onboarding/company` | any authenticated user (no company yet) |
| Company | `GET /company`, `PATCH /company` | read: any authenticated / write: admin |
| Users | `GET /users`, `POST /users/invite`, `PATCH /users/{id}/role`, `PATCH /users/{id}/deactivate` | admin |
| Customers | `GET/POST/PATCH /customers`, `GET /customers/{id}/firearms`, `GET /customers/{id}/invoices` | read: viewer+ / write: manager+ |
| Firearms | `GET/POST/PATCH /firearms`, `GET /firearms/{id}/licences` | read: viewer+ / write: manager+ |
| Licences | `GET /licences`, `POST /firearms/{id}/licences`, `PATCH /licences/{id}` | read: viewer+ / write: staff+ |
| Storage | `POST /firearms/{id}/storage`, `PATCH /storage-records/{id}/release`, `GET /storage/active`, `GET /storage/customer/{id}` | read: viewer+ / write: staff+ |
| Invoices | `GET /invoices`, `GET /invoices/{id}`, `POST /invoices/{id}/send`, `POST /invoices/{id}/payments`, `PATCH /invoices/{id}/cancel` | read: viewer+ / write: manager+ |
| Dashboard | `GET /dashboard/stats` | any authenticated |
| Audit logs | `GET /audit-logs` | manager+ |
| Me | `GET /me`, `GET /me/admin-check` | any authenticated (admin-check: admin) |

> **Automatic monthly invoicing:** there is no manual generate endpoint. A daily background job generates monthly storage invoices for every active company that has `AutoBillingEnabled`, back-filling any unbilled months since each storage record's `StoredFrom`. Active **and** released storage records are billed for the months they overlap (only cancelled storage is excluded). Invoices are dated when issued; the billed period is carried by `InvoiceMonth`. The due date is `today + Company.DueDays` (constrained to 0–365), and VAT (standard 15%) is applied only when the company has a `VatNumber` (i.e. is VAT-registered). Runs are idempotent — any existing invoice for a (customer, month), **including a cancelled one**, marks it as handled; the job never revives or regenerates cancelled invoices. Each month saves independently, so one failure doesn't block the rest, and the job skips (with an error log) until all EF migrations are applied.

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
