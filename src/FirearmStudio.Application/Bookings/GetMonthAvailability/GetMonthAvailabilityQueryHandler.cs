using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetMonthAvailability;

public sealed class GetMonthAvailabilityQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetMonthAvailabilityQuery, ErrorOr<MonthAvailabilityResponse>>
{
    public async Task<ErrorOr<MonthAvailabilityResponse>> Handle(
        GetMonthAvailabilityQuery query, CancellationToken cancellationToken)
    {
        if (query.Year is < 2000 or > 2100 || query.Month is < 1 or > 12)
        {
            return Error.Validation(ErrorCodes.InvalidMonth, "Year or month is out of range.");
        }

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
                        .Select(h => new { h.Day, h.OpenTime, h.CloseTime })
                        .ToList(),
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

            var monthStart = new DateOnly(query.Year, query.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var bookingsByDate = (await db.Bookings
                    .AsNoTracking()
                    .Where(b => b.ShootingRangeId == query.ShootingRangeId
                                && b.BookingDate >= monthStart
                                && b.BookingDate < nextMonthStart
                                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
                    .Select(b => new { b.BookingDate, b.StartTime, b.EndTime })
                    .ToListAsync(cancellationToken))
                .GroupBy(b => b.BookingDate)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<AvailabilityCalculator.BookedWindow>)g
                        .Select(b => new AvailabilityCalculator.BookedWindow(b.StartTime, b.EndTime))
                        .ToList());

            var hoursByDay = range.Hours.ToDictionary(h => h.Day);

            var days = new List<MonthAvailabilityDayDto>();
            for (var date = monthStart; date < nextMonthStart; date = date.AddDays(1))
            {
                var hasAvailability = false;
                if (hoursByDay.TryGetValue(date.DayOfWeek, out var hours))
                {
                    var bookings = bookingsByDate.GetValueOrDefault(date, []);
                    hasAvailability = AvailabilityCalculator.HasAnySlot(
                        hours.OpenTime,
                        hours.CloseTime,
                        range.SlotIntervalMinutes,
                        durationMinutes.Value,
                        range.LaneCount,
                        bookings);
                }

                days.Add(new MonthAvailabilityDayDto(date, hasAvailability));
            }

            return new MonthAvailabilityResponse(days);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    public static class ErrorCodes
    {
        public const string InvalidMonth = "GetMonthAvailabilityQuery.InvalidMonth";
        public const string CompanyNotFound = "GetMonthAvailabilityQuery.CompanyNotFound";
        public const string RangeNotFound = "GetMonthAvailabilityQuery.RangeNotFound";
        public const string PackageNotFound = "GetMonthAvailabilityQuery.PackageNotFound";
    }
}
