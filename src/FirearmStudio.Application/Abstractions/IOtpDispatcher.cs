using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Abstractions;

public sealed record OtpRecipient(string Email, string? Name, string? PhoneNumber);

public interface IOtpDispatcher
{
    Task SendAsync(
        OtpRecipient recipient,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct);
}
