using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class RegisterCellTextTests
{
    private const string Zwsp = "\u200B";

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
    public void InsertBreakOpportunities_leaves_a_null_value_as_empty()
    {
        Assert.Equal(string.Empty, RegisterCellText.InsertBreakOpportunities(null));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_an_empty_value_as_empty()
    {
        Assert.Equal(string.Empty, RegisterCellText.InsertBreakOpportunities(string.Empty));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_a_run_at_the_threshold_untouched()
    {
        Assert.Equal("SN1234", RegisterCellText.InsertBreakOpportunities("SN1234"));
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_a_short_run_untouched()
    {
        Assert.Equal("Glock", RegisterCellText.InsertBreakOpportunities("Glock"));
    }

    [Fact]
    public void InsertBreakOpportunities_adds_zero_width_spaces_between_characters_of_a_run_above_the_threshold()
    {
        var result = RegisterCellText.InsertBreakOpportunities("WC/2020/00000");

        var expected = string.Join(Zwsp, "WC/2020/00000".ToCharArray());
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InsertBreakOpportunities_leaves_several_short_space_separated_words_untouched()
    {
        Assert.Equal("CZ Shadow 2", RegisterCellText.InsertBreakOpportunities("CZ Shadow 2"));
    }

    [Fact]
    public void InsertBreakOpportunities_only_breaks_the_run_that_exceeds_the_threshold()
    {
        var result = RegisterCellText.InsertBreakOpportunities("CZ 8501015800086");

        Assert.StartsWith("CZ ", result);
        Assert.DoesNotContain("C" + Zwsp + "Z", result, StringComparison.Ordinal);
        Assert.Contains(string.Join(Zwsp, "8501015800086".ToCharArray()), result, StringComparison.Ordinal);
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
        var result = RegisterCellText.InsertBreakOpportunities(value);
        var withoutBreaks = result.Replace(Zwsp, string.Empty);

        Assert.Equal(value, withoutBreaks);
    }
}
