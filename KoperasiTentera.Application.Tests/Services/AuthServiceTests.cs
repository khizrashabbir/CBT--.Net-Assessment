using FluentAssertions;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Application.Services;
using KoperasiTentera.Application.Tests.TestHelpers;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Moq;
using Xunit;

namespace KoperasiTentera.Application.Tests.Services;

public class AuthServiceTests
{
    private static Customer CreateCustomer(CustomerStatus status, CustomerType type = CustomerType.NewCustomer, string? pinHash = null) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Mariam Abdul Rashid",
        IcNumber = "880214566831",
        MobileNumber = "+60123456675",
        Email = "mariam.rashid@example.com",
        CustomerType = type,
        Status = status,
        PinHash = pinHash,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task LookupIcAsync_WhenIcNotFound_ReturnsNotFound()
    {
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create();
        AuthService sut = new(dbContext.Object);

        IcLookupResponse response = await sut.LookupIcAsync(new IcLookupRequest { IcNumber = "000000000000" });

        response.Status.Should().Be(IcLookupStatus.NotFound);
        response.CustomerId.Should().BeNull();
    }

    [Fact]
    public async Task LookupIcAsync_WhenCustomerIsActive_ReturnsActiveUserWithoutMaskedContacts()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        IcLookupResponse response = await sut.LookupIcAsync(new IcLookupRequest { IcNumber = customer.IcNumber });

        response.Status.Should().Be(IcLookupStatus.ActiveUser);
        response.CustomerId.Should().Be(customer.Id);
        response.MaskedMobileNumber.Should().BeNull();
        response.MaskedEmail.Should().BeNull();
    }

    [Fact]
    public async Task LookupIcAsync_WhenLegacyCustomerNotYetActive_ReturnsMaskedContacts()
    {
        Customer customer = CreateCustomer(CustomerStatus.PendingVerification, CustomerType.ExistingUser);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        IcLookupResponse response = await sut.LookupIcAsync(new IcLookupRequest { IcNumber = customer.IcNumber });

        response.Status.Should().Be(IcLookupStatus.ExistingUserNotMigrated);
        response.MaskedMobileNumber.Should().Be("•• •• ••• 6675");
        response.MaskedEmail.Should().Be("ma•••@•••••.com");
    }

    [Fact]
    public async Task PinLoginAsync_WhenCustomerNotActive_ThrowsAccountNotFound()
    {
        Customer customer = CreateCustomer(CustomerStatus.PinCreated, pinHash: PinHasher.Hash("123456"));
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        Func<Task> act = () => sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "123456" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task PinLoginAsync_WhenPinIsIncorrect_ThrowsUnmatchedPin()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active, pinHash: PinHasher.Hash("123456"));
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        Func<Task> act = () => sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "000000" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.UnmatchedPin);
    }

    [Fact]
    public async Task PinLoginAsync_WhenPinIsCorrect_ReturnsCustomerSummary()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active, pinHash: PinHasher.Hash("123456"));
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        PinLoginResponse response = await sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "123456" });

        response.CustomerId.Should().Be(customer.Id);
        response.FullName.Should().Be(customer.FullName);
        response.Status.Should().Be(nameof(CustomerStatus.Active));
    }

    [Fact]
    public async Task PinLoginAsync_WhenAlreadyLocked_ThrowsAccountLockedEvenWithCorrectPin()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active, pinHash: PinHasher.Hash("123456"));
        customer.PinLockedUntilUtc = DateTime.UtcNow.AddMinutes(10);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        Func<Task> act = () => sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "123456" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountLocked);
    }

    [Fact]
    public async Task PinLoginAsync_WhenFifthConsecutiveFailureOccurs_LocksAccount()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active, pinHash: PinHasher.Hash("123456"));
        customer.FailedPinAttempts = 4;
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        Func<Task> act = () => sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "000000" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.UnmatchedPin);
        customer.PinLockedUntilUtc.Should().NotBeNull();
        customer.PinLockedUntilUtc.Should().BeAfter(DateTime.UtcNow);
        customer.FailedPinAttempts.Should().Be(0);
    }

    [Fact]
    public async Task PinLoginAsync_WhenCorrectAfterPriorFailures_ResetsFailedAttempts()
    {
        Customer customer = CreateCustomer(CustomerStatus.Active, pinHash: PinHasher.Hash("123456"));
        customer.FailedPinAttempts = 3;
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        AuthService sut = new(dbContext.Object);

        await sut.PinLoginAsync(new PinLoginRequest { IcNumber = customer.IcNumber, Pin = "123456" });

        customer.FailedPinAttempts.Should().Be(0);
        customer.PinLockedUntilUtc.Should().BeNull();
    }
}
