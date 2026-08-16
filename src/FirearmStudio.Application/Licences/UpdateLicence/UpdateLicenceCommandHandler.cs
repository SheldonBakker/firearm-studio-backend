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
        request.LicenceNumber.ApplyTo(v => licence.LicenceNumber = v);
        request.IssuedOn.ApplyTo(v => licence.IssuedOn = v);
        request.ExpiresOn.ApplyTo(v => licence.ExpiresOn = v);
        request.Status.ApplyTo(v => licence.Status = v);
        request.DocumentUrl.ApplyTo(v => licence.DocumentUrl = v);

        if (licence.IssuedOn > licence.ExpiresOn)
        {
            return Error.Validation(ErrorCodes.InvalidDateRange, "IssuedOn must be on or before ExpiresOn.");
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(ErrorCodes.LicenceNumberConflict, "This licence number already exists for the firearm.");
        }

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateLicenceCommand.NotFound";
        public const string InvalidDateRange = "UpdateLicenceCommand.InvalidDateRange";
        public const string LicenceNumberConflict = "UpdateLicenceCommand.LicenceNumberConflict";
    }
}
