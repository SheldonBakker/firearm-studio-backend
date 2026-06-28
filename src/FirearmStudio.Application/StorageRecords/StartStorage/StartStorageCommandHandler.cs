using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.StartStorage;

public sealed class StartStorageCommandHandler(IApplicationDbContext db)
    : ICommandHandler<StartStorageCommand, ErrorOr<Guid>>
{
    public async Task<ErrorOr<Guid>> Handle(StartStorageCommand command, CancellationToken cancellationToken)
    {
        var firearmExists = await db.Firearms.AnyAsync(f => f.Id == command.FirearmId, cancellationToken);
        if (!firearmExists)
        {
            return Error.NotFound(ErrorCodes.FirearmNotFound, "Firearm not found.");
        }

        var request = command.Request;
        var record = new StorageRecord
        {
            FirearmId = command.FirearmId,
            StoredFrom = request.StoredFrom,
            MonthlyRate = request.MonthlyRate,
            StorageStatus = StorageStatus.Active,
            StorageLocation = request.StorageLocation,
            RackNumber = request.RackNumber,
            SafeNumber = request.SafeNumber,
            Notes = request.Notes,
        };

        await db.StorageRecords.AddAsync(record, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return record.Id;
    }

    public static class ErrorCodes
    {
        public const string FirearmNotFound = "StartStorageCommand.FirearmNotFound";
    }
}
