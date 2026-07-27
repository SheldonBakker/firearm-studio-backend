using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Domain.Services;

public readonly record struct LicenceReminderPlan(LicenceReminderTier? Tier, LicenceStatus Status);

/// <summary>
/// Pure scheduling rules for licence renewal reminders. Only the tier the licence is
/// currently in is ever returned; missed earlier tiers are never backfilled. Licences
/// with status <see cref="LicenceStatus.Unknown"/> are left untouched: Unknown is a
/// data-quality signal, not a scheduling state.
/// </summary>
public static class LicenceReminderPlanner
{
    public static LicenceReminderPlan Plan(LicenceStatus currentStatus, DateOnly expiresOn, DateOnly today)
    {
        if (currentStatus == LicenceStatus.Unknown)
        {
            return new LicenceReminderPlan(null, LicenceStatus.Unknown);
        }

        var daysRemaining = expiresOn.DayNumber - today.DayNumber;

        // The 90-day boundary matches the renewal_due_on computed column (expires_on - 90).
        var tier = daysRemaining switch
        {
            < 0 => LicenceReminderTier.Expired,
            <= 30 => LicenceReminderTier.Days30,
            <= 60 => LicenceReminderTier.Days60,
            <= 90 => LicenceReminderTier.Days90,
            _ => (LicenceReminderTier?)null,
        };

        var status = daysRemaining switch
        {
            < 0 => LicenceStatus.Expired,
            <= 90 => LicenceStatus.RenewalDue,
            _ => LicenceStatus.Valid,
        };

        return new LicenceReminderPlan(tier, status);
    }
}
