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

            // Load the single range with all operating hours.
            var rawRange = await db.ShootingRanges
                .AsNoTracking()
                .Where(r => r.Id == request.ShootingRangeId && r.IsActive)
                .Select(r => new
                {
                    r.Name,
                    r.LaneCount,
                    r.SlotIntervalMinutes,
                    Hours = r.OperatingHours
                        .Select(h => new { h.Day, h.OpenTime, h.CloseTime })
                        .ToList(),
                })
                .FirstOrDefaultAsync(ct);

            var rangeData = rawRange is null
                ? null
                : new BookingCreation.RangeData(
                    rawRange.Name,
                    rawRange.LaneCount,
                    rawRange.SlotIntervalMinutes,
                    rawRange.Hours
                        .Select(h => new BookingCreation.OperatingHoursEntry(h.Day, h.OpenTime, h.CloseTime))
                        .ToList());

            // Load the single package with items.
            var rawPackage = await db.Packages
                .AsNoTracking()
                .Where(p => p.Id == request.PackageId && p.IsActive)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.DurationMinutes,
                    p.MaxShooters,
                    Items = p.Items
                        .OrderBy(i => i.SortOrder)
                        .ThenBy(i => i.Id)
                        .Select(i => new BookingInvoiceFactory.IncludedItem(i.Description, i.Quantity))
                        .ToList(),
                })
                .FirstOrDefaultAsync(ct);

            var packageData = rawPackage is null
                ? null
                : new BookingCreation.PackageData(
                    rawPackage.Name,
                    rawPackage.Price,
                    rawPackage.DurationMinutes,
                    rawPackage.MaxShooters,
                    rawPackage.Items);

            // Load occupancy windows for the (range, date) pair.
            var occupancyWindows = await db.Bookings
                .AsNoTracking()
                .Where(b => b.ShootingRangeId == request.ShootingRangeId
                            && b.BookingDate == request.BookingDate
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .Select(b => new BookingCreation.OccupancyWindow(
                    b.ShootingRangeId, b.BookingDate, b.StartTime, b.EndTime))
                .ToListAsync(ct);

            var bookingNumber = await db.NextBookingNumberAsync(ct);

            var result = BookingCreation.CreateBooking(
                new BookingCreation.SlotRequest(
                    request.ShootingRangeId,
                    request.PackageId,
                    request.CustomerId,
                    request.BookingDate,
                    request.StartTime,
                    request.ShooterCount,
                    request.Notes,
                    BookingSource.Staff),
                rangeData,
                packageData,
                occupancyWindows,
                pendingBookings: [],
                bookingNumber);

            if (result.IsError)
            {
                outcome = result.Errors;
                return;
            }

            var booking = result.Value.Booking;
            await db.Bookings.AddAsync(booking, ct);

            if (request.ConfirmImmediately)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ConfirmedAt = DateTime.UtcNow;

                var company = await db.Companies
                    .AsNoTracking()
                    .Where(c => c.Id == tenant.CompanyId)
                    .Select(c => new { c.VatNumber, c.DueDays })
                    .FirstAsync(ct);

                var invoice = BookingInvoiceFactory.Create(
                    booking, company.VatNumber, company.DueDays, result.Value.RangeName, result.Value.PackageItems);

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
