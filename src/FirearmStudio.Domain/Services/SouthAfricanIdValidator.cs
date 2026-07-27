namespace FirearmStudio.Domain.Services;

/// <summary>
/// Pure validation for the attendee ID number field. A value that is exactly 13 digits is
/// treated as a South African ID number and must pass the standard Luhn checksum (the
/// algorithm the Department of Home Affairs uses to derive the 13th check digit). Any other
/// value is accepted as a passport number, bounded only by length.
/// </summary>
public static class SouthAfricanIdValidator
{
    private const int SouthAfricanIdLength = 13;
    private const int MaxLength = 20;

    public static bool IsValid(string idNumber)
    {
        if (string.IsNullOrEmpty(idNumber) || idNumber.Length > MaxLength)
        {
            return false;
        }

        return idNumber.Length != SouthAfricanIdLength || !IsAllDigits(idNumber)
            ? true
            : HasValidLuhnChecksum(idNumber);
    }

    private static bool IsAllDigits(string value)
    {
        foreach (var ch in value)
        {
            if (!char.IsAsciiDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasValidLuhnChecksum(string digits)
    {
        var sum = 0;
        var doubleDigit = false;

        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';

            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleDigit = !doubleDigit;
        }

        return sum % 10 == 0;
    }
}
