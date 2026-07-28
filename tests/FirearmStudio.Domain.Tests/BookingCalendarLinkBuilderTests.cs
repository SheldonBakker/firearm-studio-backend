using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BookingCalendarLinkBuilderTests
{
    private const string CalendarToken = "abc123token";

    private static readonly Guid BookingId = Guid.Parse("018f1e2a-0000-7000-8000-000000000001");

    private static BookingIcsBuilder.BookingIcsData DefaultBooking(
        string packageName = "Standard Range Package",
        string rangeName = "Main Range",
        int shooterCount = 2) => new(
        BookingId,
        "BKG-20260801-0001",
        packageName,
        rangeName,
        new DateOnly(2026, 8, 1),
        new TimeOnly(9, 0),
        new TimeOnly(10, 0),
        shooterCount);

    private static BookingIcsBuilder.CompanyIcsData DefaultCompany(
        string name = "Range Co",
        string? addressLine1 = "1 Range Road",
        string? addressLine2 = null,
        string? city = "Pretoria",
        string? province = "Gauteng",
        string? postalCode = "0001") => new(
        name, addressLine1, addressLine2, city, province, postalCode);

    [Fact]
    public void Build_returns_null_links_when_public_base_url_is_empty()
    {
        var links = BookingCalendarLinkBuilder.Build(string.Empty, CalendarToken, DefaultBooking(), DefaultCompany());

        Assert.Null(links.IcsUrl);
        Assert.Null(links.GoogleCalendarUrl);
    }

    [Fact]
    public void Build_returns_null_links_when_public_base_url_is_whitespace()
    {
        var links = BookingCalendarLinkBuilder.Build("   ", CalendarToken, DefaultBooking(), DefaultCompany());

        Assert.Null(links.IcsUrl);
        Assert.Null(links.GoogleCalendarUrl);
    }

    [Fact]
    public void Build_composes_ics_url_from_base_url_and_calendar_token()
    {
        var links = BookingCalendarLinkBuilder.Build(
            "https://app.example.com", CalendarToken, DefaultBooking(), DefaultCompany());

        Assert.Equal(
            "https://app.example.com/api/v1/public/bookings/abc123token/calendar.ics",
            links.IcsUrl);
    }

    [Fact]
    public void Build_trims_trailing_slash_from_base_url()
    {
        var links = BookingCalendarLinkBuilder.Build(
            "https://app.example.com/", CalendarToken, DefaultBooking(), DefaultCompany());

        Assert.Equal(
            "https://app.example.com/api/v1/public/bookings/abc123token/calendar.ics",
            links.IcsUrl);
    }

    [Fact]
    public void Build_converts_south_africa_local_start_and_end_to_utc_in_google_calendar_dates()
    {
        var links = BookingCalendarLinkBuilder.Build(
            "https://app.example.com", CalendarToken, DefaultBooking(), DefaultCompany());

        // Africa/Johannesburg is UTC+2 with no DST: 09:00/10:00 local becomes 07:00/08:00 UTC.
        Assert.Contains("dates=20260801T070000Z/20260801T080000Z", links.GoogleCalendarUrl);
    }

    [Fact]
    public void Build_includes_action_template_and_encoded_text_details_and_location()
    {
        var links = BookingCalendarLinkBuilder.Build(
            "https://app.example.com",
            CalendarToken,
            DefaultBooking(packageName: "VIP Package", rangeName: "North Range"),
            DefaultCompany());

        Assert.StartsWith("https://calendar.google.com/calendar/render?action=TEMPLATE", links.GoogleCalendarUrl);
        Assert.Contains("text=VIP%20Package%20-%20North%20Range", links.GoogleCalendarUrl);
        Assert.Contains("details=Booking%20BKG-20260801-0001%20for%202%20shooter%28s%29.", links.GoogleCalendarUrl);
        Assert.Contains("location=1%20Range%20Road%2C%20Pretoria%2C%20Gauteng%2C%200001", links.GoogleCalendarUrl);
    }
}
