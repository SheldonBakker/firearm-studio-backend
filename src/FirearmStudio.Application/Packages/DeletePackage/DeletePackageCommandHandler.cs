using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Packages.DeletePackage;

public sealed class DeletePackageCommandHandler(IApplicationDbContext db)
    : ICommandHandler<DeletePackageCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeletePackageCommand command, CancellationToken cancellationToken)
    {
        var package = await db.Packages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (package is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Package not found.");
        }

        var hasBookings = await db.Bookings.AnyAsync(b => b.PackageId == command.Id, cancellationToken);
        if (hasBookings)
        {
            return Error.Conflict(ErrorCodes.HasBookings, "The package cannot be deleted while bookings reference it.");
        }

        db.Packages.Remove(package);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "DeletePackageCommand.NotFound";
        public const string HasBookings = "DeletePackageCommand.HasBookings";
    }
}
