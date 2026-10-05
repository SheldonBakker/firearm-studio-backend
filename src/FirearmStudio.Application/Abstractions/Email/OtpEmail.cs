using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Abstractions.Email;

public sealed record OtpEmail(
    OtpPurpose Purpose, string Email, string? Name, string Code, int ExpiresInMinutes) : EmailMessage;
