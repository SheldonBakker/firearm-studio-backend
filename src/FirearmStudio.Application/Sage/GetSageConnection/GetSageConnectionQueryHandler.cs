using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Sage.GetSageConnection;

public sealed class GetSageConnectionQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetSageConnectionQuery, ErrorOr<SageConnectionDetailsResponse>>
{
    public async Task<ErrorOr<SageConnectionDetailsResponse>> Handle(
        GetSageConnectionQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Sage connection not found.");
        }

        var connection = await db.SageConnections
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => new
            {
                x.Id,
                x.CompanyId,
                ApiKeyCiphertext = (string?)x.ApiKeyCiphertext,
                UsernameCiphertext = (string?)x.UsernameCiphertext,
                PasswordCiphertext = (string?)x.PasswordCiphertext,
                x.SageCompanyId,
                x.SageCompanyName,
                x.LastValidatedAt,
                x.LastRegisteredByAuthUserId,
                x.CreatedAt,
                x.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return connection is null
            ? Error.NotFound(ErrorCodes.NotFound, "Sage connection not found.")
            : new SageConnectionDetailsResponse(
                connection.Id,
                connection.CompanyId,
                connection.ApiKeyCiphertext is not null,
                connection.UsernameCiphertext is not null,
                connection.PasswordCiphertext is not null,
                connection.SageCompanyId,
                connection.SageCompanyName,
                connection.LastValidatedAt,
                connection.LastRegisteredByAuthUserId,
                connection.CreatedAt,
                connection.UpdatedAt);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetSageConnectionQuery.NotFound";
    }
}
