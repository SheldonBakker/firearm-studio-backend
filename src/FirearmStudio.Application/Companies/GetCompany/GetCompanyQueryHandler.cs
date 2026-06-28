using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies.GetCompany;

public sealed class GetCompanyQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCompanyQuery, ErrorOr<CompanyDetailsResponse>>
{
    public async Task<ErrorOr<CompanyDetailsResponse>> Handle(GetCompanyQuery query, CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(CompanyDetailsResponse.QueryProjection)
            .FirstOrDefaultAsync(cancellationToken);

        return company is null
            ? Error.NotFound(ErrorCodes.NotFound, "Company not found.")
            : company;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "GetCompanyQuery.NotFound";
    }
}
