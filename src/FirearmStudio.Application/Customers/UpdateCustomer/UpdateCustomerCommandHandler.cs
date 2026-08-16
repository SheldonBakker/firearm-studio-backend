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
        request.FullName.ApplyTo(v => customer.FullName = v);
        request.CompanyName.ApplyTo(v => customer.CompanyName = v);
        request.Email.ApplyTo(v => customer.Email = v);
        request.Phone.ApplyTo(v => customer.Phone = v);
        request.Notes.ApplyTo(v => customer.Notes = v);
        request.IsActive.ApplyTo(v => customer.IsActive = v);
        request.IdNumber.ApplyTo(v => customer.IdNumber = string.IsNullOrWhiteSpace(v) ? null : v);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCustomerCommand.NotFound";
    }
}
