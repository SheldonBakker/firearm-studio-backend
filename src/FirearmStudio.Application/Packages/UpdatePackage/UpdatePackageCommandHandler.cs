using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.UpdatePackage;

public sealed class UpdatePackageCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdatePackageCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdatePackageCommand command, CancellationToken cancellationToken)
    {
        var package = await db.Packages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (package is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Package not found.");
        }

        var request = command.Request;

        if (request.Name.IsSet)
        {
            package.Name = request.Name.Value;
        }

        if (request.Description.IsSet)
        {
            package.Description = request.Description.Value;
        }

        if (request.Price.IsSet)
        {
            package.Price = request.Price.Value;
        }

        if (request.DurationMinutes.IsSet)
        {
            package.DurationMinutes = request.DurationMinutes.Value;
        }

        if (request.MaxShooters.IsSet)
        {
            package.MaxShooters = request.MaxShooters.Value;
        }

        if (request.IsActive.IsSet)
        {
            package.IsActive = request.IsActive.Value;
        }

        if (request.Items.IsSet)
        {
            db.PackageItems.RemoveRange(package.Items);
            package.Items = request.Items.Value
                .Select(item => new PackageItem
                {
                    PackageId = package.Id,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    SortOrder = item.SortOrder,
                })
                .ToList();
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdatePackageCommand.NotFound";
    }
}
