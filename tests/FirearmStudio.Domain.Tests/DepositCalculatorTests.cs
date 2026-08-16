using FirearmStudio.Application.Bookings;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class DepositCalculatorTests
{
    [Theory]
    [InlineData(DepositMode.None, 500, 1000)]
    [InlineData(DepositMode.None, 0, 1000)]
    public void Calculate_returns_null_for_None_mode_regardless_of_value(DepositMode mode, decimal value, decimal invoiceTotal)
    {
        var result = DepositCalculator.Calculate(mode, value, invoiceTotal);

        Assert.Null(result);
    }

    [Fact]
    public void Calculate_FixedAmount_returns_value_when_below_total()
    {
        var result = DepositCalculator.Calculate(DepositMode.FixedAmount, 250m, 1000m);

        Assert.Equal(250m, result);
    }

    [Fact]
    public void Calculate_FixedAmount_clamps_to_total_when_value_exceeds_total()
    {
        var result = DepositCalculator.Calculate(DepositMode.FixedAmount, 1500m, 1000m);

        Assert.Equal(1000m, result);
    }

    [Fact]
    public void Calculate_FixedAmount_zero_value_returns_null()
    {
        var result = DepositCalculator.Calculate(DepositMode.FixedAmount, 0m, 1000m);

        Assert.Null(result);
    }

    [Fact]
    public void Calculate_Percentage_rounds_to_two_decimal_places()
    {
        var result = DepositCalculator.Calculate(DepositMode.Percentage, 1.2345m, 1000m);

        Assert.Equal(12.35m, result);
    }

    [Fact]
    public void Calculate_Percentage_rounds_midpoint_away_from_zero()
    {
        var result = DepositCalculator.Calculate(DepositMode.Percentage, 12.5m, 133m);

        Assert.Equal(16.63m, result);
    }

    [Fact]
    public void Calculate_Percentage_clamps_to_total_when_over_100_percent()
    {
        var result = DepositCalculator.Calculate(DepositMode.Percentage, 150m, 100m);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void Calculate_Percentage_zero_value_returns_null()
    {
        var result = DepositCalculator.Calculate(DepositMode.Percentage, 0m, 1000m);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(DepositMode.FixedAmount, 250)]
    [InlineData(DepositMode.Percentage, 50)]
    public void Calculate_returns_null_when_invoice_total_is_zero(DepositMode mode, decimal value)
    {
        var result = DepositCalculator.Calculate(mode, value, 0m);

        Assert.Null(result);
    }
}
