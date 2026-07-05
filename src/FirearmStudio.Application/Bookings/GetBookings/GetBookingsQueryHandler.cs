using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBookings;

public sealed class GetBookingsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingsQuery, ErrorOr<PaginatedResponse<BookingListItemDto>>>
{
    private const int MaxPageSize = 200;

    public async Task<ErrorOr<PaginatedResponse<BookingListItemDto>>> Handle(
        GetBookingsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? 20 : query.PageSize;

        var queryable = db.Bookings.AsNoTracking();

        if (query.ShootingRangeId.HasValue)
        {
            queryable = queryable.Where(b => b.ShootingRangeId == query.ShootingRangeId.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(b => b.Status == query.Status.Value);
        }

        if (query.CustomerId.HasValue)
        {
            queryable = queryable.Where(b => b.CustomerId == query.CustomerId.Value);
        }

        if (query.DateFrom.HasValue)
        {
            queryable = queryable.Where(b => b.BookingDate >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            queryable = queryable.Where(b => b.BookingDate <= query.DateTo.Value);
        }

        var asc = query.SortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);
        queryable = asc
            ? queryable.OrderBy(b => b.BookingDate).ThenBy(b => b.StartTime).ThenBy(b => b.Id)
            : queryable.OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.StartTime).ThenBy(b => b.Id);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(BookingListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<BookingListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
