using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Infrastructure.Services;

internal sealed class ResendEmailSender(
    HttpClient httpClient,
    ResendEmailMapper mapper,
    ILogger<ResendEmailSender> logger) : ITransactionalEmailSender
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var mapped = mapper.Map(message);
        if (mapped is null)
        {
            logger.LogWarning(
                "Email send skipped: required configuration is missing for {MessageType}.",
                message.GetType().Name);
            return;
        }

        var payload = new ResendEmailPayload(
            mapped.From,
            [mapped.To],
            mapped.ReplyTo,
            new ResendTemplate(mapped.Alias, mapped.Variables));

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        if (!string.IsNullOrWhiteSpace(message.IdempotencyKey))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", message.IdempotencyKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Resend POST emails failed: {(int)response.StatusCode} {body}");
        }
    }

    private sealed record ResendEmailPayload(
        string From,
        string[] To,
        [property: JsonPropertyName("reply_to")] string? ReplyTo,
        ResendTemplate Template);

    private sealed record ResendTemplate(
        [property: JsonPropertyName("id")] string Id,
        Dictionary<string, object> Variables);
}
