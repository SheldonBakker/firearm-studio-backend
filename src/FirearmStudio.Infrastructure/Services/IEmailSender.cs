using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public interface IEmailSender
{
    Task SendOtpAsync(
        string email,
        string? name,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct);
}
