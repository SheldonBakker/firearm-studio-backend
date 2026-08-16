using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicence;

public sealed class GetLicenceQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicenceQuery, ErrorOr<LicenceDetailDto>>
{
    public async Task<ErrorOr<LicenceDetailDto>> Handle(GetLicenceQuery query, CancellationToken cancellationToken)
    {
        return await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.Id == query.Id)
            .FirstOrNotFoundAsync(LicenceDetailDto.QueryProjection, ErrorCodes.NotFound, "Licence not found.", cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetLicenceQuery.NotFound";
    }
}
