using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Sage.RegisterSageConnection;

public sealed class RegisterSageConnectionCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService,
    ISageAccountingClient sageAccountingClient,
    ICredentialProtector credentialProtector)
    : ICommandHandler<RegisterSageConnectionCommand, ErrorOr<SageConnectionResponse>>
{
    public async Task<ErrorOr<SageConnectionResponse>> Handle(
        RegisterSageConnectionCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.User.IsAuthenticated)
        {
            return Error.Unauthorized(ErrorCodes.Unauthorized, "A valid Supabase session is required.");
        }

        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        var request = command.Request;
        var credentials = new SageCredentials(
            request.ApiKey,
            request.Username,
            request.Password,
            request.SageCompanyId);

        var sageCompanyResult = await sageAccountingClient.ValidateConnectionAsync(credentials, cancellationToken);
        if (sageCompanyResult.IsError)
        {
            return sageCompanyResult.Errors;
        }

        var sageCompany = sageCompanyResult.Value;
        var validatedAt = DateTime.UtcNow;
        var apiKeyCiphertext = credentialProtector.Protect(request.ApiKey);
        var usernameCiphertext = credentialProtector.Protect(request.Username);
        var passwordCiphertext = credentialProtector.Protect(request.Password);

        var connection = await db.SageConnections
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);

        if (connection is null)
        {
            connection = new SageConnection
            {
                ApiKeyCiphertext = apiKeyCiphertext,
                UsernameCiphertext = usernameCiphertext,
                PasswordCiphertext = passwordCiphertext,
                SageCompanyId = sageCompany.Id,
                SageCompanyName = sageCompany.Name,
                LastValidatedAt = validatedAt,
                LastRegisteredByAuthUserId = currentUserService.User.Id,
            };

            db.SageConnections.Add(connection);
        }
        else
        {
            connection.ApiKeyCiphertext = apiKeyCiphertext;
            connection.UsernameCiphertext = usernameCiphertext;
            connection.PasswordCiphertext = passwordCiphertext;
            connection.SageCompanyId = sageCompany.Id;
            connection.SageCompanyName = sageCompany.Name;
            connection.LastValidatedAt = validatedAt;
            connection.LastRegisteredByAuthUserId = currentUserService.User.Id;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new SageConnectionResponse(
            Connected: true,
            SageCompanyId: connection.SageCompanyId,
            SageCompanyName: connection.SageCompanyName,
            LastValidatedAt: connection.LastValidatedAt);
    }

    public static class ErrorCodes
    {
        public const string Unauthorized = "RegisterSageConnectionCommand.Unauthorized";
        public const string CompanyNotFound = "RegisterSageConnectionCommand.CompanyNotFound";
    }
}
