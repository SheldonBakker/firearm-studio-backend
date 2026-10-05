using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies.RevokeStorefrontKey;

public sealed class RevokeStorefrontKeyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : ICommandHandler<RevokeStorefrontKeyCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(RevokeStorefrontKeyCommand command, CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        if (company.StorefrontKey is null)
        {
            return Result.Deleted;
        }

        var oldSuffix = StorefrontKey.AuditSuffix(company.StorefrontKey);
        company.StorefrontKey = null;

        await StorefrontAudit.AddAsync(db, currentUserService, companyId, StorefrontAudit.Revoked, oldSuffix, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "RevokeStorefrontKeyCommand.NotFound";
    }
}
