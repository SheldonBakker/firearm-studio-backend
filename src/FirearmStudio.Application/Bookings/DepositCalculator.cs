using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Pure calculation of the deposit due on an invoice from a company's deposit policy.
/// <see cref="DepositMode.FixedAmount"/> takes <c>value</c> as a currency amount;
/// <see cref="DepositMode.Percentage"/> takes <c>value</c> as a percentage of
/// <paramref name="invoiceTotal"/>, rounded to 2 decimal places away from zero. Either mode is
/// clamped so the deposit never exceeds the invoice total. <see cref="DepositMode.None"/>, a
/// non-positive computed amount, or a non-positive invoice total all resolve to no deposit.
/// </summary>
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
