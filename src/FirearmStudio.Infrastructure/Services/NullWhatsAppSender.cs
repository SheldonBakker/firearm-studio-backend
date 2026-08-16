using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public sealed class NullWhatsAppSender : IWhatsAppSender
{
    public Task SendOtpAsync(
        string phoneE164,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct) => Task.CompletedTask;
}
