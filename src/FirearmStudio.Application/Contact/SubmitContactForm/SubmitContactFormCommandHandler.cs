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
        var parts = request.FullName.Split((char[])null!, 2,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstName = parts.Length > 0 ? parts[0] : request.FullName.Trim();
        var lastName = parts.Length > 1 ? parts[1] : null;

        await Task.WhenAll(
            AddContactAsync(request, firstName, lastName, cancellationToken),
            SendReceivedEmailAsync(request, cancellationToken),
            SendAcknowledgementEmailAsync(request, cancellationToken));

        return Result.Success;
    }

    private async Task AddContactAsync(
        ContactFormRequest request, string firstName, string? lastName, CancellationToken cancellationToken)
    {
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
    }

    private async Task SendReceivedEmailAsync(ContactFormRequest request, CancellationToken cancellationToken)
    {
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
    }

    private async Task SendAcknowledgementEmailAsync(ContactFormRequest request, CancellationToken cancellationToken)
    {
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
    }
}
