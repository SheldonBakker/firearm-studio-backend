namespace FirearmStudio.Application.Accounting;

public sealed record RegisterAccountingConnectionRequest(
    string ApiKey,
    string Username,
    string Password,
    int SageCompanyId);

public sealed record AccountingConnectionResponse(
    bool Connected,
    int SageCompanyId,
    string SageCompanyName,
    DateTime LastValidatedAt);

public sealed record AccountingConnectionDetailsResponse(
    Guid Id,
    Guid CompanyId,
    bool ApiKey,
    bool Username,
    bool Password,
    int SageCompanyId,
    string SageCompanyName,
    DateTime LastValidatedAt,
    Guid LastRegisteredByAuthUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
