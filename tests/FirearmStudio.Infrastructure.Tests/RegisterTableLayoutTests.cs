using FirearmStudio.Infrastructure.Services;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests;

public class RegisterTableLayoutTests
{
    private const double Content = 800d;

    [Fact]
    public void Widths_are_equal_when_no_weights_are_supplied()
    {
        var widths = RegisterTableLayout.ColumnWidths(4, null, Content);

        Assert.Equal(4, widths.Length);
        Assert.All(widths, w => Assert.Equal(200d, w, 6));
    }

    [Fact]
    public void Widths_are_proportional_to_the_weights()
    {
        var widths = RegisterTableLayout.ColumnWidths(3, [1f, 2f, 1f], Content);

        Assert.Equal(200.25d, widths[0], 6);
        Assert.Equal(399.5d, widths[1], 6);
        Assert.Equal(200.25d, widths[2], 6);
    }

    [Fact]
    public void Widths_always_sum_to_the_content_width_exactly()
    {
        float[] weights = [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];

        var widths = RegisterTableLayout.ColumnWidths(weights.Length, weights, Content);

        Assert.Equal(16, widths.Length);
        Assert.Equal(Content, widths.Sum(), 9);
    }

    [Fact]
    public void Widths_sum_exactly_for_the_firearms_register_weights()
    {
        float[] weights = [0.8f, 0.8f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.9f, 0.9f, 0.9f, 0.9f];

        var widths = RegisterTableLayout.ColumnWidths(weights.Length, weights, Content);

        Assert.Equal(15, widths.Length);
        Assert.Equal(Content, widths.Sum(), 9);
    }

    [Fact]
    public void A_weight_list_shorter_than_the_column_count_falls_back_to_equal_widths()
    {
        var widths = RegisterTableLayout.ColumnWidths(4, [1f, 2f], Content);

        Assert.Equal(4, widths.Length);
        Assert.All(widths, w => Assert.Equal(200d, w, 6));
    }

    [Fact]
    public void A_weight_list_longer_than_the_column_count_uses_only_the_leading_weights()
    {
        var widths = RegisterTableLayout.ColumnWidths(2, [1f, 3f, 99f], Content);

        Assert.Equal(2, widths.Length);
        Assert.Equal(200.5d, widths[0], 6);
        Assert.Equal(599.5d, widths[1], 6);
    }

    [Fact]
    public void Weights_summing_to_zero_fall_back_to_equal_widths()
    {
        var widths = RegisterTableLayout.ColumnWidths(2, [0f, 0f], Content);

        Assert.Equal(400d, widths[0], 6);
        Assert.Equal(400d, widths[1], 6);
    }

    [Fact]
    public void Negative_weights_fall_back_to_equal_widths()
    {
        var widths = RegisterTableLayout.ColumnWidths(2, [-1f, -1f], Content);

        Assert.Equal(400d, widths[0], 6);
        Assert.Equal(400d, widths[1], 6);
    }

    [Fact]
    public void A_zero_column_count_returns_no_widths()
    {
        Assert.Empty(RegisterTableLayout.ColumnWidths(0, null, Content));
    }

    [Fact]
    public void Every_column_gets_a_positive_width()
    {
        var widths = RegisterTableLayout.ColumnWidths(3, [1f, 0f, 1f], Content);

        Assert.All(widths, w => Assert.True(w > 0, $"Expected a positive width, got {w}."));
        Assert.Equal(Content, widths.Sum(), 9);
    }

    [Fact]
    public void A_negative_weight_anywhere_falls_back_to_equal_widths_even_when_the_total_is_positive()
    {
        var widths = RegisterTableLayout.ColumnWidths(2, [-1f, 5f], Content);

        Assert.Equal(400d, widths[0], 6);
        Assert.Equal(400d, widths[1], 6);
    }

    [Fact]
    public void Widths_sum_exactly_when_the_minimum_width_floor_engages()
    {
        float[] weights = [97f, .. Enumerable.Repeat(1f, 97)];

        var widths = RegisterTableLayout.ColumnWidths(98, weights, 10d);

        Assert.Equal(98, widths.Length);
        Assert.Equal(10d, widths.Sum(), 9);
    }

    [Fact]
    public void Widths_sum_exactly_for_the_safe_custody_shape_at_an_absurdly_small_content_width()
    {
        float[] weights = [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];

        var widths = RegisterTableLayout.ColumnWidths(weights.Length, weights, 10d);

        Assert.Equal(10d, widths.Sum(), 9);
    }

    [Fact]
    public void Every_column_meets_the_minimum_width_when_the_content_width_allows_it()
    {
        float[] weights = [500f, 0.01f, 0.01f];

        var widths = RegisterTableLayout.ColumnWidths(3, weights, Content);

        Assert.All(widths, w => Assert.True(w >= 1d, $"Expected at least the 1pt floor, got {w}."));
        Assert.Equal(Content, widths.Sum(), 9);
    }
}
