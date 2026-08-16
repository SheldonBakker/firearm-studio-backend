using System.Globalization;
using FirearmStudio.Application.Registers;
using FirearmStudio.Domain.Entities;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class StorageRecordPeriodTests
{
    private static bool Overlaps(string from, string to, string storedFrom, string? storedUntil)
    {
        var predicate = StorageRecordPeriod
            .OverlapsRange(ParseDate(from), ParseDate(to))
            .Compile();

        return predicate(new StorageRecord
        {
            StoredFrom = ParseDate(storedFrom),
            StoredUntil = storedUntil is null ? null : ParseDate(storedUntil),
        });
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.Parse(value, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("2026-02-01", "2026-03-01", true)]
    [InlineData("2025-01-01", null, true)]
    [InlineData("2026-06-30", null, true)]
    [InlineData("2026-07-01", null, false)]
    public void OverlapsRange_handles_start_dates(string storedFrom, string? storedUntil, bool expected)
    {
        Assert.Equal(expected, Overlaps("2026-01-01", "2026-06-30", storedFrom, storedUntil));
    }

    [Theory]
    [InlineData("2025-12-31", false)]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-02-15", true)]
    public void OverlapsRange_handles_end_dates(string storedUntil, bool expected)
    {
        Assert.Equal(expected, Overlaps("2026-01-01", "2026-06-30", "2025-01-01", storedUntil));
    }
}
