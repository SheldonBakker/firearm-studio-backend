using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Customers.GetCustomerFirearms;

public sealed class GetCustomerFirearmsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetCustomerFirearmsQuery, ErrorOr<IReadOnlyList<CustomerFirearmListItemDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<CustomerFirearmListItemDto>>> Handle(
        GetCustomerFirearmsQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerFirearmListItemDto> firearms = await db.Firearms
            .AsNoTracking()
            .Where(f => f.CustomerId == query.CustomerId)
            .Select(CustomerFirearmListItemDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(firearms);
    }
}
