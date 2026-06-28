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

        if (request.Amount <= 0)
        {
            return Error.Validation(ErrorCodes.InvalidAmount, "Payment amount must be greater than zero.");
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
        if (invoice is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Error.Conflict(ErrorCodes.Cancelled, "Cannot record a payment against a cancelled invoice.");
        }

        var alreadyPaid = await db.Payments
            .Where(p => p.InvoiceId == command.Id)
            .SumAsync(p => p.Amount, cancellationToken);

        if (alreadyPaid >= invoice.Total)
        {
            return Error.Conflict(ErrorCodes.AlreadyPaid, "Invoice has already been fully paid.");
        }

        var remaining = invoice.Total - alreadyPaid;
        if (request.Amount > remaining)
        {
            return Error.Validation(ErrorCodes.ExceedsBalance, $"Payment amount exceeds the outstanding balance of {remaining:F2}.");
        }

        await db.Payments.AddAsync(new Payment
        {
            InvoiceId = command.Id,
            Amount = request.Amount,
            PaidOn = request.PaidOn ?? DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Method = request.Method,
            Reference = request.Reference,
            Notes = request.Notes,
        }, cancellationToken);

        if (alreadyPaid + request.Amount >= invoice.Total)
        {
            invoice.Status = InvoiceStatus.Paid;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string InvalidAmount = "RecordPaymentCommand.InvalidAmount";
        public const string NotFound = "RecordPaymentCommand.NotFound";
        public const string Cancelled = "RecordPaymentCommand.Cancelled";
        public const string AlreadyPaid = "RecordPaymentCommand.AlreadyPaid";
        public const string ExceedsBalance = "RecordPaymentCommand.ExceedsBalance";
    }
}
