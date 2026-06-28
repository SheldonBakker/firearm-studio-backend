using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Customers.GetCustomer;

public sealed record GetCustomerQuery(Guid Id) : IQuery<ErrorOr<CustomerResponse>>;
