using FluentAssertions;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Application.Services;
using KoperasiTentera.Application.Tests.TestHelpers;
using KoperasiTentera.Domain.Entities;
using KoperasiTentera.Domain.Enums;
using Moq;
using Xunit;

namespace KoperasiTentera.Application.Tests.Services;

public class HomeServiceTests
{
    [Fact]
    public async Task GetHomeAsync_WhenCustomerNotFound_ThrowsAccountNotFound()
    {
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create();
        HomeService sut = new(dbContext.Object);

        Func<Task> act = () => sut.GetHomeAsync(Guid.NewGuid());

        (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be(ErrorCodes.AccountNotFound);
    }

    [Fact]
    public async Task GetHomeAsync_WhenCustomerExists_ReturnsGreetingAndActiveBannersOnly()
    {
        Customer customer = new()
        {
            Id = Guid.NewGuid(),
            FullName = "Mariam Abdul Rashid",
            IcNumber = "880214566831",
            MobileNumber = "+60123456675",
            Email = "mariam.rashid@example.com",
            CustomerType = CustomerType.ExistingUser,
            Status = CustomerStatus.Active,
            IsBiometricEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        List<Banner> banners = new()
        {
            new Banner { Id = Guid.NewGuid(), Title = "Active Banner", Description = "d", ImageUrl = "i", IsActive = true, SortOrder = 2 },
            new Banner { Id = Guid.NewGuid(), Title = "Inactive Banner", Description = "d", ImageUrl = "i", IsActive = false, SortOrder = 1 },
            new Banner { Id = Guid.NewGuid(), Title = "First Banner", Description = "d", ImageUrl = "i", IsActive = true, SortOrder = 1 }
        };
        Mock<IAppDbContext> dbContext = MockAppDbContextFactory.Create(customers: new List<Customer> { customer }, banners: banners);
        HomeService sut = new(dbContext.Object);

        var response = await sut.GetHomeAsync(customer.Id);

        response.Greeting.Should().Be("Hello, Mariam");
        response.IsBiometricEnabled.Should().BeTrue();
        response.Banners.Should().HaveCount(2);
        response.Banners.Select(b => b.Title).Should().ContainInOrder("First Banner", "Active Banner");
    }
}
