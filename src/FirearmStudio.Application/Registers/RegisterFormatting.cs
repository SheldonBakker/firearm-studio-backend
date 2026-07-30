using System.Globalization;

namespace FirearmStudio.Application.Registers;

public static class RegisterFormatting
{
    public static string Date(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}
