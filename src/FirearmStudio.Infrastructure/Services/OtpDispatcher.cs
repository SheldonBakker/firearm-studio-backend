using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Email;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public sealed class OtpDispatcher(ITransactionalEmailSender sender) : IOtpDispatcher
{
    public Task SendAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct) =>
        sender.SendAsync(
            new OtpEmail(purpose, recipient.Email, recipient.Name, code, expiresInMinutes),
            ct);
}
