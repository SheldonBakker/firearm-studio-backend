using FirearmStudio.Application.Common;
using FluentValidation;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.FullName).MaximumLength(200);
        RuleFor(request => request.Role).IsInEnum().WithMessage("Unknown role.");

        When(request => !string.IsNullOrEmpty(request.PhoneNumber), () =>
            RuleFor(request => request.PhoneNumber)
                .Matches(PhoneNumberFormat.E164Pattern)
                .WithMessage("Phone number must be in E.164 format, e.g. +27821234567."));
    }
}
