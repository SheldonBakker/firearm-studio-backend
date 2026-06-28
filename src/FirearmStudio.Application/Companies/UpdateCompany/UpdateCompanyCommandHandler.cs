using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateCompanyCommand, ErrorOr<Updated>>
{
    public async Task<ErrorOr<Updated>> Handle(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        if (currentUserService.User.CompanyId is not { } companyId)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return Error.NotFound(ErrorCodes.NotFound, "Company not found.");
        }

        var request = command.Request;
        company.Name = request.Name ?? company.Name;
        company.RegistrationNumber = request.RegistrationNumber ?? company.RegistrationNumber;
        company.VatNumber = request.VatNumber ?? company.VatNumber;
        company.Email = request.Email ?? company.Email;
        company.Phone = request.Phone ?? company.Phone;
        company.AddressLine1 = request.AddressLine1 ?? company.AddressLine1;
        company.AddressLine2 = request.AddressLine2 ?? company.AddressLine2;
        company.City = request.City ?? company.City;
        company.Province = request.Province ?? company.Province;
        company.PostalCode = request.PostalCode ?? company.PostalCode;

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCompanyCommand.NotFound";
    }
}
