using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Customers.CreateCustomer;

public sealed class CreateCustomerCommandHandler(IApplicationDbContext db)
    : ICommandHandler<CreateCustomerCommand, ErrorOr<CustomerResponse>>
{
    public async Task<ErrorOr<CustomerResponse>> Handle(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var customer = new Customer
        {
            CustomerType = request.CustomerType,
            FullName = request.FullName,
            CompanyName = request.CompanyName,
            RegistrationNumber = request.RegistrationNumber,
            VatNumber = request.VatNumber,
            Email = request.Email,
            Phone = request.Phone,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            Notes = request.Notes,
            IdNumber = string.IsNullOrWhiteSpace(request.IdNumber) ? null : request.IdNumber,
        };

        await db.Customers.AddAsync(customer, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return CustomerResponse.FromEntity(customer);
    }
}
