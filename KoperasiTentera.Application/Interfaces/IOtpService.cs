using KoperasiTentera.Application.Dtos;

namespace KoperasiTentera.Application.Interfaces;

public interface IOtpService
{
    Task<OtpSendResponse> SendAsync(OtpSendRequest request, CancellationToken cancellationToken = default);

    Task<OtpVerifyResponse> VerifyAsync(OtpVerifyRequest request, CancellationToken cancellationToken = default);
}
