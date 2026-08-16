using FirearmStudio.Application.Abstractions;
using FirearmStudio.WebApi.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FirearmStudio.WebApi.Tests.BackgroundJobs;

public sealed class BackgroundJobBaseTests
{
    [Fact]
    public async Task Migration_gate_returns_false_when_pending_migrations_exist()
    {
        var scopeFactory = BuildScopeFactory();
        var job = new TestableJob(scopeFactory) { HasPendingMigrations = true };
        using var scope = scopeFactory.CreateScope();

        var result = await job.InvokeEnsureMigrationsAsync(scope, "test job", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Migration_gate_returns_true_when_no_pending_migrations()
    {
        var scopeFactory = BuildScopeFactory();
        var job = new TestableJob(scopeFactory) { HasPendingMigrations = false };
        using var scope = scopeFactory.CreateScope();

        var result = await job.InvokeEnsureMigrationsAsync(scope, "test job", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Migration_gate_does_not_recheck_after_passing()
    {
        var scopeFactory = BuildScopeFactory();
        var job = new TestableJob(scopeFactory) { HasPendingMigrations = false };
        using var scope = scopeFactory.CreateScope();

        await job.InvokeEnsureMigrationsAsync(scope, "test job", CancellationToken.None);
        job.MigrationCheckCount = 0;

        var result = await job.InvokeEnsureMigrationsAsync(scope, "test job", CancellationToken.None);

        Assert.True(result);
        Assert.Equal(0, job.MigrationCheckCount);
    }

    [Fact]
    public async Task Error_isolation_continues_past_a_throwing_company()
    {
        var scopeFactory = BuildScopeFactory(withTenant: true);
        var job = new TestableJob(scopeFactory);
        var companies = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var throwingId = companies[1];
        var processed = new List<Guid>();
        var failedIds = new List<Guid>();

        await job.InvokeRunForAllCompaniesAsync(
            companies,
            static id => id,
            (_, id, _) =>
            {
                if (id == throwingId)
                {
                    throw new InvalidOperationException("simulated failure");
                }

                processed.Add(id);
                return Task.CompletedTask;
            },
            (_, id) => failedIds.Add(id),
            CancellationToken.None);

        Assert.Equal(2, processed.Count);
        Assert.Contains(companies[0], processed);
        Assert.Contains(companies[2], processed);
        Assert.Single(failedIds);
        Assert.Equal(throwingId, failedIds[0]);
    }

    [Fact]
    public async Task Cancellation_propagates_out_of_company_loop()
    {
        var scopeFactory = BuildScopeFactory(withTenant: true);
        var job = new TestableJob(scopeFactory);
        var companies = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            job.InvokeRunForAllCompaniesAsync(
                companies,
                static id => id,
                (_, _, _) => Task.CompletedTask,
                (_, _) => { },
                cts.Token));
    }

    [Fact]
    public async Task Cancellation_inside_company_work_propagates()
    {
        var scopeFactory = BuildScopeFactory(withTenant: true);
        var job = new TestableJob(scopeFactory);
        var companies = new List<Guid> { Guid.NewGuid() };
        using var cts = new CancellationTokenSource();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            job.InvokeRunForAllCompaniesAsync(
                companies,
                static id => id,
                (_, _, ct) =>
                {
                    cts.Cancel();
                    ct.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                },
                (_, _) => { },
                cts.Token));
    }

    private static FakeScopeFactory BuildScopeFactory(bool withTenant = false)
    {
        var services = new ServiceCollection();
        if (withTenant)
        {
            services.AddSingleton<ITenantContext, FakeTenantContext>();
        }

        return new FakeScopeFactory(services.BuildServiceProvider());
    }
}

internal sealed class TestableJob(IServiceScopeFactory scopeFactory) : BackgroundJobBase(scopeFactory, NullLogger.Instance)
{
    public bool HasPendingMigrations { get; set; }
    public int MigrationCheckCount { get; set; }

    protected override Task<IReadOnlyList<string>> GetPendingMigrationsAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        MigrationCheckCount++;
        IReadOnlyList<string> result = HasPendingMigrations ? ["Migration1"] : [];
        return Task.FromResult(result);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public Task<bool> InvokeEnsureMigrationsAsync(IServiceScope scope, string jobName, CancellationToken ct)
        => EnsureMigrationsVerifiedAsync(scope, jobName, ct);

    public Task InvokeRunForAllCompaniesAsync<T>(
        IReadOnlyList<T> companies,
        Func<T, Guid> getCompanyId,
        Func<IServiceScope, T, CancellationToken, Task> runForCompany,
        Action<Exception, Guid> onCompanyFailed,
        CancellationToken cancellationToken)
        => RunForAllCompaniesAsync(companies, getCompanyId, runForCompany, onCompanyFailed, cancellationToken);
}

internal sealed class FakeScopeFactory(IServiceProvider provider) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new FakeScope(provider);
}

internal sealed class FakeScope(IServiceProvider provider) : IServiceScope
{
    public IServiceProvider ServiceProvider => provider;
    public void Dispose() { }
}

internal sealed class FakeTenantContext : ITenantContext
{
    public Guid? CompanyId { get; private set; }
    public bool BypassFilter => false;
    public IDisposable BeginBypass() => new FakeDisposable();

    public IDisposable BeginCompanyScope(Guid id)
    {
        CompanyId = id;
        return new FakeDisposable(() => CompanyId = null);
    }
}

internal sealed class FakeDisposable(Action? cleanup = null) : IDisposable
{
    public void Dispose() => cleanup?.Invoke();
}
