using ErrorOr;

namespace FirearmStudio.Application.Abstractions;

public interface ISageAccountingClient
{
    Task<ErrorOr<SageCompanySummary>> ValidateConnectionAsync(
        SageCredentials credentials,
        CancellationToken cancellationToken);
}

public sealed record SageCredentials(
    string ApiKey,
    string Username,
    string Password,
    int SageCompanyId);

public sealed record SageCompanySummary(int Id, string Name);
