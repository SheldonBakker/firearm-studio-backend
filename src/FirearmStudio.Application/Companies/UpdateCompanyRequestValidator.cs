using FluentValidation;

namespace FirearmStudio.Application.Companies;

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);
        RuleFor(x => x.VatNumber).MaximumLength(50);
    }
}
