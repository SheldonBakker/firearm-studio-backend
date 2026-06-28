using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.CreateLicence;

public sealed class CreateLicenceCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CreateLicenceCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(CreateLicenceCommand command, CancellationToken cancellationToken)
    {
        var firearmExists = await db.Firearms.AnyAsync(f => f.Id == command.FirearmId, cancellationToken);
        if (!firearmExists)
        {
            return Error.NotFound(ErrorCodes.FirearmNotFound, "Firearm not found.");
        }

        var request = command.Request;
        var licence = new FirearmLicence
        {
            FirearmId = command.FirearmId,
            LicenceNumber = request.LicenceNumber,
            IssuedOn = request.IssuedOn,
            ExpiresOn = request.ExpiresOn,
            Status = LicenceStatus.Valid,
            DocumentUrl = request.DocumentUrl,
        };

        await db.FirearmLicences.AddAsync(licence, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return licence.Id;
    }

    public static class ErrorCodes
    {
        public const string FirearmNotFound = "CreateLicenceCommand.FirearmNotFound";
    }
}
