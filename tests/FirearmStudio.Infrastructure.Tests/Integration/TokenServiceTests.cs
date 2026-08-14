using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class TokenServiceTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "https://api.test.local",
        Audience = "firearm-studio",
        SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-abcdef",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 14,
    };

    private static readonly DateTimeOffset Start =
        new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private async Task<(TokenService Service, TestTimeProvider Clock, Guid UserId, string Email)>
        CreateAsync(Guid? companyId = null, AppRole role = AppRole.Admin)
    {
        await fixture.MigrateAllAsync();

        var clock = new TestTimeProvider(Start);
        var auth = fixture.CreateAuthDbContext();
        var app = fixture.CreateDbContext();

        var userId = Guid.NewGuid();
        var email = $"{Guid.NewGuid():N}@example.com";

        auth.Users.Add(new AppIdentityUser
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
        });
        await auth.SaveChangesAsync();

        if (companyId is not null)
        {
            app.Companies.Add(new Company { Id = companyId.Value, Name = "Test Co" });
            app.AppUsers.Add(new AppUser
            {
                CompanyId = companyId.Value,
                AuthUserId = userId,
                Email = email,
                Role = role,
                IsActive = true,
            });
            await app.SaveChangesAsync();
        }

        return (new TokenService(auth, app, Settings, clock), clock, userId, email);
    }

    private static JwtSecurityToken Read(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    [Fact]
    public async Task Access_token_carries_subject_and_email()
    {
        var (service, _, userId, email) = await CreateAsync();

        var pair = await service.IssueAsync(userId, email, default);
        var token = Read(pair.AccessToken);

        Assert.Equal(userId.ToString(), token.Claims.First(c => c.Type == AppClaimTypes.Subject).Value);
        Assert.Equal(email, token.Claims.First(c => c.Type == AppClaimTypes.Email).Value);
        Assert.Equal(Settings.Issuer, token.Issuer);
    }

    [Fact]
    public async Task Access_token_carries_company_and_role_when_an_app_user_exists()
    {
        var companyId = Guid.NewGuid();
        var (service, _, userId, email) = await CreateAsync(companyId, AppRole.Manager);

        var pair = await service.IssueAsync(userId, email, default);
        var token = Read(pair.AccessToken);

        Assert.Equal(
            companyId.ToString(),
            token.Claims.First(c => c.Type == AppClaimTypes.CompanyId).Value);
        Assert.Equal(
            "manager",
            token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task Access_token_omits_company_and_role_before_onboarding()
    {
        var (service, _, userId, email) = await CreateAsync();

        var pair = await service.IssueAsync(userId, email, default);
        var token = Read(pair.AccessToken);

        Assert.DoesNotContain(token.Claims, c => c.Type == AppClaimTypes.CompanyId);
        Assert.DoesNotContain(token.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public async Task Refresh_rotates_and_revokes_the_old_token()
    {
        var (service, _, userId, email) = await CreateAsync();
        var first = await service.IssueAsync(userId, email, default);

        var (second, failure) = await service.RefreshAsync(first.RefreshToken, default);

        Assert.Null(failure);
        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second!.RefreshToken);

        var (_, replayFailure) = await service.RefreshAsync(first.RefreshToken, default);
        Assert.Equal(RefreshFailure.Reused, replayFailure);
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_the_whole_chain()
    {
        var (service, _, userId, email) = await CreateAsync();
        var first = await service.IssueAsync(userId, email, default);
        var (second, _) = await service.RefreshAsync(first.RefreshToken, default);

        await service.RefreshAsync(first.RefreshToken, default);

        var (_, failure) = await service.RefreshAsync(second!.RefreshToken, default);
        Assert.Equal(RefreshFailure.Revoked, failure);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        var (service, clock, userId, email) = await CreateAsync();
        var pair = await service.IssueAsync(userId, email, default);

        clock.Advance(TimeSpan.FromDays(Settings.RefreshTokenDays) + TimeSpan.FromMinutes(1));

        var (_, failure) = await service.RefreshAsync(pair.RefreshToken, default);

        Assert.Equal(RefreshFailure.Expired, failure);
    }

    [Fact]
    public async Task Revoked_refresh_token_is_rejected()
    {
        var (service, _, userId, email) = await CreateAsync();
        var pair = await service.IssueAsync(userId, email, default);

        await service.RevokeAsync(pair.RefreshToken, default);

        var (_, failure) = await service.RefreshAsync(pair.RefreshToken, default);

        Assert.Equal(RefreshFailure.Revoked, failure);
    }

    [Fact]
    public async Task Revoke_all_kills_every_outstanding_token()
    {
        var (service, _, userId, email) = await CreateAsync();
        var first = await service.IssueAsync(userId, email, default);
        var second = await service.IssueAsync(userId, email, default);

        await service.RevokeAllAsync(userId, default);

        Assert.Equal(RefreshFailure.Revoked, (await service.RefreshAsync(first.RefreshToken, default)).Failure);
        Assert.Equal(RefreshFailure.Revoked, (await service.RefreshAsync(second.RefreshToken, default)).Failure);
    }

    [Fact]
    public async Task Refresh_token_is_never_stored_in_plaintext()
    {
        var (service, _, userId, email) = await CreateAsync();
        var pair = await service.IssueAsync(userId, email, default);

        await using var db = fixture.CreateAuthDbContext();
        var hashes = await db.RefreshTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.TokenHash)
            .ToListAsync();

        Assert.NotEmpty(hashes);
        Assert.DoesNotContain(pair.RefreshToken, hashes);
    }
}
