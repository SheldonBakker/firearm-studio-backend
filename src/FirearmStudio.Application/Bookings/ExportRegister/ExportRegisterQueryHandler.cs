using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Bookings.ExportRegister;

public sealed class ExportRegisterQueryHandler(IApplicationDbContext db)
    : IQueryHandler<ExportRegisterQuery, ErrorOr<byte[]>>
{
    private const int MaxExportRows = 20000;

    public async Task<ErrorOr<byte[]>> Handle(ExportRegisterQuery query, CancellationToken cancellationToken)
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

        var totalCount = await queryable.CountAsync(cancellationToken);

        if (totalCount > MaxExportRows)
        {
            return Error.Validation(
                ErrorCodes.TooManyRows,
                $"The register export is limited to {MaxExportRows} rows. Narrow the date range or range filter and try again.");
        }

        var rows = await queryable
            .OrderBy(a => a.Booking!.BookingDate)
            .ThenBy(a => a.Booking!.StartTime)
            .ThenBy(a => a.Booking!.BookingNumber)
            .ThenBy(a => a.FullName)
            .Select(RegisterRowDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return RegisterCsvBuilder.Build(rows);
    }

    public static class ErrorCodes
    {
        public const string TooManyRows = "ExportRegisterQuery.TooManyRows";
    }
}
