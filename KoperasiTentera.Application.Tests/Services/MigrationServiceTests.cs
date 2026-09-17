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

public class MigrationServiceTests
{
    private static Customer CreateLegacyCustomer(CustomerStatus status = CustomerStatus.PendingVerification) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Mariam Abdul Rashid",
        IcNumber = "880214566831",
        MobileNumber = "+60123456675",
        Email = "mariam.rashid@example.com",
        CustomerType = CustomerType.ExistingUser,
        Status = status,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task StartAsync_WhenLegacyCustomerNotFound_ThrowsAccountNotFound()
    {
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create();
        MigrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.StartAsync(new MigrationStartRequest { IcNumber = "880214566831" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyMigrated_ThrowsInvalidState()
    {
        Customer customer = CreateLegacyCustomer(CustomerStatus.Active);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        MigrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.StartAsync(new MigrationStartRequest { IcNumber = customer.IcNumber });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.InvalidState);
    }

    [Fact]
    public async Task StartAsync_WhenLegacyCustomerFound_ReturnsMaskedContactsAndIssuesMobileOtp()
    {
        Customer customer = CreateLegacyCustomer();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        Mock<IOtpService> otpService = new();
        DateTime expiry = DateTime.UtcNow.AddMinutes(2);
        otpService
            .Setup(o => o.SendAsync(It.Is<OtpSendRequest>(r => r.OtpType == OtpTypeDto.Mobile && r.CustomerId == customer.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OtpSendResponse { ExpiresAtUtc = expiry, OtpCode = "5678" });
        MigrationService sut = new(dbContext.Object, otpService.Object);

        MigrationStartResponse response = await sut.StartAsync(new MigrationStartRequest { IcNumber = customer.IcNumber });

        response.CustomerId.Should().Be(customer.Id);
        response.MaskedMobileNumber.Should().Be("•• •• ••• 6675");
        response.MaskedEmail.Should().Be("ma•••@•••••.com");
        response.MobileOtpCode.Should().Be("5678");
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenCustomerAlreadyActive_ThrowsInvalidState()
    {
        Customer customer = CreateLegacyCustomer(CustomerStatus.Active);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        MigrationService sut = new(dbContext.Object, Mock.Of<IOtpService>());

        Func<Task> act = () => sut.ChangeEmailAsync(new ChangeEmailRequest { CustomerId = customer.Id, NewEmail = "new@example.com" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.InvalidState);
    }

    [Fact]
    public async Task ChangeEmailAsync_WhenValid_UpdatesEmailAndReissuesEmailOtp()
    {
        Customer customer = CreateLegacyCustomer(CustomerStatus.MobileVerified);
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        Mock<IOtpService> otpService = new();
        otpService
            .Setup(o => o.SendAsync(It.Is<OtpSendRequest>(r => r.OtpType == OtpTypeDto.Email && r.CustomerId == customer.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OtpSendResponse { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2), OtpCode = "4321" });
        MigrationService sut = new(dbContext.Object, otpService.Object);

        ChangeEmailResponse response = await sut.ChangeEmailAsync(new ChangeEmailRequest { CustomerId = customer.Id, NewEmail = "mariam.new@example.com" });

        customer.Email.Should().Be("mariam.new@example.com");
        response.MaskedEmail.Should().Be("ma•••@•••••.com");
        response.EmailOtpCode.Should().Be("4321");
    }
}
