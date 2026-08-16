using FluentValidation;

namespace FirearmStudio.Application.Auth.Login;

public sealed class LoginVerifyRequestValidator : AbstractValidator<LoginVerifyRequest>
{
    public LoginVerifyRequestValidator()
    {
        RuleFor(x => x.PreAuthToken).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
