using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.GetActiveStorageFirearms;

public sealed class GetActiveStorageFirearmsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetActiveStorageFirearmsQuery, ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ActiveStorageFirearmDto>>> Handle(
        GetActiveStorageFirearmsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ActiveStorageFirearmDto> records = await db.StorageRecords
            .AsNoTracking()
            .ActiveOpen()
            .Select(s => new ActiveStorageFirearmDto(
                s.FirearmId,
                s.Firearm!.SerialNumber,
                s.Firearm.Make,
                s.Firearm.Model,
                s.MonthlyRate,
                s.StorageLocation,
                s.StoredFrom))
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(records);
    }
}
