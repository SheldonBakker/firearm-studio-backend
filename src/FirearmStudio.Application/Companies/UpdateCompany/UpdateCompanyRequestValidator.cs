using FluentValidation;

namespace FirearmStudio.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(request => request)
            .Must(HasAtLeastOneChange)
            .WithMessage("At least one field must be supplied.");

        RuleFor(request => request.Name.Value)
            .NotEmpty()
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.Name))
            .When(request => request.Name.IsSet);
        RuleFor(request => request.RegistrationNumber.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCompanyRequest.RegistrationNumber))
            .When(request => request.RegistrationNumber.IsSet);
        RuleFor(request => request.VatNumber.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCompanyRequest.VatNumber))
            .When(request => request.VatNumber.IsSet);
        RuleFor(request => request.Email.Value)
            .EmailAddress()
            .MaximumLength(320)
            .OverridePropertyName(nameof(UpdateCompanyRequest.Email))
            .When(request => request.Email.IsSet && !string.IsNullOrWhiteSpace(request.Email.Value));
        RuleFor(request => request.Phone.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCompanyRequest.Phone))
            .When(request => request.Phone.IsSet);
        RuleFor(request => request.AddressLine1.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.AddressLine1))
            .When(request => request.AddressLine1.IsSet);
        RuleFor(request => request.AddressLine2.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.AddressLine2))
            .When(request => request.AddressLine2.IsSet);
        RuleFor(request => request.City.Value)
            .MaximumLength(120)
            .OverridePropertyName(nameof(UpdateCompanyRequest.City))
            .When(request => request.City.IsSet);
        RuleFor(request => request.Province.Value)
            .MaximumLength(120)
            .OverridePropertyName(nameof(UpdateCompanyRequest.Province))
            .When(request => request.Province.IsSet);
        RuleFor(request => request.PostalCode.Value)
            .MaximumLength(20)
            .OverridePropertyName(nameof(UpdateCompanyRequest.PostalCode))
            .When(request => request.PostalCode.IsSet);
        RuleFor(request => request.BankName.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankName))
            .When(request => request.BankName.IsSet);
        RuleFor(request => request.BankAccountHolder.Value)
            .MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankAccountHolder))
            .When(request => request.BankAccountHolder.IsSet);
        RuleFor(request => request.BankAccountNumber.Value)
            .MaximumLength(34)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankAccountNumber))
            .When(request => request.BankAccountNumber.IsSet);
        RuleFor(request => request.BankBranchCode.Value)
            .MaximumLength(20)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankBranchCode))
            .When(request => request.BankBranchCode.IsSet);
        RuleFor(request => request.BankAccountType.Value)
            .MaximumLength(20)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankAccountType))
            .When(request => request.BankAccountType.IsSet);
        RuleFor(request => request.BankSwiftCode.Value)
            .MaximumLength(11)
            .OverridePropertyName(nameof(UpdateCompanyRequest.BankSwiftCode))
            .When(request => request.BankSwiftCode.IsSet);
    }

    private static bool HasAtLeastOneChange(UpdateCompanyRequest request) =>
        request.Name.IsSet
        || request.RegistrationNumber.IsSet
        || request.VatNumber.IsSet
        || request.Email.IsSet
        || request.Phone.IsSet
        || request.AddressLine1.IsSet
        || request.AddressLine2.IsSet
        || request.City.IsSet
        || request.Province.IsSet
        || request.PostalCode.IsSet
        || request.BankName.IsSet
        || request.BankAccountHolder.IsSet
        || request.BankAccountNumber.IsSet
        || request.BankBranchCode.IsSet
        || request.BankAccountType.IsSet
        || request.BankSwiftCode.IsSet;
}
