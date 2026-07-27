using System.Text.Json;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using FirearmStudio.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Licences.Reminders;

internal sealed class LicenceReminderGenerator(
    IApplicationDbContext db,
    ILogger<LicenceReminderGenerator> logger) : ILicenceReminderGenerator
{
    public async Task<LicenceReminderRunResult> GenerateAsync(
        LicenceReminderCompany company, DateOnly today, CancellationToken cancellationToken)
    {
        var windowEnd = today.AddDays(90);

        var licences = await db.FirearmLicences
            .Include(l => l.Firearm!).ThenInclude(f => f.Customer)
            .Where(l => l.ExpiresOn <= windowEnd && l.Status != LicenceStatus.Unknown)
            .ToListAsync(cancellationToken);

        if (licences.Count == 0)
        {
            return new LicenceReminderRunResult(0, 0, 0);
        }

        var licenceIds = licences.Select(l => l.Id).ToList();
        var alreadySent = (await db.LicenceReminders
                .Where(r => licenceIds.Contains(r.LicenceId))
                .Select(r => new { r.LicenceId, r.Tier })
                .ToListAsync(cancellationToken))
            .Select(x => (x.LicenceId, x.Tier))
            .ToHashSet();

        var queued = 0;
        var statusesUpdated = 0;
        var skippedNoEmail = 0;

        foreach (var licence in licences)
        {
            var plan = LicenceReminderPlanner.Plan(licence.Status, licence.ExpiresOn, today);

            if (plan.Status != licence.Status)
            {
                licence.Status = plan.Status;
                statusesUpdated++;
            }

            if (plan.Tier is not { } tier || alreadySent.Contains((licence.Id, tier)))
            {
                continue;
            }

            var customer = licence.Firearm?.Customer;
            var email = customer?.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                skippedNoEmail++;
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Skipped licence renewal reminder for licence {LicenceId} ({Tier}): customer has no email.",
                        licence.Id, tier);
                }
                continue;
            }

            var payload = new LicenceRenewalReminderPayload(
                email,
                customer!.CustomerType == CustomerType.Company ? customer.CompanyName : customer.FullName,
                licence.LicenceNumber,
                licence.ExpiresOn,
                licence.ExpiresOn.DayNumber - today.DayNumber,
                tier.ToString(),
                licence.Firearm!.Make,
                licence.Firearm.Model,
                licence.Firearm.SerialNumber,
                company.Id,
                company.Name);

            db.LicenceReminders.Add(new LicenceReminder
            {
                CompanyId = company.Id,
                LicenceId = licence.Id,
                Tier = tier,
            });

            db.OutboxMessages.Add(new OutboxMessage
            {
                Type = OutboxMessageTypes.LicenceRenewalReminder,
                Payload = JsonSerializer.Serialize(payload, OutboxJson.Options),
                CompanyId = company.Id,
            });

            queued++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new LicenceReminderRunResult(queued, statusesUpdated, skippedNoEmail);
    }
}
