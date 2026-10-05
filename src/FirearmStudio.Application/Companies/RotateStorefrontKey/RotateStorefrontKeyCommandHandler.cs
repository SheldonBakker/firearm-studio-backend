using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies.RotateStorefrontKey;

public sealed class RotateStorefrontKeyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : ICommandHandler<RotateStorefrontKeyCommand, ErrorOr<StorefrontKeyResult>>
{
    public async Task<ErrorOr<StorefrontKeyResult>> Handle(RotateStorefrontKeyCommand command, CancellationToken cancellationToken)
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

        var newKey = StorefrontKey.Generate();
        company.StorefrontKey = newKey;

        await StorefrontAudit.AddAsync(db, currentUserService, companyId, StorefrontAudit.Rotated, StorefrontKey.AuditSuffix(newKey), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new StorefrontKeyResult(companyId, newKey);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "RotateStorefrontKeyCommand.NotFound";
    }
}
