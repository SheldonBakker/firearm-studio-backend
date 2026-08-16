namespace FirearmStudio.Application.Abstractions;

public interface ITenantContext
{
    Guid? CompanyId { get; }

    bool BypassFilter { get; }

    IDisposable BeginBypass();
    IDisposable BeginCompanyScope(Guid companyId);
}
