namespace FirearmStudio.WebApi.BackgroundJobs;

public abstract class PeriodicJobBase(IServiceScopeFactory scopeFactory, ILogger logger)
    : BackgroundJobBase(scopeFactory, logger)
{
    protected abstract TimeSpan Interval { get; }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
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
                LogRunFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    protected abstract void LogRunFailed(Exception ex);
    protected abstract Task RunAsync(CancellationToken cancellationToken);
}
