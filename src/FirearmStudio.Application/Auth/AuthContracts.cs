namespace FirearmStudio.Application.Auth;

public sealed record LoginOutcome(
    AuthTokensResponse? Tokens,
    TwoFactorChallengeResponse? Challenge);

public sealed record RegisterRequest(string Email, string Password, string? PhoneNumber = null);

public sealed record VerifyEmailRequest(string Email, string Code);

public sealed record ResendCodeRequest(string Email, string Purpose);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

public sealed record AcceptInviteRequest(string Email, string Code, string Password, string? PhoneNumber = null);

public sealed record LoginVerifyRequest(string PreAuthToken, string Code);

public sealed record DisableTwoFactorRequest(string Password);

public sealed record RegisterResponse(string Message = "If that address can be registered, a verification code is on its way.");

public sealed record ResendCodeResponse(string Message = "If that address can receive a code, one is on its way.");

public sealed record TwoFactorChallengeResponse(bool RequiresTwoFactor, string PreAuthToken);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessExpiresAt);
