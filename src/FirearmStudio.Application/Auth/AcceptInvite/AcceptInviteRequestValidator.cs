using FirearmStudio.Application.Common;
using FluentValidation;

namespace FirearmStudio.Application.Auth.AcceptInvite;

public sealed class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);

        When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber)
                .Matches(PhoneNumberFormat.E164Pattern)
                .WithMessage("Phone number must be in E.164 format, e.g. +27821234567."));
    }
}
