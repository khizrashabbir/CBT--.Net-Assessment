namespace KoperasiTentera.Application.Dtos;

public class PrivacyPolicyResponse
{
    public string Version { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public class PinLoginRequest
{
    public string IcNumber { get; set; } = string.Empty;

    public string Pin { get; set; } = string.Empty;
}

public class PinLoginResponse
{
    public Guid CustomerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class BannerDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
}

public class HomeResponse
{
    public string Greeting { get; set; } = string.Empty;

    public bool IsBiometricEnabled { get; set; }

    public List<BannerDto> Banners { get; set; } = new();
}
