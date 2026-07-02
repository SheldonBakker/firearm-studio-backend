using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Contact.SubmitContactForm;

public sealed class SubmitContactFormCommandHandler(
    IKlaviyoClient klaviyo,
    KlaviyoSettings settings,
    ILogger<SubmitContactFormCommandHandler> logger)
    : ICommandHandler<SubmitContactFormCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SubmitContactFormCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var properties = new Dictionary<string, object?> { ["message"] = request.Message };
        if (!string.IsNullOrWhiteSpace(request.Company))
        {
            properties["company"] = request.Company;
        }

        if (!string.IsNullOrWhiteSpace(settings.ContactListId))
        {
            try
            {
                await klaviyo.SubscribeProfileAsync(settings.ContactListId, request.Email, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe contact {Email} to the Klaviyo list.", request.Email);
            }
        }

        try
        {
            await klaviyo.TrackEventAsync(
                settings.ContactMetricName,
                request.Email,
                request.FullName,
                properties,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact form submission to Klaviyo for {Email}.", request.Email);
        }

        return Result.Success;
    }
}
