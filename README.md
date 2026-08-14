# Firearm Studio Backend

A multi-tenant SaaS API for South African firearm-storage businesses, built with **.NET 10** and
a self-hosted **PostgreSQL** database. Each company that signs up becomes its own isolated tenant: the
signing-up user becomes that company's `admin` and can assign `manager` / `staff` / `viewer` users.
One company can never see or modify another company's data.

> Compliance note: this is a technical scaffold, not legal advice. Confirm all firearm storage,
> dealer, safe-custody, invoicing, VAT and POPIA obligations with a qualified professional before
> production use.

## Features

- **Authentication** — ASP.NET Core Identity with API-issued HS256 access tokens and rotating refresh tokens. Email confirmation, password reset, and invites all use one-time codes.
- **API-key gate** — every `/api/*` request must carry a valid `X-Api-Key` header (checked before auth).
- **Multi-tenancy** — strict per-company isolation enforced by an EF Core global query filter plus a `SaveChanges` interceptor that stamps and guards `CompanyId`.
- **Role-based access** — `admin` / `manager` / `staff` / `viewer`, stamped into the access token at issue time and enforced with `[Authorize(Roles = ...)]`.
- **Domain** — companies, app users, customers, firearms, licences (with auto-calculated renewal date), storage records, invoices, invoice lines, payments, audit logs.
- **Billing** — monthly invoice generation from active storage, payments, send/cancel lifecycle.

## Tech stack

| Concern | Choice |
| --- | --- |
| Runtime | .NET 10 |
| Web | ASP.NET Core (controllers, API versioning, Swagger) |
| Data | EF Core 10 + Npgsql (Postgres), snake_case naming, native PG enums |
| Auth | ASP.NET Core Identity, API-issued HS256 JWT + rotating refresh tokens, OTP by email |
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
- A PostgreSQL 16 or later instance reachable from your machine, with a database and a role
  that owns it
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
| `ConnectionStrings__DefaultConnection` | Postgres connection (Npgsql key-value format). |
| `TestDatabase__AdminConnection` | Superuser connection used **only** by the integration tests, to create and drop throwaway databases. Not read by the API. |
| `JwtSettings__Issuer` | Issuer stamped into, and required on, every access token. |
| `JwtSettings__SigningKey` | HMAC-SHA256 signing key (e.g. `openssl rand -base64 48`). Rotating it invalidates every outstanding access token. |
| `ApiKeySettings__Key` | Shared secret required on every `/api/*` request via the `X-Api-Key` header (e.g. `openssl rand -base64 32`). |
| `CredentialProtectionSettings__Key` | Base64 32-byte key used to encrypt stored external API credentials (e.g. `openssl rand -base64 32`). |
| `NotificationSettings__PublicBaseUrl` | Public origin used to build absolute links (ICS download, Google Calendar) in booking notification emails. |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production`. |

### Connection string

Npgsql **key-value** format, not the `postgresql://` URI:

```
ConnectionStrings__DefaultConnection=Host=<host>;Port=5432;Database=firearmstudio;Username=firearmstudio;Password=<your-password>;SSL Mode=Disable;Maximum Pool Size=20
```

`SSL Mode=Disable` is only appropriate when the database is on a trusted network. Use
`SSL Mode=Require` for anything else - without it, credentials and data cross the network in
clear text.

Set `Maximum Pool Size` deliberately. Npgsql defaults to 100, which sits badly against
PostgreSQL's own default `max_connections` of 100 once more than one instance connects.

`.env` is git-ignored; never commit real credentials.

## Database setup

The schema is managed by EF Core migrations. The API does not apply pending migrations on startup;
migrations must be applied manually against the target database before deploying new code that
depends on them. Apply migrations first, then deploy/restart the API - PostgreSQL enum type
catalogs are cached per data source, so a running API instance cannot see enum types created after
it started until it restarts.

```bash
dotnet ef database update -p src/FirearmStudio.Infrastructure -s src/FirearmStudio.WebApi
```

Tenant isolation is enforced entirely in application code, by the EF Core global query filter
and the `TenantAndAuditInterceptor`. The database grants no direct table access to anyone but
the application role, so data is reachable only through this WebApi.

### Integration tests

The integration tests need a real PostgreSQL. They create a database named
`firearmstudio_test_<guid>`, migrate it, and drop it afterwards, using the superuser
connection in `TestDatabase__AdminConnection`. The fixture refuses to create or drop any
database whose name does not start with `firearmstudio_test_`, so it cannot touch a real one.

```bash
dotnet test FirearmStudio.slnx --filter "Category!=Performance"
```

## Building & running

```bash
# Restore dependencies and build the whole solution
dotnet build

# Run the API
dotnet run --project src/FirearmStudio.WebApi
```

Swagger UI (Development): `http://localhost:5146/swagger`. Paste an access token from `POST /api/v1/auth/login` via the
**Authorize** button, and send the `X-Api-Key` header, to call protected endpoints.

## Deployment (Docker Compose)

The repo is container-ready: a multi-stage `Dockerfile` (build on `sdk:10.0`, run on the distroless,
non-root `aspnet:10.0-noble-chiseled`) plus a `docker-compose.yml` for the stack.

- The container serves **plain HTTP on port 5146** — TLS is terminated by your reverse proxy.
- Configuration comes from **environment variables** (no `.env` is baked into the image). Set the
  config keys above as environment variables in the stack's `.env` file, using the `Section__Key`
  convention.
- Liveness endpoint: `GET /health` (anonymous, no API key). Point your reverse proxy's health check
  there — the chiseled runtime has no shell, so a container-level `HEALTHCHECK` is intentionally omitted.
- Database migrations are not run automatically by the API (including inside the container); apply
  them manually against the target database before rolling out a new image. Apply migrations first,
  then deploy/restart - PostgreSQL enum type catalogs are cached per data source, so a running API
  cannot see enum types created after it started until it restarts. Ensure the connection used to
  apply migrations has DDL privileges.

The image is **built by GitHub Actions** (`.github/workflows/deploy.yml`) on every push to
`main` and pushed to **GHCR** as `ghcr.io/sheldonbakker/firearm-studio-api:latest`, then deployed to the
host over SSH by the same workflow. `docker-compose.yml` references that image. The package starts
**private** — either make it public or configure a `ghcr.io` registry login on the host (your username
plus a PAT with `read:packages`) so it can pull.

To build/run locally instead:

```bash
docker build -t firearm-studio-api .
docker run -p 5146:5146 \
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=firearmstudio;Username=...;Password=...;SSL Mode=Require" \
  -e JwtSettings__Issuer="https://your-api-domain.example.com" \
  -e JwtSettings__SigningKey="<base64-48-byte-key>" \
  -e ApiKeySettings__Key="<your-api-key>" \
  -e CredentialProtectionSettings__Key="<base64-32-byte-key>" \
  firearm-studio-api
```

## API overview

All routes are versioned under `api/v1` and require the `X-Api-Key` header. Reads require any
authenticated role; writes are gated per the table below.

| Area | Routes | Minimum role |
| --- | --- | --- |
| Auth | `POST /auth/register`, `verify-email`, `resend-code`, `login`, `refresh`, `logout`, `forgot-password`, `reset-password`, `accept-invite` | anonymous (API key still required) |
| Onboarding | `POST /onboarding/company` | any authenticated user (no company yet) |
| Company | `GET /company`, `PATCH /company` | read: any authenticated / write: admin |
| Users | `GET /users`, `POST /users/invite`, `PATCH /users/{id}/role`, `PATCH /users/{id}/deactivate` | admin |
| Customers | `GET/POST/PATCH /customers`, `GET /customers/{id}/firearms`, `GET /customers/{id}/invoices` | read: viewer+ / write: manager+ |
| Firearms | `GET/POST/PATCH /firearms`, `GET /firearms/{id}/licences` | read: viewer+ / write: manager+ |
| Licences | `GET /licences`, `POST /firearms/{id}/licences`, `PATCH /licences/{id}` | read: viewer+ / write: staff+ |
| Storage | `POST /firearms/{id}/storage`, `PATCH /storage-records/{id}/release`, `GET /storage/active`, `GET /storage/customer/{id}` | read: viewer+ / write: staff+ |
| Invoices | `GET /invoices`, `GET /invoices/{id}`, `POST /invoices/{id}/send`, `POST /invoices/{id}/payments`, `PATCH /invoices/{id}/cancel` | read: viewer+ / write: manager+ |
| Sage | `GET /sage/connections`, `POST /sage/register` | admin |
| Dashboard | `GET /dashboard/stats` | any authenticated |
| Audit logs | `GET /audit-logs` | manager+ |
| Registers | `GET /registers/firearms/export`, `GET /registers/safe-custody/export` | manager+ |
| Me | `GET /me`, `GET /me/admin-check` | any authenticated (admin-check: admin) |

> **Automatic monthly invoicing:** there is no manual generate endpoint. A daily background job generates monthly storage invoices for every active company that has `AutoBillingEnabled`, back-filling any unbilled months since each storage record's `StoredFrom`. Active **and** released storage records are billed for the months they overlap (only cancelled storage is excluded). Invoices are dated when issued; the billed period is carried by `InvoiceMonth`. The due date is `today + Company.DueDays` (constrained to 0–365), and VAT (standard 15%) is applied only when the company has a `VatNumber` (i.e. is VAT-registered). Runs are idempotent — any existing invoice for a (customer, month), **including a cancelled one**, marks it as handled; the job never revives or regenerates cancelled invoices. Each month saves independently, so one failure doesn't block the rest, and the job skips (with an error log) until all EF migrations are applied.

### Onboarding flow

1. A user calls `POST /api/v1/auth/register`, then `POST /api/v1/auth/verify-email` with the
   six-digit code emailed to them. Verification returns an access token and a refresh token.
2. With that access token, they call `POST /api/v1/onboarding/company` — this creates the company
   and makes them its `admin`.
3. They call `POST /api/v1/auth/refresh`. The new access token now carries their `company_id` and
   `admin` role. **This step is required**: a token issued before onboarding names no tenant,
   because the claims are resolved at issue time.
4. Subsequent requests are fully tenant-scoped and role-aware.

### Invite flow

1. An admin calls `POST /api/v1/users/invite`. This creates the `AppUser` row **and** a login
   account with no usable password, then emails the invitee a six-digit code.
2. The invitee calls `POST /api/v1/auth/accept-invite` with that code and a password of their
   choosing. This sets the password, confirms the address, links the login account to the
   pending `AppUser`, and returns tokens.
3. Their very first access token already carries `company_id` and their role, so unlike the
   self-signup path there is no extra refresh step.

An invitee who registers through `POST /auth/register` instead is linked to the pending row when
they confirm their email, so both routes converge. Linking happens only at the moment mailbox
control is proven, which is what makes claiming an invited address safe.

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
