using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Invoices.RecordPayment;

public sealed class RecordPaymentCommandHandler(
    IApplicationDbContext db,
    IBookingLifecycleOutbox lifecycleOutbox,
    NotificationSettings notificationSettings,
    ILogger<RecordPaymentCommandHandler> logger)
    : ICommandHandler<RecordPaymentCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(RecordPaymentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        ErrorOr<Updated> outcome = Error.Conflict(
            ErrorCodes.ConcurrentModification,
            "The invoice was modified by another request. Please retry.");

        var committed = await db.TryExecuteInSerializableTransactionAsync(async ct =>
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.Id, ct);
            if (invoice is null)
            {
                outcome = Error.NotFound(ErrorCodes.NotFound, "Invoice not found.");
                return;
            }

            if (invoice.Status == InvoiceStatus.Cancelled)
            {
                outcome = Error.Conflict(ErrorCodes.Cancelled, "Cannot record a payment against a cancelled invoice.");
                return;
            }

            if (invoice.Status == InvoiceStatus.Paid)
            {
                outcome = Error.Conflict(ErrorCodes.AlreadyPaid, "Invoice has already been fully paid.");
                return;
            }

            var alreadyPaid = await db.Payments
                .Where(p => p.InvoiceId == command.Id)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

            var remaining = invoice.Total - alreadyPaid;
            if (request.Amount > remaining)
            {
                outcome = Error.Validation(ErrorCodes.ExceedsBalance, $"Payment amount exceeds the outstanding balance of {remaining:F2}.");
                return;
            }

            await db.Payments.AddAsync(new Payment
            {
                InvoiceId = command.Id,
                Amount = request.Amount,
                PaidOn = request.PaidOn ?? DateOnly.FromDateTime(DateTime.UtcNow.Date),
                Method = request.Method,
                Reference = request.Reference,
                Notes = request.Notes,
            }, ct);

            var totalPaid = alreadyPaid + request.Amount;

            if (totalPaid >= invoice.Total)
            {
                invoice.Status = InvoiceStatus.Paid;
            }

            var depositJustCovered = invoice.DepositAmount is not null
                && invoice.DepositPaidAt is null
                && totalPaid >= invoice.DepositAmount.Value;

            if (depositJustCovered)
            {
                invoice.DepositPaidAt = DateTime.UtcNow;
            }

            // No deposit policy on this invoice: preserve the pre-deposit behaviour of confirming
            // pending bookings as soon as any payment is recorded. When a deposit policy applies,
            // only confirm once the deposit threshold is actually reached.
            var shouldConfirmPendingBookings = invoice.DepositAmount is null || depositJustCovered;

            if (shouldConfirmPendingBookings)
            {
                var pendingBookings = await db.Bookings
                    .Where(b => b.InvoiceId == command.Id && b.Status == BookingStatus.Pending)
                    .ToListAsync(ct);

                if (pendingBookings.Count > 0)
                {
                    var company = await db.Companies
                        .AsNoTracking()
                        .FirstAsync(c => c.Id == invoice.CompanyId, ct);

                    var customer = await db.Customers
                        .AsNoTracking()
                        .Where(c => c.Id == invoice.CustomerId)
                        .Select(c => new
                        {
                            c.Email,
                            Name = c.CustomerType == CustomerType.Company ? c.CompanyName : c.FullName,
                        })
                        .FirstAsync(ct);

                    var rangeIds = pendingBookings.Select(b => b.ShootingRangeId).Distinct().ToList();
                    var rangeNames = await db.ShootingRanges
                        .AsNoTracking()
                        .Where(r => rangeIds.Contains(r.Id))
                        .ToDictionaryAsync(r => r.Id, r => r.Name, ct);

                    foreach (var booking in pendingBookings)
                    {
                        booking.Status = BookingStatus.Confirmed;
                        booking.ConfirmedAt = DateTime.UtcNow;

                        var rangeName = rangeNames[booking.ShootingRangeId];

                        if (string.IsNullOrWhiteSpace(customer.Email))
                        {
                            if (logger.IsEnabled(LogLevel.Information))
                            {
                                logger.LogInformation(
                                    "Skipped BookingConfirmed event for booking {BookingNumber}: customer has no email.",
                                    booking.BookingNumber);
                            }

                            continue;
                        }

                        var links = BookingCalendarLinkBuilder.Build(
                            notificationSettings.PublicBaseUrl,
                            booking.CalendarToken,
                            new BookingIcsBuilder.BookingIcsData(
                                booking.Id,
                                booking.BookingNumber,
                                booking.PackageName,
                                rangeName,
                                booking.BookingDate,
                                booking.StartTime,
                                booking.EndTime,
                                booking.ShooterCount),
                            new BookingIcsBuilder.CompanyIcsData(
                                company.Name,
                                company.AddressLine1,
                                company.AddressLine2,
                                company.City,
                                company.Province,
                                company.PostalCode));

                        lifecycleOutbox.Add(
                            OutboxMessageTypes.BookingConfirmed,
                            company,
                            booking,
                            rangeName,
                            customer.Email,
                            customer.Name,
                            icsUrl: links.IcsUrl,
                            googleCalendarUrl: links.GoogleCalendarUrl,
                            depositAmount: invoice.DepositAmount,
                            depositDueAt: invoice.DepositDueAt,
                            invoiceNumber: invoice.InvoiceNumber);
                    }
                }
            }

            await db.SaveChangesAsync(ct);
            outcome = Result.Updated;
        }, cancellationToken);

        if (outcome.IsError)
        {
            return outcome.Errors;
        }

        if (!committed)
        {
            return Error.Conflict(
                ErrorCodes.ConcurrentModification,
                "The invoice was modified by another request. Please retry.");
        }

        return outcome;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "RecordPaymentCommand.NotFound";
        public const string Cancelled = "RecordPaymentCommand.Cancelled";
        public const string AlreadyPaid = "RecordPaymentCommand.AlreadyPaid";
        public const string ExceedsBalance = "RecordPaymentCommand.ExceedsBalance";
        public const string ConcurrentModification = "RecordPaymentCommand.ConcurrentModification";
    }
}
