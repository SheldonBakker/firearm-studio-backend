namespace FirearmStudio.Application.Common;

/// <summary>
/// The single shared time zone for the business (South Africa has no DST, so this is a fixed
/// offset in practice, but resolving it via <see cref="TimeZoneInfo"/> keeps every local-time
/// conversion in the codebase pointed at the same IANA definition).
/// </summary>
public static class SouthAfricaTimeZone
{
    public static readonly TimeZoneInfo Instance = TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
}
