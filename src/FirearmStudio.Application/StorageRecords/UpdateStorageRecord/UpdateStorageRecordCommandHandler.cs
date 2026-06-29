using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
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

        if (record.StoredUntil < record.StoredFrom)
        {
            return Error.Validation(ErrorCodes.InvalidDateRange, "StoredUntil must be on or after StoredFrom.");
        }

        if (record.StorageStatus == StorageStatus.Active && record.StoredUntil is not null)
        {
            return Error.Validation(ErrorCodes.InvalidActiveState, "An active storage record cannot have StoredUntil set.");
        }

        if (record.StorageStatus != StorageStatus.Active && record.StoredUntil is null)
        {
            return Error.Validation(ErrorCodes.InvalidClosedState, "A released or cancelled storage record requires StoredUntil.");
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error.Conflict(ErrorCodes.ActiveStorageConflict, "The firearm already has an active storage record.");
        }

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateStorageRecordCommand.NotFound";
        public const string InvalidDateRange = "UpdateStorageRecordCommand.InvalidDateRange";
        public const string InvalidActiveState = "UpdateStorageRecordCommand.InvalidActiveState";
        public const string InvalidClosedState = "UpdateStorageRecordCommand.InvalidClosedState";
        public const string ActiveStorageConflict = "UpdateStorageRecordCommand.ActiveStorageConflict";
    }
}
