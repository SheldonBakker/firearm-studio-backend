namespace FirearmStudio.Application.Abstractions;

public interface ICustomerEngagementClient
{
    Task TrackEventAsync(
        string metricName,
        string email,
        string? name,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken);

    Task SubscribeProfileAsync(
        string listId,
        string email,
        CancellationToken cancellationToken);
}
