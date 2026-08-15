using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class BypassTenantContext : ITenantContext
{
    private bool _bypass;

    public Guid? CompanyId { get; set; }

    public bool BypassFilter => _bypass;

    public IDisposable BeginBypass()
    {
        var previous = _bypass;
        _bypass = true;
        return new Scope(() => _bypass = previous);
    }

    public IDisposable BeginCompanyScope(Guid companyId)
    {
        var previous = CompanyId;
        CompanyId = companyId;
        return new Scope(() => CompanyId = previous);
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
