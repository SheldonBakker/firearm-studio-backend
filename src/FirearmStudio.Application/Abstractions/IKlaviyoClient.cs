namespace FirearmStudio.Application.Abstractions;

public interface IKlaviyoClient
{
    // Records an event in Klaviyo under the given metric, associated with the profile identified by email.
    Task TrackEventAsync(
        string metricName,
        string email,
        string? name,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken);
}
