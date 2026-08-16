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
        request.Name.ApplyTo(v => package.Name = v);
        request.Description.ApplyTo(v => package.Description = v);
        request.Price.ApplyTo(v => package.Price = v);
        request.DurationMinutes.ApplyTo(v => package.DurationMinutes = v);
        request.MaxShooters.ApplyTo(v => package.MaxShooters = v);
        request.IsActive.ApplyTo(v => package.IsActive = v);
        request.Items.ApplyTo(items =>
        {
            db.PackageItems.RemoveRange(package.Items);
            package.Items = items.Select(item => new PackageItem
            {
                PackageId = package.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                SortOrder = item.SortOrder,
            }).ToList();
        });

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdatePackageCommand.NotFound";
    }
}
