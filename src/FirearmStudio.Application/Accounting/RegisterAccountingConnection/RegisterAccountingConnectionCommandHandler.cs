using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Accounting.RegisterAccountingConnection;

public sealed class RegisterAccountingConnectionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    IAccountingConnectionValidator accountingConnectionValidator,
    ICredentialProtector credentialProtector)
    : ICommandHandler<RegisterAccountingConnectionCommand, ErrorOr<AccountingConnectionResponse>>
{
    public async Task<ErrorOr<AccountingConnectionResponse>> Handle(
        RegisterAccountingConnectionCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.User.IsAuthenticated)
        {
            return Error.Unauthorized(ErrorCodes.Unauthorized, "A valid session is required.");
        }

        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        var request = command.Request;
        var credentials = new AccountingCredentials(
            request.ApiKey,
            request.Username,
            request.Password,
            request.SageCompanyId);

        var accountingCompanyResult = await accountingConnectionValidator.ValidateConnectionAsync(credentials, cancellationToken);
        if (accountingCompanyResult.IsError)
        {
            return accountingCompanyResult.Errors;
        }

        var accountingCompany = accountingCompanyResult.Value;
        var validatedAt = DateTime.UtcNow;
        var apiKeyCiphertext = credentialProtector.Protect(request.ApiKey);
        var usernameCiphertext = credentialProtector.Protect(request.Username);
        var passwordCiphertext = credentialProtector.Protect(request.Password);

        var connection = await db.AccountingConnections
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);

        if (connection is null)
        {
            connection = new AccountingConnection
            {
                ApiKeyCiphertext = apiKeyCiphertext,
                UsernameCiphertext = usernameCiphertext,
                PasswordCiphertext = passwordCiphertext,
                SageCompanyId = accountingCompany.Id,
                SageCompanyName = accountingCompany.Name,
                LastValidatedAt = validatedAt,
                LastRegisteredByAuthUserId = currentUserService.User.Id,
            };

            db.AccountingConnections.Add(connection);
        }
        else
        {
            connection.ApiKeyCiphertext = apiKeyCiphertext;
            connection.UsernameCiphertext = usernameCiphertext;
            connection.PasswordCiphertext = passwordCiphertext;
            connection.SageCompanyId = accountingCompany.Id;
            connection.SageCompanyName = accountingCompany.Name;
            connection.LastValidatedAt = validatedAt;
            connection.LastRegisteredByAuthUserId = currentUserService.User.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AccountingConnectionResponse(
            Connected: true,
            SageCompanyId: connection.SageCompanyId,
            SageCompanyName: connection.SageCompanyName,
            LastValidatedAt: connection.LastValidatedAt);
    }

    public static class ErrorCodes
    {
        public const string Unauthorized = "RegisterAccountingConnectionCommand.Unauthorized";
        public const string CompanyNotFound = "RegisterAccountingConnectionCommand.CompanyNotFound";
    }
}
