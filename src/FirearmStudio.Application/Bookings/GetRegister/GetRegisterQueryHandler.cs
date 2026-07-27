using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetRegister;

public sealed class GetRegisterQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetRegisterQuery, ErrorOr<PaginatedResponse<RegisterRowDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<RegisterRowDto>>> Handle(
        GetRegisterQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.BookingAttendees.AsNoTracking();

        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(a => a.Booking!.BookingDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            queryable = queryable.Where(a => a.Booking!.BookingDate <= query.DateTo.Value);
        }

        if (query.ShootingRangeId.HasValue)
        {
            queryable = queryable.Where(a => a.Booking!.ShootingRangeId == query.ShootingRangeId.Value);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderBy(a => a.Booking!.BookingDate)
            .ThenBy(a => a.Booking!.StartTime)
            .ThenBy(a => a.Booking!.BookingNumber)
            .ThenBy(a => a.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(RegisterRowDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<RegisterRowDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
