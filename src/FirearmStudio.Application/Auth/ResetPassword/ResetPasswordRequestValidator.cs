using FluentValidation;

namespace FirearmStudio.Application.Auth.ResetPassword;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(256);
    }
}
