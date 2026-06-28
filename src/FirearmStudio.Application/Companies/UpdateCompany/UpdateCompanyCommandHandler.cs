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
        if (request.Name.IsSet)
        {
            company.Name = request.Name.Value;
        }

        if (request.RegistrationNumber.IsSet)
        {
            company.RegistrationNumber = request.RegistrationNumber.Value;
        }

        if (request.VatNumber.IsSet)
        {
            company.VatNumber = request.VatNumber.Value;
        }

        if (request.Email.IsSet)
        {
            company.Email = request.Email.Value;
        }

        if (request.Phone.IsSet)
        {
            company.Phone = request.Phone.Value;
        }

        if (request.AddressLine1.IsSet)
        {
            company.AddressLine1 = request.AddressLine1.Value;
        }

        if (request.AddressLine2.IsSet)
        {
            company.AddressLine2 = request.AddressLine2.Value;
        }

        if (request.City.IsSet)
        {
            company.City = request.City.Value;
        }

        if (request.Province.IsSet)
        {
            company.Province = request.Province.Value;
        }

        if (request.PostalCode.IsSet)
        {
            company.PostalCode = request.PostalCode.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCompanyCommand.NotFound";
    }
}
