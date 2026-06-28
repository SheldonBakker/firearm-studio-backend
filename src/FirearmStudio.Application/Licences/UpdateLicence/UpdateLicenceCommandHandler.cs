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
        if (request.LicenceNumber.IsSet)
        {
            licence.LicenceNumber = request.LicenceNumber.Value;
        }

        if (request.IssuedOn.IsSet)
        {
            licence.IssuedOn = request.IssuedOn.Value;
        }

        if (request.ExpiresOn.IsSet)
        {
            licence.ExpiresOn = request.ExpiresOn.Value;
        }

        if (request.Status.IsSet)
        {
            licence.Status = request.Status.Value;
        }

        if (request.DocumentUrl.IsSet)
        {
            licence.DocumentUrl = request.DocumentUrl.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateLicenceCommand.NotFound";
    }
}
