namespace KoperasiTentera.Domain.Entities;

public class OtpVerification
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Enums.OtpType OtpType { get; set; }

    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsUsed { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
