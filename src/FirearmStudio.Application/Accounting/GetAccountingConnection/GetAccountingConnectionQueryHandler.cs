using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Accounting.GetAccountingConnection;

public sealed class GetAccountingConnectionQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetAccountingConnectionQuery, ErrorOr<AccountingConnectionDetailsResponse?>>
{
    public async Task<ErrorOr<AccountingConnectionDetailsResponse?>> Handle(
        GetAccountingConnectionQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.Unauthorized(
                ErrorCodes.CompanyContextRequired,
                "The authenticated session is not associated with a company.");
        }

        var connection = await db.AccountingConnections
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
            ? (AccountingConnectionDetailsResponse?)null
            : new AccountingConnectionDetailsResponse(
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
        public const string CompanyContextRequired = "GetAccountingConnectionQuery.CompanyContextRequired";
    }
}
