using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using MockQueryable.Moq;

namespace KoperasiTentera.Application.Tests.TestHelpers;

/// <summary>
/// Builds a mocked <see cref="IAppDbContext"/> backed by in-memory lists, using
/// MockQueryable.Moq so the async LINQ operators the services rely on
/// (FirstOrDefaultAsync, AnyAsync, ToListAsync, ...) work against Moq-created DbSets
/// without touching a real database.
/// </summary>
public static class MockAppDbContextFactory
{
    public static Mock<IAppDbContext> Create(
        List<Customer>? customers = null,
        List<OtpVerification>? otpVerifications = null,
        List<PrivacyPolicy>? privacyPolicies = null,
        List<Banner>? banners = null,
        List<Customer>? addedCustomers = null,
        List<OtpVerification>? addedOtpVerifications = null)
    {
        customers ??= new List<Customer>();
        otpVerifications ??= new List<OtpVerification>();
        privacyPolicies ??= new List<PrivacyPolicy>();
        banners ??= new List<Banner>();

        Mock<DbSet<Customer>> customerSet = customers.BuildMockDbSet();
        if (addedCustomers is not null)
        {
            customerSet.Setup(s => s.Add(It.IsAny<Customer>())).Callback<Customer>(addedCustomers.Add);
        }

        Mock<DbSet<OtpVerification>> otpSet = otpVerifications.BuildMockDbSet();
        if (addedOtpVerifications is not null)
        {
            otpSet.Setup(s => s.Add(It.IsAny<OtpVerification>())).Callback<OtpVerification>(addedOtpVerifications.Add);
        }

        Mock<DbSet<PrivacyPolicy>> policySet = privacyPolicies.BuildMockDbSet();
        Mock<DbSet<Banner>> bannerSet = banners.BuildMockDbSet();

        Mock<IAppDbContext> dbContext = new();
        dbContext.Setup(d => d.Customers).Returns(customerSet.Object);
        dbContext.Setup(d => d.OtpVerifications).Returns(otpSet.Object);
        dbContext.Setup(d => d.PrivacyPolicies).Returns(policySet.Object);
        dbContext.Setup(d => d.Banners).Returns(bannerSet.Object);
        dbContext.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return dbContext;
    }
}
