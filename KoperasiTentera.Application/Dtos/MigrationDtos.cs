namespace KoperasiTentera.Application.Dtos;

public class MigrationStartRequest
{
    public string IcNumber { get; set; } = string.Empty;
}

public class MigrationStartResponse
{
    public Guid CustomerId { get; set; }

    public string MaskedMobileNumber { get; set; } = string.Empty;

    public string MaskedEmail { get; set; } = string.Empty;

    public DateTime MobileOtpExpiresAtUtc { get; set; }

    public string? MobileOtpCode { get; set; }
}

public class ChangeEmailRequest
{
    public Guid CustomerId { get; set; }

    public string NewEmail { get; set; } = string.Empty;
}

public class ChangeEmailResponse
{
    public string MaskedEmail { get; set; } = string.Empty;

    public DateTime EmailOtpExpiresAtUtc { get; set; }

    public string? EmailOtpCode { get; set; }
}
