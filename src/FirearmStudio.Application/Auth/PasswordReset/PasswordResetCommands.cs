using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;

namespace FirearmStudio.Application.Auth.PasswordReset;

public sealed record ForgotPasswordCommand(ForgotPasswordRequest Request)
    : ICommand<ErrorOr<Success>>;

public sealed class ForgotPasswordCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    IEmailSender email)
    : ICommandHandler<ForgotPasswordCommand, ErrorOr<Success>>
{
    private const int CodeLifetimeMinutes = 15;

    public async Task<ErrorOr<Success>> Handle(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);

        if (account is not null)
        {
            var issued = await otp.IssueAsync(
                account.Id, OtpPurpose.PasswordReset, cancellationToken);

            if (issued.Status == OtpIssueStatus.Issued)
            {
                await email.SendOtpAsync(
                    address, null, OtpPurpose.PasswordReset, issued.Code!,
                    CodeLifetimeMinutes, cancellationToken);
            }
        }

        return Result.Success;
    }
}

public sealed record ResetPasswordCommand(ResetPasswordRequest Request)
    : ICommand<ErrorOr<Success>>;

public sealed class ResetPasswordCommandHandler(
    IUserAccountService accounts,
    IOtpService otp,
    ITokenService tokens)
    : ICommandHandler<ResetPasswordCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var address = command.Request.Email.Trim().ToLowerInvariant();

        var account = await accounts.FindByEmailAsync(address, cancellationToken);
        if (account is null)
        {
            return Error.Validation(AuthErrorCodes.CodeInvalid, "The code is not valid.");
        }

        var result = await otp.VerifyAsync(
            account.Id, OtpPurpose.PasswordReset, command.Request.Code, cancellationToken);

        var failure = AuthResults.ToError(result);
        if (failure is not null)
        {
            return failure.Value;
        }

        var errors = await accounts.SetPasswordAsync(
            account.Id, command.Request.NewPassword, cancellationToken);

        if (errors.Count > 0)
        {
            return Error.Validation(
                AuthErrorCodes.PasswordRejected,
                string.Join(" ", errors));
        }

        await accounts.ConfirmEmailAsync(account.Id, cancellationToken);
        await tokens.RevokeAllAsync(account.Id, cancellationToken);

        return Result.Success;
    }
}
