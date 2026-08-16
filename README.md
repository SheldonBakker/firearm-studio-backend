# Firearm Studio Backend

A multi-tenant SaaS API for South African firearm-storage businesses, built with .NET 10 and
PostgreSQL. Each company is an isolated tenant: the signing-up user becomes that company's `admin`
and assigns `manager` / `staff` / `viewer` roles. One company can never see another's data.

> Compliance note: this is a technical scaffold, not legal advice. Confirm firearm storage, dealer,
> safe-custody, invoicing, VAT and POPIA obligations with a qualified professional before
> production use.

## Stack

.NET 10, ASP.NET Core, EF Core 10 with Npgsql, ASP.NET Core Identity, FluentValidation, ErrorOr,
Serilog. Auth is HS256 access tokens plus rotating refresh tokens, with one-time codes delivered by
email (Klaviyo) and, where a verified number exists, WhatsApp.

## Architecture

Clean Architecture, dependencies pointing inward:

```
src/
  FirearmStudio.Domain/          Entities, enums, role constants. No dependencies.
  FirearmStudio.Application/     Abstractions, options, contracts, validators.
  FirearmStudio.Infrastructure/  EF Core, tenancy, auditing, external adapters.
  FirearmStudio.WebApi/          Controllers, auth wiring, middleware.
```

Tenant isolation lives in infrastructure, not in individual queries. Every `ITenantEntity` is scoped
to the caller's company by a global query filter, and `TenantAndAuditInterceptor` stamps `CompanyId`
on insert and blocks cross-tenant writes.

## Quick start

Requires the .NET 10 SDK and a PostgreSQL 16+ instance.

```bash
dotnet tool restore
cp .env.example .env
dotnet build
dotnet run --project src/FirearmStudio.WebApi
```

Swagger runs at `http://localhost:5146/swagger` in Development. Protected endpoints need both an
`X-Api-Key` header and a bearer token from `POST /api/v1/auth/login`.

## Configuration

Read from `appsettings.json`, then `.env`, then environment variables. Later sources win. Keys use
the `Section__Key` double-underscore convention.

| Key | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Postgres, Npgsql key-value format, not a `postgresql://` URI. |
| `TestDatabase__AdminConnection` | Superuser connection used only by integration tests. Not read by the API. |
| `JwtSettings__Issuer`, `JwtSettings__SigningKey` | Token issuer and HMAC-SHA256 key. Rotating the key invalidates every outstanding token. |
| `ApiKeySettings__Key` | Shared secret required on every `/api/*` request. |
| `CredentialProtectionSettings__Key` | Base64 32-byte key encrypting stored external credentials. |
| `KlaviyoSettings__ApiKey`, `KlaviyoSettings__ContactListId` | Transactional email. |
| `WahaSettings__*` | WhatsApp OTP delivery via a self-hosted WAHA instance. Set `Enabled=false` to disable. |
| `ForwardedHeaders__KnownNetworks__0` | Reverse-proxy or tunnel network to trust. Required for per-IP rate limiting to work. |
| `NotificationSettings__PublicBaseUrl` | Public origin for absolute links in notification emails. |

Generate secrets with `openssl rand -base64 48`. Use `SSL Mode=Require` unless the database is on a
trusted network, and set `Maximum Pool Size` deliberately: Npgsql defaults to 100, which collides
with PostgreSQL's own default `max_connections` once more than one instance connects.

`.env` is git-ignored. Never commit real credentials.

## Database

Migrations are never applied automatically. Apply them, then restart the API, in that order:
PostgreSQL caches enum type catalogs per data source, so a running instance cannot see enum values
created after it started.

```bash
dotnet ef database update -p src/FirearmStudio.Infrastructure -s src/FirearmStudio.WebApi
```

If `WahaSettings__Enabled` is true, all of `BaseUrl`, `SessionId` and `ApiKey` must be set or the
application refuses to start outside Development.

## Tests

```bash
dotnet test FirearmStudio.slnx --filter "Category!=Performance"
```

Integration tests create a `firearmstudio_test_<guid>` database, migrate it, and drop it afterwards
using `TestDatabase__AdminConnection`. The fixture refuses to touch any database whose name does not
start with `firearmstudio_test_`.

## Licence

Proprietary. All rights reserved. The source is published for transparency and review only, and no
right to use, deploy or distribute it is granted. See [LICENSE](LICENSE).
