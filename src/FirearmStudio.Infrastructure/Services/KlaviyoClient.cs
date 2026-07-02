using System.Net.Http.Json;
using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Services;

public sealed class KlaviyoClient(HttpClient httpClient) : IKlaviyoClient
{
    public Task TrackEventAsync(
        string metricName,
        string email,
        string? name,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            data = new
            {
                type = "event",
                attributes = new
                {
                    properties,
                    metric = new { data = new { type = "metric", attributes = new { name = metricName } } },
                    profile = new { data = new { type = "profile", attributes = BuildProfileAttributes(email, name) } },
                },
            },
        };

        return SendAsync("api/events/", payload, cancellationToken);
    }

    public Task SubscribeProfileAsync(string listId, string email, CancellationToken cancellationToken)
    {
        var payload = new
        {
            data = new
            {
                type = "profile-subscription-bulk-create-job",
                attributes = new
                {
                    profiles = new
                    {
                        data = new[]
                        {
                            new
                            {
                                type = "profile",
                                attributes = new
                                {
                                    email,
                                    subscriptions = new { email = new { marketing = new { consent = "SUBSCRIBED" } } },
                                },
                            },
                        },
                    },
                },
                relationships = new { list = new { data = new { type = "list", id = listId } } },
            },
        };

        return SendAsync("api/profile-subscription-bulk-create-jobs/", payload, cancellationToken);
    }

    private static Dictionary<string, object?> BuildProfileAttributes(string email, string? name)
    {
        var attributes = new Dictionary<string, object?> { ["email"] = email };
        if (!string.IsNullOrWhiteSpace(name))
        {
            attributes["properties"] = new Dictionary<string, object?> { ["full_name"] = name };
        }

        return attributes;
    }

    private async Task SendAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(path, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Klaviyo POST {path} failed with status {(int)response.StatusCode}: {body}");
        }
    }
}
