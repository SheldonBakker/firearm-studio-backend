using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Licences.GetLicence;

public sealed class GetLicenceQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetLicenceQuery, ErrorOr<LicenceDetailDto>>
{
    public async Task<ErrorOr<LicenceDetailDto>> Handle(GetLicenceQuery query, CancellationToken cancellationToken)
    {
        var licence = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.Id == query.Id)
            .Select(LicenceDetailDto.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return licence is null
            ? Error.NotFound(ErrorCodes.NotFound, "Licence not found.")
            : licence;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetLicenceQuery.NotFound";
    }
}
