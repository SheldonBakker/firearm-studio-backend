using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Persistence;

namespace FirearmStudio.WebApi.BackgroundJobs;

public sealed class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger)
    : PeriodicJobBase(scopeFactory, logger)
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    protected override TimeSpan Interval => _interval;
    protected override void LogRunFailed(Exception ex) =>
        logger.LogError(ex, "Outbox processing run failed.");

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();

        if (!await EnsureMigrationsVerifiedAsync(scope, "outbox processing", cancellationToken))
        {
            return;
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
