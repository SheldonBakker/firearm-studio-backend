using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    private bool _migrationsVerified;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        if (!_migrationsVerified)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count > 0)
            {
                logger.LogError(
                    "Skipping outbox processing: {Count} pending database migration(s): {Migrations}. " +
                    "Apply migrations and the job will resume on its next tick.",
                    pending.Count, string.Join(", ", pending));
                return;
            }

            _migrationsVerified = true;
        }

        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IBookingRequestedDispatcher>();
        var licenceReminderDispatcher = scope.ServiceProvider.GetRequiredService<ILicenceRenewalReminderDispatcher>();
        var bookingLifecycleDispatcher = scope.ServiceProvider.GetRequiredService<IBookingLifecycleDispatcher>();

        var messages = await db.ClaimOutboxBatchAsync(BatchSize, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                switch (message.Type)
                {
                    case OutboxMessageTypes.BookingRequested:
                        await dispatcher.DispatchAsync(message.Payload, cancellationToken);
                        break;
                    case OutboxMessageTypes.LicenceRenewalReminder:
                        await licenceReminderDispatcher.DispatchAsync(message.Payload, cancellationToken);
                        break;
                    case OutboxMessageTypes.BookingConfirmed:
                    case OutboxMessageTypes.BookingReminder:
                    case OutboxMessageTypes.BookingCancelled:
                        await bookingLifecycleDispatcher.DispatchAsync(message.Type, message.Payload, cancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'.");
                }

                await db.MarkOutboxProcessedAsync(message.Id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to dispatch outbox message {MessageId} (attempt {Attempt} of {MaxAttempts}).",
                    message.Id, message.Attempts + 1, OutboxMessageTypes.MaxAttempts);
                await db.MarkOutboxFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}
