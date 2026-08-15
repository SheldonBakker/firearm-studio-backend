using System.Net.Http.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Infrastructure.Services;

public sealed class WahaWhatsAppSender(HttpClient httpClient, WahaSettings settings) : IWhatsAppSender
{
    public async Task SendOtpAsync(
        string phoneE164,
        OtpPurpose purpose,
        string code,
        int expiresInMinutes,
        CancellationToken ct)
    {
        var payload = new
        {
            chatId = ToChatId(phoneE164),
            text = BuildMessage(purpose, code, expiresInMinutes),
        };

        var path = $"api/sessions/{settings.SessionId}/messages/send-text";

        using var response = await httpClient.PostAsJsonAsync(path, payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Never include the response body or session id in this message: WAHA echoes the
            // recipient back in error bodies, and Task 5's dispatcher logs this exception.
            throw new HttpRequestException(
                $"WAHA send-text failed with status {(int)response.StatusCode}.");
        }
    }

    // "+27821234567" -> "27821234567@c.us"
    private static string ToChatId(string phoneE164) => phoneE164.TrimStart('+') + "@c.us";

    private static string BuildMessage(OtpPurpose purpose, string code, int expiresInMinutes) => purpose switch
    {
        OtpPurpose.EmailConfirmation =>
            $"Your Firearm Studio verification code is {code}. It expires in {expiresInMinutes} minutes.",
        OtpPurpose.PasswordReset =>
            $"Your Firearm Studio password reset code is {code}. It expires in {expiresInMinutes} minutes.",
        OtpPurpose.Invite =>
            $"Your Firearm Studio team invite code is {code}. It expires in {expiresInMinutes} minutes.",
        OtpPurpose.TwoFactor =>
            $"Your Firearm Studio login code is {code}. It expires in {expiresInMinutes} minutes.",
        OtpPurpose.PhoneChange =>
            $"Your Firearm Studio phone verification code is {code}. It expires in {expiresInMinutes} minutes.",
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null),
    };
}
