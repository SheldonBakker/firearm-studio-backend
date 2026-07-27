# Booking Confirmations, Deposits, and Range Check-In - Design

Date: 2026-07-27
Status: Approved (design), with implementation plan at `docs/superpowers/plans/2026-07-27-booking-lifecycle.md`

## Purpose

Three related upgrades to the bookings area:

1. **Confirmations and reminders**: email the customer when a booking is confirmed and again 24 hours before the session, with an "add to calendar" (ICS) link, via the existing outbox -> Klaviyo pipeline.
2. **Deposits**: a per-company deposit policy (none / fixed amount / percentage) for public bookings, tracked against the booking invoice with manual EFT payment, auto-confirmation when the deposit is recorded, and auto-cancellation of unpaid public bookings after a payment window. No payment gateway yet.
3. **Check-in and shooter register**: staff check in a booking on arrival and capture each shooter (name, ID number, licence number, firearm, calibre), producing a filterable, CSV-exportable attendance register per range.

## Decisions (agreed with user)

- Email only for now, through the existing Klaviyo pipeline. No SMS/WhatsApp vendor; dispatchers stay behind interfaces so a second channel can be added later.
- No payment gateway yet. Deposits are paid by EFT against the banking details already shown on the public confirmation; staff record the payment as today. A gateway (PayFast or similar) can be added later behind the same invoice/payment model.
- Deposit policy is configurable per company: mode (None / FixedAmount / Percentage), value, and a payment window in hours after which unpaid public bookings are cancelled automatically.
- Full shooter register at check-in (per-shooter identity, licence, and firearm details), not just an arrived flag. Walk-ins without a booking are out of scope for this iteration.

## Context (existing infrastructure)

- `Booking` (`src/FirearmStudio.Domain/Entities/Booking.cs`): status machine Pending -> Confirmed -> Completed / Cancelled / NoShow; `BookingDate` (DateOnly) + `StartTime`/`EndTime` (TimeOnly) in local South African time; `InvoiceId` links to a combined cart invoice for public bookings.
- Public flow: `CreatePublicBookingCommandHandler` creates Pending bookings plus one combined `Invoice` (Kind = Booking, Status = Sent) inside a serializable transaction, then enqueues a `BookingRequested` outbox message. Lane occupancy counts Pending and Confirmed bookings, so cancelling a booking frees its slot.
- Staff flow: `ConfirmBookingCommandHandler` moves Pending -> Confirmed (creating a per-booking invoice if none exists). `RecordPaymentCommandHandler` records payments against invoices.
- Outbox: `OutboxMessage` + `OutboxProcessorService` (30s tick, claim batch of 20, max 5 attempts). Existing types: `BookingRequested`, `LicenceRenewalReminder`, each with a payload record and an `I...Dispatcher` implementation calling `IKlaviyoClient.TrackEventAsync`.
- Background job template: `MonthlyInvoiceGenerationService` (migration gate, iterate active companies, `ITenantContext.BeginCompanyScope` per company).
- Klaviyo events cannot carry attachments, so the ICS file must be a link, not an attachment.

## Components

### 1. Notification events (confirmations, reminders, cancellations)

New outbox message types in `OutboxMessageTypes`: `BookingConfirmed`, `BookingReminder`, `BookingCancelled`. Each gets a payload record (mirroring `BookingRequestedPayload`: customer email/name, booking fields, range name, `CompanyNotificationData`, and calendar link URLs) and a dispatcher (mirroring `BookingRequestedDispatcher`) with new `KlaviyoSettings` metric names: `BookingConfirmedMetricName` ("Booking Confirmed"), `BookingReminderMetricName` ("Booking Reminder"), `BookingCancelledMetricName` ("Booking Cancelled"). `OutboxProcessorService`'s type switch routes the new types. Email content lives in Klaviyo flows, not code.

Enqueue points for `BookingConfirmed`: every Pending -> Confirmed transition. That is `ConfirmBookingCommandHandler`, `CreateBookingCommandHandler` when `ConfirmImmediately` is true, and the deposit auto-confirm path (component 4). Skip with an information log when the customer has no email.

`BookingCancelled` is enqueued by the deposit expiry job (component 5) and by `CancelBookingCommandHandler` so customers hear about staff cancellations too.

### 2. ICS calendar link

- New `Booking.CalendarToken`: unique, URL-safe, 32+ chars of cryptographic randomness, generated at booking creation. Unique index. Backfill for existing rows in the migration.
- New anonymous endpoint `GET /api/v1/public/bookings/{token}/calendar.ics` returning `text/calendar` with a single VEVENT: DTSTART/DTEND with `TZID=Africa/Johannesburg`, SUMMARY (package + range), LOCATION (company address), DESCRIPTION (booking number, shooter count), UID (booking id). Returns 404 for unknown tokens and 410-style behaviour (404) for cancelled bookings.
- The API-key middleware exempts this route (like `/health`); the token is the credential. The `public` rate-limit policy applies.
- New `NotificationSettings` options class with `PublicBaseUrl` (e.g. the production API origin) used to build absolute `ics_url` and a Google Calendar template URL (`https://calendar.google.com/calendar/render?action=TEMPLATE&...`) into every booking notification payload, so Klaviyo templates can render "Add to calendar" buttons.

### 3. Booking reminder job

New hosted service `BookingReminderService` (hourly tick, migration gate, iterate active companies like the invoice job). Per tenant: load Confirmed bookings where `ReminderSentAt` is null and the session start (BookingDate + StartTime, Africa/Johannesburg -> UTC) is within the next 24 hours but still in the future; for each, set new column `Booking.ReminderSentAt` and enqueue a `BookingReminder` outbox message in the same `SaveChanges`. `ReminderSentAt` is the idempotency guard; bookings created inside the 24h window get their reminder on the next tick.

### 4. Deposit policy and tracking

Company additions: `DepositMode` (new PG enum: None / FixedAmount / Percentage, default None), `DepositValue` numeric(12,2) default 0, `DepositWindowHours` int default 48 (clamped 1-336). Exposed via GET/PATCH `/company` and, read-only, in the public booking options response so the wizard can warn up front.

Invoice additions (used when Kind = Booking): `DepositAmount` numeric(12,2) nullable, `DepositDueAt` (UTC, nullable), `DepositPaidAt` (UTC, nullable). `CreatePublicBookingCommandHandler` computes the deposit from the company policy (FixedAmount = min(value, total); Percentage = round(total * value / 100, 2)) and stamps `DepositDueAt = now + DepositWindowHours`. Staff-created bookings never get a deposit. `PublicBookingResponse` gains `DepositAmount` and `DepositDueAt`, and the `BookingRequested` Klaviyo payload gains the same so the first email states the deposit and deadline (payment reference = invoice number).

`RecordPaymentCommandHandler` extension: after saving a payment, if the invoice has an unpaid deposit and the payment total now covers it, set `DepositPaidAt`, flip all Pending bookings on that invoice to Confirmed (with `ConfirmedAt`), and enqueue `BookingConfirmed` messages, all in one `SaveChanges`.

When `DepositMode` is None, public bookings keep today's behaviour (Pending until staff confirm; no auto-cancel).

### 5. Deposit expiry job

New hosted service `BookingDepositExpiryService` (hourly tick, migration gate, per-tenant scope). Finds Pending public-source bookings whose invoice has `DepositAmount` set, `DepositPaidAt` null, and `DepositDueAt < now`: sets bookings to Cancelled (+ `CancelledAt`), cancels the invoice when it has no recorded payments (partial payments leave the invoice open and log a warning for staff follow-up), and enqueues `BookingCancelled` messages. Cancelling frees the lane because occupancy only counts Pending/Confirmed.

### 6. Check-in and shooter register

New tenant entity `BookingAttendee` -> table `booking_attendees`: `booking_id` (FK, cascade delete), `full_name` (required), `id_number` (required; SA ID or passport, max 20), `licence_number` (nullable), `firearm_make_model` (nullable), `firearm_serial_number` (nullable), `calibre` (nullable), `firearm_origin` (new PG enum: Own / RangeRental, default Own), `signed_indemnity` bool default false, `notes` (nullable). Index on `booking_id`. New `Booking.CheckedInAt` (UTC, nullable).

Endpoints (BookingsController + a new AttendeesController or route group, staff+ writes):

- `POST /bookings/{id}/check-in` with an attendee list: allowed only for Confirmed bookings dated today (Africa/Johannesburg); sets `CheckedInAt` and inserts attendees. Attendee count may differ from `ShooterCount` (the register records who actually shot).
- `POST /bookings/{id}/attendees`, `PATCH /attendees/{id}`, `DELETE /attendees/{id}` (delete manager+) for late arrivals and corrections.
- `GET /bookings/{id}/attendees` (staff+; attendee ID numbers are POPIA personal information, so viewers do not get attendee reads).
- `GET /register?dateFrom&dateTo&rangeId` (staff+, paginated) and `GET /register/export` (manager+, `text/csv`): one row per attendee joined to booking/range/customer with date, time, range, booking number, shooter name, ID number, licence number, firearm, serial, calibre, origin, indemnity, checked-in time.

`CompleteBookingCommandHandler` is unchanged; completing remains a manual staff action.

### 7. Frontend (firearm-studio-frontend)

- Settings (admin): deposit policy card (mode select, value, window hours) on the existing company settings form.
- Public wizard: show deposit amount, deadline, and payment reference on the review and confirmation steps when the company has a deposit policy.
- Booking detail: deposit status (due / paid / expired) derived from the invoice fields; check-in panel with an attendee form dialog (SA ID number checksum validation, Luhn over 13 digits, with passport fallback), attendee list with edit/remove.
- New `/register` route: date-range + range filter, paginated table, CSV download button. Nav visibility staff+.
- API modules extended per the existing `lib/api/<area>` pattern; `swagger.json` refreshed from the backend after the API work lands.

## Error handling

- Jobs: per-company try/catch, migration gate, same as existing jobs. Reminder and expiry idempotency come from `ReminderSentAt` / status transitions inside single `SaveChanges` calls.
- Outbox: existing at-least-once semantics, 5 attempts, errors recorded on the message.
- Check-in: state conflicts (not Confirmed, wrong day, already checked in) return ErrorOr conflicts with stable codes.
- ICS: unknown or cancelled tokens 404; endpoint stays read-only and rate-limited.

## Testing

- Extend the xUnit domain test project: deposit calculator (modes, rounding, clamp), reminder window logic (boundaries around 24h, TZ conversion), ICS builder output (escaping, TZID), SA ID checksum validator (frontend mirrors it).
- Handler-level behaviour verified end-to-end locally: confirm -> outbox row, record deposit payment -> auto-confirm + outbox, expiry job -> cancellations, check-in flows, register CSV shape.

## Out of scope

- Payment gateway integration (PayFast/Yoco/Paystack) - future work behind the same invoice/payment model.
- SMS or WhatsApp channels.
- Walk-in shooters without a booking.
- Building the Klaviyo flows themselves (event contracts documented in the plan).
- Per-company timezone configuration (Africa/Johannesburg is assumed, kept as a single named constant).
