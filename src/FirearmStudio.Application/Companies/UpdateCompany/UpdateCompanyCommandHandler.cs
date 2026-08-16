using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
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
        request.Name.ApplyTo(v => company.Name = v);
        request.RegistrationNumber.ApplyTo(v => company.RegistrationNumber = v);
        request.VatNumber.ApplyTo(v => company.VatNumber = v);
        request.Email.ApplyTo(v => company.Email = v);
        request.Phone.ApplyTo(v => company.Phone = v);
        request.AddressLine1.ApplyTo(v => company.AddressLine1 = v);
        request.AddressLine2.ApplyTo(v => company.AddressLine2 = v);
        request.City.ApplyTo(v => company.City = v);
        request.Province.ApplyTo(v => company.Province = v);
        request.PostalCode.ApplyTo(v => company.PostalCode = v);
        request.BankName.ApplyTo(v => company.BankName = v);
        request.BankAccountHolder.ApplyTo(v => company.BankAccountHolder = v);
        request.BankAccountNumber.ApplyTo(v => company.BankAccountNumber = v);
        request.BankBranchCode.ApplyTo(v => company.BankBranchCode = v);
        request.BankAccountType.ApplyTo(v => company.BankAccountType = v);
        request.BankSwiftCode.ApplyTo(v => company.BankSwiftCode = v);
        request.DepositMode.ApplyTo(v => company.DepositMode = v);
        request.DepositValue.ApplyTo(v => company.DepositValue = v);
        request.DepositWindowHours.ApplyTo(v => company.DepositWindowHours = v);

        if (company.DepositMode == DepositMode.Percentage && company.DepositValue > 100)
        {
            return Error.Validation(ErrorCodes.InvalidDepositPercentage, "DepositValue must be 100 or less when DepositMode is Percentage.");
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }

    public static class ErrorCodes
    {
        public const string NotFound = "UpdateCompanyCommand.NotFound";
        public const string InvalidDepositPercentage = "UpdateCompanyCommand.InvalidDepositPercentage";
    }
}
