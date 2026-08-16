using ErrorOr;

namespace FirearmStudio.Application.Abstractions;

public interface IAccountingConnectionValidator
{
    Task<ErrorOr<AccountingCompanySummary>> ValidateConnectionAsync(
        AccountingCredentials credentials,
        CancellationToken cancellationToken);
}

public sealed record AccountingCredentials(
    string ApiKey,
    string Username,
    string Password,
    int SageCompanyId);

public sealed record AccountingCompanySummary(int Id, string Name);
