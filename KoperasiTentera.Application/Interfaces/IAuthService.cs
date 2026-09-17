using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Interfaces;

public interface IAuthService
{
    Task<IcLookupResponse> LookupIcAsync(IcLookupRequest request, CancellationToken cancellationToken = default);

    Task<PinLoginResponse> PinLoginAsync(PinLoginRequest request, CancellationToken cancellationToken = default);
}
