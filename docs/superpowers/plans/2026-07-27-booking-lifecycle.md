# Booking Confirmations, Deposits, and Range Check-In Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Booking confirmation/reminder/cancellation emails with an ICS calendar link, a per-company deposit policy (manual EFT) with auto-confirm and auto-cancel, and a check-in flow that produces a per-shooter attendance register with CSV export.

**Architecture:** Three new outbox message types ride the existing outbox -> Klaviyo pipeline. Two new hourly hosted services (reminders, deposit expiry) follow the `MonthlyInvoiceGenerationService` pattern. Deposits live on the booking invoice (`DepositAmount`/`DepositDueAt`/`DepositPaidAt`) and are driven by company policy fields. Check-in adds a `booking_attendees` table plus register queries. The frontend adds settings, wizard, booking-detail, and register UI.

**Tech Stack:** Backend: .NET 10, ASP.NET Core hosted services, EF Core 10 + Npgsql (native PG enums, snake_case), xUnit, existing Klaviyo client. Frontend: React Router v7 SPA, shadcn/ui, existing `lib/api/<area>` modules.

**Spec:** `docs/superpowers/specs/2026-07-27-booking-lifecycle-design.md`

**Repos:** Backend tasks run in `firearm-studio-backend`. Frontend tasks (12-14) run in the sibling repo `../firearm-studio-frontend`.

## Global Constraints

- Never use the em-dash character anywhere (code, comments, commits, docs); use a plain hyphen.
- No Claude/AI attribution or Co-Authored-By lines in commits.
- NEVER run `dotnet ef database update` or apply migrations. The connection string in `.env` may point at production (this caused a real incident, see `docs/code-review-2026-07-25.md`). Generate migrations only; the user applies them manually.
- Work on branch `feature/booking-lifecycle` in each repo (branch off `main` before starting).
- Central package management: versions in `Directory.Packages.props`; csproj files reference packages without `Version` attributes.
- British spelling: Licence, not License.
- Build check: `dotnet build FirearmStudio.slnx` and `./scripts/check-conventions.sh`.
- Follow `CONVENTIONS.md`: one folder per operation, `ErrorOr` with nested `ErrorCodes`, validators colocated, controllers thin, `AsNoTracking` + projections for queries, cancellation tokens everywhere, handlers never assign `CompanyId`.
- Frontend: never leave comments in code, never disable or silence eslint rules, never use `any`.
- Timezone: define `SouthAfricaTimeZone` once (`TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg")`, in `FirearmStudio.Domain` or `Application` common code) and use it for every local-time conversion in this plan.

---

### Task 1: Notification payloads, dispatchers, and outbox routing

**Files:**
- Modify: `src/FirearmStudio.Application/Abstractions/OutboxMessageTypes.cs` (add `BookingConfirmed`, `BookingReminder`, `BookingCancelled`)
- Modify: `src/FirearmStudio.Application/Model/Options/KlaviyoSettings.cs` (add `BookingConfirmedMetricName` = "Booking Confirmed", `BookingReminderMetricName` = "Booking Reminder", `BookingCancelledMetricName` = "Booking Cancelled")
- Create: `src/FirearmStudio.Application/Model/Options/NotificationSettings.cs` (`PublicBaseUrl` string, empty default; register in DI/composition like `KlaviyoSettings`; add `NotificationSettings__PublicBaseUrl` to `.env.example` and README config table)
- Create: `src/FirearmStudio.Application/Bookings/BookingLifecyclePayload.cs` (shared payload record for the three new events)
- Create: `src/FirearmStudio.Application/Abstractions/IBookingLifecycleOutbox.cs` + `src/FirearmStudio.Application/Bookings/BookingLifecycleOutbox.cs`
- Create: `src/FirearmStudio.Application/Abstractions/IBookingLifecycleDispatcher.cs` + `src/FirearmStudio.Application/Bookings/BookingLifecycleDispatcher.cs`
- Modify: `src/FirearmStudio.WebApi/BackgroundJobs/OutboxProcessorService.cs` (route the three new types)
- Modify: DI registrations (wherever `IBookingRequestedOutbox`/`IBookingRequestedDispatcher` are registered)

**Interfaces:**
- `BookingLifecyclePayload`: `Email`, `FullName?`, booking snapshot (`BookingId`, `BookingNumber`, `BookingDate`, `StartTime`, `EndTime`, `RangeName`, `PackageName`, `PackagePrice`, `ShooterCount`), `IcsUrl?`, `GoogleCalendarUrl?`, deposit snapshot (`DepositAmount?`, `DepositDueAt?`, `InvoiceNumber?`), `CompanyNotificationData`.
- `IBookingLifecycleOutbox.Add(string messageType, ...)` builds the payload and adds one `OutboxMessage` per booking; callers pass preloaded company + booking + range name.
- `IBookingLifecycleDispatcher.DispatchAsync(string messageType, string payloadJson, ct)` maps type -> metric name and reuses `BookingRequestedNotifier.Flatten`/`BuildCompanyProperties`.

**Steps:**

- [ ] Branch: `git checkout -b feature/booking-lifecycle`
- [ ] Add the three type constants, metric-name settings, and `NotificationSettings`.
- [ ] Implement payload, outbox helper, dispatcher; register in DI.
- [ ] Extend the `OutboxProcessorService` switch; unknown types still throw.
- [ ] `dotnet build FirearmStudio.slnx` and `./scripts/check-conventions.sh` pass.

### Task 2: Calendar token + ICS endpoint

**Files:**
- Modify: `src/FirearmStudio.Domain/Entities/Booking.cs` (add `CalendarToken` string required, `ReminderSentAt` DateTime?, `CheckedInAt` DateTime? - all three columns in this one migration)
- Modify: `src/FirearmStudio.Infrastructure/Persistence/Configurations/BookingConfigurations.cs` (max length 64, unique index on `calendar_token`)
- Create: `src/FirearmStudio.Application/Bookings/CalendarTokenGenerator.cs` (32 bytes from `RandomNumberGenerator`, Base64Url encoded)
- Create: `src/FirearmStudio.Application/Bookings/BookingIcsBuilder.cs` (pure; VEVENT with `TZID=Africa/Johannesburg` DTSTART/DTEND, SUMMARY, LOCATION from company address, DESCRIPTION with booking number and shooter count, UID = booking id; RFC 5545 text escaping and CRLF line endings)
- Create: `src/FirearmStudio.Application/Bookings/GetBookingCalendar/GetBookingIcsQuery.cs` + handler (token lookup, tenant-filter bypass like other public reads via company scope from the booking row; 404 unknown token or Cancelled/NoShow booking)
- Modify: `src/FirearmStudio.WebApi/Controllers/PublicBookingsController.cs` or new `PublicCalendarController` with route `GET /api/v1/public/bookings/{token}/calendar.ics` (anonymous, `public` rate limit, returns `File(bytes, "text/calendar", "booking.ics")`)
- Modify: API-key middleware to exempt this route (same mechanism as `/health`)
- Modify: `BookingCreation.CreateBooking` to stamp `CalendarToken` on new bookings
- Create: migration `AddBookingLifecycleColumns` (backfills `CalendarToken` for existing rows with `gen_random_uuid()`-derived text or a raw SQL update producing unique values; do NOT apply it)
- Tests: `BookingIcsBuilderTests` (escaping, TZ, CRLF), `CalendarTokenGeneratorTests` (length, URL safety)

**Steps:**

- [ ] Entity + configuration + token generation at creation.
- [ ] ICS builder with unit tests.
- [ ] Query handler + endpoint + API-key exemption.
- [ ] Generate the migration; verify the model snapshot; do not apply.
- [ ] Build + conventions + `dotnet test` pass.

### Task 3: Enqueue confirmation and cancellation events

**Files:**
- Modify: `src/FirearmStudio.Application/Bookings/ConfirmBooking/ConfirmBookingCommandHandler.cs`
- Modify: `src/FirearmStudio.Application/Bookings/CreateBooking/CreateBookingCommandHandler.cs` (the `ConfirmImmediately` path)
- Modify: `src/FirearmStudio.Application/Bookings/CancelBooking/CancelBookingCommandHandler.cs`
- Modify: `src/FirearmStudio.Application/Bookings/BookingRequestedPayload.cs` / `BookingRequestedNotifier.cs` (add `ics_url` + `google_calendar_url` per booking, and deposit properties, to the existing BookingRequested event)

**Interfaces:** every Pending -> Confirmed transition enqueues one `BookingConfirmed` message per booking via `IBookingLifecycleOutbox`, inside the same `SaveChanges`; cancellations enqueue `BookingCancelled`. Missing customer email: log information, skip enqueue, still transition.

**Steps:**

- [ ] Load customer email + range name + company data in each handler (single queries, no N+1) and enqueue.
- [ ] Extend the BookingRequested payload with calendar URLs (built from `NotificationSettings.PublicBaseUrl` + `CalendarToken`).
- [ ] Build + conventions pass; manual check: confirm a booking locally, see the outbox row.

### Task 4: Booking reminder job

**Files:**
- Create: `src/FirearmStudio.Application/Bookings/BookingReminderPlanner.cs` (pure: given now-UTC and a booking's date/start-time, returns whether the reminder window [start - 24h, start) contains now)
- Create: `src/FirearmStudio.WebApi/BackgroundJobs/BookingReminderService.cs` (hourly `PeriodicTimer`, migration gate, iterate active companies with `BeginCompanyScope`; per tenant load Confirmed bookings with `ReminderSentAt == null` and `BookingDate` in [today - 1, today + 1] then filter with the planner; stamp `ReminderSentAt`, enqueue `BookingReminder`, one `SaveChanges` per tenant)
- Modify: WebApi composition to register the service
- Tests: `BookingReminderPlannerTests` (boundaries at exactly 24h, session already started, TZ conversion around midnight)

**Steps:**

- [ ] Planner + tests.
- [ ] Hosted service cloned from the invoice job pattern.
- [ ] Build + conventions + tests pass.

### Task 5: Company deposit policy

**Files:**
- Create: `src/FirearmStudio.Domain/Enums/DepositMode.cs` (None, FixedAmount, Percentage; map as native PG enum like existing enums)
- Modify: `src/FirearmStudio.Domain/Entities/Company.cs` (`DepositMode DepositMode = DepositMode.None`, `decimal DepositValue`, `int DepositWindowHours = 48`)
- Modify: company entity configuration (numeric(12,2); check constraints: value >= 0, percentage <= 100 when applicable, window 1-336)
- Modify: `Application/Company` contracts, PATCH handler + validator (`Optional<T>` pattern; validator mirrors the constraints)
- Modify: `GetPublicBookingOptionsQueryHandler` + `PublicBookingOptionsResponse` (expose `DepositMode`, `DepositValue`, `DepositWindowHours` read-only)
- Create: migration `AddCompanyDepositPolicy` (with the new PG enum; do not apply)

**Steps:**

- [ ] Enum, entity, configuration, migration generated.
- [ ] Contracts + validator + PATCH plumbing + public options exposure.
- [ ] Build + conventions pass.

### Task 6: Deposit stamping on public bookings + auto-confirm on payment

**Files:**
- Modify: `src/FirearmStudio.Domain/Entities/Invoice.cs` (`decimal? DepositAmount`, `DateTime? DepositDueAt`, `DateTime? DepositPaidAt`) + configuration (numeric(12,2)) + migration `AddInvoiceDepositFields` (do not apply; may be combined with Task 5's migration if executed together)
- Create: `src/FirearmStudio.Application/Bookings/DepositCalculator.cs` (pure: `(DepositMode, decimal value, decimal invoiceTotal) -> decimal?`; FixedAmount = min(value, total); Percentage = round(total * value / 100, 2, away-from-zero); None or computed 0 -> null)
- Modify: `CreatePublicBookingCommandHandler` (stamp `DepositAmount`/`DepositDueAt` on the combined invoice; include deposit in `PublicBookingResponse` and the BookingRequested payload)
- Modify: `BookingContracts.cs` (`PublicBookingResponse` + `DepositAmount`, `DepositDueAt`)
- Modify: `src/FirearmStudio.Application/Invoices/RecordPayment/RecordPaymentCommandHandler.cs` (after adding the payment: if `DepositAmount` set, `DepositPaidAt` null, and sum of payments including this one >= `DepositAmount`, set `DepositPaidAt`, confirm all Pending bookings on the invoice, enqueue `BookingConfirmed` per booking; single `SaveChanges`)
- Modify: `Application/Invoices/InvoiceContracts.cs` projections to expose the three deposit fields
- Tests: `DepositCalculatorTests` (all modes, rounding, clamp-to-total, zero)

**Steps:**

- [ ] Invoice fields + migration.
- [ ] Calculator + tests.
- [ ] Public-create and record-payment handler changes.
- [ ] Build + conventions + tests pass.

### Task 7: Deposit expiry job

**Files:**
- Create: `src/FirearmStudio.WebApi/BackgroundJobs/BookingDepositExpiryService.cs` (hourly, migration gate, per-tenant scope; find Pending Public-source bookings whose invoice has `DepositAmount` set, `DepositPaidAt` null, `DepositDueAt < now`; set Cancelled + `CancelledAt`; cancel the invoice only when it has zero payments, else log warning and leave it; enqueue `BookingCancelled` per booking; one `SaveChanges` per tenant)
- Modify: WebApi composition to register the service

**Steps:**

- [ ] Implement + register.
- [ ] Build + conventions pass; manual check with a short window locally.

### Task 8: BookingAttendee entity and check-in commands

**Files:**
- Create: `src/FirearmStudio.Domain/Enums/FirearmOrigin.cs` (Own, RangeRental; native PG enum)
- Create: `src/FirearmStudio.Domain/Entities/BookingAttendee.cs` (ITenantEntity; `BookingId`, `FullName` required max 200, `IdNumber` required max 20, `LicenceNumber?` max 50, `FirearmMakeModel?` max 200, `FirearmSerialNumber?` max 100, `Calibre?` max 50, `FirearmOrigin` default Own, `bool SignedIndemnity`, `Notes?` max 500)
- Create: entity configuration (FK cascade to bookings, index on `booking_id`) + migration `AddBookingAttendees` (do not apply)
- Create: `Application/Bookings/CheckInBooking/` (command + handler + validator): only Confirmed, `BookingDate` == today in Africa/Johannesburg, not already checked in; sets `CheckedInAt`, inserts attendees (>= 1). ErrorCodes: NotFound, NotConfirmed, WrongDay, AlreadyCheckedIn.
- Create: `Application/Bookings/AddAttendee/`, `UpdateAttendee/` (Optional<T> PATCH), `RemoveAttendee/`, `GetBookingAttendees/` operations with contracts in `BookingContracts.cs` (`AttendeeRequest`, `AttendeeResponse` with `QueryProjection`)
- Modify: `src/FirearmStudio.WebApi/Controllers/BookingsController.cs`: `POST {id}/check-in` (staff+), `POST {id}/attendees` (staff+), `GET {id}/attendees` (staff+, NOT viewer - ID numbers are POPIA personal information)
- Create: `src/FirearmStudio.WebApi/Controllers/AttendeesController.cs`: `PATCH /attendees/{id}` (staff+), `DELETE /attendees/{id}` (manager+)

**Steps:**

- [ ] Entity + enum + configuration + migration.
- [ ] Commands/queries/validators (ID number: 13-digit numeric SA ID gets Luhn-13 checksum validation; anything else accepted as passport, length-bounded).
- [ ] Controller endpoints with role gates.
- [ ] Build + conventions pass.

### Task 9: Attendance register query + CSV export

**Files:**
- Create: `Application/Bookings/GetRegister/GetRegisterQuery.cs` + handler (paginated; filters dateFrom/dateTo/rangeId; one row per attendee joined to booking + range; deterministic ordering by date, start time, booking number, attendee name)
- Create: `Application/Bookings/ExportRegister/ExportRegisterQuery.cs` + handler (same filter, no paging, returns rows for CSV; cap plus clear error above e.g. 20000 rows)
- Create: `RegisterContracts.cs` in `Application/Bookings` (`RegisterRowDto`: date, start/end, range name, booking number, customer name, attendee full name, ID number, licence number, firearm make/model, serial, calibre, origin, signed indemnity, checked-in at)
- Modify: `BookingsController` (or new `RegisterController`): `GET /register` (staff+), `GET /register/export` (manager+, `text/csv; charset=utf-8`, RFC 4180 quoting, filename `range-register-{from}-{to}.csv`)

**Steps:**

- [ ] Queries + projections + CSV writer (simple, quoted, no third-party package).
- [ ] Build + conventions pass.

### Task 10: Backend verification (verifier)

- [ ] `dotnet build FirearmStudio.slnx` (Release) with zero warnings; `./scripts/check-conventions.sh`; `dotnet test`.
- [ ] Migrations generated but NOT applied; model snapshot consistent; new tables/columns have RLS + no Data API grants per convention (mirror what existing migrations do).
- [ ] Swagger: run the API locally, export fresh `swagger.json`, copy it over `../firearm-studio-frontend/swagger.json`.
- [ ] End-to-end smoke (local DB only): public booking with deposit policy -> response carries deposit; record payment -> booking auto-confirms + outbox row; confirm/cancel -> outbox rows; ICS endpoint returns valid calendar (validate with a parser or lint); reminder + expiry services tick without error.

### Task 11: Klaviyo event contract doc

- [ ] Add `docs/klaviyo-events.md` documenting the four booking metrics and their flattened property names (from the dispatcher code), including `ics_url`, `google_calendar_url`, and deposit properties, so flows can be built without reading C#.

### Task 12: Frontend - deposit settings + public wizard

Repo: `../firearm-studio-frontend`, branch `feature/booking-lifecycle`.

**Files:**
- Modify: `app/lib/api/company/types.ts` + `company.ts` (deposit fields), `app/lib/api/public/types.ts` (options + response deposit fields)
- Modify: `app/routes/settings.tsx` (admin-only deposit policy section: mode select, value input with R/% affordance, window hours; client validation mirrors backend clamps)
- Modify: `app/components/public-booking/review-step.tsx` + `confirmation.tsx` (deposit amount, pay-by deadline, invoice number as EFT reference, reuse existing banking display)

**Steps:**

- [ ] Types + API modules.
- [ ] Settings UI + wizard UI.
- [ ] `npm run typecheck` passes; no eslint suppressions; no `any`; no comments.

### Task 13: Frontend - check-in on booking detail

**Files:**
- Modify: `app/lib/api/bookings/types.ts` + `bookings.ts` (checkedInAt, reminder fields, attendee CRUD, check-in call)
- Create: `app/lib/utils/sa-id.ts` (13-digit Luhn checksum validator; passport fallback = non-empty, <= 20 chars)
- Create: `app/components/modals/attendee-form-dialog.tsx` (full name, ID number with validation, licence number, firearm make/model, serial, calibre, origin select, indemnity checkbox, notes)
- Modify: `app/routes/booking-detail.tsx` (deposit status badge from invoice fields; check-in button enabled for Confirmed bookings today; attendee table with add/edit/remove per role; checked-in timestamp)

**Steps:**

- [ ] Validator + dialog + detail-page integration, RBAC-aware (staff+ write, manager+ delete, hidden for viewers).
- [ ] `npm run typecheck` passes.

### Task 14: Frontend - register page + final verification (verifier)

**Files:**
- Create: `app/lib/api/register/` (types + calls), `app/routes/register.tsx` (filters: date range, range select; paginated table; Export CSV button hitting the export endpoint via the authed client and triggering a download)
- Modify: `app/routes.ts`, `app/components/layout/sidebar.tsx`, `app/lib/utils/rbac.ts` (staff+ nav item)

**Steps:**

- [ ] Route + table + export.
- [ ] `npm run typecheck` and `npm run build` pass in the frontend; `dotnet build` + tests still pass in the backend.
- [ ] Walkthrough with both apps running locally: settings -> public booking with deposit -> record payment -> auto-confirm email row -> check-in with two attendees -> register shows rows -> CSV downloads.
