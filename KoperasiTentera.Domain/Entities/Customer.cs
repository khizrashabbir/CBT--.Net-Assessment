namespace KoperasiTentera.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string IcNumber { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Enums.CustomerType CustomerType { get; set; }

    public Enums.CustomerStatus Status { get; set; }

    public bool IsBiometricEnabled { get; set; }

    public string? PinHash { get; set; }

    /// <summary>Consecutive failed PIN login attempts since the last success or lockout. Resets to 0 on success.</summary>
    public int FailedPinAttempts { get; set; }

    /// <summary>When set (and in the future), PIN login is locked out regardless of whether the PIN is correct.</summary>
    public DateTime? PinLockedUntilUtc { get; set; }

    public DateTime? PolicyAcceptedAtUtc { get; set; }

    public string? AcceptedPolicyVersion { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();
}
