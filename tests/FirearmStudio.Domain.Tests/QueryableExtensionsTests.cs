using FirearmStudio.Application.Extensions;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class QueryableExtensionsTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(50, 50)]
    public void ClampPageNumber_produces_expected_value(int raw, int expected)
    {
        Assert.Equal(expected, QueryableExtensions.ClampPageNumber(raw));
    }

    [Theory]
    [InlineData(0, QueryableExtensions.DefaultPageSize)]
    [InlineData(-1, QueryableExtensions.DefaultPageSize)]
    [InlineData(QueryableExtensions.MaxPageSize + 1, QueryableExtensions.DefaultPageSize)]
    [InlineData(1000, QueryableExtensions.DefaultPageSize)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(QueryableExtensions.MaxPageSize, QueryableExtensions.MaxPageSize)]
    public void ClampPageSize_produces_expected_value(int raw, int expected)
    {
        Assert.Equal(expected, QueryableExtensions.ClampPageSize(raw));
    }

    [Fact]
    public void DefaultPageSize_is_twenty()
    {
        Assert.Equal(20, QueryableExtensions.DefaultPageSize);
    }

    [Fact]
    public void MaxPageSize_is_two_hundred()
    {
        Assert.Equal(200, QueryableExtensions.MaxPageSize);
    }

    [Fact]
    public void PageSize_at_max_is_not_clamped_to_default()
    {
        Assert.Equal(QueryableExtensions.MaxPageSize, QueryableExtensions.ClampPageSize(QueryableExtensions.MaxPageSize));
    }

    [Fact]
    public void PageSize_one_above_max_falls_back_to_default()
    {
        Assert.Equal(QueryableExtensions.DefaultPageSize, QueryableExtensions.ClampPageSize(QueryableExtensions.MaxPageSize + 1));
    }
}
