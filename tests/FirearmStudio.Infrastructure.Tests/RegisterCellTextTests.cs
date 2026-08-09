using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class RegisterCellTextTests
{
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
}
