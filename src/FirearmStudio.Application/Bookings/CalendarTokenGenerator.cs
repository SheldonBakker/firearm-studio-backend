using System.Buffers.Text;
using System.Security.Cryptography;

namespace FirearmStudio.Application.Bookings;

public static class CalendarTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
}
