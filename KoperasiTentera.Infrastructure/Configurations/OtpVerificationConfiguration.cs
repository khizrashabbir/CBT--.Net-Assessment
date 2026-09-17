using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoperasiTentera.Infrastructure.Configurations;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("OtpVerifications");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Code).HasMaxLength(4).IsRequired();

        builder.Property(o => o.OtpType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(o => new { o.CustomerId, o.OtpType, o.CreatedAtUtc });
    }
}
