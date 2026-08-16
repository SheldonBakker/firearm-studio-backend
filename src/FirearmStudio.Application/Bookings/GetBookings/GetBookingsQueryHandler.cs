using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetBookings;

public sealed class GetBookingsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetBookingsQuery, ErrorOr<PaginatedResponse<BookingListItemDto>>>
{
    public async Task<ErrorOr<PaginatedResponse<BookingListItemDto>>> Handle(
        GetBookingsQuery query, CancellationToken cancellationToken)
    {
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

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, BookingListItemDto.QueryProjection, cancellationToken);
    }
}
