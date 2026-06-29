using FluentValidation;

namespace FirearmStudio.Application.Users.InviteUser;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.FullName).MaximumLength(200);
        RuleFor(request => request.Role).IsInEnum().WithMessage("Unknown role.");
    }
}
