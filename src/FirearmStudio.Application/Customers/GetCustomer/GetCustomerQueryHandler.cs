using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using FirearmStudio.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomer;

public sealed class GetCustomerQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerQuery, ErrorOr<CustomerDetailResponse>>
{
    public async Task<ErrorOr<CustomerDetailResponse>> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        var result = await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .FirstOrNotFoundAsync(CustomerDetailResponse.QueryProjection, ErrorCodes.NotFound, "Customer not found.", cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var customer = result.Value;
        return customer.IdNumber is null
            ? customer
            : customer with { IdNumberMasked = IdNumberMask.Mask(customer.IdNumber) };
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetCustomerQuery.NotFound";
    }
}
