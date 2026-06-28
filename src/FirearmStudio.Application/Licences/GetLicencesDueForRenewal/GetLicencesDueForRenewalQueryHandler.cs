using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicencesDueForRenewal;

public sealed class GetLicencesDueForRenewalQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicencesDueForRenewalQuery, ErrorOr<IReadOnlyList<LicenceDueForRenewalDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<LicenceDueForRenewalDto>>> Handle(
        GetLicencesDueForRenewalQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var horizon = today.AddDays(30);

        IReadOnlyList<LicenceDueForRenewalDto> licences = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.RenewalDueOn >= today && l.RenewalDueOn <= horizon)
            .OrderBy(l => l.RenewalDueOn)
            .Select(LicenceDueForRenewalDto.QueryProjection)
            .ToListAsync(cancellationToken);

        return ErrorOrFactory.From(licences);
    }
}
