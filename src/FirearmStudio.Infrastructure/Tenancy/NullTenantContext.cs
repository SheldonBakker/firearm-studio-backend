using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tenancy;

public sealed class NullTenantContext : ITenantContext
{
    public Guid? CompanyId => null;
    public bool BypassFilter => true;
    public IDisposable BeginBypass() => new NoopScope();
    public IDisposable BeginCompanyScope(Guid companyId) => new NoopScope();

    private sealed class NoopScope : IDisposable
    {
        public void Dispose() { }
    }
}
