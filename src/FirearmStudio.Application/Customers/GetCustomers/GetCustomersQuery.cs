using ErrorOr;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Model;

namespace FirearmStudio.Application.Customers.GetCustomers;

public sealed record GetCustomersQuery(
    int PageNumber,
    int PageSize,
    string SortOrder,
    string? Name,
    string? Email,
    string? Phone
) : IQuery<ErrorOr<PaginatedResponse<CustomerListItemDto>>>;
