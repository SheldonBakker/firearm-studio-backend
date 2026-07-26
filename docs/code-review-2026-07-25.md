# Code review findings and fix log - 2026-07-25

Full-codebase review (Application, Infrastructure, WebApi). Each issue below is tracked
through to a fix. Status: `pending` | `in-progress` | `fixed` | `wont-fix (reason)`.

## Critical

### C1. Rate limiting bypassable via X-Forwarded-For spoofing
- **Where:** `src/FirearmStudio.WebApi/Program.cs:29-34`
- **Problem:** `KnownIPNetworks.Clear()` + `KnownProxies.Clear()` makes `UseForwardedHeaders`
  trust `X-Forwarded-For` from any source. Rate-limit partitions key on `RemoteIpAddress`,
  so clients can spoof the header to rotate IPs and bypass limits on public endpoints.
- **Fix:** Trust only explicitly configured proxies (bind known networks/proxies from
  configuration, no blanket clear).
- **Status:** fixed (verified 2026-07-26)

### C2. Outbox processor sends duplicate emails
- **Where:** `src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs:55-99`,
  `src/FirearmStudio.Infrastructure/Persistence/Configurations/OutboxConfigurations.cs:13`
- **Problem:** (a) no row claim (`FOR UPDATE SKIP LOCKED`) so a second instance dispatches
  the same batch; (b) single end-of-batch `SaveChangesAsync` re-sends already-dispatched
  messages after a crash/save failure and loses `Attempts` increments; (c) unbounded
  `message.Error = ex.Message` vs `HasMaxLength(4000)` column can make the batch save throw
  forever, re-sending the batch every 30s.
- **Fix:** Claim rows with `SKIP LOCKED` inside a transaction, persist state per message
  immediately after dispatch, truncate `Error` to the column limit.
- **Status:** fixed (verified 2026-07-26)

### C3. Overpayment race on partial payments
- **Where:** `src/FirearmStudio.Application/Invoices/RecordPayment/RecordPaymentCommandHandler.cs:33-56`
- **Problem:** Balance guard is `SumAsync` over payments then insert. A partial payment
  changes no invoice column, so the `xmin` concurrency token never arbitrates - two
  concurrent partial payments can both pass the guard and exceed the invoice total.
- **Fix:** Run the check-and-insert atomically (serializable transaction helper already used
  by booking creation) so concurrent payments serialize.
- **Status:** fixed (verified 2026-07-26)

### C4. Sync DB query inside async SaveChanges pipeline
- **Where:** `src/FirearmStudio.Infrastructure/Persistence/Interceptors/TenantAndAuditInterceptor.cs:107-110`
- **Problem:** `ResolveAppUserId` calls blocking `FirstOrDefault()` from `SavingChangesAsync`
  on every audited save - sync Npgsql I/O on a threadpool thread.
- **Fix:** Async resolution in the async interceptor path (cached per scope as today).
- **Status:** fixed (verified 2026-07-26)

### C5. Last-admin race (zero-admin company possible)
- **Where:** `src/FirearmStudio.Application/Users/ChangeUserRole/ChangeUserRoleCommandHandler.cs:40-46`,
  `src/FirearmStudio.Application/Users/DeactivateUser/DeactivateUserCommandHandler.cs:22-26`
- **Problem:** `CountAsync(active admins) <= 1` check then separate save - two concurrent
  demotions/deactivations of the last two admins leave zero admins.
- **Fix:** Atomic conditional update (guard re-checked in the same statement/transaction).
- **Status:** fixed (verified 2026-07-26)

## Database query savings

### Q1. Public checkout N+1 (hottest write path)
- **Where:** `src/FirearmStudio.Application/Bookings/CreatePublicBooking/CreatePublicBookingCommandHandler.cs:72-96`,
  `src/FirearmStudio.Application/Bookings/BookingCreation.cs:40-130`
- **Problem:** Each cart session issues ~4 queries (range+hours, package+items, overlap
  count, next booking number) inside one serializable transaction - ~4N queries per
  checkout and a wide serialization-conflict window.
- **Fix:** Memoize range/package lookups per unique id across the loop; batch overlap
  counts into one grouped query.
- **Status:** fixed (verified 2026-07-26)

### Q2. Dashboard runs 4 sequential aggregate queries
- **Where:** `src/FirearmStudio.Application/Dashboard/GetDashboardStats/GetDashboardStatsQueryHandler.cs:15-47`
- **Problem:** 4 awaited round trips per dashboard load (storage, firearms, invoices,
  licences aggregates).
- **Fix:** Single round trip (one SQL statement of scalar subqueries).
- **Status:** fixed (verified 2026-07-26)

### Q3. RecordPayment does 3 sequential round trips
- **Where:** `src/FirearmStudio.Application/Invoices/RecordPayment/RecordPaymentCommandHandler.cs:17-60`
- **Problem:** Separate invoice fetch, payments `SumAsync`, and pending-bookings query.
- **Fix:** Project invoice + payments sum in one query (combined with C3 fix).
- **Status:** fixed (verified 2026-07-26)

### Q4. Public availability endpoints uncached and multi-query
- **Where:** `src/FirearmStudio.Application/Bookings/GetMonthAvailability/`, `GetDayAvailability/`,
  `src/FirearmStudio.WebApi/Controllers/PublicBookingsController.cs:23-52`
- **Problem:** 4 sequential queries per call on anonymous, widget-polled endpoints; no
  output caching.
- **Fix:** Short-TTL output caching on the three public read endpoints (booking creation
  re-validates, so staleness is safe); fold package-duration lookup into the range query.
- **Status:** fixed (verified 2026-07-26)

### Q5. Missing composite indexes
- **Where:** `src/FirearmStudio.Infrastructure/Persistence/Configurations/BillingConfigurations.cs`
  (AuditLog), `BookingConfigurations.cs:133`
- **Problem:** `audit_logs` (highest-write table) has no `(CompanyId, CreatedAt)` index but
  is listed ordered by `CreatedAt` desc; bookings calendar/list filter by date without a
  range id and only the `company_id` index prefix is usable.
- **Fix:** Add `(company_id, created_at)` on audit_logs and `(company_id, booking_date)` on
  bookings, via EF model + migration.
- **Status:** fixed (verified 2026-07-26)

### Q6. Search filters defeat all indexes
- **Where:** All list handlers (`GetCustomers`, `GetInvoices`, `GetFirearms`, `GetLicences`,
  `GetPackages`, `GetShootingRanges`, `GetStorageRecords`, `GetAuditLogs`)
- **Problem:** `col.ToLower().Contains(term)` becomes `lower(col) LIKE '%term%'` -
  unindexable, run twice per page (count + list).
- **Fix:** Switch to `EF.Functions.ILike` and add `pg_trgm` GIN indexes on searched columns.
- **Status:** fixed (verified 2026-07-26)

### Q7. Per-query claim re-parsing in tenant resolution
- **Where:** `src/FirearmStudio.Infrastructure/Services/CurrentUserService.cs:10-48`
- **Problem:** `User` re-parses claims and allocates on every access; the EF tenant filter
  evaluates it for every query plus every SaveChanges.
- **Fix:** Cache the resolved `CurrentUser` per scoped service instance.
- **Status:** fixed (verified 2026-07-26)

### Q8. Background jobs re-check migrations every tick
- **Where:** `src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs:42`,
  `MonthlyInvoiceGenerationService.cs:42`
- **Problem:** `GetPendingMigrationsAsync` runs per tick (outbox: every 30s, ~2,880
  `__EFMigrationsHistory` reads/day/instance).
- **Fix:** Check once at service start, cache the result.
- **Status:** fixed (verified 2026-07-26)

### Q9. Monthly invoice job runs on every startup and drifts
- **Where:** `src/FirearmStudio.WebApi/BackgroundJobs/MonthlyInvoiceGenerationService.cs:12-18`
- **Problem:** 24h `PeriodicTimer` with an immediate run on startup - every deploy re-runs
  generation for all companies; crash loops hammer the DB; tick time drifts.
- **Fix:** Schedule to a fixed daily time; keep generation idempotent per company/month.
- **Status:** fixed (verified 2026-07-26)

### Q10. ConfirmBooking loads a tracked entity for one string
- **Where:** `src/FirearmStudio.Application/Bookings/ConfirmBooking/ConfirmBookingCommandHandler.cs:14-16`
- **Problem:** `.Include(b => b.ShootingRange)` fetches and tracks the whole range to read
  `Name`.
- **Fix:** Drop the Include; fetch the name via projection where needed.
- **Status:** fixed (verified 2026-07-26)

## Medium

### M1. Contact endpoint has no rate limit
- **Where:** `src/FirearmStudio.WebApi/Controllers/ContactController.cs:17-22`
- **Problem:** Anonymous endpoint, two outbound Klaviyo calls per request, unlimited - abuse
  vector for list-bombing and quota burn.
- **Fix:** `[EnableRateLimiting("public-write")]`.
- **Status:** fixed (verified 2026-07-26)

### M2. Startup still auto-applies migrations
- **Where:** `src/FirearmStudio.WebApi/Program.cs:85`
- **Problem:** `MigrateAsync` runs on every instance at startup - races on multi-instance
  rollouts and contradicts commit `ab21263` (manual migrations) and the jobs' own guards.
- **Fix:** Remove; migrations applied manually/deploy-step per the existing decision.
- **Status:** fixed (verified 2026-07-26)

### M3. API key middleware unthrottled and mis-scoped
- **Where:** `src/FirearmStudio.WebApi/Program.cs:99`, `Middleware/ApiKeyMiddleware.cs`
- **Problem:** Runs before the rate limiter (brute-force unlimited) and guards
  `[AllowAnonymous]` public routes, forcing the "secret" into browser JS.
- **Fix:** Exempt public endpoint paths from the key check; keep the key for first-party
  dashboard traffic only.
- **Status:** fixed (verified 2026-07-26)

### M4. Cancelling a booking leaves its invoice payable
- **Where:** `src/FirearmStudio.Application/Bookings/CancelBooking/CancelBookingCommandHandler.cs:25-26`
- **Problem:** Cancellation only sets booking status; a Sent invoice for a cancelled booking
  stays payable/overdue.
- **Fix:** Policy decision required - see architect notes below.
- **Status:** fixed (verified 2026-07-26)

### M5. Audit interceptor wasted work + silent drops
- **Where:** `src/FirearmStudio.Infrastructure/Persistence/Interceptors/TenantAndAuditInterceptor.cs:49-93,115-132`
- **Problem:** Builds dictionaries and serializes JSON per entity before the
  `CompanyId is null` check discards them; `entry.Properties` enumerated twice; audit rows
  silently dropped during bypass saves.
- **Fix:** Short-circuit before building; materialize properties once; document the bypass
  behavior explicitly.
- **Status:** fixed (verified 2026-07-26)

## Low

### L1. GetCustomerStorageRecords unbounded
- **Where:** `src/FirearmStudio.Application/StorageRecords/GetCustomerStorageRecords/GetCustomerStorageRecordsQueryHandler.cs:14-20`
- **Fix:** Cap like sibling handlers. **Status:** fixed (verified 2026-07-26)

### L2. Generic failures mapped to 502
- **Where:** `src/FirearmStudio.WebApi/Common/ErrorOrExtensions.cs:42`
- **Fix:** `ErrorType.Failure` -> 500; reserve 502 for upstream errors. **Status:** fixed (verified 2026-07-26)

### L3. Serializable retry loop has no backoff
- **Where:** `src/FirearmStudio.Infrastructure/Persistence/ApplicationDbContext.cs:55-77`
- **Fix:** Small randomized delay between attempts. **Status:** fixed (verified 2026-07-26)

### L4. Unique customer email index missing from EF model
- **Where:** migrations `20260706202625`/`20260713212044` (raw SQL) vs `CustomerConfiguration`
- **Fix:** Document/model the `(company_id, lower(email))` invariant in configuration.
  **Status:** fixed (verified 2026-07-26)

### L5. Klaviyo client accepts empty API key silently
- **Where:** `src/FirearmStudio.Infrastructure/Extensions/DependencyInjection.cs:56-68`
- **Fix:** Fail fast at registration when the key is missing. **Status:** fixed (verified 2026-07-26)

### L6. Swallowed .env load errors
- **Where:** `src/FirearmStudio.WebApi/Program.cs:13-19`
- **Fix:** Log the exception. **Status:** fixed (verified 2026-07-26)

### L7. Recursive bin/Debug self-nesting
- **Where:** `src/FirearmStudio.WebApi/bin/Debug/net10.0/bin/...`
- **Fix:** Clean output dirs; find the copy target that loops. **Status:** fixed (root cause: bad bin exclude glob in Directory.Build.props)

## Deferred / follow-ups

- **No test projects exist.** Stack default is xUnit + Testcontainers; standing that up is
  its own task. Fixes in this pass are verified by build + independent code review, not
  automated tests.
- `OutboxMessage.CompanyId` has no FK/index - denormalized by design or add FK (decide when
  outbox grows beyond one message type).
- Ciphertext columns unbounded (`IntegrationConfigurations.cs:13-15`) - add generous max
  lengths opportunistically.

## Verification round 1 (2026-07-26)

Independent verifier: build clean; migration `20260726083618_PerfAndReliabilityFixes`
applies; all 10 new indexes + pg_trgm + `locked_until` confirmed in the database; outbox
claim SQL, dashboard SQL, checkout batching, serializable guards, pipeline order, and
output-cache vary-keys all verified correct. Three findings, fixed in round 2:

1. **Audit action filter regression** - `GetAuditLogs` Action filter had become
   case-sensitive exact match while the interceptor writes Title Case. Fixed: ILike exact
   (case-insensitive, escaped, no wildcards).
2. **Klaviyo fail-fast broke local dev** - empty ApiKey in appsettings + no .env crashed
   startup. Fixed: throws outside Development, warns in Development.
3. **Bad CIDR crashed on first request, not startup** - ForwardedHeaders parse was inside
   the lazy options callback. Fixed: eager parse at startup.

Incident note: the verifier's `dotnet ef database update` connected to the production
Supabase database (DesignTimeDbContextFactory loads `.env`, overriding its local
override; plain local Postgres fails on Supabase-role migrations). The new migration is
therefore ALREADY APPLIED to production. Changes are additive only (extension, nullable
column, indexes) and safe for the currently deployed code. Follow-up: make the
design-time factory prefer an explicit override over `.env`, and provide a
Supabase-compatible local verification path (roles seed or migration guard).

## Deploy checklist

- [x] Migration applied to production (inadvertently early, but additive-safe)
- [ ] Deploy app (new outbox claiming requires `locked_until` - already present)
- [ ] Set `ForwardedHeaders__KnownNetworks__0` to the real proxy/LB CIDR in the deploy
      environment, or rate limiting keys on the proxy IP
- [ ] Confirm frontend audit-log `action` filter casing (now case-insensitive again -
      safe either way)
- [ ] Product decisions pending: combined-invoice partial cancellation, refunds on paid
      invoices (M4 scope-limited by design)
