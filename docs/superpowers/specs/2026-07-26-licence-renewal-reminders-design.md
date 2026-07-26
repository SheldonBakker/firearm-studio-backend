# Licence Renewal Reminders via Klaviyo - Design

Date: 2026-07-26
Status: Approved (design), pending implementation plan

## Purpose

Automatically remind customers when a firearm licence is approaching expiry, using tiered Klaviyo events (90/60/30 days before expiry, plus one on expiry), and keep `LicenceStatus` in sync with the licence dates so dashboard counts stay accurate.

## Decisions (agreed with user)

- Cadence: tiered reminders at 90, 60, and 30 days before `ExpiresOn`, plus one at/after expiry.
- The job also owns status transitions: `Valid -> RenewalDue -> Expired`.
- Runs for all active companies; no per-company opt-out toggle for now.
- Approach: nightly background job + outbox + Klaviyo track events (matches the existing BookingRequested pipeline). Email content and per-tier messaging live in Klaviyo flows, not code.

## Context (existing infrastructure)

- `FirearmLicence` (`src/FirearmStudio.Domain/Entities/FirearmLicence.cs`): `ExpiresOn` (DateOnly), `RenewalDueOn` (stored computed column `expires_on - 90`), `Status` (`LicenceStatus`: Valid, RenewalDue, Expired, Unknown). Licence -> Firearm -> Customer (nullable `Customer.Email`).
- Outbox: `OutboxMessage` entity, `OutboxProcessorService` (every 30s, claims up to 20 with `FOR UPDATE SKIP LOCKED`, max 5 attempts). One existing type: `BookingRequested`, dispatched via `IBookingRequestedDispatcher`.
- Klaviyo: `IKlaviyoClient.TrackEventAsync(metricName, email, name?, properties, ct)`, configured via `KlaviyoSettings` (single global API key; all tenants share one Klaviyo account, events carry company identifiers for segmentation).
- Background job template: `MonthlyInvoiceGenerationService` (daily 02:00 UTC, migration gate, iterate active companies, `ITenantContext.BeginCompanyScope` per company).

## Components

### 1. Data model

New tenant entity `LicenceReminder` -> table `licence_reminders`:

- `id` (Guid, PK)
- `company_id` (Guid, tenant filter via `ITenantEntity`)
- `licence_id` (Guid, FK -> `firearm_licences`, cascade delete)
- `tier` (`LicenceReminderTier` enum: `Days90`, `Days60`, `Days30`, `Expired`)
- `created_at`

Unique index on `(licence_id, tier)`; this is the dedup guarantee, including under concurrent job runs. One EF migration, applied manually per repo convention.

### 2. Reminder planner (pure domain logic)

Stateless planner, no I/O, unit-testable:

- Input: licence dates/status + today (UTC date).
- Current tier: `Expired` if `today > ExpiresOn`; else `Days30` if days remaining <= 30; else `Days60` if <= 60; else `Days90` if <= 90; else none.
- Target status: `Expired` if `today > ExpiresOn`; `RenewalDue` if `today >= RenewalDueOn`; else `Valid`.
- Rules:
  - Only the current tier is ever sent. Missed earlier tiers are never backfilled (a licence entered with 45 days left gets only the 60-day reminder). At most one reminder per licence per run.
  - Licences with status `Unknown` are skipped entirely (no reminder, no status change); `Unknown` is a data-quality signal.

### 3. Nightly job: `LicenceReminderService`

New hosted service in `src/FirearmStudio.WebApi/BackgroundJobs/`, daily at 03:00 UTC (after the 02:00 invoice job), cloned from the `MonthlyInvoiceGenerationService` pattern (migration gate, loop over active companies, scope per company).

Per tenant:

1. Load licences with `ExpiresOn <= today + 90 days` and status != `Unknown`, including Firearm -> Customer.
2. For each licence:
   - Apply status transition from the planner if changed.
   - If the planner yields a tier, no `licence_reminders` row exists for `(licence, tier)`, and the customer has an email: insert the `LicenceReminder` row and an `OutboxMessage`.
   - If the customer has no email: apply the status change, skip the reminder, log at information level.
3. One `SaveChanges` per tenant: status updates, reminder log rows, and outbox messages commit atomically.

### 4. Outbox message + Klaviyo dispatch

- New constant in `OutboxMessageTypes`: `LicenceRenewalReminder`.
- Payload record `LicenceRenewalReminderPayload`: email, customer name, licence number, expires_on, days_until_expiry, tier, firearm descriptor (make/model/serial), company id, company name. Serialized with `OutboxJson.Options`.
- New `ILicenceRenewalReminderDispatcher` + implementation mirroring `BookingRequestedDispatcher`: calls `IKlaviyoClient.TrackEventAsync` with `KlaviyoSettings.LicenceRenewalMetricName` (new setting, default `"Licence Renewal Reminder"`), flattened properties for Klaviyo segmentation.
- Extend the `OutboxProcessorService` type switch to route the new type. Existing retry semantics (5 attempts, lock timeout) apply unchanged; delivery to Klaviyo is at-least-once.

### 5. Klaviyo side (ops, not code)

Create a flow triggered by the `Licence Renewal Reminder` metric, branching/filtering on the `tier` property for per-tier messaging. Out of scope for this repo beyond documenting the event contract.

## Error handling

- Job level: per-company try/catch (log and continue to next tenant), migration gate skips the run when migrations are pending, consistent with existing jobs.
- Dispatch level: outbox retries up to 5 attempts with error recorded on the message; permanent failures remain visible in `outbox_messages`.
- Duplicate protection: unique `(licence_id, tier)` index; a violated insert means another run already handled it.
- Renewals: a licence renewal is modelled as a new `FirearmLicence` row (existing model), so new rows naturally get fresh reminder tracking. If an existing row's `ExpiresOn` is edited in place, old tier rows persist and reminders resume only when the licence re-enters a window; accepted behaviour.

## Testing

- New minimal xUnit test project (first in the repo) covering the planner: tier boundaries (91/90/61/60/31/30/1/0/-1 days remaining), status transitions, `Unknown` skip, none-due case.
- Job + dispatcher verified end-to-end against a local run (background service triggering, outbox row creation, dispatch path with a stubbed/sandbox Klaviyo key).

## Out of scope

- Per-company opt-out toggle (add later if requested).
- Per-tenant Klaviyo accounts/keys.
- Building the Klaviyo flows themselves.
- SMS or channels other than Klaviyo email flows.
