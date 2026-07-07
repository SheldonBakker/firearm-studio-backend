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
