using System.Text;
using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class BookingIcsBuilderTests
{
    private static readonly Guid BookingId = Guid.Parse("018f1e2a-0000-7000-8000-000000000001");
    private static readonly DateTime UtcNow = new(2026, 7, 27, 10, 0, 0, DateTimeKind.Utc);

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

    private static string BuildText(
        BookingIcsBuilder.BookingIcsData? booking = null,
        BookingIcsBuilder.CompanyIcsData? company = null)
    {
        var bytes = BookingIcsBuilder.Build(booking ?? DefaultBooking(), company ?? DefaultCompany(), UtcNow);
        return Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public void Build_uses_crlf_line_endings_throughout()
    {
        var text = BuildText();

        Assert.Contains("\r\n", text);
        Assert.DoesNotContain("\r\n\r\n", text); // no bare LF anywhere producing a doubled CRLF

        // Every line break in the document must be CRLF, never a bare LF.
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            Assert.True(i > 0 && text[i - 1] == '\r', $"Bare LF found at index {i}.");
        }
    }

    [Fact]
    public void Build_starts_and_ends_with_vcalendar_markers()
    {
        var text = BuildText();

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", text);
        Assert.EndsWith("END:VCALENDAR\r\n", text);
        Assert.Contains("BEGIN:VEVENT\r\n", text);
        Assert.Contains("END:VEVENT\r\n", text);
    }

    [Fact]
    public void Build_sets_uid_to_booking_id()
    {
        var text = BuildText();

        Assert.Contains($"UID:{BookingId}\r\n", text);
    }

    [Fact]
    public void Build_uses_south_africa_tzid_for_dtstart_and_dtend()
    {
        var text = BuildText();

        Assert.Contains("DTSTART;TZID=Africa/Johannesburg:20260801T090000\r\n", text);
        Assert.Contains("DTEND;TZID=Africa/Johannesburg:20260801T100000\r\n", text);
    }

    [Fact]
    public void Build_writes_dtstamp_in_utc()
    {
        var text = BuildText();

        Assert.Contains("DTSTAMP:20260727T100000Z\r\n", text);
    }

    [Fact]
    public void Build_includes_booking_number_and_shooter_count_in_description()
    {
        var text = BuildText(booking: DefaultBooking(shooterCount: 3));

        Assert.Contains("DESCRIPTION:", text);
        Assert.Contains("BKG-20260801-0001", text);
        Assert.Contains("3", text);
    }

    [Fact]
    public void Build_combines_package_and_range_name_in_summary()
    {
        var text = BuildText(booking: DefaultBooking(packageName: "VIP Package", rangeName: "North Range"));

        Assert.Contains("SUMMARY:VIP Package - North Range\r\n", text);
    }

    [Fact]
    public void Build_joins_company_address_parts_into_location()
    {
        var text = BuildText();

        Assert.Contains("LOCATION:1 Range Road\\, Pretoria\\, Gauteng\\, 0001\r\n", text);
    }

    [Fact]
    public void Build_omits_missing_address_parts_from_location()
    {
        var company = DefaultCompany(addressLine1: null, addressLine2: null, city: "Cape Town", province: null, postalCode: null);

        var text = BuildText(company: company);

        Assert.Contains("LOCATION:Cape Town\r\n", text);
    }

    [Fact]
    public void Build_escapes_commas_semicolons_and_backslashes_in_text_fields()
    {
        var booking = DefaultBooking(packageName: "Range, Time; Slot\\Test", rangeName: "Range");

        var text = BuildText(booking: booking);

        Assert.Contains("SUMMARY:Range\\, Time\\; Slot\\\\Test - Range\r\n", text);
    }

    [Fact]
    public void Build_escapes_newlines_in_text_fields_as_literal_backslash_n()
    {
        var company = DefaultCompany(addressLine1: "Line one\nLine two", addressLine2: null, city: null, province: null, postalCode: null);

        var text = BuildText(company: company);

        Assert.Contains("LOCATION:Line one\\nLine two\r\n", text);
    }
}
