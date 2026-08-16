using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings;

public static class DepositCalculator
{
    public static decimal? Calculate(DepositMode mode, decimal value, decimal invoiceTotal)
    {
        if (invoiceTotal <= 0)
        {
            return null;
        }

        var amount = mode switch
        {
            DepositMode.FixedAmount => value,
            DepositMode.Percentage => Math.Round(invoiceTotal * value / 100m, 2, MidpointRounding.AwayFromZero),
            _ => 0m,
        };

        if (amount <= 0)
        {
            return null;
        }

        return Math.Min(amount, invoiceTotal);
    }
}
