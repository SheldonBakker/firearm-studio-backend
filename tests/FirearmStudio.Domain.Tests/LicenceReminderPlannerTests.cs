using FirearmStudio.Domain.Enums;
using FirearmStudio.Domain.Services;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class LicenceReminderPlannerTests
{
    private static readonly DateOnly Today = new(2026, 7, 26);

    private static LicenceReminderPlan PlanWithDaysRemaining(
        int daysRemaining, LicenceStatus currentStatus = LicenceStatus.Valid)
        => LicenceReminderPlanner.Plan(currentStatus, Today.AddDays(daysRemaining), Today);

    [Theory]
    [InlineData(120, null)]
    [InlineData(91, null)]
    [InlineData(90, LicenceReminderTier.Days90)]
    [InlineData(61, LicenceReminderTier.Days90)]
    [InlineData(60, LicenceReminderTier.Days60)]
    [InlineData(31, LicenceReminderTier.Days60)]
    [InlineData(30, LicenceReminderTier.Days30)]
    [InlineData(1, LicenceReminderTier.Days30)]
    [InlineData(0, LicenceReminderTier.Days30)]
    [InlineData(-1, LicenceReminderTier.Expired)]
    [InlineData(-365, LicenceReminderTier.Expired)]
    public void Plan_returns_expected_tier(int daysRemaining, LicenceReminderTier? expectedTier)
    {
        var plan = PlanWithDaysRemaining(daysRemaining);

        Assert.Equal(expectedTier, plan.Tier);
    }

    [Theory]
    [InlineData(120, LicenceStatus.Valid)]
    [InlineData(91, LicenceStatus.Valid)]
    [InlineData(90, LicenceStatus.RenewalDue)]
    [InlineData(30, LicenceStatus.RenewalDue)]
    [InlineData(0, LicenceStatus.RenewalDue)]
    [InlineData(-1, LicenceStatus.Expired)]
    public void Plan_returns_expected_status(int daysRemaining, LicenceStatus expectedStatus)
    {
        var plan = PlanWithDaysRemaining(daysRemaining);

        Assert.Equal(expectedStatus, plan.Status);
    }

    [Theory]
    [InlineData(120)]
    [InlineData(45)]
    [InlineData(-10)]
    public void Plan_skips_unknown_licences_entirely(int daysRemaining)
    {
        var plan = PlanWithDaysRemaining(daysRemaining, LicenceStatus.Unknown);

        Assert.Null(plan.Tier);
        Assert.Equal(LicenceStatus.Unknown, plan.Status);
    }

    [Fact]
    public void Plan_recovers_status_when_expiry_moves_out()
    {
        // A licence marked Expired whose ExpiresOn was corrected to the future goes back to Valid.
        var plan = PlanWithDaysRemaining(120, LicenceStatus.Expired);

        Assert.Equal(LicenceStatus.Valid, plan.Status);
        Assert.Null(plan.Tier);
    }
}
