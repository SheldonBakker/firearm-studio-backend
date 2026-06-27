using FluentValidation;

namespace FirearmStudio.Application.Onboarding;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.VatNumber).MaximumLength(50);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);
    }
}
