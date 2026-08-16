using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Registers;

public static class StorageRecordPeriod
{
    public static Expression<Func<StorageRecord, bool>> OverlapsRange(DateOnly from, DateOnly to) =>
        r => r.StoredFrom <= to && (r.StoredUntil == null || r.StoredUntil >= from);
}
