namespace KoperasiTentera.Domain.Entities;

public class PrivacyPolicy
{
    public Guid Id { get; set; }

    public string Version { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
