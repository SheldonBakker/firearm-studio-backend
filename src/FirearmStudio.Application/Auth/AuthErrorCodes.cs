namespace FirearmStudio.Application.Auth;

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "Auth.InvalidCredentials";
    public const string EmailNotConfirmed = "Auth.EmailNotConfirmed";
    public const string LockedOut = "Auth.LockedOut";
    public const string RegistrationFailed = "Auth.RegistrationFailed";
    public const string CodeInvalid = "Auth.CodeInvalid";
    public const string CodeExpired = "Auth.CodeExpired";
    public const string CodeAttemptsExceeded = "Auth.CodeAttemptsExceeded";
    public const string RefreshInvalid = "Auth.RefreshInvalid";
    public const string PasswordRejected = "Auth.PasswordRejected";
    public const string UnknownPurpose = "Auth.UnknownPurpose";
    public const string PurposeNotResendable = "Auth.PurposeNotResendable";
    public const string ChallengeUnavailable = "Auth.ChallengeUnavailable";
    public const string PreAuthInvalid = "Auth.PreAuthInvalid";
    public const string PhoneMissing = "Auth.PhoneMissing";
    public const string NoPendingPhoneChange = "Auth.NoPendingPhoneChange";
    public const string PhoneChannelUnavailable = "Auth.PhoneChannelUnavailable";
}
