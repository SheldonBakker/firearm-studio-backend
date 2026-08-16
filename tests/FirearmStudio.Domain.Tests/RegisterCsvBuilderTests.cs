using System.Text;
using FirearmStudio.Application.Bookings;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class RegisterCsvBuilderTests
{
    private const string HeaderRow =
        "Date,Start Time,End Time,Range,Booking Number,Customer Name,Attendee Name,ID Number," +
        "Licence Number,Firearm Make/Model,Serial Number,Calibre,Origin,Signed Indemnity,Checked In At";

    private static RegisterRowDto DefaultRow(
        string attendeeFullName = "Jane Shooter",
        string? customerName = "Acme Corp",
        string rangeName = "Main Range",
        string bookingNumber = "BKG-20260801-0001",
        string? licenceNumber = "LIC-123",
        bool signedIndemnity = true,
        DateTime? checkedInAt = null) => new(
        new DateOnly(2026, 8, 1),
        new TimeOnly(9, 0),
        new TimeOnly(10, 0),
        rangeName,
        bookingNumber,
        customerName,
        attendeeFullName,
        "8001015009087",
        licenceNumber,
        "Glock 17",
        "SN12345",
        "9mm",
        FirearmOrigin.Own,
        signedIndemnity,
        checkedInAt);

    private static string BuildText(params RegisterRowDto[] rows)
    {
        var bytes = RegisterCsvBuilder.Build(rows);
        return Encoding.UTF8.GetString(bytes);
    }

    [Fact]
    public void Build_writes_header_row_first()
    {
        var text = BuildText(DefaultRow());

        Assert.StartsWith(HeaderRow + "\r\n", text);
    }

    [Fact]
    public void Build_uses_crlf_line_endings_throughout()
    {
        var text = BuildText(DefaultRow(), DefaultRow());

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
    public void Build_writes_one_data_row_per_attendee_row()
    {
        var text = BuildText(DefaultRow(attendeeFullName: "Jane Shooter"), DefaultRow(attendeeFullName: "John Shooter"));

        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Contains("Jane Shooter", lines[1]);
        Assert.Contains("John Shooter", lines[2]);
    }

    [Fact]
    public void Build_leaves_plain_values_unquoted()
    {
        var text = BuildText(DefaultRow());

        Assert.Contains("Main Range", text);
        Assert.DoesNotContain("\"Main Range\"", text);
    }

    [Fact]
    public void Build_quotes_field_containing_a_comma()
    {
        var text = BuildText(DefaultRow(customerName: "Acme, Inc"));

        Assert.Contains("\"Acme, Inc\"", text);
    }

    [Fact]
    public void Build_quotes_field_containing_a_quote_and_doubles_the_embedded_quote()
    {
        var text = BuildText(DefaultRow(rangeName: "The \"Main\" Range"));

        Assert.Contains("\"The \"\"Main\"\" Range\"", text);
    }

    [Fact]
    public void Build_quotes_field_containing_a_newline()
    {
        var text = BuildText(DefaultRow(customerName: "Acme\nInc"));

        Assert.Contains("\"Acme\nInc\"", text);
    }

    [Fact]
    public void Build_quotes_field_containing_a_carriage_return()
    {
        var text = BuildText(DefaultRow(customerName: "Acme\rInc"));

        Assert.Contains("\"Acme\rInc\"", text);
    }

    [Fact]
    public void Build_formats_signed_indemnity_as_yes_or_no()
    {
        var yesText = BuildText(DefaultRow(signedIndemnity: true));
        var noText = BuildText(DefaultRow(signedIndemnity: false));

        Assert.Contains(",Yes,", yesText);
        Assert.Contains(",No,", noText);
    }

    [Fact]
    public void Build_leaves_checked_in_at_blank_when_null()
    {
        var text = BuildText(DefaultRow(checkedInAt: null));

        var lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.EndsWith(",", lines[1]);
    }

    [Fact]
    public void Build_formats_checked_in_at_when_present()
    {
        var text = BuildText(DefaultRow(checkedInAt: new DateTime(2026, 8, 1, 9, 5, 0, DateTimeKind.Utc)));

        Assert.Contains("2026-08-01 09:05:00", text);
    }

    [Fact]
    public void Build_writes_only_header_when_no_rows()
    {
        var text = BuildText();

        Assert.Equal(HeaderRow + "\r\n", text);
    }

    [Fact]
    public void Build_leaves_null_optional_fields_blank_not_literal_null()
    {
        var text = BuildText(DefaultRow(licenceNumber: null));

        Assert.DoesNotContain("null", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("=cmd|'/c calc'!A1")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(1+1)")]
    [InlineData("\tmalicious")]
    [InlineData("\rmalicious")]
    public void Build_neutralizes_leading_formula_trigger_characters(string customerName)
    {
        var text = BuildText(DefaultRow(customerName: customerName));

        Assert.Contains("'" + customerName, text);
    }

    [Fact]
    public void Build_does_not_neutralize_a_value_without_a_leading_formula_trigger()
    {
        var text = BuildText(DefaultRow(customerName: "Acme Corp"));

        Assert.DoesNotContain("'Acme Corp", text);
    }
}
