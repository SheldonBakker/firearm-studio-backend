using FluentValidation;
using FirearmStudio.Application.Common;

namespace FirearmStudio.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(256);

        When(x => !string.IsNullOrEmpty(x.PhoneNumber), () =>
            RuleFor(x => x.PhoneNumber)
                .Matches(PhoneNumberFormat.E164Pattern)
                .WithMessage("Phone number must be in E.164 format, e.g. +27821234567."));
    }
}

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}

public sealed class ResendCodeRequestValidator : AbstractValidator<ResendCodeRequest>
{
    public ResendCodeRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Purpose).NotEmpty();
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

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

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(256);
    }
}

public sealed class LoginVerifyRequestValidator : AbstractValidator<LoginVerifyRequest>
{
    public LoginVerifyRequestValidator()
    {
        RuleFor(x => x.PreAuthToken).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Code).NotEmpty().Length(6).Matches("^[0-9]{6}$");
    }
}
