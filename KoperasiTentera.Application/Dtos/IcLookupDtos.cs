namespace KoperasiTentera.Application.Dtos;

public class IcLookupRequest
{
    public string IcNumber { get; set; } = string.Empty;
}

public enum IcLookupStatus
{
    NotFound,
    ExistingUserNotMigrated,
    ActiveUser
}

public class IcLookupResponse
{
    public IcLookupStatus Status { get; set; }

    public Guid? CustomerId { get; set; }

    public string? MaskedMobileNumber { get; set; }

    public string? MaskedEmail { get; set; }
}
