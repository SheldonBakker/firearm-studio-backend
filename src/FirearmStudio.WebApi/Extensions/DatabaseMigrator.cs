using FirearmStudio.Infrastructure.Identity;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.Extensions;

public static class DatabaseMigrator
{
    public const string Switch = "--migrate";

    public static bool IsRequested(string[] args) => args.Contains(Switch, StringComparer.Ordinal);

    public static async Task<int> MigrateAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrator));
        try
        {
            await MigrateAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), logger, cancellationToken);
            await MigrateAsync(scope.ServiceProvider.GetRequiredService<AuthDbContext>(), logger, cancellationToken);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            return 1;
        }
    }

    public static async Task MigrateAsync(DbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        var name = context.GetType().Name;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Context}: {Count} pending migration(s) {Migrations}", name, pending.Count, pending);
        }

        await context.Database.MigrateAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{Context}: migrations applied.", name);
        }
    }
}
