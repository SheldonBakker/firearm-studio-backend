using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tenancy;

public sealed class TenantContext(ICurrentUserService currentUserService) : ITenantContext
{
    private bool _bypass;

    public Guid? CompanyId => currentUserService.User.CompanyId;

    public bool BypassFilter => _bypass;

    public IDisposable BeginBypass()
    {
        _bypass = true;
        return new BypassScope(this);
    }

    private sealed class BypassScope(TenantContext owner) : IDisposable
    {
        public void Dispose() => owner._bypass = false;
    }
}
