using System.Buffers.Text;
using System.Security.Cryptography;

namespace FirearmStudio.Application.Bookings;

/// <summary>
/// Generates opaque, unguessable tokens used to look up a booking's public calendar (.ics) file
/// without authentication.
/// </summary>
public static class CalendarTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
}
