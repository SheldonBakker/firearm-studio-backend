namespace FirearmStudio.Application.Abstractions;

public static class OutboxMessageTypes
{
    public const string BookingRequested = "BookingRequested";

    /// <summary>
    /// Maximum dispatch attempts before a message is abandoned.
    /// Must match the SQL filter in <c>ApplicationDbContext.ClaimOutboxBatchAsync</c>.
    /// </summary>
    public const int MaxAttempts = 5;
}
