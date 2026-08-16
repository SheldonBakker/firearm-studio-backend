using FluentValidation;

namespace FirearmStudio.Application.Auth.ResendCode;

public sealed class ResendCodeRequestValidator : AbstractValidator<ResendCodeRequest>
{
    public ResendCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Purpose).NotEmpty();
    }
}
