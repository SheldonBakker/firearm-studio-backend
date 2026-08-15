using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FirearmStudio.Infrastructure.Identity;

public sealed class TokenService(
    AuthDbContext auth,
    ApplicationDbContext app,
    JwtSettings settings,
    TimeProvider timeProvider) : ITokenService
{
    public async Task<TokenPair> IssueAsync(Guid userId, string email, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var claims = new List<Claim>
        {
            new(AppClaimTypes.Subject, userId.ToString()),
            new(AppClaimTypes.Email, email),
            new(AppClaimTypes.TokenId, Guid.NewGuid().ToString()),
        };

        var appUser = await app.AppUsers
            .IgnoreQueryFilters()
            .Where(u => u.AuthUserId == userId && u.IsActive)
            .Select(u => new { u.CompanyId, u.Role })
            .FirstOrDefaultAsync(ct);

        if (appUser is not null)
        {
            claims.Add(new Claim(AppClaimTypes.CompanyId, appUser.CompanyId.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, appUser.Role.ToRoleString()));
        }

        var expires = now.AddMinutes(settings.AccessTokenMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = GenerateRefreshToken();

        auth.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(settings.RefreshTokenDays),
        });

        await auth.SaveChangesAsync(ct);

        return new TokenPair(accessToken, refreshToken, expires);
    }

    public async Task<(TokenPair? Pair, RefreshFailure? Failure)> RefreshAsync(
        string refreshToken,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = Hash(refreshToken);

        var stored = await auth.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            return (null, RefreshFailure.NotFound);
        }

        if (stored.ReplacedById is not null)
        {
            await RevokeAllAsync(stored.UserId, ct);
            return (null, RefreshFailure.Reused);
        }

        if (stored.RevokedAt is not null)
        {
            return (null, RefreshFailure.Revoked);
        }

        if (stored.ExpiresAt <= now)
        {
            return (null, RefreshFailure.Expired);
        }

        var user = await auth.Users
            .Where(u => u.Id == stored.UserId)
            .Select(u => new { u.Id, u.Email })
            .FirstOrDefaultAsync(ct);

        if (user?.Email is null)
        {
            return (null, RefreshFailure.NotFound);
        }

        var pair = await IssueAsync(user.Id, user.Email, ct);

        stored.RevokedAt = now;
        stored.ReplacedById = await auth.RefreshTokens
            .Where(t => t.TokenHash == Hash(pair.RefreshToken))
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        await auth.SaveChangesAsync(ct);

        return (pair, null);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct)
    {
        var hash = Hash(refreshToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var stored = await auth.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);

        if (stored is null)
        {
            return;
        }

        stored.RevokedAt = now;
        await auth.SaveChangesAsync(ct);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var outstanding = await auth.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in outstanding)
        {
            token.RevokedAt = now;
        }

        await auth.SaveChangesAsync(ct);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
