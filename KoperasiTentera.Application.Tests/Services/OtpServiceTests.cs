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

public class OtpServiceTests
{
    private static Customer CreateCustomer(CustomerStatus status = CustomerStatus.PendingVerification) => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test Customer",
        IcNumber = "990101011234",
        MobileNumber = "+60123456789",
        Email = "test@example.com",
        CustomerType = CustomerType.NewCustomer,
        Status = status,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static Mock<IAppEnvironment> CreateEnvironment(bool returnOtpInResponse = true)
    {
        Mock<IAppEnvironment> environment = new();
        environment.Setup(e => e.ReturnOtpInResponse).Returns(returnOtpInResponse);
        return environment;
    }

    [Fact]
    public async Task SendAsync_WhenCustomerDoesNotExist_ThrowsAccountNotFound()
    {
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create();
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.SendAsync(new OtpSendRequest { CustomerId = Guid.NewGuid(), OtpType = OtpTypeDto.Mobile });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task SendAsync_WhenNoExistingOtp_CreatesOtpAndReturnsCodeInDevelopment()
    {
        Customer customer = CreateCustomer();
        List<OtpVerification> addedOtps = new();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            addedOtpVerifications: addedOtps);
        OtpService sut = new(dbContext.Object, CreateEnvironment(returnOtpInResponse: true).Object);

        OtpSendResponse response = await sut.SendAsync(new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile });

        addedOtps.Should().ContainSingle();
        response.OtpCode.Should().Be(addedOtps[0].Code);
        response.OtpCode.Should().MatchRegex("^[0-9]{4}$");
        response.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(2), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SendAsync_WhenNotDevelopment_DoesNotReturnCode()
    {
        Customer customer = CreateCustomer();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            addedOtpVerifications: new List<OtpVerification>());
        OtpService sut = new(dbContext.Object, CreateEnvironment(returnOtpInResponse: false).Object);

        OtpSendResponse response = await sut.SendAsync(new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile });

        response.OtpCode.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenRecentOtpExists_ThrowsResendNotAllowed()
    {
        Customer customer = CreateCustomer();
        OtpVerification recentOtp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(-30),
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(90),
            IsUsed = false
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { recentOtp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.SendAsync(new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile });

        AppException exception = (await act.Should().ThrowAsync<AppException>()).Which;
        exception.Code.Should().Be(ErrorCodes.ResendNotAllowed);
    }

    [Fact]
    public async Task SendAsync_WhenCooldownElapsed_InvalidatesPreviousOtpAndIssuesNewOne()
    {
        Customer customer = CreateCustomer();
        OtpVerification oldOtp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            IsUsed = false
        };
        List<OtpVerification> addedOtps = new();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { oldOtp },
            addedOtpVerifications: addedOtps);
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        await sut.SendAsync(new OtpSendRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile });

        oldOtp.IsUsed.Should().BeTrue();
        addedOtps.Should().ContainSingle();
    }

    [Fact]
    public async Task VerifyAsync_WhenNoActiveOtp_ThrowsOtpExpired()
    {
        Customer customer = CreateCustomer();
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile, Code = "1234" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpExpired);
    }

    [Fact]
    public async Task VerifyAsync_WhenMaxAttemptsReached_ThrowsOtpMaxAttempts()
    {
        Customer customer = CreateCustomer();
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            FailedAttempts = 3
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile, Code = "1234" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpMaxAttempts);
    }

    [Fact]
    public async Task VerifyAsync_WhenExpired_ThrowsOtpExpired()
    {
        Customer customer = CreateCustomer();
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            IsUsed = false
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile, Code = "1234" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.OtpExpired);
    }

    [Fact]
    public async Task VerifyAsync_WhenCodeIsIncorrect_IncrementsAttemptsAndThrowsIncorrectOtp()
    {
        Customer customer = CreateCustomer();
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false,
            FailedAttempts = 0
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile, Code = "0000" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.IncorrectOtp);
        otp.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task VerifyAsync_WhenMobileCodeIsCorrect_AdvancesCustomerToMobileVerified()
    {
        Customer customer = CreateCustomer(CustomerStatus.PendingVerification);
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Mobile,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        OtpVerifyResponse response = await sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Mobile, Code = "1234" });

        response.Status.Should().Be(nameof(CustomerStatus.MobileVerified));
        customer.Status.Should().Be(CustomerStatus.MobileVerified);
        otp.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WhenEmailOtpVerifiedBeforeMobile_ThrowsInvalidState()
    {
        Customer customer = CreateCustomer(CustomerStatus.PendingVerification);
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Email,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        Func<Task> act = () => sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Email, Code = "1234" });

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.InvalidState);
    }

    [Fact]
    public async Task VerifyAsync_WhenEmailCodeIsCorrectAfterMobileVerified_AdvancesToEmailVerified()
    {
        Customer customer = CreateCustomer(CustomerStatus.MobileVerified);
        OtpVerification otp = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            OtpType = OtpType.Email,
            Code = "1234",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(
            customers: new List<Customer> { customer },
            otpVerifications: new List<OtpVerification> { otp });
        OtpService sut = new(dbContext.Object, CreateEnvironment().Object);

        OtpVerifyResponse response = await sut.VerifyAsync(new OtpVerifyRequest { CustomerId = customer.Id, OtpType = OtpTypeDto.Email, Code = "1234" });

        response.Status.Should().Be(nameof(CustomerStatus.EmailVerified));
        customer.Status.Should().Be(CustomerStatus.EmailVerified);
    }
}
