using FluentValidation;

namespace FirearmStudio.Application.Users.VerifyPhone;

public sealed class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
