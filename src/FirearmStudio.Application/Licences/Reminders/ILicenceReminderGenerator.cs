namespace FirearmStudio.Application.Licences.Reminders;

public sealed record LicenceReminderCompany(Guid Id, string Name);

public sealed record LicenceReminderRunResult(int RemindersQueued, int StatusesUpdated, int SkippedNoEmail);

public interface ILicenceReminderGenerator
{
    Task<LicenceReminderRunResult> GenerateAsync(
        LicenceReminderCompany company, DateOnly today, CancellationToken cancellationToken);
}
