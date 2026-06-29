using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tenancy;

public sealed class TenantContext(ICurrentUserService currentUserService) : ITenantContext
{
    private int _bypassDepth;

    public Guid? CompanyId => currentUserService.User.CompanyId;

    public bool BypassFilter => _bypassDepth > 0;

    public IDisposable BeginBypass()
    {
        _bypassDepth++;
        return new BypassScope(this);
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
}
