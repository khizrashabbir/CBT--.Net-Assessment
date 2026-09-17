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

public class RegistrationServiceTests
{
    private static Customer CreateCustomer(CustomerStatus status) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Ahmad Bin Ismail",
        IcNumber = "990101011234",
        MobileNumber = "+60123456789",
        Email = "ahmad@example.com",
        CustomerType = CustomerType.NewCustomer,
        Status = status,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task StartAsync_WhenIcAlreadyExists_ThrowsAccountAlreadyExists()
    {
        Customer existing = CreateCustomer(CustomerStatus.Active);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { existing });
        Mock<IOtpService> otpService = new();
        RegistrationService sut = new(dbContext.Object, otpService.Object);

        Func<Task> act = () => sut.StartAsync(new RegistrationStartRequest
        {
            FullName = "Duplicate",
            IcNumber = existing.IcNumber,
            MobileNumber = "+60123456789",
            Email = "dup@example.com"
        });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountAlreadyExists);
        otpService.Verify(o => o.SendAsync(It.IsAny<OtpSendRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WhenIcIsNew_CreatesCustomerAndAutoIssuesMobileOtp()
    {
        List<Customer> addedCustomers = new();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(addedCustomers: addedCustomers);
        Mock<IOtpService> otpService = new();
        DateTime expiry = DateTime.UtcNow.AddMinutes(2);
        otpService
            .Setup(o => o.SendAsync(It.Is<OtpSendRequest>(r => r.OtpType == OtpTypeDto.Mobile), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OtpSendResponse { ExpiresAtUtc = expiry, OtpCode = "1234" });
        RegistrationService sut = new(dbContext.Object, otpService.Object);

        RegistrationStartResponse response = await sut.StartAsync(new RegistrationStartRequest
        {
            FullName = "Ahmad Bin Ismail",
            IcNumber = "990101011234",
            MobileNumber = "+60123456789",
            Email = "ahmad@example.com"
        });

        addedCustomers.Should().ContainSingle();
        addedCustomers[0].Status.Should().Be(CustomerStatus.PendingVerification);
        response.Status.Should().Be(nameof(CustomerStatus.PendingVerification));
        response.MobileOtpCode.Should().Be("1234");
        response.MobileOtpExpiresAtUtc.Should().Be(expiry);
        otpService.Verify(o => o.SendAsync(
            It.Is<OtpSendRequest>(r => r.OtpType == OtpTypeDto.Mobile && r.CustomerId == addedCustomers[0].Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptPolicyAsync_WhenCustomerNotEmailVerified_ThrowsInvalidState()
    {
        Customer customer = CreateCustomer(CustomerStatus.MobileVerified);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.AcceptPolicyAsync(new AcceptPolicyRequest { CustomerId = customer.Id });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.InvalidState);
    }

    [Fact]
    public async Task AcceptPolicyAsync_WhenEmailVerified_StampsConsentAndAdvancesStatus()
    {
        Customer customer = CreateCustomer(CustomerStatus.EmailVerified);
        PrivacyPolicy policy = new() { Id = Guid.NewGuid(), Version = "1.0", Content = "Terms", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            privacyPolicies: new List<PrivacyPolicy> { policy });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        RegistrationStepResponse response = await sut.AcceptPolicyAsync(new AcceptPolicyRequest { CustomerId = customer.Id });

        response.Status.Should().Be(nameof(CustomerStatus.PolicyAccepted));
        customer.AcceptedPolicyVersion.Should().Be("1.0");
        customer.PolicyAcceptedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePinAsync_WhenPinAndConfirmPinMismatch_ThrowsUnmatchedPin()
    {
        Customer customer = CreateCustomer(CustomerStatus.PolicyAccepted);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.CreatePinAsync(new CreatePinRequest { CustomerId = customer.Id, Pin = "123456", ConfirmPin = "654321" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.UnmatchedPin);
        customer.PinHash.Should().BeNull();
    }

    [Fact]
    public async Task CreatePinAsync_WhenPinsMatch_HashesPinAndAdvancesStatus()
    {
        Customer customer = CreateCustomer(CustomerStatus.PolicyAccepted);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        RegistrationStepResponse response = await sut.CreatePinAsync(new CreatePinRequest { CustomerId = customer.Id, Pin = "123456", ConfirmPin = "123456" });

        response.Status.Should().Be(nameof(CustomerStatus.PinCreated));
        customer.PinHash.Should().NotBeNullOrEmpty();
        PinHasher.Verify("123456", customer.PinHash!).Should().BeTrue();
    }

    [Fact]
    public async Task SetBiometricAsync_WhenPinNotCreated_ThrowsInvalidState()
    {
        Customer customer = CreateCustomer(CustomerStatus.PolicyAccepted);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.SetBiometricAsync(new BiometricRequest { CustomerId = customer.Id, Enable = true });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.InvalidState);
    }

    [Fact]
    public async Task SetBiometricAsync_WhenPinCreated_CompletesOnboarding()
    {
        Customer customer = CreateCustomer(CustomerStatus.PinCreated);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        RegistrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        RegistrationStepResponse response = await sut.SetBiometricAsync(new BiometricRequest { CustomerId = customer.Id, Enable = true });

        response.Status.Should().Be(nameof(CustomerStatus.Active));
        customer.Status.Should().Be(CustomerStatus.Active);
        customer.IsBiometricEnabled.Should().BeTrue();
    }
}
