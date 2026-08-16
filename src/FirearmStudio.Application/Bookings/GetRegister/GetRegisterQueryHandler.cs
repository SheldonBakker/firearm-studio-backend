using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Application.Model;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.GetRegister;

public sealed class GetRegisterQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetRegisterQuery, ErrorOr<PaginatedResponse<RegisterRowDto>>>
{
    public async Task<ErrorOr<PaginatedResponse<RegisterRowDto>>> Handle(
        GetRegisterQuery query, CancellationToken cancellationToken)
    {
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

        queryable = queryable
            .OrderBy(a => a.Booking!.BookingDate)
            .ThenBy(a => a.Booking!.StartTime)
            .ThenBy(a => a.Booking!.BookingNumber)
            .ThenBy(a => a.FullName)
            .ThenBy(a => a.Id);

        return await queryable.ToPaginatedAsync(
            query.PageNumber, query.PageSize, RegisterRowDto.QueryProjection, cancellationToken);
    }
}
