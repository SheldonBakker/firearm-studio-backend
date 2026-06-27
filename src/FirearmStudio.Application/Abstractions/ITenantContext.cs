namespace FirearmStudio.Application.Abstractions;

public interface ITenantContext
{
    Guid? CompanyId { get; }

    bool HasTenant => CompanyId is not null;

    bool BypassFilter { get; }

    IDisposable BeginBypass();
}
