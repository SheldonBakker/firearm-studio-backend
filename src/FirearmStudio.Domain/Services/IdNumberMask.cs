namespace FirearmStudio.Domain.Services;

public static class IdNumberMask
{
    private const int LeadingVisible = 6;
    private const int TrailingVisible = 3;

    public static string Mask(string idNumber)
    {
        if (idNumber.Length <= LeadingVisible + TrailingVisible)
        {
            return new string('*', idNumber.Length);
        }

        return string.Concat(
            idNumber.AsSpan(0, LeadingVisible),
            new string('*', idNumber.Length - LeadingVisible - TrailingVisible),
            idNumber.AsSpan(idNumber.Length - TrailingVisible));
    }
}
