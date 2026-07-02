using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tenancy;

public sealed class TenantContext(ICurrentUserService currentUserService) : ITenantContext
{
    private int _bypassDepth;
    private Guid? _companyOverride;
    
    public Guid? CompanyId => _companyOverride ?? currentUserService.User.CompanyId;

    public bool BypassFilter => _bypassDepth > 0;

    public IDisposable BeginBypass()
    {
        _bypassDepth++;
        return new BypassScope(this);
    }

    public IDisposable BeginCompanyScope(Guid companyId)
    {
        var previous = _companyOverride;
        _companyOverride = companyId;
        return new CompanyScope(this, previous);
    }

    private sealed class BypassScope(TenantContext owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner._bypassDepth--;
        }
    }

    private sealed class CompanyScope(TenantContext owner, Guid? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            owner._companyOverride = previous;
        }
    }
}
