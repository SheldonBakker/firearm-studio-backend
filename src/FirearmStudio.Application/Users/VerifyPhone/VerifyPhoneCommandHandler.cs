using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Application.Auth;
using FirearmStudio.Domain.Common;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Users.VerifyPhone;

public sealed class VerifyPhoneCommandHandler(
    ICurrentUserService currentUser,
    IUserAccountService accounts,
    IOtpService otp,
    IApplicationDbContext db,
    ITenantContext tenant)
    : ICommandHandler<VerifyPhoneCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(VerifyPhoneCommand command, CancellationToken ct)
    {
        var userId = currentUser.User.Id;

        var result = await otp.VerifyAsync(userId, OtpPurpose.PhoneChange, command.Request.Code, ct);
        var failure = AuthResults.ToError(result);
        if (failure is not null)
        {
            if (NoLiveCodeRemains(result))
            {
                await accounts.ClearPendingPhoneNumberAsync(userId, ct);
            }

            return failure.Value;
        }

        var promoted = await accounts.ConfirmPhoneChangeAsync(userId, ct);
        if (promoted is null)
        {
            return Error.Validation(
                AuthErrorCodes.NoPendingPhoneChange,
                "There is no pending phone change to confirm.");
        }

        using (tenant.BeginBypass())
        {
            var appUsers = await db.AppUsers
                .IgnoreQueryFilters()
                .Where(u => u.AuthUserId == userId)
                .ToListAsync(ct);

            foreach (var appUser in appUsers)
            {
                appUser.PhoneNumber = promoted;
            }

            await db.SaveChangesAsync(ct);
        }

        return Result.Success;
    }

    private static bool NoLiveCodeRemains(OtpVerifyResult result) =>
        result is OtpVerifyResult.Expired or OtpVerifyResult.NotFound or OtpVerifyResult.TooManyAttempts;
}
