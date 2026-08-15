namespace FirearmStudio.Application.Auth;

public sealed record RegisterRequest(string Email, string Password);

public sealed record VerifyEmailRequest(string Email, string Code);

public sealed record ResendCodeRequest(string Email, string Purpose);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);

public sealed record AcceptInviteRequest(string Email, string Code, string Password);

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessExpiresAt);
