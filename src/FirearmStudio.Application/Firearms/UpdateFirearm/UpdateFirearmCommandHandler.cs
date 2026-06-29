using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.UpdateFirearm;

public sealed class UpdateFirearmCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateFirearmCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateFirearmCommand command, CancellationToken cancellationToken)
    {
        var firearm = await db.Firearms.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (firearm is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Firearm not found.");
        }

        var request = command.Request;
        if (request.Model.IsSet)
        {
            firearm.Model = request.Model.Value;
        }

        if (request.Calibre.IsSet)
        {
            firearm.Calibre = request.Calibre.Value;
        }

        if (request.FirearmType.IsSet)
        {
            firearm.FirearmType = request.FirearmType.Value;
        }

        if (request.Notes.IsSet)
        {
            firearm.Notes = request.Notes.Value;
        }

        if (request.Status.IsSet)
        {
            firearm.Status = request.Status.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateFirearmCommand.NotFound";
    }
}
