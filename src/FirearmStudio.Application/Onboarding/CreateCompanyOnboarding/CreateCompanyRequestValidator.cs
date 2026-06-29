using FluentValidation;

namespace FirearmStudio.Application.Onboarding.CreateCompanyOnboarding;

public sealed class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.RegistrationNumber).MaximumLength(50);
        RuleFor(request => request.VatNumber).MaximumLength(50);
        RuleFor(request => request.Email)
            .EmailAddress()
            .MaximumLength(320)
            .When(request => !string.IsNullOrWhiteSpace(request.Email));
        RuleFor(request => request.Phone).MaximumLength(50);
        RuleFor(request => request.AddressLine1).MaximumLength(200);
        RuleFor(request => request.AddressLine2).MaximumLength(200);
        RuleFor(request => request.City).MaximumLength(120);
        RuleFor(request => request.Province).MaximumLength(120);
        RuleFor(request => request.PostalCode).MaximumLength(20);
    }
}
