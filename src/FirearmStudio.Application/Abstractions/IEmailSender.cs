using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Abstractions;

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
