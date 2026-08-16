using FluentValidation;

namespace FirearmStudio.Application.Accounting.RegisterAccountingConnection;

public sealed class RegisterAccountingConnectionRequestValidator : AbstractValidator<RegisterAccountingConnectionRequest>
{
    public RegisterAccountingConnectionRequestValidator()
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

        RuleFor(request => request.ExternalCompanyId)
            .GreaterThan(0);
    }
}
