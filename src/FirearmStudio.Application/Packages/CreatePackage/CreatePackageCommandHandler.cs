using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Packages.CreatePackage;

public sealed class CreatePackageCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CreatePackageCommand, ErrorOr<PackageResponse>>
{
    public async Task<ErrorOr<PackageResponse>> Handle(CreatePackageCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // The item graph needs the parent ID before saving.
        var packageId = Guid.CreateVersion7();
        var package = new Package
        {
            Id = packageId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            MaxShooters = request.MaxShooters,
            Items = request.Items
                .Select(item => new PackageItem
                {
                    PackageId = packageId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    SortOrder = item.SortOrder,
                })
                .ToList(),
        };

        await db.Packages.AddAsync(package, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return PackageResponse.FromEntity(package);
    }
}
