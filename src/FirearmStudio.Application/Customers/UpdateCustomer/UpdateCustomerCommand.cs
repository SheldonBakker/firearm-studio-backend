using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Customers.UpdateCustomer;

public sealed record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest Request) : ICommand<ErrorOr<Updated>>;
