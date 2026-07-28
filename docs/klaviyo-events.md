# Klaviyo booking event contract

This documents the four booking-lifecycle metrics sent to Klaviyo, and the exact flattened
property names each one carries on the wire, so marketing can build flows/segments without
reading the C# dispatcher code.

Source of truth (read this if anything below looks stale):
- `src/FirearmStudio.Application/Bookings/BookingRequestedNotifier.cs` - property building and the
  flattening helper (`Flatten`, `BuildCompanyProperties`).
- `src/FirearmStudio.Application/Bookings/BookingRequestedDispatcher.cs` and
  `BookingRequestedPayload.cs` - Booking Requested outbox payload.
- `src/FirearmStudio.Application/Bookings/BookingLifecycleDispatcher.cs` and
  `BookingLifecyclePayload.cs` - Booking Confirmed / Reminder / Cancelled outbox payload.
- `src/FirearmStudio.Application/Model/Options/KlaviyoSettings.cs` - configurable metric name
  strings (defaults shown below; a company could theoretically override these via config).
- `src/FirearmStudio.Infrastructure/Services/KlaviyoClient.cs` - how the event and profile are
  actually posted to Klaviyo's Events API.

## How profiles are identified

Every event is sent via `KlaviyoClient.TrackEventAsync(metricName, email, name, properties)`,
which builds the Klaviyo profile from the booking customer's `email` and (optional) `name`:

```
profile.data.attributes.email = <customer email>
profile.data.attributes.properties.full_name = <customer name>   // only if name is present
```

Klaviyo profiles are keyed on **email**. There is no external ID or phone sent. If the customer
has no email on file, the event is never dispatched (see "Skip conditions" below) - so a
customer profile is never created from a booking event alone.

`name` is the customer's full name for individuals, or the company name for company-type
customers (`CustomerType.Company ? CompanyName : FullName`), resolved at the point each event
fires.

## Flattening rules

Nested objects are flattened into the top-level `properties` bag using an underscore separator,
e.g. `company.bank_name` becomes `company_bank_name`. This is so segments, flow filters and
conditional content in Klaviyo can address any field directly.

Arrays are **not** flattened - the `bookings` array on the Booking Requested event stays an array
of objects, each with its own unprefixed keys (e.g. `bookings[0].ics_url`, not
`bookings_0_ics_url`). This is deliberate: flattening an array would produce unbounded,
unaddressable keys, and the array form is what email templates loop over.

## Skip conditions (all four metrics)

An event is **not** sent - not even with a null-ish payload - when the customer has no email on
file. This is checked at the point the outbox message is created (or, for Booking Requested, at
dispatch time) and logged as a warning/info; no Klaviyo API call happens and no profile is
touched.

---

## 1. Booking Requested

- **Metric name:** `Booking Requested` (`KlaviyoSettings.BookingRequestedMetricName`)
- **Fires:** once per public multi-session booking cart, when a member of the public submits a
  booking request via the public booking endpoint (`CreatePublicBookingCommandHandler`). One
  event covers the whole cart/invoice, not one event per session.
- **Never fires for staff-created bookings** (those go through Booking Confirmed instead).

### Properties

| Property | Type | Description |
|---|---|---|
| `invoice_id` | GUID | Combined invoice ID covering every session in the cart. |
| `invoice_number` | string | Combined invoice number. |
| `session_count` | integer | Number of booking sessions in the cart. |
| `subtotal` | decimal | Invoice subtotal (excl. VAT). |
| `vat_amount` | decimal | VAT amount. |
| `total` | decimal | Invoice total (incl. VAT). |
| `bookings` | array of objects | One entry per session; see below. Not flattened. |
| `company_id` | GUID | See "Company properties" below. |
| `company_name` | string, nullable | |
| `company_registration_number` | string, nullable | |
| `company_vat_number` | string, nullable | |
| `company_email` | string, nullable | |
| `company_phone` | string, nullable | |
| `company_address_line1` | string, nullable | |
| `company_address_line2` | string, nullable | |
| `company_city` | string, nullable | |
| `company_province` | string, nullable | |
| `company_postal_code` | string, nullable | |
| `company_bank_name` | string, nullable | |
| `company_bank_account_holder` | string, nullable | |
| `company_bank_account_number` | string, nullable | |
| `company_bank_branch_code` | string, nullable | |
| `company_bank_account_type` | string, nullable | |
| `company_bank_swift_code` | string, nullable | |

### `bookings[]` entry properties

Each object in the `bookings` array carries:

| Property | Type | Description |
|---|---|---|
| `booking_id` | GUID | |
| `booking_number` | string | |
| `status` | string | Enum name: `Pending`, `Confirmed`, `Completed`, `Cancelled`, or `NoShow`. Always `Pending` at request time. |
| `booking_date` | string | `yyyy-MM-dd`, Africa/Johannesburg local date. |
| `start_time` | string | `HH:mm`, local time. |
| `end_time` | string | `HH:mm`, local time. |
| `range_name` | string | |
| `package_name` | string | |
| `package_price` | decimal | |
| `ics_url` | string, nullable | Public "add to calendar" .ics download link. |
| `google_calendar_url` | string, nullable | Prefilled Google Calendar event link. |
| `deposit_amount` | decimal, nullable | See "Deposit fields" below. |
| `deposit_due_at` | ISO 8601 UTC timestamp, nullable | See "Deposit fields" below. |

**`ics_url` / `google_calendar_url`:** both null together whenever the booking's session could
not be matched back to a persisted `Booking` row when the outbox message was built (an edge case
guarded in `BookingRequestedOutbox`), or whenever `NotificationSettings.PublicBaseUrl` is not
configured - `BookingCalendarLinkBuilder.Build` returns null links rather than emitting broken
relative URLs.

**Deposit fields:** `deposit_amount` and `deposit_due_at` are the same value repeated on every
session in the cart (the deposit applies to the combined invoice, not per-session). Both are
null when the company has no deposit policy configured (`DepositCalculator.Calculate` returns
null for `DepositMode.None`); otherwise `deposit_amount` is the calculated deposit and
`deposit_due_at` is `now + company.DepositWindowHours` (UTC) at the time the request was
submitted.

---

## 2. Booking Confirmed

- **Metric name:** `Booking Confirmed` (`KlaviyoSettings.BookingConfirmedMetricName`)
- **Fires** on every path that transitions a booking into `Confirmed` status with a customer
  email on file:
  - Staff creates a booking with "confirm immediately" set (`CreateBookingCommandHandler`).
  - Staff explicitly confirms a pending booking (`ConfirmBookingCommandHandler`).
  - A payment is recorded that covers the invoice's deposit threshold (or the invoice has no
    deposit policy and any payment is recorded), which auto-confirms every pending booking on
    that invoice (`RecordPaymentCommandHandler`).
- One event per booking (a multi-session cart that gets confirmed via payment fires one event
  per session confirmed, not one for the invoice).

## 3. Booking Reminder

- **Metric name:** `Booking Reminder` (`KlaviyoSettings.BookingReminderMetricName`)
- **Fires** from an hourly background job (`BookingReminderService`) that looks at all
  `Confirmed` bookings with `ReminderSentAt` unset, dated within +/-1 day of "today"
  (Africa/Johannesburg), and queues a reminder once `BookingReminderPlanner.IsReminderDue`
  says it's due (time-before-session based). `ReminderSentAt` is stamped the first time a
  booking is evaluated as due, whether or not an email is actually sent, so a booking is never
  re-evaluated on a later tick.

## 4. Booking Cancelled

- **Metric name:** `Booking Cancelled` (`KlaviyoSettings.BookingCancelledMetricName`)
- **Fires** on:
  - Staff or customer-initiated cancellation of a `Pending` or `Confirmed` booking
    (`CancelBookingCommandHandler`).
  - Automatic cancellation of a `Pending` public booking whose deposit was never paid by
    `DepositDueAt` (`BookingDepositExpiryService`, hourly job).

### Properties (Confirmed / Reminder / Cancelled - shared shape)

Unlike Booking Requested, these three metrics carry one flat property set per event (one booking,
one event) rather than a `bookings` array:

| Property | Type | Description |
|---|---|---|
| `booking_id` | GUID | |
| `booking_number` | string | |
| `booking_date` | string | `yyyy-MM-dd`, Africa/Johannesburg local date. |
| `start_time` | string | `HH:mm`, local time. |
| `end_time` | string | `HH:mm`, local time. |
| `range_name` | string, nullable | Null only for Booking Cancelled when the booking's shooting range row could not be found (dangling FK data-quality case, logged as a warning). Always present for Confirmed and Reminder. |
| `package_name` | string | |
| `package_price` | decimal | |
| `shooter_count` | integer | |
| `ics_url` | string, nullable | See below. |
| `google_calendar_url` | string, nullable | See below. |
| `deposit_amount` | decimal, nullable | See below. |
| `deposit_due_at` | ISO 8601 UTC timestamp, nullable | See below. |
| `invoice_number` | string, nullable | Null if the booking has no invoice yet, or the invoice lookup returned nothing. |
| `company_id` | GUID | Same company property set as Booking Requested (see above), flattened the same way: `company_name`, `company_email`, `company_bank_name`, etc. |

**`ics_url` / `google_calendar_url`:**
- **Booking Cancelled:** always both null. A cancelled booking has nothing to add to a calendar,
  so the cancellation handlers pass `icsUrl: null, googleCalendarUrl: null` explicitly rather
  than building links.
- **Booking Confirmed / Booking Reminder:** built from `BookingCalendarLinkBuilder.Build`; both
  null together only if `NotificationSettings.PublicBaseUrl` is not configured.

**`deposit_amount` / `deposit_due_at`:**
- **Booking Confirmed:** null when confirmed by staff directly (`CreateBookingCommandHandler`,
  `ConfirmBookingCommandHandler` always pass `depositAmount: null, depositDueAt: null` - staff
  confirmation bypasses any deposit policy). Populated from the invoice's deposit fields only
  when the booking was auto-confirmed by `RecordPaymentCommandHandler` reaching the deposit
  threshold on a public booking's invoice.
- **Booking Reminder:** always null. The reminder job never populates these fields.
- **Booking Cancelled:** always null. Cancellation never populates these fields (there is no
  outstanding deposit to communicate once a booking is cancelled).

---

## Company properties (all four metrics)

`BookingRequestedNotifier.BuildCompanyProperties` builds the same shape for every metric, flattened
under the `company_` prefix:

| Property | Type |
|---|---|
| `company_id` | GUID |
| `company_name` | string, nullable |
| `company_registration_number` | string, nullable |
| `company_vat_number` | string, nullable |
| `company_email` | string, nullable |
| `company_phone` | string, nullable |
| `company_address_line1` | string, nullable |
| `company_address_line2` | string, nullable |
| `company_city` | string, nullable |
| `company_province` | string, nullable |
| `company_postal_code` | string, nullable |
| `company_bank_name` | string, nullable |
| `company_bank_account_holder` | string, nullable |
| `company_bank_account_number` | string, nullable |
| `company_bank_branch_code` | string, nullable |
| `company_bank_account_type` | string, nullable |
| `company_bank_swift_code` | string, nullable |

All fields except `company_id` come straight off the `Company` entity and are only null if that
column is null in the database - they are not derived or conditional on booking state.
