using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Abstractions;

public enum OtpIssueStatus
{
    Issued,
    Throttled,
}

public sealed record OtpIssueResult(
    OtpIssueStatus Status,
    string? Code,
    TimeSpan? RetryAfter);

public enum OtpVerifyResult
{
    Valid,
    Invalid,
    Expired,
    TooManyAttempts,
    NotFound,
}

public interface IOtpService
{
    Task<OtpIssueResult> IssueAsync(Guid userId, OtpPurpose purpose, CancellationToken ct);

    Task<OtpVerifyResult> VerifyAsync(
        Guid userId,
        OtpPurpose purpose,
        string code,
        CancellationToken ct);

    Task InvalidateAsync(Guid userId, OtpPurpose purpose, CancellationToken ct);
}
