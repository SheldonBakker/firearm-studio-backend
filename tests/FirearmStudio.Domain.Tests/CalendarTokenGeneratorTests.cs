using FirearmStudio.Application.Bookings;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class CalendarTokenGeneratorTests
{
    [Fact]
    public void Generate_returns_url_safe_token_of_43_characters()
    {
        var token = CalendarTokenGenerator.Generate();

        Assert.Equal(43, token.Length);
    }

    [Fact]
    public void Generate_returns_only_url_safe_characters()
    {
        var token = CalendarTokenGenerator.Generate();

        Assert.All(token, c => Assert.True(
            char.IsAsciiLetterOrDigit(c) || c is '-' or '_',
            $"Unexpected character '{c}' in calendar token."));
    }

    [Fact]
    public void Generate_returns_unique_tokens()
    {
        var first = CalendarTokenGenerator.Generate();
        var second = CalendarTokenGenerator.Generate();

        Assert.NotEqual(first, second);
    }
}
