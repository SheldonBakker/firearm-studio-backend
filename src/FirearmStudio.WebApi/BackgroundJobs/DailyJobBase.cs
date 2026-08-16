namespace FirearmStudio.WebApi.BackgroundJobs;

public abstract class DailyJobBase(IServiceScopeFactory scopeFactory, ILogger logger)
    : BackgroundJobBase(scopeFactory, logger)
{
    protected abstract int ScheduledHourUtc { get; }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var next = NextScheduledRunUtc(now);

            try
            {
                await Task.Delay(next - now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

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
                LogRunFailed(ex);
            }
        }
    }

    protected abstract void LogRunFailed(Exception ex);
    protected abstract Task RunAsync(CancellationToken cancellationToken);

    private DateTime NextScheduledRunUtc(DateTime nowUtc)
    {
        var todayScheduled = nowUtc.Date.AddHours(ScheduledHourUtc);
        return nowUtc < todayScheduled ? todayScheduled : todayScheduled.AddDays(1);
    }
}
