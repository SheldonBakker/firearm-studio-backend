using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Firearms.CreateFirearm;

public sealed class CreateFirearmCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CreateFirearmCommand, ErrorOr<FirearmResponse>>
{
    public async Task<ErrorOr<FirearmResponse>> Handle(CreateFirearmCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var customerExists = await db.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            return Error.NotFound("CreateFirearmCommand.CustomerNotFound", "Customer not found.");
        }

        var firearm = new Firearm
        {
            CustomerId = request.CustomerId,
            Make = request.Make,
            Model = request.Model,
            Calibre = request.Calibre,
            FirearmType = request.FirearmType,
            SerialNumber = request.SerialNumber,
            InternalReference = request.InternalReference,
            Notes = request.Notes,
        };

        await db.Firearms.AddAsync(firearm, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return FirearmResponse.FromEntity(firearm);
    }
}
