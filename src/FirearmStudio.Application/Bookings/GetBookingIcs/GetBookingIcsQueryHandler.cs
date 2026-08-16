using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBookingIcs;

public sealed class GetBookingIcsQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetBookingIcsQuery, ErrorOr<byte[]>>
{
    public async Task<ErrorOr<byte[]>> Handle(GetBookingIcsQuery query, CancellationToken cancellationToken)
    {
        BookingRow? booking;
        using (tenant.BeginBypass())
        {
            booking = await db.Bookings
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(b => b.CalendarToken == query.Token)
                .Select(b => new BookingRow(
                    b.Id,
                    b.CompanyId,
                    b.BookingNumber,
                    b.PackageName,
                    b.ShootingRange!.Name,
                    b.BookingDate,
                    b.StartTime,
                    b.EndTime,
                    b.ShooterCount,
                    b.Status))
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (booking is null || booking.Status is BookingStatus.Cancelled or BookingStatus.NoShow)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking calendar not found.");
        }

        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == booking.CompanyId && c.IsActive)
            .Select(c => new CompanyRow(c.Name, c.AddressLine1, c.AddressLine2, c.City, c.Province, c.PostalCode))
            .FirstOrDefaultAsync(cancellationToken);

        if (company is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Booking calendar not found.");
        }

        return BookingIcsBuilder.Build(
            new BookingIcsBuilder.BookingIcsData(
                booking.Id,
                booking.BookingNumber,
                booking.PackageName,
                booking.RangeName,
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
                company.PostalCode),
            DateTime.UtcNow);
    }

    private sealed record BookingRow(
        Guid Id,
        Guid CompanyId,
        string BookingNumber,
        string PackageName,
        string RangeName,
        DateOnly BookingDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        int ShooterCount,
        BookingStatus Status);

    private sealed record CompanyRow(
        string Name,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? Province,
        string? PostalCode);

    public static class ErrorCodes
    {
        public const string NotFound = "GetBookingIcsQuery.NotFound";
    }
}
