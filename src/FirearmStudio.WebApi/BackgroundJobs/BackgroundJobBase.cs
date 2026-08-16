using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.WebApi.BackgroundJobs;

public abstract class BackgroundJobBase(IServiceScopeFactory scopeFactory, ILogger logger) : BackgroundService
{
    private bool _migrationsVerified;

    protected IServiceScopeFactory ScopeFactory => scopeFactory;
    protected ILogger Logger => logger;

    protected virtual async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
    }

    protected async Task<bool> EnsureMigrationsVerifiedAsync(
        IServiceScope scope, string jobName, CancellationToken cancellationToken)
    {
        if (_migrationsVerified)
        {
            return true;
        }

        var pending = await GetPendingMigrationsAsync(scope, cancellationToken);

        if (pending.Count > 0)
        {
            logger.LogError(
                "Skipping {JobName}: {Count} pending database migration(s): {Migrations}. " +
                "Apply migrations and the job will resume on its next tick.",
                jobName, pending.Count, string.Join(", ", pending));
            return false;
        }

        _migrationsVerified = true;
        return true;
    }

    protected async Task RunForAllCompaniesAsync<T>(
        IReadOnlyList<T> companies,
        Func<T, Guid> getCompanyId,
        Func<IServiceScope, T, CancellationToken, Task> runForCompany,
        Action<Exception, Guid> onCompanyFailed,
        CancellationToken cancellationToken)
    {
        foreach (var company in companies)
        {
            var companyId = getCompanyId(company);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();

                using (tenant.BeginCompanyScope(companyId))
                {
                    await runForCompany(scope, company, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                onCompanyFailed(ex, companyId);
            }
        }
    }
}
