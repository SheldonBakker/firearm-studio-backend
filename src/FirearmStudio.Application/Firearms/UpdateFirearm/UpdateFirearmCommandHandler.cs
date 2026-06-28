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
            return Error.NotFound("UpdateFirearmCommand.NotFound", "Firearm not found.");
        }

        var request = command.Request;
        firearm.Model = request.Model ?? firearm.Model;
        firearm.Calibre = request.Calibre ?? firearm.Calibre;
        firearm.FirearmType = request.FirearmType ?? firearm.FirearmType;
        firearm.Notes = request.Notes ?? firearm.Notes;
        if (request.Status is { } status)
        {
            firearm.Status = status;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
