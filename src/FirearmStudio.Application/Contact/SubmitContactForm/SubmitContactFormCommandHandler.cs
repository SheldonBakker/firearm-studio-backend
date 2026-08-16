using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model.Options;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Contact.SubmitContactForm;

public sealed class SubmitContactFormCommandHandler(
    ICustomerEngagementClient engagement,
    CustomerEngagementSettings settings,
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
                await engagement.SubscribeProfileAsync(settings.ContactListId, request.Email, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to subscribe contact {Email} to the engagement list.", request.Email);
            }
        }

        try
        {
            await engagement.TrackEventAsync(
                settings.ContactMetricName,
                request.Email,
                request.FullName,
                properties,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send the contact-form engagement event for {Email}.", request.Email);
        }

        return Result.Success;
    }
}
