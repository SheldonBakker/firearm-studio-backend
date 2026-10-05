using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Contact.SubmitContactForm;

public sealed class SubmitContactFormCommandHandler(
    IContactDirectory contactDirectory,
    ITransactionalEmailSender emailSender,
    ILogger<SubmitContactFormCommandHandler> logger)
    : ICommandHandler<SubmitContactFormCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SubmitContactFormCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var spaceIndex = request.FullName.IndexOf(' ');
        var firstName = spaceIndex >= 0 ? request.FullName[..spaceIndex] : request.FullName;
        var lastName = spaceIndex >= 0 ? request.FullName[(spaceIndex + 1)..] : null;

        try
        {
            await contactDirectory.AddContactAsync(
                new ContactEntry(request.Email, firstName, lastName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add contact {Email} to the directory.", request.Email);
        }

        try
        {
            await emailSender.SendAsync(
                new ContactFormReceivedEmail(request.FullName, request.Email, request.Company, request.Message),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact-form-received email for {Email}.", request.Email);
        }

        try
        {
            await emailSender.SendAsync(
                new ContactFormAcknowledgementEmail(request.Email, request.FullName),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact-form-acknowledgement email for {Email}.", request.Email);
        }

        return Result.Success;
    }
}
