namespace FirearmStudio.Application.Accounting;

public sealed record RegisterAccountingConnectionRequest(
    string ApiKey,
    string Username,
    string Password,
    int ExternalCompanyId);

public sealed record AccountingConnectionResponse(
    bool Connected,
    int ExternalCompanyId,
    string ExternalCompanyName,
    DateTime LastValidatedAt);

public sealed record AccountingConnectionDetailsResponse(
    Guid Id,
    Guid CompanyId,
    bool ApiKey,
    bool Username,
    bool Password,
    int ExternalCompanyId,
    string ExternalCompanyName,
    DateTime LastValidatedAt,
    Guid LastRegisteredByAuthUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
