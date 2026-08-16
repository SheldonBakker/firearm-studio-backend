using FirearmStudio.Application.Common;
using FluentValidation;

namespace FirearmStudio.Application.Auth.Register;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);

        When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber)
                .Matches(PhoneNumberFormat.E164Pattern)
                .WithMessage("Phone number must be in E.164 format, e.g. +27821234567."));
    }
}
