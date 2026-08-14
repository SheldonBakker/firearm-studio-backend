using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class TestTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}

public sealed class OtpServiceTests(TestDatabaseFixture fixture)
    : IClassFixture<TestDatabaseFixture>
{
    private static readonly DateTimeOffset Start =
        new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    private async Task<(OtpService Service, TestTimeProvider Clock, Guid UserId)> CreateAsync()
    {
        await fixture.MigrateAllAsync();

        var clock = new TestTimeProvider(Start);
        var db = fixture.CreateAuthDbContext();

        var user = new AppIdentityUser
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.com",
            UserName = $"{Guid.NewGuid():N}@example.com",
        };
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new OtpService(db, new PasswordHasher<AppIdentityUser>(), clock);

        return (service, clock, user.Id);
    }

    [Fact]
    public async Task Issue_returns_a_six_digit_numeric_code()
    {
        var (service, _, userId) = await CreateAsync();

        var result = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        Assert.Equal(OtpIssueStatus.Issued, result.Status);
        Assert.NotNull(result.Code);
        Assert.Equal(6, result.Code!.Length);
        Assert.True(result.Code.All(char.IsAsciiDigit));
    }

    [Fact]
    public async Task Correct_code_verifies()
    {
        var (service, _, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        var result = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, issued.Code!, default);

        Assert.Equal(OtpVerifyResult.Valid, result);
    }

    [Fact]
    public async Task Code_is_single_use()
    {
        var (service, _, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        await service.VerifyAsync(userId, OtpPurpose.EmailConfirmation, issued.Code!, default);

        var second = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, issued.Code!, default);

        Assert.Equal(OtpVerifyResult.NotFound, second);
    }

    [Fact]
    public async Task Wrong_code_is_invalid()
    {
        var (service, _, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        var wrong = issued.Code == "000000" ? "111111" : "000000";

        var result = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, wrong, default);

        Assert.Equal(OtpVerifyResult.Invalid, result);
    }

    [Fact]
    public async Task Five_failed_attempts_burn_the_code()
    {
        var (service, _, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);
        var wrong = issued.Code == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            await service.VerifyAsync(userId, OtpPurpose.EmailConfirmation, wrong, default);
        }

        var result = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, issued.Code!, default);

        Assert.Equal(OtpVerifyResult.TooManyAttempts, result);
    }

    [Fact]
    public async Task Code_expires_after_fifteen_minutes()
    {
        var (service, clock, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        clock.Advance(TimeSpan.FromMinutes(15) + TimeSpan.FromSeconds(1));

        var result = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, issued.Code!, default);

        Assert.Equal(OtpVerifyResult.Expired, result);
    }

    [Fact]
    public async Task Resending_within_sixty_seconds_is_throttled()
    {
        var (service, clock, userId) = await CreateAsync();
        await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        clock.Advance(TimeSpan.FromSeconds(30));

        var second = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        Assert.Equal(OtpIssueStatus.Throttled, second.Status);
        Assert.Null(second.Code);
        Assert.NotNull(second.RetryAfter);
    }

    [Fact]
    public async Task Sixth_code_within_an_hour_is_throttled()
    {
        var (service, clock, userId) = await CreateAsync();

        for (var i = 0; i < 5; i++)
        {
            var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);
            Assert.Equal(OtpIssueStatus.Issued, issued.Status);
            clock.Advance(TimeSpan.FromSeconds(61));
        }

        var sixth = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        Assert.Equal(OtpIssueStatus.Throttled, sixth.Status);
    }

    [Fact]
    public async Task Issuing_a_new_code_invalidates_the_previous_one()
    {
        var (service, clock, userId) = await CreateAsync();
        var first = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        clock.Advance(TimeSpan.FromSeconds(61));
        await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        var result = await service.VerifyAsync(
            userId, OtpPurpose.EmailConfirmation, first.Code!, default);

        Assert.NotEqual(OtpVerifyResult.Valid, result);
    }

    [Fact]
    public async Task Purposes_are_independent()
    {
        var (service, _, userId) = await CreateAsync();
        var confirmation = await service.IssueAsync(
            userId, OtpPurpose.EmailConfirmation, default);

        var result = await service.VerifyAsync(
            userId, OtpPurpose.PasswordReset, confirmation.Code!, default);

        Assert.Equal(OtpVerifyResult.NotFound, result);
    }

    [Fact]
    public async Task Plaintext_code_is_never_stored()
    {
        var (service, _, userId) = await CreateAsync();
        var issued = await service.IssueAsync(userId, OtpPurpose.EmailConfirmation, default);

        await using var db = fixture.CreateAuthDbContext();
        var stored = await db.OtpCodes
            .Where(c => c.UserId == userId)
            .Select(c => c.CodeHash)
            .ToListAsync();

        Assert.NotEmpty(stored);
        Assert.DoesNotContain(issued.Code!, stored);
    }
}
