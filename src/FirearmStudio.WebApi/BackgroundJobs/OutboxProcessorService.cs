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
    private const int MaxAttempts = 5;

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
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pendingMigrations.Count > 0)
        {
            logger.LogError(
                "Skipping outbox processing: {Count} pending database migration(s): {Migrations}. " +
                "Apply migrations and the job will resume on its next tick.",
                pendingMigrations.Count, string.Join(", ", pendingMigrations));
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IBookingRequestedDispatcher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

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
                    default:
                        throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'.");
                }

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                logger.LogError(
                    ex,
                    "Failed to dispatch outbox message {MessageId} (attempt {Attempt} of {MaxAttempts}).",
                    message.Id, message.Attempts, MaxAttempts);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
