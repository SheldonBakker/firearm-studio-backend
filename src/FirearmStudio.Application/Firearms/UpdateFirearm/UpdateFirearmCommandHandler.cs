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
        request.Model.ApplyTo(v => firearm.Model = v);
        request.Calibre.ApplyTo(v => firearm.Calibre = v);
        request.FirearmType.ApplyTo(v => firearm.FirearmType = v);
        request.Notes.ApplyTo(v => firearm.Notes = v);
        request.Status.ApplyTo(v => firearm.Status = v);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateFirearmCommand.NotFound";
    }
}
