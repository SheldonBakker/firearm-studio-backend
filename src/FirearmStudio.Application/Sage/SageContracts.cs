namespace FirearmStudio.Application.Sage;

public sealed record RegisterSageConnectionRequest(
    string ApiKey,
    string Username,
    string Password,
    int SageCompanyId);

public sealed record SageConnectionResponse(
    bool Connected,
    int SageCompanyId,
    string SageCompanyName,
    DateTime LastValidatedAt);

public sealed record SageConnectionDetailsResponse(
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
