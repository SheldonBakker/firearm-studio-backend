# Resend template provisioning

Creates and updates the 12 transactional email templates on Resend. Safe to re-run; each run updates existing templates and re-publishes them.

## Prerequisites

- `curl` and `jq` on your PATH
- A Resend API key with template write access

## Usage

```bash
RESEND_API_KEY=re_... ./tools/resend-templates/provision.sh
```

Provision a single template only:

```bash
RESEND_API_KEY=re_... ./tools/resend-templates/provision.sh --only invoice-sent
```

Preview request bodies without calling the API:

```bash
RESEND_API_KEY=x ./tools/resend-templates/provision.sh --dry-run
```

Override the API base URL (for testing against a proxy):

```bash
RESEND_BASE_URL=https://your-proxy.example.com RESEND_API_KEY=re_... ./tools/resend-templates/provision.sh
```

The key is never printed or logged.

## How it works

For each entry in `manifest.json` the script:

1. Runs a placeholder consistency check (declared variables vs. `{{{VAR}}}` occurrences in html/ and text/).
2. Calls `GET /templates` to find an existing template by alias.
3. Creates (`POST /templates`) if absent, updates (`PATCH /templates/{id}`) if present.
4. Publishes (`POST /templates/{id}/publish`).

Any non-2xx response prints the status and response body, then exits 1.

## Fragment contracts

The adapter in `src/FirearmStudio.Infrastructure/Services/ResendEmailFragments.cs` produces these fragments. The templates insert them as raw HTML or plain text.

### LINES_HTML / LINES_TEXT (invoice-sent)

`LINES_HTML` is injected directly into the invoice table body. Each item must be a `<tr>` with exactly four `<td>` elements: Description, Qty, Unit price, Total. Every dynamic value inside the `<td>` elements must be HTML-encoded.

Example row shape:

```html
<tr style="border-top:1px solid #e5e7eb;">
  <td style="padding:10px 12px;font-size:14px;color:#111827;">Range session - Standard package</td>
  <td style="padding:10px 12px;font-size:14px;color:#374151;text-align:right;">1</td>
  <td style="padding:10px 12px;font-size:14px;color:#374151;text-align:right;">R 350.00</td>
  <td style="padding:10px 12px;font-size:14px;color:#111827;text-align:right;">R 350.00</td>
</tr>
```

`LINES_TEXT` is plain text, one line per item:

```
Range session - Standard package  x1  R 350.00  R 350.00
```

### SESSIONS_HTML / SESSIONS_TEXT (booking-requested)

`SESSIONS_HTML` is injected into a container `<td>` and must be self-contained block markup. Each session is rendered as a bordered block with: booking number, date, start-end time, range name, package name and price, deposit amount and due date, and Add-to-calendar links (ICS and Google Calendar). Omit a calendar link button when its URL is empty. Every dynamic value must be HTML-encoded.

Example session block shape:

```html
<div style="border:1px solid #e5e7eb;border-radius:6px;padding:16px;margin-bottom:12px;">
  <p style="margin:0 0 8px;font-size:13px;font-weight:600;color:#1f2937;">BK-001 - 05 Oct 2026</p>
  <p style="margin:0 0 4px;font-size:13px;color:#374151;">09:00 - 11:00 | Main Range | Standard Package - R 350.00</p>
  <p style="margin:0 0 12px;font-size:13px;color:#374151;">Deposit: R 100.00 due 10 Oct 2026</p>
  <a href="https://..." style="...">Add to calendar (ICS)</a>
  <a href="https://..." style="...">Add to Google Calendar</a>
</div>
```

`SESSIONS_TEXT` is plain text, one block per session:

```
BK-001 - 05 Oct 2026
09:00 - 11:00 | Main Range | Standard Package - R 350.00
Deposit: R 100.00 due 10 Oct 2026
ICS: https://...
Google Calendar: https://...
```

### CALENDAR_HTML / CALENDAR_TEXT (booking-confirmed, booking-reminder)

`CALENDAR_HTML` is a block of one or two anchor buttons. Omit a button when its URL is empty. Every URL must be HTML-encoded for attribute context.

Example:

```html
<p style="margin:0 0 16px;">
  <a href="https://..." style="display:inline-block;padding:10px 20px;background:#1f2937;color:#ffffff;text-decoration:none;border-radius:4px;font-size:14px;margin-right:8px;">Add to calendar</a>
  <a href="https://..." style="display:inline-block;padding:10px 20px;background:#1f2937;color:#ffffff;text-decoration:none;border-radius:4px;font-size:14px;">Add to Google Calendar</a>
</p>
```

`CALENDAR_TEXT` is plain text links:

```
Add to calendar: https://...
Add to Google Calendar: https://...
```
