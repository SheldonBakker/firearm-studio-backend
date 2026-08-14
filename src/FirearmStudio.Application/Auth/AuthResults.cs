using ErrorOr;
using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Application.Auth;

internal static class AuthResults
{
    public static Error? ToError(OtpVerifyResult result) => result switch
    {
        OtpVerifyResult.Valid => null,

        OtpVerifyResult.Expired => Error.Validation(
            AuthErrorCodes.CodeExpired,
            "That code has expired. Request a new one."),

        OtpVerifyResult.TooManyAttempts => Error.Validation(
            AuthErrorCodes.CodeAttemptsExceeded,
            "Too many incorrect attempts. Request a new code."),

        _ => Error.Validation(
            AuthErrorCodes.CodeInvalid,
            "The code is not valid."),
    };
}
