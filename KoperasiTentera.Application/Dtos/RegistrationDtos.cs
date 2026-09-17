namespace KoperasiTentera.Application.Dtos;

public class RegistrationStartRequest
{
    public string FullName { get; set; } = string.Empty;

    public string IcNumber { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public class RegistrationStartResponse
{
    public Guid CustomerId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime MobileOtpExpiresAtUtc { get; set; }

    public string? MobileOtpCode { get; set; }
}

public class AcceptPolicyRequest
{
    public Guid CustomerId { get; set; }
}

public class CreatePinRequest
{
    public Guid CustomerId { get; set; }

    public string Pin { get; set; } = string.Empty;

    public string ConfirmPin { get; set; } = string.Empty;
}

public class BiometricRequest
{
    public Guid CustomerId { get; set; }

    public bool Enable { get; set; }
}

public class RegistrationStepResponse
{
    public Guid CustomerId { get; set; }

    public string Status { get; set; } = string.Empty;
}
