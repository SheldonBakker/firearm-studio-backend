using FluentValidation;

namespace FirearmStudio.Application.Sage.RegisterSageConnection;

public sealed class RegisterSageConnectionRequestValidator : AbstractValidator<RegisterSageConnectionRequest>
{
    public RegisterSageConnectionRequestValidator()
    {
        RuleFor(request => request.ApiKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Username)
            .NotEmpty()
            .MaximumLength(320);

        RuleFor(request => request.Password)
            .NotEmpty()
            .MaximumLength(1024);

        RuleFor(request => request.SageCompanyId)
            .GreaterThan(0);
    }
}
