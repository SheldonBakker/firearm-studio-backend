using FluentValidation;

namespace FirearmStudio.Application.Users;

public sealed class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.FullName).MaximumLength(200);
        RuleFor(x => x.Role).IsInEnum().WithMessage("Unknown role.");
    }
}

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().WithMessage("Unknown role.");
    }
}
