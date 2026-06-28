using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomer;

public sealed class GetCustomerQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerQuery, ErrorOr<CustomerResponse>>
{
    public async Task<ErrorOr<CustomerResponse>> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(CustomerResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return customer is null
            ? Error.NotFound(ErrorCodes.NotFound, "Customer not found.")
            : customer;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetCustomerQuery.NotFound";
    }
}
