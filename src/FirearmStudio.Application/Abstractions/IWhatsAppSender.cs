using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Abstractions;

public interface IWhatsAppSender
{
    Task SendOtpAsync(
        string phoneE164,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct);
}
