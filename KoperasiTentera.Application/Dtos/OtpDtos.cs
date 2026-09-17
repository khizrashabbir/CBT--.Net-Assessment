namespace KoperasiTentera.Application.Dtos;

public enum OtpTypeDto
{
    Mobile,
    Email
}

public class OtpSendRequest
{
    public Guid CustomerId { get; set; }

    public OtpTypeDto OtpType { get; set; }
}

public class OtpSendResponse
{
    public DateTime ExpiresAtUtc { get; set; }

    public string? OtpCode { get; set; }
}

public class OtpVerifyRequest
{
    public Guid CustomerId { get; set; }

    public OtpTypeDto OtpType { get; set; }

    public string Code { get; set; } = string.Empty;
}

public class OtpVerifyResponse
{
    public Guid CustomerId { get; set; }

    public string Status { get; set; } = string.Empty;
}
