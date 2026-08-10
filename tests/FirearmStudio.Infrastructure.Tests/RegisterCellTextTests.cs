using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class RegisterCellTextTests
{
    private const string Zwsp = "\u200B";

    private static readonly Func<string, double> OnePointPerCharacter = segment => segment.Length;

    private static string Break(string? value, double usableWidth) =>
        RegisterCellText.InsertBreakOpportunities(value, usableWidth, OnePointPerCharacter);

    [Fact]
    public void Null_becomes_an_empty_string()
    {
        Assert.Equal(string.Empty, RegisterCellText.Sanitise(null));
    }

    [Fact]
    public void Ordinary_text_is_unchanged()
    {
        Assert.Equal("CZ Shadow 2", RegisterCellText.Sanitise("CZ Shadow 2"));
    }

    [Theory]
    [InlineData("a\tb", "a b")]
    [InlineData("a\nb", "a b")]
    [InlineData("a\r\nb", "a b")]
    [InlineData("a\vb", "a b")]
    public void Control_characters_become_spaces(string input, string expected)
    {
        Assert.Equal(expected, RegisterCellText.Sanitise(input));
    }

    [Fact]
    public void Runs_of_whitespace_collapse_to_one_space()
    {
        Assert.Equal("12 Range Rd, Paarl", RegisterCellText.Sanitise("12  Range\t\tRd,\n Paarl"));
    }

    [Fact]
    public void Leading_and_trailing_whitespace_is_trimmed()
    {
        Assert.Equal("SN123", RegisterCellText.Sanitise("  SN123\r\n"));
    }

    [Fact]
    public void An_all_whitespace_value_becomes_empty()
    {
        Assert.Equal(string.Empty, RegisterCellText.Sanitise(" \t\r\n "));
    }

    [Fact]
    public void Non_ascii_latin_text_survives_intact()
    {
        Assert.Equal("Muizenberg Straße", RegisterCellText.Sanitise("Muizenberg Straße"));
    }

    [Fact]
    public void Sanitise_passes_a_break_opportunity_through_unchanged()
    {
        var alreadyBroken = "Han" + Zwsp + "dgu" + Zwsp + "n";

        Assert.Equal(alreadyBroken, RegisterCellText.Sanitise(alreadyBroken));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_a_null_value_as_empty()
    {
        Assert.Equal(string.Empty, Break(null, 1));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_an_empty_value_as_empty()
    {
        Assert.Equal(string.Empty, Break(string.Empty, 1));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_a_segment_that_fits_untouched()
    {
        Assert.Equal("Handgun", Break("Handgun", 7));
    }

    [Fact]
    public void InsertBreakOpportunities_breaks_a_segment_one_point_too_wide()
    {
        Assert.Equal("Han" + Zwsp + "dgu" + Zwsp + "n", Break("Handgun", 6.9));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_a_whole_run_that_fits_untouched()
    {
        Assert.Equal("2026-01-01", Break("2026-01-01", 10));
    }

    [Fact]
    public void InsertBreakOpportunities_breaks_a_date_at_its_hyphens_and_nowhere_else()
    {
        Assert.Equal("2026-" + Zwsp + "01-" + Zwsp + "01", Break("2026-01-01", 5));
    }

    [Fact]
    public void InsertBreakOpportunities_breaks_a_licence_number_at_its_slashes_and_nowhere_else()
    {
        Assert.Equal("WC/" + Zwsp + "2020/" + Zwsp + "00000", Break("WC/2020/00000", 5));
    }

    [Fact]
    public void InsertBreakOpportunities_chunks_only_the_slash_segments_that_do_not_fit()
    {
        var result = Break("WC/2020/00000", 4);

        Assert.Equal(
            "WC/" + Zwsp + "202" + Zwsp + "0/" + Zwsp + "000" + Zwsp + "00",
            result);
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_several_short_space_separated_words_untouched()
    {
        Assert.Equal("CZ Blade 2", Break("CZ Blade 2", 5));
    }

    [Fact]
    public void InsertBreakOpportunities_only_breaks_the_run_that_exceeds_the_width()
    {
        var result = Break("CZ 8501015800086", 5);

        Assert.StartsWith("CZ ", result, StringComparison.Ordinal);
        Assert.DoesNotContain("C" + Zwsp + "Z", result, StringComparison.Ordinal);

        var expectedIdNumber = string.Join(Zwsp, "8501015800086".Chunk(3).Select(chunk => new string(chunk)));
        Assert.Contains(expectedIdNumber, result, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SN1234")]
    [InlineData("WC/2020/00000")]
    [InlineData("8501015800086")]
    [InlineData("Muizenberg")]
    [InlineData("Handgun")]
    [InlineData("CZ Shadow 2")]
    [InlineData("")]
    public void InsertBreakOpportunities_preserves_the_original_characters_in_order(string value)
    {
        var result = Break(value, 3);
        var withoutBreaks = result.Replace(Zwsp, string.Empty);

        Assert.Equal(value, withoutBreaks);
    }
}
