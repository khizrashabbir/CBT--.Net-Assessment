using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();

    public DbSet<PrivacyPolicy> PrivacyPolicies => Set<PrivacyPolicy>();

    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Seed(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
