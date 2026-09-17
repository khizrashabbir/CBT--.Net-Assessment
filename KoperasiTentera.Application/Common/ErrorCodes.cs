namespace KoperasiTentera.Application.Common;

public static class ErrorCodes
{
    public const string AccountAlreadyExists = "ACCOUNT_ALREADY_EXISTS";
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string IncorrectOtp = "INCORRECT_OTP";
    public const string OtpExpired = "OTP_EXPIRED";
    public const string OtpMaxAttempts = "OTP_MAX_ATTEMPTS";
    public const string ResendNotAllowed = "RESEND_NOT_ALLOWED";
    public const string UnmatchedPin = "UNMATCHED_PIN";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string InvalidState = "INVALID_STATE";
    public const string ValidationError = "VALIDATION_ERROR";
}
