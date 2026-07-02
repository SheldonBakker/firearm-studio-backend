using System.Net.Http.Json;
using FirearmStudio.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

// Typed HttpClient for Klaviyo's Events API (JSON:API). Auth/revision headers and BaseAddress
// are configured on the HttpClient in AddInfrastructure.
public sealed class KlaviyoClient(HttpClient httpClient, ILogger<KlaviyoClient> logger) : IKlaviyoClient
{
    public async Task TrackEventAsync(
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
                    profile = new
                    {
                        data = new
                        {
                            type = "profile",
                            attributes = new
                            {
                                email,
                                properties = new Dictionary<string, object?> { ["full_name"] = name },
                            },
                        },
                    },
                },
            },
        };

        using var response = await httpClient.PostAsJsonAsync("api/events/", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "Klaviyo event track failed with status {StatusCode}: {Body}",
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }
    }
}
