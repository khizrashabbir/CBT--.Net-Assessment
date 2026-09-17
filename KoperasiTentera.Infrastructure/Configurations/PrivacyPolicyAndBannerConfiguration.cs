using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoperasiTentera.Infrastructure.Configurations;

public class PrivacyPolicyConfiguration : IEntityTypeConfiguration<PrivacyPolicy>
{
    public void Configure(EntityTypeBuilder<PrivacyPolicy> builder)
    {
        builder.ToTable("PrivacyPolicies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Version).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Content).IsRequired();
    }
}

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.ToTable("Banners");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(150).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(500);
        builder.Property(b => b.ImageUrl).HasMaxLength(500);
    }
}
