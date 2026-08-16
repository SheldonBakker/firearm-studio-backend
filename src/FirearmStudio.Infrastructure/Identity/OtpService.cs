using System.Security.Cryptography;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class OtpService(
    AuthDbContext db,
    IPasswordHasher<AppIdentityUser> hasher,
    TimeProvider timeProvider) : IOtpService
{
    internal static readonly TimeSpan Ttl = TimeSpan.FromMinutes(OtpConstants.CodeLifetimeMinutes);
    private static readonly TimeSpan ResendInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ResendWindow = TimeSpan.FromHours(1);

    private const int MaxAttempts = 5;
    private const int DefaultMaxPerWindow = 5;
    private const int TwoFactorMaxPerWindow = 20;

    private static readonly AppIdentityUser HashingPlaceholder = new();

    private static int MaxPerWindowFor(OtpPurpose purpose) => purpose switch
    {
        OtpPurpose.TwoFactor => TwoFactorMaxPerWindow,
        _ => DefaultMaxPerWindow,
    };

    public async Task<OtpIssueResult> IssueAsync(
        Guid userId,
        OtpPurpose purpose,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var recent = await db.OtpCodes
            .Where(c => c.UserId == userId
                        && c.Purpose == purpose
                        && c.CreatedAt > now - ResendWindow)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var last = recent.FirstOrDefault();
        if (last is not null && last.CreatedAt + ResendInterval > now)
        {
            return new OtpIssueResult(
                OtpIssueStatus.Throttled,
                Code: null,
                RetryAfter: last.CreatedAt + ResendInterval - now);
        }

        if (recent.Count >= MaxPerWindowFor(purpose))
        {
            var oldest = recent[^1];
            return new OtpIssueResult(
                OtpIssueStatus.Throttled,
                Code: null,
                RetryAfter: oldest.CreatedAt + ResendWindow - now);
        }

        var outstanding = await db.OtpCodes
            .Where(c => c.UserId == userId
                        && c.Purpose == purpose
                        && c.ConsumedAt == null
                        && c.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var code in outstanding)
        {
            code.ExpiresAt = now;
        }

        var plaintext = GenerateCode();

        db.OtpCodes.Add(new OtpCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = purpose,
            CodeHash = hasher.HashPassword(HashingPlaceholder, plaintext),
            ExpiresAt = now + Ttl,
            CreatedAt = now,
            AttemptCount = 0,
        });

        await db.SaveChangesAsync(ct);

        return new OtpIssueResult(OtpIssueStatus.Issued, plaintext, RetryAfter: null);
    }

    public async Task InvalidateAsync(Guid userId, OtpPurpose purpose, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var outstanding = await db.OtpCodes
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.ConsumedAt == null)
            .ToListAsync(ct);

        if (outstanding.Count == 0)
        {
            return;
        }

        foreach (var code in outstanding)
        {
            code.ConsumedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<OtpVerifyResult> VerifyAsync(
        Guid userId,
        OtpPurpose purpose,
        string code,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var candidate = await db.OtpCodes
            .Where(c => c.UserId == userId && c.Purpose == purpose && c.ConsumedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (candidate is null)
        {
            return OtpVerifyResult.NotFound;
        }

        if (candidate.AttemptCount >= MaxAttempts)
        {
            return OtpVerifyResult.TooManyAttempts;
        }

        if (candidate.ExpiresAt <= now)
        {
            return OtpVerifyResult.Expired;
        }

        var verification = hasher.VerifyHashedPassword(
            HashingPlaceholder, candidate.CodeHash, code);

        if (verification == PasswordVerificationResult.Failed)
        {
            candidate.AttemptCount++;
            await db.SaveChangesAsync(ct);

            return candidate.AttemptCount >= MaxAttempts
                ? OtpVerifyResult.TooManyAttempts
                : OtpVerifyResult.Invalid;
        }

        candidate.ConsumedAt = now;
        await db.SaveChangesAsync(ct);

        return OtpVerifyResult.Valid;
    }

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
