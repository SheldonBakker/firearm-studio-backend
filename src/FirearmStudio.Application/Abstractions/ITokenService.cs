namespace FirearmStudio.Application.Abstractions;

public sealed record PreAuthPrincipal(Guid UserId, string Email);

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    DateTime AccessExpiresAt);

public enum RefreshFailure
{
    NotFound,
    Expired,
    Revoked,

    Reused,
}

public interface ITokenService
{
    Task<TokenPair> IssueAsync(Guid userId, string email, CancellationToken ct);

    Task<(TokenPair? Pair, RefreshFailure? Failure)> RefreshAsync(
        string refreshToken,
        CancellationToken ct);

    Task RevokeAsync(string refreshToken, CancellationToken ct);

    Task RevokeAllAsync(Guid userId, CancellationToken ct);

    string IssuePreAuthToken(Guid userId, string email);

    PreAuthPrincipal? ValidatePreAuthToken(string token);
}
