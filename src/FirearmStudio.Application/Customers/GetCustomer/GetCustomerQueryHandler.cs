using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomer;

public sealed class GetCustomerQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerQuery, ErrorOr<CustomerDetailResponse>>
{
    public async Task<ErrorOr<CustomerDetailResponse>> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(CustomerDetailResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Customer not found.");
        }

        return customer.IdNumber is null
            ? customer
            : customer with { IdNumberMasked = IdNumberMask.Mask(customer.IdNumber) };
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetCustomerQuery.NotFound";
    }
}
