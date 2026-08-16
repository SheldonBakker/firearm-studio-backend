using FluentValidation;

namespace FirearmStudio.Application.Auth.DisableTwoFactor;

public sealed class DisableTwoFactorRequestValidator : AbstractValidator<DisableTwoFactorRequest>
{
    public DisableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
