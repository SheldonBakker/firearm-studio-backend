using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Invoices.RecordPayment;

public sealed class RecordPaymentCommandHandler(IApplicationDbContext db)
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

            if (alreadyPaid + request.Amount >= invoice.Total)
            {
                invoice.Status = InvoiceStatus.Paid;
            }

            var pendingBookings = await db.Bookings
                .Where(b => b.InvoiceId == command.Id && b.Status == BookingStatus.Pending)
                .ToListAsync(ct);

            foreach (var booking in pendingBookings)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAt = DateTime.UtcNow;
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
