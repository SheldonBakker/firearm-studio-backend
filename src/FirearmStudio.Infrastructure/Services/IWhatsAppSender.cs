using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public interface IWhatsAppSender
{
    Task SendOtpAsync(
        string phoneE164,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct);
}
