using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Customers.GetCustomerFirearms;

public sealed record GetCustomerFirearmsQuery(Guid CustomerId)
    : IQuery<ErrorOr<IReadOnlyList<CustomerFirearmListItemDto>>>;
