using FirearmStudio.Application.Abstractions.Email;

namespace FirearmStudio.Application.Abstractions;

public interface ITransactionalEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
