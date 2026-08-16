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
