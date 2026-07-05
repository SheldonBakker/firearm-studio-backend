using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.CreateBooking;

public sealed class CreateBookingCommandHandler(IApplicationDbContext db, ITenantContext tenant)
    : ICommandHandler<CreateBookingCommand, ErrorOr<BookingResponse>>
{
    public async Task<ErrorOr<BookingResponse>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        ErrorOr<Booking> outcome = Error.Conflict(
            BookingCreation.ErrorCodes.SlotContention,
            "The slot could not be reserved due to concurrent bookings. Please retry.");

        var committed = await db.TryExecuteInSerializableTransactionAsync(async ct =>
        {
            var customerExists = await db.Customers
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.CustomerId && c.IsActive, ct);

            if (!customerExists)
            {
                outcome = Error.NotFound(ErrorCodes.CustomerNotFound, "Customer not found.");
                return;
            }

            var result = await BookingCreation.AddBookingAsync(
                db,
                new BookingCreation.SlotRequest(
                    request.ShootingRangeId,
                    request.PackageId,
                    request.CustomerId,
                    request.BookingDate,
                    request.StartTime,
                    request.ShooterCount,
                    request.Notes,
                    BookingSource.Staff),
                ct);

            if (result.IsError)
            {
                outcome = result;
                return;
            }

            var booking = result.Value;

            if (request.ConfirmImmediately)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAt = DateTime.UtcNow;

                var company = await db.Companies
                    .AsNoTracking()
                    .Where(c => c.Id == tenant.CompanyId)
                    .Select(c => new { c.VatNumber, c.DueDays })
                    .FirstAsync(ct);

                var rangeName = await db.ShootingRanges
                    .AsNoTracking()
                    .Where(r => r.Id == booking.ShootingRangeId)
                    .Select(r => r.Name)
                    .FirstAsync(ct);

                var packageItems = await db.PackageItems
                    .AsNoTracking()
                    .Where(i => i.PackageId == booking.PackageId)
                    .OrderBy(i => i.SortOrder)
                    .ThenBy(i => i.Id)
                    .Select(i => new BookingInvoiceFactory.IncludedItem(i.Description, i.Quantity))
                    .ToListAsync(ct);

                var invoice = BookingInvoiceFactory.Create(
                    booking, company.VatNumber, company.DueDays, rangeName, packageItems);

                db.Invoices.Add(invoice);
                booking.InvoiceId = invoice.Id;
            }

            await db.SaveChangesAsync(ct);
            outcome = booking;
        }, cancellationToken);

        if (outcome.IsError)
        {
            return outcome.Errors;
        }

        if (!committed)
        {
            return Error.Conflict(
                BookingCreation.ErrorCodes.SlotContention,
                "The slot could not be reserved due to concurrent bookings. Please retry.");
        }

        return await db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == outcome.Value.Id)
            .Select(BookingResponse.QueryProjection)
            .FirstAsync(cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string CustomerNotFound = "CreateBookingCommand.CustomerNotFound";
    }
}
