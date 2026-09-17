using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KoperasiTentera.Infrastructure.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(150).IsRequired();

        builder.Property(c => c.IcNumber).HasMaxLength(12).IsRequired();
        builder.HasIndex(c => c.IcNumber).IsUnique();

        builder.Property(c => c.MobileNumber).HasMaxLength(20).IsRequired();

        builder.Property(c => c.Email).HasMaxLength(150).IsRequired();

        builder.Property(c => c.CustomerType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.PinHash).HasMaxLength(200);

        builder.Property(c => c.AcceptedPolicyVersion).HasMaxLength(20);

        builder.HasMany(c => c.OtpVerifications)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
