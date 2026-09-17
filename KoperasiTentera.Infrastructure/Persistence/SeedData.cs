using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Infrastructure.Persistence;

/// <summary>
/// Deterministic seed data applied via EF migrations: one active privacy policy,
/// two home-screen banners, one legacy "ExistingUser" (Mariam, PendingVerification) so
/// the migration flow is testable immediately, and one already-Active demo customer
/// (Ali) so PIN login / home dashboard can be exercised without walking the full flow.
/// </summary>
public static class SeedData
{
    private static readonly DateTime SeedTimestampUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid LegacyCustomerId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ActiveDemoCustomerId = new("55555555-5555-5555-5555-555555555555");
    public static readonly Guid PrivacyPolicyId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid BannerOneId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid BannerTwoId = new("44444444-4444-4444-4444-444444444444");

    /// <summary>
    /// BCrypt hash of PIN "111111" (pre-computed so the migration seed stays deterministic;
    /// BCrypt.HashPassword generates a new random salt on every call, which EF `HasData`
    /// snapshots would otherwise flag as a pending model change on every build).
    /// </summary>
    public const string ActiveDemoCustomerPin = "111111";
    private const string ActiveDemoCustomerPinHash = "$2a$11$YE0/Pi43pcuyWFulDWl5pOhTOlnhUgvC.qq.HxpO0FQsodl6hnhkq";

    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivacyPolicy>().HasData(new PrivacyPolicy
        {
            Id = PrivacyPolicyId,
            Version = "1.0",
            Content = "By continuing, you agree to Koperasi Tentera's Terms & Conditions and Privacy Policy, " +
                      "which describe how we collect, use, and protect your personal data.",
            IsActive = true,
            CreatedAtUtc = SeedTimestampUtc
        });

        modelBuilder.Entity<Banner>().HasData(
            new Banner
            {
                Id = BannerOneId,
                Title = "Oh My Cashback!",
                Description = "Earn cashback on every eligible transaction this month.",
                ImageUrl = "https://example.com/banners/cashback.png",
                IsActive = true,
                SortOrder = 1
            },
            new Banner
            {
                Id = BannerTwoId,
                Title = "New Shariah Savings",
                Description = "Discover our newest Shariah-compliant savings plan.",
                ImageUrl = "https://example.com/banners/savings.png",
                IsActive = true,
                SortOrder = 2
            });

        modelBuilder.Entity<Customer>().HasData(
            new Customer
            {
                Id = LegacyCustomerId,
                FullName = "Mariam Abdul Rashid",
                IcNumber = "880214566831",
                MobileNumber = "+60123456675",
                Email = "mariam.rashid@example.com",
                CustomerType = CustomerType.ExistingUser,
                Status = CustomerStatus.PendingVerification,
                IsBiometricEnabled = false,
                PinHash = null,
                PolicyAcceptedAtUtc = null,
                AcceptedPolicyVersion = null,
                CreatedAtUtc = SeedTimestampUtc,
                UpdatedAtUtc = SeedTimestampUtc
            },
            new Customer
            {
                Id = ActiveDemoCustomerId,
                FullName = "Ali Zulkifli",
                IcNumber = "900101011111",
                MobileNumber = "+60123450099",
                Email = "ali.zulkifli@example.com",
                CustomerType = CustomerType.NewCustomer,
                Status = CustomerStatus.Active,
                IsBiometricEnabled = true,
                PinHash = ActiveDemoCustomerPinHash,
                PolicyAcceptedAtUtc = SeedTimestampUtc,
                AcceptedPolicyVersion = "1.0",
                CreatedAtUtc = SeedTimestampUtc,
                UpdatedAtUtc = SeedTimestampUtc
            });
    }
}
