using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Interfaces;

public interface IHomeService
{
    Task<HomeResponse> GetHomeAsync(Guid customerId, CancellationToken cancellationToken = default);
}
