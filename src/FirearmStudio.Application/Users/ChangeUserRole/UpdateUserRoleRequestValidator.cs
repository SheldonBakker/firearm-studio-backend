using FluentValidation;

namespace FirearmStudio.Application.Users.ChangeUserRole;

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(request => request.Role).IsInEnum().WithMessage("Unknown role.");
    }
}
