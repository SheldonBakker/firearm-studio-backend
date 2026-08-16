using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class ExportRegisterSortOrderTests
{
    private static Booking MakeBooking() => new()
    {
        Id = Guid.NewGuid(),
        BookingDate = new DateOnly(2026, 8, 1),
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        BookingNumber = "BKG-20260801-0001",
        PackageName = "Standard",
        CalendarToken = "token",
    };

    private static BookingAttendee MakeAttendee(Guid id, Booking booking, string fullName = "Alice Shooter") =>
        new()
        {
            Id = id,
            BookingId = booking.Id,
            Booking = booking,
            FullName = fullName,
            IdNumber = "8001015009087",
            FirearmOrigin = FirearmOrigin.Own,
        };

    [Fact]
    public void Items_with_equal_sort_keys_are_ordered_by_id_ascending()
    {
        var booking = MakeBooking();
        var smallerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var largerId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var attendees = new[]
        {
            MakeAttendee(largerId, booking),
            MakeAttendee(smallerId, booking),
        };

        var sorted = attendees.AsQueryable()
            .OrderBy(a => a.Booking!.BookingDate)
            .ThenBy(a => a.Booking!.StartTime)
            .ThenBy(a => a.Booking!.BookingNumber)
            .ThenBy(a => a.FullName)
            .ThenBy(a => a.Id)
            .ToList();

        Assert.Equal(smallerId, sorted[0].Id);
        Assert.Equal(largerId, sorted[1].Id);
    }

    [Fact]
    public void Items_with_equal_sort_keys_keep_insertion_order_without_id_tiebreaker()
    {
        var booking = MakeBooking();
        var smallerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var largerId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var attendees = new[]
        {
            MakeAttendee(largerId, booking),
            MakeAttendee(smallerId, booking),
        };

        var sortedWithoutIdTiebreaker = attendees.AsQueryable()
            .OrderBy(a => a.Booking!.BookingDate)
            .ThenBy(a => a.Booking!.StartTime)
            .ThenBy(a => a.Booking!.BookingNumber)
            .ThenBy(a => a.FullName)
            .ToList();

        Assert.Equal(largerId, sortedWithoutIdTiebreaker[0].Id);
        Assert.Equal(smallerId, sortedWithoutIdTiebreaker[1].Id);
    }
}
