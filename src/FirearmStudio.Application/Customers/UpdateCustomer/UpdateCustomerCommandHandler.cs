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
        customer.FullName = request.FullName ?? customer.FullName;
        customer.CompanyName = request.CompanyName ?? customer.CompanyName;
        customer.Email = request.Email ?? customer.Email;
        customer.Phone = request.Phone ?? customer.Phone;
        customer.Notes = request.Notes ?? customer.Notes;
        if (request.IsActive is { } active)
        {
            customer.IsActive = active;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCustomerCommand.NotFound";
    }
}
