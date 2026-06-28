using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.UpdateCustomer;

public sealed class UpdateCustomerCommandHandler(IApplicationDbContext db)
    : ICommandHandler<UpdateCustomerCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (customer is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Customer not found.");
        }

        var request = command.Request;
        if (request.FullName.IsSet)
        {
            customer.FullName = request.FullName.Value;
        }

        if (request.CompanyName.IsSet)
        {
            customer.CompanyName = request.CompanyName.Value;
        }

        if (request.Email.IsSet)
        {
            customer.Email = request.Email.Value;
        }

        if (request.Phone.IsSet)
        {
            customer.Phone = request.Phone.Value;
        }

        if (request.Notes.IsSet)
        {
            customer.Notes = request.Notes.Value;
        }

        if (request.IsActive.IsSet)
        {
            customer.IsActive = request.IsActive.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCustomerCommand.NotFound";
    }
}
