using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.UpdateLicence;

public sealed class UpdateLicenceCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateLicenceCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateLicenceCommand command, CancellationToken cancellationToken)
    {
        var licence = await db.FirearmLicences.FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken);
        if (licence is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Licence not found.");
        }

        var request = command.Request;
        licence.LicenceNumber = request.LicenceNumber ?? licence.LicenceNumber;
        licence.IssuedOn = request.IssuedOn ?? licence.IssuedOn;
        if (request.ExpiresOn is { } expires)
        {
            licence.ExpiresOn = expires;
        }
        if (request.Status is { } status)
        {
            licence.Status = status;
        }
        licence.DocumentUrl = request.DocumentUrl ?? licence.DocumentUrl;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateLicenceCommand.NotFound";
    }
}
