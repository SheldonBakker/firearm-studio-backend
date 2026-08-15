using FirearmStudio.Application.Common;
using FluentValidation;

namespace FirearmStudio.Application.Users.UpdatePhone;

public sealed class UpdatePhoneRequestValidator : AbstractValidator<UpdatePhoneRequest>
{
    public UpdatePhoneRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(PhoneNumberFormat.E164Pattern)
            .WithMessage("Phone number must be in E.164 format, e.g. +27821234567.");
    }
}

public sealed class VerifyPhoneRequestValidator : AbstractValidator<VerifyPhoneRequest>
{
    public VerifyPhoneRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
