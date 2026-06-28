using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Common;

public static class StorageRecordQueryExtensions
{
    public static IQueryable<StorageRecord> ActiveOpen(this IQueryable<StorageRecord> source) =>
        source.Where(s => s.StorageStatus == StorageStatus.Active && s.StoredUntil == null);
}
