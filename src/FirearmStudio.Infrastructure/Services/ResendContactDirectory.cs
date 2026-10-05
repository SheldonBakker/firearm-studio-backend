using System.Net.Http.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Infrastructure.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

public sealed class ResendContactDirectory(
    HttpClient httpClient,
    ResendSettings settings,
    ILogger<ResendContactDirectory> logger) : IContactDirectory
{
    public async Task AddContactAsync(ContactEntry entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ContactSegmentId))
        {
            logger.LogWarning(
                "Contact not added: ResendSettings:ContactSegmentId is not configured.");
            return;
        }

        var payload = new
        {
            email = entry.Email,
            first_name = entry.FirstName,
            last_name = entry.LastName,
            segments = new[] { new { id = settings.ContactSegmentId } },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "contacts");
        request.Content = JsonContent.Create(payload);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend POST contacts failed: {(int)response.StatusCode} {body}");
        }
    }
}
