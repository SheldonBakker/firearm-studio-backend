using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public sealed class KlaviyoEmailSender(ICustomerEngagementClient klaviyo) : IEmailSender
{
    public Task SendOtpAsync(
        string email,
        string? name,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        var properties = new Dictionary<string, object?>
        {
            ["code"] = code,
            ["expires_in_minutes"] = expiresInMinutes,
        };

        return klaviyo.TrackEventAsync(MetricFor(purpose), email, name, properties, ct);
    }

    private static string MetricFor(OtpPurpose purpose) => purpose switch
    {
        OtpPurpose.EmailConfirmation => "Signup Verification Code",
        OtpPurpose.PasswordReset => "Password Reset Code",
        OtpPurpose.Invite => "Team Invite Code",
        OtpPurpose.TwoFactor => "Login Verification Code",
        OtpPurpose.PhoneChange => "Phone Verification Code",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
    };
}
