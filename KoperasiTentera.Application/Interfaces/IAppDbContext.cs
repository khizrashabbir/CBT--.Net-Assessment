using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer's services
/// stay independent of the Infrastructure implementation (and remain unit-testable
/// with the EF Core in-memory provider).
/// </summary>
public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }

    DbSet<OtpVerification> OtpVerifications { get; }

    DbSet<PrivacyPolicy> PrivacyPolicies { get; }

    DbSet<Banner> Banners { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
