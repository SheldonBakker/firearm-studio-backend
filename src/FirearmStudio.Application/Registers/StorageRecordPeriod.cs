using System.Linq.Expressions;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Registers;

/// <summary>
/// A storage record belongs in a register for [from, to] when its custody period overlaps the
/// range. Open-ended records (still in custody) overlap every range that starts on or before
/// today, so current holdings always appear - which is exactly what an inspector expects to see.
/// </summary>
public static class StorageRecordPeriod
{
    public static Expression<Func<StorageRecord, bool>> OverlapsRange(DateOnly from, DateOnly to) =>
        r => r.StoredFrom <= to && (r.StoredUntil == null || r.StoredUntil >= from);
}
