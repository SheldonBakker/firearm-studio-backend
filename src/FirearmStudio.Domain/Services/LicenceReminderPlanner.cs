using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Services;

public readonly record struct LicenceReminderPlan(LicenceReminderTier? Tier, LicenceStatus Status);

public static class LicenceReminderPlanner
{
    public static LicenceReminderPlan Plan(LicenceStatus currentStatus, DateOnly expiresOn, DateOnly today)
    {
        if (currentStatus == LicenceStatus.Unknown)
        {
            return new LicenceReminderPlan(null, LicenceStatus.Unknown);
        }

        var daysRemaining = expiresOn.DayNumber - today.DayNumber;

        var tier = daysRemaining switch
        {
            < 0 => LicenceReminderTier.Expired,
            <= 30 => LicenceReminderTier.Days30,
            <= 60 => LicenceReminderTier.Days60,
            <= LicenceConstants.RenewalWindowDays => LicenceReminderTier.Days90,
            _ => (LicenceReminderTier?)null,
        };

        var status = daysRemaining switch
        {
            < 0 => LicenceStatus.Expired,
            <= LicenceConstants.RenewalWindowDays => LicenceStatus.RenewalDue,
            _ => LicenceStatus.Valid,
        };

        return new LicenceReminderPlan(tier, status);
    }
}
