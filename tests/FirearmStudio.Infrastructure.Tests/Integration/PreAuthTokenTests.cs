using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FirearmStudio.Application.Model.Options;
using FirearmStudio.Domain.Authentication;
using FirearmStudio.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class PreAuthTokenTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "https://api.test.local",
        Audience = "firearm-studio",
        SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256-abcdef",
    };

    private async Task<TokenService> BuildAsync(DateTimeOffset start)
    {
        await fixture.MigrateAllAsync();
        var auth = fixture.CreateAuthDbContext();
        var app = fixture.CreateDbContext();
        return new TokenService(auth, app, Settings, new TestTimeProvider(start));
    }

    // Mirrors the JwtBearer options configured in AuthenticationExtensions.AddWebAuthentication
    // for the ordinary api audience, so this test proves what the real middleware would do.
    private static TokenValidationParameters BuildAccessTokenValidationParameters()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Settings.SigningKey));

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Settings.Issuer,
            ValidateAudience = true,
            ValidAudience = Settings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidAlgorithms = Settings.ValidAlgorithms,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    [Fact]
    public async Task Pre_auth_token_round_trips()
    {
        var service = await BuildAsync(DateTimeOffset.UtcNow);
        var userId = Guid.NewGuid();

        var token = service.IssuePreAuthToken(userId, "user@example.com");
        var principal = service.ValidatePreAuthToken(token);

        Assert.NotNull(principal);
        Assert.Equal(userId, principal!.UserId);
        Assert.Equal("user@example.com", principal.Email);
    }

    [Fact]
    public async Task Pre_auth_token_uses_the_pre_auth_audience_and_no_scopes()
    {
        var service = await BuildAsync(DateTimeOffset.UtcNow);

        var token = service.IssuePreAuthToken(Guid.NewGuid(), "user@example.com");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains("firearm-studio:pre-auth", jwt.Audiences);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == AppClaimTypes.CompanyId);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == ClaimTypes.Role);
        Assert.Equal("pre_auth", jwt.Claims.First(c => c.Type == AppClaimTypes.TokenPurpose).Value);
    }

    [Fact]
    public async Task An_ordinary_access_token_is_rejected_by_pre_auth_validation()
    {
        await fixture.MigrateAllAsync();
        var auth = fixture.CreateAuthDbContext();
        var app = fixture.CreateDbContext();
        var service = new TokenService(auth, app, Settings, new TestTimeProvider(DateTimeOffset.UtcNow));

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

        var pair = await service.IssueAsync(userId, email, default);

        // Wrong audience (api audience, not "<audience>:pre-auth") and no purpose claim.
        Assert.Null(service.ValidatePreAuthToken(pair.AccessToken));
    }

    [Fact]
    public async Task A_pre_auth_token_is_rejected_by_the_ordinary_bearer_middleware_validation()
    {
        var service = await BuildAsync(DateTimeOffset.UtcNow);

        var preAuthToken = service.IssuePreAuthToken(Guid.NewGuid(), "user@example.com");

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = BuildAccessTokenValidationParameters();

        // This asserts against the same TokenValidationParameters shape the JwtBearer
        // middleware in AuthenticationExtensions uses, proving the pre-auth token would be
        // rejected (401) on ordinary API endpoints because its audience does not match.
        Assert.Throws<SecurityTokenInvalidAudienceException>(
            () => handler.ValidateToken(preAuthToken, parameters, out _));
    }

    [Fact]
    public async Task Expired_pre_auth_token_is_rejected()
    {
        // Issued 10 minutes in the past so its 5-minute lifetime is already over by wall-clock now.
        var service = await BuildAsync(DateTimeOffset.UtcNow.AddMinutes(-10));

        var token = service.IssuePreAuthToken(Guid.NewGuid(), "user@example.com");

        Assert.Null(service.ValidatePreAuthToken(token));
    }

    [Fact]
    public async Task Tampered_signature_is_rejected()
    {
        var service = await BuildAsync(DateTimeOffset.UtcNow);

        var token = service.IssuePreAuthToken(Guid.NewGuid(), "user@example.com");
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        // Flip the last character of the signature segment so validation fails.
        var lastChar = parts[2][^1];
        var replacement = lastChar == 'A' ? 'B' : 'A';
        var tamperedSignature = parts[2][..^1] + replacement;
        var tampered = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        Assert.Null(service.ValidatePreAuthToken(tampered));
    }

    [Fact]
    public async Task Garbage_input_is_rejected()
    {
        var service = await BuildAsync(DateTimeOffset.UtcNow);

        Assert.Null(service.ValidatePreAuthToken("not-a-jwt"));
    }
}
