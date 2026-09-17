using System.Net;
using KoperasiTentera.Application.Common;
using KoperasiTentera.Application.Dtos;
using KoperasiTentera.Application.Interfaces;
using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Application.Services;

public class HomeService : IHomeService
{
    private readonly IAppDbContext _dbContext;

    public HomeService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HomeResponse> GetHomeAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
        {
            throw new AppException(ErrorCodes.AccountNotFound, "Customer not found.", HttpStatusCode.NotFound);
        }

        string firstName = customer.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? customer.FullName;

        List<BannerDto> banners = await _dbContext.Banners
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BannerDto { Id = b.Id, Title = b.Title, Description = b.Description, ImageUrl = b.ImageUrl })
            .ToListAsync(cancellationToken);

        return new HomeResponse
        {
            Greeting = $"Hello, {firstName}",
            IsBiometricEnabled = customer.IsBiometricEnabled,
            Banners = banners
        };
    }
}
