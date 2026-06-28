using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.GetActiveStorageRecords;

public sealed class GetActiveStorageRecordsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetActiveStorageRecordsQuery, ErrorOr<IReadOnlyList<ActiveStorageRecordDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ActiveStorageRecordDto>>> Handle(
        GetActiveStorageRecordsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ActiveStorageRecordDto> records = await db.StorageRecords
            .AsNoTracking()
            .ActiveOpen()
            .Select(s => new ActiveStorageRecordDto(
                s.Id, s.FirearmId, s.MonthlyRate, s.StorageLocation, s.RackNumber, s.SafeNumber, s.StoredFrom))
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
