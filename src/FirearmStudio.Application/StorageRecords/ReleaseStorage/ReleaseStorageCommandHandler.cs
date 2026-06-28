using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.StorageRecords.ReleaseStorage;

public sealed class ReleaseStorageCommandHandler(IApplicationDbContext db)
    : ICommandHandler<ReleaseStorageCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(ReleaseStorageCommand command, CancellationToken cancellationToken)
    {
        var record = await db.StorageRecords.FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken);
        if (record is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Storage record not found.");
        }

        record.StoredUntil = command.Request?.StoredUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        record.StorageStatus = StorageStatus.Released;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "ReleaseStorageCommand.NotFound";
    }
}
