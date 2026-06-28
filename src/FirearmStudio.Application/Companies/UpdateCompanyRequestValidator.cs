using FluentValidation;

namespace FirearmStudio.Application.Companies;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name.Value)
            .NotEmpty().MaximumLength(200)
            .OverridePropertyName(nameof(UpdateCompanyRequest.Name))
            .When(x => x.Name.IsSet);

        RuleFor(x => x.Email.Value)
            .EmailAddress()
            .OverridePropertyName(nameof(UpdateCompanyRequest.Email))
            .When(x => x.Email.IsSet && !string.IsNullOrWhiteSpace(x.Email.Value));

        RuleFor(x => x.RegistrationNumber.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCompanyRequest.RegistrationNumber))
            .When(x => x.RegistrationNumber.IsSet);

        RuleFor(x => x.VatNumber.Value)
            .MaximumLength(50)
            .OverridePropertyName(nameof(UpdateCompanyRequest.VatNumber))
            .When(x => x.VatNumber.IsSet);
    }
}
