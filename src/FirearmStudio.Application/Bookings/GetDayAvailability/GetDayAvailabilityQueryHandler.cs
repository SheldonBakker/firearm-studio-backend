using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetDayAvailability;

public sealed class GetDayAvailabilityQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetDayAvailabilityQuery, ErrorOr<DayAvailabilityResponse>>
{
    public async Task<ErrorOr<DayAvailabilityResponse>> Handle(
        GetDayAvailabilityQuery query, CancellationToken cancellationToken)
    {
        IDisposable? scope = null;
        try
        {
            if (query.CompanyId is { } companyId)
            {
                var companyExists = await db.Companies
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == companyId && c.IsActive, cancellationToken);

                if (!companyExists)
                {
                    return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
                }

                scope = tenant.BeginCompanyScope(companyId);
            }
            else if (tenant.CompanyId is null)
            {
                return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
            }

            var range = await db.ShootingRanges
                .AsNoTracking()
                .Where(r => r.Id == query.ShootingRangeId && r.IsActive)
                .Select(r => new
                {
                    r.LaneCount,
                    r.SlotIntervalMinutes,
                    Hours = r.OperatingHours
                        .Where(h => h.Day == query.Date.DayOfWeek)
                        .Select(h => new { h.OpenTime, h.CloseTime })
                        .FirstOrDefault(),
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (range is null)
            {
                return Error.NotFound(ErrorCodes.RangeNotFound, "Shooting range not found.");
            }

            var durationMinutes = await db.Packages
                .AsNoTracking()
                .Where(p => p.Id == query.PackageId && p.IsActive)
                .Select(p => (int?)p.DurationMinutes)
                .FirstOrDefaultAsync(cancellationToken);

            if (durationMinutes is null)
            {
                return Error.NotFound(ErrorCodes.PackageNotFound, "Package not found.");
            }

            if (range.Hours is null)
            {
                return new DayAvailabilityResponse(query.Date, []);
            }

            var bookings = await db.Bookings
                .AsNoTracking()
                .Where(b => b.ShootingRangeId == query.ShootingRangeId
                            && b.BookingDate == query.Date
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                .Select(b => new AvailabilityCalculator.BookedWindow(b.StartTime, b.EndTime))
                .ToListAsync(cancellationToken);

            var slots = AvailabilityCalculator.GetDaySlots(
                range.Hours.OpenTime,
                range.Hours.CloseTime,
                range.SlotIntervalMinutes,
                durationMinutes.Value,
                range.LaneCount,
                bookings);

            return new DayAvailabilityResponse(query.Date, slots);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    public static class ErrorCodes
    {
        public const string CompanyNotFound = "GetDayAvailabilityQuery.CompanyNotFound";
        public const string RangeNotFound = "GetDayAvailabilityQuery.RangeNotFound";
        public const string PackageNotFound = "GetDayAvailabilityQuery.PackageNotFound";
    }
}
