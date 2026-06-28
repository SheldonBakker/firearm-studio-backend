using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Customers.CreateCustomer;

public sealed record CreateCustomerCommand(CreateCustomerRequest Request) : ICommand<ErrorOr<CustomerResponse>>;
