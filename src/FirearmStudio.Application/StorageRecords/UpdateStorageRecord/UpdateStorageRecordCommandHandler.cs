using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.UpdateStorageRecord;

public sealed class UpdateStorageRecordCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateStorageRecordCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateStorageRecordCommand command, CancellationToken cancellationToken)
    {
        var record = await db.StorageRecords.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (record is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Storage record not found.");
        }

        var request = command.Request;
        if (request.StoredFrom.IsSet)
        {
            record.StoredFrom = request.StoredFrom.Value;
        }

        if (request.StoredUntil.IsSet)
        {
            record.StoredUntil = request.StoredUntil.Value;
        }

        if (request.MonthlyRate.IsSet)
        {
            record.MonthlyRate = request.MonthlyRate.Value;
        }

        if (request.StorageStatus.IsSet)
        {
            record.StorageStatus = request.StorageStatus.Value;
        }

        if (request.StorageLocation.IsSet)
        {
            record.StorageLocation = request.StorageLocation.Value;
        }

        if (request.RackNumber.IsSet)
        {
            record.RackNumber = request.RackNumber.Value;
        }

        if (request.SafeNumber.IsSet)
        {
            record.SafeNumber = request.SafeNumber.Value;
        }

        if (request.Notes.IsSet)
        {
            record.Notes = request.Notes.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateStorageRecordCommand.NotFound";
    }
}
